# Skugga Benchmarks

Comprehensive performance benchmarks comparing Skugga against Moq, NSubstitute, and FakeItEasy.

## Running Benchmarks

**Performance Benchmarks (Release mode):**
```bash
dotnet run --project src/Skugga.Benchmarks/Skugga.Benchmarks.csproj -c Release
```

**Feature Demos (Debug mode):**
```bash
dotnet run --project src/Skugga.Benchmarks/Skugga.Benchmarks.csproj
```

## What Gets Run

### Release Mode - Performance Benchmarks
Runs comprehensive performance comparisons:
- **Part 1:** MoqVsSkuggaBenchmarks (12 comprehensive scenarios)
- **Part 2:** FourFrameworkBenchmarks (4 common scenarios across all frameworks)

Results automatically saved to `/benchmarks` directory with timestamps.

### Debug Mode - Feature Showcase
Demonstrates Skugga's unique capabilities:
- **AutoScribe:** Self-writing tests (code generation from real interactions)
- **Chaos Mode:** Resilience testing (fault injection)
- **ZeroAlloc Guard:** Allocation detection (performance enforcement)

## Benchmark Suites

### 1. MoqVsSkuggaBenchmarks - Comprehensive Feature Testing
Tests **12 scenarios** covering all major mocking features:
- Mock creation, Setup with Returns, Multiple setups
- Argument matching (It.IsAny), Verify (Once, Never)
- Callbacks, Property setup, Sequential returns
- Advanced matchers (It.Is), Void methods, Complex scenarios

### 2. FourFrameworkBenchmarks - Common Scenario Comparison
Tests **4 common scenarios** across all frameworks:
- Mock creation
- Simple setup + invoke
- Multiple setups
- Property-like methods

## Benchmark Implementation

Uses manual timing with `Stopwatch` for accurate performance measurement:
- **50,000 iterations** per benchmark
- **5,000 warmup** iterations  
- Manual GC collection between measurements
- Results automatically saved to `/benchmarks` directory with timestamps

**Why Manual Timing?** Skugga uses compile-time source generators, which are incompatible with BenchmarkDotNet's isolated build process. Manual timing provides reliable, reproducible results.

## Latest Results (2 January 2026)

**Hardware:** Intel Core i7-4980HQ @ 2.80GHz, 16GB RAM, macOS 15.7
**Runtime:** .NET 10.0.1

### Comprehensive Moq vs Skugga
**Overall: Skugga is 6.36x faster than Moq**
- Argument Matching: **79.84x faster**
- Void Method Setup: **59.26x faster**
- Callback Execution: **53.34x faster**
- Simple Mock Creation: **15.29x faster**

### All 4 Frameworks Comparison
**Skugga vs Competitors (baseline):**
- Moq: 2.58x slower
- NSubstitute: 3.49x slower
- FakeItEasy: 3.88x slower

See saved benchmark files in `/benchmarks` directory for complete results.

## Debug Mode - Feature Showcase

Running in Debug mode demonstrates Skugga's unique features that set it apart from traditional mocking libraries:

### AutoScribe - Self-Writing Tests ✍️
Records real interactions and generates the mock setup code for you.

### Chaos Mode - Resilience Testing 💥
Inject random faults (exceptions, latency) to test error handling.

### ZeroAlloc Guard - Allocation Detection 📉
Enforce zero-allocation requirements in performance-critical code paths.

```bash
dotnet run --project src/Skugga.Benchmarks/Skugga.Benchmarks.csproj
```

## Detailed Analysis

See [docs/BENCHMARK_COMPARISON.md](../../docs/BENCHMARK_COMPARISON.md) for complete benchmark data and analysis.
