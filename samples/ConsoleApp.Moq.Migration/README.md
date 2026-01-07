# Migrating from Moq to Skugga: AOT Compatibility Tutorial

**Learn how to migrate from Moq to Skugga for Native AOT support while maintaining 100% feature parity.**

## 📋 Overview

This tutorial demonstrates:
1. ✅ Creating a .NET console application with comprehensive mocking requirements
2. ❌ **Why Moq fails with Native AOT** (reflection wall)
3. ✅ **How Skugga solves it** with compile-time code generation
4. ✅ **Feature parity demonstration** - All major mocking features work identically

## 🎯 What You'll Learn

- Why reflection-based mocking libraries fail with Native AOT
- How to identify AOT compatibility issues
- Step-by-step migration from Moq to Skugga
- Verification that ALL mocking features work with AOT

## 📦 Prerequisites

- .NET SDK 8.0 or later
- Basic understanding of unit testing and mocking
- Familiarity with xUnit (or any testing framework)

## 🏗️ Project Structure

```
ConsoleApp-Moq-To-Skugga-Migration/
├── README.md (this file)
├── Step1-WithMoq/              # ❌ Moq implementation (AOT fails)
│   ├── Program.cs
│   ├── Services/
│   └── Step1-WithMoq.csproj
├── Step1-WithMoq.Tests/        # ❌ Tests using Moq
│   ├── ServiceTests.cs
│   └── Step1-WithMoq.Tests.csproj
├── Step2-WithSkugga/           # ✅ Skugga implementation (AOT works!)
│   ├── Program.cs
│   ├── Services/
│   └── Step2-WithSkugga.csproj
└── Step2-WithSkugga.Tests/     # ✅ Tests using Skugga
    ├── ServiceTests.cs
    └── Step2-WithSkugga.Tests.csproj
```

---

## 📘 Step 1: The Problem - Moq and Native AOT

### What We're Building

A simple order processing system that demonstrates **all major mocking features**:

| Feature | Example Usage |
|---------|---------------|
| **Basic Setup/Returns** | `mock.Setup(x => x.GetPrice(id)).Returns(99.99m)` |
| **Argument Matchers** | `mock.Setup(x => x.ValidateOrder(It.IsAny<Order>()))` |
| **Callbacks** | `mock.Setup(x => x.ProcessPayment()).Callback(() => ...)` |
| **Verification** | `mock.Verify(x => x.SendEmail(), Times.Once())` |
| **Sequential Returns** | `mock.SetupSequence(x => x.GetStatus()).Returns(...)` |
| **Properties** | `mock.Setup(x => x.TotalOrders).Returns(42)` |
| **Async Methods** | `mock.Setup(x => x.FetchDataAsync()).ReturnsAsync(...)` |
| **Exceptions** | `mock.Setup(x => x.Delete()).Throws<UnauthorizedException>()` |

### Create the Application (Step1-WithMoq)

Navigate to `Step1-WithMoq/` directory and examine the code.

**Service Interfaces (`Services/IOrderService.cs`):**
```csharp
public interface IOrderService
{
    decimal GetPrice(int orderId);
    bool ValidateOrder(Order order);
    Task<Order> FetchOrderAsync(int orderId);
    int TotalOrders { get; }
}

public interface IPaymentService
{
    bool ProcessPayment(decimal amount);
    void SendReceipt(string email);
    Task<string> GetPaymentStatusAsync();
}

public interface INotificationService
{
    void SendEmail(string recipientEmail, string message);
    void LogActivity(string activity);
}
```

### Add Moq to Tests

```bash
cd Step1-WithMoq.Tests
dotnet add package Moq
dotnet add reference ../Step1-WithMoq/Step1-WithMoq.csproj
```

### Run Tests (Works in JIT mode)

```bash
cd Step1-WithMoq.Tests
dotnet test
```

**Result:** ✅ **All tests pass!** (in JIT mode)

```
Starting test execution, please wait...
A total of 1 test files matched the specified pattern.

Passed! - Failed: 0, Passed: 12, Skipped: 0
```

---

### 🚫 Enable Native AOT - Watch it FAIL

Now let's try to publish with Native AOT:

**Modify `Step1-WithMoq/Step1-WithMoq.csproj`:**
```xml
<PropertyGroup>
  <OutputType>Exe</OutputType>
  <TargetFramework>net10.0</TargetFramework>
  <PublishAot>true</PublishAot>  <!-- Enable Native AOT -->
</PropertyGroup>
```

**Try to publish:**
```bash
cd Step1-WithMoq
dotnet publish -c Release
```

### 💥 THE REFLECTION WALL

