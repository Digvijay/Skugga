# The Reflection Wall

## The Core Problem

For 15 years, the .NET ecosystem's testing tools have relied on **runtime reflection** (`System.Reflection.Emit`) to generate mock objects dynamically. This creates a fundamental incompatibility with modern compute paradigms.

## What is the Reflection Wall?

The **Reflection Wall** is the barrier that prevents organizations from adopting **Native AOT** (Ahead-of-Time compilation) while maintaining testable code.

### How it Works

1. **Legacy mocking** (Moq, NSubstitute, FakeItEasy) uses `System.Reflection.Emit` to generate proxy classes at **runtime**
2. These proxies require a **JIT compiler** to compile IL (Intermediate Language) into machine code on the fly
3. **Native AOT** compiles everything to machine code **before** execution — there is no JIT at runtime
4. When legacy mocks try to emit IL in an AOT binary -- **crash**

### The Impossible Choice

CTOs are forced to choose:

| Option | Benefit | Cost |
|--------|---------|------|
| **Adopt Native AOT** | Lower cloud costs, faster startup | Lose testability (mocking breaks) |
| **Keep JIT** | Maintain test infrastructure | Higher cloud costs, slower startup |

## Skugga's Solution

Skugga breaks through the Reflection Wall by moving mocking from **runtime** to **compile-time**:

- **No `Reflection.Emit`** -- Mocks are Roslyn-generated C# classes
- **No JIT required** -- Generated code compiles directly to native machine code
- **No dynamic proxies** -- Standard static classes with interface implementations

The result: organizations can adopt **both** Native AOT **and** comprehensive testing.

## Impact

| Metric | With Reflection Wall | Without (Skugga) |
|--------|---------------------|-------------------|
| **AOT Compatible** | No | Yes |
| **Cold Start** | ~476ms | **72ms** (6.6x faster) |
| **Container Size** | ~200 MB | **47 MB** (4x smaller) |
| **CPU Efficiency** | Baseline | **5.1x better** |

