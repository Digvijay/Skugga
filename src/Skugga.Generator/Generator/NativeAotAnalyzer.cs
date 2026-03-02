#nullable enable
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Skugga.Generator;

/// <summary>
/// Roslyn diagnostic analyzer that prevents the use of reflection-based mocking features
/// (such as Mock.Of&lt;T&gt;) when the project targets Native AOT.
/// </summary>
/// <remarks>
/// <para>
/// Mock.Of&lt;T&gt;() relies on LINQ expression compilation and reflection to evaluate
/// property setup expressions at runtime. These mechanisms are incompatible with
/// Native AOT's ahead-of-time compilation model, which strips the JIT and trims
/// reflection metadata.
/// </para>
/// <para>
/// This analyzer reports SKUGGA003 when it detects a call to Mock.Of&lt;T&gt;() in a project
/// that has PublishAot=true or IsAotCompatible=true set in its build properties.
/// The recommended replacement is Mock.Create&lt;T&gt;() with explicit Setup() calls.
/// </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class NativeAotAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// Diagnostic ID for Mock.Of usage in AOT-targeted projects.
    /// </summary>
    public const string DiagnosticId = "SKUGGA003";

    private static readonly LocalizableString Title =
        "Mock.Of<T> is incompatible with Native AOT";

    private static readonly LocalizableString MessageFormat =
        "Mock.Of<T> uses reflection-based expression evaluation and is incompatible with Native AOT. Use Mock.Create<T>() with explicit Setup() calls instead.";

    private static readonly LocalizableString Description =
        "Mock.Of<T> relies on System.Linq.Expressions compilation and System.Reflection at runtime, " +
        "which are trimmed or unavailable under Native AOT. Replace with Mock.Create<T>() and call " +
        ".Setup(x => x.Property).Returns(value) for each property.";

    private const string Category = "Skugga";

    /// <summary>
    /// The diagnostic descriptor for SKUGGA003.
    /// </summary>
    public static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
        DiagnosticId,
        Title,
        MessageFormat,
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: Description,
        helpLinkUri: "https://github.com/Digvijay/Skugga#aot-constraint-mockoft-limitation");

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(Rule);

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(compilationStartContext =>
        {
            // Check if the project targets Native AOT via MSBuild properties
            var options = compilationStartContext.Options.AnalyzerConfigOptionsProvider;
            bool isAotTarget = false;

            // Check global analyzer config options for PublishAot or IsAotCompatible
            if (options.GlobalOptions.TryGetValue("build_property.PublishAot", out var publishAot))
            {
                isAotTarget = string.Equals(publishAot, "true", System.StringComparison.OrdinalIgnoreCase);
            }

            if (!isAotTarget && options.GlobalOptions.TryGetValue("build_property.IsAotCompatible", out var isAotCompatible))
            {
                isAotTarget = string.Equals(isAotCompatible, "true", System.StringComparison.OrdinalIgnoreCase);
            }

            if (!isAotTarget)
            {
                // Not targeting AOT, skip analysis
                return;
            }

            // Find the Mock class symbol
            var mockType = compilationStartContext.Compilation.GetTypeByMetadataName("Skugga.Core.Mock");
            if (mockType == null)
                return;

            // Register syntax node action for invocation expressions
            compilationStartContext.RegisterSyntaxNodeAction(syntaxNodeContext =>
            {
                var invocation = (InvocationExpressionSyntax)syntaxNodeContext.Node;

                // Check if this is a call to Mock.Of<T>(...)
                if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
                {
                    if (memberAccess.Name.Identifier.Text == "Of")
                    {
                        // Verify it's from Skugga.Core.Mock
                        var symbolInfo = syntaxNodeContext.SemanticModel.GetSymbolInfo(invocation);
                        if (symbolInfo.Symbol is IMethodSymbol methodSymbol &&
                            SymbolEqualityComparer.Default.Equals(methodSymbol.ContainingType, mockType))
                        {
                            var diagnostic = Diagnostic.Create(Rule, invocation.GetLocation());
                            syntaxNodeContext.ReportDiagnostic(diagnostic);
                        }
                    }
                }
            }, SyntaxKind.InvocationExpression);
        });
    }
}
