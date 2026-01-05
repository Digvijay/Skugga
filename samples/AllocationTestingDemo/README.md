# Zero-Allocation Testing Demo ⚡

**Stop guessing. Prove your code allocates zero bytes.**

## The Problem

```csharp
// Looks fast, but allocates 50MB for 1M calls!
public string GetCacheKey(int id) {
    return $"user:{id}";  // Allocates every time!
}
```

Your "high-performance" API is creating garbage that triggers GC pauses and slows everything down.

## The Solution

```csharp
// Prove it's truly zero-allocation
AssertAllocations.Zero(() => {
    cache.Lookup(key);  // Must not allocate!
});

// Catch regressions immediately
AssertAllocations.AtMost(() => {
    ProcessRequest(data);
}, maxBytes: 1024);  // Fail if > 1KB
```

## Quick Start

```bash
cd samples/AllocationTestingDemo

# See allocation comparisons
dotnet test --logger "console;verbosity=detailed"
```

## What You'll Learn

✅ How to measure allocations precisely  
✅ Common allocation sources (boxing, strings, LINQ)  
✅ Zero-allocation techniques (Span<T>, structs)  
✅ Before/After comparisons showing real impact  

## The Demos

All tests are in `tests/Skugga.Core.Tests/Advanced/AllocationTests.cs`:

1. **String Concat** - 50MB allocated for 1M calls ❌
2. **Span<T>** - Zero allocations ✅
3. **LINQ vs For Loop** - 10x difference 📊
4. **Boxing** - Hidden allocations exposed
5. **Enforcement** - Prevent regressions

## Run It

```bash
dotnet test tests/Skugga.Core.Tests --filter "Allocation" --logger "console;verbosity=detailed"
```

See precise allocation measurements and learn what allocates in your code!

## Real Impact

**Before optimization:**
- 1M requests = 50MB allocated
- GC pauses every few seconds
- Throughput: 100K req/sec

**After optimization:**
- 1M requests = 0 bytes allocated
- No GC pauses
- Throughput: 1M req/sec (10x improvement!)

This is why allocation testing matters in production code! 🚀
