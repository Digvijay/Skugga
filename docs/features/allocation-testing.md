# Zero-Allocation Testing -- Performance Enforcement

> **"Catch memory regressions before they hit production."**

**Industry First:** Skugga is the **only .NET mocking library** providing allocation assertions to prove your hot paths are truly zero-allocation.

## The Problem

You've optimized your code to avoid allocations, but how do you **prove** it stays that way?

```csharp
// You think this is zero-allocation...
public string GetCacheKey(int userId, string resource)
{
    return $"user:{userId}:{resource}"; // Allocates!
}
// 1M requests/day x 80 bytes = 80 GB of garbage!
```

## The Solution

### Zero-Allocation Assertion

```csharp
AssertAllocations.Zero(() => {
    cache.Lookup(key); // If this allocates, test FAILS
});
```

### Allocation Budgets

```csharp
AssertAllocations.AtMost(() => {
    ProcessRequest(data);
}, maxBytes: 1024); // Fail if > 1KB
```

### Measurement & Comparison

```csharp
var report = AssertAllocations.Measure(() => {
    for (int i = 0; i < 1000; i++) {
        GetCacheKey(i, "profile");
    }
}, "Cache Key Generation");

Console.WriteLine($"Allocated: {report.BytesAllocated:N0} bytes");
Console.WriteLine($"Gen0: {report.Gen0Collections}");
Console.WriteLine($"Gen1: {report.Gen1Collections}");
```

## Real-World Examples

### String Optimization

```csharp
// Before: 50,000 bytes
var key = "user:" + userId + ":profile";

// After: 0 bytes
FormatCacheKey(stackAllocBuffer, userId);
```

### LINQ vs Loops

```csharp
// Before: 80,000 bytes (LINQ iterators + ToList)
var filtered = items.Where(x => x % 2 == 0).Select(x => x * 2).ToList();

// After: 4,000 bytes (pre-sized list, for-loop)
var result = new List<int>(500);
for (int i = 0; i < items.Count; i++)
    if (items[i] % 2 == 0) result.Add(items[i] * 2);
```

## CI/CD Integration

```csharp
[Fact]
public void HotPath_NoAllocationRegression()
{
    const long MaxAllowedBytes = 512; // Baseline

    var report = AssertAllocations.Measure(() => HotPath.Execute(input));

    Assert.True(
        report.BytesAllocated <= MaxAllowedBytes,
        $"Regression! Expected <= {MaxAllowedBytes}, got {report.BytesAllocated}"
    );
}
```

## Comparison

| Tool | Assertions | CI/CD | Zero Setup |
|------|:----------:|:-----:|:----------:|
| **Skugga** | Yes | Yes | Yes |
| BenchmarkDotNet | No | Partial | No |
| PerfView | No | No | No |
| dotMemory | No | No | No |

[Full Allocation Testing guide](https://github.com/Digvijay/Skugga/blob/master/docs/ALLOCATION_TESTING.md) | [Demo code](https://github.com/Digvijay/Skugga/tree/master/samples/AllocationTestingDemo)
