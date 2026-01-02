# Skugga API Reference

Complete API documentation for Skugga mocking library.

## Table of Contents
- [Mock Creation](#mock-creation)
- [Setup API](#setup-api)
- [Verify API](#verify-api)
- [Argument Matchers](#argument-matchers)
- [Setup Sequence](#setup-sequence)
- [Callbacks](#callbacks)
- [Mock Behavior](#mock-behavior)
- [AutoScribe](#autoscribe)
- [Chaos Mode](#chaos-mode)
- [Performance Testing](#performance-testing)

---

## Mock Creation

### `Mock.Create<T>()`
Creates a mock instance of interface or class `T`.

```csharp
// Interface mocking (recommended)
var mock = Mock.Create<IEmailService>();

// Class mocking (members must be virtual)
var mock = Mock.Create<EmailService>();
```

### `Mock.Create<T>(MockBehavior)`
Creates a mock with specific behavior.

```csharp
// Loose behavior (default) - returns default values for un-setup members
var loose = Mock.Create<IService>(MockBehavior.Loose);

// Strict behavior - throws exception for un-setup members
var strict = Mock.Create<IService>(MockBehavior.Strict);
```

---

## Setup API

### Basic Setup
Configure return values for methods and properties.

```csharp
// Method setup
mock.Setup(x => x.GetData(1)).Returns("one");

// Property setup
mock.Setup(x => x.Count).Returns(42);

// Void method setup (for verification)
mock.Setup(x => x.Process(It.IsAny<int>()));
```

### Setup with Functions
Return values computed at invocation time.

```csharp
// Compute return value
mock.Setup(x => x.GetTimestamp())
    .Returns(() => DateTime.UtcNow);

// Access arguments
mock.Setup(x => x.Transform(It.IsAny<string>()))
    .Returns((string input) => input.ToUpper());
```

### Chaining Multiple Setups
```csharp
mock.Setup(x => x.GetData(1)).Returns("one");
mock.Setup(x => x.GetData(2)).Returns("two");
mock.Setup(x => x.Count).Returns(10);
```

---

## Verify API

### `Verify(expression, Times)`
Verify method was called with specific arguments.

```csharp
// Verify exact call
mock.Verify(x => x.GetData(1), Times.Once());

// Verify with any arguments
mock.Verify(x => x.Process(It.IsAny<int>()), Times.AtLeast(2));

// Verify never called
mock.Verify(x => x.Delete(), Times.Never());
```

### Times Helper

| Method | Description | Example |
|--------|-------------|---------|
| `Times.Once()` | Exactly one call | `Times.Once()` |
| `Times.Never()` | Zero calls | `Times.Never()` |
| `Times.Exactly(n)` | Exactly n calls | `Times.Exactly(3)` |
| `Times.AtLeast(n)` | n or more calls | `Times.AtLeast(2)` |
| `Times.AtMost(n)` | n or fewer calls | `Times.AtMost(5)` |
| `Times.Between(m,n)` | Between m and n calls (inclusive) | `Times.Between(2,4)` |

```csharp
// Verify exact count
mock.Verify(x => x.Save(), Times.Exactly(3));

// Verify range
mock.Verify(x => x.Retry(), Times.Between(1, 3));
```

---

## Argument Matchers

### `It.IsAny<T>()`
Matches any value of type T.

```csharp
mock.Setup(x => x.Process(It.IsAny<int>()))
    .Returns("any number");

mock.Process(1);    // Returns "any number"
mock.Process(999);  // Returns "any number"
```

### `It.Is<T>(predicate)`
Matches values satisfying a predicate.

```csharp
// Match positive numbers
mock.Setup(x => x.Process(It.Is<int>(n => n > 0)))
    .Returns("positive");

mock.Process(5);   // Returns "positive"
mock.Process(-1);  // Returns null (no match)

// Complex predicates
mock.Setup(x => x.ValidateUser(It.Is<User>(u => u.Age >= 18 && u.IsActive)))
    .Returns(true);
```

### `It.IsIn<T>(params T[])`
Matches values in a specified set.

```csharp
mock.Setup(x => x.GetColor(It.IsIn("red", "green", "blue")))
    .Returns("primary");

mock.GetColor("red");     // Returns "primary"
mock.GetColor("yellow");  // Returns null (no match)
```

### `It.IsNotNull<T>()`
Matches any non-null value.

```csharp
mock.Setup(x => x.Process(It.IsNotNull<string>()))
    .Returns("valid");

mock.Process("hello");  // Returns "valid"
mock.Process(null);     // Returns null (no match)
```

### `It.IsRegex(pattern)`
Matches strings against regex pattern.

```csharp
mock.Setup(x => x.ValidateEmail(It.IsRegex(@"^[\w\.-]+@[\w\.-]+\.\w+$")))
    .Returns(true);

mock.ValidateEmail("test@example.com");  // Returns true
mock.ValidateEmail("invalid");           // Returns false (no match)
```

### Combining Matchers
```csharp
mock.Setup(x => x.ProcessOrder(
    It.Is<int>(id => id > 0),
    It.IsNotNull<string>(),
    It.IsIn("USD", "EUR", "GBP")
)).Returns("processed");
```

---

## Setup Sequence

### `SetupSequence(expression)`
Configure different return values for consecutive calls.

```csharp
// Basic sequence
mock.SetupSequence(x => x.GetNext())
    .Returns(1)
    .Returns(2)
    .Returns(3);

mock.GetNext(); // Returns 1
mock.GetNext(); // Returns 2
mock.GetNext(); // Returns 3
mock.GetNext(); // Returns 3 (repeats last value)
```

### Sequence with Exceptions
Perfect for testing retry logic.

```csharp
mock.SetupSequence(x => x.FetchData())
    .Throws(new TimeoutException("Connection timeout"))
    .Throws(new TimeoutException("Still timing out"))
    .Returns("success");

// First two calls throw, third succeeds
try { mock.FetchData(); } catch { /* retry */ }
try { mock.FetchData(); } catch { /* retry */ }
var data = mock.FetchData(); // "success"
```

### Property Sequences
```csharp
mock.SetupSequence(x => x.Counter)
    .Returns(0)
    .Returns(1)
    .Returns(2);

var a = mock.Counter; // 0
var b = mock.Counter; // 1
var c = mock.Counter; // 2
```

---

## Callbacks

### `Callback(action)`
Execute code when mock is invoked.

```csharp
var called = false;
mock.Setup(x => x.Execute())
    .Callback(() => called = true);

mock.Execute();
Assert.True(called);
```

### Callbacks with Arguments
Access method arguments in callback.

```csharp
var capturedValue = 0;
mock.Setup(x => x.Process(It.IsAny<int>()))
    .Callback((int value) => capturedValue = value)
    .Returns("processed");

mock.Process(42);
Assert.Equal(42, capturedValue);
```

### Chaining Callback and Returns
```csharp
mock.Setup(x => x.Save(It.IsAny<Data>()))
    .Callback((Data d) => Console.WriteLine($"Saving {d.Id}"))
    .Returns(true);
```

---

## Mock Behavior

### MockBehavior.Loose (Default)
Returns default values for un-setup members.

```csharp
var mock = Mock.Create<IService>(MockBehavior.Loose);
// or
var mock = Mock.Create<IService>();

// No setup for GetData
var result = mock.GetData(); // Returns null (default for string)
```

### MockBehavior.Strict
Throws exception for un-setup members.

```csharp
var mock = Mock.Create<IService>(MockBehavior.Strict);

// Throws exception - GetData not setup
var result = mock.GetData(); // Throws!

// Must setup all called members
mock.Setup(x => x.GetData()).Returns("value");
var result = mock.GetData(); // Returns "value"
```

---

## AutoScribe

### `AutoScribe.Capture<T>(implementation)`
Record real interactions and generate test setup code.

```csharp
// 1. Create recording proxy
var realService = new RealEmailService();
var recorder = AutoScribe.Capture<IEmailService>(realService);

// 2. Use recorder like normal service
var email = recorder.GetEmail(101);
recorder.SendEmail("test@test.com", "Hello");

// 3. Console output (auto-generated test code):
// [AutoScribe] mock.Setup(x => x.GetEmail(101)).Returns("user101@example.com");
// [AutoScribe] mock.Setup(x => x.SendEmail("test@test.com", "Hello")).Returns();
```

### Use Cases
- Bootstrap tests from existing implementations
- Document real API behavior
- Generate regression test suites
- Validate mock configurations against reality

**Status:** ✅ Core functionality complete (18 tests passing). Enhanced features (timing analysis, export/replay, diff tool) planned for future releases.

---

## Chaos Mode

### `mock.Chaos(policy => ...)`
Inject random failures for resilience testing.

```csharp
mock.Chaos(policy => {
    policy.FailureRate = 0.3; // 30% failure rate
    policy.PossibleExceptions = new[] {
        new TimeoutException(),
        new HttpRequestException("Service unavailable")
    };
});

// 30% of calls will randomly throw one of the exceptions
for (int i = 0; i < 100; i++) {
    try {
        mock.CallService();
    } catch (TimeoutException) {
        // Handle timeout
    } catch (HttpRequestException) {
        // Handle service error
    }
}
```

### Configuration Options

| Property | Type | Description |
|----------|------|-------------|
| `FailureRate` | `double` | Probability of failure (0.0 - 1.0) |
| `PossibleExceptions` | `Exception[]` | Exceptions to randomly throw |

**Status:** ✅ Core functionality complete (9 tests passing). Advanced features (latency simulation, chaos schedules, Polly integration) planned for future releases.

---

## Performance Testing

### `AssertAllocations.Zero(action)`
Verify code doesn't allocate heap memory.

```csharp
AssertAllocations.Zero(() => {
    // This block must not allocate
    mock.GetCachedData(); // Should return cached value without allocation
});

// Throws if any heap allocations detected
```

### Use Cases
- Validate hot path performance
- Ensure caching works correctly
- Verify allocation-free operations
- Benchmark optimization improvements

**Status:** ✅ Core functionality complete. Advanced features (detailed allocation reports, CPU profiling, BenchmarkDotNet integration) planned for future releases.

---

## Best Practices

### 1. Prefer Interfaces
```csharp
// ✅ Good
public interface IEmailService { }
var mock = Mock.Create<IEmailService>();

// ⚠️ Acceptable (requires virtual members)
public class EmailService {
    public virtual string GetEmail() => "";
}
var mock = Mock.Create<EmailService>();
```

### 2. Use Specific Matchers
```csharp
// ❌ Too broad
mock.Setup(x => x.Process(It.IsAny<int>()));

// ✅ More specific
mock.Setup(x => x.Process(It.Is<int>(n => n > 0)));
```

### 3. Verify Important Interactions
```csharp
// Always verify critical operations
mock.Verify(x => x.SaveToDatabase(It.IsAny<Data>()), Times.Once());
mock.Verify(x => x.DeleteTempFiles(), Times.Never());
```

### 4. Clean Mocks Between Tests
```csharp
[Fact]
public void Test1() {
    var mock = Mock.Create<IService>(); // Fresh mock
    // ... test
}

[Fact]
public void Test2() {
    var mock = Mock.Create<IService>(); // Fresh mock
    // ... test
}
```

---

## Advanced Scenarios

### Multiple Setups for Same Method
```csharp
// Different setups for different arguments
mock.Setup(x => x.GetData(1)).Returns("one");
mock.Setup(x => x.GetData(2)).Returns("two");
mock.Setup(x => x.GetData(It.Is<int>(n => n > 10))).Returns("large");
```

### Setup with Complex Types
```csharp
mock.Setup(x => x.ProcessOrder(It.Is<Order>(o => 
    o.Total > 100 && 
    o.Items.Count > 0 &&
    o.Status == OrderStatus.Pending
))).Returns(true);
```

### Verify with Timeout
```csharp
// Note: Use your test framework's timeout features
[Fact(Timeout = 5000)]
public void TestWithTimeout() {
    mock.Process();
    mock.Verify(x => x.Process(), Times.Once());
}
```

---

## Generator Diagnostics

Skugga provides compile-time diagnostics to catch issues early:

### SKUGGA001: Cannot mock sealed class
```csharp
public sealed class Service { }
var mock = Mock.Create<Service>(); // ❌ Compile error
```
**Solution:** Use interfaces or remove `sealed` modifier.

### SKUGGA002: Class has no virtual members
```csharp
public class Service {
    public string GetData() => ""; // Not virtual
}
var mock = Mock.Create<Service>(); // ⚠️ Warning
```
**Solution:** Make members `virtual` or mock an interface instead.

---

## Migration from Other Libraries

### From Moq
| Moq | Skugga |
|-----|--------|
| `Mock.Of<T>()` | `Mock.Create<T>()` |
| `Mock.Get(obj).Setup(...)` | `mock.Setup(...)` |
| All other APIs identical | ✅ |

### From NSubstitute
| NSubstitute | Skugga |
|-------------|--------|
| `Substitute.For<T>()` | `Mock.Create<T>()` |
| `sub.Method().Returns(value)` | `mock.Setup(x => x.Method()).Returns(value)` |
| `sub.Received().Method()` | `mock.Verify(x => x.Method(), Times.Once())` |

---

## Support

- 📖 [Full Documentation](https://github.com/Digvijay/Skugga)
- 🐛 [Report Issues](https://github.com/Digvijay/Skugga/issues)
- 💬 [Discussions](https://github.com/Digvijay/Skugga/discussions)
