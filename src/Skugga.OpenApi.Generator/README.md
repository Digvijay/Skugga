# Skugga OpenAPI Generator (Doppelgänger)

This source generator creates mock implementations from OpenAPI (Swagger) specifications, enabling **Contract-First Testing** where your mocks stay in sync with the actual API contract.

## Features

- ✅ **Compile-time generation** - Zero runtime overhead, Native AOT compatible
- ✅ **Auto-sync with specs** - Contract drift = build failure, not production bugs
- ✅ **Realistic test data** - Uses OpenAPI `example` fields for defaults
- ✅ **Flexible sources** - URLs, relative paths, absolute paths
- ✅ **Smart caching** - ETags, offline support, incremental builds
- ✅ **Works with existing Skugga** - Setup, Verify, Chaos, Harness all work

## Usage

```csharp
using Skugga.Core;

// Mark an interface with the OpenAPI spec location
[SkuggaFromOpenApi("https://api.stripe.com/v1/swagger.json")]
public partial interface IStripeClient { }

// In your test:
var mock = Mock.Create<IStripeClient>();

// The mock has realistic defaults from the OpenAPI spec
var invoice = mock.GetInvoice("inv_123");
Assert.NotNull(invoice.Id);
```

## Supported Sources

### Remote URLs

```csharp
[SkuggaFromOpenApi("https://api.example.com/openapi.json")]
```

**Requirements:** Add the MSBuild tasks to your project:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  
  <!-- Your project configuration -->
  
  <!-- Define URL specs -->
  <ItemGroup>
    <SkuggaOpenApiUrl Include="https://api.example.com/openapi.json" />
  </ItemGroup>

  <!-- Import MSBuild targets (at the end) -->
  <PropertyGroup>
    <SkuggaOpenApiTasksAssembly>path/to/Skugga.OpenApi.Tasks.dll</SkuggaOpenApiTasksAssembly>
  </PropertyGroup>
  <Import Project="path/to/Skugga.OpenApi.Tasks.targets" />
  
</Project>
```

**How it works:**
- First build: Downloads spec to `obj/skugga-openapi-cache/` (generator can't see it yet)
- Second build: Loads cached spec and generates code
- Uses ETags to avoid unnecessary downloads
- Works offline with cached version

**Cache location:** `obj/skugga-openapi-cache/48d20ee6325eab90.json` (SHA256 hash of URL)

### Local Files

For local OpenAPI specs, add them to `AdditionalFiles`:

```xml
<ItemGroup>
  <AdditionalFiles Include="specs/api.json" />
</ItemGroup>
```

Then reference in code:

```csharp
// Relative to project directory
[SkuggaFromOpenApi("specs/api.json")]
[SkuggaFromOpenApi("../shared/openapi/stripe.yaml")]

// Absolute paths
[SkuggaFromOpenApi("/Users/me/specs/api.json")]
[SkuggaFromOpenApi("C:\\Specs\\api.json")]
```

## Advanced Options

```csharp
[SkuggaFromOpenApi("stripe.json", 
    GenerateAsync = false,                  // Generate sync methods (default: true)
    OperationFilter = "payments,invoices",  // Only generate these tags
    UseExampleSet = "success-case",         // Which named example to use (v1.3.0+)
    SchemaPrefix = "Stripe",                // Prefix for schema names to avoid collisions
    ValidateSchemas = true,                 // Enable schema validation
    CachePath = "custom/cache/path")]       // Custom cache location
```

### Using Named Example Sets (v1.3.0+)

OpenAPI 3.0 supports multiple named examples for responses. Use `UseExampleSet` to select specific examples for mock data:

```json
{
  "responses": {
    "200": {
      "content": {
        "application/json": {
          "examples": {
            "success": {
              "value": {"id": 123, "status": "active"}
            },
            "new-user": {
              "value": {"id": 456, "status": "pending"}
            },
            "premium": {
              "value": {"id": 789, "status": "premium"}
            }
          }
        }
      }
    }
  }
}
```

```csharp
// Generate different mocks for different test scenarios
[SkuggaFromOpenApi("users.json", UseExampleSet = "success", SchemaPrefix = "Success")]
public partial interface ISuccessUserApi { }

