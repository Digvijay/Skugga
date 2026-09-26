# Known issues

This file records defects found in Skugga, what they affected, and how they were resolved.

## 13. Culture-sensitive decimal literals broke generated setup and verify argument matching

**Severity: high. Fixed.**

### What happened

On machines whose current culture uses a comma decimal separator, setups and verifies containing
literal `decimal` arguments could silently fail to match. For example:

```csharp
mock.Setup(x => x.CalculateDiscount(999.99m, "Electronics")).Returns(50m);
```

returned the default `0m` instead of `50m`, and:

```csharp
mock.Verify(x => x.SendPriceChangeNotification(1, 999.99m, 899.99m), Times.Once());
```

reported zero matching calls even when the invocation had happened.

The `Update_PriceChanged_SendsNotification` sample failure first looked like
`It.IsAny<Product>()` might not match. Instrumenting the test showed the controller reached
`NoContent`, so `UpdateAsync(It.IsAny<Product>())` had matched; the failure was the decimal-literal
`Verify`.

### Root cause

`GeneratorHelpers.FormatConstantValue` emitted numeric constants with culture-sensitive formatting:

```csharp
if (value is decimal m) return $"{m}m";
```

On an `en-SE` machine the generated source became:

```csharp
new object?[] { 999,99m, "Electronics" }
```

That is valid C#, but it is three arguments (`999`, `99m`, `"Electronics"`) instead of two. The
mock invocation therefore failed by argument-count mismatch before value comparison. The
runtime fallback matcher (`MockSetup.AreArgumentsEquivalent`) was not the problem.

The same culture-dependent formatting pattern also affected generated `Verify` interceptors,
AutoScribe's generated value serialization helper, and OpenAPI example/header generation. In the
OpenAPI path it failed loudly as invalid generated C# such as `Price = 29,99` inside object
initializers.

### How it escaped

The repository's strict CI build was already failing on an analyzer issue, so CI did not reach the
sample test step that would have exposed the behavior. One sample setup also used
`Returns(false)` for a `bool` method; because `false` is the default return value, that setup could
fail to match while the test still appeared to pass.

### Fix and proof

The source generator now formats emitted numeric constants with
`CultureInfo.InvariantCulture`. AutoScribe and OpenAPI generated numeric values also use invariant
formatting.

Regression tests use non-default return values so a missed match cannot be masked:

- literal `decimal` setup returning `50m`;
- literal `decimal` setup returning `true`;
- literal `decimal` verify with two decimal arguments;
- a generator test that forces `sv-SE` and asserts generated source contains `999.99m`, not
  `999,99m`.

Those tests fail before the fix and pass after it. The OpenAPI test project also now builds and
passes under the same locale.
