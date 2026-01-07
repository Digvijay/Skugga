using Skugga.Core;
using Xunit;

namespace Skugga.OpenApi.Tests
{
    /// <summary>
    /// Tests for nested interface support - interfaces defined inside classes.
    /// This was previously unsupported and required interfaces to be top-level.
    /// </summary>
    public partial class NestedInterfaceTests
    {
        // Nested interface should now be supported
        [SkuggaFromOpenApi("specs/petstore.json", SchemaPrefix = "Nested")]
        public partial interface INestedPetStoreApi
        {
        }

        [Fact]
        [Trait("Category", "Integration")]
        public void NestedInterface_CanBeGenerated()
        {
            // The nested interface should be generated correctly
            var interfaceType = typeof(INestedPetStoreApi);
            Assert.NotNull(interfaceType);
        }

        [Fact]
        [Trait("Category", "Integration")]
        public void NestedInterface_HasMethods()
        {
            // Methods should be generated for nested interface
            var interfaceType = typeof(INestedPetStoreApi);
            var methods = interfaceType.GetMethods();

            Assert.NotEmpty(methods);
        }

        [Fact]
        [Trait("Category", "Integration")]
        public void NestedInterface_MockCanBeCreated()
        {
            // Mock should be generated for nested interface
            var mock = new INestedPetStoreApiMock();
            Assert.NotNull(mock);

            // Verify it implements the interface
            INestedPetStoreApi api = mock;
            Assert.NotNull(api);
        }
    }
}
