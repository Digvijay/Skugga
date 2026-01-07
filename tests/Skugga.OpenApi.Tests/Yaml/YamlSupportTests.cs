using System.Linq;
using System.Threading.Tasks;
using Skugga.Core;
using Xunit;

namespace Skugga.OpenApi.Tests.Yaml
{
    [SkuggaFromOpenApi("specs/yaml-simple.yaml", SchemaPrefix = "YamlSimple")]
    public partial interface IYamlSimpleApi { }

    [SkuggaFromOpenApi("specs/yaml-petstore.yml", SchemaPrefix = "YamlPetstore")]
    public partial interface IYamlPetstoreApi { }

    /// <summary>
    /// Tests for YAML OpenAPI specification support.
    /// Validates that Skugga can parse and generate code from both .yaml and .yml files.
    /// </summary>
    public class YamlSupportTests
    {
        [Fact]
        [Trait("Category", "Yaml")]
        public void YamlSimpleApi_InterfaceGenerated()
        {
            // Verify that the interface is generated from YAML spec
            var interfaceType = typeof(IYamlSimpleApi);
            Assert.NotNull(interfaceType);
        }

        [Fact]
        [Trait("Category", "Yaml")]
        public void YamlSimpleApi_HasExpectedMethods()
        {
            // Verify that methods from YAML spec are generated
            var interfaceType = typeof(IYamlSimpleApi);
            var methods = interfaceType.GetMethods();

            Assert.Contains(methods, m => m.Name == "ListUsers");
            Assert.Contains(methods, m => m.Name == "GetUser");
        }

        [Fact]
        [Trait("Category", "Yaml")]
        public async Task YamlSimpleApi_MockReturnsValidData()
        {
            // Create mock from YAML spec - use the OpenAPI-generated mock directly
            var mock = new IYamlSimpleApiMock();

            // Test listUsers method
            var users = await mock.ListUsers(status: "active");
            Assert.NotNull(users);
            Assert.NotEmpty(users);

            var firstUser = users.First();
            Assert.Equal("Alice", firstUser.Name);
            Assert.Equal("alice@example.com", firstUser.Email);
            Assert.Equal("active", firstUser.Status);
        }

        [Fact]
        [Trait("Category", "Yaml")]
        public async Task YamlSimpleApi_EnumParameterWorks()
        {
            // Verify enum parameter from YAML works correctly
            var mock = new IYamlSimpleApiMock();

            var activeUsers = await mock.ListUsers(status: "active");
            Assert.NotNull(activeUsers);

            var inactiveUsers = await mock.ListUsers(status: "inactive");
            Assert.NotNull(inactiveUsers);
        }

        [Fact]
        [Trait("Category", "Yaml")]
        public async Task YamlSimpleApi_PathParameterWorks()
        {
            // Test path parameter from YAML
            var mock = new IYamlSimpleApiMock();

            var user = await mock.GetUser(userId: 1);
            Assert.NotNull(user);
            Assert.Equal(1, user.Id);
            Assert.Equal("Alice", user.Name);
        }

        [Fact]
        [Trait("Category", "Yaml")]
        public void YamlPetstoreApi_InterfaceGenerated()
        {
            // Verify that the interface is generated from YML spec
            var interfaceType = typeof(IYamlPetstoreApi);
            Assert.NotNull(interfaceType);
        }

        [Fact]
        [Trait("Category", "Yaml")]
        public void YamlPetstoreApi_HasExpectedMethods()
        {
            // Verify that methods from YML spec are generated
            var interfaceType = typeof(IYamlPetstoreApi);
            var methods = interfaceType.GetMethods();

            Assert.Contains(methods, m => m.Name == "ListPets");
            Assert.Contains(methods, m => m.Name == "GetPet");
        }

        [Fact]
        [Trait("Category", "Yaml")]
        public async Task YamlPetstoreApi_MockReturnsValidData()
        {
            // Create mock from YML spec
            var mock = new IYamlPetstoreApiMock();

            // Test listPets method
            var pets = await mock.ListPets();
            Assert.NotNull(pets);
            Assert.NotEmpty(pets);

            var firstPet = pets.First();
            Assert.Equal(1, firstPet.Id);
            Assert.Equal("Fluffy", firstPet.Name);
            Assert.Equal("cat", firstPet.Tag);
        }

        [Fact]
        [Trait("Category", "Yaml")]
        public async Task YamlPetstoreApi_GetPetWorks()
        {
            // Test getting a specific pet from YML spec
            var mock = new IYamlPetstoreApiMock();

            var pet = await mock.GetPet(petId: 1);
            Assert.NotNull(pet);
            Assert.Equal(1, pet.Id);
            Assert.Equal("Fluffy", pet.Name);
            Assert.Equal("cat", pet.Tag);
        }

        [Fact]
        [Trait("Category", "Yaml")]
        public void YamlSchemas_GeneratedCorrectly()
        {
            // Verify that schemas from YAML are generated with correct properties
            var userType = typeof(IYamlSimpleApi).Assembly.GetTypes()
                .FirstOrDefault(t => t.Name == "YamlSimple_User");

            Assert.NotNull(userType);
            Assert.NotNull(userType.GetProperty("Id"));
            Assert.NotNull(userType.GetProperty("Name"));
            Assert.NotNull(userType.GetProperty("Email"));
            Assert.NotNull(userType.GetProperty("Status"));
        }

        [Fact]
        [Trait("Category", "Yaml")]
        public void YmlSchemas_GeneratedCorrectly()
        {
            // Verify that schemas from YML are generated with correct properties
            var petType = typeof(IYamlPetstoreApi).Assembly.GetTypes()
                .FirstOrDefault(t => t.Name == "YamlPetstore_Pet");

            Assert.NotNull(petType);
            Assert.NotNull(petType.GetProperty("Id"));
            Assert.NotNull(petType.GetProperty("Name"));
            Assert.NotNull(petType.GetProperty("Tag"));
        }

        [Fact]
        [Trait("Category", "Yaml")]
        public void YamlAndYml_BothExtensionsSupported()
        {
            // Verify both .yaml and .yml extensions work
            var yamlInterface = typeof(IYamlSimpleApi);
            var ymlInterface = typeof(IYamlPetstoreApi);

            Assert.NotNull(yamlInterface);
            Assert.NotNull(ymlInterface);

            // Both should have generated methods
            Assert.NotEmpty(yamlInterface.GetMethods());
            Assert.NotEmpty(ymlInterface.GetMethods());
        }
    }
}
