# Strict Mocks

Ensure no interaction goes unnoticed with strict mock behavior.

## Overview

By default, Skugga mocks use **Loose** behavior -- un-setup members return `null`/`default`. **Strict** mode throws `MockException` if any un-setup member is accessed.

## Usage

```csharp
// Strict: throws on ANY un-setup member access
var mock = Mock.Create<IEmailService>(MockBehavior.Strict);

// Must setup before use
mock.Setup(x => x.GetEmail(1)).Returns("test@test.com");
mock.GetEmail(1); // Returns "test@test.com"
mock.GetEmail(2); // Throws MockException! (no matching setup)
```

## When to Use Strict Mocks

**Use Strict Mode when:**
- You want complete control over mock behavior
- You need to ensure only expected methods are called
- You're testing critical paths where unexpected calls indicate bugs

**Use Loose Mode when:**
- You're testing specific behavior and don't care about other interactions
- You want minimal setup for focused tests
- You're prototyping or exploratory testing

## Combining with VerifyNoOtherCalls

For maximum safety:

```csharp
var mock = Mock.Create<IService>(MockBehavior.Strict);
mock.Setup(x => x.Process(It.IsAny<int>())).Returns(true);

// ... code under test ...

mock.Verify(x => x.Process(It.IsAny<int>()), Times.Once);
mock.VerifyNoOtherCalls();
```
