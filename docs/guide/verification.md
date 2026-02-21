# Verification

Assert that your code interacted with mocks as expected.

## Basic Verification

```csharp
var mock = Mock.Create<IEmailService>();

// ... use mock in code under test ...

// Verify method was called
mock.Verify(x => x.SendEmail("user@example.com"));
```

## Verify with Times

Control how many times a method should have been called:

```csharp
// Called exactly once
mock.Verify(x => x.SendEmail(It.IsAny<string>()), Times.Once);

// Called exactly N times
mock.Verify(x => x.Process(It.IsAny<int>()), Times.Exactly(3));

// Called at least/at most
mock.Verify(x => x.Log(It.IsAny<string>()), Times.AtLeast(2));
mock.Verify(x => x.Log(It.IsAny<string>()), Times.AtMost(5));

// Never called
mock.Verify(x => x.Delete(It.IsAny<int>()), Times.Never);
```

## Verify with Matchers

Use argument matchers in verification:

```csharp
mock.Verify(
   x => x.Process(It.Is<int>(n => n > 10)),
   Times.AtLeast(2)
);
```

## Verify Properties

```csharp
// Verify property getter was accessed
mock.Verify(x => x.TenantName, Times.Once);

// Verify property setter
mock.VerifySet(x => x.ServerUrl = "https://api.example.com");
```

## VerifyAll

Verify all setups marked as `Verifiable()` were invoked:

```csharp
mock.Setup(x => x.SendEmail(It.IsAny<string>())).Verifiable();
mock.Setup(x => x.Log(It.IsAny<string>())).Verifiable();

// ... code under test ...

mock.VerifyAll(); // Throws if any Verifiable setup wasn't called
```

## VerifyNoOtherCalls

Ensure no unexpected interactions occurred:

```csharp
mock.Verify(x => x.SendEmail("user@example.com"), Times.Once);
mock.VerifyNoOtherCalls(); // Throws if any other method was called
```

## Event Verification

```csharp
mock.VerifyAdd(handler => mock.MyEvent += handler, Times.Once);
mock.VerifyRemove(handler => mock.MyEvent -= handler, Times.Never);
```
