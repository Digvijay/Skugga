# Skugga vs Others

A comprehensive comparison of Skugga against other .NET mocking frameworks.

## At a Glance

| Feature | Skugga | Moq | NSubstitute | FakeItEasy |
|---------|--------|-----|-------------|------------|
| **Native AOT** | Yes | No | No | No |
| **Zero Reflection** | Yes | No | No | No |
| **Compile-Time** | Yes | No | No | No |
| **Familiar API** | Yes (Moq-compat) | N/A | Different | Different |
| **Chaos Engineering** | Yes | No | No | No |
| **OpenAPI Mocking** | Yes | No | No | No |
| **Zero-Alloc Testing** | Yes | No | No | No |
| **Self-Writing Tests** | Yes | No | No | No |

## Performance

**Comprehensive benchmarks across 12 scenarios** (50,000 iterations each):

| Framework | Speed vs Skugga | Notes |
|-----------|-----------------|-------|
| **Skugga** | **Baseline** | Compile-time, zero reflection |
| Moq | 2.6-80x slower | 80x slower on argument matching |
| NSubstitute | 3.5x slower | Consistent but reflection-heavy |
| FakeItEasy | 3.9x slower | Similar overhead across scenarios |

### Key Findings

- **Overall**: Skugga is **6.36x faster** than Moq
- **Argument Matching**: **79.84x faster**
- **Void Method Setup**: **59.26x faster**
- **Callback Execution**: **53.34x faster**
- **Mock Creation**: **15.29x faster**

### Real-World Impact

For a test suite with 10,000 tests using argument matchers:
- **Skugga**: 2.7 seconds
- **Moq**: 218 seconds
- **Saved**: 215 seconds per test run

## Why is Skugga Faster?

Legacy libraries use `System.Reflection.Emit` to generate proxy classes at **runtime**. This incurs heavy CPU penalties and forces the JIT compiler to work overtime.

Skugga does all heavy lifting at **compile-time**:
- **Zero JIT Penalties** -- Code is already compiled to native machine code
- **Zero Reflection** -- No `Expression.Lambda().Compile()` or `MethodInfo` lookups
- **Zero Dynamic Allocation** -- No `Castle.DynamicProxy` or similar
- **Optimized Dispatch** -- Simple dictionary lookups instead of reflection
