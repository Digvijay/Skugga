# Ref & Out Parameters

Full support for methods with `ref` and `out` parameters.

## Out Parameters

Configure out values for methods using the `TryParse` pattern:

```csharp
mock.Setup(x => x.TryParse("123", out It.Ref<int>.IsAny))
   .OutValue(123)
   .Returns(true);

mock.TryParse("123", out var result); // result = 123, returns true
```

## Ref Parameters

```csharp
mock.Setup(x => x.Swap(ref It.Ref<int>.IsAny, ref It.Ref<int>.IsAny))
   .RefValue(0, 20)
   .RefValue(1, 10);
```

## Dynamic Values

Use functions for dynamic out/ref values:

```csharp
mock.Setup(x => x.TryGetValue("key", out It.Ref<string>.IsAny))
   .OutValueFunc(0, () => DateTime.UtcNow.ToString())
   .Returns(true);
```

## Complex Ref/Out Scenarios

Use `CallbackRefOut` for complex cases with mixed ref/out:

```csharp
mock.Setup(x => x.ComplexMethod(
   It.IsAny<string>(), 
   out It.Ref<int>.IsAny, 
   ref It.Ref<string>.IsAny
))
.CallbackRefOut((string input, out int count, ref string msg) => {
   count = input.Length;
   msg = $"Processed: {input}";
})
.Returns(true);
```
