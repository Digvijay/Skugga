using Xunit;
using Skugga.Core;

namespace Skugga.Core.Tests
{
    public abstract class GrandBase
    {
        protected abstract int GrandValue();
    }

    public abstract class BaseWithProtected : GrandBase
    {
        protected virtual int GetValue() => 10;
        protected abstract string GetText(int id);
        protected virtual void DoWork() { }
        protected virtual int Count { get; } = 5;

        public int CallGrandValue() => GrandValue();
        public int CallGetValue() => GetValue();
        public string CallGetText(int id) => GetText(id);
        public void CallDoWork() => DoWork();
        public int CallGetCount() => Count;
    }

    public class ProtectedMemberTests
    {
        [Fact]
        public void Protected_FromGrandBase_CanBeMocked()
        {
            // Arrange
            var mock = Mock.Create<BaseWithProtected>();
            mock.Protected().Setup<int>("GrandValue").Returns(999);

            // Act
            int result = mock.CallGrandValue();

            // Assert
            Assert.Equal(999, result);
        }

        [Fact]
        public void Protected_Property_CanBeMocked()
        {
            // Arrange
            var mock = Mock.Create<BaseWithProtected>();
            mock.Protected().SetupGet<int>("Count").Returns(100);

            // Act
            int result = mock.CallGetCount();

            // Assert
            Assert.Equal(100, result);
        }

        [Fact]
        public void Protected_VirtualMethod_CanBeMocked()
        {
            // Arrange
            var mock = Mock.Create<BaseWithProtected>();
            mock.Protected().Setup<int>("GetValue").Returns(42);

            // Act
            int result = mock.CallGetValue();

            // Assert
            Assert.Equal(42, result);
        }

        [Fact]
        public void Protected_AbstractMethod_CanBeMocked()
        {
            // Arrange
            var mock = Mock.Create<BaseWithProtected>();
            mock.Protected().Setup<string>("GetText", ItExpr.IsAny<int>()).Returns("mocked");

            // Act
            string result = mock.CallGetText(1);

            // Assert
            Assert.Equal("mocked", result);
        }

        [Fact]
        public void Protected_VoidMethod_CanBeVerified()
        {
            // Arrange
            var mock = Mock.Create<BaseWithProtected>();

            // Act
            mock.CallDoWork();

            // Assert
            mock.Protected().Verify("DoWork", Times.Once());
        }

        [Fact]
        public void Protected_WithItExprMatcher_Works()
        {
            // Arrange
            var mock = Mock.Create<BaseWithProtected>();
            mock.Protected().Setup<string>("GetText", ItExpr.Is<int>(id => id > 10)).Returns("large");
            mock.Protected().Setup<string>("GetText", ItExpr.Is<int>(id => id <= 10)).Returns("small");

            // Act & Assert
            Assert.Equal("large", mock.CallGetText(15));
            Assert.Equal("small", mock.CallGetText(5));
        }
    }
}
