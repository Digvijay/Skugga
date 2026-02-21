# Setup & Returns

Configure mock method return values and behavior with Skugga's fluent API.

## Method Setup

```csharp
var mock = Mock.Create<IEmailService>();

// Return a value for specific arguments
mock.Setup(x => x.GetEmail(1)).Returns("user@example.com");

// Return for any argument
mock.Setup(x => x.GetEmail(It.IsAny<int>())).Returns("default@example.com");
```

## Property Setup

```csharp
// Property getters
mock.Setup(x => x.TenantName).Returns("Contoso");

// Property setters
mock.SetupSet(x => x.ServerUrl = "https://api.example.com").Verifiable();
```

## Async Methods

```csharp
mock.Setup(x => x.SendEmailAsync(It.IsAny<string>()))
   .ReturnsAsync(true);
```

## Callbacks

Execute custom logic when a mock method is called:

```csharp
int callCount = 0;
mock.Setup(x => x.Process(It.IsAny<int>()))
   .Callback<int>(n => callCount += n)
   .Returns(true);
```

## Throwing Exceptions

```csharp
// Throw on specific call
mock.Setup(x => x.GetData("invalid"))
   .Throws(new ArgumentException("Invalid key"));

// Throw typed exception
mock.Setup(x => x.GetData("invalid"))
   .Throws<ArgumentException>();
```

## Conditional Returns

Combine argument matchers for conditional behavior:

```csharp
mock.Setup(x => x.Process(It.Is<int>(n => n > 0))).Returns("positive");
mock.Setup(x => x.Process(It.Is<int>(n => n < 0))).Returns("negative");
mock.Setup(x => x.Process(0)).Returns("zero");
```

## Void Methods

```csharp
mock.Setup(x => x.Log(It.IsAny<string>()))
   .Callback<string>(msg => Console.WriteLine($"Logged: {msg}"));
```
