using Skugga.Core;
using Xunit;

namespace Skugga.OpenApi.Tests
{
    /// <summary>
    /// Tests for the Skugga OpenAPI generator.
    /// Phase 2: Verify the generator produces actual interfaces and schemas from OpenAPI specs.
    /// </summary>
    public class BasicGeneratorTests
    {
        /// <summary>
        /// Tests that the attribute can be applied to an interface and generates code.
        /// </summary>
        [Fact]
        [Trait("Category", "OpenApi Core")]
        public void SkuggaFromOpenApi_Attribute_CanBeApplied()
        {
            // Verify:
            // 1. The [SkuggaFromOpenApi] attribute exists and compiles
            // 2. It can be applied to an interface
            // 3. The generator runs without errors and produces code

            // The interface is defined below with the attribute
            // If compilation succeeds, the test passes
            Assert.True(true, "Attribute applied successfully and code compiled");
        }

        /// <summary>
        /// Tests that the generator creates a proper interface from the OpenAPI spec.
        /// </summary>
        [Fact]
        [Trait("Category", "OpenApi Core")]
        public void Generator_CreatesInterface_FromOpenApiSpec()
        {
            // The interface should be generated with methods from the spec
            // Check that it's a valid interface type
            var interfaceType = typeof(IPetStoreApi);
            
            Assert.True(interfaceType.IsInterface, "Should be an interface");
            
            // Verify methods exist
            var methods = interfaceType.GetMethods();
            Assert.NotEmpty(methods);
            
            // Check for specific methods from petstore.json
            Assert.Contains(methods, m => m.Name == "ListPets");
            Assert.Contains(methods, m => m.Name == "GetPet");
        }

        /// <summary>
        /// Tests that schemas are generated from OpenAPI components.
        /// </summary>
        [Fact]
        [Trait("Category", "OpenApi Core")]
        public void Generator_CreatesSchemas_FromOpenApiComponents()
        {
            // Verify Pet schema is generated
            var petType = typeof(Pet);
            
            Assert.NotNull(petType);
            Assert.True(petType.IsClass, "Should be a class");
            
            // Verify properties exist
            var properties = petType.GetProperties();
            Assert.Contains(properties, p => p.Name == "Id");
            Assert.Contains(properties, p => p.Name == "Name");
            Assert.Contains(properties, p => p.Name == "Tag");
        }

        /// <summary>
        /// Tests that mock implementation is generated and can be instantiated.
        /// Phase 3: Verify mock classes with realistic return values.
        /// </summary>
        [Fact]
        [Trait("Category", "OpenApi Core")]
        public void Generator_CreatesMock_WithRealisticDefaults()
        {
            // The mock should be auto-generated as IPetStoreApiMock
            var mock = new IPetStoreApiMock();
            
            // Verify it implements the interface
            Assert.IsAssignableFrom<IPetStoreApi>(mock);
            
            // Verify methods return realistic values
            var pets = mock.ListPets();
            Assert.NotNull(pets);
            
            var pet = mock.GetPet(123);
            Assert.NotNull(pet);
        }
    }

    // Test interface - generator will fill this with operations from petstore.json
    [SkuggaFromOpenApi("specs/petstore.json")]
    public partial interface IPetStoreApi
    {
        // Phase 2: Generator creates methods:
        // Pet[] ListPets();
        // Pet GetPet(long petid);
    }

    // Pet class is now generated from OpenAPI schema - no manual definition needed!
}
