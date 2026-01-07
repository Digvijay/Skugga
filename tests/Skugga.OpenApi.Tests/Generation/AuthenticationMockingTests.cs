using System;
using System.Linq;
using System.Reflection;
using Skugga.OpenApi.Tests.Generation.AuthDisabled;
using Skugga.OpenApi.Tests.Generation.AuthEnabled;
using Xunit;

namespace Skugga.OpenApi.Tests.Generation
{
    /// <summary>
    /// Tests for Authentication Mocking.
    /// Validates token generation, auth validation, and security test scenarios.
    /// </summary>
    public class AuthenticationMockingTests
    {
        [Fact]
        [Trait("Category", "Authentication")]
        public void AuthEnabled_Interface_IsGenerated()
        {
            var assembly = typeof(IAuthEnabledApi).Assembly;
            var mockType = assembly.GetType("Skugga.OpenApi.Tests.Generation.AuthEnabled.IAuthEnabledApiMock");

            Assert.NotNull(mockType);
            Assert.Contains(mockType.GetInterfaces(), i => i.Name == "IAuthEnabledApi");
        }

        [Fact]
        [Trait("Category", "Authentication")]
        public void AuthEnabled_Mock_HasConfigureSecurityMethod()
        {
            var assembly = typeof(IAuthEnabledApi).Assembly;
            var mockType = assembly.GetType("Skugga.OpenApi.Tests.Generation.AuthEnabled.IAuthEnabledApiMock");

            Assert.NotNull(mockType);

            var configMethod = mockType.GetMethod("ConfigureSecurity");
            Assert.NotNull(configMethod);

            // Verify parameters
            var parameters = configMethod.GetParameters();
            Assert.Contains(parameters, p => p.Name == "tokenExpired");
            Assert.Contains(parameters, p => p.Name == "tokenInvalid");
            Assert.Contains(parameters, p => p.Name == "credentialsRevoked");
        }

        [Fact]
        [Trait("Category", "Authentication")]
        public void AuthEnabled_Mock_HasGenerateAccessTokenMethod()
        {
            var assembly = typeof(IAuthEnabledApi).Assembly;
            var mockType = assembly.GetType("Skugga.OpenApi.Tests.Generation.AuthEnabled.IAuthEnabledApiMock");

            Assert.NotNull(mockType);

            var tokenMethod = mockType.GetMethod("GenerateAccessToken");
            Assert.NotNull(tokenMethod);
            Assert.Equal(typeof(string), tokenMethod.ReturnType);
        }

        [Fact]
        [Trait("Category", "Authentication")]
        public void AuthEnabled_Mock_HasGenerateApiKeyMethod()
        {
            var assembly = typeof(IAuthEnabledApi).Assembly;
            var mockType = assembly.GetType("Skugga.OpenApi.Tests.Generation.AuthEnabled.IAuthEnabledApiMock");

            Assert.NotNull(mockType);

            var apiKeyMethod = mockType.GetMethod("GenerateApiKey");
            Assert.NotNull(apiKeyMethod);
            Assert.Equal(typeof(string), apiKeyMethod.ReturnType);
        }

        [Fact]
        [Trait("Category", "Authentication")]
        public void AuthEnabled_Mock_HasGenerateBasicAuthMethod()
        {
            var assembly = typeof(IAuthEnabledApi).Assembly;
            var mockType = assembly.GetType("Skugga.OpenApi.Tests.Generation.AuthEnabled.IAuthEnabledApiMock");

            Assert.NotNull(mockType);

            var basicAuthMethod = mockType.GetMethod("GenerateBasicAuth");
            Assert.NotNull(basicAuthMethod);
            Assert.Equal(typeof(string), basicAuthMethod.ReturnType);
        }

        [Fact]
        [Trait("Category", "Authentication")]
        public void AuthEnabled_GenerateAccessToken_ReturnsValidBearerToken()
        {
            var mock = new IAuthEnabledApiMock();

            var token = mock.GenerateAccessToken();

            Assert.NotNull(token);
            Assert.StartsWith("Bearer ", token);

            // Verify JWT format (header.payload.signature)
            var jwtPart = token.Substring("Bearer ".Length);
            var parts = jwtPart.Split('.');
            Assert.Equal(3, parts.Length);
        }

        [Fact]
        [Trait("Category", "Authentication")]
        public void AuthEnabled_GenerateApiKey_ReturnsValidKey()
        {
            var mock = new IAuthEnabledApiMock();

            var apiKey = mock.GenerateApiKey();

            Assert.NotNull(apiKey);
            Assert.StartsWith("sk_test_", apiKey);
            Assert.True(apiKey.Length > 15);
        }

