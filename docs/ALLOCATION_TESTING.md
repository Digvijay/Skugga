# Zero-Allocation Testing - Performance Enforcement

> **"Catch memory regressions before they hit production."**

**Industry First:** Skugga is a **.NET mocking library** that also provides allocation assertions to prove your hot paths are truly zero-allocation.

## The Problem

You've optimized your code to avoid allocations, but how do you **prove** it stays that way?

```csharp
// You think this is zero-allocation...
public string GetCacheKey(int userId, string resource)
{
    return $"user:{userId}:{resource}"; //  Allocates! String interpolation
}

// Tests pass, code ships, but...
// 1M requests/day x 80 bytes = 80 GB of garbage!
```

**Problems:**
- Can't prove code is allocation-free
- Regressions sneak in during refactoring
- No visibility into allocation patterns
- Performance degrades over time

---

## The Solution: GC-Level Assertions

Enforce zero-allocation requirements with precise measurements:

```csharp
// PROVE it's zero-allocation
AssertAllocations.Zero(() => {
    cache.Lookup(key); // If this allocates, test FAILS
});

// Set allocation budgets
AssertAllocations.AtMost(() => {
    ProcessRequest(data);
}, maxBytes: 1024); // Fail if > 1KB

// Measure and compare
var report = AssertAllocations.Measure(() => {
    for (int i = 0; i < 1000; i++) {
        GetCacheKey(i, "profile");
    }
});

Console.WriteLine($"Allocated: {report.BytesAllocated:N0} bytes");
// Before optimization: 80,000 bytes
// After optimization: 0 bytes
```

---

## Quick Start

### Step 1: Install Skugga

```bash
dotnet add package Skugga
```

### Step 2: Assert Zero Allocations

```csharp
using Skugga.Core;

[Fact]
public void CacheLookup_IsZeroAllocation()
{
    var cache = new HighPerformanceCache();
    var key = "user:123";

    // This test FAILS if any heap allocation occurs
    AssertAllocations.Zero(() => {
        cache.Lookup(key);
    });
}
```

### Step 3: Fix Allocations

```csharp
// Before - Allocates (string interpolation)
public string GetCacheKey(int userId, string resource)
{
    return $"user:{userId}:{resource}";
}

// After - Zero allocation (string.Create + ValueStringBuilder)
public string GetCacheKey(int userId, string resource)
{
    return string.Create(null,
        $"user:{userId}:{resource}");
}
```

---

## Core Features

### 1. Zero-Allocation Assertion

Enforce zero allocations on hot paths:

```csharp
[Fact]
public void HotPath_MustBeZeroAllocation()
{
    AssertAllocations.Zero(() => {
        // Code that MUST NOT allocate
        var result = highPerformanceParser.Parse(input);
    });

    // Test fails if ANY allocation occurs:
    // "Expected 0 bytes allocated, but got 256 bytes"
}
```

Perfect for:
- Cache lookups
- Hot parsing paths
- Inner loops
- Real-time systems

### 2. Allocation Budgets

Set maximum allowed allocations:

```csharp
[Fact]
public void ApiEndpoint_StaysUnder10KB()
{
    AssertAllocations.AtMost(() => {
        var response = controller.HandleRequest(request);
    }, maxBytes: 10_240); // 10KB limit

    // Fails if allocation exceeds budget
}
```

### 3. Allocation Measurement

Measure and track allocation patterns:

```csharp
[Fact]
public void CompareAllocationPatterns()
{
    // Measure old implementation
    var oldReport = AssertAllocations.Measure(() => {
        OldImplementation.Process(data);
    }, "Old Implementation");

    // Measure new implementation
    var newReport = AssertAllocations.Measure(() => {
        NewImplementation.Process(data);
    }, "New Implementation");

    // Compare
    Console.WriteLine($"Old: {oldReport.BytesAllocated:N0} bytes");
    Console.WriteLine($"New: {newReport.BytesAllocated:N0} bytes");
    Console.WriteLine($"Saved: {oldReport.BytesAllocated - newReport.BytesAllocated:N0} bytes");

    // Assert improvement
    Assert.True(newReport.BytesAllocated < oldReport.BytesAllocated);
}
```

