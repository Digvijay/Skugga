using Skugga.Core;
using System.Threading.Tasks;
using Xunit;

namespace Skugga.OpenApi.Tests.Generation
{
    /// <summary>
    /// Tests for response headers support - operations that return headers alongside response bodies.
    /// </summary>
    public class ResponseHeadersTests
    {
        [Fact]
        [Trait("Category", "Generation")]
        public void ResponseHeaders_Interface_IsGenerated()
        {
            var interfaceType = typeof(IResponseHeadersApi);
            Assert.NotNull(interfaceType);
        }

        [Fact]
        [Trait("Category", "Generation")]
        public void GetUser_Method_Exists()
        {
            var interfaceType = typeof(IResponseHeadersApi);
            var method = interfaceType.GetMethod("GetUser");
            
            Assert.NotNull(method);
        }

        [Fact]
        [Trait("Category", "Generation")]
        public void ListProducts_Method_Exists()
        {
            var interfaceType = typeof(IResponseHeadersApi);
            var method = interfaceType.GetMethod("ListProducts");
            
            Assert.NotNull(method);
        }

        [Fact]
        [Trait("Category", "Generation")]
        public async Task GetUser_Mock_ReturnsNonNull()
        {
            var mock = new IResponseHeadersApiMock();
            var result = await mock.GetUser(123);
            
            Assert.NotNull(result);
            Assert.NotNull(result.Body);
            Assert.NotNull(result.Headers);
        }

        [Fact]
        [Trait("Category", "Generation")]
        public async Task ListProducts_Mock_ReturnsNonNull()
        {
            var mock = new IResponseHeadersApiMock();
            var result = await mock.ListProducts();
            
            Assert.NotNull(result);
            Assert.NotNull(result.Body);
            Assert.NotNull(result.Headers);
        }

        [Fact]
        [Trait("Category", "Generation")]
        public async Task GetUser_Mock_ReturnsRateLimitHeaders()
        {
            var mock = new IResponseHeadersApiMock();
            var response = await mock.GetUser(123);
            
            Assert.NotNull(response.Body);
            Assert.NotNull(response.Headers);
            Assert.True(response.Headers.ContainsKey("X-RateLimit-Limit"));
            Assert.Equal("1000", response.Headers["X-RateLimit-Limit"]);
            Assert.True(response.Headers.ContainsKey("X-RateLimit-Remaining"));
            Assert.Equal("999", response.Headers["X-RateLimit-Remaining"]);
            Assert.True(response.Headers.ContainsKey("X-RateLimit-Reset"));
            Assert.Equal("1704110400", response.Headers["X-RateLimit-Reset"]);
        }

        [Fact]
        [Trait("Category", "Generation")]
        public async Task ListProducts_Mock_ReturnsETagHeader()
        {
            var mock = new IResponseHeadersApiMock();
            var response = await mock.ListProducts();
            
            Assert.NotNull(response.Headers);
            Assert.True(response.Headers.ContainsKey("ETag"));
            Assert.Equal("\"products-v1\"", response.Headers["ETag"]);
            Assert.True(response.Headers.ContainsKey("X-Total-Count"));
            Assert.Equal("42", response.Headers["X-Total-Count"]);
        }

        [Fact]
        [Trait("Category", "Generation")]
        public async Task GetUser_Mock_ReturnsValidUserData()
        {
            var mock = new IResponseHeadersApiMock();
            var response = await mock.GetUser(123);
            
            Assert.NotNull(response.Body);
            Assert.Equal(123, response.Body.Id);
            Assert.Equal("John Doe", response.Body.Name);
            Assert.Equal("john@example.com", response.Body.Email);
        }

        // TODO: Remove commented tests since they're now implemented
        // [Fact]
        // public async Task GetUser_Mock_ReturnsResponseWithHeaders()
        // {
        //     var mock = new IResponseHeadersApiMock();
        //     var response = await mock.GetUser(123);
        //     
        //     Assert.NotNull(response.Body);
        //     Assert.NotNull(response.Headers);
        //     Assert.True(response.Headers.ContainsKey("X-RateLimit-Limit"));
        //     Assert.Equal("1000", response.Headers["X-RateLimit-Limit"]);
        // }
    }

    #region Test Interface

    [SkuggaFromOpenApi("specs/response-headers.json")]
    public partial interface IResponseHeadersApi { }

    #endregion
}
