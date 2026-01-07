# Doppelgänger: OpenAPI Mock Generation

## Overview

**Doppelgänger** brings **Consumer-Driven Contracts to the unit testing level**. Instead of mocking blindly, you mock against the spec - ensuring your tests always reflect the real API contract.

## The Pain Point: "Contract Drift"

**Scenario:** Your team integrates with a payment API.

```csharp
// You manually mock the interface
public interface IPaymentGateway
{
    Invoice GetInvoice(string id);
}

var mock = new Mock<IPaymentGateway>();
mock.Setup(x => x.GetInvoice(It.IsAny<string>()))
    .Returns(new Invoice { Id = "fake", Amount = 100 });
```

**Meanwhile...** The platform team updates the Payment API OpenAPI spec:
- Renames `GetInvoice` → `RetrieveInvoice`
- Changes `Amount` from `int` to `decimal`
- Adds required `Currency` field

**Result:**
- ✅ **Your tests pass** (mock is outdated, doesn't match API)
- ❌ **Production crashes** (real API has changed)

This is **contract drift** - your mocks lie to you.

## The Problem with Manual Mocks

```csharp
// Manual mock - easy to get out of sync with real API
public interface IStripeClient
{
    Invoice GetInvoice(string id);  // What if Stripe changed this?
}

var mock = new Mock<IStripeClient>();
mock.Setup(x => x.GetInvoice(It.IsAny<string>()))
    .Returns(new Invoice { Id = "fake" }); // What if Invoice schema changed?
```

**Problems:**
- ❌ Manual updates when API changes
- ❌ No compile-time validation
- ❌ **Tests pass, production crashes**
- ❌ You mock blindly, not against the spec

## The Skugga Solution: Never Mock Blindly

Doppelgänger generates mocks **FROM** the OpenAPI specification at build time:

```csharp
using Skugga.Core;

// "God Mode" Attribute - auto-generates interface + mock
[SkuggaFromOpenApi("https://api.stripe.com/v1/swagger.json")]
public partial interface IStripeClient { }

// In your test:
var mock = Mock.Create<IStripeClient>();
var invoice = mock.GetInvoice("inv_123"); 
// Returns realistic Invoice populated with dummy data from spec examples
Assert.NotNull(invoice.Id);
Assert.Equal("USD", invoice.Currency);
```

**What happens when the API changes?**

❌ **Old Way (Manual Mocks):**
1. API team updates OpenAPI spec
2. Your manual mock stays outdated
3. Tests pass ✅ (lying to you)
4. Production crashes ❌ (real API changed)

✅ **Skugga Way (Contract-First):**
1. API team updates OpenAPI spec
2. Your build fails ❌ (missing method, wrong types)
3. You fix the code before deploying
4. Production works ✅ (always in sync)

**Consumer-Driven Contracts at Unit Test Level:**
- ✅ Never mock blindly - always mock against the spec
- ✅ Interface auto-generated from spec
- ✅ Realistic defaults from spec examples
- ✅ **Contract drift = build failure, not production crash**
- ✅ Zero manual maintenance
- ✅ Works with URLs or local files
- ✅ Smart caching with offline support

## Usage

### Basic Usage

```csharp
[SkuggaFromOpenApi("path/to/spec.json")]
public partial interface IMyApi { }
```

### URL Sources

```csharp
[SkuggaFromOpenApi("https://api.example.com/openapi.json")]
public partial interface IExternalApi { }
```

Specs from URLs are:
- Downloaded at build time
- Cached in `obj/skugga-openapi-cache/`
- Validated with ETags to avoid unnecessary downloads
- Available offline from cache

### Local Paths

```csharp
// Relative to project directory
[SkuggaFromOpenApi("../specs/api.json")]

// Absolute path
[SkuggaFromOpenApi("/Users/me/specs/api.json")]
```

### Advanced Options

```csharp
[SkuggaFromOpenApi("api.json",
    GenerateAsync = false,                  // Generate sync methods (default: true)
    OperationFilter = "payments,invoices",  // Only these operations
    UseExampleSet = "success-case",         // Which example to use
    ValidateSchemas = true,                 // Schema validation
    CachePath = "custom/cache/path")]       // Custom cache location
```

#### Async vs Sync Generation

Control whether generated methods use async/await patterns:

```csharp
// Async (default) - generates Task<T> return types
[SkuggaFromOpenApi("api.json")]
public partial interface IModernApi { }
// Generates:
// Task<User[]> GetUsers();
// Task<User> GetUser(int id);
// Task CreateUser(User user);

// Sync - generates direct return types
[SkuggaFromOpenApi("api.json", GenerateAsync = false)]
public partial interface ILegacyApi { }
// Generates:
// User[] GetUsers();
// User GetUser(int id);
// void CreateUser(User user);
```

**When to use sync mode:**
- Legacy APIs that don't support async
- Performance-critical paths where Task allocation matters
- Wrapping synchronous operations that shouldn't appear async
- Testing code that uses synchronous patterns

**Note:** OpenAPI specs don't indicate sync vs async - it's an implementation detail you control.

## Project Setup

### 1. Add the generator

```xml
<ItemGroup>
  <ProjectReference Include="path/to/Skugga.OpenApi.Generator/Skugga.OpenApi.Generator.csproj" 
                    OutputItemType="Analyzer" 
                    ReferenceOutputAssembly="false" />
</ItemGroup>
```

### 2. Add your OpenAPI spec to AdditionalFiles

#### Local Files

For local OpenAPI spec files, add them to `AdditionalFiles`:

```xml
<ItemGroup>
  <AdditionalFiles Include="specs/api.json" />
  <AdditionalFiles Include="../shared/petstore.json" />
</ItemGroup>
```

#### Remote URLs

For OpenAPI specs hosted at URLs, use the `SkuggaOpenApiUrl` item:

```xml
<!-- Import the MSBuild targets for URL downloading -->
<PropertyGroup>
  <SkuggaOpenApiTasksAssembly>$(MSBuildThisFileDirectory)path/to/Skugga.OpenApi.Tasks.dll</SkuggaOpenApiTasksAssembly>
</PropertyGroup>
<Import Project="path/to/Skugga.OpenApi.Tasks.targets" />

<!-- Add URL specs -->
<ItemGroup>
  <SkuggaOpenApiUrl Include="https://api.stripe.com/v1/openapi.json" />
  <SkuggaOpenApiUrl Include="https://petstore3.swagger.io/api/v3/openapi.json" />
</ItemGroup>
```

**How it works:**
- First build: Spec is downloaded and cached in `obj/skugga-openapi-cache/`
- Second build: Spec is loaded from cache and used for generation
- Cache uses SHA256 hash filenames (e.g., `48d20ee6325eab90.json`)
- ETag validation: Only re-downloads if the remote spec changes
- Offline support: Uses cached spec if network unavailable

**Important:** Import the `.targets` file **at the end** of your `.csproj` file (after other `ItemGroup` definitions) to ensure proper MSBuild evaluation order.

### 3. Mark your interface

```csharp
[SkuggaFromOpenApi("specs/api.json")]
public partial interface IMyApi { }
```

### 4. Build

The generator runs automatically during compilation and creates:
- Interface definition from OpenAPI operations
- Mock implementation with realistic defaults
- Extension methods for setup and verification

**For URL-based specs:** Remember that the first build downloads the spec, and the second build uses it for generation:

```bash
# First build: Downloads and caches spec (generator can't see it yet)
dotnet build
# May show: error SKUGGA_OPENAPI_003: Could not load OpenAPI spec

# Second build: Uses cached spec for generation
dotnet build
# ✅ Succeeds - interface and mock generated
```

## Complete Example: URL-Based Spec

Here's a complete working example using a remote OpenAPI spec:

**MyApi.Tests.csproj:**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Skugga" Version="1.3.0" />
    <PackageReference Include="xunit" Version="2.9.3" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="path/to/Skugga.OpenApi.Generator.csproj" 
                      OutputItemType="Analyzer" 
                      ReferenceOutputAssembly="false" />
  </ItemGroup>

  <!-- Define URL specs first -->
  <ItemGroup>
    <SkuggaOpenApiUrl Include="https://petstore3.swagger.io/api/v3/openapi.json" />
  </ItemGroup>

  <!-- Import targets at the end -->
  <PropertyGroup>
    <SkuggaOpenApiTasksAssembly>path/to/Skugga.OpenApi.Tasks.dll</SkuggaOpenApiTasksAssembly>
  </PropertyGroup>
  <Import Project="path/to/Skugga.OpenApi.Tasks.targets" />
  
</Project>
```

**PetStoreTests.cs:**
```csharp
using Skugga.Core;
using Xunit;

namespace MyApi.Tests
{
    // Mark interface with URL from SkuggaOpenApiUrl
    [SkuggaFromOpenApi("https://petstore3.swagger.io/api/v3/openapi.json")]
    public partial interface IPetStoreApi { }

    public class PetStoreTests
    {
        [Fact]
        public void Can_Get_Pet_By_Id()
        {
            // Arrange
            var api = Mock.Create<IPetStoreApi>();
            
            // Act
            var pet = api.GetPetById(123);
            
            // Assert - realistic data from OpenAPI examples
            Assert.NotNull(pet);
            Assert.NotNull(pet.Name);
        }

        [Fact]
        public void Can_Add_New_Pet()
        {
            // Arrange
            var api = Mock.Create<IPetStoreApi>();
            var newPet = new Pet { Name = "Fluffy", Status = "available" };
            
            // Act
            var result = api.AddPet(newPet);
            
            // Assert
            Assert.NotNull(result);
        }
    }
}
```

**Build and run:**
```bash
# First build (downloads spec)
dotnet build
# [Skugga] Downloading/validating OpenAPI specifications...

# Second build (generates code)
dotnet build
# ✅ Build succeeded

# Run tests
dotnet test
# ✅ Tests pass with generated mock
```

**Cache location:**
```
obj/skugga-openapi-cache/
├── 48d20ee6325eab90.json    # Downloaded spec (SHA256 hash filename)
└── 48d20ee6325eab90.meta    # Metadata (URL, ETag, timestamp)
```

## Implementation Status

### ✅ Phase 1: MVP (v1.2.0)

**Completed:**
- `[SkuggaFromOpenApi]` attribute with all options
- Basic generator infrastructure
- OpenApiSpecLoader with AdditionalFiles support
- URL and file path specification
- MSBuild integration scaffolding
- Test infrastructure

**Status:** ✅ Complete

### ✅ Phase 2: OpenAPI Parsing (v1.2.0)

**Completed:**
- Microsoft.OpenApi.Readers integration
- Interface generation from operations
- Mock class generation
- Example-based default values
- Complex type handling ($ref, allOf, oneOf)
- Response model generation
- Schema classes with proper type mapping
- Path and query parameter handling

**Status:** ✅ Complete

### ✅ Phase 3: Mock Implementation (v1.2.0)

**Completed:**
- Realistic mock data generation from OpenAPI examples
- ExampleGenerator for all schema types
- Complex object initialization (arrays, objects, primitives)
- Nullable type handling
- Request body parameter support
- Multiple response status codes (200, 201, 202, 204)
- Task.FromResult() for async operations

**Status:** ✅ Complete

### ✅ Phase 4: Production Ready (v1.2.0)

**Completed:**
- Async/sync generation control (GenerateAsync property)
- Advanced schema features (discriminators, allOf, oneOf, anyOf)
- Polymorphic type support
- URL downloading with caching
- AdditionalFiles integration
- Comprehensive error diagnostics
- Full OpenAPI 3.0 support

**Status:** ✅ Complete

### 📋 Future Enhancements (v1.3.0+)

**Potential additions:**
- OpenAPI 2.0 (Swagger) support
- YAML format support
- Multiple example set selection
- Operation filtering by tags
- Schema validation strictness levels
- Custom type mapping configuration
- ETag-based cache invalidation

## Architecture

```
[SkuggaFromOpenApi("source")]
           ↓
   OpenApiSyntaxReceiver
   (finds attributes)
           ↓
    OpenApiSpecLoader
   (loads from AdditionalFiles)
           ↓
  Microsoft.OpenApi.Readers
   (parses spec)
           ↓
   Interface Generator
   (creates interface)
           ↓
     Mock Generator
   (creates mock with defaults)
           ↓
  Interceptor Generator
  (redirects Mock.Create)
```

## Examples

### Example 1: Pet Store API

```csharp
[SkuggaFromOpenApi("https://petstore3.swagger.io/api/v3/openapi.json")]
public partial interface IPetStoreApi { }

// In tests:
var mock = Mock.Create<IPetStoreApi>();

// Setup like normal Skugga mocks
mock.Setup(x => x.GetPet(123))
    .Returns(new Pet { Id = 123, Name = "Fluffy" });

// Verify
mock.Verify(x => x.GetPet(123), Times.Once);
```

### Example 2: Multiple APIs

```csharp
[SkuggaFromOpenApi("specs/stripe.json")]
public partial interface IStripeClient { }

[SkuggaFromOpenApi("specs/twilio.json")]
public partial interface ITwilioClient { }

[SkuggaFromOpenApi("specs/sendgrid.json")]
public partial interface ISendGridClient { }

// All mocks stay in sync with their respective APIs
```

### Example 3: Filtered Operations

```csharp
// Only generate payment-related operations
[SkuggaFromOpenApi("stripe.json", OperationFilter = "payments")]
public partial interface IStripePayments { }

// Only generate subscription operations
[SkuggaFromOpenApi("stripe.json", OperationFilter = "subscriptions")]
public partial interface IStripeSubscriptions { }
```

## Cache Management

Generated files and downloaded specs are cached for fast incremental builds:

```bash
# Cache location
obj/skugga-openapi-cache/
├── abc123hash.json      # Cached OpenAPI spec
├── abc123hash.meta      # Metadata (ETag, URL, timestamp)
└── def456hash.json

# Clean cache
dotnet clean  # Automatically removes cache

# Or manually
rm -rf obj/skugga-openapi-cache/
```

## Diagnostics

| Code | Severity | Description |
|------|----------|-------------|
| SKUGGA_OPENAPI_001 | Error | Unexpected generator error |
| SKUGGA_OPENAPI_002 | Error | Empty or invalid source parameter |
| SKUGGA_OPENAPI_003 | Warning | Using stale cached spec (offline) |
| SKUGGA_OPENAPI_004 | Error | OpenAPI spec parse error |

## Known Limitations & Workarounds

### Mock Data for Composite Schemas (allOf)

**Issue:** The ExampleGenerator doesn't currently generate example data for schemas using `allOf` composition. Mock methods returning these types will return `null`.

**Example:**
```csharp
// OpenAPI spec:
{
  "Product": {
    "allOf": [
      { "$ref": "#/components/schemas/NewProduct" },
      { 
        "type": "object",
        "properties": {
          "id": { "type": "integer" }
        }
      }
    ]
  }
}

// Generated mock:
var mock = new IProductApiMock();
var product = await mock.UpdateProduct(123, request);
// product is null - ExampleGenerator doesn't handle allOf
```

**Workaround:**
Use Skugga's `.Setup()` to provide custom return values:

```csharp
var mock = Mock.Create<IProductApi>();
mock.Setup(m => m.UpdateProduct(It.IsAny<long>(), It.IsAny<Product>()))
    .Returns(Task.FromResult(new Product 
    { 
        Id = 123, 
        Name = "Widget", 
        Category = "tools" 
    }));

var result = await mock.UpdateProduct(123, request);
Assert.NotNull(result); // Now works
```

**Root Cause:**
The `ExampleGenerator.GenerateDefaultValue()` method returns `null` for schemas with `allOf` because it requires merging example values from multiple schemas. This is a known limitation that will be addressed in a future release.

**Impact:**
- Interface generation ✅ Works correctly
- Type generation ✅ Works correctly (properties are properly merged)
- Mock return values ❌ Returns null for allOf schemas
- Status code handling ✅ Works correctly

**Planned Fix:**
Enhance `ExampleGenerator` to:
1. Recursively collect example values from all `allOf` schemas
2. Merge properties from base schemas with overrides
3. Generate composite example objects

### URL Spec Download Timing

**Issue:** Source generators run in the compiler process and cannot perform network I/O. URLs in `[SkuggaFromOpenApi("https://...")]` are downloaded by an MSBuild task that runs **before** compilation.

**Behavior:**
```csharp
// This triggers MSBuild download task
[SkuggaFromOpenApi("https://api.stripe.com/v1/swagger.json")]
public partial interface IStripeClient { }

// Flow:
// 1. MSBuild task downloads URL to obj/skugga-openapi-cache/
// 2. MSBuild adds cached file to AdditionalFiles
// 3. Source generator reads from AdditionalFiles
```

**Cache Behavior:**
- First build: Downloads spec, caches with ETag
- Incremental builds: Revalidates with server using ETag
- Offline builds: Uses last cached version (warning SKUGGA_OPENAPI_003)
- Clean build: Re-downloads everything

**Important:** The URL must be accessible at build time. CI/CD pipelines must have network access or pre-cache specs.

## OpenAPI Quality Linting

Doppelgänger includes **Spectral-inspired linting rules** to enforce OpenAPI quality and best practices at build time. The linting system reports diagnostics for common issues that can lead to poor API documentation or inconsistent contracts.

### Linting Rules

**Info Section Rules:**
- `info-contact` (SKUGGA_LINT_001) - Ensures API contact information is provided
- `info-description` (SKUGGA_LINT_002) - Ensures API description exists
- `info-license` (SKUGGA_LINT_003) - Ensures license information is specified

**Operation Rules:**
- `operation-operationId` (SKUGGA_LINT_004) - Ensures all operations have unique IDs
- `operation-tags` (SKUGGA_LINT_005) - Ensures operations are tagged for organization
- `operation-description` (SKUGGA_LINT_006) - Ensures operations have descriptions
- `operation-summary` (SKUGGA_LINT_007) - Ensures operations have summaries
- `operation-success-response` (SKUGGA_LINT_008/009) - Ensures 2xx responses are defined
- `operation-parameters` (SKUGGA_LINT_010) - Ensures parameters have descriptions

**Path Rules:**
- `path-parameters` (SKUGGA_LINT_011) - Ensures path parameters are defined in operations
- `no-identical-paths` (SKUGGA_LINT_012) - Prevents duplicate path patterns

**Tag Rules:**
- `tag-description` (SKUGGA_LINT_013) - Ensures tags have descriptions
- `openapi-tags` (SKUGGA_LINT_014) - Ensures referenced tags are defined globally

**Schema Rules:**
- `typed-enum` (SKUGGA_LINT_015) - Ensures enum types are specified
- `schema-description` (SKUGGA_LINT_016) - Ensures schemas have descriptions

**Component Rules:**
- `no-unused-components` (SKUGGA_LINT_017) - Warns about unreferenced schemas

### Configuring Linting Rules

Use the `LintingRules` property to customize linting behavior:

```csharp
// Disable specific rules
[SkuggaFromOpenApi("api.json", LintingRules = "info-license:off,no-unused-components:off")]
public partial interface IMyApi { }

// Change severity levels
[SkuggaFromOpenApi("api.json", LintingRules = "operation-tags:error,info-contact:warn")]
public partial interface IStrictApi { }

// Combine multiple configurations
[SkuggaFromOpenApi(
    "api.json", 
    LintingRules = "info-license:off,operation-tags:error,tag-description:warn,no-unused-components:off"
)]
public partial interface ICustomApi { }
```

**Format:** `"rule1:severity,rule2:severity,..."` where severity is `off`, `warn`, `error`, or `info`.

### Suppressing Linting Diagnostics

For test projects or legacy specs, suppress diagnostics in your `.csproj`:

```xml
<PropertyGroup>
  <!-- Suppress all linting rules -->
  <NoWarn>$(NoWarn);SKUGGA_LINT_001;SKUGGA_LINT_002;SKUGGA_LINT_003;SKUGGA_LINT_004;SKUGGA_LINT_005;SKUGGA_LINT_006;SKUGGA_LINT_007;SKUGGA_LINT_008;SKUGGA_LINT_009;SKUGGA_LINT_010;SKUGGA_LINT_011;SKUGGA_LINT_012;SKUGGA_LINT_013;SKUGGA_LINT_014;SKUGGA_LINT_015;SKUGGA_LINT_016;SKUGGA_LINT_017</NoWarn>
</PropertyGroup>
```

### Example: Fixing Linting Violations

**Problem:** Build shows `SKUGGA_LINT_005` warning:

```
warning SKUGGA_LINT_005: Operation 'get /users' should have tags for better organization
```

**Solution:** Add tags to the operation in your OpenAPI spec:

```json
{
  "paths": {
    "/users": {
      "get": {
        "operationId": "getUsers",
        "tags": ["Users"],  // ✅ Fixed!
        "summary": "Get all users",
        "responses": {
          "200": { "description": "Success" }
        }
      }
    }
  }
}
```

**Benefits:**
- ✅ Enforces OpenAPI best practices at build time
- ✅ Configurable severity per rule
- ✅ AOT-compatible (zero runtime overhead)
- ✅ Inspired by industry-standard Spectral rules
- ✅ Actionable diagnostic messages with fix guidance

## FAQ

**Q: Do I need to check in downloaded OpenAPI specs?**  
A: No, they're cached in `obj/` which is gitignored. However, caching enables offline builds.

**Q: What happens if the API changes?**  
A: Your build fails with clear errors about missing/changed methods. Fix your tests, then rebuild.

**Q: Can I use multiple OpenAPI specs?**  
A: Yes! Apply `[SkuggaFromOpenApi]` to as many interfaces as needed.

**Q: Does it support OpenAPI 2.0 (Swagger)?**  
A: Phase 2 will support both OpenAPI 2.0 and 3.0+.

**Q: Can I override generated mock behavior?**  
A: Yes! Use normal Skugga `.Setup()` calls to override defaults.

**Q: Does this work with Native AOT?**  
A: Yes! Everything is compile-time generated, zero reflection, fully AOT-compatible.

## Contributing

See [Implementation Roadmap](../docs/openapi-roadmap.md) for detailed plans and contribution opportunities.

## Related

- [OpenAPI Specification](https://swagger.io/specification/)
- [Microsoft.OpenApi](https://github.com/microsoft/OpenAPI.NET)
- [Main Skugga README](../../README.md)