        [Fact]
        [Trait("Category", "Authentication")]
        public void AuthEnabled_GenerateBasicAuth_ReturnsValidCredentials()
        {
            var mock = new IAuthEnabledApiMock();

            var basicAuth = mock.GenerateBasicAuth();

            Assert.NotNull(basicAuth);
            Assert.StartsWith("Basic ", basicAuth);

            // Verify it's base64 encoded
            var encoded = basicAuth.Substring("Basic ".Length);
            var decoded = System.Text.Encoding.UTF8.GetString(System.Convert.FromBase64String(encoded));
            Assert.Contains(":", decoded);
        }

        [Fact]
        [Trait("Category", "Authentication")]
        public async System.Threading.Tasks.Task AuthEnabled_PublicEndpoint_WorksWithoutAuth()
        {
            var mock = new IAuthEnabledApiMock();

            // Public endpoint should work without auth configuration
            var result = await mock.GetHealth();

            Assert.NotNull(result);
        }

        [Fact]
        [Trait("Category", "Authentication")]
        public async System.Threading.Tasks.Task AuthEnabled_SecuredEndpoint_WorksWithValidAuth()
        {
            var mock = new IAuthEnabledApiMock();

            // Valid auth (default state)
            var user = await mock.GetCurrentUser();

            Assert.NotNull(user);
        }

        [Fact]
        [Trait("Category", "Authentication")]
        public async System.Threading.Tasks.Task AuthEnabled_SecuredEndpoint_ThrowsWhenTokenExpired()
        {
            var mock = new IAuthEnabledApiMock();

            // Configure expired token
            mock.ConfigureSecurity(tokenExpired: true);

            // Should throw UnauthorizedAccessException
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => mock.GetCurrentUser());
        }

        [Fact]
        [Trait("Category", "Authentication")]
        public async System.Threading.Tasks.Task AuthEnabled_SecuredEndpoint_ThrowsWhenTokenInvalid()
        {
            var mock = new IAuthEnabledApiMock();

            // Configure invalid token
            mock.ConfigureSecurity(tokenInvalid: true);

            // Should throw UnauthorizedAccessException
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => mock.ListProducts());
        }

        [Fact]
        [Trait("Category", "Authentication")]
        public async System.Threading.Tasks.Task AuthEnabled_SecuredEndpoint_ThrowsWhenCredentialsRevoked()
        {
            var mock = new IAuthEnabledApiMock();

            // Configure revoked credentials
            mock.ConfigureSecurity(credentialsRevoked: true);

            // Should throw UnauthorizedAccessException
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => mock.ListAllUsers());
        }

        [Fact]
        [Trait("Category", "Authentication")]
        public async System.Threading.Tasks.Task AuthEnabled_CanResetAuthState()
        {
            var mock = new IAuthEnabledApiMock();

            // Configure expired token
            mock.ConfigureSecurity(tokenExpired: true);

            // Should throw
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => mock.GetCurrentUser());

            // Reset to valid auth
            mock.ConfigureSecurity(tokenExpired: false);

            // Should work now
            var user = await mock.GetCurrentUser();
            Assert.NotNull(user);
        }

        [Fact]
        [Trait("Category", "Authentication")]
        public void AuthDisabled_Mock_DoesNotHaveAuthMethods()
        {
            var assembly = typeof(IAuthDisabledApi).Assembly;
            var mockType = assembly.GetType("Skugga.OpenApi.Tests.Generation.AuthDisabled.IAuthDisabledApiMock");

            Assert.NotNull(mockType);

            // Should not have auth methods when AutomaticallyHandleAuth = false (default)
            var configMethod = mockType.GetMethod("ConfigureSecurity");
            Assert.Null(configMethod);

            var tokenMethod = mockType.GetMethod("GenerateAccessToken");
            Assert.Null(tokenMethod);
        }

        [Fact]
        [Trait("Category", "Authentication")]
        public async System.Threading.Tasks.Task AuthDisabled_SecuredEndpoints_WorkWithoutValidation()
        {
            var mock = new IAuthDisabledApiMock();

            // Should work without any auth validation
            var user = await mock.GetCurrentUser();
            Assert.NotNull(user);

            var products = await mock.ListProducts();
            Assert.NotNull(products);

            var users = await mock.ListAllUsers();
            Assert.NotNull(users);
        }

        [Fact]
        [Trait("Category", "Authentication")]
        public async System.Threading.Tasks.Task AuthEnabled_MultipleSecuritySchemes_WorksCorrectly()
        {
            var mock = new IAuthEnabledApiMock();

            // Endpoint with multiple auth options (BearerAuth OR ApiKeyAuth)
            var resource = await mock.GetMixedResource();

            Assert.NotNull(resource);
        }

        [Fact]
        [Trait("Category", "Authentication")]
        public async System.Threading.Tasks.Task AuthEnabled_ExceptionMessage_ContainsUsefulInfo()
        {
            var mock = new IAuthEnabledApiMock();

            // Configure expired token
            mock.ConfigureSecurity(tokenExpired: true);

            var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() => mock.GetCurrentUser());

            Assert.Contains("Token expired", exception.Message);
        }
    }
}
