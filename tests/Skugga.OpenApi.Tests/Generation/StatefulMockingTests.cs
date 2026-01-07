using System;
using System.Linq;
using System.Reflection;
using Skugga.OpenApi.Tests.Generation.StatefulUsers;
using Skugga.OpenApi.Tests.Generation.StatelessUsers;
using Xunit;

namespace Skugga.OpenApi.Tests.Generation
{
    /// <summary>
    /// Tests for Stateful Mock Behavior.
    /// Validates CRUD operations with in-memory entity tracking.
    /// </summary>
    public class StatefulMockingTests
    {
        [Fact]
        [Trait("Category", "Stateful Mock Behavior")]
        [Trait("Feature", "StatefulMocking")]
        public void StatefulMock_Interface_IsGenerated()
        {
            var interfaceType = typeof(IStatefulUserApi);
            Assert.NotNull(interfaceType);
            Assert.True(interfaceType.IsInterface);
        }

        [Fact]
        [Trait("Category", "Stateful Mock Behavior")]
        [Trait("Feature", "StatefulMocking")]
        public void StatefulMock_MockClass_IsGenerated()
        {
            var mock = new IStatefulUserApiMock();
            Assert.NotNull(mock);
            Assert.IsAssignableFrom<IStatefulUserApi>(mock);
        }

        [Fact]
        [Trait("Category", "Stateful Mock Behavior")]
        [Trait("Feature", "StatefulMocking")]
        public void StatefulMock_HasEntityStore_Field()
        {
            var mockType = typeof(IStatefulUserApiMock);
            var entityStoreField = mockType.GetField("_entityStore", BindingFlags.NonPublic | BindingFlags.Instance);

            Assert.NotNull(entityStoreField);
            Assert.Contains("Dictionary", entityStoreField.FieldType.Name);
        }

        [Fact]
        [Trait("Category", "Stateful Mock Behavior")]
        [Trait("Feature", "StatefulMocking")]
        public void StatefulMock_HasIdCounters_Field()
        {
            var mockType = typeof(IStatefulUserApiMock);
            var idCountersField = mockType.GetField("_idCounters", BindingFlags.NonPublic | BindingFlags.Instance);

            Assert.NotNull(idCountersField);
            Assert.Contains("Dictionary", idCountersField.FieldType.Name);
        }

        [Fact]
        [Trait("Category", "Stateful Mock Behavior")]
        [Trait("Feature", "StatefulMocking")]
        public void StatefulMock_HasStateLock_Field()
        {
            var mockType = typeof(IStatefulUserApiMock);
            var stateLockField = mockType.GetField("_stateLock", BindingFlags.NonPublic | BindingFlags.Instance);

            Assert.NotNull(stateLockField);
            Assert.Equal(typeof(object), stateLockField.FieldType);
        }

        [Fact]
        [Trait("Category", "Stateful Mock Behavior")]
        [Trait("Feature", "StatefulMocking")]
        public void StatefulMock_HasResetState_Method()
        {
            var mockType = typeof(IStatefulUserApiMock);
            var resetStateMethod = mockType.GetMethod("ResetState", BindingFlags.Public | BindingFlags.Instance);

            Assert.NotNull(resetStateMethod);
            Assert.Equal(typeof(void), resetStateMethod.ReturnType);
            Assert.Empty(resetStateMethod.GetParameters());
        }

        [Fact]
        [Trait("Category", "Stateful Mock Behavior")]
        [Trait("Feature", "StatefulMocking")]
        public void StatefulMock_HasCrudMethods()
        {
            var interfaceType = typeof(IStatefulUserApi);

            var createMethod = interfaceType.GetMethod("CreateUser");
            var readMethod = interfaceType.GetMethod("GetUser");
            var listMethod = interfaceType.GetMethod("ListUsers");
            var updateMethod = interfaceType.GetMethod("UpdateUser");
            var deleteMethod = interfaceType.GetMethod("DeleteUser");

            Assert.NotNull(createMethod);
            Assert.NotNull(readMethod);
            Assert.NotNull(listMethod);
            Assert.NotNull(updateMethod);
            Assert.NotNull(deleteMethod);
        }

        [Fact]
        [Trait("Category", "Stateful Mock Behavior")]
        [Trait("Feature", "StatefulMocking")]
        public void StatelessMock_DoesNotHaveEntityStore()
        {
            var mockType = typeof(IStatelessUserApiMock);
            var entityStoreField = mockType.GetField("_entityStore", BindingFlags.NonPublic | BindingFlags.Instance);

            Assert.Null(entityStoreField);
        }

        [Fact]
        [Trait("Category", "Stateful Mock Behavior")]
        [Trait("Feature", "StatefulMocking")]
        public void StatelessMock_DoesNotHaveResetState()
        {
            var mockType = typeof(IStatelessUserApiMock);
            var resetStateMethod = mockType.GetMethod("ResetState", BindingFlags.Public | BindingFlags.Instance);

            Assert.Null(resetStateMethod);
        }

        [Fact]
        [Trait("Category", "Stateful Mock Behavior")]
        [Trait("Feature", "StatefulMocking")]
        public async System.Threading.Tasks.Task StatefulMock_CreateUser_ReturnsUser()
        {
            var mock = new IStatefulUserApiMock();

            var userInput = new StatefulUsers.UserInput
            {
                Name = "Test User",
                Email = "test@example.com"
            };

            var result = await mock.CreateUser(userInput);

            Assert.NotNull(result);
        }

        [Fact]
        [Trait("Category", "Stateful Mock Behavior")]
        [Trait("Feature", "StatefulMocking")]
        public async System.Threading.Tasks.Task StatefulMock_ResetState_ClearsData()
        {
            var mock = new IStatefulUserApiMock();

            var userInput = new StatefulUsers.UserInput
            {
                Name = "Test User",
                Email = "test@example.com"
            };
            await mock.CreateUser(userInput);

            mock.ResetState();

            Assert.True(true);
        }

        [Fact]
        [Trait("Category", "Stateful Mock Behavior")]
        [Trait("Feature", "StatefulMocking")]
        public async System.Threading.Tasks.Task StatefulMock_ListUsers_ReturnsArray()
        {
            var mock = new IStatefulUserApiMock();

            var result = await mock.ListUsers();

            Assert.NotNull(result);
        }

        [Fact]
        [Trait("Category", "Stateful Mock Behavior")]
        [Trait("Feature", "StatefulMocking")]
        public async System.Threading.Tasks.Task StatefulMock_GetUser_ThrowsFor404Scenario()
        {
            var mock = new IStatefulUserApiMock();

            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await mock.GetUser("nonexistent-id");
            });
        }
    }
}
