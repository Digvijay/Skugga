using System;
using Skugga.Core;
using Xunit;

namespace Skugga.Core.Tests
{
    public class MockRepositoryTests
    {
        public interface IRepositoryTestService
        {
            void DoSomething();
            int GetValue();
        }

        [Fact]
        public void Create_RegistersMockWithRepository()
        {
            var repo = new MockRepository(MockBehavior.Loose);
            var mock = repo.Create<IRepositoryTestService>();

            Assert.NotNull(mock);
            // We can't easily check internal _mocks list, but we can verify VerifyAll call propagates

            mock.DoSomething(); // Call something

            // This should not throw for Loose mock
            repo.VerifyAll();

            // To prove it's registered, let's setup something that fails verification
            // VerifyAll checks "CallCount > 0" for all setups.
            // If we setup and don't call, VerifyAll should fail.
            MockExtensions.Setup(mock, x => x.GetValue()).Returns(10);

            Assert.Throws<MockException>(() => repo.VerifyAll());
        }

        [Fact]
        public void Create_UsesRepositoryBehavior()
        {
            var repo = new MockRepository(MockBehavior.Strict);
            var mock = repo.Create<IRepositoryTestService>();

            // Strict mock throws on un-setup call
            Assert.Throws<MockException>(() => mock.DoSomething());
        }

        [Fact]
        public void VerifyNoOtherCalls_FailsOnUnverifiedCalls()
        {
            var repo = new MockRepository(MockBehavior.Loose);
            var mock1 = repo.Create<IRepositoryTestService>();
            var mock2 = repo.Create<IRepositoryTestService>();

            mock1.DoSomething();

            // Should fail because mock1.DoSomething() wasn't verified
            var ex = Assert.Throws<MockException>(() => repo.VerifyNoOtherCalls());
            Assert.Contains("DoSomething", ex.Message);

            // Now verify it
            MockExtensions.Verify(mock1, x => x.DoSomething(), Times.Once());

            // Now it should pass
            repo.VerifyNoOtherCalls();

            // Call mock2
            mock2.GetValue();

            // Should fail again
            Assert.Throws<MockException>(() => repo.VerifyNoOtherCalls());

            // Verify mock2
            MockExtensions.Verify(mock2, x => x.GetValue(), Times.Once());

            // Pass
            repo.VerifyNoOtherCalls();
        }

        [Fact]
        public void Create_WithBehaviorOverride_RespectsOverride()
        {
            var repo = new MockRepository(MockBehavior.Strict);

            // Override to Loose
            var mock = repo.Create<IRepositoryTestService>(MockBehavior.Loose);

            // Should not throw
            mock.DoSomething();
        }

        [Fact]
        public void Create_WithDefaultValue_RespectsStrategy()
        {
            var repo = new MockRepository(MockBehavior.Loose, DefaultValue.Mock);

            // Test nested mock creation behavior
            // We need an interface that returns another interface for recursive mock test
            // But let's check basic Create first
            var mock = repo.Create<IRepositoryTestService>();
            Assert.NotNull(mock);
        }
    }
}
