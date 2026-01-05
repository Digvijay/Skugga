using Skugga.Core;
using Xunit;

namespace Skugga.OpenApi.Tests
{
    // Interface for testing Swagger 2.0 / OpenAPI 2.0 support
    [SkuggaFromOpenApi("specs/swagger2-test.json")]
    public partial interface ISwagger2TestApi
    {
    }

    /// <summary>
    /// Tests for OpenAPI 2.0 (Swagger) support.
    /// Microsoft.OpenApi library automatically converts 2.0 to 3.0 during parsing.
    /// </summary>
    public class Swagger2Tests
    {
        [Fact]
        [Trait("Category", "Integration")]
        public void Swagger2_Interface_IsGenerated()
        {
            var interfaceType = typeof(ISwagger2TestApi);
            Assert.NotNull(interfaceType);
        }

        [Fact]
        [Trait("Category", "Integration")]
        public void Swagger2_Interface_HasMethods()
        {
            var interfaceType = typeof(ISwagger2TestApi);
            var methods = interfaceType.GetMethods();
            
            // Should have GetUser and CreateUser methods
            Assert.NotEmpty(methods);
            Assert.True(methods.Length >= 2, $"Expected at least 2 methods, found {methods.Length}");
            
            var getUserMethod = interfaceType.GetMethod("GetUser");
            var createUserMethod = interfaceType.GetMethod("CreateUser");
            
            Assert.NotNull(getUserMethod);
            Assert.NotNull(createUserMethod);
        }

        [Fact]
        [Trait("Category", "Integration")]
        public void Swagger2_Mock_CanBeCreated()
        {
            var mock = new ISwagger2TestApiMock();
            Assert.NotNull(mock);
            
            // Verify it implements the interface
            ISwagger2TestApi api = mock;
            Assert.NotNull(api);
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task Swagger2_Mock_ReturnsValidData()
        {
            var mock = new ISwagger2TestApiMock();
            
            // Test GET method
            var user = await mock.GetUser("user123");
            Assert.NotNull(user);
            
            // Test POST method - pass the user object, not the Task
            var newUser = await mock.CreateUser(user);
            Assert.NotNull(newUser);
        }
    }
}
