# Azure Functions with Skugga - Non-Invasive Architecture Demo

This sample demonstrates **the correct, non-invasive architecture** for using Skugga with Azure Functions.

## 🎯 Key Architecture Principle

**Production code has ZERO knowledge of Skugga!**

```
┌─────────────────────────┐
│  src/OrdersApi.csproj   │  ← NO Skugga references!
│  (Production Code)      │     Only Azure Functions packages
└─────────────────────────┘
            ↑
            │ Project Reference
            │
┌─────────────────────────┐
│ tests/OrdersApi.Tests   │  ← Skugga ONLY here!
│  (Test Code)            │     • Skugga.Core
└─────────────────────────┘     • Skugga.Generator
```

## ✅ What Makes This Non-Invasive?

1. **Production Project (`src/OrdersApi.csproj`)**:
   - Zero Skugga dependencies
   - Only production Azure Functions packages
   - Defines services (IOrderService, ICustomerService, etc.)
   - Ready for deployment without test dependencies

2. **Test Project (`tests/OrdersApi.Tests.csproj`)**:
   - References the production project
   - References Skugga.Core and Skugga.Generator
   - Generates mocks at compile-time via Source Generator
   - Uses C# 12 Interceptors for seamless `Mock.Create<T>()` syntax

3. **How It Works**:
   - When you compile the **test project**, Skugga's generator activates
   - Generator scans test code for `Mock.Create<IOrderService>()`
   - Resolves `IOrderService` from the referenced production assembly
   - Generates mock implementation inside test compilation
   - Production code never sees or depends on Skugga!

## 🚀 Running the Sample

### Build and Test
```bash
cd tests
dotnet test
```

### Key Test Patterns

```csharp
// Mocking Azure Functions types - works seamlessly!
var mockRequest = Mock.Create<HttpRequestData>();
var mockResponse = Mock.Create<HttpResponseData>();

// Mocking your services - no attributes needed in production code!
var mockOrderService = Mock.Create<IOrderService>();
mockOrderService.Setup(x => x.GetOrderByIdAsync("123"))
    .ReturnsAsync(new Order());
```

## 📦 Production Deployment

When you deploy `src/OrdersApi`, **no Skugga assemblies are included**. The published output contains:
- ✅ Your Azure Functions code
- ✅ Azure Functions runtime
- ✅ Your dependencies
- ❌ NO test frameworks
- ❌ NO Skugga libraries

This is exactly what you want for production!

## 🆚 Comparison with Moq

| Feature | Moq | Skugga |
|---------|-----|--------|
| Non-invasive (test-only) | ✅ | ✅ |
| Native AOT compatible | ❌ | ✅ |
| Compile-time generation | ❌ | ✅ |
| No reflection at runtime | ❌ | ✅ |
| Azure Functions support | ✅ | ✅ |

## 🏗️ Architecture Benefits

1. **Zero Production Pollution**: Your production binaries are clean
2. **AOT Ready**: Works with Native AOT compilation
3. **Type Safe**: All mocks generated at compile-time
4. **No Runtime Overhead**: No reflection, no proxies, pure generated code
5. **Standard Pattern**: Same approach used by modern .NET tools
