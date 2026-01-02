# Skugga Samples

Comprehensive examples demonstrating Skugga's capabilities across different .NET scenarios, from simple console apps to Azure Functions with Native AOT compilation.

## 📚 Available Samples

### [1. BasicConsoleApp](./BasicConsoleApp/) - Getting Started
**Complexity:** ⭐ Beginner  
**Focus:** Core concepts, basic mocking patterns

A simple console application showing the fundamentals of Skugga:
- Creating mocks with `Mock.Create<T>()`
- Setting up behavior with `.Setup()`
- Verifying calls with `.Verify()`
- Using `It.IsAny<T>()` for flexible matching

**Perfect for:** Learning Skugga basics, understanding core concepts

```bash
cd BasicConsoleApp
dotnet run
dotnet test
```

---

### [2. AspNetCoreWebApi](./AspNetCoreWebApi/) - Real-World API
**Complexity:** ⭐⭐ Intermediate  
**Focus:** Controller testing, service layer patterns, dependency injection

Full-featured ASP.NET Core Web API with:
- REST API controllers with multiple dependencies
- Repository and service layer architecture
- Testing controllers, services, and business logic
- Mocking `ILogger` and other framework interfaces

**Perfect for:** Real-world applications, learning API testing patterns

```bash
cd AspNetCoreWebApi
dotnet run  # API at https://localhost:5001/swagger
cd ../AspNetCoreWebApi.Tests
dotnet test
```

---

### [3. MinimalApiAot](./MinimalApiAot/) - Native AOT
**Complexity:** ⭐⭐⭐ Advanced  
**Focus:** Native AOT compilation, performance optimization

Minimal API with **PublishAot=true** demonstrating:
- Native AOT compilation (instant startup, minimal memory)
- JSON source generation for AOT compatibility
- Testing AOT-compiled code with Skugga
- Breaking through the "Reflection Wall"

**Perfect for:** Cloud-native apps, serverless, containerized microservices

```bash
cd MinimalApiAot
dotnet run  # API at http://localhost:5000

# Native AOT compilation
dotnet publish -c Release -r linux-x64
./bin/Release/net9.0/linux-x64/publish/MinimalApiAot
```

---

### [4. AzureFunctions](./AzureFunctions/) - Serverless
**Complexity:** ⭐⭐⭐ Advanced  
**Focus:** Serverless architecture, Azure Functions v4, isolated worker

Azure Functions with isolated worker model:
- HTTP-triggered functions for order processing
- Service layer with payment and notification integrations
- Testing serverless functions with mocked dependencies
- Native AOT ready for optimized cold starts

**Perfect for:** Serverless applications, event-driven architectures, Azure deployments

```bash
cd AzureFunctions
func start  # Or: dotnet run
cd ../AzureFunctions.Tests
dotnet test
```

---

### [5. Skugga.Performance.E2E](./Skugga.Performance.E2E/) - Benchmarks & Production
**Complexity:** ⭐⭐⭐ Advanced  
**Focus:** Performance analysis, production deployments, containerization

Real-world microservice for benchmarking:
- Native AOT vs JIT comparison
- Container image optimization (Alpine, Debian, Chiseled)
- Startup time analysis
- Memory and CPU profiling

**Perfect for:** Performance analysis, understanding Skugga's benefits

See the [dedicated README](./Skugga.Performance.E2E/README.md) for detailed benchmarks.

---

## 🎯 Learning Path

### Beginner → Intermediate → Advanced

1. **Start Here:** [BasicConsoleApp](./BasicConsoleApp/)
   - Learn core Skugga concepts
   - Understand mock setup and verification
   - 15-minute introduction

2. **Build On It:** [AspNetCoreWebApi](./AspNetCoreWebApi/)
   - Apply to real-world scenarios
   - Test controllers and services
   - 30-minute exploration

3. **Go Native:** [MinimalApiAot](./MinimalApiAot/)
   - Enable Native AOT compilation
   - Understand the performance benefits
   - 45-minute deep dive

4. **Go Serverless:** [AzureFunctions](./AzureFunctions/)
   - Deploy to Azure
   - Test serverless architectures
   - 1-hour comprehensive guide

---

## 🔥 Key Concepts Demonstrated

### Mock Creation
```csharp
var mock = Mock.Create<IService>();
```

### Setup Behavior
```csharp
mock.Setup(x => x.GetData(1)).Returns("result");
mock.Setup(x => x.ProcessAsync(It.IsAny<string>())).Returns(Task.CompletedTask);
```

### Verification
```csharp
mock.Verify(x => x.SaveData(It.IsAny<Data>()), Times.Once());
mock.Verify(x => x.DeleteData(1), Times.Never());
```

### Advanced Patterns
```csharp
// Callbacks
mock.Setup(x => x.Process(It.IsAny<int>()))
    .Callback<int>(id => Console.WriteLine($"Processing {id}"))
    .Returns(true);

// Sequential returns
mock.Setup(x => x.GetNext())
    .ReturnsInOrder(1, 2, 3);

// Chaos mode
mock.EnableChaosMode(failureRate: 0.1, seed: 42);
```

---

## 📊 Comparison Matrix

| Sample | AOT | Complexity | Lines of Code | Test Count | Use Case |
|--------|-----|-----------|---------------|------------|----------|
| BasicConsoleApp | ✅ | ⭐ | ~200 | 5 | Learning |
| AspNetCoreWebApi | ✅ | ⭐⭐ | ~800 | 13 | APIs |
| MinimalApiAot | ✅ | ⭐⭐⭐ | ~400 | 7 | Cloud-Native |
| AzureFunctions | ✅ | ⭐⭐⭐ | ~600 | 4 | Serverless |
| Performance.E2E | ✅ | ⭐⭐⭐ | ~1000 | - | Benchmarking |

