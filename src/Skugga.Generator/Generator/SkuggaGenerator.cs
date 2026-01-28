#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace Skugga.Generator
{
    [Generator]
    public class SkuggaGenerator : IIncrementalGenerator
    {
        private static readonly DiagnosticDescriptor SealedClassRule = new(
            id: "SKUGGA001",
            title: "Cannot mock sealed class",
            messageFormat: "Cannot mock sealed class '{0}'. Sealed classes cannot be mocked.",
            category: "Skugga",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "Skugga cannot generate mocks for sealed classes.");

        private static readonly DiagnosticDescriptor NoVirtualMembersRule = new(
            id: "SKUGGA002",
            title: "Class has no virtual members",
            messageFormat: "Class '{0}' has no virtual members to mock. Consider mocking an interface instead or make members virtual.",
            category: "Skugga",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Classes must have virtual members to be mocked effectively.");

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var provider = context.SyntaxProvider.CreateSyntaxProvider(
                predicate: (node, _) => node is InvocationExpressionSyntax,
                transform: (ctx, _) => GetTarget(ctx)
            ).Where(m => m != null);

            var compilationAndClasses = context.CompilationProvider.Combine(provider.Collect());

            context.RegisterSourceOutput(compilationAndClasses, (spc, source) =>
            {
                var (compilation, targets) = source;
                var distinctMocks = new HashSet<string>();
                var mockQueue = new Queue<TargetInfo>();

                // Initial targets from user code (Mock.Create, etc.)
                foreach (var target in targets)
                {
                    if (target == null) continue;

                    if (target.Type == TargetType.Harness)
                    {
                        InterceptorGenerator.GenerateInterceptor(spc, target);
                        HarnessGenerator.GenerateHarnessClass(spc, target);
                    }
                    else if (target.Type == TargetType.Mock || target.Type == TargetType.Repository || target.Type == TargetType.MockOf)
                    {
                        InterceptorGenerator.GenerateInterceptor(spc, target);
                        mockQueue.Enqueue(target);
                    }
                    else if (target.Type == TargetType.AutoScribe)
                    {
                        InterceptorGenerator.GenerateInterceptor(spc, target);

                        var symbolKey = target.Symbol!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                        if (distinctMocks.Add(symbolKey + "_AutoScribe"))
                        {
                            RecordingProxyGenerator.GenerateRecordingProxy(spc, target);
                        }
                    }
                    else if (target.Type == TargetType.Setup || target.Type == TargetType.Verify || target.Type == TargetType.SetupSet || target.Type == TargetType.VerifySet)
                    {
                        if (target.MethodName != null && target.Arguments != null)
                        {
                            SetupVerifyInterceptorGenerator.GenerateSetupVerifyInterceptor(spc, target);
                        }
                    }
                }

                // Process mock queue and discover recursive mocks
                var generatedMocks = new List<(string BaseType, string MockClassName)>();
                while (mockQueue.Count > 0)
                {
                    var target = mockQueue.Dequeue();
                    var symbol = target.Symbol;
                    if (symbol == null) continue;

                    var symbolKey = symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                    if (!distinctMocks.Add(symbolKey)) continue;

                    // Generate the mock class
                    var mockInfo = MockGenerator.GenerateMockClass(spc, target, SealedClassRule, NoVirtualMembersRule);
                    if (mockInfo.HasValue)
                    {
                        generatedMocks.Add(mockInfo.Value);
                    }

                    // Scan members for return types that might need mocking (recursive mocking)
                    foreach (var method in GeneratorHelpers.GetAllMethods(symbol))
                    {
                        if (method.MethodKind != MethodKind.Ordinary) continue;
                        EnqueueRecursiveType(method.ReturnType, target.Location, mockQueue, distinctMocks);
                    }

                    foreach (var prop in GeneratorHelpers.GetAllProperties(symbol))
                    {
                        EnqueueRecursiveType(prop.Type, target.Location, mockQueue, distinctMocks);
                    }
                }

                if (generatedMocks.Count > 0)
                {
                    GeneratorHelpers.GenerateModuleInitializer(spc, generatedMocks);
                }
            });
        }

        private static void EnqueueRecursiveType(ITypeSymbol type, Location location, Queue<TargetInfo> queue, HashSet<string> distinctMocks)
        {
            // Unwrap Task/ValueTask
            var actualType = type;
            if (type is INamedTypeSymbol named &&
                (named.ToDisplayString().StartsWith("System.Threading.Tasks.Task") ||
                 named.ToDisplayString().StartsWith("System.Threading.Tasks.ValueTask")))
            {
                if (named.IsGenericType && named.TypeArguments.Length > 0)
                {
                    actualType = named.TypeArguments[0];
                }
            }

            if (actualType is INamedTypeSymbol namedSymbol && (namedSymbol.TypeKind == TypeKind.Interface || namedSymbol.IsAbstract))
            {
                // Skip special types that cannot be inherited/mocked
                var fullName = namedSymbol.ToDisplayString();
                if (fullName == "System.Delegate" || fullName == "System.MulticastDelegate" ||
                    fullName == "System.Enum" || fullName == "System.ValueType" || fullName == "System.Array")
                {
                    return;
                }

                var key = namedSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                if (!distinctMocks.Contains(key))
                {
                    queue.Enqueue(new TargetInfo(namedSymbol, location, TargetType.Mock, null, null));
                }
            }
        }

        private static string? TryExtractVariableArgument(GeneratorSyntaxContext context, ExpressionSyntax expression)
        {
            switch (expression)
            {
                case IdentifierNameSyntax identifier:
                    return identifier.Identifier.Text;
                case MemberAccessExpressionSyntax memberAccess:
                    var operation = context.SemanticModel.GetOperation(memberAccess);
                    if (operation is IFieldReferenceOperation fieldRef && fieldRef.Field.IsConst)
                    {
                        var constantValue = context.SemanticModel.GetConstantValue(memberAccess);
                        if (constantValue.HasValue) return GeneratorHelpers.FormatConstantValue(constantValue.Value);
                    }
                    return memberAccess.ToString();
                case BinaryExpressionSyntax binaryExpr:
                    var binaryConstant = context.SemanticModel.GetConstantValue(binaryExpr);
                    if (binaryConstant.HasValue) return GeneratorHelpers.FormatConstantValue(binaryConstant.Value);
                    return $"({binaryExpr})";
                case InvocationExpressionSyntax invocation:
                    return invocation.ToString();
                case ElementAccessExpressionSyntax elementAccess:
                    return elementAccess.ToString();
                case CastExpressionSyntax castExpr:
                    var innerExtracted = TryExtractVariableArgument(context, castExpr.Expression);
                    if (innerExtracted != null) return $"({castExpr.Type}){innerExtracted}";
                    return castExpr.ToString();
                case ParenthesizedExpressionSyntax parenthesized:
                    return TryExtractVariableArgument(context, parenthesized.Expression);
                case ConditionalExpressionSyntax conditional:
                    return conditional.ToString();
                case PrefixUnaryExpressionSyntax or PostfixUnaryExpressionSyntax:
                    return expression.ToString();
                default:
                    return null;
            }
        }

        private static ArgumentInfo ParseArgument(GeneratorSyntaxContext context, ExpressionSyntax expression, bool isRefOrOut, int index)
        {
            var constantValue = context.SemanticModel.GetConstantValue(expression);
            if (constantValue.HasValue)
            {
                return new ArgumentInfo(ArgumentKind.Value, GeneratorHelpers.FormatConstantValue(constantValue.Value), isRefOrOut: isRefOrOut);
            }
            else if (expression is InvocationExpressionSyntax argInvocation)
            {
                var symbol = context.SemanticModel.GetSymbolInfo(argInvocation).Symbol as IMethodSymbol;
                if (symbol != null && symbol.ContainingType.Name == "It" && symbol.ContainingNamespace.ToString() == "Skugga.Core")
                {
                    if (symbol.Name == "IsAny")
                    {
                        var type = symbol.TypeArguments[0].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                        return new ArgumentInfo(ArgumentKind.MatcherIsAny, type, isRefOrOut: isRefOrOut);
                    }
                    else if (symbol.Name == "Is" && argInvocation.ArgumentList.Arguments.Count == 1)
                    {
                        var type = symbol.TypeArguments[0].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                        var predicate = argInvocation.ArgumentList.Arguments[0].Expression.ToString();
                        return new ArgumentInfo(ArgumentKind.MatcherIs, type, predicate, isRefOrOut: isRefOrOut);
                    }
                }

                // Fallback for other invocations or unknown It methods
                return new ArgumentInfo(ArgumentKind.Expression, $"__RUNTIME_EXTRACT__{index}", isRefOrOut: isRefOrOut);
            }
            else
            {
                // Any non-constant (variable, property, calculation)
                // is tagged for runtime extraction in the interceptor
                return new ArgumentInfo(ArgumentKind.Expression, $"__RUNTIME_EXTRACT__{index}", isRefOrOut: isRefOrOut);
            }
        }



        private static TargetInfo? GetTarget(GeneratorSyntaxContext context)
        {
            var invocation = (InvocationExpressionSyntax)context.Node;
            if (invocation.Expression is MemberAccessExpressionSyntax member)
            {
                var methodName = member.Name.Identifier.Text;
                if (methodName == "Create" || methodName == "Capture")
                {
                    var expressionStr = member.Expression.ToString();
                    TargetType? targetType = null;
                    if (expressionStr == "Mock" || expressionStr.EndsWith(".Mock")) targetType = TargetType.Mock;
                    else if (expressionStr == "Harness" || expressionStr.EndsWith(".Harness")) targetType = TargetType.Harness;
                    else if ((expressionStr == "AutoScribe" || expressionStr.EndsWith(".AutoScribe")) && methodName == "Capture")
                        targetType = TargetType.AutoScribe;
                    else
                    {
                        var exprType = context.SemanticModel.GetTypeInfo(member.Expression).Type;
                        if (exprType != null && (exprType.Name == "MockRepository" || exprType.ToDisplayString().EndsWith(".MockRepository")))
                        {
                            targetType = TargetType.Repository;
                        }
                    }

                    if (targetType.HasValue && member.Name is GenericNameSyntax genericName)
                    {
                        var typeArg = genericName.TypeArgumentList.Arguments.FirstOrDefault();
                        if (typeArg != null)
                        {
                            var symbol = context.SemanticModel.GetTypeInfo(typeArg).Type as INamedTypeSymbol;
                            if (symbol != null)
                            {
                                var info = new TargetInfo(symbol, member.Name.Identifier.GetLocation(), targetType.Value, null, invocation);

                                // Determine overload
                                if (methodName == "Create" && (targetType == TargetType.Mock || targetType == TargetType.Repository))
                                {
                                    if (invocation.ArgumentList.Arguments.Count == 2)
                                    {
                                        info.Overload = CreateOverload.BehaviorAndDefaultValue;
                                    }
                                    else if (invocation.ArgumentList.Arguments.Count == 1)
                                    {
                                        var argType = context.SemanticModel.GetTypeInfo(invocation.ArgumentList.Arguments[0].Expression).Type;
                                        if (argType != null && argType.Name == "DefaultValue")
                                        {
                                            info.Overload = CreateOverload.DefaultValue;
                                        }
                                        else
                                        {
                                            info.Overload = CreateOverload.Behavior;
                                        }
                                    }
                                    else
                                    {
                                        info.Overload = CreateOverload.Behavior;
                                    }
                                }

                                return info;
                            }
                        }
                    }
                }
                else if (methodName == "Of")
                {
                    var expressionStr = member.Expression.ToString();
                    if (expressionStr == "Mock" || expressionStr.EndsWith(".Mock"))
                    {
                        if (member.Name is GenericNameSyntax genericName)
                        {
                            var typeArg = genericName.TypeArgumentList.Arguments.FirstOrDefault();
                            if (typeArg != null)
                            {
                                var symbol = context.SemanticModel.GetTypeInfo(typeArg).Type as INamedTypeSymbol;
                                if (symbol != null)
                                {
                                    return new TargetInfo(symbol, member.Name.Identifier.GetLocation(), TargetType.MockOf, null, invocation);
                                }
                            }
                        }
                    }
                }
                else if (methodName == "Setup" || methodName == "Verify" || methodName == "SetupSet" || methodName == "VerifySet")
                {
                    var mockType = context.SemanticModel.GetTypeInfo(member.Expression).Type;
                    if (mockType != null && mockType.ToDisplayString().StartsWith("Moq.Mock<"))
                    {
                        return null; // Skip Moq's Mock<T>
                    }
                    if (invocation.ArgumentList.Arguments.Count > 0)
                    {
                        var lambdaArg = invocation.ArgumentList.Arguments[0].Expression;
                        var targetType = methodName == "Setup" ? TargetType.Setup :
                                         methodName == "SetupSet" ? TargetType.SetupSet :
                                         methodName == "VerifySet" ? TargetType.VerifySet :
                                         TargetType.Verify;
                        var parsed = ParseLambdaExpression(context, lambdaArg);
                        if (parsed.HasValue && parsed.Value.methodName != null)
                        {
                            if (methodName == "VerifySet" && parsed.Value.methodName.StartsWith("get_"))
                            {
                                return null; // Skip old VerifySet version
                            }

                            bool isFunc = false;
                            var methodSymbol = context.SemanticModel.GetSymbolInfo(invocation).Symbol as IMethodSymbol;
                            if (methodSymbol != null && methodSymbol.IsGenericMethod && methodSymbol.TypeArguments.Length == 2)
                            {
                                isFunc = true;
                            }
                            else if (parsed.Value.isProperty && targetType != TargetType.SetupSet && targetType != TargetType.VerifySet)
                            {
                                isFunc = true;
                            }

                            return new TargetInfo(mockType as INamedTypeSymbol, member.Name.Identifier.GetLocation(),
                                targetType, lambdaArg, invocation, parsed.Value.methodName,
                                parsed.Value.arguments, parsed.Value.isProperty, isFunc, parsed.Value.isRecursive);
                        }
                    }
                }
            }
            return null;
        }

        private static (string? methodName, List<ArgumentInfo> arguments, bool isProperty, bool isRecursive)? ParseLambdaExpression(
            GeneratorSyntaxContext context, ExpressionSyntax lambdaExpr)
        {
            string paramName;
            ExpressionSyntax? body;

            if (lambdaExpr is SimpleLambdaExpressionSyntax simple)
            {
                paramName = simple.Parameter.Identifier.Text;
                body = simple.Body as ExpressionSyntax;
            }
            else if (lambdaExpr is ParenthesizedLambdaExpressionSyntax parenthesized)
            {
                if (parenthesized.ParameterList.Parameters.Count == 0) return null;
                paramName = parenthesized.ParameterList.Parameters[0].Identifier.Text;
                body = parenthesized.Body as ExpressionSyntax;
            }
            else
            {
                return null; // Not a lambda
            }

            if (body is InvocationExpressionSyntax invocation &&
                invocation.Expression is MemberAccessExpressionSyntax methodAccess)
            {
                var methodName = methodAccess.Name.Identifier.Text;
                var arguments = new List<ArgumentInfo>();

                // Check for recursion: if the expression on left of .Method() is NOT the parameter
                var expressionStr = methodAccess.Expression.ToString();
                var isRecursive = expressionStr != paramName;

                var methodSymbol = context.SemanticModel.GetSymbolInfo(invocation).Symbol as IMethodSymbol;
                if (methodSymbol != null)
                {
                    var parameters = methodSymbol.Parameters;
                    var syntaxArguments = invocation.ArgumentList.Arguments;

                    for (int i = 0; i < parameters.Length; i++)
                    {
                        var parameter = parameters[i];

                        // If it's a params call with multiple arguments in syntax,
                        // we MUST use the parameter's index for the runtime extraction.
                        if (parameter.IsParams && syntaxArguments.Count > parameters.Length)
                        {
                            // Point to the combined array at index i in the runtime MethodCallExpression
                            arguments.Add(new ArgumentInfo(ArgumentKind.Expression, $"__RUNTIME_EXTRACT__{i}", isRefOrOut: false));
                        }
                        else if (i < syntaxArguments.Count)
                        {
                            var arg = syntaxArguments[i];
                            bool isRefOrOut = !arg.RefOrOutKeyword.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.None);
                            arguments.Add(ParseArgument(context, arg.Expression, isRefOrOut, i));
                        }
                    }
                    return (methodName, arguments, false, isRecursive);
                }

                for (int i = 0; i < invocation.ArgumentList.Arguments.Count; i++)
                {
                    var arg = invocation.ArgumentList.Arguments[i];
                    bool isRefOrOut = !arg.RefOrOutKeyword.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.None);
                    arguments.Add(ParseArgument(context, arg.Expression, isRefOrOut, i));
                }
                return (methodName, arguments, false, isRecursive);
            }

            if (body is MemberAccessExpressionSyntax propAccess)
            {
                // Check for recursion
                var expressionStr = propAccess.Expression.ToString();
                var isRecursive = expressionStr != paramName;

                var propSymbol = context.SemanticModel.GetSymbolInfo(propAccess).Symbol as IPropertySymbol;
                if (propSymbol != null)
                {
                    return ("get_" + propSymbol.Name, new List<ArgumentInfo>(), true, isRecursive);
                }
            }

            if (body is AssignmentExpressionSyntax assignment &&
                assignment.Left is MemberAccessExpressionSyntax memberAssignment)
            {
                // Check for recursion
                var expressionStr = memberAssignment.Expression.ToString();
                var isRecursive = expressionStr != paramName;

                var symbol = context.SemanticModel.GetSymbolInfo(memberAssignment).Symbol;
                if (symbol is IPropertySymbol property)
                {
                    var methodName = "set_" + property.Name;
                    var arguments = new List<ArgumentInfo>
                    {
                        ParseArgument(context, assignment.Right, false, 0)
                    };
                    return (methodName, arguments, true, isRecursive);
                }
            }

            return null;
        }
    }
}