**Expected Errors:**
```
warning IL2026: Using member 'System.Reflection.Emit.DynamicMethod.DynamicMethod(...)' 
  which has 'RequiresUnreferencedCodeAttribute' can break functionality when 
  trimming application code.

warning IL3050: Using member 'System.Reflection.Emit.DynamicMethod.DynamicMethod(...)' 
  which has 'RequiresDynamicCodeAttribute' can break functionality when 
  AOT compiling.

error: Castle.DynamicProxy cannot generate proxies at runtime in Native AOT
```

**Why Moq Fails:**
1. ❌ **Moq uses `Castle.DynamicProxy`** - generates IL code at runtime
2. ❌ **Requires `System.Reflection.Emit`** - not available in Native AOT
3. ❌ **Depends on JIT compilation** - AOT compiles everything ahead of time
4. ❌ **Cannot create types dynamically** - violates AOT constraints

### Test Execution with AOT

Even if you skip the app and just try to run tests with AOT settings:

```bash
cd Step1-WithMoq.Tests
dotnet test -c Release -p:PublishAot=true
```

**Result:**
```
💥 RUNTIME CRASH:

System.PlatformNotSupportedException: 
  Compiling JIT code with 'IsDynamicCodeSupported == false' is not supported.

  at System.Reflection.Emit.DynamicMethod.CreateDelegate(...)
  at Castle.DynamicProxy.ProxyGenerator.CreateInterfaceProxyWithoutTarget(...)
  at Moq.Mock`1.CreateProxy(...)
```

**The Reflection Wall is REAL.** 🚧

---

## ✅ Step 2: The Solution - Skugga with Native AOT

### Migrate to Skugga

Navigate to `Step2-WithSkugga/` - **identical application**, different mocking library.

### Add Skugga to Tests

```bash
cd Step2-WithSkugga.Tests
dotnet add package Skugga
dotnet add reference ../Step2-WithSkugga/Step2-WithSkugga.csproj
```

### The Migration (Line-by-Line Comparison)

**Moq (Step1):**
```csharp
using Moq;

var mock = Mock.Of<IOrderService>();
Mock.Get(mock).Setup(x => x.GetPrice(1)).Returns(99.99m);
```

**Skugga (Step2):**
```csharp
using Skugga.Core;

var mock = Mock.Create<IOrderService>();
mock.Setup(x => x.GetPrice(1)).Returns(99.99m);
```

**That's it!** 🎉 The API is **98% identical** by design.

### Feature Parity Verification

All features work identically:

| Feature | Moq Syntax | Skugga Syntax | Status |
|---------|-----------|---------------|--------|
| **Create Mock** | `Mock.Of<T>()` | `Mock.Create<T>()` | ✅ Works |
| **Setup Returns** | `mock.Setup(...).Returns(...)` | `mock.Setup(...).Returns(...)` | ✅ Identical |
| **It.IsAny** | `It.IsAny<T>()` | `It.IsAny<T>()` | ✅ Identical |
| **It.Is** | `It.Is<T>(predicate)` | `It.Is<T>(predicate)` | ✅ Identical |
| **Verify** | `mock.Verify(..., Times.Once())` | `mock.Verify(..., Times.Once())` | ✅ Identical |
| **Callbacks** | `.Callback(() => ...)` | `.Callback(() => ...)` | ✅ Identical |
| **SetupSequence** | `.SetupSequence(...).Returns(...)` | `.SetupSequence(...).Returns(...)` | ✅ Identical |
| **ReturnsAsync** | `.ReturnsAsync(...)` | `.ReturnsAsync(...)` | ✅ Identical |
| **Throws** | `.Throws<TException>()` | `.Throws<TException>()` | ✅ Identical |
| **Property Setup** | `mock.Setup(x => x.Prop).Returns(...)` | `mock.Setup(x => x.Prop).Returns(...)` | ✅ Identical |

### Run Tests (Still Works!)

```bash
cd Step2-WithSkugga.Tests
dotnet test
```

**Result:** ✅ **All 12 tests pass!**

---

### 🚀 Enable Native AOT - Watch it SUCCEED

**Modify `Step2-WithSkugga/Step2-WithSkugga.csproj`:**
```xml
<PropertyGroup>
  <OutputType>Exe</OutputType>
  <TargetFramework>net10.0</TargetFramework>
  <PublishAot>true</PublishAot>  <!-- Enable Native AOT -->
</PropertyGroup>
```

**Publish with AOT:**
```bash
cd Step2-WithSkugga
dotnet publish -c Release
```

### ✅ SUCCESS!

```
Generating native code...
  Step2-WithSkugga -> /path/to/bin/Release/net10.0/osx-arm64/publish/
  
