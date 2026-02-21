# Mock Creation

Skugga provides several ways to create mocks, all resolved at compile time via source generators and C# 12 interceptors.

## Basic Creation

```csharp
using Skugga.Core;

// Create a mock from an interface
var mock = Mock.Create<IEmailService>();
```

Unlike Moq, the returned object **is** the mock -- no `.Object` property needed.

## Mock Behaviors

### Loose Mode (Default)

Un-setup members return `null` or `default`:

```csharp
var mock = Mock.Create<IEmailService>();
var email = mock.GetEmailAddress(1); // Returns null
```

### Strict Mode

Throws `MockException` if any un-setup member is accessed:

```csharp
var mock = Mock.Create<IEmailService>(MockBehavior.Strict);
mock.GetEmailAddress(1); // Throws MockException!
```

## Mocking Abstract Classes

Skugga can mock abstract classes with virtual/abstract members:

```csharp
public abstract class AbstractService
{
   public string Execute(string input) => ProcessCore(input);
   protected abstract string ProcessCore(string input);
}

var mock = Mock.Create<AbstractService>();
mock.Protected()
   .Setup<string>("ProcessCore", It.IsAny<string>())
   .Returns("mocked result");
```

## Mock.Get\<T>()

Retrieve the mock interface from a mocked object:

```csharp
var service = Mock.Create<IEmailService>();
var mockSetup = Mock.Get(service);
mockSetup.Setup(x => x.GetEmail(1)).Returns("test@test.com");
```

## MockRepository

Centralized mock management and batch verification:

```csharp
var repo = new MockRepository(MockBehavior.Strict);

var emailMock = repo.Create<IEmailService>();
var logMock = repo.Create<ILogger>();

// Setup...

// Verify all mocks at once
repo.VerifyAll();
repo.VerifyNoOtherCalls();
```

## Multiple Interfaces

Mock an object that implements multiple interfaces:

```csharp
var mock = Mock.Create<IEmailService>();
mock.As<IDisposable>().Setup(x => x.Dispose());

// Cast and use
var disposable = (IDisposable)mock;
disposable.Dispose();
```

## DefaultValue Strategies

Control default values for un-setup members:

```csharp
var mock = Mock.Create<IService>();

// Auto-mock nested properties (recursive mocking)
mock.DefaultValue = DefaultValue.Mock;

// Access nested properties without explicit setup
var nested = mock.Config.Database.ConnectionString; // Auto-mocked
```
