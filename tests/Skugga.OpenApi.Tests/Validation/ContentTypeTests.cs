using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Skugga.Core;
using Xunit;

namespace Skugga.OpenApi.Tests
{
    /// <summary>
    /// Tests for content type handling priority chain.
    /// Verifies that the generator correctly prioritizes content types:
    /// 1. application/json (standard)
    /// 2. application/*+json (vendor-specific like vnd.api+json, hal+json)
    /// 3. text/json (legacy)
    /// 4. Any content with "json" in name
    /// 5. First content with schema (Swagger 2.0 fallback)
    /// </summary>
    public class ContentTypeTests
    {
        #region Content Type Priority Tests

        [Fact]
        [Trait("Category", "Validation")]
        public void ContentType_Standard_ApplicationJson_IsPreferred()
        {
            // Standard application/json should be used when available
            var interfaceType = typeof(IContentTypeApi);
            var method = interfaceType.GetMethod("GetStandard");

            Assert.NotNull(method);

            // Should return Task<ContentTypeUser>
            Assert.True(method.ReturnType.IsGenericType);
            var innerType = method.ReturnType.GetGenericArguments()[0];
            Assert.Equal("ContentTypeUser", innerType.Name);
        }

        [Fact]
        [Trait("Category", "Validation")]
        public void ContentType_VendorJson_IsSupported()
        {
            // Vendor-specific JSON formats (application/vnd.api+json) should work
            var interfaceType = typeof(IContentTypeApi);
            var method = interfaceType.GetMethod("GetVendorJson");

            Assert.NotNull(method);

            // Should return Task<ContentTypeUser>
            Assert.True(method.ReturnType.IsGenericType);
            var innerType = method.ReturnType.GetGenericArguments()[0];
            Assert.Equal("ContentTypeUser", innerType.Name);
        }

        [Fact]
        [Trait("Category", "Validation")]
        public void ContentType_HalJson_IsSupported()
        {
            // HAL+JSON format (application/hal+json) should be recognized
            var interfaceType = typeof(IContentTypeApi);
            var method = interfaceType.GetMethod("GetHalJson");

            Assert.NotNull(method);

            // Should return Task<ContentTypeUser>
            Assert.True(method.ReturnType.IsGenericType);
            var innerType = method.ReturnType.GetGenericArguments()[0];
            Assert.Equal("ContentTypeUser", innerType.Name);
        }

        [Fact]
        [Trait("Category", "Validation")]
        public void ContentType_TextJson_Legacy_IsSupported()
        {
            // Legacy text/json format should be handled
            var interfaceType = typeof(IContentTypeApi);
            var method = interfaceType.GetMethod("GetTextJson");

            Assert.NotNull(method);

            // Should return Task<ContentTypeUser>
            Assert.True(method.ReturnType.IsGenericType);
            var innerType = method.ReturnType.GetGenericArguments()[0];
            Assert.Equal("ContentTypeUser", innerType.Name);
        }

        [Fact]
        [Trait("Category", "Validation")]
        public void ContentType_MixedPriority_PrefersApplicationJson()
        {
            // When multiple content types exist, application/json should be preferred
            // even if listed after other types in the spec
            var interfaceType = typeof(IContentTypeApi);
            var method = interfaceType.GetMethod("GetMixedPriority");

            Assert.NotNull(method);

            // Should return Task<ContentTypeUser> (not null/object)
            Assert.True(method.ReturnType.IsGenericType);
            var innerType = method.ReturnType.GetGenericArguments()[0];
            Assert.Equal("ContentTypeUser", innerType.Name);
        }

        [Fact]
        [Trait("Category", "Validation")]
        public void ContentType_GenericJson_IsRecognized()
        {
            // Any content type containing "json" should be recognized
            var interfaceType = typeof(IContentTypeApi);
            var method = interfaceType.GetMethod("GetGenericJson");

            Assert.NotNull(method);

            // Should return Task<ContentTypeUser>
            Assert.True(method.ReturnType.IsGenericType);
            var innerType = method.ReturnType.GetGenericArguments()[0];
            Assert.Equal("ContentTypeUser", innerType.Name);
        }

        [Fact]
        [Trait("Category", "Validation")]
        public void ContentType_Fallback_FirstWithSchema()
        {
            // When no JSON content type exists, fallback to first content with schema
            var interfaceType = typeof(IContentTypeApi);
            var method = interfaceType.GetMethod("GetFallback");

            Assert.NotNull(method);

            // Should still return Task<ContentTypeUser> (from XML schema)
            Assert.True(method.ReturnType.IsGenericType);
            var innerType = method.ReturnType.GetGenericArguments()[0];
            Assert.Equal("ContentTypeUser", innerType.Name);
        }

