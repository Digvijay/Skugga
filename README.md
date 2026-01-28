# Skugga

[![Skugga CI](https://github.com/Digvijay/Skugga/actions/workflows/ci.yml/badge.svg)](https://github.com/Digvijay/Skugga/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/Skugga.svg)](https://www.nuget.org/packages/Skugga/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![Tests](https://img.shields.io/badge/tests-1018%20passing-brightgreen)](https://github.com/Digvijay/Skugga)
[![Docs](https://img.shields.io/badge/docs-comprehensive-blue)](docs/)

![Skugga Banner](docs/images/skugga_banner_small.png)

> **"Mocking at the Speed of Compilation."**

**Skugga** (Swedish for *Shadow*) is a mocking library engineered specifically for **Native AOT** and Cloud-Native .NET.

**[📚 Complete Documentation →](docs/)** | **[🚀 Quick Start](#installation)** | **[🎯 Example Code](samples/)**

---

Legacy tools like Moq rely on runtime reflection, which is slow, memory-intensive, and incompatible with Native AOT. Skugga takes a different approach: it moves the mocking logic to **Compile-Time**. The result is a library that is 100% AOT-compatible, uses zero reflection, and enables "Distroless" container deployments.

---

## The "Reflection Wall"

As organizations adopt **Native AOT** to reduce cloud costs, they hit a barrier: the **"Reflection Wall"**.

Legacy mocking tools depend on the JIT (Just-In-Time) compiler to generate proxy objects on the fly. Since Native AOT strips away the JIT, these tools crash instantly. Teams are forced to choose between **performance** (AOT) and **quality** (Testability).

**Skugga eliminates this trade-off.** By generating mock implementations during the build process, it treats test doubles as standard, static code.

```mermaid
graph TB
    subgraph "Legacy (Runtime Approach)"
        A1[Mock.Of<T>] -->|Requires| B1(JIT Compilation)
        B1 -->|Uses| C1(System.Reflection.Emit)
        C1 --x|CRASH| D1[🚫 The Reflection Wall]
    end

    subgraph "Skugga (Compile-Time Approach)"
        A2[Mock.Create<T>] -->|Bypasses| B2(Source Generator)
        B2 -->|Generates| C2[Static Shadow Class]
        C2 -->|Compiles to| D2(Native Machine Code)
        D2 -->|Result| E2(✅ Zero Overhead)
    end

    style D1 fill:#b30000,stroke:#333,color:#fff
    style E2 fill:#006600,stroke:#333,color:#fff
````

-----
## 🔥 Key Features

> **Industry-First Features:** Skugga is the **only .NET mocking library** offering built-in [Chaos Engineering](#chaos-engineering-🔥) and [Zero-Allocation Testing](#zero-allocation-testing-⚡). While resilience libraries like [Polly](https://github.com/App-vNext/Polly) + [Simmy](https://github.com/Polly-Contrib/Simmy) provide chaos testing for production code, Skugga uniquely integrates chaos directly into your mocks for test-time resilience validation.

### 1. Doppelgänger (OpenAPI Mock Generation)

> **"Your tests should fail when APIs change, not your production."**

**The Problem: "Contract Drift"**

You mock `IPaymentGateway` manually in your tests. Meanwhile, the platform team updates the actual Payment API OpenAPI definition. **Your tests pass** (because the mock is outdated), **but production crashes**.

This is contract drift - your mocks lie to you.

**The Skugga Solution: Build-Time Contract Validation**

Doppelgänger generates mocks from OpenAPI specs at compile time. When the API changes:
- ❌ **Manual Mocks**: Tests pass ✓ → Production crashes 💥
- ✅ **Doppelgänger**: Build fails ❌ → Fix before deploy ✅

**[👉 Demo and Example Code](samples/DoppelgangerDemo)** - Shows contract drift detection with real examples

**Never mock blindly. Mock against the spec.**

```csharp
// "God Mode" Attribute - generates interface + mock from spec
[SkuggaFromOpenApi("https://api.stripe.com/v1/swagger.json")]
public partial interface IStripeClient { }

// In your test:
var mock = Mock.Create<IStripeClient>();
var invoice = mock.GetInvoice("inv_123"); 
// Returns realistic Invoice with dummy data from spec examples

// When Stripe updates their API:
// ❌ Old way: Tests pass, production crashes
// ✅ Skugga: Build fails with clear error, fix before deploy
```

#### What Makes Doppelgänger Unique?

**vs OpenAPI Generator**: Generates production clients, not test mocks  
**vs NSwag**: Generates clients + Swagger UI, not test mocks  
**vs Manual Mocks (Moq)**: No OpenAPI integration, contracts drift  
**Doppelgänger**: Only tool for test mocks with build-time contract validation

**ROI**: Save $23k-33k per year preventing contract drift incidents ([see calculation](samples/DoppelgangerDemo#demo-2-feature-comparison))

#### Quick Start

```csharp
// One attribute - generates interface + mock from OpenAPI spec
[SkuggaFromOpenApi("petstore.json")]
public partial interface IPetStoreApi { }

// Use in tests
var mock = Mock.Create<IPetStoreApi>();
var pet = mock.GetPet("123"); // Returns realistic data from spec
```

**When the API changes, your build fails with clear errors:**
```
error CS0117: 'IPetStoreApi' does not contain definition for 'GetPet'
error CS0029: Cannot convert type 'decimal' to 'int'
```

#### Core Features

**✨ Automatic Interface Generation** - No manual coding required  
**🔄 Async/Sync Configuration** - Control method signatures  
**🎯 Realistic Test Data** - Uses examples from OpenAPI spec  
**🔐 Auth Mocking** - OAuth2/JWT token generation built-in  
**🗄️ Stateful Behavior** - In-memory CRUD for integration tests  
**✅ Schema Validation** - Runtime validation against OpenAPI schemas  
**⚡ Native AOT Compatible** - 100% compile-time generation

**[📖 Read the full Doppelgänger guide →](docs/DOPPELGANGER.md)** | **[🎯 Demo and example code →](samples/DoppelgangerDemo)**

---

**📋 Response Headers Support** - Access headers alongside response bodies
```csharp
[SkuggaFromOpenApi("api-with-headers.json")]
public partial interface IApiWithHeaders { }

var mock = Mock.Create<IApiWithHeaders>();
var response = mock.GetUser(123); // Returns ApiResponse<User>
var user = response.Body;          // Access the user data
var rateLimit = response.Headers["X-RateLimit-Limit"]; // Access headers
```

**🎯 Example Set Selection** - Choose specific test scenarios
```csharp
// Use "success" example set for happy path testing
[SkuggaFromOpenApi("users.json", UseExampleSet = "success")]
public partial interface IUserApiSuccess { }

// Use "error" example set for error handling tests
[SkuggaFromOpenApi("users.json", UseExampleSet = "error")]
public partial interface IUserApiError { }
```

**🔐 Authentication Mocking** - Test security scenarios
```csharp
[SkuggaFromOpenApi("secure-api.json")]
public partial interface ISecureApi { }

var mock = new ISecureApiMock();
mock.ConfigureSecurity(
    tokenExpired: true,       // Simulate expired token
    tokenInvalid: false,      // Valid token format
    credentialsRevoked: false // Active credentials
);
// Operations will return 401 Unauthorized when called
```

**📊 Stateful Mocking** - In-memory entity tracking for CRUD testing
```csharp
[SkuggaFromOpenApi("users.json")]
public partial interface IUserApi { }

var mock = new IUserApiMock();
var user = mock.CreateUser(new User { Name = "Alice" }); // Stored in-memory
var retrieved = mock.GetUser(user.Id);                   // Retrieves from store
Assert.Equal("Alice", retrieved.Name);
```

**✅ Contract Validation** - Runtime schema validation
```csharp
[SkuggaFromOpenApi("products.json", ValidateSchemas = true)]
public partial interface IValidatedApi { }

var mock = new IValidatedApiMock();
// Mock validates all responses against OpenAPI schemas
// Throws ContractViolationException if schema doesn't match
```

**🌐 URL & Local File Support** - Flexible spec sources
```csharp
// Remote URL (cached locally)
[SkuggaFromOpenApi("https://api.example.com/openapi.json")]
public partial interface IRemoteApi { }

// Local file path
[SkuggaFromOpenApi("../specs/api.json")]
public partial interface ILocalApi { }
```

**🔍 OpenAPI Quality Linting** - Enforce best practices at build time
```csharp
// Customize linting rules
[SkuggaFromOpenApi("api.json", LintingRules = "operation-tags:error,info-license:off")]
public partial interface IMyApi { }
// Build fails if operations missing tags, ignores missing license
```

#### Benefits

- ✅ Zero manual interface maintenance
- ✅ Realistic defaults from OpenAPI examples
- ✅ Compile-time contract validation
- ✅ Works with URLs or local paths
- ✅ Smart caching with offline support
- ✅ Configurable async/sync generation
- ✅ Response headers automatically populated
- ✅ Authentication & security testing
- ✅ Stateful CRUD operations
- ✅ Runtime schema validation
- ✅ OpenAPI quality linting
- ✅ 100% Native AOT compatible

**[📖 Read the full Doppelgänger guide →](docs/DOPPELGANGER.md)**  
**[🎓 Step-by-step tutorial with examples →](docs/API_REFERENCE.md#doppelgänger-openapi-mock-generation)**

### 2. Auto-Scribe (Self-Writing Tests) ✍️

Stop manually writing mock setup code. AutoScribe **records real interactions** and generates the test code for you—turning 15 minutes of tedious mock setup into 30 seconds.

**[▶️ Demo and Example Code](samples/AutoScribeDemo)** - Complex 9-dependency controller example with side-by-side comparison.

```C#
// 1. Wrap your real service with AutoScribe
var recorder = AutoScribe.Capture<IOrderRepository>(new RealOrderRepository());

// 2. Exercise your code manually or run your app
var order = recorder.GetOrder(12345);
recorder.UpdateStatus(12345, "Shipped");

// 3. AutoScribe generates the mock setup code:
// [AutoScribe] mock.Setup(x => x.GetOrder(12345)).Returns(new Order { Id = 12345, Status = "Pending" });
// [AutoScribe] mock.Setup(x => x.UpdateStatus(12345, "Shipped")).Returns(true);

// 4. Copy/paste into your test - done!
```

**Real Impact:**
- **Manual approach**: 15 minutes to setup 9 mock dependencies (50+ lines)
- **AutoScribe approach**: 30 seconds to generate the same setup
- **Accuracy**: Captures real return values, not guesses

**Key Features:**
- Records method calls and return values
- Generates copy/paste ready mock.Setup() code
- Handles complex objects and collections
- Works with async methods
- Perfect for testing with real database/API interactions

**[📖 Read the full AutoScribe guide →](docs/AUTOSCRIBE.md)** | **[🎯 Demo and example code →](samples/AutoScribeDemo)**

---

### 3. Chaos Engineering 🔥

> **Industry First:** Skugga is the **only .NET mocking library** with built-in chaos engineering for testing resilience patterns directly in your mocks. While [Polly](https://github.com/App-vNext/Polly) + [Simmy](https://github.com/Polly-Contrib/Simmy) provide chaos for production code, Skugga brings chaos to test time.

Test how your application handles failure. Inject random faults (latency, exceptions, timeouts) into mocks to **prove** your retry logic works.

**[▶️ Demo and Example Code](samples/ChaosEngineeringDemo)** - 4 scenarios showing resilience testing with retry policies and circuit breakers.

```C#
mock.Chaos(policy => {
    policy.FailureRate = 0.3; // 30% of calls fail
    policy.PossibleExceptions = new[] { 
        new TimeoutException(),
        new HttpRequestException("503")
    };
    policy.Seed = 42; // Reproducible chaos
});

// Test retry logic survives!
for (int i = 0; i < 100; i++) {
    await RetryPolicy.ExecuteAsync(() => mock.CallAsync());
}

// Verify chaos was injected
var stats = mock.GetChaosStatistics();
Console.WriteLine($"Chaos triggered {stats.ChaosTriggeredCount} times");
```

**Key Features:**
- Random failure injection with configurable rates
- Delay simulation for timeout testing  
- Reproducible chaos with seeds
- Detailed statistics tracking
- Works with async methods

**[📖 Read the full Chaos Engineering guide →](docs/CHAOS_ENGINEERING.md)** | **[🎯 Demo and example code →](samples/ChaosEngineeringDemo)**

---

### 4. Zero-Allocation Testing ⚡

> **Industry First:** Skugga is the **only .NET mocking library** providing allocation assertions to **prove** your hot paths are truly zero-allocation. No other mocking framework (Moq, NSubstitute, FakeItEasy) offers this capability.

Ensure your "hot paths" remain allocation-free with precise GC-level measurements. Catch performance regressions before they hit production.

**[▶️ Demo and Example Code](samples/AllocationTestingDemo)** - 6 scenarios showing before/after comparisons (50MB → 0 bytes).

```C#
// Enforce zero allocations
AssertAllocations.Zero(() => {
    cache.Lookup(key); // Must not allocate!
});

// Set allocation budgets
AssertAllocations.AtMost(() => {
    ProcessRequest(data);
}, maxBytes: 1024); // Fail if > 1KB

// Measure and compare
var report = AssertAllocations.Measure(() => {
    for (int i = 0; i < 1000; i++) {
        GetCacheKey(i); // String concat allocates!
    }
}, "String concat (1000x)");

Console.WriteLine($"Allocated: {report.BytesAllocated:N0} bytes");
// Output: Allocated: 50,000 bytes
```

**Key Features:**
- Precise GC-level allocation tracking
- Zero-allocation enforcement for hot paths
- Allocation budgets for controlled memory use
- Before/after comparison reports
- Catch regressions in CI/CD

**[📖 Read the full Allocation Testing guide →](docs/ALLOCATION_TESTING.md)** | **[🎯 Demo and example code →](samples/AllocationTestingDemo)**

---

### 5. Strict Mocks (Verify All) 🔒
Ensure no interaction goes unnoticed. By enabling "Strict Mode", Skugga will throw an exception if any method is called that wasn't explicitly setup.

```C#
// Strict: Throws if ANY un-setup member is accessed
var mock = Mock.Create<IEmailService>(MockBehavior.Strict); 

// Loose (Default): Returns null/default for un-setup members
var mock = Mock.Create<IEmailService>();
```

### 6. Argument Matchers (Flexible Matching) 🎯
Match method arguments with flexible predicates, value sets, null checks, and regex patterns.

```C#

// Match with custom predicate
mock.Setup(x => x.Process(It.Is<int>(n => n > 0))).Returns("positive");
mock.Process(5);   // Returns "positive"
mock.Process(-1);  // Returns null (no match)

// Match values in a set
mock.Setup(x => x.Handle(It.IsIn("red", "green", "blue"))).Returns("color");
mock.Handle("red");     // Returns "color"
mock.Handle("yellow");  // Returns null (no match)

// Match only non-null values
mock.Setup(x => x.ValidateObject(It.IsNotNull<object>())).Returns(true);
mock.ValidateObject(new object()); // Returns true
mock.ValidateObject(null);         // Returns false (no match)

// Match with regex patterns
mock.Setup(x => x.ValidateEmail(It.IsRegex(@"^[\w\.-]+@[\w\.-]+\.\w+$"))).Returns(true);
mock.ValidateEmail("test@example.com"); // Returns true
mock.ValidateEmail("invalid");          // Returns false (no match)

// Combine matchers
mock.Setup(x => x.ProcessTwo(It.Is<int>(n => n > 0), It.IsNotNull<string>())).Returns("valid");

// Use in Verify
mock.Verify(x => x.Process(It.Is<int>(n => n > 10)), Times.AtLeast(2));

```
6. Protected Members (Abstract Class Mocking) 🔐
Mock protected methods and properties on abstract classes - essential for testing inheritance hierarchies and template method patterns.

```C#
// Abstract class with protected members
public abstract class AbstractService
{
    public string Execute(string input)
    {
        // Public method calls protected method
        return ProcessCore(input);
    }
    
    protected abstract string ProcessCore(string input);
    protected abstract int MaxRetries { get; }
}

// Mock the abstract class
var mock = Mock.Create<AbstractService>();

// Setup protected method by name
mock.Protected()
    .Setup<string>("ProcessCore", It.IsAny<string>())
    .Returns("mocked result");

// Setup protected property
mock.Protected()
    .SetupGet<int>("MaxRetries")
    .Returns(3);

// Test the public API which uses protected members
var result = mock.Execute("test"); // Returns "mocked result"

// Protected callbacks for side effects
mock.Protected()
    .Setup("ProcessCore", It.IsAny<string>())
    .Callback<string>(input => Console.WriteLine($"Processing: {input}"));

// Works with Verify too
mock.Protected().Verify("ProcessCore", Times.Once(), It.Is<string>(s => s.Length > 0));

```
7. Setup Sequence (State Simulation) 🔄
Configure methods to return different values on consecutive calls - perfect for testing retry logic, pagination, and stateful scenarios.

```C#

// Return different values on each call
mock.SetupSequence(x => x.GetNext())
    .Returns(1)
    .Returns(2)
    .Returns(3);
    
mock.GetNext(); // Returns 1
mock.GetNext(); // Returns 2
mock.GetNext(); // Returns 3
mock.GetNext(); // Returns 3 (repeats last value)

// Mix returns and exceptions for retry testing
mock.SetupSequence(x => x.FetchData())
    .Throws(new TimeoutException())
    .Throws(new TimeoutException())
    .Returns("success");
    
// First two calls throw, third succeeds
try { mock.FetchData(); } catch { /* retry */ }
try { mock.FetchData(); } catch { /* retry */ }
var data = mock.FetchData(); // "success"

// Works with properties too
mock.SetupSequence(x => x.Counter)
    .Returns(0)
    .Returns(1)
    .Returns(2);

```
-----

## ⚡ Benchmarks

Skugga isn't just AOT-compatible; it is significantly faster and lighter than reflection-based alternatives.

### Quick Comparison: Skugga vs. Alternatives

**Comprehensive benchmarks across 12 scenarios covering all major features** (50,000 iterations each):

| Framework     | Speed vs Skugga | Notes                              |
|---------------|-----------------|-----------------------------------|
| **Skugga**    | **Baseline**    | Compile-time, zero reflection      |
| Moq           | 2.6-80x slower  | ⚠️ 80x slower on argument matching  |
| NSubstitute   | 3.5x slower     | Consistent but reflection-heavy    |
| FakeItEasy    | 3.9x slower     | Similar overhead across scenarios  |

> **Environment:** Intel Core i7-4980HQ @ 2.80GHz, 16GB RAM, macOS 15.7, .NET 10.0.1 | [Full Benchmark Report →](docs/BENCHMARK_COMPARISON.md)

### Critical Performance Findings

**Overall Performance (12-scenario comprehensive test):**
- **Skugga is 6.36x faster than Moq overall**
- Argument Matching: **79.84x faster** ⚡
- Void Method Setup: **59.26x faster**
- Callback Execution: **53.34x faster**
- Simple Mock Creation: **15.29x faster**

**4-Framework Common Scenarios:**
- Moq: **2.58x slower** than Skugga
- NSubstitute: **3.49x slower** than Skugga
- FakeItEasy: **3.88x slower** than Skugga

**Real-World Impact:** For a test suite with 10,000 tests using argument matchers, Skugga completes in **2.7 seconds** vs. Moq's **218 seconds** - that's **215 seconds saved per test run**! ⚡

> **Benchmark Environment:** Intel Core i7-4980HQ @ 2.80GHz, 16GB RAM, macOS 15.7, .NET 10.0.1  
> **Latest Results:** See `/benchmarks/MoqVsSkugga.md` and `/benchmarks/FourFramework.md`  
> **Methodology:** Manual timing with 50,000 iterations per scenario (BenchmarkDotNet incompatible with source generators)

### Why is Skugga Faster?

Legacy libraries like Moq, NSubstitute, and FakeItEasy use `System.Reflection.Emit` to generate proxy classes at **runtime**. This incurs heavy CPU penalties and forces the JIT compiler to work overtime.

**Skugga** does all the heavy lifting at **compile-time**. By the time your application runs, the mock is just a standard C# class. This results in:
* **Zero JIT Penalties:** The code is already compiled to native machine code.
* **Zero Reflection:** No expensive type inspection, `Expression.Lambda().Compile()`, or `MethodInfo` lookups at runtime.
* **Zero Dynamic Allocation:** No generating assemblies on the fly via Castle.DynamicProxy or similar.
* **Optimized Dispatch:** Simple dictionary lookups instead of reflection-based invocation.

> **Reproducing Results:** Run `dotnet run --project src/Skugga.Benchmarks/Skugga.Benchmarks.csproj -c Release` to generate fresh benchmark data. Results are saved to `/benchmarks/MoqVsSkugga.md` and `/benchmarks/FourFramework.md`.

-----
## Proven Performance: Solves the .NET "Cold Start" Problem

Skugga isn't just AOT-compatible; it's a key enabler for high-performance, cloud-native .NET applications. Our benchmarks, conducted in a real-world microservice pilot, prove that Skugga's compile-time architecture delivers massive efficiency gains.

### 1. 7x Faster Cold Starts

In serverless environments like AWS Lambda and Azure Functions, "cold start" times are critical. Skugga, when combined with Native AOT, makes cold starts a thing of the past.

| Metric        | Standard .NET (JIT) | Skugga (Native AOT) | Impact                  |
| :------------ | :------------------ | :------------------ | :---------------------- |
| **Startup Time** | 476 ms              | **72 ms**             | **6.6x Faster Startup** ⚡ |

This means your serverless functions can respond to requests almost instantly, eliminating the latency that plagues traditional .NET serverless applications.

### 2. Alpine vs. Debian: Optimizing AOT Deployments

Choosing the right base image for your Native AOT application can further enhance performance. Our benchmarks show a significant difference between Alpine and Debian.

| Metric        | Native AOT (Alpine) | Native AOT (Debian) | Impact                               |
| :------------ | :------------------ | :------------------ | :----------------------------------- |
| **Startup Time** | **66 ms**           | 835 ms              | **12.6x Faster on Alpine** 🚀        |

Alpine Linux, with its minimal footprint, provides an even faster startup for Native AOT applications compared to Debian. This is crucial for maximizing efficiency in resource-constrained environments.

### 3. 4x Faster Execution

Beyond startup, Skugga's zero-overhead mocks lead to faster execution times for your application logic.

| Metric          | Standard .NET (JIT) | Skugga (Native AOT) | Impact                    |
| :-------------- | :------------------ | :------------------ | :------------------------ |
| **Execution Time** | ~1.3 s              | **~0.3 s**          | **4x Faster Execution** 🚀 |

This translates to lower CPU bills and a more responsive application for your users.

### 3. Zero-Impact on Developer Workflow

A common concern with source generators is their impact on build times. Skugga is designed to be fast. Our stress test, which involved compiling over 500 mock objects, completed in **under 6 seconds**. This proves that Skugga has a negligible impact on your day-to-day development workflow.

By using Skugga, you can finally embrace the performance and cost benefits of .NET Native AOT without sacrificing testability or developer productivity.

-----

## How It Works

Skugga leverages **C\# 12 Interceptors** to seamlessly rewire your code during compilation.

1.  **Scan:** The Source Generator detects calls to `Mock.Create<T>()`.
2.  **Generate:** It writes a concrete, optimized C\# class (`Skugga_T`) that implements `T`.
3.  **Intercept:** The compiler physically replaces your `Mock.Create` call with `new Skugga_T()`.

> **Zero Friction:** To the developer, it looks like a normal method call. To the runtime, it looks like hand-written, optimized code.

-----

## Installation

```bash
dotnet add package Skugga
```

*Requirements: .NET 8.0+ and C\# 12 enabled.*

## Usage

The API is designed to feel familiar. If you know Moq, you already know Skugga.

```csharp
using Skugga.Core;

public interface IEmailService
{
    string GetEmailAddress(int userId);
    string TenantName { get; }
}

public class Test
{
    public void Run()
    {
        // 1. Create the mock (Intercepted at compile time)
        var mock = Mock.Create<IEmailService>();

        // 2. Configure behavior (Strict matching & Property support)
        mock.Setup(x => x.GetEmailAddress(1)).Returns("digvijay@digvijay.dev");
        mock.Setup(x => x.TenantName).Returns("Contoso");

        // 3. Execute
        var email = mock.GetEmailAddress(1); // Returns "digvijay@digvijay.dev"
        var tenant = mock.TenantName;        // Returns "Contoso"
    }
}
```

## 🔧 Troubleshooting & Best Practices

### Common Issues

**"Cannot mock sealed classes" (SKUGGA001)**
```csharp
// ❌ Won't work - sealed class
public sealed class EmailService { }
var mock = Mock.Create<EmailService>(); // Error!

// ✅ Use interfaces instead
public interface IEmailService { }
var mock = Mock.Create<IEmailService>(); // Works!
```

**"Class has no virtual members" (SKUGGA002)**
```csharp
// ❌ Won't work - non-virtual members
public class EmailService {
    public string GetEmail() => ""; // Not virtual!
}

// ✅ Make members virtual
public class EmailService {
    public virtual string GetEmail() => ""; // Virtual!
}
```

**Generated code not updating**
```bash
# Clean and rebuild
dotnet clean && dotnet build
```

**Setup not matching**
```csharp
// ❌ Exact match required
mock.Setup(x => x.GetData(1)).Returns("one");
mock.GetData(2); // Returns null - no match

// ✅ Use It.IsAny<T>() for flexible matching
mock.Setup(x => x.GetData(It.IsAny<int>())).Returns("any");
mock.GetData(2); // Returns "any"
```

### Migrating from Moq

Skugga achieves **100% practical parity** with Moq's core API (937 tests covering all major features). The API is intentionally identical for seamless migration:

#### Feature Comparison Table

| Feature | Moq | Skugga | Migration Notes |
|---------|-----|--------|----------------|
| **Core Setup/Returns** | ✅ | ✅ | Identical API |
| **Verify with Times** | ✅ | ✅ | Identical API |
| **Properties (Get/Set)** | ✅ | ✅ | Identical API |
| **Callbacks** | ✅ | ✅ | Identical API |
| **Multiple Returns/Throws** | ✅ | ✅ | Identical API |
| **Argument Matchers** | ✅ | ✅ | `It.IsAny`, `It.Is`, `It.IsIn`, `It.IsNotNull`, `It.IsRegex` |
| **Strict Mocks** | ✅ | ✅ | `MockBehavior.Strict` |
| **Setup Sequences** | ✅ | ✅ | Identical API |
| **Protected Members** | ✅ | ✅ | `.Protected().Setup<T>("MethodName")` |
| **Mock.Get<T>()** | ✅ | ✅ | Retrieve IMockSetup from mocked object |
| **Generic Type Parameters** | ✅ | ✅ | `Setup(x => x.Process<int>(It.IsAny<int>()))` |
| **Multiple Interfaces (As)** | ✅ | ✅ | `mock.As<IDisposable>()` |
| **Custom Matchers** | ✅ | ✅ | `Match.Create<T>(predicate)` |
| **Verify with Matchers** | ✅ | ✅ | Works with all matcher types |
| **Events (Raise)** | ✅ | ✅ | Identical API |
| **Partial Mocks** | ✅ | ✅ | Override specific methods via interceptors |
| **Mock.Of<T>(expr)** | ✅ | ✅ | Functional style setup with LINQ expressions |
| **Native AOT Support** | ❌ | ✅ | Moq crashes in AOT, Skugga is AOT-first |
| **Zero Reflection** | ❌ | ✅ | Skugga uses compile-time generation |
| **AutoScribe** | ❌ | ✅ | Self-writing tests (Skugga exclusive) |
| **Chaos Mode** | ❌ | ✅ | Resilience testing (Skugga exclusive) |
| **Zero-Alloc Guard** | ❌ | ✅ | Performance enforcement (Skugga exclusive) |

#### Quick Migration Examples

**1. Basic Setup/Returns**
```csharp
// Moq
var moqMock = new Mock<IEmailService>();
moqMock.Setup(x => x.GetEmail(1)).Returns("test@test.com");
var service = moqMock.Object;

// Skugga - Identical setup API
var skuggaMock = Mock.Create<IEmailService>();
skuggaMock.Setup(x => x.GetEmail(1)).Returns("test@test.com");
// No .Object property needed - mock IS the object
```

**2. Verify with Times**
```csharp
// Moq
moqMock.Verify(x => x.SendEmail(It.IsAny<string>()), Times.Exactly(3));

// Skugga - Identical
skuggaMock.Verify(x => x.SendEmail(It.IsAny<string>()), Times.Exactly(3));
```

**3. Properties**
```csharp
// Moq
moqMock.Setup(x => x.ServerUrl).Returns("https://api.example.com");
moqMock.SetupSet(x => x.ServerUrl = "https://new.example.com").Verifiable();

// Skugga - Identical
skuggaMock.Setup(x => x.ServerUrl).Returns("https://api.example.com");
skuggaMock.SetupSet(x => x.ServerUrl = "https://new.example.com").Verifiable();
```

**4. Callbacks**
```csharp
// Moq
int callCount = 0;
moqMock.Setup(x => x.Process(It.IsAny<int>()))
       .Callback<int>(n => callCount += n)
       .Returns(true);

// Skugga - Identical
int callCount = 0;
skuggaMock.Setup(x => x.Process(It.IsAny<int>()))
           .Callback<int>(n => callCount += n)
           .Returns(true);
```

**5. Setup Sequences**
```csharp
// Moq
moqMock.SetupSequence(x => x.GetNext())
       .Returns(1)
       .Returns(2)
       .Throws(new InvalidOperationException());

// Skugga - Identical
skuggaMock.SetupSequence(x => x.GetNext())
           .Returns(1)
           .Returns(2)
           .Throws(new InvalidOperationException());
```

**6. Protected Members (Abstract Classes)**
```csharp
// Moq
var moqMock = new Mock<AbstractService>();
moqMock.Protected()
       .Setup<string>("ProcessCore", ItExpr.IsAny<string>())
       .Returns("mocked");

// Skugga - Similar API (uses It.IsAny instead of ItExpr)
var skuggaMock = Mock.Create<AbstractService>();
skuggaMock.Protected()
           .Setup<string>("ProcessCore", It.IsAny<string>())
           .Returns("mocked");
```

**7. Strict Mocks**
```csharp
// Moq
var moqMock = new Mock<IService>(MockBehavior.Strict);
// Throws on any un-setup member access

// Skugga - Identical
var skuggaMock = Mock.Create<IService>(MockBehavior.Strict);
// Throws on any un-setup member access
```

**8. Mock.Get (Retrieve Mock from Object)**
```csharp
// Moq
var service = Mock.Of<IEmailService>();
var moqMock = Mock.Get(service);
moqMock.Setup(x => x.GetEmail(1)).Returns("test@test.com");

// Skugga - Mock.Get is supported!
var service = Mock.Create<IEmailService>();
var skuggaMock = Mock.Get(service);
skuggaMock.Setup(x => x.GetEmail(1)).Returns("test@test.com");
```

**9. Argument Matchers**
```csharp
// Moq
moqMock.Setup(x => x.Process(It.Is<int>(n => n > 0))).Returns("positive");
moqMock.Setup(x => x.Handle(It.IsIn("red", "green"))).Returns("color");
moqMock.Setup(x => x.Validate(It.IsRegex("^\\d+$"))).Returns(true);

// Skugga - Identical
skuggaMock.Setup(x => x.Process(It.Is<int>(n => n > 0))).Returns("positive");
skuggaMock.Setup(x => x.Handle(It.IsIn("red", "green"))).Returns("color");
skuggaMock.Setup(x => x.Validate(It.IsRegex("^\\d+$"))).Returns(true);
```

**10. LINQ to Mocks**
```csharp
// Moq
var service = Mock.Of<IService>(x => x.Id == 1 && x.Name == "Test");

// Skugga - Identical
var service = Mock.Of<IService>(x => x.Id == 1 && x.Name == "Test");
```

**11. Ref & Out Parameters**
```csharp
// Skugga uses a simplified API for ref/out values
mock.Setup(x => x.TryParse("123", out It.Ref<int>.IsAny))
    .OutValue(123)
    .Returns(true);
```

**12. Multiple Interfaces**
```csharp
// Moq
var moqMock = new Mock<IEmailService>();
moqMock.As<IDisposable>().Setup(x => x.Dispose());

// Skugga - Identical
var skuggaMock = Mock.Create<IEmailService>();
skuggaMock.As<IDisposable>().Setup(x => x.Dispose());
```

#### Migration Checklist

- ✅ Replace `new Mock<T>()` with `Mock.Create<T>()`
- ✅ Remove `.Object` property access (Skugga mock IS the object)
- ✅ Replace `Mock.Of<T>(expr)` with `Mock.Create<T>()` + explicit `Setup()` calls
- ✅ Replace `ItExpr.*` with `It.*` in Protected() setups
- ✅ All other API calls remain identical
- ✅ Test early and often - Skugga's strict type checking catches issues at compile time

### AOT Constraint: Mock.Of<T>() Limitation

**Note**: Skugga does **not** support `Mock.Of<T>(expression)` syntax due to a fundamental C# interceptor limitation:

```csharp
// ❌ NOT SUPPORTED in Skugga
var mock = Mock.Of<IFoo>(f => f.Name == "bar" && f.Count == 42);

// ✅ Use this pattern instead
var mock = Mock.Create<IFoo>();
mock.Setup(f => f.Name).Returns("bar");
mock.Setup(f => f.Count).Returns(42);
```

**Why?** C# interceptors only work on direct call sites in user code being compiled. When `Mock.Of()` internally calls `Mock.Create()`, that library-internal call cannot be intercepted without runtime IL generation (which breaks AOT compatibility). This is an architectural trade-off to maintain Native AOT support.

**Mock.Get()** *is* fully supported for retrieving the mock interface from created objects.

## Contributing

We welcome community contributions! Skugga is evolving from proof-of-concept to production-ready, and your help is valuable.

### How to Contribute

- 🐛 **Found a bug?** Open an [Issue](https://github.com/Digvijay/Skugga/issues)
- 💡 **Have an idea?** Start a [Discussion](https://github.com/Digvijay/Skugga/discussions)
- 🔧 **Want to help?** Check our [Contributing Guide](CONTRIBUTING.md)
- ✨ **Submit a PR** following our guidelines

Read the [full contributing guidelines](CONTRIBUTING.md) to get started.

## Running on Azure.

Skugga and Azure are a perfect match. By combining Skugga's AOT efficiency with Azure's serverless compute, you can build hyper-efficient, scalable, and secure applications.

```mermaid
graph TD
    subgraph "Azure"
        A[Azure Container Apps]
        B[Azure Functions]
        C[Azure Kubernetes Service]
    end

    subgraph "Skugga-Powered .NET App"
        D{Native AOT}
        E[Distroless Container]
    end

    D --> E
    E --> A
    E --> B
    E --> C

    style A fill:#0078D4,stroke:#fff,color:#fff
    style B fill:#0078D4,stroke:#fff,color:#fff
    style C fill:#0078D4,stroke:#fff,color:#fff
```

### Key Advantages

*   **Cost Efficiency on ACA & Functions:** Run your services on Azure Container Apps or Azure Functions with minimal resource allocation. Skugga's low CPU and memory footprint means you pay less for the same workload.
*   **Instant Scale with AKS:** Deploy to Azure Kubernetes Service (AKS) and benefit from near-instant pod scaling. Smaller container images mean faster pulls and quicker startup times.
*   **Enhanced Security:** "Distroless" containers, made possible by Skugga, dramatically reduce the attack surface of your application, aligning perfectly with Azure's security-first principles.

---

## 📚 Documentation

- **[API Reference](docs/API_REFERENCE.md)** - Complete API documentation with examples
- **[Troubleshooting Guide](docs/TROUBLESHOOTING.md)** - Common issues and solutions
- **[Executive Summary](docs/EXECUTIVE_SUMMARY.md)** - Business value and key differentiators
- **[Technical Summary](docs/TECHNICAL_SUMMARY.md)** - Architecture and implementation details
- **[Benchmark Comparison](docs/BENCHMARK_COMPARISON.md)** - Performance vs other mocking libraries
- **[Benchmark Summary](docs/BENCHMARK_SUMMARY.md)** - Detailed performance analysis
- **[Dependencies](docs/DEPENDENCIES.md)** - Package versions and requirements
- **[Security Policy](docs/SECURITY.md)** - Vulnerability reporting and security guidelines

## License

[MIT](LICENSE)

![Skugga Fun](docs/images/cartoon.png)