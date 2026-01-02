using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Skugga.Generator;

namespace Skugga.Generator.Tests;

public class SkuggaGeneratorTests
{
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

    [Fact]
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

    [Fact]
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
            interceptor.Should().Contain("IService Create", "should intercept Create method");
        });
    }

    [Fact]
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

    [Fact]
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

    [Fact]
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

    [Fact]
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

    [Fact]
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

    [Fact]
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

    [Fact]
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

    [Fact]
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
}
