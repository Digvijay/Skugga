using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Skugga.Generator.Tests;

/// <summary>
/// The generator runs inside every consumer's IDE session, re-evaluating its predicate for every
/// node in every file on every keystroke. These tests pin the properties that keep that cheap.
/// </summary>
public class GeneratorIncrementalityTests
{
    private const string Source = """
        namespace Skugga.Core
        {
            public static class Mock { public static T Create<T>() => default!; }
        }

        namespace App
        {
            using Skugga.Core;
            using System.Linq;
            using System.Collections.Generic;

            public interface IService { int Work(); }

            public class Consumer
            {
                public void Run()
                {
                    var mock = Mock.Create<IService>();

                    var items = new List<int> { 1, 2, 3 };
                    items.Add(4);
                    items.Remove(1);
                    var mapped = items.Select(i => i * 2).Where(i => i > 2).ToList();
                    System.Console.WriteLine(mapped.Count);
                    System.Console.WriteLine(items.Sum());
                }
            }
        }
        """;

    /// <summary>
    /// The predicate must reject on syntax alone. It previously matched every invocation in the
    /// compilation, which meant the semantic model was consulted for every call in the user's
    /// codebase — the most expensive thing a generator can do — and almost all of it discarded.
    /// </summary>
    [Fact]
    public void Predicate_matches_only_skugga_calls_not_every_invocation()
    {
        var tree = CSharpSyntaxTree.ParseText(Source);
        var invocations = tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().ToList();

        var candidates = invocations.Where(SkuggaGenerator.IsCandidateInvocation).ToList();

        // The sample deliberately contains plenty of ordinary calls alongside the single Skugga one.
        invocations.Count.Should().BeGreaterThan(8, "the sample must contain unrelated invocations");
        candidates.Should().ContainSingle("only the Mock.Create call is a Skugga call")
            .Which.ToString().Should().Contain("Mock.Create<IService>");
    }

    /// <summary>
    /// A predicate is only cheap if it is also correct: every call shape the transform can act on
    /// must survive it, or the generator silently stops generating.
    /// </summary>
    [Theory]
    [InlineData("Mock.Create<IService>()")]
    [InlineData("Mock.Of<IService>()")]
    [InlineData("AutoScribe.Capture<IService>(x)")]
    [InlineData("Harness.Create<IService>()")]
    [InlineData("m.Setup(x => x.Work())")]
    [InlineData("m.Verify(x => x.Work())")]
    [InlineData("m.SetupSet(x => x.Value)")]
    [InlineData("m.VerifySet(x => x.Value)")]
    public void Predicate_accepts_every_intercepted_call_shape(string expression)
    {
        var invocation = CSharpSyntaxTree.ParseText($"class C {{ void M() {{ var r = {expression}; }} }}")
            .GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().First();

        SkuggaGenerator.IsCandidateInvocation(invocation).Should().BeTrue();
    }

    [Theory]
    [InlineData("list.Add(1)")]
    [InlineData("System.Console.WriteLine(x)")]
    [InlineData("Local()")]
    public void Predicate_rejects_ordinary_calls(string expression)
    {
        var invocation = CSharpSyntaxTree.ParseText($"class C {{ void M() {{ {expression}; }} }}")
            .GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().First();

        SkuggaGenerator.IsCandidateInvocation(invocation).Should().BeFalse();
    }

    /// <summary>
    /// The output stage must not depend on the compilation. The compilation object is replaced on
    /// every keystroke, so combining with it forces the whole output to re-run even when nothing
    /// the generator cares about changed. It was combined in, and then never used.
    /// </summary>
    [Fact]
    public void Output_stage_does_not_depend_on_the_compilation()
    {
        var references = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
            .Select(a => MetadataReference.CreateFromFile(a.Location))
            .ToList();

        var compilation = CSharpCompilation.Create("Incremental",
            new[] { CSharpSyntaxTree.ParseText(Source) },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var driver = CSharpGeneratorDriver.Create(
            new ISourceGenerator[] { new SkuggaGenerator().AsSourceGenerator() },
            driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));

        var steps = driver.RunGenerators(compilation).GetRunResult().Results.Single().TrackedSteps;

        steps.Keys.Should().NotContain("Compilation",
            "the generator must not combine its pipeline with CompilationProvider");
    }
}