        #endregion

        #region Mock Content Type Tests

        [Fact]
        [Trait("Category", "Validation")]
        public async System.Threading.Tasks.Task Mock_Standard_ApplicationJson_ReturnsValidData()
        {
            var mock = new IContentTypeApiMock();
            var user = await mock.GetStandard();

            Assert.NotNull(user);
        }

        [Fact]
        [Trait("Category", "Validation")]
        public async System.Threading.Tasks.Task Mock_VendorJson_ReturnsValidData()
        {
            var mock = new IContentTypeApiMock();
            var user = await mock.GetVendorJson();

            Assert.NotNull(user);
        }

        [Fact]
        [Trait("Category", "Validation")]
        public async System.Threading.Tasks.Task Mock_HalJson_ReturnsValidData()
        {
            var mock = new IContentTypeApiMock();
            var user = await mock.GetHalJson();

            Assert.NotNull(user);
        }

        [Fact]
        [Trait("Category", "Validation")]
        public async System.Threading.Tasks.Task Mock_TextJson_ReturnsValidData()
        {
            var mock = new IContentTypeApiMock();
            var user = await mock.GetTextJson();

            Assert.NotNull(user);
        }

        [Fact]
        [Trait("Category", "Validation")]
        public async System.Threading.Tasks.Task Mock_MixedPriority_ReturnsValidData()
        {
            var mock = new IContentTypeApiMock();
            var user = await mock.GetMixedPriority();

            Assert.NotNull(user);
        }

        [Fact]
        [Trait("Category", "Validation")]
        public async System.Threading.Tasks.Task Mock_AllContentTypes_ReturnSameUserType()
        {
            // All methods should return the same ContentTypeUser type regardless of content type
            var mock = new IContentTypeApiMock();

            var standard = await mock.GetStandard();
            var vendor = await mock.GetVendorJson();
            var hal = await mock.GetHalJson();
            var text = await mock.GetTextJson();
            var mixed = await mock.GetMixedPriority();

            // All should be ContentTypeUser type
            Assert.NotNull(standard);
            Assert.NotNull(vendor);
            Assert.NotNull(hal);
            Assert.NotNull(text);
            Assert.NotNull(mixed);

            Assert.Equal(standard.GetType(), vendor.GetType());
            Assert.Equal(standard.GetType(), hal.GetType());
            Assert.Equal(standard.GetType(), text.GetType());
            Assert.Equal(standard.GetType(), mixed.GetType());
        }

        #endregion

        #region Content Type Schema Generation Tests

        [Fact]
        [Trait("Category", "Validation")]
        public void ContentType_AllMethods_GenerateUserSchema()
        {
            // Verify that User schema is generated and shared across all methods
            var interfaceType = typeof(IContentTypeApi);
            var methods = interfaceType.GetMethods();

            // All methods should return Task<ContentTypeUser>
            foreach (var method in methods)
            {
                Assert.True(method.ReturnType.IsGenericType);
                var innerType = method.ReturnType.GetGenericArguments()[0];
                Assert.Equal("ContentTypeUser", innerType.Name);
            }
        }

        [Fact]
        [Trait("Category", "Validation")]
        public async System.Threading.Tasks.Task ContentType_ContentTypeUserSchema_HasCorrectProperties()
        {
            // Verify ContentTypeUser schema has expected properties from spec
            var mock = new IContentTypeApiMock();
            var user = await mock.GetStandard();

            Assert.NotNull(user);

            var userType = user.GetType();
            var properties = userType.GetProperties();

            // Should have id, name, email properties
            Assert.Contains(properties, p => p.Name == "Id");
            Assert.Contains(properties, p => p.Name == "Name");
            Assert.Contains(properties, p => p.Name == "Email");
        }

        #endregion
    }

    #region Test Interface

    // Spec has various content type variations to test priority chain
    [SkuggaFromOpenApi("specs/content-type-variants.json")]
    public partial interface IContentTypeApi
    {
        // Generated methods with different content types:
        // Task<ContentTypeUser> GetStandard(); // application/json
        // Task<ContentTypeUser> GetVendorJson(); // application/vnd.api+json
        // Task<ContentTypeUser> GetHalJson(); // application/hal+json
        // Task<ContentTypeUser> GetTextJson(); // text/json
        // Task<ContentTypeUser> GetMixedPriority(); // multiple types, prefers application/json
        // Task<ContentTypeUser> GetGenericJson(); // something/json
        // Task<ContentTypeUser> GetFallback(); // application/xml (fallback)
    }

    #endregion
}
