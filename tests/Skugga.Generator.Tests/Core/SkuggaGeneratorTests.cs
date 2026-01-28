using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Skugga.Generator;

namespace Skugga.Generator.Tests;

/// <summary>
/// Tests for the Skugga source generator (SkuggaGenerator).
/// Verifies that the generator correctly produces mock classes and interceptors at compile-time.
/// </summary>
/// <remarks>
/// <para>
/// Skugga uses Roslyn source generators to analyze code at compile-time and generate mock implementations.
/// This is a key advantage over reflection-based mocking frameworks, enabling Native AOT support and
/// better performance.
/// </para>
/// <para>
/// The generator performs several operations:
/// 1. Detects calls to Mock.Create&lt;T&gt;() in user code
/// 2. Generates concrete mock classes implementing the target interfaces
/// 3. Creates interceptor methods that redirect Mock.Create calls to generated mocks
/// 4. Supports Harness.Create&lt;T&gt;() for automatic dependency injection in tests
/// </para>
/// <para>
/// Generated code includes:
/// - Mock classes with MockHandler integration for setup/verification
/// - Properties and methods matching the interface
/// - Interceptor attributes for compile-time method redirection
/// - TestHarness subclasses with automatic mock creation
/// </para>
/// <para>
/// Test methodology:
/// - Uses CSharpCompilation to simulate compile-time environment
/// - Provides mock Skugga.Core definitions for isolated testing
/// - Verifies generated source contains expected patterns
/// - Checks for proper fully-qualified type names and namespace handling
/// </para>
/// </remarks>
public class SkuggaGeneratorTests
{
    #region Mock Skugga.Core Assembly

    /// <summary>
    /// Mock definitions of core Skugga types for testing the generator in isolation.
    /// This simulates the Skugga.Core assembly without requiring actual project references.
    /// </summary>
    private const string CoreAssembly = @"
        namespace Skugga.Core
        {
            public static class Mock
            {
                public static T Create<T>(MockBehavior behavior = MockBehavior.Loose) => default!;
            }

            public static class Harness
            {
                public static TestHarness<T> Create<T>() where T : class => default!;
            }

            public enum MockBehavior { Loose, Strict }

            public interface IMockSetup
            {
                MockHandler Handler { get; }
            }

            public class MockHandler
            {
                public MockBehavior Behavior { get; set; }
                public object? Invoke(string methodName, object?[] args) => null;
            }

            public abstract class TestHarness<T> where T : class
            {
                protected readonly Dictionary<Type, object> _mocks = new();
                public T SUT { get; protected set; } = default!;
                public TMock Mock<TMock>() where TMock : class => (TMock)_mocks[typeof(TMock)];
            }
        }
        ";

    #endregion

    #region Basic Mock Generation Tests

    /// <summary>
    /// Verifies that the generator creates a mock class for a simple interface.
    /// This is the fundamental test ensuring basic code generation works.
    /// </summary>
    [Fact]
    [Trait("Category", "Generator")]
    public async Task Generator_ShouldGenerateMockClass_ForInterface()
    {
        var source = """
            using Skugga.Core;

            namespace TestNamespace
            {
                public interface IService
                {
                    string GetData();
                }

                public class TestClass
                {
                    public void TestMethod()
                    {
                        var mock = Mock.Create<IService>();
                    }
                }
            }
            """;

        await VerifyGeneratorAsync(source, generatedSources =>
        {
            generatedSources.Should().NotBeEmpty("generator should produce output");

            var mockClass = generatedSources.FirstOrDefault(s => s.Contains("class Skugga_IService_"));
            mockClass.Should().NotBeNull("should generate mock class for IService");
            mockClass.Should().Contain("GetData()", "should implement GetData method");
            mockClass.Should().Contain("MockHandler _handler", "should have MockHandler field");
        });
    }

    /// <summary>
    /// Verifies that the generator creates interceptor methods with InterceptsLocationAttribute.
    /// Interceptors redirect Mock.Create calls to generated mock constructors at compile-time.
    /// </summary>
    [Fact]
    [Trait("Category", "Generator")]
    public async Task Generator_ShouldGenerateInterceptor_ForMockCreate()
    {
        var source = """
            using Skugga.Core;

            namespace TestNamespace
            {
                public interface IService
                {
                    void DoWork();
                }

                public class TestClass
                {
                    public void TestMethod()
                    {
                        var mock = Mock.Create<IService>();
                    }
                }
            }
            """;

        await VerifyGeneratorAsync(source, generatedSources =>
        {
            var interceptor = generatedSources.FirstOrDefault(s => s.Contains("InterceptsLocationAttribute"));
            interceptor.Should().NotBeNull("should generate interceptor");
            interceptor.Should().Contain("public static", "interceptor should be public static");
            interceptor.Should().Contain("Create<T>", "should intercept Create method");
        });
    }