---

## 🚀 Quick Start

### Run All Samples
```bash
# From the samples/ directory
for dir in BasicConsoleApp AspNetCoreWebApi MinimalApiAot AzureFunctions; do
  echo "Building $dir..."
  cd $dir
  dotnet build
  cd ..
done
```

### Run All Tests
```bash
# Test all sample projects
dotnet test BasicConsoleApp.Tests/
dotnet test AspNetCoreWebApi.Tests/
dotnet test MinimalApiAot.Tests/
dotnet test AzureFunctions.Tests/
```

---

## 💡 Common Patterns

### Testing with FluentAssertions
```csharp
result.Should().NotBeNull();
result.Should().BeOfType<OkObjectResult>();
result.Value.Should().BeEquivalentTo(expected);
```

### Testing Async Methods
```csharp
mock.Setup(x => x.GetDataAsync()).Returns(Task.FromResult(data));
var result = await mock.Object.GetDataAsync();
```

### Testing with Multiple Mocks
```csharp
var repoMock = Mock.Create<IRepository>();
var serviceMock = Mock.Create<IService>();
var controller = new MyController(repoMock.Object, serviceMock.Object);
```

---

## 🎓 What You'll Learn

### From BasicConsoleApp
- ✅ Mock creation and object access
- ✅ Method setup with return values
- ✅ Call verification with Times
- ✅ Argument matchers (It.IsAny)

### From AspNetCoreWebApi
- ✅ Controller testing patterns
- ✅ Service layer isolation
- ✅ HTTP response type assertions
- ✅ Repository pattern mocking

### From MinimalApiAot
- ✅ Native AOT compilation
- ✅ JSON source generation
- ✅ Performance optimization
- ✅ Breaking the "Reflection Wall"

### From AzureFunctions
- ✅ Serverless testing
- ✅ Azure Functions v4 patterns
- ✅ Isolated worker model
- ✅ Event-driven architectures

---

## 📦 Dependencies

All samples use:
- **Skugga.Core** - Core mocking library
- **Skugga.Generator** - Source generator
- **xunit** - Test framework
- **FluentAssertions** - Readable assertions

Additional dependencies per sample:
- **AspNetCoreWebApi:** Swashbuckle.AspNetCore (Swagger)
- **AzureFunctions:** Microsoft.Azure.Functions.Worker

---

## 🔍 Troubleshooting

### Build Issues
```bash
# Clean and rebuild
dotnet clean
dotnet build --no-incremental
```

### Source Generator Not Running
```bash
# Force regeneration
dotnet clean
rm -rf obj/ bin/
dotnet build
```

### Native AOT Issues
```bash
# Check AOT warnings
dotnet publish -c Release -r linux-x64 /p:PublishAot=true
```

---

## 🌟 Best Practices

1. **Start Simple:** Begin with BasicConsoleApp before tackling advanced scenarios
2. **Read the Tests:** Each sample includes comprehensive test examples
3. **Experiment:** Modify the samples to understand behavior
4. **Check READMEs:** Each sample has detailed documentation
5. **Run Benchmarks:** Use Performance.E2E to see real-world impact

---

## ⚠️ Critical: Solution Structure Requirement

**These sample projects MUST be in a separate solution from the main Skugga source code.**

### Why This Matters

MSBuild prioritizes `ProjectReference` over `PackageReference` when projects share a solution. This prevents Skugga's source generator from loading from the NuGet package, causing compile-time mocking to fail.

**✅ Correct Structure (Current):**
```
Skugga/
├── Skugga.slnx                     ← Main Skugga solution
├── src/
│   ├── Skugga.Core/
│   └── Skugga.Generator/
└── samples-separate/
    └── samples/                    ← Separate solution
        ├── Skugga.Samples.slnx    
        └── [Sample Projects]
```

**❌ Incorrect (Will Fail):**
```
Skugga/
├── Skugga.slnx                     ← Same solution
├── src/
│   └── ...
└── samples/                        ← Projects in same solution = failure
```

**If you see runtime errors** about generator not intercepting calls, check:
1. Samples are in separate solution
2. Using `PackageReference` (not `ProjectReference`) to Skugga  
3. NuGet.config points to correct artifacts path

See [Troubleshooting Guide](../../docs/TROUBLESHOOTING.md) for complete diagnostic steps.

---

## 📖 Additional Resources

- **[Main README](../README.md)** - Project overview and features
- **[Troubleshooting Guide](../../docs/TROUBLESHOOTING.md)** - **⭐ Essential for setup issues**
- **[API Reference](../docs/API_REFERENCE.md)** - Complete API documentation
- **[Technical Summary](../docs/TECHNICAL_SUMMARY.md)** - Architecture details
- **[Benchmark Summary](../docs/BENCHMARK_SUMMARY.md)** - Performance analysis

---

## 🤝 Contributing

Found an issue or want to add a sample? See [CONTRIBUTING.md](../CONTRIBUTING.md)

---

## 📄 License

All samples are MIT licensed. See [LICENSE](../LICENSE) for details.

---

**Ready to break through the Reflection Wall? Start with [BasicConsoleApp](./BasicConsoleApp/)!** 🚀
