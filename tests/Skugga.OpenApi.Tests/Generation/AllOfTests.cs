using Skugga.Core;
using Xunit;

namespace Skugga.OpenApi.Tests
{
    // Interface for testing allOf support
    [SkuggaFromOpenApi("specs/allof-test.json", SchemaPrefix = "AllOf")]
    public partial interface IAllOfTestApi
    {
    }

    /// <summary>
    /// Tests for allOf schema composition support.
    /// Previously, mocks returned null for allOf schemas.
    /// Now they should merge examples from composed schemas.
    /// </summary>
    public class AllOfTests
    {
        [Fact]
        [Trait("Category", "Generation")]
        public void AllOf_Interface_IsGenerated()
        {
            var interfaceType = typeof(IAllOfTestApi);
            Assert.NotNull(interfaceType);
        }

        [Fact]
        [Trait("Category", "Generation")]
        public void AllOf_Interface_HasMethod()
        {
            var interfaceType = typeof(IAllOfTestApi);
            var methods = interfaceType.GetMethods();
            var methodNames = string.Join(", ", methods.Select(m => m.Name)); Console.WriteLine($"Methods found: {methodNames}");
            Console.WriteLine($"Method count: {methods.Length}");
            var method = interfaceType.GetMethod("GetProduct");

            Assert.NotNull(method); // Will fail with method names in output
        }

        [Fact]
        [Trait("Category", "Generation")]
        public async Task AllOf_Mock_ReturnsNonNull()
        {
            // The mock should return a non-null Product with merged examples
            var mock = new IAllOfTestApiMock();
            var product = await mock.GetProduct(123);

            Assert.NotNull(product);
        }

        [Fact]
        [Trait("Category", "Generation")]
        public async Task AllOf_Mock_HasMergedProperties()
        {
            // The mock should have properties from both BaseEntity and Product
            var mock = new IAllOfTestApiMock();
            var product = await mock.GetProduct(123);

            // From BaseEntity
            Assert.NotEqual(0, product.Id);

            // From Product
            Assert.NotNull(product.Name);
            Assert.NotEqual(0, product.Price);
        }
    }
}
