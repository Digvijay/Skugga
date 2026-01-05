using Skugga.Core;

namespace Skugga.OpenApi.Tests.Generation.AuthEnabled
{
    /// <summary>
    /// Test interface with authentication mocking enabled.
    /// Demonstrates auth token generation, validation, and error scenarios.
    /// </summary>
    [SkuggaFromOpenApi("specs/auth-test.yaml", AutomaticallyHandleAuth = true, SchemaPrefix = "AuthEnabled")]
    public partial interface IAuthEnabledApi
    {
    }
}

namespace Skugga.OpenApi.Tests.Generation.AuthDisabled
{
    /// <summary>
    /// Test interface with authentication mocking disabled (default behavior).
    /// No auth validation or token generation.
    /// </summary>
    [SkuggaFromOpenApi("specs/auth-test.yaml", SchemaPrefix = "AuthDisabled")]
    public partial interface IAuthDisabledApi
    {
    }
}