    /// <summary>
    /// Verifies that generated mock classes include all interface methods (void and non-void).
    /// Tests method signature generation with parameters and return types.
    /// </summary>
    [Fact]
    [Trait("Category", "Generator")]
    public async Task Generator_ShouldGenerateMockWithMethods_ForInterface()
    {
        var source = """
            using Skugga.Core;

            namespace TestNamespace
            {
                public interface ICalculator
                {
                    int Add(int a, int b);
                    void Reset();
                }

                public class TestClass
                {
                    public void TestMethod()
                    {
                        var mock = Mock.Create<ICalculator>();
                    }
                }
            }
            """;

        await VerifyGeneratorAsync(source, generatedSources =>
        {
            var mockClass = generatedSources.FirstOrDefault(s => s.Contains("class Skugga_ICalculator_"));
            mockClass.Should().NotBeNull("should generate mock class");
            mockClass.Should().Contain("Add(int a, int b)", "should generate Add method");
            mockClass.Should().Contain("Reset()", "should generate Reset method");
        });
    }

    /// <summary>
    /// Verifies that generated mock classes include properties (get-only and get/set).
    /// Tests property generation with different accessor combinations.
    /// </summary>
    [Fact]
    [Trait("Category", "Generator")]
    public async Task Generator_ShouldGenerateMockWithProperties_ForInterface()
    {
        var source = """
            using Skugga.Core;

            namespace TestNamespace
            {
                public interface IRepository
                {
                    string ConnectionString { get; set; }
                    int Count { get; }
                }

                public class TestClass
                {
                    public void TestMethod()
                    {
                        var mock = Mock.Create<IRepository>();
                    }
                }
            }
            """;

        await VerifyGeneratorAsync(source, generatedSources =>
        {
            var mockClass = generatedSources.FirstOrDefault(s => s.Contains("class Skugga_IRepository_"));
            mockClass.Should().NotBeNull("should generate mock class");
            mockClass.Should().Contain("public string ConnectionString", "should generate ConnectionString property");
            mockClass.Should().Contain("public int Count", "should generate Count property");
        });
    }

    #endregion

    #region Advanced Generation Tests

    /// <summary>
    /// Verifies that the generator handles Mock.Create calls with MockBehavior parameter.
    /// Tests that generated interceptors accept and pass through behavior settings.
    /// </summary>
    [Fact]
    [Trait("Category", "Generator")]
    public async Task Generator_ShouldHandleMockBehaviorParameter()
    {
        var source = """
            using Skugga.Core;

            namespace TestNamespace
            {
                public interface IService
                {
                    void Execute();
                }

                public class TestClass
                {
                    public void TestMethod()
                    {
                        var mock = Mock.Create<IService>(MockBehavior.Strict);
                    }
                }
            }
            """;

        await VerifyGeneratorAsync(source, generatedSources =>
        {
            var interceptor = generatedSources.FirstOrDefault(s => s.Contains("InterceptsLocationAttribute"));
            interceptor.Should().NotBeNull("should generate interceptor");
            interceptor.Should().Contain("Skugga.Core.MockBehavior behavior", "should accept MockBehavior parameter");
        });
    }

    /// <summary>
    /// Verifies that the generator creates TestHarness subclasses for Harness.Create calls.
    /// Harness classes automatically create mocks for constructor dependencies.
    /// </summary>
    [Fact]
    [Trait("Category", "Generator")]
    public async Task Generator_ShouldGenerateHarnessClass_ForHarnessCreate()
    {
        var source = """
            using Skugga.Core;

            namespace TestNamespace
            {
                public interface IRepository
                {
                    string Get(int id);
                }

                public class UserService
                {
                    private readonly IRepository _repo;
                    public UserService(IRepository repo) => _repo = repo;
                }

                public class TestClass
                {
                    public void TestMethod()
                    {
                        var harness = Harness.Create<UserService>();
                    }
                }
            }
            """;

        await VerifyGeneratorAsync(source, generatedSources =>
        {
            var harnessClass = generatedSources.FirstOrDefault(s => s.Contains("class Harness_UserService_"));
            harnessClass.Should().NotBeNull("should generate harness class");
            harnessClass.Should().Contain("TestHarness<", "should inherit from TestHarness");
            harnessClass.Should().Contain("SUT = new", "should initialize SUT");
        });
    }

    /// <summary>
    /// Verifies that the generator produces only one mock class per interface type,
    /// even when multiple Mock.Create calls exist for the same interface.
    /// </summary>
    [Fact]
    [Trait("Category", "Generator")]
    public async Task Generator_ShouldNotGenerateDuplicateMockClasses()
    {
        var source = """
            using Skugga.Core;

            namespace TestNamespace
            {
                public interface IService
                {
                    void Execute();
                }

                public class TestClass
                {
                    public void TestMethod()
                    {
                        var mock1 = Mock.Create<IService>();
                        var mock2 = Mock.Create<IService>();
                    }
                }
            }
            """;

        await VerifyGeneratorAsync(source, generatedSources =>
        {
            var mockClasses = generatedSources.Where(s => s.Contains("class Skugga_IService_")).ToList();
            mockClasses.Should().HaveCount(1, "should generate only one mock class for IService despite multiple Create calls");
        });
    }

