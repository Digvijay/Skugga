# Architecture -- How Skugga Works

Skugga is a **compile-time mocking framework** that eliminates reflection by generating mock implementations during the build process.

## The Three-Phase Pipeline

### 1. Scan

The **Roslyn Source Generator** scans your code during compilation and detects calls to `Mock.Create<T>()`.

### 2. Generate

For each detected mock target, the generator writes a concrete C# class (`Skugga_T`) that fully implements the interface or abstract class. This class contains:
- All method implementations with setup/verify infrastructure
- Property backing stores
- Event registration logic
- Argument matcher evaluation

### 3. Intercept

Using **C# 12 Interceptors**, the compiler physically replaces your `Mock.Create<T>()` call with `new Skugga_T()` in the final binary. The interceptor attribute points to the exact file/line/column of the original call.

```
Your Code:     var mock = Mock.Create<IService>();
             ↓ (intercepted at compile time)
Final Binary:  var mock = new Skugga_IService();
```

## Why This Matters

### Traditional Approach (Moq, NSubstitute)

```
Test Code -> Runtime -> Reflection.Emit -> Dynamic Proxy -> JIT Compile -> Execute
```

-  Requires JIT compiler (incompatible with AOT)
-  Runtime CPU overhead for proxy generation
-  Dynamic memory allocation for assemblies

### Skugga Approach

```
Test Code -> Roslyn -> Static Shadow Class -> Native Machine Code -> Execute
```

-  No JIT required (works with Native AOT)
-  Zero runtime overhead (pre-compiled)
-  No dynamic allocation (static code)

## Technical Details

### Incremental Generation

Skugga uses the **IIncrementalGenerator** pipeline for efficient builds:
- < 1ms for cache hits
- ~50-200ms for cache misses
- 70% memory reduction on first builds
- 90% memory reduction on incremental builds

### Parallel Processing

The generator automatically processes mock targets in parallel:
- Linear scaling with CPU cores
- Thread-safe by design

### Stress Test Results

- **500 mock classes**: 0.41 seconds build time
- **500 test executions**: 0.49 seconds (~1ms per test)
