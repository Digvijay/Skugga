# Protected Members

Mock protected methods and properties on abstract classes -- essential for testing inheritance hierarchies and template method patterns.

## Overview

Many frameworks use the **Template Method** pattern where a base class defines the algorithm and subclasses implement protected methods. Skugga lets you mock these protected members.

## Mocking Protected Methods

```csharp
public abstract class AbstractService
{
   public string Execute(string input) => ProcessCore(input);
   protected abstract string ProcessCore(string input);
   protected abstract int MaxRetries { get; }
}

var mock = Mock.Create<AbstractService>();

// Setup protected method by name
mock.Protected()
   .Setup<string>("ProcessCore", It.IsAny<string>())
   .Returns("mocked result");

// Test through the public API
var result = mock.Execute("test"); // Returns "mocked result"
```

## Protected Properties

```csharp
mock.Protected()
   .SetupGet<int>("MaxRetries")
   .Returns(3);
```

## Protected Callbacks

```csharp
mock.Protected()
   .Setup("ProcessCore", It.IsAny<string>())
   .Callback<string>(input => Console.WriteLine($"Processing: {input}"));
```

## Verifying Protected Members

```csharp
mock.Protected().Verify(
   "ProcessCore", 
   Times.Once(), 
   It.Is<string>(s => s.Length > 0)
);
```
