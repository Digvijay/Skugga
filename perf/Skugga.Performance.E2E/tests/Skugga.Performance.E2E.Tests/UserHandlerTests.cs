using Xunit;
using Skugga.Performance.E2E.Api;
using Skugga.Performance.E2E.Domain;
using Microsoft.AspNetCore.Http.HttpResults;
using Skugga.Core;

namespace Skugga.Performance.E2E.Tests
{
    public class UserHandlerTests
    {
        [Fact]
        public void GetUser_ReturnsOk_WhenUserExists()
        {
            // 1. Skugga Magic
            var mockRepo = Mock.Create<IUserRepository>();
            mockRepo.Setup(x => x.GetUserRole(1)).Returns("SuperAdmin");

            // 2. Instantiate Handler (Pure C#, no MVC overhead)
            var handler = new UserHandler(mockRepo);

            // 3. Act
            var result = handler.GetUser(1);

            // 4. Assert (Typed Results pattern for Minimal APIs)
            // Note: In AOT/Minimal, results are often 'Ok<T>'
            var okResult = Assert.IsType<Ok<UserResponse>>(result.Result);
            Assert.Equal("SuperAdmin", okResult.Value!.Role);
        }
    }
}