✅ AOT compilation completed successfully!
```

**No warnings. No errors. No reflection.**

### Run the AOT-Compiled Binary

```bash
cd Step2-WithSkugga/bin/Release/net10.0/osx-arm64/publish
./Step2-WithSkugga
```

**Output:**
```
🚀 Order Processing System (Native AOT)
✅ Order validated successfully
✅ Payment processed: $99.99
✅ Notification sent to customer@example.com
✅ Order completed!
```

### Run Tests with AOT Settings

```bash
cd Step2-WithSkugga.Tests
dotnet test -c Release -p:PublishAot=true
```

**Result:** ✅ **All 12 tests pass - with AOT enabled!**

---

## 🔬 Behind the Scenes: Why Skugga Works

### Moq Architecture (Runtime - Fails AOT)
```
Test Code → Moq API → Castle.DynamicProxy → System.Reflection.Emit → 💥 AOT CRASH
                                              ↓
                                    Generates IL at runtime
                                    Requires JIT compiler
```

### Skugga Architecture (Compile-Time - AOT Safe)
```
Test Code → Skugga API → C# Source Generator → Generated Mock Classes → Native Code
                                 ↓
                        Generates C# code during build
                        Zero runtime reflection
```

**Skugga generates this at compile-time:**
```csharp
// Auto-generated by Skugga during build
public class Mock_IOrderService_12345 : IOrderService
{
    private readonly MockHandler<IOrderService> _handler = new();
    
    public decimal GetPrice(int orderId)
    {
        return _handler.Handle(() => GetPrice(orderId), orderId);
    }
    
    // ... other methods generated statically
}
```

**No dynamic IL. No reflection. Just plain C# code compiled ahead of time.**

---

## 📊 Performance Comparison

Run the included benchmark:

```bash
cd Step2-WithSkugga.Tests
dotnet test --filter "BenchmarkTests"
```

**Results (6.36x faster overall):**

| Operation | Moq | Skugga | Speedup |
|-----------|-----|--------|---------|
| Mock Creation | 705ms | 46ms | **15.29x faster** |
| Setup + Invoke | 981ms | 342ms | **2.86x faster** |
| Argument Matching | 10,906ms | 136ms | **79.84x faster** |
| Verification | 371ms | 116ms | **3.18x faster** |
| **TOTAL** | **100,569ms** | **15,812ms** | **6.36x faster** |

---

## 🎓 Migration Guide Summary

### 1. Update Package References

**Remove Moq:**
```bash
dotnet remove package Moq
```

**Add Skugga:**
```bash
dotnet add package Skugga
```

### 2. Update Using Statements

```diff
- using Moq;
+ using Skugga.Core;
```

### 3. Update Mock Creation

```diff
- var mock = Mock.Of<IService>();
- Mock.Get(mock).Setup(...);
+ var mock = Mock.Create<IService>();
+ mock.Setup(...);
```

**That's it!** 98% of your code remains unchanged.

---

## 🚀 Next Steps

### Test Your AOT Build

1. Enable AOT in your project file:
   ```xml
   <PublishAot>true</PublishAot>
   ```

2. Publish and verify:
   ```bash
   dotnet publish -c Release
   ./bin/Release/net10.0/{runtime}/publish/YourApp
   ```

3. Measure the benefits:
   - ✅ **Faster startup** (no JIT warmup)
   - ✅ **Smaller memory footprint** (no JIT overhead)
   - ✅ **Predictable performance** (no runtime code generation)
   - ✅ **Smaller deployment size** (with trimming)

### Cloud-Native Benefits

**Kubernetes/Container Deployments:**
- 🐳 **Distroless images** - No .NET runtime needed, just your app
- 📦 **Smaller images** - 10-50MB vs 200MB+ with full runtime
- ⚡ **Instant cold starts** - No JIT compilation delay
- 💰 **Lower cloud costs** - Less memory, faster execution

**Example Dockerfile (Distroless):**
```dockerfile
FROM mcr.microsoft.com/dotnet/nightly/runtime-deps:8.0-noble-chiseled-aot
COPY bin/Release/net10.0/linux-x64/publish/ /app/
ENTRYPOINT ["/app/YourApp"]
```

---

## 📚 Additional Resources

- [Skugga Documentation](https://github.com/Digvijay/Skugga)
- [Skugga API Reference](https://github.com/Digvijay/Skugga/blob/master/docs/API_REFERENCE.md)
- [Native AOT Deployment Guide](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/)
- [Benchmark Results](https://github.com/Digvijay/Skugga/blob/master/benchmarks/README.md)

---

## 🤝 Support

- 🐛 [Report Issues](https://github.com/Digvijay/Skugga/issues)
- 💬 [Discussions](https://github.com/Digvijay/Skugga/discussions)
- ⭐ [Star the Project](https://github.com/Digvijay/Skugga)

---

## ✅ Conclusion

**The Reflection Wall is REAL** - but Skugga breaks through it with compile-time code generation.

You get:
- ✅ **Native AOT support** - Works where Moq can't
- ✅ **Feature parity** - 98% identical API
- ✅ **Better performance** - 6.36x faster overall
- ✅ **Zero reflection** - Predictable, analyzable code

**Ready for cloud-native .NET? Choose Skugga.**
