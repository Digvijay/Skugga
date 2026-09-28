using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Skugga.OpenApi.Generator;
using Xunit;

namespace Skugga.OpenApi.Tests.Core
{
    /// <summary>
    /// Compiles the generator's own output instead of relying on the test project
    /// compiling it implicitly.
    /// </summary>
    /// <remarks>
    /// Every other test in this project consumes the generator through the test
    /// project itself, which declares its interfaces inside a namespace and has
    /// ImplicitUsings switched on by the SDK. Both of those masked real defects that
    /// shipped in Skugga 1.5.0: a global-namespace interface produced the literal text
    /// "namespace &lt;global namespace&gt;", and the generated members referenced Task
    /// without any using directive. A Roslyn compilation has no implicit usings, so
    /// driving the generator through one reproduces what an ordinary consumer sees.
    /// </remarks>
    public class GeneratedCodeCompilesTests
    {
        private const string PetSpec = @"{
  ""openapi"": ""3.0.0"",
  ""info"": { ""title"": ""Pet"", ""version"": ""1.0.0"" },
  ""paths"": {
    ""/pets"": {
      ""get"": {
        ""operationId"": ""listPets"",
        ""responses"": {
          ""200"": {
            ""description"": ""ok"",
            ""content"": {
              ""application/json"": {
                ""schema"": { ""$ref"": ""#/components/schemas/Pet"" }
              }
            }
          }
        }
      }
    }
  },
  ""components"": {
    ""schemas"": {
      ""Pet"": {
        ""type"": ""object"",
        ""properties"": {
          ""id"": { ""type"": ""integer"" },
          ""name"": { ""type"": ""string"" }
        }
      }
    }
  }
}";

        [Fact]
        [Trait("Category", "OpenApi Core")]
        public void GlobalNamespaceInterface_ProducesCompilableCode()
        {
            var generated = RunGenerator(@"
using Skugga.Core;

[SkuggaFromOpenApi(""pet.json"")]
public partial interface IPetApi { }
");

            Assert.NotEmpty(generated);

            // Guards the exact string that shipped in 1.5.0. ContainingNamespace is not
            // null for the global namespace - it renders as "<global namespace>" - so the
            // old null-coalescing fallback never ran and this text reached the output.
            foreach (var source in generated)
            {
                Assert.DoesNotContain("<global namespace>", source, StringComparison.Ordinal);
            }
        }

        [Fact]
        [Trait("Category", "OpenApi Core")]
        public void GeneratedCode_ImportsTaskNamespace()
        {
            var generated = RunGenerator(@"
using Skugga.Core;

namespace Consumer
{
    [SkuggaFromOpenApi(""pet.json"")]
    public partial interface IPetApi { }
}
");

            var usingTask = generated.Where(s => s.Contains("Task")).ToList();
            Assert.NotEmpty(usingTask);

            foreach (var source in usingTask)
            {
                Assert.Contains("using System.Threading.Tasks;", source, StringComparison.Ordinal);
            }
        }

        [Theory]
        [Trait("Category", "OpenApi Core")]
        [InlineData("global", @"
using Skugga.Core;

[SkuggaFromOpenApi(""pet.json"")]
public partial interface IPetApi { }
")]
        [InlineData("namespaced", @"
using Skugga.Core;

namespace Consumer
{
    [SkuggaFromOpenApi(""pet.json"")]
    public partial interface IPetApi { }
}
")]
        public void GeneratedCode_CompilesWithoutImplicitUsings(string scenario, string userSource)
        {
            var compilation = CreateCompilation(userSource);
            var updated = RunGenerator(userSource, out _, compilation);

            var errors = updated
                .GetDiagnostics()
                .Where(d => d.Severity == DiagnosticSeverity.Error)
                .Select(d => $"{d.Id} {d.GetMessage()}")
                .ToList();

            Assert.True(
                errors.Count == 0,
                $"[{scenario}] generated code must compile on its own, but produced: {string.Join("; ", errors)}");
        }

        private static List<string> RunGenerator(string userSource)
        {
            RunGenerator(userSource, out var generated, CreateCompilation(userSource));
            return generated;
        }

        private static Compilation RunGenerator(string userSource, out List<string> generatedSources, Compilation compilation)
        {
            var generator = new SkuggaOpenApiGenerator();

            var driver = CSharpGeneratorDriver.Create(
                new[] { generator.AsSourceGenerator() },
                additionalTexts: new AdditionalText[] { new InMemoryAdditionalText("pet.json", PetSpec) });

            driver.RunGeneratorsAndUpdateCompilation(compilation, out var updated, out _);

            generatedSources = updated.SyntaxTrees
                .Where(t => t.FilePath.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase))
                .Select(t => t.GetText().ToString())
                .ToList();

            return updated;
        }

        private static CSharpCompilation CreateCompilation(string userSource)
        {
            // Touch a type from Skugga.Core so the assembly is loaded before the
            // AppDomain is enumerated; otherwise [SkuggaFromOpenApi] is unresolvable
            // and the generator never sees a candidate interface.
            var coreAssembly = typeof(Skugga.Core.SkuggaFromOpenApiAttribute).Assembly;

            var references = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
                .Append(coreAssembly)
                .Distinct()
                .Select(a => MetadataReference.CreateFromFile(a.Location))
                .ToList();

            return CSharpCompilation.Create(
                "OpenApiGeneratedCodeTests",
                new[] { CSharpSyntaxTree.ParseText(userSource) },
                references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        }

        private sealed class InMemoryAdditionalText : AdditionalText
        {
            private readonly SourceText _text;

            public InMemoryAdditionalText(string path, string text)
            {
                Path = path;
                _text = SourceText.From(text, Encoding.UTF8);
            }

            public override string Path { get; }

            public override SourceText GetText(CancellationToken cancellationToken = default) => _text;
        }
    }
}
