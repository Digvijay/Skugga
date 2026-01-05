using Skugga.Core;
using System.Threading.Tasks;
using Xunit;

namespace Skugga.OpenApi.Tests.Generation
{
    /// <summary>
    /// Tests for UseExampleSet attribute property - selecting specific named examples from OpenAPI specs.
    /// </summary>
    public class ExampleSetTests
    {
        #region Default Behavior (No UseExampleSet)

        [Fact]
        [Trait("Category", "Generation")]
        public async Task NoExampleSet_UsesFirstExample()
        {
            // When UseExampleSet is not specified, should use first available example
            var mock = new IExampleSetDefaultApiMock();
            var user = await mock.GetUserById(1);
            
            Assert.NotNull(user);
            // First example is "success" - should have Alice's data
            Assert.Equal(123, user.Id);
            Assert.Equal("Alice Success", user.Name);
            Assert.Equal("alice@success.com", user.Email);
            Assert.Equal("active", user.Status);
        }

        #endregion

        #region Named Example Selection

        [Fact]
        [Trait("Category", "Generation")]
        public async Task UseExampleSet_Success_SelectsSuccessExample()
        {
            // Should select the "success" named example
            var mock = new IExampleSetSuccessApiMock();
            var user = await mock.GetUserById(1);
            
            Assert.NotNull(user);
            Assert.Equal(123, user.Id);
            Assert.Equal("Alice Success", user.Name);
            Assert.Equal("alice@success.com", user.Email);
            Assert.Equal("active", user.Status);
        }

        [Fact]
        [Trait("Category", "Generation")]
        public async Task UseExampleSet_NewUser_SelectsNewUserExample()
        {
            // Should select the "new-user" named example
            var mock = new IExampleSetNewUserApiMock();
            var user = await mock.GetUserById(1);
            
            Assert.NotNull(user);
            Assert.Equal(456, user.Id);
            Assert.Equal("Bob NewUser", user.Name);
            Assert.Equal("bob@newuser.com", user.Email);
            Assert.Equal("pending", user.Status);
        }

        [Fact]
        [Trait("Category", "Generation")]
        public async Task UseExampleSet_Premium_SelectsPremiumExample()
        {
            // Should select the "premium" named example
            var mock = new IExampleSetPremiumApiMock();
            var user = await mock.GetUserById(1);
            
            Assert.NotNull(user);
            Assert.Equal(789, user.Id);
            Assert.Equal("Carol Premium", user.Name);
            Assert.Equal("carol@premium.com", user.Email);
            Assert.Equal("premium", user.Status);
        }

        #endregion

        #region Nonexistent Example Set Fallback

        [Fact]
        [Trait("Category", "Generation")]
        public async Task UseExampleSet_Nonexistent_FallsBackToFirst()
        {
            // When specified example set doesn't exist, should fall back to first example
            var mock = new IExampleSetNonexistentApiMock();
            var user = await mock.GetUserById(1);
            
            Assert.NotNull(user);
            // Should fall back to first example (success)
            Assert.Equal(123, user.Id);
            Assert.Equal("Alice Success", user.Name);
        }

        #endregion

        #region Different Example Sets Return Different Data

        [Fact]
        [Trait("Category", "Generation")]
        public async Task DifferentExampleSets_ReturnDifferentData()
        {
            // Verify that different example sets actually return different data
            var defaultMock = new IExampleSetDefaultApiMock();
            var newUserMock = new IExampleSetNewUserApiMock();
            var premiumMock = new IExampleSetPremiumApiMock();
            
            var defaultUser = await defaultMock.GetUserById(1);
            var newUser = await newUserMock.GetUserById(1);
            var premiumUser = await premiumMock.GetUserById(1);
            
            // All should be valid users
            Assert.NotNull(defaultUser);
            Assert.NotNull(newUser);
            Assert.NotNull(premiumUser);
            
            // But with different IDs
            Assert.NotEqual(defaultUser.Id, newUser.Id);
            Assert.NotEqual(defaultUser.Id, premiumUser.Id);
            Assert.NotEqual(newUser.Id, premiumUser.Id);
            
            // And different names
            Assert.NotEqual(defaultUser.Name, newUser.Name);
            Assert.NotEqual(defaultUser.Name, premiumUser.Name);
            Assert.NotEqual(newUser.Name, premiumUser.Name);
        }

        #endregion
    }

    #region Test Interfaces

    // No UseExampleSet specified - should use first example ("success")
    [SkuggaFromOpenApi("specs/example-sets.json", SchemaPrefix = "Default")]
    public partial interface IExampleSetDefaultApi { }

    // Explicitly select "success" example
    [SkuggaFromOpenApi("specs/example-sets.json", UseExampleSet = "success", SchemaPrefix = "Success")]
    public partial interface IExampleSetSuccessApi { }

    // Select "new-user" example
    [SkuggaFromOpenApi("specs/example-sets.json", UseExampleSet = "new-user", SchemaPrefix = "NewUser")]
    public partial interface IExampleSetNewUserApi { }

    // Select "premium" example
    [SkuggaFromOpenApi("specs/example-sets.json", UseExampleSet = "premium", SchemaPrefix = "Premium")]
    public partial interface IExampleSetPremiumApi { }

    // Select nonexistent example - should fall back to first
    [SkuggaFromOpenApi("specs/example-sets.json", UseExampleSet = "nonexistent", SchemaPrefix = "Nonexistent")]
    public partial interface IExampleSetNonexistentApi { }

    #endregion
}
