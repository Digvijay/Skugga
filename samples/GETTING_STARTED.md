# Getting Started with Skugga Samples

This guide helps you run the Skugga samples in two scenarios:

---

## Scenario 1: Cloned Repository (You're Here Now)

**You've cloned the Skugga repository and want to explore the samples.**

### Quick Start

```bash
# From repository root
cd samples/ChaosEngineeringDemo
dotnet test --verbosity normal
```

### How It Works

The sample projects use `<ProjectReference>` to the source code in `/src`:
```xml
<ProjectReference Include="../../src/Skugga.Core/Skugga.Core.csproj" />
<ProjectReference Include="../../src/Skugga.Generator/Skugga.Generator.csproj" 
                  OutputItemType="Analyzer" 
                  ReferenceOutputAssembly="false" />
```

**Benefits:**
- ✅ See the latest source code
- ✅ Debug into Skugga internals
- ✅ Experiment with changes

**Commands:**
```bash
# Build all samples
dotnet build samples/

# Run specific sample tests
dotnet test samples/ChaosEngineeringDemo
dotnet test samples/AllocationTestingDemo
dotnet test samples/DoppelgangerDemo

# Run with detailed output
dotnet test samples/ChaosEngineeringDemo --logger "console;verbosity=detailed"
```

---

## Scenario 2: Your Own Project (NuGet Installation)

**You want to use Skugga in your own .NET project.**

### Quick Start

```bash
# Create your test project
dotnet new xunit -n MyProject.Tests
cd MyProject.Tests

# Install Skugga from NuGet
dotnet add package Skugga
dotnet add package FluentAssertions  # Optional but recommended

# Add interceptor support to .csproj
```

### Project Configuration

Edit your `.csproj` file:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <IsTestProject>true</IsTestProject>
    
    <!-- Required: Enable C# 12 Interceptors -->
    <InterceptorsPreviewNamespaces>$(InterceptorsPreviewNamespaces);Skugga.Generated</InterceptorsPreviewNamespaces>
  </PropertyGroup>

  <ItemGroup>
    <!-- Skugga from NuGet -->
    <PackageReference Include="Skugga" Version="1.2.0" />
    
    <!-- Standard test packages -->
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.11.0" />
    <PackageReference Include="xunit" Version="2.9.0" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2" />
    <PackageReference Include="FluentAssertions" Version="6.12.0" />
  </ItemGroup>

</Project>
```

### Example Test File

Create `MyFirstSkuggaTest.cs`:

```csharp
using Skugga.Core;
using Xunit;
using FluentAssertions;

public interface IPaymentService
{
    decimal ProcessPayment(string orderId, decimal amount);
}

public class PaymentServiceTests
{
    [Fact]
    public void ProcessPayment_WithValidOrder_ReturnsAmount()
    {
        // Arrange
        var mock = Mock.Create<IPaymentService>();
        mock.Setup(x => x.ProcessPayment("ORDER-123", 99.99m))
            .Returns(99.99m);

        // Act
        var result = mock.ProcessPayment("ORDER-123", 99.99m);

        // Assert
        result.Should().Be(99.99m);
        mock.Verify(x => x.ProcessPayment("ORDER-123", 99.99m), Times.Once());
    }
}
```

### Run Your Test

```bash
dotnet test
```

**Expected output:**
```
Test run for MyProject.Tests.dll (.NETCoreApp,Version=v8.0)
Test Run Successful.
Total tests: 1
     Passed: 1
```

---

## Sample Code Usage Guide

### Copy Code from Samples

You can copy code directly from the sample files:

**Chaos Engineering:**
- Copy from: `/samples/ChaosEngineeringDemo/ChaosDemo.cs`
- Demonstrates: Failure injection, retry testing, timeout simulation

**Allocation Testing:**
- Copy from: `/samples/AllocationTestingDemo/AllocationDemo.cs`
- Demonstrates: Zero-allocation assertions, performance testing

**Doppelgänger (OpenAPI):**
- Copy from: `/samples/DoppelgangerDemo/DoppelgangerSimulatedExample.cs`
- Demonstrates: Interface generation from OpenAPI specs

**AutoScribe:**
- Copy from: `/samples/AutoScribeDemo/tests/OrderService.Tests/`
- Demonstrates: Test code generation from real executions

### Adapting Sample Code

When copying sample code to your project:

1. **Update namespaces** to match your project
2. **Keep the `using`** statements at the top
3. **Ensure InterceptorsPreviewNamespaces** is in your `.csproj`
4. **Run `dotnet build`** to trigger source generators

---

## Troubleshooting

### "The name 'Mock' does not exist"

**Cause:** Missing `using Skugga.Core;`

**Fix:**
```csharp
using Skugga.Core;  // Add this
```

### "Setup does not exist on type"

**Cause:** Interceptors not enabled or source generator not running

**Fix:** Ensure `.csproj` has:
```xml
<InterceptorsPreviewNamespaces>$(InterceptorsPreviewNamespaces);Skugga.Generated</InterceptorsPreviewNamespaces>
```

Then rebuild:
```bash
dotnet clean
dotnet build
```

### Generator Output Not Found

**Cause:** Source generators run during build, not during editing

**Fix:** Build the project to trigger code generation:
```bash
dotnet build
```

You can see generated files in `obj/` folder:
```bash
ls obj/Debug/net8.0/generated/Skugga.Generator/
```

---

## Differences: Repository vs NuGet

| Aspect | Repository (ProjectReference) | Your Project (NuGet) |
|--------|------------------------------|----------------------|
| **Installation** | Already set up | `dotnet add package Skugga` |
| **Updates** | `git pull` | `dotnet add package Skugga --version X.Y.Z` |
| **Source Code** | Visible in `/src` | Compiled in NuGet package |
| **Debugging** | Can debug Skugga internals | Use released version |
| **Configuration** | Pre-configured | Add `InterceptorsPreviewNamespaces` |

---

## Next Steps

### If You're Exploring (Repository)
1. ✅ Run the existing samples (you're doing this)
2. Try modifying tests to understand behavior
3. Check out `/docs` for detailed guides
4. Look at source code in `/src` to understand implementation

### If You're Integrating (Your Project)
1. ✅ Install from NuGet: `dotnet add package Skugga`
2. Configure `.csproj` with `InterceptorsPreviewNamespaces`
3. Copy sample code that matches your needs
4. Write your first test and run `dotnet test`

---

## Resources

- **Main Documentation:** [/README.md](../../README.md)
- **API Reference:** [/docs/API_REFERENCE.md](../../docs/API_REFERENCE.md)
- **Chaos Engineering Guide:** [Demo README](../ChaosEngineeringDemo/README.md)
- **Allocation Testing Guide:** [Demo README](../AllocationTestingDemo/README.md)
- **Benchmarks:** [/benchmarks/README.md](../../benchmarks/README.md)

---

**Built by [Digvijay Chauhan](https://github.com/Digvijay)** • Open Source • MIT License
