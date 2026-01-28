#nullable enable
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Skugga.Generator
{
    internal enum TargetType { Mock, Harness, Setup, Verify, AutoScribe, SetupSet, VerifySet, Repository, MockOf }

    internal class TargetInfo
    {
        public INamedTypeSymbol? Symbol;
        public Location Location;
        public TargetType Type;
        public ExpressionSyntax? LambdaExpression;
        public InvocationExpressionSyntax? InvocationSyntax;
        public string? MethodName;
        public List<ArgumentInfo>? Arguments;
        public bool IsProperty;
        public bool IsFunc;
        public bool IsRecursive;

        public TargetInfo(INamedTypeSymbol? s, Location l, TargetType t, ExpressionSyntax? lambda = null,
            InvocationExpressionSyntax? invocation = null, string? methodName = null,
            List<ArgumentInfo>? args = null, bool isProperty = false, bool isFunc = false, bool isRecursive = false)
        {
            Symbol = s;
            Location = l;
            Type = t;
            LambdaExpression = lambda;
            InvocationSyntax = invocation;
            MethodName = methodName;
            Arguments = args;
            IsProperty = isProperty;
            IsFunc = isFunc;
            IsRecursive = isRecursive;
        }

        public CreateOverload Overload { get; set; } = CreateOverload.Behavior;
    }

    internal enum ArgumentKind
    {
        Value,          // Constant or simple value
        Expression,     // Needs runtime extraction via GetArgumentValue
        MatcherIsAny,   // It.IsAny<T>()
        MatcherIs       // It.Is<T>(predicate)
    }

    internal class ArgumentInfo
    {
        public ArgumentKind Kind;
        public string ValueOrExpression; // The value string, or expression code, or type name for Matcher
        public string? Predicate;        // For MatcherIs
        public bool IsRefOrOut;          // Whether the argument has ref or out modifier

        public ArgumentInfo(ArgumentKind kind, string value, string? predicate = null, bool isRefOrOut = false)
        {
            Kind = kind;
            ValueOrExpression = value;
            Predicate = predicate;
            IsRefOrOut = isRefOrOut;
        }
    }

    internal enum CreateOverload
    {
        Behavior,
        DefaultValue,
        BehaviorAndDefaultValue
    }
}
