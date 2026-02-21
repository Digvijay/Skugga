# Doppelgänger — OpenAPI Mock Generation

> **"Your tests should fail when APIs change, not your production."**

## The Problem: Contract Drift

You mock `IPaymentGateway` manually in your tests. Meanwhile, the platform team updates the actual Payment API spec. **Your tests pass** (because the mock is outdated), **but production crashes**.

-  **Manual Mocks**: Tests pass  → Production crashes 
-  **Doppelgänger**: Build fails  → Fix before deploy 

## Quick Start

```csharp
// One attribute — generates interface + mock from spec
[SkuggaFromOpenApi("petstore.json")]
public partial interface IPetStoreApi { }

// Use in tests
var mock = Mock.Create<IPetStoreApi>();
var pet = mock.GetPet("123"); // Returns realistic data from spec
```

**When the API changes, your build fails with clear errors:**
```
error CS0117: 'IPetStoreApi' does not contain definition for 'GetPet'
error CS0029: Cannot convert type 'decimal' to 'int'
```

## URL Sources

```csharp
[SkuggaFromOpenApi("https://api.example.com/openapi.json")]
public partial interface IExternalApi { }
```

Specs from URLs are automatically cached locally for offline builds.

## Async/Sync Configuration

```csharp
// Async (default)
[SkuggaFromOpenApi("api.json")]
public partial interface IAsyncApi { }
// Generates: Task<User[]> GetUsers();

// Sync
[SkuggaFromOpenApi("api.json", GenerateAsync = false)]
public partial interface ISyncApi { }
// Generates: User[] GetUsers();
```

## Response Headers

```csharp
[SkuggaFromOpenApi("api-with-headers.json")]
public partial interface IApiWithHeaders { }

var mock = Mock.Create<IApiWithHeaders>();
var response = mock.GetUser(123);
var user = response.Body;
var rateLimit = response.Headers["X-RateLimit-Limit"];
```

## Example Set Selection

```csharp
// Happy path
[SkuggaFromOpenApi("users.json", UseExampleSet = "success")]
public partial interface IUserApiSuccess { }

// Error handling
[SkuggaFromOpenApi("users.json", UseExampleSet = "error")]
public partial interface IUserApiError { }
```

## Authentication Mocking

```csharp
[SkuggaFromOpenApi("secure-api.json")]
public partial interface ISecureApi { }

var mock = new ISecureApiMock();
mock.ConfigureSecurity(
   tokenExpired: true,
   tokenInvalid: false,
   credentialsRevoked: false
);
// Operations return 401 Unauthorized when called
```

## Stateful Mocking

In-memory CRUD for integration tests:

```csharp
[SkuggaFromOpenApi("users.json")]
public partial interface IUserApi { }

var mock = new IUserApiMock();
var user = mock.CreateUser(new User { Name = "Alice" }); // Stored
var retrieved = mock.GetUser(user.Id);                   // Retrieved
Assert.Equal("Alice", retrieved.Name);
```

## Contract Validation

```csharp
[SkuggaFromOpenApi("products.json", ValidateSchemas = true)]
public partial interface IValidatedApi { }

// Mock validates all responses against OpenAPI schemas
// Throws ContractViolationException if schema doesn't match
```

## OpenAPI Linting

Enforce best practices at build time:

```csharp
[SkuggaFromOpenApi("api.json", LintingRules = "operation-tags:error,info-license:off")]
public partial interface IMyApi { }
```

16 Spectral-inspired linting rules (SKUGGA_LINT_001 through SKUGGA_LINT_017).

## What Makes Doppelgänger Unique?

| Tool | Purpose |
|------|---------|
| **OpenAPI Generator** | Generates production clients, not test mocks |
| **NSwag** | Generates clients + Swagger UI, not test mocks |
| **Moq (manual)** | No OpenAPI integration, contracts drift |
| **Doppelgänger** | Only tool for test mocks with build-time contract validation |

**ROI**: Save $23k–33k per year preventing contract drift incidents.

[Full Doppelgänger guide →](https://github.com/Digvijay/Skugga/blob/master/docs/DOPPELGANGER.md) |  [Demo code →](https://github.com/Digvijay/Skugga/tree/master/samples/DoppelgangerDemo)
