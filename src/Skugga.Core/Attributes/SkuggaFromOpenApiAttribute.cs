#nullable enable
using System;

namespace Skugga.Core
{
    /// <summary>
    /// Marks an interface to be auto-generated from an OpenAPI (Swagger) specification.
    /// Skugga will generate the interface definition and mock implementation with realistic defaults
    /// from the OpenAPI spec's examples and schemas.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This attribute enables "Contract-First Testing" where your mocks are always in sync with
    /// the actual API contract. When the OpenAPI spec changes, your code will fail to compile
    /// rather than passing tests with outdated mocks.
    /// </para>
    /// <para>
    /// The source can be:
    /// - **Absolute file path**: /Users/me/specs/stripe.json
    /// - **Relative file path**: ../specs/stripe.json (relative to project directory)
    /// - **HTTP/HTTPS URL**: https://api.stripe.com/v1/swagger.json
    /// - **Local resource**: stripe-api.json (in project root)
    /// </para>
    /// <para>
    /// For URLs, Skugga caches the spec in obj/skugga-openapi-cache/ and uses ETags
    /// to avoid unnecessary downloads. Cached specs are reused on offline builds.
    /// </para>
    /// <para>
    /// Example usage:
    /// <code>
    /// [SkuggaFromOpenApi("https://api.stripe.com/v1/swagger.json")]
    /// public partial interface IStripeClient { }
    /// 
    /// // In your test:
    /// var mock = Mock.Create&lt;IStripeClient&gt;();
    /// var invoice = mock.GetInvoice("inv_123"); // Returns realistic data from spec
    /// </code>
    /// </para>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]
    public sealed class SkuggaFromOpenApiAttribute : Attribute
    {
        /// <summary>
        /// Gets the source location of the OpenAPI specification.
        /// Can be a local file path (absolute or relative) or HTTP/HTTPS URL.
        /// </summary>
        public string Source { get; }

        /// <summary>
        /// Gets or sets a filter to generate only specific operations by tags.
        /// Use comma-separated tag names (e.g., "payments,invoices").
        /// Default is null (generate all operations).
        /// </summary>
        /// <example>
        /// [SkuggaFromOpenApi("stripe.json", OperationFilter = "payments,subscriptions")]
        /// </example>
        public string? OperationFilter { get; set; }

        /// <summary>
        /// Gets or sets the name of the example set to use for default values.
        /// If the OpenAPI spec has multiple named examples, this specifies which to use.
        /// Default is null (use first example or generate from schema).
        /// </summary>
        public string? UseExampleSet { get; set; }

        /// <summary>
        /// Gets or sets whether to validate mock return values against OpenAPI schemas at compile-time.
        /// Default is false for performance, but recommended for strict contract testing.
        /// </summary>
        public bool ValidateSchemas { get; set; }

        /// <summary>
        /// Gets or sets the cache directory for downloaded OpenAPI specs.
        /// Default is "obj/skugga-openapi-cache" relative to project directory.
        /// </summary>
        public string? CachePath { get; set; }

        /// <summary>
        /// Gets or sets whether to generate async methods (Task&lt;T&gt; return types).
        /// Default is true for modern async/await patterns.
        /// Set to false for synchronous APIs or when wrapping sync operations.
        /// </summary>
        /// <remarks>
        /// When true: generates Task&lt;Pet[]&gt; Listpets()
        /// When false: generates Pet[] Listpets()
        /// </remarks>
        public bool GenerateAsync { get; set; } = true;

        /// <summary>
        /// Gets or sets a custom prefix for generated schema class names.
        /// Useful when multiple interfaces in the same namespace use specs with identical schema names.
        /// Default is null (no prefix, uses simple schema names like "Pet", "Product").
        /// </summary>
        /// <example>
        /// [SkuggaFromOpenApi("petstore.json", SchemaPrefix = "Store")]
        /// // Generates: Store_Pet, Store_Order instead of Pet, Order
        /// </example>
        public string? SchemaPrefix { get; set; }

        /// <summary>
        /// Gets or sets the linting rule configuration for OpenAPI quality checks.
        /// Format: "rule1:off,rule2:error,rule3:warn" to customize severity or disable rules.
        /// Default is null (all rules enabled with default severities).
        /// </summary>
        /// <remarks>
        /// <para>
        /// Spectral-inspired rules include:
        /// - info-contact, info-description, info-license (Info section quality)
        /// - operation-operationId, operation-tags, operation-success-response (Operation quality)
        /// - path-parameters, no-identical-paths (Path validation)
        /// - tag-description, openapi-tags (Tag organization)
        /// - typed-enum, schema-description (Schema quality)
        /// - no-unused-components (Component cleanup)
        /// </para>
        /// </remarks>
        /// <example>
        /// [SkuggaFromOpenApi("api.json", LintingRules = "info-license:off,operation-tags:error")]
        /// // Disables info-license check, escalates operation-tags to error
        /// </example>
        public string? LintingRules { get; set; }