### 4. Detailed Reports

Get granular allocation breakdowns:

```csharp
var report = AssertAllocations.Measure(() => {
    for (int i = 0; i < 1000; i++) {
        ProcessItem(items[i]);
    }
}, "Batch Processing");

Console.WriteLine($"Total allocated: {report.BytesAllocated:N0} bytes");
Console.WriteLine($"Gen0 collections: {report.Gen0Collections}");
Console.WriteLine($"Gen1 collections: {report.Gen1Collections}");
Console.WriteLine($"Gen2 collections: {report.Gen2Collections}");
Console.WriteLine($"Allocations per operation: {report.BytesPerOperation} bytes");
```

### 5. CI/CD Integration

Fail builds on allocation regressions:

```csharp
[Fact]
public void HotPath_AllocationBudget()
{
    // Baseline from last release
    const long MaxAllowedBytes = 512;

    var report = AssertAllocations.Measure(() => {
        HotPath.Execute(input);
    });

    // Fail CI if we regress
    Assert.True(
        report.BytesAllocated <= MaxAllowedBytes,
        $"Allocation regression detected! " +
        $"Expected <={MaxAllowedBytes} bytes, got {report.BytesAllocated} bytes"
    );
}
```

---

## Real-World Examples

### Example 1: String Concatenation

**Before - 50KB allocated:**

```csharp
[Fact]
public void StringConcat_Allocates()
{
    var report = AssertAllocations.Measure(() => {
        for (int i = 0; i < 1000; i++) {
            var key = "user:" + i + ":profile"; // Allocates!
        }
    });

    // Output: Allocated: 50,000 bytes
    Console.WriteLine($"Allocated: {report.BytesAllocated:N0} bytes");
}
```

**After - 0 bytes allocated:**

```csharp
[Fact]
public void StringCreate_ZeroAllocation()
{
    AssertAllocations.Zero(() => {
        var buffer = new char[64]; // Stack allocated
        for (int i = 0; i < 1000; i++) {
            FormatCacheKey(buffer, i); // Reuse buffer
        }
    });

    //  Test passes - zero allocation!
}

private static void FormatCacheKey(Span<char> buffer, int userId)
{
    // Uses stack-allocated buffer, no heap allocation
    var written = 0;
    "user:".AsSpan().CopyTo(buffer);
    written += 5;
    userId.TryFormat(buffer[written..], out var userIdLen);
    written += userIdLen;
    ":profile".AsSpan().CopyTo(buffer[written..]);
}
```

### Example 2: JSON Parsing

**Before - 500KB allocated:**

```csharp
[Fact]
public void JsonParse_Allocates()
{
    var json = GetLargeJson(); // 100KB JSON

    var report = AssertAllocations.Measure(() => {
        for (int i = 0; i < 100; i++) {
            var obj = JsonSerializer.Deserialize<Order>(json); // Allocates!
        }
    });

    // Output: Allocated: 500,000 bytes
    Console.WriteLine($"Allocated: {report.BytesAllocated:N0} bytes");
}
```

**After - 5KB allocated (95% reduction):**

```csharp
[Fact]
public void JsonParse_Pooled_LowAllocation()
{
    var json = GetLargeJson();
    var pool = ArrayPool<byte>.Shared;

    var report = AssertAllocations.Measure(() => {
        for (int i = 0; i < 100; i++) {
            var buffer = pool.Rent(json.Length);
            try {
                var reader = new Utf8JsonReader(buffer);
                ParseOrderFromReader(ref reader); // Custom parser
            } finally {
                pool.Return(buffer);
            }
        }
    });

    // Output: Allocated: 5,000 bytes (95% reduction!)
    Console.WriteLine($"Allocated: {report.BytesAllocated:N0} bytes");
}
```

### Example 3: LINQ vs Loops

**Before - 80KB allocated (LINQ):**