[SkuggaFromOpenApi("users.json", UseExampleSet = "new-user", SchemaPrefix = "NewUser")]
public partial interface INewUserApi { }

[SkuggaFromOpenApi("users.json", UseExampleSet = "premium", SchemaPrefix = "Premium")]
public partial interface IPremiumUserApi { }

// Use in tests
var successMock = new ISuccessUserApiMock();
var user = await successMock.GetUser(1); // Returns success example data

var newUserMock = new INewUserApiMock();
var newUser = await newUserMock.GetUser(1); // Returns new-user example data
```

If the specified example doesn't exist, Skugga falls back to the first available example.

## Implementation Status

### ✅ Phase 1: MVP (v1.2.0) - Complete
- [x] `SkuggaFromOpenApiAttribute` with all options
- [x] Basic generator infrastructure
- [x] OpenApiSpecLoader with AdditionalFiles support
- [x] URL and file path specification
- [x] MSBuild tasks for URL downloading
- [x] Cache infrastructure with ETag support
- [x] Test infrastructure

### ✅ Phase 2: OpenAPI Parsing (v1.2.0) - Complete
- [x] Microsoft.OpenApi.Readers integration
- [x] Interface generation from operations
- [x] Mock class generation
- [x] Example-based default values
- [x] Complex type handling ($ref, allOf, oneOf, anyOf)
- [x] Response model generation
- [x] Schema classes with proper type mapping
- [x] Path and query parameter handling

### ✅ Phase 3: Advanced Features (v1.2.0) - Complete
- [x] Async/sync generation control (GenerateAsync property)
- [x] Advanced schema features (discriminators, allOf, oneOf, anyOf)
- [x] Polymorphic type support
- [x] URL downloading with caching
- [x] Comprehensive error diagnostics
- [x] Full OpenAPI 3.0 support

### 📋 Future Enhancements (v1.3.0+)
- [ ] Operation filtering by tags
- [ ] Multiple example sets
- [ ] Namespace prefixes for schema collision avoidance
- [ ] OpenAPI 3.1 support
- [ ] YAML format support

## Architecture

```
[SkuggaFromOpenApi("url")] 
    ↓
OpenApiSyntaxReceiver (find attributes)
    ↓
OpenApiSpecLoader (fetch/cache spec)
    ↓
Microsoft.OpenApi.Readers (parse spec)
    ↓
Interface Generator (create interface from operations)
    ↓
Mock Generator (create mock with defaults)
    ↓
Interceptor Generator (redirect Mock.Create calls)
```

## Cache Structure

```
obj/skugga-openapi-cache/
├── a1b2c3d4e5f6... .json   (spec content)
├── a1b2c3d4e5f6... .meta   (ETag, timestamp, URL)
└── 9f8e7d6c5b4a... .json
```

Cache keys are SHA256 hashes of normalized URLs.

## Diagnostics

| Code | Severity | Description |
|------|----------|-------------|
| SKUGGA_OPENAPI_001 | Error | Unexpected generator error |
| SKUGGA_OPENAPI_002 | Error | Empty or invalid source parameter |
| SKUGGA_OPENAPI_003 | Error | Could not load OpenAPI spec from source (check AdditionalFiles or URL download) |
| SKUGGA_OPENAPI_004 | Error | OpenAPI spec parse error |
| SKUGGA_OPENAPI_005 | Error | Failed to generate code from OpenAPI spec |
| SKUGGA_OPENAPI_006 | Info | OpenAPI 2.0 (Swagger) detected - automatically converted to OpenAPI 3.0 |
| SKUGGA_OPENAPI_007 | Warning | Operation has no success response (2xx or default) - method will return void |
| SKUGGA_OPENAPI_008 | Warning | OpenAPI document validation found issues (missing paths, null schemas, etc.) |

**Common issue:** SKUGGA_OPENAPI_003 on first build with URLs is expected. The spec is downloaded during the first build but only available on the second build. See [Troubleshooting](../../docs/TROUBLESHOOTING.md#doppelgänger-openapi-issues) for details.

**Validation warnings (007, 008):** These help identify potential issues in your OpenAPI specs. Operations without success responses will generate void methods. Document validation checks for missing required properties and structural issues.

## Contributing

See [Implementation Roadmap](../../docs/openapi-roadmap.md) for planned features.
