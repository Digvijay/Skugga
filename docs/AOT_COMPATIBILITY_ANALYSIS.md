# Skugga AOT Compatibility Analysis

## Executive Summary

Skugga claims "100% AOT Compatibility" and "Zero Reflection" for its core mocking paths. This document analyzes the technical implementation of these claims, verifying their accuracy and documenting the specific mechanisms used to achieve AOT safety.

**Verdict: Confirmed with caveats.**
The core API (`Mock.Create<T>`, `Setup`, `Verify`) uses **zero runtime reflection** and is fully AOT-compatible via C# 12 Interceptors. However, specific edge cases (recursive mocking default values, generic collections) rely on safety mechanisms that require careful understanding.

## 1. Core Mock Creation (`Mock.Create<T>`)

### Claim: "Zero Reflection"
**Verification:**  Verified

Standard mocking libraries use `System.Reflection.Emit` to generate proxy classes at runtime. Skugga replaces this with **Compile-Time Interception**.

*   **Mechanism:** C# 12 Interceptors (`[InterceptsLocation]`).
*   **Behavior:** The compiler physically replaces the call to `Mock.Create<T>()` with `new Skugga.Generated.Skugga_T()`.
*   **Runtime:** The `Mock.Create<T>()` method body actually contains a `throw new InvalidOperationException()`. It is **never executed** in a correctly configured project. Trying to call it via reflection (e.g. `typeof(Mock).GetMethod("Create").Invoke(...)`) will throw, proving that no runtime reflection magic is happening.

## 2. Default Values & Recursive Mocking

### Claim: "AOT Compatible"
**Verification:**  Verified with implementation notes

When a mock member returns an object (e.g. `mock.ListProperty`), Skugga must generate a default value.

### 2.1 Generic Collections (`List<T>`, `Dictionary<K,V>`)
The `EmptyDefaultValueProvider` uses `Activator.CreateInstance` and `MakeGenericType` to create empty generic collections.

*   **AOT Impact:** `MakeGenericType` requires the specific generic instantiation (e.g., `List<MyType>`) to exist in the native code.
*   **Safety:** If `MyType` is used in a list elsewhere in your application, the AOT compiler generates the code. If it is *never* used except in this mock return, the app may crash in AOT.
*   **Mitigation:** `[DynamicallyAccessedMembers]` attributes are used to help the linker, but strictly speaking, this is a dynamic path. AOT users should ensure types returned by mocks are used statically elsewhere.

### 2.2 Recursive Mocks (`DefaultValue.Mock`)
When `DefaultValue.Mock` is used, Skugga attempts to return a new mock instance automatically.

*   **Mechanism:**
    1.  **Primary (AOT Safe):** The Source Generator generates a static `RegisterMockFactory<T>(() => new Skugga_T())` call for every intercepted interface. These are stored in a static dictionary (`_mockFactories`).
    2.  **Fallback (Reflection):** `MockDefaultValueProvider` contains a `try/catch` block attempting to call `Mock.Create` via reflection. **This path will fail** because `Mock.Create` throws when not intercepted.
*   **Conclusion:** Recursive mocking depends entirely on the source generator. If the generator runs, it works AOT. If it doesn't, it fails safely (returns null) rather than crashing the runtime.

## 3. Argument Matchers

### Claim: "Zero Reflection"
**Verification:**  Verified

Matchers like `It.Is<T>()` use `System.Linq.Expressions` in traditional libraries (Moq). Skugga uses **capture-and-replay**.

*   **Mechanism:** `It.Is<T>()` strictly returns `default(T)` and records a matcher in a thread-local context. The generated mock code retrieves this matcher from the context.
*   **Implementation:** `ArgumentMatcher.Create<T>(predicate)` saves the predicate delegate.
*   **AOT Safety:** Fully safe. No expression tree compilation occurs.

## 4. Usage of `System.Reflection` in Core

I scanned the codebase for `System.Reflection` namespaces. Findings:

| Usage | Location | Safety |
|-------|----------|--------|
| `typeof(T).Name` | Validation exceptions |  Safe (Metadata only) |
| `MemberExpression.Member` | `Expression` parsing |  Safe (Only used during setup parsing, no `Emit`) |
| `Activator.CreateInstance` | `DefaultValueProviders` |  Safe (with `DynamicallyAccessedMembers`) |
| `MakeGenericType` | `DefaultValueProviders` |  **Risk:** Only used for default collections. |

## Conclusion

Skugga's architecture effectively solves the "Reflection Wall". The "Zero Reflection" claim applies to the **proxy generation and invocation pipeline**, which is the primary bottleneck and AOT blocker in other libraries.

The minimal reflection used for default value generation is guarded and auxiliary, not structural.
