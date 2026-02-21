# Benchmark Overview

Skugga's compile-time architecture delivers measurable performance advantages across all mocking operations.

## Framework Comparison (12 Scenarios)

**50,000 iterations per scenario** | Intel Core i7-4980HQ @ 2.80GHz | .NET 10.0.1

| Framework | Speed vs Skugga | Notes |
|-----------|-----------------|-------|
| **Skugga** | **Baseline** | Compile-time, zero reflection |
| Moq | 2.6-80x slower |  80x on argument matching |
| NSubstitute | 3.5x slower | Consistent overhead |
| FakeItEasy | 3.9x slower | Similar pattern |

## Scenario Breakdown (Skugga vs Moq)

| Scenario | Speedup | Impact |
|----------|---------|--------|
| **Argument Matching** | 79.84x  | Largest gap due to expression tree overhead |
| **Void Method Setup** | 59.26x | Proxy creation vs static dispatch |
| **Callback Execution** | 53.34x | Delegate wrapping overhead |
| **Mock Creation** | 15.29x | Constructor vs Reflection.Emit |
| **Simple Setup/Returns** | 2.58x | Baseline overhead |

## Real-World Impact

For a test suite with **10,000 tests** using argument matchers:

| Framework | Time | Savings |
|-----------|------|---------|
| Skugga | **2.7s** | -- |
| Moq | 218s | **215s saved** |
| NSubstitute | 9.5s | 6.8s saved |
| FakeItEasy | 10.5s | 7.8s saved |

## Why Skugga is Faster

| Aspect | Legacy (Moq) | Skugga |
|--------|-------------|--------|
| **Proxy Generation** | `Reflection.Emit` at runtime | Roslyn at build time |
| **Method Dispatch** | `MethodInfo` + expression eval | Dictionary lookup |
| **Argument Matching** | `Expression.Lambda().Compile()` | Direct predicate call |
| **Memory** | ~4,150 bytes/mock | ~1,110 bytes/mock |

## Reproducing Results

```bash
dotnet run --project src/Skugga.Benchmarks/Skugga.Benchmarks.csproj -c Release
```

Results are saved to `/benchmarks/MoqVsSkugga.md` and `/benchmarks/FourFramework.md`.
