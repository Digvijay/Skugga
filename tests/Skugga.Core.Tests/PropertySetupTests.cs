using System;
using Skugga.Core;
using Xunit;

namespace Skugga.Core.Tests
{
    public class PropertySetupTests
    {
        public interface IService
        {
            string Name { get; set; }
            int Age { get; set; }
        }

        [Fact]
        public void VerifySet_WithSpecificValue_Matches()
        {
            // Arrange
            var mock = Mock.Create<IService>();

            // Act
            mock.Name = "John";

            // Assert
            mock.VerifySet(m => m.Name = "John", Times.Once());
        }

        [Fact]
        public void VerifySet_WithItIsAny_Matches()
        {
            // Arrange
            var mock = Mock.Create<IService>();

            // Act
            mock.Age = 42;

            // Assert
            mock.VerifySet(m => m.Age = It.IsAny<int>(), Times.Once());
        }

        [Fact]
        public void VerifySet_Throws_WhenCountDoesNotMatch()
        {
            // Arrange
            var mock = Mock.Create<IService>();

            // Act
            mock.Name = "John";
            mock.Name = "John";

            // Assert
            Assert.Throws<MockException>(() => mock.VerifySet(m => m.Name = "John", Times.Once()));
        }


        [Fact]
        public void SetupSet_WithSpecificValue_TriggersCallback()
        {
            // Arrange
            var mock = Mock.Create<IService>();
            string? capturedName = null;

            mock.SetupSet(m => m.Name = "John")
                .Callback(() => capturedName = "John");

            // Act
            mock.Name = "John";
            var JohnSet = capturedName == "John";

            mock.Name = "Jane"; // Should not trigger callback

            // Assert
            Assert.True(JohnSet);
            Assert.Equal("John", capturedName);
        }

        [Fact]
        public void SetupSet_WithItIsAny_TriggersCallback()
        {
            // Arrange
            var mock = Mock.Create<IService>();
            int lastAgeValue = 0;

            mock.SetupSet(m => m.Age = It.IsAny<int>())
                .Callback((int val) =>
                {
                    lastAgeValue = val;
                });

            // Act
            mock.Age = 42;

            // Assert
            Assert.Equal(42, lastAgeValue);
        }

        [Fact]
        public void SetupSet_MultipleSetups_AreRespected()
        {
            // Arrange
            var mock = Mock.Create<IService>();
            int johnCalls = 0;
            int janeCalls = 0;

            mock.SetupSet(m => m.Name = "John").Callback(() => johnCalls++);
            mock.SetupSet(m => m.Name = "Jane").Callback(() => janeCalls++);

            // Act
            mock.Name = "John";
            mock.Name = "Jane";
            mock.Name = "John";

            // Assert
            Assert.Equal(2, johnCalls);
            Assert.Equal(1, janeCalls);
        }
    }
}
