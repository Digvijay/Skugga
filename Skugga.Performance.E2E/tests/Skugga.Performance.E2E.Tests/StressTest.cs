using System.Collections.Generic;
using System.Linq;
using Xunit;
using Skugga.Performance.E2E.Api;
using Skugga.Performance.E2E.Domain;
using Skugga.Core;

namespace Skugga.Performance.E2E.Tests
{
    public class StressTest
    {
        public static IEnumerable<object[]> GetData()
        {
            return Enumerable.Range(1, 500).Select(i => new object[] { i });
        }

        [Theory]
        [MemberData(nameof(GetData))]
        public void Run(int userId)
        {
            var mock = Mock.Create<IUserRepository>();
            mock.Setup(x => x.GetUserRole(userId)).Returns($"Role_{userId}");
            
            var handler = new UserHandler(mock);
            var result = handler.GetUser(userId);
            
            Assert.NotNull(result);
        }
    }
}
