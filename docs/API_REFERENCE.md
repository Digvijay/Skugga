# Skugga API Reference

Complete API documentation for Skugga mocking library.

## Table of Contents
- [Mock Creation](#mock-creation)
- [Setup API](#setup-api)
- [Verify API](#verify-api)
- [Argument Matchers](#argument-matchers)
- [Setup Sequence](#setup-sequence)
- [Callbacks](#callbacks)
- [Mock Behavior](#mock-behavior)
- [AutoScribe](#autoscribe)
- [Chaos Mode](#chaos-mode)
- [Performance Testing](#performance-testing)
- [Doppelgänger (OpenAPI Mock Generation)](#doppelgänger-openapi-mock-generation)

---

## Doppelgänger (OpenAPI Mock Generation)

**Doppelgänger** brings **Consumer-Driven Contracts to the unit testing level** - eliminating "contract drift" by generating mocks directly from OpenAPI specifications at build time.

### The Pain Point: Contract Drift

**You mock blindly. Your tests pass. Production crashes.**

Traditional mocking creates a dangerous illusion:

```csharp
// You manually create this interface
public interface IPaymentGateway
{
    Invoice GetInvoice(string id);
}

// You manually mock it
var mock = new Mock<IPaymentGateway>();
mock.Setup(x => x.GetInvoice(It.IsAny<string>()))
    .Returns(new Invoice { Id = "fake", Amount = 100 });

// Tests pass!
```

**Meanwhile...** the platform team updates the Payment API:
- Renames `GetInvoice` -> `RetrieveInvoice`
- Changes `Amount` from `int` to `decimal`
- Adds required `Currency` field

**Result:**
- Tests still pass (outdated mock doesn't match API)
- Production crashes (real API has changed)

This is **contract drift** - your mocks lie to you.

### The Skugga Solution: Never Mock Blindly

Doppelgänger reads the OpenAPI spec at build time and generates mocks that **must** match the contract:

```csharp
using Skugga.Core;

// "God Mode" Attribute - generates interface + mock from spec
[SkuggaFromOpenApi("https://api.stripe.com/v1/swagger.json")]
public partial interface IStripeClient { }

// In your test:
var mock = Mock.Create<IStripeClient>();
var invoice = mock.GetInvoice("inv_123");
// Returns realistic Invoice from spec examples

// When the API changes:
// Old way: Tests pass, production crashes
// Skugga: Build fails, fix before deploy
```

**Consumer-Driven Contracts at Unit Test Level** - when the spec changes, your code breaks at compile time, not at runtime.

### Quick Start

```csharp
using Skugga.Core;
using Xunit;

// 1. Define interface with OpenAPI spec
[SkuggaFromOpenApi("https://petstore3.swagger.io/api/v3/openapi.json")]
public partial interface IPetStoreApi { }

// 2. Create mock and test
[Fact]
public async Task Can_Get_Pet()
{
    var api = Mock.Create<IPetStoreApi>();
    var pet = await api.GetPetById(123);
    Assert.NotNull(pet);
}
```

### Feature 1: Automatic Interface Generation

**What it does:** Generates complete interface definitions from OpenAPI operations - no manual coding required.

**Use Case:** You're integrating with a third-party API and need a typed interface for testing.

**Example:**
```csharp
// Before: Manual interface creation (tedious and error-prone)
public interface IStripeClient
{
    Task<Invoice> GetInvoice(string id);
    Task<Customer> CreateCustomer(CustomerRequest request);
    // ... 50+ more methods to maintain manually
}

// After: Auto-generated from OpenAPI spec
[SkuggaFromOpenApi("https://api.stripe.com/v1/openapi.json")]
public partial interface IStripeClient { }
// All methods generated automatically!
```

**Testing with generated interface:**
```csharp
[Fact]
public async Task Invoice_Retrieval_Returns_Valid_Data()
{
    // Arrange
    var stripe = Mock.Create<IStripeClient>();

    // Act - method exists because it's in the OpenAPI spec
    var invoice = await stripe.GetInvoice("inv_123");

    // Assert - realistic defaults from spec examples
    Assert.NotNull(invoice);
    Assert.NotNull(invoice.Id);
    Assert.True(invoice.Total > 0);
}
```

**Reference Tests:**
- [`BasicGeneratorTests.cs`](../../tests/Skugga.OpenApi.Tests/Core/BasicGeneratorTests.cs) - Interface generation validation
- [`AdvancedFeaturesTests.cs`](../../tests/Skugga.OpenApi.Tests/Generation/AdvancedFeaturesTests.cs) - Complex schema handling

---

### Feature 2: Async/Sync Method Generation

**What it does:** Controls whether generated methods use `async Task<T>` or synchronous return types.

**Use Case:** You're wrapping a synchronous API or need to match your existing codebase patterns.

**Async Example (Default):**
```csharp
[SkuggaFromOpenApi("users.json")]
public partial interface IUserApiAsync { }

// Generated methods:
// Task<User[]> GetUsers();
// Task<User> GetUser(int id);
// Task DeleteUser(int id);

[Fact]
public async Task Async_Methods_Work_With_Await()
{
    var api = Mock.Create<IUserApiAsync>();

    var users = await api.GetUsers();
    Assert.NotNull(users);

    var user = await api.GetUser(1);
    Assert.NotNull(user);
}
```

**Sync Example:**
```csharp
[SkuggaFromOpenApi("users.json", GenerateAsync = false)]
public partial interface IUserApiSync { }

// Generated methods:
// User[] GetUsers();
// User GetUser(int id);
// void DeleteUser(int id);

[Fact]
public void Sync_Methods_Work_Without_Await()
{
    var api = Mock.Create<IUserApiSync>();

    var users = api.GetUsers();  // No await needed
    Assert.NotNull(users);

    var user = api.GetUser(1);
    Assert.NotNull(user);
}
```

**When to use sync mode:**
- Legacy APIs that don't support async operations
- Performance-critical code where Task allocation overhead matters
- Testing synchronous business logic
- Compatibility with frameworks that don't support async

**Reference Tests:**
- [`GenerateAsyncConfigurationTests.cs`](../../tests/Skugga.OpenApi.Tests/Integration/GenerateAsyncConfigurationTests.cs) - Async vs sync generation

---

### Feature 3: Response Headers Support

**What it does:** Wraps response bodies with headers when OpenAPI operations define response headers.

**Use Case:** Testing APIs that return important metadata in headers (rate limits, ETags, pagination).

**Example:**
```csharp
[SkuggaFromOpenApi("api-with-headers.json")]
public partial interface IGitHubApi { }

[Fact]
public async Task Can_Access_RateLimit_Headers()
{
    // Arrange
    var api = Mock.Create<IGitHubApi>();

    // Act
    var response = await api.ListRepositories("microsoft");

    // Assert - access response body
    Assert.NotNull(response.Body);
    Assert.True(response.Body.Length > 0);

    // Assert - access headers
    Assert.NotNull(response.Headers);
    Assert.Contains("X-RateLimit-Limit", response.Headers.Keys);
    Assert.Contains("X-RateLimit-Remaining", response.Headers.Keys);

    // Parse header values
    var limit = int.Parse(response.Headers["X-RateLimit-Limit"]);
    Assert.True(limit > 0);
}

[Fact]
public async Task Can_Test_Pagination_With_LinkHeader()
{
    var api = Mock.Create<IGitHubApi>();

    var response = await api.ListUsers(page: 1);

    // Check pagination headers
    if (response.Headers.ContainsKey("Link"))
    {
        var linkHeader = response.Headers["Link"];
        Assert.Contains("rel=\"next\"", linkHeader);
    }
}
```

**OpenAPI Spec Example:**
```json
{
  "/users/{id}": {
    "get": {
      "responses": {
        "200": {
          "headers": {
            "X-RateLimit-Limit": {
              "schema": { "type": "integer" },
              "example": 5000
            },
            "ETag": {
              "schema": { "type": "string" },
              "example": "\"686897696a7c876b7e\""
            }
          }
        }
      }
    }
  }
}
```

**Reference Tests:**
- [`ResponseHeadersTests.cs`](../../tests/Skugga.OpenApi.Tests/Generation/ResponseHeadersTests.cs) - Header access and validation

---

### Feature 4: Example Set Selection

**What it does:** Selects specific named example sets from OpenAPI specs for different test scenarios.

**Use Case:** Your API spec defines multiple examples (success, error, edge cases) and you want to test each scenario.

**Example:**
```csharp
// Success case testing
[SkuggaFromOpenApi("users.json", UseExampleSet = "success")]
public partial interface IUserApiSuccess { }

// Error case testing
[SkuggaFromOpenApi("users.json", UseExampleSet = "notfound")]
public partial interface IUserApiNotFound { }

// Edge case testing
[SkuggaFromOpenApi("users.json", UseExampleSet = "suspended")]
public partial interface IUserApiSuspended { }

[Fact]
public async Task Success_Example_Returns_Active_User()
{
    var api = Mock.Create<IUserApiSuccess>();
    var user = await api.GetUser(1);

    Assert.NotNull(user);
    Assert.Equal("active", user.Status);
    Assert.NotNull(user.Email);
}

[Fact]
public async Task NotFound_Example_Returns_Null()
{
    var api = Mock.Create<IUserApiNotFound>();
    var user = await api.GetUser(999);

    Assert.Null(user);  // Not found example returns null
}

[Fact]
public async Task Suspended_Example_Returns_Suspended_User()
{
    var api = Mock.Create<IUserApiSuspended>();
    var user = await api.GetUser(1);

    Assert.NotNull(user);
    Assert.Equal("suspended", user.Status);
    Assert.Null(user.Email);  // Suspended users have no email
}
```

**OpenAPI Spec Example:**
```json
{
  "/users/{id}": {
    "get": {
      "responses": {
        "200": {
          "content": {
            "application/json": {
              "examples": {
                "success": {
                  "value": {
                    "id": 123,
                    "name": "Alice",
                    "status": "active",
                    "email": "alice@example.com"
                  }
                },
                "suspended": {
                  "value": {
                    "id": 456,
                    "name": "Bob",
                    "status": "suspended",
                    "email": null
                  }
                }
              }
            }
          }
        }
      }
    }
  }
}
```

**Reference Tests:**
- [`ExampleSetTests.cs`](../../tests/Skugga.OpenApi.Tests/Generation/ExampleSetTests.cs) - Multiple example selection

---

### Feature 5: Authentication & Security Testing

**What it does:** Generates methods to configure security scenarios (expired tokens, invalid credentials, etc.).

**Use Case:** Testing how your application handles various authentication failures.

**Example:**
```csharp
[SkuggaFromOpenApi("secure-api.json")]
public partial interface ISecureApi { }

[Fact]
public async Task ExpiredToken_Returns_401()
{
    // Arrange
    var api = new ISecureApiMock();
    api.ConfigureSecurity(
        tokenExpired: true,
        tokenInvalid: false,
        credentialsRevoked: false
    );

    // Act
    var result = await api.GetProtectedResource();

    // Assert - Mock returns 401 response
    Assert.Null(result);  // Or throws UnauthorizedException depending on spec
}

[Fact]
public async Task InvalidToken_Returns_401()
{
    var api = new ISecureApiMock();
    api.ConfigureSecurity(tokenInvalid: true);

    var result = await api.GetProtectedResource();
    Assert.Null(result);
}

[Fact]
public async Task RevokedCredentials_Returns_403()
{
    var api = new ISecureApiMock();
    api.ConfigureSecurity(credentialsRevoked: true);

    var result = await api.GetProtectedResource();
    Assert.Null(result);  // Or throws ForbiddenException
}

[Fact]
public async Task ValidAuthentication_Returns_Data()
{
    var api = new ISecureApiMock();
    api.ConfigureSecurity(
        tokenExpired: false,
        tokenInvalid: false,
        credentialsRevoked: false
    );

    var result = await api.GetProtectedResource();
    Assert.NotNull(result);  // Success case
}

[Fact]
public async Task Can_Generate_AccessToken()
{
    var api = new ISecureApiMock();

    var token = api.GenerateAccessToken(
        clientId: "test-client",
        scopes: new[] { "read", "write" }
    );

    Assert.NotNull(token);
    Assert.Contains("Bearer", token);
}
```

**Reference Tests:**
- [`AuthenticationMockingTests.cs`](../../tests/Skugga.OpenApi.Tests/Generation/AuthenticationMockingTests.cs) - Security testing scenarios

---

### Feature 6: Stateful Mocking (CRUD Operations)

**What it does:** Tracks created/updated/deleted entities in-memory for realistic CRUD testing.

**Use Case:** Testing application logic that performs multiple CRUD operations.

**Example:**
```csharp
[SkuggaFromOpenApi("users.json")]
public partial interface IUserApi { }

[Fact]
public async Task Can_Create_And_Retrieve_User()
{
    // Arrange
    var api = new IUserApiMock();  // Use concrete mock class for stateful behavior

    // Act - Create user
    var created = await api.CreateUser(new User
    {
        Name = "Alice",
        Email = "alice@example.com"
    });

    // Assert - User was created with ID
    Assert.NotNull(created);
    Assert.True(created.Id > 0);

    // Act - Retrieve the same user
    var retrieved = await api.GetUser(created.Id);

    // Assert - Retrieved user matches created user
    Assert.NotNull(retrieved);
    Assert.Equal("Alice", retrieved.Name);
    Assert.Equal("alice@example.com", retrieved.Email);
}

[Fact]
public async Task Can_Update_Existing_User()
{
    var api = new IUserApiMock();

    // Create initial user
    var user = await api.CreateUser(new User { Name = "Bob" });

    // Update user
    user.Name = "Bob Updated";
    var updated = await api.UpdateUser(user.Id, user);

    // Verify update persisted
    var retrieved = await api.GetUser(user.Id);
    Assert.Equal("Bob Updated", retrieved.Name);
}

[Fact]
public async Task Can_Delete_User()
{
    var api = new IUserApiMock();

    // Create user
    var user = await api.CreateUser(new User { Name = "Charlie" });

    // Delete user
    await api.DeleteUser(user.Id);

    // Verify user no longer exists
    var retrieved = await api.GetUser(user.Id);
    Assert.Null(retrieved);
}

[Fact]
public async Task Can_List_Multiple_Users()
{
    var api = new IUserApiMock();

    // Create multiple users
    await api.CreateUser(new User { Name = "Alice" });
    await api.CreateUser(new User { Name = "Bob" });
    await api.CreateUser(new User { Name = "Charlie" });

    // List all users
    var users = await api.ListUsers();

    Assert.NotNull(users);
    Assert.Equal(3, users.Length);
}
```

**Reference Tests:**
- [`StatefulMockingTests.cs`](../../tests/Skugga.OpenApi.Tests/Generation/StatefulMockingTests.cs) - In-memory entity tracking

---

### Feature 7: Contract Validation

**What it does:** Validates mock responses against OpenAPI schemas at runtime to catch contract violations.

**Use Case:** Ensuring your mocks stay compliant with the API contract, catching breaking changes early.

**Example:**
```csharp
[SkuggaFromOpenApi("products.json", ValidateSchemas = true)]
public partial interface IValidatedProductApi { }

[Fact]
public async Task ValidResponse_PassesValidation()
{
    // Arrange
    var api = new IValidatedProductApiMock();

    // Act - Mock generates valid response
    var product = await api.GetProduct(123);

    // Assert - No exception thrown, response is valid
    Assert.NotNull(product);
    Assert.NotNull(product.Name);
    Assert.True(product.Price > 0);
}

[Fact]
public async Task InvalidResponse_ThrowsContractViolation()
{
    var api = Mock.Create<IValidatedProductApi>();

    // Setup invalid response (missing required field)
    api.Setup(m => m.GetProduct(It.IsAny<long>()))
       .Returns(Task.FromResult(new Product
       {
           Id = 123,
           // Name is required but missing!
           Price = 99.99m
       }));

    // Act & Assert - Validation throws exception
    await Assert.ThrowsAsync<ContractViolationException>(
        async () => await api.GetProduct(123)
    );
}

[Fact]
public async Task Validation_ChecksRequiredFields()
{
    var api = new IValidatedProductApiMock();

    // Generated mock ensures all required fields are present
    var product = await api.GetProduct(1);

    Assert.NotNull(product.Id);    // Required
    Assert.NotNull(product.Name);  // Required
    Assert.NotNull(product.Price); // Required
    // Optional fields can be null
}
```

**Reference Tests:**
- [`ContractValidationTests.cs`](../../tests/Skugga.OpenApi.Tests/Generation/ContractValidationTests.cs) - Schema validation

---

### Feature 8: URL & Local File Support

**What it does:** Load OpenAPI specs from remote URLs (with caching) or local file paths.

**Use Case:** Using publicly available API specs or sharing specs across team members.

**URL Example:**
```csharp
// Remote spec with automatic caching
[SkuggaFromOpenApi("https://petstore3.swagger.io/api/v3/openapi.json")]
public partial interface IPetStoreApi { }

[Fact]
public async Task RemoteSpec_GeneratesWorkingMock()
{
    var api = Mock.Create<IPetStoreApi>();
    var pet = await api.GetPetById(1);
    Assert.NotNull(pet);
}
```

**Local File Example:**
```csharp
// Relative path from project directory
[SkuggaFromOpenApi("../specs/internal-api.json")]
public partial interface IInternalApi { }

// Absolute path
[SkuggaFromOpenApi("/path/to/specs/api.json")]
public partial interface IMyApi { }

[Fact]
public async Task LocalSpec_GeneratesWorkingMock()
{
    var api = Mock.Create<IInternalApi>();
    var data = await api.GetData();
    Assert.NotNull(data);
}
```

**Cache Behavior:**
```csharp
// First build: Downloads spec
// obj/skugga-openapi-cache/abc123.json (created)

// Second build: Uses cache
// obj/skugga-openapi-cache/abc123.json (reused)

// Changed spec: Auto-redownload via ETag
// obj/skugga-openapi-cache/abc123.json (updated)
```

**Reference Tests:**
- [`UrlDownloadingTests.cs`](../../tests/Skugga.OpenApi.Tests/Integration/UrlDownloadingTests.cs) - URL caching and loading

---

### Feature 9: OpenAPI Quality Linting

**What it does:** Enforces OpenAPI best practices at build time with customizable rules.

**Use Case:** Maintaining high-quality API documentation and catching common mistakes.

**Example:**
```csharp
// Strict linting - all rules enabled
[SkuggaFromOpenApi("api.json")]
public partial interface IStrictApi { }
// Build shows warnings for:
// - Missing operation tags
// - Missing descriptions
// - Missing license info
// - Undefined tags
// etc.

// Custom linting configuration
[SkuggaFromOpenApi("api.json",
    LintingRules = "operation-tags:error,info-license:off,no-unused-components:off")]
public partial interface ICustomApi { }
// Build fails if operations missing tags
// Ignores missing license and unused components

// Disable all linting
[SkuggaFromOpenApi("api.json", LintingRules = "all:off")]
public partial interface INoLintApi { }
```

**Available Rules:**
- `info-contact`, `info-description`, `info-license` - API info section
- `operation-operationId`, `operation-tags`, `operation-description` - Operations
- `operation-success-response`, `operation-parameters` - Response definitions
- `path-parameters`, `no-identical-paths` - Path configuration
- `tag-description`, `openapi-tags` - Tag definitions
- `typed-enum`, `schema-description` - Schema quality
- `no-unused-components` - Dead code detection

**Severity Levels:**
- `off` - Rule disabled
- `info` - Informational message
- `warn` - Warning (doesn't fail build)
- `error` - Error (fails build)

**Reference Tests:**
- [`SpectralLintingTests.cs`](../../tests/Skugga.OpenApi.Tests/Linting/SpectralLintingTests.cs) - Linting rule validation

---

### Feature 10: Advanced Schema Support

**What it does:** Handles complex OpenAPI schemas (allOf, oneOf, anyOf, discriminators, nested refs).

**Use Case:** Working with sophisticated API specifications that use composition and polymorphism.

**AllOf Example (Composition):**
```csharp
[SkuggaFromOpenApi("products.json")]
public partial interface IProductApi { }

// OpenAPI spec uses allOf:
// Product = BaseProduct + { id: number }

[Fact]
public async Task AllOf_CombinesProperties()
{
    var api = Mock.Create<IProductApi>();

    // Override default since ExampleGenerator doesn't handle allOf yet
    api.Setup(m => m.UpdateProduct(It.IsAny<long>(), It.IsAny<Product>()))
       .Returns(Task.FromResult(new Product
       {
           Id = 123,                    // From allOf extension
           Name = "Widget",             // From BaseProduct
           Category = "tools",          // From BaseProduct
           Price = 29.99m               // From BaseProduct
       }));

    var product = await api.UpdateProduct(123, new Product());

    Assert.NotNull(product);
    Assert.Equal(123, product.Id);
    Assert.Equal("Widget", product.Name);
}
```

**OneOf Example (Polymorphism):**
```csharp
// OpenAPI spec uses oneOf for payment methods
[SkuggaFromOpenApi("payments.json")]
public partial interface IPaymentApi { }

[Fact]
public async Task OneOf_HandlesMultipleTypes()
{
    var api = Mock.Create<IPaymentApi>();

    // Can be CreditCardPayment OR BankTransfer OR PayPalPayment
    var payment = await api.GetPayment(123);

    Assert.NotNull(payment);
    // Check discriminator to determine actual type
    if (payment.Type == "credit_card")
    {
        var cc = payment as CreditCardPayment;
        Assert.NotNull(cc.CardNumber);
    }
}
```

**Reference Tests:**
- [`AllOfTests.cs`](../../tests/Skugga.OpenApi.Tests/Generation/AllOfTests.cs) - Schema composition
- [`AdvancedFeaturesTests.cs`](../../tests/Skugga.OpenApi.Tests/Generation/AdvancedFeaturesTests.cs) - Complex schemas

---

### Complete Real-World Example

**Scenario:** Testing a microservice that integrates with GitHub API.

```csharp
using Skugga.Core;
using Xunit;

// Generate interface from GitHub's OpenAPI spec
[SkuggaFromOpenApi("https://raw.githubusercontent.com/github/rest-api-description/main/descriptions/api.github.com/api.github.com.json")]
public partial interface IGitHubApi { }

public class GitHubIntegrationTests
{
    [Fact]
    public async Task Can_List_User_Repositories()
    {
        // Arrange
        var github = Mock.Create<IGitHubApi>();

        // Act
        var repos = await github.ListUserRepos("microsoft");

        // Assert - realistic data from OpenAPI examples
        Assert.NotNull(repos);
        Assert.True(repos.Length > 0);
        Assert.All(repos, repo => Assert.NotNull(repo.Name));
    }

    [Fact]
    public async Task Can_Get_Repository_Details()
    {
        var github = Mock.Create<IGitHubApi>();

        var repo = await github.GetRepository("microsoft", "vscode");

        Assert.NotNull(repo);
        Assert.Equal("vscode", repo.Name);
        Assert.NotNull(repo.Description);
    }

    [Fact]
    public async Task RateLimit_Headers_Are_Available()
    {
        var github = Mock.Create<IGitHubApi>();

        var response = await github.ListUserReposWithHeaders("microsoft");

        // Access response body
        Assert.NotNull(response.Body);

        // Access rate limit headers
        Assert.Contains("X-RateLimit-Limit", response.Headers.Keys);
        Assert.Contains("X-RateLimit-Remaining", response.Headers.Keys);

        var limit = int.Parse(response.Headers["X-RateLimit-Limit"]);
        Assert.Equal(5000, limit);
    }

    [Fact]
    public async Task Can_Override_Default_Behavior()
    {
        var github = Mock.Create<IGitHubApi>();

        // Override with custom data
        github.Setup(g => g.GetRepository("test", "repo"))
              .Returns(Task.FromResult(new Repository
              {
                  Name = "repo",
                  FullName = "test/repo",
                  Private = true,
                  Description = "Custom test repo"
              }));

        var repo = await github.GetRepository("test", "repo");

        Assert.True(repo.Private);
        Assert.Equal("Custom test repo", repo.Description);
    }
}
```

---

### Project Setup Guide

**Step 1: Add NuGet Packages**
```xml
<ItemGroup>
  <PackageReference Include="Skugga.Core" Version="1.3.0" />
  <PackageReference Include="Skugga.OpenApi.Generator" Version="1.3.0" />
</ItemGroup>
```

**Step 2: Add OpenAPI Spec**
```xml
<!-- For local files -->
<ItemGroup>
  <AdditionalFiles Include="specs/api.json" />
</ItemGroup>

<!-- For URLs -->
<ItemGroup>
  <SkuggaOpenApiUrl Include="https://api.example.com/openapi.json" />
</ItemGroup>
```

**Step 3: Mark Interface**
```csharp
[SkuggaFromOpenApi("specs/api.json")]
public partial interface IMyApi { }
```

**Step 4: Build & Test**
```bash
dotnet build   # Generates interface and mock
dotnet test    # Run tests with generated mocks
```

---

### Troubleshooting

**Issue:** `SKUGGA_OPENAPI_003: Could not load OpenAPI spec`

**Solution:** For URL specs, run `dotnet build` twice - first downloads, second generates.

---

**Issue:** `allOf` schema returns null

**Solution:** Use `.Setup()` to provide explicit return values (see [Feature 10](#feature-10-advanced-schema-support)).

---

**Issue:** Too many linting warnings

**Solution:** Configure `LintingRules` property or suppress in `.csproj`:
```xml
<PropertyGroup>
  <NoWarn>$(NoWarn);SKUGGA_LINT_001;SKUGGA_LINT_002</NoWarn>
</PropertyGroup>
```

---

### Additional Resources

- **[Full Doppelgänger Guide](DOPPELGANGER.md)** - Comprehensive documentation
- **[Test Examples](../../tests/Skugga.OpenApi.Tests/)** - 200+ real test cases
- **[OpenAPI Specification](https://swagger.io/specification/)** - OpenAPI format reference

---

## Mock Creation

### `Mock.Create<T>()`
Creates a mock instance of interface or class `T`.

```csharp
// Interface mocking (recommended)
var mock = Mock.Create<IEmailService>();

// Class mocking (members must be virtual)
var mock = Mock.Create<EmailService>();
```

### `Mock.Create<T>(MockBehavior)`
Creates a mock with specific behavior.

```csharp
// Loose behavior (default) - returns default values for un-setup members
var loose = Mock.Create<IService>(MockBehavior.Loose);

// Strict behavior - throws exception for un-setup members
var strict = Mock.Create<IService>(MockBehavior.Strict);
```

### `Mock.Of<T>(predicate)`
Creates a mock and sets up properties in one line (LINQ to Mocks).

```csharp
var mock = Mock.Of<IUser>(u =>
    u.Id == 1 &&
    u.Name == "Alice" &&
    u.IsActive
);
```

### `MockRepository`
Manage multiple mocks with shared configuration.

```csharp
var repo = new MockRepository(MockBehavior.Strict);

// Create mocks attached to repository
var service = repo.Create<IService>();
var repo = repo.Create<IRepository>();

// Verify all mocks in repository
repo.VerifyAll();
```

---

## Setup API

### Basic Setup
Configure return values for methods and properties.

```csharp
// Method setup
mock.Setup(x => x.GetData(1)).Returns("one");

// Property setup
mock.Setup(x => x.Count).Returns(42);

// Void method setup (for verification)
mock.Setup(x => x.Process(It.IsAny<int>()));
```

### Setup with Functions
Return values computed at invocation time.

```csharp
// Compute return value
mock.Setup(x => x.GetTimestamp())
    .Returns(() => DateTime.UtcNow);

// Access arguments
mock.Setup(x => x.Transform(It.IsAny<string>()))
    .Returns((string input) => input.ToUpper());
```

### Chaining Multiple Setups
```csharp
mock.Setup(x => x.GetData(1)).Returns("one");
mock.Setup(x => x.GetData(2)).Returns("two");
mock.Setup(x => x.Count).Returns(10);
```

---

## Verify API

### `Verify(expression, Times)`
Verify method was called with specific arguments.

```csharp
// Verify exact call
mock.Verify(x => x.GetData(1), Times.Once());

// Verify with any arguments
mock.Verify(x => x.Process(It.IsAny<int>()), Times.AtLeast(2));

// Verify never called
mock.Verify(x => x.Delete(), Times.Never());
```

### Times Helper

| Method | Description | Example |
|--------|-------------|---------|
| `Times.Once()` | Exactly one call | `Times.Once()` |
| `Times.Never()` | Zero calls | `Times.Never()` |
| `Times.Exactly(n)` | Exactly n calls | `Times.Exactly(3)` |
| `Times.AtLeast(n)` | n or more calls | `Times.AtLeast(2)` |
| `Times.AtMost(n)` | n or fewer calls | `Times.AtMost(5)` |
| `Times.Between(m,n)` | Between m and n calls (inclusive) | `Times.Between(2,4)` |

```csharp
// Verify exact count
mock.Verify(x => x.Save(), Times.Exactly(3));

// Verify range
mock.Verify(x => x.Retry(), Times.Between(1, 3));
```

---

## Argument Matchers

### `It.IsAny<T>()`
Matches any value of type T.

```csharp
mock.Setup(x => x.Process(It.IsAny<int>()))
    .Returns("any number");

mock.Process(1);    // Returns "any number"
mock.Process(999);  // Returns "any number"
```

### `It.Is<T>(predicate)`
Matches values satisfying a predicate.

```csharp
// Match positive numbers
mock.Setup(x => x.Process(It.Is<int>(n => n > 0)))
    .Returns("positive");

mock.Process(5);   // Returns "positive"
mock.Process(-1);  // Returns null (no match)

// Complex predicates
mock.Setup(x => x.ValidateUser(It.Is<User>(u => u.Age >= 18 && u.IsActive)))
    .Returns(true);
```

### `It.IsIn<T>(params T[])`
Matches values in a specified set.

```csharp
mock.Setup(x => x.GetColor(It.IsIn("red", "green", "blue")))
    .Returns("primary");

mock.GetColor("red");     // Returns "primary"
mock.GetColor("yellow");  // Returns null (no match)
```

### `It.IsNotNull<T>()`
Matches any non-null value.

```csharp
mock.Setup(x => x.Process(It.IsNotNull<string>()))
    .Returns("valid");

mock.Process("hello");  // Returns "valid"
mock.Process(null);     // Returns null (no match)
```

### `It.IsRegex(pattern)`
Matches strings against regex pattern.

```csharp
mock.Setup(x => x.ValidateEmail(It.IsRegex(@"^[\w\.-]+@[\w\.-]+\.\w+$")))
    .Returns(true);

mock.ValidateEmail("test@example.com");  // Returns true
mock.ValidateEmail("invalid");           // Returns false (no match)
```

### Combining Matchers
```csharp
mock.Setup(x => x.ProcessOrder(
    It.Is<int>(id => id > 0),
    It.IsNotNull<string>(),
    It.IsIn("USD", "EUR", "GBP")
)).Returns("processed");
```

### `It.Ref<T>.IsAny`
Matches any value for ref/out parameters.

```csharp
// Setup out parameter
mock.Setup(x => x.TryParse("123", out It.Ref<int>.IsAny))
    .OutValue(123)
    .Returns(true);

// Setup ref parameter
mock.Setup(x => x.Increment(ref It.Ref<int>.IsAny))
    .RefValue(42);
```

---

## Setup Sequence

### `SetupSequence(expression)`
Configure different return values for consecutive calls.

```csharp
// Basic sequence
mock.SetupSequence(x => x.GetNext())
    .Returns(1)
    .Returns(2)
    .Returns(3);

mock.GetNext(); // Returns 1
mock.GetNext(); // Returns 2
mock.GetNext(); // Returns 3
mock.GetNext(); // Returns 3 (repeats last value)
```

### Sequence with Exceptions
Perfect for testing retry logic.

```csharp
mock.SetupSequence(x => x.FetchData())
    .Throws(new TimeoutException("Connection timeout"))
    .Throws(new TimeoutException("Still timing out"))
    .Returns("success");

// First two calls throw, third succeeds
try { mock.FetchData(); } catch { /* retry */ }
try { mock.FetchData(); } catch { /* retry */ }
var data = mock.FetchData(); // "success"
```

### Property Sequences
```csharp
mock.SetupSequence(x => x.Counter)
    .Returns(0)
    .Returns(1)
    .Returns(2);

var a = mock.Counter; // 0
var b = mock.Counter; // 1
var c = mock.Counter; // 2
```

---

## Protected Members

Mock protected methods and properties on abstract classes.

### `Setup<T>(methodName, args)`
```csharp
var mock = Mock.Create<AbstractBase>();

// Setup protected method
mock.Protected()
    .Setup<string>("ExecuteCore", It.IsAny<string>())
    .Returns("mocked");
```

### `SetupGet/Set(propertyName)`
```csharp
// Setup protected property
mock.Protected()
    .SetupGet<int>("RetryCount")
    .Returns(5);
```

---

## Callbacks

### `Callback(action)`
Execute code when mock is invoked.

```csharp
var called = false;
mock.Setup(x => x.Execute())
    .Callback(() => called = true);

mock.Execute();
Assert.True(called);
```

### Callbacks with Arguments
Access method arguments in callback.

```csharp
var capturedValue = 0;
mock.Setup(x => x.Process(It.IsAny<int>()))
    .Callback((int value) => capturedValue = value)
    .Returns("processed");

mock.Process(42);
Assert.Equal(42, capturedValue);
```

### Chaining Callback and Returns
```csharp
mock.Setup(x => x.Save(It.IsAny<Data>()))
    .Callback((Data d) => Console.WriteLine($"Saving {d.Id}"))
    .Returns(true);
```

---

## Mock Behavior

### MockBehavior.Loose (Default)
Returns default values for un-setup members.

```csharp
var mock = Mock.Create<IService>(MockBehavior.Loose);
// or
var mock = Mock.Create<IService>();

// No setup for GetData
var result = mock.GetData(); // Returns null (default for string)
```

### MockBehavior.Strict
Throws exception for un-setup members.

```csharp
var mock = Mock.Create<IService>(MockBehavior.Strict);

// Throws exception - GetData not setup
var result = mock.GetData(); // Throws!

// Must setup all called members
mock.Setup(x => x.GetData()).Returns("value");
var result = mock.GetData(); // Returns "value"
```

---

## AutoScribe

### `AutoScribe.Capture<T>(implementation)`
Record real interactions and generate test setup code.

```csharp
// 1. Create recording proxy
var realService = new RealEmailService();
var recorder = AutoScribe.Capture<IEmailService>(realService);

// 2. Use recorder like normal service
var email = recorder.GetEmail(101);
recorder.SendEmail("test@test.com", "Hello");

// 3. Console output (auto-generated test code):
// [AutoScribe] mock.Setup(x => x.GetEmail(101)).Returns("user101@example.com");
// [AutoScribe] mock.Setup(x => x.SendEmail("test@test.com", "Hello")).Returns();
```

### Use Cases
- Bootstrap tests from existing implementations
- Document real API behavior
- Generate regression test suites
- Validate mock configurations against reality

**Status:**  Core functionality complete (18 tests passing). Enhanced features (timing analysis, export/replay, diff tool) planned for future releases.

---

## Chaos Mode

### `mock.Chaos(policy => ...)`
Inject random failures for resilience testing.

```csharp
mock.Chaos(policy => {
    policy.FailureRate = 0.3; // 30% failure rate
    policy.PossibleExceptions = new[] {
        new TimeoutException(),
        new HttpRequestException("Service unavailable")
    };
});

// 30% of calls will randomly throw one of the exceptions
for (int i = 0; i < 100; i++) {
    try {
        mock.CallService();
    } catch (TimeoutException) {
        // Handle timeout
    } catch (HttpRequestException) {
        // Handle service error
    }
}
```

### Configuration Options

| Property | Type | Description |
|----------|------|-------------|
| `FailureRate` | `double` | Probability of failure (0.0 - 1.0) |
| `PossibleExceptions` | `Exception[]` | Exceptions to randomly throw |

**Status:**  Core functionality complete (9 tests passing). Advanced features (latency simulation, chaos schedules, Polly integration) planned for future releases.

---

## Performance Testing

### `AssertAllocations.Zero(action)`
Verify code doesn't allocate heap memory.

```csharp
AssertAllocations.Zero(() => {
    // This block must not allocate
    mock.GetCachedData(); // Should return cached value without allocation
});

// Throws if any heap allocations detected
```

### Use Cases
- Validate hot path performance
- Ensure caching works correctly
- Verify allocation-free operations
- Benchmark optimization improvements

**Status:**  Core functionality complete. Advanced features (detailed allocation reports, CPU profiling, BenchmarkDotNet integration) planned for future releases.

---

## Best Practices

### 1. Prefer Interfaces
```csharp
// Good
public interface IEmailService { }
var mock = Mock.Create<IEmailService>();

// Acceptable (requires virtual members)
public class EmailService {
    public virtual string GetEmail() => "";
}
var mock = Mock.Create<EmailService>();
```

### 2. Use Specific Matchers
```csharp
// Too broad
mock.Setup(x => x.Process(It.IsAny<int>()));

// More specific
mock.Setup(x => x.Process(It.Is<int>(n => n > 0)));
```

### 3. Verify Important Interactions
```csharp
// Always verify critical operations
mock.Verify(x => x.SaveToDatabase(It.IsAny<Data>()), Times.Once());
mock.Verify(x => x.DeleteTempFiles(), Times.Never());
```

### 4. Clean Mocks Between Tests
```csharp
[Fact]
public void Test1() {
    var mock = Mock.Create<IService>(); // Fresh mock
    // ... test
}

[Fact]
public void Test2() {
    var mock = Mock.Create<IService>(); // Fresh mock
    // ... test
}
```

---

## Advanced Scenarios

### Multiple Setups for Same Method
```csharp
// Different setups for different arguments
mock.Setup(x => x.GetData(1)).Returns("one");
mock.Setup(x => x.GetData(2)).Returns("two");
mock.Setup(x => x.GetData(It.Is<int>(n => n > 10))).Returns("large");
```

### Setup with Complex Types
```csharp
mock.Setup(x => x.ProcessOrder(It.Is<Order>(o =>
    o.Total > 100 &&
    o.Items.Count > 0 &&
    o.Status == OrderStatus.Pending
))).Returns(true);
```

### Verify with Timeout
```csharp
// Note: Use your test framework's timeout features
[Fact(Timeout = 5000)]
public void TestWithTimeout() {
    mock.Process();
    mock.Verify(x => x.Process(), Times.Once());
}
```

---

## Generator Diagnostics

Skugga provides compile-time diagnostics to catch issues early:

### SKUGGA001: Cannot mock sealed class
```csharp
public sealed class Service { }
var mock = Mock.Create<Service>(); //  Compile error
```
**Solution:** Use interfaces or remove `sealed` modifier.

### SKUGGA002: Class has no virtual members
```csharp
public class Service {
    public string GetData() => ""; // Not virtual
}
var mock = Mock.Create<Service>(); //  Warning
```
**Solution:** Make members `virtual` or mock an interface instead.

---

## OpenAPI Diagnostics

### SKUGGA_OPENAPI_001 to SKUGGA_OPENAPI_008
**OpenAPI document validation errors** - See [DOPPELGANGER.md](DOPPELGANGER.md) for details.

### SKUGGA_OPENAPI_009
**OpenAPI document error** - Generic validation failure. Check build output for specific issue.

---

## OpenAPI Linting Diagnostics

Spectral-inspired linting rules for OpenAPI quality. Configure via `LintingRules` property. See [DOPPELGANGER.md - OpenAPI Quality Linting](DOPPELGANGER.md#openapi-quality-linting) for full documentation.

### Info Section Rules
- **SKUGGA_LINT_001** (`info-contact`) - API contact information missing
- **SKUGGA_LINT_002** (`info-description`) - API description missing
- **SKUGGA_LINT_003** (`info-license`) - License information missing

### Operation Rules
- **SKUGGA_LINT_004** (`operation-operationId`) - Operation missing unique ID
- **SKUGGA_LINT_005** (`operation-tags`) - Operation missing tags
- **SKUGGA_LINT_006** (`operation-description`) - Operation missing description
- **SKUGGA_LINT_007** (`operation-summary`) - Operation missing summary
- **SKUGGA_LINT_008** (`operation-success-response`) - Missing 200 response
- **SKUGGA_LINT_009** (`operation-success-response`) - Missing 2xx response
- **SKUGGA_LINT_010** (`operation-parameters`) - Parameter missing description

### Path Rules
- **SKUGGA_LINT_011** (`path-parameters`) - Path parameter not defined in operation
- **SKUGGA_LINT_012** (`no-identical-paths`) - Duplicate path patterns detected

### Tag Rules
- **SKUGGA_LINT_013** (`tag-description`) - Tag missing description
- **SKUGGA_LINT_014** (`openapi-tags`) - Referenced tag not defined globally

### Schema Rules
- **SKUGGA_LINT_015** (`typed-enum`) - Enum missing type specification
- **SKUGGA_LINT_016** (`schema-description`) - Schema missing description

### Component Rules
- **SKUGGA_LINT_017** (`no-unused-components`) - Unreferenced schema component detected

**Configuration Example:**
```csharp
[SkuggaFromOpenApi("api.json", LintingRules = "info-license:off,operation-tags:error")]
public partial interface IMyApi { }
```

---

## Migration from Other Libraries

### From Moq
| Moq | Skugga |
|-----|--------|
| `Mock.Of<T>()` | `Mock.Create<T>()` |
| `Mock.Get(obj).Setup(...)` | `mock.Setup(...)` |
| All other APIs identical | -- |

### From NSubstitute
| NSubstitute | Skugga |
|-------------|--------|
| `Substitute.For<T>()` | `Mock.Create<T>()` |
| `sub.Method().Returns(value)` | `mock.Setup(x => x.Method()).Returns(value)` |
| `sub.Received().Method()` | `mock.Verify(x => x.Method(), Times.Once())` |

---

## Support

- [Full Documentation](https://github.com/Digvijay/Skugga)
- [Report Issues](https://github.com/Digvijay/Skugga/issues)
- [Discussions](https://github.com/Digvijay/Skugga/discussions)
