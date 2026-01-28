# Advanced Features Demo

This sample demonstrates advanced Skugga features that achieve 100% feature parity with Moq.

## Features Demonstrated

### 1. LINQ to Mocks (`Mock.Of<T>`)
Functional-style mock initialization using expression trees.

```csharp
var service = Mock.Of<IService>(s => 
    s.Id == 1 && 
    s.Name == "Demo Service" && 
    s.IsActive
);
```

### 2. Ref/Out Parameter Support
Mock methods with `ref` and `out` parameters.

```csharp
// Out parameter
int outDummy = 0;
mock.Setup(x => x.TryParse("100", out outDummy))
    .Returns(true)
    .OutValue(1, 100);  // Parameter index 1

// Ref parameter
int refDummy = It.IsAny<int>();
mock.Setup(x => x.Increment(ref refDummy))
    .RefValue(0, 50);  // Parameter index 0
```

### 3. MockRepository
Manage multiple mocks with shared configuration and verification.

```csharp
var repo = new MockRepository(MockBehavior.Strict);
var service1 = repo.Create<IService>();
service1.Setup(x => x.Name).Returns("Repo Mock");

repo.VerifyAll();  // Verify all mocks in repository
```

### 4. Protected Members
Mock protected methods and properties on abstract classes.

```csharp
var mock = Mock.Create<AbstractService>();
mock.Protected()
    .Setup<string>("ProcessCore", It.IsAny<string>())
    .Returns("Mocked Protected");
```

## Running the Demo

```bash
dotnet run
```

## Note on Generator Integration

This demo is designed to showcase the API surface. For full generator integration in console applications, ensure:
- Generator is referenced with `OutputItemType="Analyzer"` and `ReferenceOutputAssembly="false"`
- `InterceptorsPreviewNamespaces` includes `Skugga.Generated`
- Project targets .NET 8.0 or later

For guaranteed working examples, see the test projects in `tests/Skugga.Core.Tests/`.

## Related Documentation

- [README.md](../../README.md) - Main project documentation
- [API_REFERENCE.md](../../docs/API_REFERENCE.md) - Complete API reference
- [OutRefTests.cs](../../tests/Skugga.Core.Tests/Advanced/OutRefTests.cs) - Comprehensive ref/out tests
