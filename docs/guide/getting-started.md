# Getting Started

## What is Skugga?

**Skugga** (Swedish for *Shadow*) is a mocking library engineered specifically for **Native AOT** and Cloud-Native .NET. It moves mocking logic from runtime reflection to **compile-time code generation**, making it 100% AOT-compatible with zero overhead.

Legacy tools like Moq rely on `System.Reflection.Emit` to generate proxy objects at runtime. Since Native AOT strips away the JIT compiler, these tools crash instantly. **Skugga eliminates this trade-off** by generating mock implementations during the build process.

## Installation

### 1. Install Skugga

```bash
dotnet add package Skugga
```

::: info Requirements
- .NET 8.0 or later
- C# 12 enabled (LangVersion `12` or `latest`)
:::

### 2. Enable Interceptors

Add the following to your test project's `.csproj`:

```xml
<PropertyGroup>
   <InterceptorsPreviewNamespaces>$(InterceptorsPreviewNamespaces);Skugga</InterceptorsPreviewNamespaces>
</PropertyGroup>
```

## Writing Your First Test

```csharp
using Skugga.Core;

public interface IEmailService
{
   string GetEmailAddress(int userId);
   string TenantName { get; }
}

public class MyFirstTest
{
   public void Run()
   {
       // 1. Create the mock (intercepted at compile time)
       var mock = Mock.Create<IEmailService>();

       // 2. Configure behavior
       mock.Setup(x => x.GetEmailAddress(1)).Returns("digvijay@digvijay.dev");
       mock.Setup(x => x.TenantName).Returns("Contoso");

       // 3. Execute
       var email = mock.GetEmailAddress(1); // Returns "digvijay@digvijay.dev"
       var tenant = mock.TenantName;        // Returns "Contoso"
   }
}
```

## How It Works

Skugga leverages **C# 12 Interceptors** to seamlessly rewire your code during compilation:

1. **Scan** -- The Source Generator detects calls to `Mock.Create<T>()`.
2. **Generate** -- It writes a concrete, optimized C# class (`Skugga_T`) that implements `T`.
3. **Intercept** -- The compiler replaces your `Mock.Create` call with `new Skugga_T()`.

> **Zero Friction:** To the developer, it looks like a normal method call. To the runtime, it looks like hand-written, optimized code.

## Next Steps

- [Mock Creation](/guide/mock-creation) -- Explore all ways to create mocks
- [Setup & Returns](/guide/setup-returns) -- Configure mock behavior
- [Verification](/guide/verification) -- Assert method calls happened
- [Migrating from Moq](/guide/migration) -- Seamless migration guide
- [Architecture](/concepts/architecture) -- Deep dive into how Skugga works
