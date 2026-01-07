using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Skugga.Core;
using Xunit;

namespace Skugga.OpenApi.Tests
{
    /// <summary>
    /// Tests for the GenerateAsync configuration parameter.
    /// Verifies the attribute property works correctly and that async generation is the default behavior.
    /// </summary>
    public class GenerateAsyncConfigurationTests
    {
        [Fact]
        [Trait("Category", "Integration")]
        public void GenerateAsync_DefaultsToTrue()
        {
            var attr = new SkuggaFromOpenApiAttribute("test.json");
            Assert.True(attr.GenerateAsync);
        }

        [Fact]
        [Trait("Category", "Integration")]
        public void GenerateAsync_CanBeSetToFalse()
        {
            var attr = new SkuggaFromOpenApiAttribute("test.json")
            {
                GenerateAsync = false
            };
            Assert.False(attr.GenerateAsync);
        }

        [Fact]
        [Trait("Category", "Integration")]
        public void GenerateAsync_CanBeExplicitlySetToTrue()
        {
            var attr = new SkuggaFromOpenApiAttribute("test.json")
            {
                GenerateAsync = true
            };
            Assert.True(attr.GenerateAsync);
        }

        [Fact]
        [Trait("Category", "Integration")]
        public void GenerateAsync_WorksWithOtherAttributeProperties()
        {
            var attr = new SkuggaFromOpenApiAttribute("api.json")
            {
                GenerateAsync = false,
                OperationFilter = "payments",
                ValidateSchemas = true,
                CachePath = "cache/"
            };

            Assert.False(attr.GenerateAsync);
            Assert.Equal("payments", attr.OperationFilter);
            Assert.True(attr.ValidateSchemas);
            Assert.Equal("cache/", attr.CachePath);
        }

        [Fact]
        [Trait("Category", "Integration")]
        public void ExistingPetStoreApi_UsesAsyncByDefault()
        {
            // The existing IPetStoreApi doesn't specify GenerateAsync,
            // so it should default to async (Task<T> return types)
            var interfaceType = typeof(IPetStoreApi);
            var methods = interfaceType.GetMethods();

            var listpetsMethod = methods.FirstOrDefault(m => m.Name == "ListPets");
            Assert.NotNull(listpetsMethod);

            // Should return Task<Pet[]>
            Assert.True(listpetsMethod.ReturnType.IsGenericType);
            Assert.Equal(typeof(Task<>), listpetsMethod.ReturnType.GetGenericTypeDefinition());

            var innerType = listpetsMethod.ReturnType.GetGenericArguments()[0];
            Assert.True(innerType.IsArray);
            Assert.Equal("Pet", innerType.GetElementType()?.Name);
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task ExistingMock_ReturnsCompletedTasks()
        {
            var mock = new IPetStoreApiMock();
            var task = mock.ListPets();

            Assert.NotNull(task);
            Assert.True(task.IsCompleted);

            var pets = await task;
            Assert.NotNull(pets);
            Assert.NotEmpty(pets);
        }

        [Fact]
        [Trait("Category", "Integration")]
        public void AllAsyncMethods_ReturnTaskOfT()
        {
            var interfaceType = typeof(IPetStoreApi);
            var methods = interfaceType.GetMethods().Where(m => !m.IsSpecialName);

            foreach (var method in methods)
            {
                if (method.ReturnType != typeof(void))
                {
                    Assert.True(
                        method.ReturnType.IsGenericType &&
                        method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>),
                        $"Method {method.Name} should return Task<T>");
                }
            }
        }

        [Fact]
        [Trait("Category", "Integration")]
        public void AsyncMock_UsesTaskFromResult()
        {
            var mock = new IPetStoreApiMock();

            var task1 = mock.GetPet(1);
            var task2 = mock.GetPet(2);

            // Each call creates a new Task
            Assert.NotSame(task1, task2);

            // Both are completed (Task.FromResult)
            Assert.True(task1.IsCompleted);
            Assert.True(task2.IsCompleted);
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task AsyncMock_IntegratesWithAsyncAwait()
        {
            var mock = new IPetStoreApiMock();

            async Task<Pet> GetPetAsync(int id)
            {
                return await mock.GetPet(id);
            }

            var result = await GetPetAsync(123);
            Assert.NotNull(result);
            // Mock returns example data with Id=1, not the requested Id
            Assert.Equal(1, result.Id);
        }
    }
}
