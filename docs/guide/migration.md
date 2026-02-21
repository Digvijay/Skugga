# Migrating from Moq

Skugga achieves **100% practical parity** with Moq's core API (937 tests covering all major features). The API is intentionally identical for seamless migration.

## Migration Checklist

- Replace `new Mock<T>()` with `Mock.Create<T>()`
- Remove `.Object` property access (Skugga mock IS the object)
- Replace `ItExpr.*` with `It.*` in `Protected()` setups
- All other API calls remain identical
- Test early and often -- Skugga's strict type checking catches issues at compile time

## Quick Examples

### Basic Setup/Returns

```csharp
// Moq
var moqMock = new Mock<IEmailService>();
moqMock.Setup(x => x.GetEmail(1)).Returns("test@test.com");
var service = moqMock.Object;

// Skugga -- identical setup API
var skuggaMock = Mock.Create<IEmailService>();
skuggaMock.Setup(x => x.GetEmail(1)).Returns("test@test.com");
// No .Object property needed -- mock IS the object
```

### Verify with Times

```csharp
// Moq
moqMock.Verify(x => x.SendEmail(It.IsAny<string>()), Times.Exactly(3));

// Skugga -- identical
skuggaMock.Verify(x => x.SendEmail(It.IsAny<string>()), Times.Exactly(3));
```

### Properties

```csharp
// Moq
moqMock.Setup(x => x.ServerUrl).Returns("https://api.example.com");
moqMock.SetupSet(x => x.ServerUrl = "https://new.example.com").Verifiable();

// Skugga -- identical
skuggaMock.Setup(x => x.ServerUrl).Returns("https://api.example.com");
skuggaMock.SetupSet(x => x.ServerUrl = "https://new.example.com").Verifiable();
```

### Setup Sequences

```csharp
// Moq
moqMock.SetupSequence(x => x.GetNext())
       .Returns(1)
       .Returns(2)
       .Throws(new InvalidOperationException());

// Skugga -- identical
skuggaMock.SetupSequence(x => x.GetNext())
           .Returns(1)
           .Returns(2)
           .Throws(new InvalidOperationException());
```

### Protected Members

```csharp
// Moq
var moqMock = new Mock<AbstractService>();
moqMock.Protected()
       .Setup<string>("ProcessCore", ItExpr.IsAny<string>())
       .Returns("mocked");

// Skugga -- uses It.IsAny instead of ItExpr
var skuggaMock = Mock.Create<AbstractService>();
skuggaMock.Protected()
           .Setup<string>("ProcessCore", It.IsAny<string>())
           .Returns("mocked");
```

### Strict Mocks

```csharp
// Moq
var moqMock = new Mock<IService>(MockBehavior.Strict);

// Skugga -- identical
var skuggaMock = Mock.Create<IService>(MockBehavior.Strict);
```

### Multiple Interfaces

```csharp
// Moq
var moqMock = new Mock<IEmailService>();
moqMock.As<IDisposable>().Setup(x => x.Dispose());

// Skugga -- identical
var skuggaMock = Mock.Create<IEmailService>();
skuggaMock.As<IDisposable>().Setup(x => x.Dispose());
```

## Feature Comparison

| Feature | Moq | Skugga | Notes |
|---------|-----|--------|-------|
| **Core Setup/Returns** | Yes | Yes | Identical API |
| **Verify with Times** | Yes | Yes | Identical API |
| **Properties (Get/Set)** | Yes | Yes | Identical API |
| **Callbacks** | Yes | Yes | Identical API |
| **Argument Matchers** | Yes | Yes | `It.IsAny`, `It.Is`, `It.IsIn`, `It.IsNotNull`, `It.IsRegex` |
| **Strict Mocks** | Yes | Yes | `MockBehavior.Strict` |
| **Setup Sequences** | Yes | Yes | Identical API |
| **Protected Members** | Yes | Yes | `.Protected().Setup<T>("MethodName")` |
| **Generic Type Parameters** | Yes | Yes | Full support |
| **Multiple Interfaces (As)** | Yes | Yes | `mock.As<IDisposable>()` |
| **Custom Matchers** | Yes | Yes | `Match.Create<T>(predicate)` |
| **Events (Raise)** | Yes | Yes | Identical API |
| **Mock.Get\<T>()** | Yes | Yes | Retrieve mock from object |
| **Native AOT Support** | No | Yes | Moq crashes in AOT |
| **Zero Reflection** | No | Yes | Compile-time generation |
| **AutoScribe** | No | Yes | Self-writing tests |
| **Chaos Mode** | No | Yes | Resilience testing |
| **Zero-Alloc Guard** | No | Yes | Performance enforcement |

## AOT Limitation: Mock.Of\<T>(expression)

Skugga does **not** support `Mock.Of<T>(expression)` syntax due to a fundamental C# interceptor limitation:

```csharp
// NOT SUPPORTED in Skugga
var mock = Mock.Of<IFoo>(f => f.Name == "bar" && f.Count == 42);

// Use this pattern instead
var mock = Mock.Create<IFoo>();
mock.Setup(f => f.Name).Returns("bar");
mock.Setup(f => f.Count).Returns(42);
```

**Why?** C# interceptors only work on direct call sites in user code. When `Mock.Of()` internally calls `Mock.Create()`, that library-internal call cannot be intercepted without runtime IL generation (which breaks AOT compatibility).