```csharp
[Fact]
public void Linq_Allocates()
{
    var items = Enumerable.Range(0, 1000).ToList();

    var report = AssertAllocations.Measure(() => {
        var filtered = items
            .Where(x => x % 2 == 0)     // Allocates iterator
            .Select(x => x * 2)          // Allocates iterator
            .ToList();                   // Allocates list
    });

    // Output: Allocated: 80,000 bytes
    Console.WriteLine($"Allocated: {report.BytesAllocated:N0} bytes");
}
```

**After - 4KB allocated (for-loop):**

```csharp
[Fact]
public void ForLoop_MinimalAllocation()
{
    var items = Enumerable.Range(0, 1000).ToList();

    var report = AssertAllocations.Measure(() => {
        var result = new List<int>(500); // Pre-sized
        for (int i = 0; i < items.Count; i++) {
            if (items[i] % 2 == 0) {
                result.Add(items[i] * 2);
            }
        }
    });

    // Output: Allocated: 4,000 bytes (95% reduction!)
    Console.WriteLine($"Allocated: {report.BytesAllocated:N0} bytes");
}
```

---

## Demo and Example Code

See Zero-Allocation Testing in action:

**[-> View Demo and Example Code](../samples/AllocationTestingDemo)**

The demo shows:
- 6 before/after scenarios
- String concatenation optimization (50KB -> 0 bytes)
- JSON parsing optimization (500KB -> 5KB)
- LINQ vs loops comparison (80KB -> 4KB)

---

## Advanced Scenarios

### Testing Object Pooling

Verify pooled objects don't allocate:

```csharp
[Fact]
public void ObjectPool_ZeroAllocation()
{
    var pool = new ObjectPool<StringBuilder>(() => new StringBuilder(256));

    AssertAllocations.Zero(() => {
        var sb = pool.Get();
        try {
            sb.Append("user:");
            sb.Append(123);
            var result = sb.ToString(); // Only string allocation
        } finally {
            sb.Clear();
            pool.Return(sb);
        }
    });
}
```

### Testing Span<T> Usage

Verify stack allocations work:

```csharp
[Fact]
public void SpanUsage_StackAllocationOnly()
{
    AssertAllocations.Zero(() => {
        Span<byte> buffer = stackalloc byte[256]; // Stack only
        WriteDataToSpan(buffer);
        ProcessSpan(buffer);
    });
}
```

### Comparing Implementations

A/B test allocation patterns:

```csharp
[Theory]
[InlineData("StringConcat", 50_000)]
[InlineData("StringBuilder", 10_000)]
[InlineData("Span", 0)]
public void CompareStringBuilding(string approach, long expectedMaxBytes)
{
    var report = approach switch {
        "StringConcat" => MeasureStringConcat(),
        "StringBuilder" => MeasureStringBuilder(),
        "Span" => MeasureSpan(),
        _ => throw new ArgumentException()
    };

    Assert.True(report.BytesAllocated <= expectedMaxBytes,
        $"{approach}: Expected <={expectedMaxBytes}, got {report.BytesAllocated}");
}
```

---

## Best Practices

### 1. Test Hot Paths Only

Don't over-optimize cold paths:

```csharp
// Good - hot path needs zero-allocation
[Fact]
public void CacheLookup_HotPath_ZeroAllocation()
{
    AssertAllocations.Zero(() => cache.Lookup(key));
}

// Bad - startup code doesn't need to be zero-allocation
[Fact]
public void ApplicationStartup_ZeroAllocation() // Too strict!
{
    AssertAllocations.Zero(() => app.Initialize());
}
```

### 2. Use Budgets for Complex Code

Zero-allocation isn't always realistic:

```csharp
// Good - realistic budget
AssertAllocations.AtMost(() => {
    var response = controller.HandleRequest(request);
}, maxBytes: 10_240); // 10KB is reasonable

// Bad - unrealistic for complex operation
AssertAllocations.Zero(() => {
    var response = controller.HandleRequest(request); // Too strict!
});
```

### 3. Track Regressions in CI/CD

Store baselines and detect regressions:

```csharp
[Fact]
public void HotPath_NoAllocationRegression()
{
    var baseline = LoadBaselineFromFile(); // Last release

    var current = AssertAllocations.Measure(() => {
        HotPath.Execute(input);
    });

    // Fail if allocation increased by >10%
    var threshold = baseline * 1.10m;
    Assert.True(current.BytesAllocated <= threshold,
        $"Regression! Baseline: {baseline}, Current: {current.BytesAllocated}");
}
```

### 4. Measure Large Iteration Counts

Single operations may be too small to measure:

```csharp
// Bad - too small to measure accurately
AssertAllocations.Zero(() => {
    ParseInt("123"); // Single call
});

// Good - loop amplifies allocations
AssertAllocations.Zero(() => {
    for (int i = 0; i < 10_000; i++) {
        ParseInt("123"); // 10k iterations
    }
});
```

### 5. Warm Up First

Avoid JIT allocations in measurements:

```csharp
[Fact]
public void Measure_AfterWarmup()
{
    // Warm up to trigger JIT
    for (int i = 0; i < 100; i++) {
        HotPath.Execute(input);
    }

    // Now measure (no JIT allocations)
    var report = AssertAllocations.Measure(() => {
        for (int i = 0; i < 10_000; i++) {
            HotPath.Execute(input);
        }
    });

    Assert.True(report.BytesAllocated == 0);
}
```

---

## Troubleshooting

### Issue: "Allocations detected, but I can't find them"

**Solution:** Use memory profilers to identify sources:

```csharp
// Add detailed tracking
var report = AssertAllocations.MeasureDetailed(() => {
    SuspiciousMethod();
});

// Check GC stats
Console.WriteLine($"Gen0: {report.Gen0Collections}");
Console.WriteLine($"Gen1: {report.Gen1Collections}");
Console.WriteLine($"Allocated: {report.BytesAllocated}");

// Use dotMemory, PerfView, or BenchmarkDotNet for deep analysis
```

### Issue: "Results are inconsistent"

**Solution:** Run in Release mode and disable background GC:

```csharp
// In test setup
GC.TryStartNoGCRegion(10_000_000); // 10MB

try {
    AssertAllocations.Zero(() => {
        HotPath.Execute(input);
    });
} finally {
    GC.EndNoGCRegion();
}
```

### Issue: "Test fails due to JIT allocations"

**Solution:** Warm up before measuring:

```csharp
// Warm up
for (int i = 0; i < 1000; i++) {
    Method();
}

// Force GC to clear JIT allocations
GC.Collect();
GC.WaitForPendingFinalizers();
GC.Collect();

// Now measure
AssertAllocations.Zero(() => Method());
```

---

## Comparison with Other Tools

| Tool | Skugga | BenchmarkDotNet | PerfView | dotMemory |
|------|--------|-----------------|----------|-----------|
| **Allocation Assertions** |  Yes |  No |  No |  No |
| **Unit Test Integration** |  Native |  Separate |  No |  No |
| **CI/CD Friendly** |  Yes |  Complex |  No |  No |
| **Zero Setup** |  Yes |  Requires config |  GUI only |  GUI only |
| **Fail Tests on Regression** |  Yes |  No |  No |  No |

**Use Both:**
- **Skugga** for unit test assertions and CI/CD gates
- **BenchmarkDotNet** for detailed performance analysis
- **PerfView/dotMemory** for deep profiling

---

## FAQ

**Q: Does this work in Debug mode?**
A: Best results in Release mode. Debug mode has additional allocations from the compiler.

**Q: Can I measure allocations in production?**
A: No, this is for testing only. Use ETW profilers or custom metrics for production.

**Q: What about stack allocations?**
A: Stack allocations (stackalloc, Span<T>) are not tracked - they don't hit the heap!

**Q: Does this work on .NET Framework?**
A: Yes, but .NET 5+ gives more accurate results.

---

## Related Features

- **[Chaos Engineering](CHAOS_ENGINEERING.md)** - Resilience testing
- **[API Reference](API_REFERENCE.md#allocation-testing)** - Complete allocation testing API

---

**Built with  by [Digvijay](https://github.com/Digvijay) | Contributions welcome!**

*Prove performance, don't hope for it.*
