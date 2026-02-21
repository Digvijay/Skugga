# Skugga Troubleshooting Guide

This guide helps you resolve common issues when using Skugga in your projects.

## Table of Contents

- [Source Generator Not Running](#source-generator-not-running)
- [API Usage Errors](#api-usage-errors)
- [Compilation Errors](#compilation-errors)
- [Generic Type Issues](#generic-type-issues)
- [Solution Structure Requirements](#solution-structure-requirements)
- [Diagnostic Codes Reference](#diagnostic-codes-reference)

---

## Source Generator Not Running

### Symptom

You see runtime errors like:
```
InvalidOperationException: [Skugga] Source generator failed to intercept Mock.Create<T>().
Skugga is a COMPILE-TIME mocking library with zero reflection.
```

### Root Cause

The Skugga source generator is not executing during compilation. This is **always** a build configuration issue, never a runtime problem.

### Solution: Check Project Structure

**CRITICAL:** Projects consuming Skugga from NuGet **MUST NOT** be in the same solution as the Skugga source code.

#### Incorrect (Will Fail)
```
MyRepo/
├── Skugga.slnx
├── src/
│   ├── Skugga.Core/
│   └── Skugga.Generator/
└── samples/              ←  Same solution as Skugga
    └── MyApp.Tests/
```

#### Correct (Will Work)
```
MyRepo/
├── Skugga.slnx
├── src/
│   ├── Skugga.Core/
│   └── Skugga.Generator/
└── samples-separate/
    └── samples/          ←  Separate solution
        ├── Skugga.Samples.slnx
        └── MyApp.Tests/
```

**Why?** MSBuild prioritizes `ProjectReference` over `PackageReference` when projects are in the same solution. This prevents the analyzer from being loaded from the NuGet package.

### Solution: Verify NuGet Package Configuration

1. **Check your `.csproj` file:**
```xml
<ItemGroup>
  <PackageReference Include="Skugga" Version="1.1.0" />
  <!-- Use PackageReference for Skugga -->
  
  <!--  Do NOT use ProjectReference to Skugga.Core -->
  <!-- <ProjectReference Include="../../src/Skugga.Core/Skugga.Core.csproj" /> -->
</ItemGroup>
```

2. **Verify NuGet.config points to correct location:**
```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="Local" value="../artifacts/" />  <!-- Adjust path as needed -->
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
</configuration>
```

3. **Clean and rebuild:**
```bash
dotnet clean
dotnet restore --force
dotnet build
```

4. **Verify generator loads (verbose build):**
```bash
dotnet build -v:n | grep -i "Skugga.Generator"
```

You should see output like:
```
Skugga.Generator (netstandard2.0) analyzer at: 
/path/to/packages/skugga/1.1.0/analyzers/dotnet/cs/Skugga.Generator.dll
```

### Solution: Check InterceptorsPreviewNamespaces

The Skugga NuGet package includes a `.targets` file that automatically configures this, but if you're having issues:

```xml
<PropertyGroup>
  <InterceptorsPreviewNamespaces>$(InterceptorsPreviewNamespaces);Skugga.Generated</InterceptorsPreviewNamespaces>
</PropertyGroup>
```

---

## API Usage Errors

### Symptom: "Cannot convert from 'T' to 'Func<Task<T>>'"

```csharp
//  Incorrect
weatherMock.Setup(x => x.GetTemperatureAsync("Seattle")).Returns(-5.0);
// Error: Argument 2: cannot convert from 'double' to 'System.Func<System.Threading.Tasks.Task<double>>'
```

### Solution: Use Task.FromResult for Async Methods

For methods returning `Task<T>`, wrap the return value in `Task.FromResult()`:

```csharp
//  Correct
weatherMock.Setup(x => x.GetTemperatureAsync("Seattle")).Returns(Task.FromResult(-5.0));
weatherMock.Setup(x => x.GetConditionAsync("Seattle")).Returns(Task.FromResult("Sunny"));
```

**For synchronous methods**, use the value directly:
```csharp
//  Correct (synchronous method)
calculator.Setup(x => x.Add(2, 3)).Returns(5);
```

---

### Symptom: "Does not contain a definition for 'Object'"

```csharp
//  Incorrect
var mock = Mock.Create<ICalculator>();
var result = mock.Object.Add(2, 3);  // Error: 'ICalculator' does not contain 'Object'
```

### Solution: Mock IS the Interface

Unlike Moq, Skugga mocks directly implement the interface. No `.Object` property is needed:

```csharp
//  Correct
var mock = Mock.Create<ICalculator>();
var result = mock.Add(2, 3);  // Call methods directly on the mock
```

---

### Symptom: "The non-generic type 'Mock' cannot be used with type arguments"

```csharp
//  Incorrect
private readonly Mock<IRepository> _repositoryMock;

public MyTests()
{
    _repositoryMock = Mock.Create<IRepository>();
    _service = new MyService(_repositoryMock.Object);  // Error
}
```

### Solution: Store Mocks as Interface Type

Skugga doesn't have a `Mock<T>` wrapper type. Store and use mocks as the interface type directly:

```csharp
//  Correct
private readonly IRepository _repositoryMock;

public MyTests()
{
    _repositoryMock = Mock.Create<IRepository>();
    _service = new MyService(_repositoryMock);  // No .Object needed
}
```

---

## Compilation Errors

### Symptom: Namespace Not Found

```csharp
using Skugga;  //  Error: namespace 'Skugga' not found
```

### Solution: Use Skugga.Core Namespace

```csharp
using Skugga.Core;  //  Correct
```

---

### Symptom: "Skugga_InterfaceName does not implement interface member..."

This typically occurs with generic interfaces that have complex type parameters.

**Example Error:**
```
error CS0535: 'Skugga_ILogger_1048383564' does not implement interface member 'ILogger.Log<TState>(...)'
```

**Workaround:** Avoid mocking complex generic interfaces like `ILogger<T>`. Use concrete implementations or simpler abstractions:

```csharp
//  May cause issues
var logger = Mock.Create<ILogger<MyClass>>();

//  Better approach - use a test double
public class TestLogger : ILogger<MyClass>
{
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, 
        Exception? exception, Func<TState, Exception?, string> formatter) { }
    // ... other members
}

//  Or use NullLogger
var logger = NullLogger<MyClass>.Instance;
```

---

## Generic Type Issues

### Symptom: Generator Fails with Complex Generics

Skugga's generator has limitations with:
- Generic methods with multiple type parameters
- Interfaces with method-level generic constraints  
- Generic interfaces with unconstrained type parameters used in generic methods

**Current Limitation:** `ILogger<T>` interface cannot be fully mocked due to its `Log<TState>()` method.

### Workaround

1. **Simplify interfaces:** Create wrapper interfaces without generic methods
2. **Use concrete test doubles:** Implement simple test versions of problematic interfaces
3. **Report issues:** These are known limitations being addressed

---

## Solution Structure Requirements

### For Library Consumers (You)

When consuming Skugga from NuGet in your projects:

 **Do:**
- Use `PackageReference` to reference Skugga
- Keep your test projects in their own solution
- Use separate NuGet.config if referencing local packages

 **Don't:**
- Add your test project to the Skugga repository solution
- Use `ProjectReference` to reference Skugga source code
- Try to mix PackageReference and ProjectReference to Skugga

### For Skugga Development

When working on Skugga itself:

 **Do:**
- Use `ProjectReference` in the main Skugga.slnx solution
- Test core functionality in `Skugga.Core.Tests` (same solution)
- Keep sample projects in a separate solution structure

---

## Quick Diagnostic Checklist

When Skugga isn't working, check these in order:

- [ ] **1. Is the generator loading?**
  ```bash
  dotnet build -v:n | grep "Skugga.Generator"
  ```

- [ ] **2. Are you using PackageReference?**
  ```bash
  grep -r "Skugga" *.csproj
  # Should show: <PackageReference Include="Skugga" Version="1.1.0" />
  ```

- [ ] **3. Is your project in a separate solution from Skugga?**
  ```bash
  # Your project should NOT be in same solution as Skugga source
  ```

- [ ] **4. Are you using correct API?**
  - No `.Object` property
  - `Task.FromResult()` for async returns
  - Store mocks as interface type, not `Mock<T>`

- [ ] **5. Is InterceptorsPreviewNamespaces configured?**
  ```bash
  grep -i "InterceptorsPreviewNamespaces" *.csproj
  # Should be set automatically by Skugga.targets, but verify if needed
  ```

- [ ] **6. Did you clean and rebuild?**
  ```bash
  dotnet clean && dotnet restore --force && dotnet build
  ```

---

## Still Having Issues?

1. **Create a minimal reproduction:**
   - New console/test project
   - Outside any Skugga source directories
   - Simple interface with 1-2 methods
   - Follow the [working example from tests](/Users/digvijay/test-skugga-nuget/SkuggaNuGetTest/)

2. **Check generator output:**
   ```bash
   # Generated interceptors are in:
   ls obj/Debug/net*/Skugga.Generator/Skugga.Generator.SkuggaGenerator/
   ```

3. **Report the issue:**
   Include:
   - Your .csproj file
   - Sample code that fails
   - Generator output (if any)
   - Full build log with `-v:n` verbosity

---

## Common Patterns

### Correct Async Method Mocking

```csharp
// Interface
public interface IWeatherService
{
    Task<double> GetTemperatureAsync(string city);
    Task<string> GetConditionAsync(string city);
}

// Test
var mock = Mock.Create<IWeatherService>();
mock.Setup(x => x.GetTemperatureAsync("Seattle"))
    .Returns(Task.FromResult(-5.0));
mock.Setup(x => x.GetConditionAsync("Seattle"))
    .Returns(Task.FromResult("Snowy"));

var temp = await mock.GetTemperatureAsync("Seattle");
var condition = await mock.GetConditionAsync("Seattle");
```

### Correct Constructor Pattern

```csharp
public class MyTests
{
    private readonly IRepository _repository;
    private readonly IService _service;
    private readonly MyClass _sut;

    public MyTests()
    {
        _repository = Mock.Create<IRepository>();
        _service = Mock.Create<IService>();
        _sut = new MyClass(_repository, _service);
    }
}
```

### Correct Verification

```csharp
// Verify a method was called
mock.Verify(x => x.MethodCall(arg), Times.Once());

// Verify with It.IsAny<T>()
mock.Verify(x => x.Process(It.IsAny<string>()), Times.Once());

// Verify never called
mock.Verify(x => x.DangerousMethod(), Times.Never());
```

---

## Doppelgänger (OpenAPI) Issues

### Symptom: "Could not load OpenAPI spec from source"

```
error SKUGGA_OPENAPI_003: Could not load OpenAPI spec from source: https://api.example.com/spec.json. 
Ensure the file is added to AdditionalFiles.
```

### Root Causes and Solutions

#### 1. URL Not Downloaded (First Build)

**Problem:** On the first build, URL specs don't exist yet because they're downloaded during build.

**Solution:** Build twice when using URLs:
```bash
# First build: Downloads and caches the spec
dotnet build  #  Fails with SKUGGA_OPENAPI_003

# Second build: Uses cached spec
dotnet build  #  Succeeds
```

**Why?** MSBuild evaluation phase (when source generators run) happens BEFORE the execution phase (when download tasks run). The cache is populated during the first build's execution phase, but source generators can't see it until the second build's evaluation phase.

#### 2. MSBuild Targets Import Order

**Problem:** AdditionalFiles not being populated from cached specs even after download.

**Solution:** Import the `.targets` file **at the end** of your `.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  
  <!-- ... other content ... -->
  
  <ItemGroup>
    <AdditionalFiles Include="specs/local.json" />
  </ItemGroup>
  
  <ItemGroup>
    <SkuggaOpenApiUrl Include="https://api.example.com/spec.json" />
  </ItemGroup>

  <!--  Import at the END -->
  <Import Project="path/to/Skugga.OpenApi.Tasks.targets" />
  
</Project>
```

**Why?** ItemGroups in imported `.targets` files are evaluated during MSBuild's evaluation phase. If the Import happens before your `SkuggaOpenApiUrl` items are defined, the targets file can't see them.

#### 3. Cache Directory Missing

**Problem:** Cache exists but source generator can't find files.

**Solution:** Verify cache location and contents:
```bash
# Check if cache exists
ls -lh obj/skugga-openapi-cache/

# Should see .json files with hash names:
# 48d20ee6325eab90.json
# 48d20ee6325eab90.meta
```

If cache is missing, check MSBuild output:
```bash
dotnet build -v:detailed | grep -i "Skugga"
```

You should see:
```
[Skugga] Downloading/validating OpenAPI specifications...
[Skugga] Using 1 cached spec(s) for source generation.
```

#### 4. AdditionalFiles Not Populated

**Problem:** Cache exists but isn't being added to AdditionalFiles.

**Debug:** Check what AdditionalFiles the generator sees:
```bash
# Enable diagnostic logging in SkuggaOpenApiGenerator.cs
dotnet build -v:diag 2>&1 | grep -i "AdditionalFiles"
```

**Common causes:**
- `.targets` Import is too early in `.csproj`
- `SkuggaOpenApiUrl` items not defined before Import
- Glob pattern `obj/skugga-openapi-cache/*.json` not expanding

**Solution:** Ensure this order in your `.csproj`:
1. Define `SkuggaOpenApiUrl` items
2. Import `.targets` file (at end)
3. Rebuild project

#### 5. Schema Name Collisions

**Problem:** Multiple OpenAPI specs define the same schema names (e.g., multiple specs with a `Pet` class).

```
error CS0101: The namespace 'MyNamespace' already contains a definition for 'Pet'
```

**Solution:** Use different specs or apply namespace prefixes (future feature).

### Local File Path Issues

**Problem:** Spec file not found even though it exists.

**Solution:** Check your path syntax:
```csharp
//  Correct - relative to project directory
[SkuggaFromOpenApi("specs/api.json")]

//  Correct - relative with parent directory
[SkuggaFromOpenApi("../shared/api.json")]

//  Correct - absolute path
[SkuggaFromOpenApi("/Users/me/specs/api.json")]

//  Wrong - must match AdditionalFiles path exactly
[SkuggaFromOpenApi("api.json")]  // If AdditionalFiles has "specs/api.json"
```

The path in `[SkuggaFromOpenApi]` must match the `AdditionalFiles` path.

### Cache Configuration

**Default cache location:**
```
$(MSBuildProjectDirectory)/obj/skugga-openapi-cache/
```

**Custom cache location:**
```xml
<PropertyGroup>
  <SkuggaOpenApiCacheDirectory>$(MSBuildProjectDirectory)/custom/cache</SkuggaOpenApiCacheDirectory>
</PropertyGroup>
```

**Cache contents:**
- `*.json` - The downloaded OpenAPI spec
- `*.meta` - Metadata file with URL and ETag

**Clean cache:**
```bash
rm -rf obj/skugga-openapi-cache/
dotnet build  # Re-downloads on next build
```

---

## Diagnostic Codes Reference

Skugga reports errors and warnings using diagnostic codes to help identify and resolve issues quickly.

### Core Skugga Diagnostics

| Code | Severity | Description | Solution |
|------|----------|-------------|----------|
| **SKUGGA001** | Error | Cannot mock sealed class | Sealed classes cannot be mocked. Mock an interface instead or remove the `sealed` keyword. |
| **SKUGGA002** | Warning | Class has no virtual members | Classes must have virtual members to be mocked. Consider mocking an interface or make members `virtual`. |

### OpenAPI Generator Diagnostics

| Code | Severity | Description | Solution |
|------|----------|-------------|----------|
| **SKUGGA_OPENAPI_001** | Error | Unexpected generator error | Internal error during code generation. Check build output for stack trace and report as bug. |
| **SKUGGA_OPENAPI_002** | Error | Empty or invalid source parameter | The `SkuggaFromOpenApi` attribute requires a valid file path or URL. |
| **SKUGGA_OPENAPI_003** | Error | Could not load OpenAPI spec | Ensure file is added to `<AdditionalFiles>` in `.csproj`. For URLs, run build twice (see [OpenAPI URL issues](#doppelgänger-openapi-issues)). |
| **SKUGGA_OPENAPI_004** | Error | OpenAPI spec parse error | The OpenAPI/Swagger file has syntax errors. Validate using [Swagger Editor](https://editor.swagger.io/). |
| **SKUGGA_OPENAPI_005** | Error | Failed to generate code | Error occurred during code generation. Check that schemas and operations are valid. |
| **SKUGGA_OPENAPI_006** | Info | OpenAPI 2.0 (Swagger) detected | The spec is being automatically converted to OpenAPI 3.0. No action needed. |
| **SKUGGA_OPENAPI_007** | Warning | Operation has no success response | Operation has no 2xx or default response. The generated method will return `void` (or `Task` if async). |
| **SKUGGA_OPENAPI_008** | Warning | Document validation issues | OpenAPI document has structural issues (missing paths, null schemas, etc.). Review the diagnostic message for details. |

**Note:** These diagnostics appear during compilation, not at runtime. Skugga is a compile-time library--runtime errors indicate build configuration problems.

---

## Key Takeaways

1. **Skugga is compile-time only** - If it fails at runtime, your build configuration is wrong
2. **Separate solutions required** - Don't mix Skugga source with NuGet consumption
3. **No `.Object` property** - Mocks implement interfaces directly  
4. **Use `Task.FromResult()`** - For async methods returning `Task<T>`
5. **Simple test doubles** - For complex generic interfaces like `ILogger<T>`
6. **URL specs require two builds** - First build downloads, second build uses cache
7. **Import order matters** - Import `.targets` files at the end of your `.csproj`
8. **Check diagnostic codes** - Error and warning codes provide specific guidance (see [Diagnostic Codes Reference](#diagnostic-codes-reference))