    /// <summary>
    /// Verifies that the generator handles multiple different interfaces in the same compilation.
    /// Tests that each interface gets its own mock class.
    /// </summary>
    [Fact]
    [Trait("Category", "Generator")]
    public async Task Generator_ShouldHandleMultipleInterfaces()
    {
        var source = """
            using Skugga.Core;

            namespace TestNamespace
            {
                public interface IServiceA { void MethodA(); }
                public interface IServiceB { void MethodB(); }

                public class TestClass
                {
                    public void TestMethod()
                    {
                        var mockA = Mock.Create<IServiceA>();
                        var mockB = Mock.Create<IServiceB>();
                    }
                }
            }
            """;

        await VerifyGeneratorAsync(source, generatedSources =>
        {
            generatedSources.Should().Contain(s => s.Contains("class Skugga_IServiceA_"), "should generate mock for IServiceA");
            generatedSources.Should().Contain(s => s.Contains("class Skugga_IServiceB_"), "should generate mock for IServiceB");
        });
    }

    /// <summary>
    /// Verifies that the generator correctly handles generic return types like Task&lt;T&gt;.
    /// Tests async method signature generation.
    /// </summary>
    [Fact]
    [Trait("Category", "Generator")]
    public async Task Generator_ShouldHandleGenericReturnTypes()
    {
        var source = """
            using Skugga.Core;
            using System.Threading.Tasks;

            namespace TestNamespace
            {
                public interface IAsyncService
                {
                    Task<string> GetAsync(int id);
                }

                public class TestClass
                {
                    public void TestMethod()
                    {
                        var mock = Mock.Create<IAsyncService>();
                    }
                }
            }
            """;

        await VerifyGeneratorAsync(source, generatedSources =>
        {
            var mockClass = generatedSources.FirstOrDefault(s => s.Contains("class Skugga_IAsyncService_"));
            mockClass.Should().NotBeNull("should generate mock class");
            mockClass.Should().Contain("Task<string> GetAsync", "should handle generic return types");
        });
    }

    /// <summary>
    /// Verifies that generated code uses fully qualified type names to avoid namespace conflicts.
    /// Tests that both Skugga.Core types and user interface types are properly qualified.
    /// </summary>
    [Fact]
    [Trait("Category", "Generator")]
    public async Task Generator_ShouldUseFullyQualifiedNames()
    {
        var source = """
            using Skugga.Core;

            namespace TestNamespace
            {
                public interface IService
                {
                    void Execute();
                }

                public class TestClass
                {
                    public void TestMethod()
                    {
                        var mock = Mock.Create<IService>();
                    }
                }
            }
            """;

        await VerifyGeneratorAsync(source, generatedSources =>
        {
            var mockClass = generatedSources.FirstOrDefault(s => s.Contains("class Skugga_IService_"));
            mockClass.Should().NotBeNull("should generate mock class");
            mockClass.Should().Contain("Skugga.Core.MockBehavior", "should use fully qualified name for MockBehavior");
            mockClass.Should().Contain("global::TestNamespace.IService", "should use fully qualified name for interface");
        });
    }

    #endregion

    #region Test Infrastructure

    /// <summary>
    /// Helper method to verify source generator output.
    /// Creates a test compilation, runs the generator, and validates generated sources.
    /// </summary>
    /// <param name="source">User source code to compile</param>
    /// <param name="assertions">Callback to assert on generated source strings</param>
    /// <remarks>
    /// This method simulates the compile-time environment by:
    /// 1. Parsing user source code and mock Skugga.Core assembly
    /// 2. Creating a CSharpCompilation with all necessary references
    /// 3. Running the SkuggaGenerator through CSharpGeneratorDriver
    /// 4. Extracting generated source trees and passing them to assertions
    /// 5. Verifying no compilation errors were produced
    /// </remarks>
    private static async Task VerifyGeneratorAsync(string source, Action<List<string>> assertions)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var references = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
            .Select(a => MetadataReference.CreateFromFile(a.Location))
            .ToList();

        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            new[] { syntaxTree, CSharpSyntaxTree.ParseText(CoreAssembly) },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var generator = new SkuggaGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);
        driver = (CSharpGeneratorDriver)driver.RunGenerators(compilation);

        var result = driver.GetRunResult();

        result.Diagnostics.Should().NotContain(d => d.Severity == DiagnosticSeverity.Error,
            "generator should not produce errors");

        var generatedSources = result.GeneratedTrees
            .Select(tree => tree.GetText().ToString())
            .ToList();

        assertions(generatedSources);

        await Task.CompletedTask;
    }

    #endregion
}