        /// <summary>
        /// Gets or sets whether to enable stateful behavior for CRUD operations.
        /// When enabled, mocks maintain state across calls (create/read/update/delete).
        /// Default is false (stateless mocks with fixed return values).
        /// </summary>
        /// <remarks>
        /// <para><strong>Enhancement : Stateful Mocks</strong></para>
        /// <para>
        /// When enabled, the mock maintains an in-memory state store that:
        /// - Tracks entities created via POST operations
        /// - Returns stored entities via GET operations
        /// - Updates entities via PUT/PATCH operations
        /// - Deletes entities via DELETE operations
        /// - Generates sequential IDs for new entities
        /// - Throws 404 errors for non-existent entities
        /// </para>
        /// <para>
        /// Each mock instance has independent state, enabling test isolation.
        /// State can be reset between tests using ResetState() method.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// [SkuggaFromOpenApi("api.json", StatefulBehavior = true)]
        /// public partial interface IUserApi { }
        /// 
        /// var mock = Mock.Create&lt;IUserApi&gt;();
        /// 
        /// // POST creates user with ID 1
        /// var user = mock.CreateUser(new User { Name = "Alice" });
        /// Assert.Equal(1, user.Id);
        /// 
        /// // GET retrieves created user
        /// var retrieved = mock.GetUser(1);
        /// Assert.Equal("Alice", retrieved.Name);
        /// 
        /// // PUT updates user
        /// mock.UpdateUser(1, new User { Name = "Alice Updated" });
        /// 
        /// // DELETE removes user
        /// mock.DeleteUser(1);
        /// Assert.Throws&lt;NotFoundException&gt;(() => mock.GetUser(1));
        /// </code>
        /// </example>
        public bool StatefulBehavior { get; set; }

        /// <summary>
        /// Gets or sets whether to enable runtime contract verification for API responses.
        /// When enabled, validates that responses match the OpenAPI schema at runtime.
        /// Default is false (no runtime validation).
        /// </summary>
        /// <remarks>
        /// <para><strong>Enhancement : Runtime Contract Verification</strong></para>
        /// <para>
        /// When enabled, the generated client validates all responses against the OpenAPI schema:
        /// - Response structure matches schema definitions
        /// - Required fields are present
        /// - Field types are correct (string, int, boolean, etc.)
        /// - Enum values are valid
        /// - Nested object structures are valid
        /// - Array items match item schema
        /// </para>
        /// <para>
        /// Throws ContractViolationException when validation fails.
        /// Useful for integration tests, contract testing, and detecting breaking changes.
        /// Performance overhead is typically &lt; 5ms per call.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// [SkuggaFromOpenApi("api.json", ValidateContracts = true)]
        /// public partial interface IStripeApi { }
        /// 
        /// var client = new StripeClient();
        /// 
        /// // Automatically validates response matches schema
        /// var invoice = await client.GetInvoice("inv_123");
        /// 
        /// // If API returns { id: 123 } instead of { id: "inv_123" }
        /// // Throws: ContractViolationException: Field 'id' expected type 'string', got 'integer'
        /// </code>
        /// </example>
        public bool ValidateContracts { get; set; }

        /// <summary>
        /// Gets or sets whether to automatically handle authentication in mock operations.
        /// When enabled, generates token generation, validation, and auth flow mocking.
        /// Default is false (no authentication handling).
        /// </summary>
        /// <remarks>
        /// <para><strong>Feature: Authentication Mocking</strong></para>
        /// <para>
        /// When enabled, the mock automatically handles authentication based on security schemes
        /// defined in the OpenAPI spec (document.components.securitySchemes):
        /// - **OAuth2**: Generates access tokens, refresh tokens, handles expiration
        /// - **Bearer JWT**: Generates valid JWT tokens with configurable claims
        /// - **ApiKey**: Generates and validates API keys (header, query, cookie)
        /// - **HTTP Basic/Bearer**: Validates credentials
        /// </para>
        /// <para>
        /// The mock exposes a ConfigureSecurity() method to control test scenarios:
        /// - Token expiration (for testing expired token handling)
        /// - Invalid tokens (for testing error paths)
        /// - Multiple auth schemes (for testing scheme priority)
        /// - Revoked credentials (for testing revocation)
        /// </para>
        /// <para>
        /// Operations with security requirements automatically validate authentication.
        /// Throws UnauthorizedException (401) when auth fails.
        /// Useful for testing authentication flows, error handling, and security policies.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// [SkuggaFromOpenApi("api.json", AutomaticallyHandleAuth = true)]
        /// public partial interface IGitHubApi { }
        /// 
        /// var mock = new IGitHubApiMock();
        /// 
        /// // Configure test scenario: expired token
        /// mock.ConfigureSecurity(tokenExpired: true);
        /// 
        /// // This will throw UnauthorizedException
        /// await Assert.ThrowsAsync&lt;UnauthorizedException&gt;(() =&gt; 
        ///     mock.GetRepository("owner", "repo"));
        /// 
        /// // Reset to valid token
        /// mock.ConfigureSecurity(tokenExpired: false);
        /// var repo = await mock.GetRepository("owner", "repo"); // Works
        /// 
        /// // Access generated tokens for assertions
        /// var token = mock.GenerateAccessToken(); // "Bearer eyJ..."
        /// Assert.StartsWith("Bearer ", token);
        /// </code>
        /// </example>
        public bool AutomaticallyHandleAuth { get; set; }

        /// <summary>
        /// Initializes a new instance of the SkuggaFromOpenApiAttribute.
        /// </summary>
        /// <param name="source">
        /// The source location of the OpenAPI specification.
        /// Can be a local file path (absolute or relative) or HTTP/HTTPS URL.
        /// </param>
        /// <exception cref="ArgumentNullException">Thrown if source is null or empty.</exception>
        public SkuggaFromOpenApiAttribute(string source)
        {
            if (string.IsNullOrWhiteSpace(source))
                throw new ArgumentNullException(nameof(source), "OpenAPI source cannot be null or empty");

            Source = source;
        }
    }
}
