using Skugga.Core;
using Xunit;

namespace Skugga.Core.Tests
{
    public class ResetTests
    {
        public interface IService
        {
            string GetData(int id);
            void Execute();
        }

        [Fact]
        public void Reset_ClearsSetups()
        {
            var mock = Mock.Create<IService>();
            mock.Setup(x => x.GetData(1)).Returns("data");

            Assert.Equal("data", mock.GetData(1));

            mock.Reset();

            // After reset, it should return default (as it's Loose behavior by default)
            Assert.Null(mock.GetData(1));
        }

        [Fact]
        public void Reset_ClearsInvocationHistory()
        {
            var mock = Mock.Create<IService>();
            mock.Execute();

            mock.Verify(x => x.Execute(), Times.Once());

            mock.Reset();

            // After reset, verification should fail (0 calls)
            mock.Verify(x => x.Execute(), Times.Never());
        }

        [Fact]
        public void ResetCalls_ClearsInvocationHistory_ButKeepsSetups()
        {
            var mock = Mock.Create<IService>();
            mock.Setup(x => x.GetData(1)).Returns("data");

            Assert.Equal("data", mock.GetData(1));
            mock.Verify(x => x.GetData(1), Times.Once());

            mock.ResetCalls();

            // Setup should still work
            Assert.Equal("data", mock.GetData(1));

            // Invocation history should be cleared (but we just made a new call, so count is 1 again)
            // To properly test, we check that it's NOT 2.
            mock.Verify(x => x.GetData(1), Times.Once());
        }

        [Fact]
        public void MockRepository_Reset_AffectsAllMocks()
        {
            var repository = new MockRepository();
            var mock1 = repository.Create<IService>();
            var mock2 = repository.Create<IService>();

            mock1.Setup(x => x.GetData(1)).Returns("data1");
            mock2.Setup(x => x.GetData(1)).Returns("data2");

            Assert.Equal("data1", mock1.GetData(1));
            Assert.Equal("data2", mock2.GetData(1));

            repository.Reset();

            Assert.Null(mock1.GetData(1));
            Assert.Null(mock2.GetData(1));
        }

        [Fact]
        public void MockRepository_ResetCalls_AffectsAllMocks()
        {
            var repository = new MockRepository();
            var mock1 = repository.Create<IService>();
            var mock2 = repository.Create<IService>();

            mock1.Execute();
            mock2.Execute();

            mock1.Verify(x => x.Execute(), Times.Once());
            mock2.Verify(x => x.Execute(), Times.Once());

            repository.ResetCalls();

            mock1.Verify(x => x.Execute(), Times.Never());
            mock2.Verify(x => x.Execute(), Times.Never());
        }
    }
}
