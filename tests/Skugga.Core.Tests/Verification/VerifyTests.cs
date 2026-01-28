using Skugga.Core;

namespace Skugga.Core.Tests;

public class VerifyTests
{
    public interface ITestService
    {
        void Execute();
        void ExecuteWithArgs(int value);
        string GetData();
        int Calculate(int a, int b);
    }

    [Fact]
    [Trait("Category", "Verification")]
    public void Verify_MethodCalledOnce_ShouldPass()
    {
        // Arrange
        var mock = Mock.Create<ITestService>();

        // Act
        mock.Execute();

        // Assert
        mock.Verify(x => x.Execute(), Times.Once());
    }

    [Fact]
    [Trait("Category", "Verification")]
    public void Verify_MethodNeverCalled_ShouldPass()
    {
        // Arrange
        var mock = Mock.Create<ITestService>();

        // Assert - method was never called
        mock.Verify(x => x.Execute(), Times.Never());
    }

    [Fact]
    [Trait("Category", "Verification")]
    public void Verify_MethodCalledNever_ButWasCalled_ShouldThrow()
    {
        // Arrange
        var mock = Mock.Create<ITestService>();
        mock.Execute();

        // Act & Assert
        var exception = Assert.ThrowsAny<MockException>(() =>
            mock.Verify(x => x.Execute(), Times.Never()));

        exception.Message.Should().Contain("Expected exactly 0 call(s)");
        exception.Message.Should().Contain("but was called 1 time(s)");
    }

    [Fact]
    [Trait("Category", "Verification")]
    public void Verify_MethodNotCalled_ButExpectedOnce_ShouldThrow()
    {
        // Arrange
        var mock = Mock.Create<ITestService>();

        // Act & Assert
        var exception = Assert.ThrowsAny<MockException>(() =>
            mock.Verify(x => x.Execute(), Times.Once()));

        exception.Message.Should().Contain("Expected exactly 1 call(s)");
        exception.Message.Should().Contain("but was called 0 time(s)");
    }

    [Fact]
    [Trait("Category", "Verification")]
    public void Verify_MethodCalledMultipleTimes_Exactly_ShouldPass()
    {
        // Arrange
        var mock = Mock.Create<ITestService>();

        // Act
        mock.Execute();
        mock.Execute();
        mock.Execute();

        // Assert
        mock.Verify(x => x.Execute(), Times.Exactly(3));
    }

    [Fact]
    [Trait("Category", "Verification")]
    public void Verify_MethodWithArguments_ShouldVerifyCorrectCall()
    {
        // Arrange
        var mock = Mock.Create<ITestService>();

        // Act
        mock.ExecuteWithArgs(42);
        mock.ExecuteWithArgs(100);

        // Assert
        mock.Verify(x => x.ExecuteWithArgs(42), Times.Once());
        mock.Verify(x => x.ExecuteWithArgs(100), Times.Once());
        mock.Verify(x => x.ExecuteWithArgs(999), Times.Never());
    }

    [Fact]
    [Trait("Category", "Verification")]
    public void Verify_AtLeast_ShouldPassWhenConditionMet()
    {
        // Arrange
        var mock = Mock.Create<ITestService>();

        // Act
        mock.Execute();
        mock.Execute();
        mock.Execute();

        // Assert
        mock.Verify(x => x.Execute(), Times.AtLeast(2)); // Should pass (3 >= 2)
        mock.Verify(x => x.Execute(), Times.AtLeast(3)); // Should pass (3 >= 3)
    }

    [Fact]
    [Trait("Category", "Verification")]
    public void Verify_AtLeast_ShouldFailWhenConditionNotMet()
    {
        // Arrange
        var mock = Mock.Create<ITestService>();
        mock.Execute();

        // Act & Assert
        var exception = Assert.ThrowsAny<MockException>(() =>
            mock.Verify(x => x.Execute(), Times.AtLeast(2)));

        exception.Message.Should().Contain("at least 2");
    }

    [Fact]
    [Trait("Category", "Verification")]
    public void Verify_AtMost_ShouldPassWhenConditionMet()
    {
        // Arrange
        var mock = Mock.Create<ITestService>();

        // Act
        mock.Execute();
        mock.Execute();

        // Assert
        mock.Verify(x => x.Execute(), Times.AtMost(3)); // Should pass (2 <= 3)
        mock.Verify(x => x.Execute(), Times.AtMost(2)); // Should pass (2 <= 2)
    }

    [Fact]
    [Trait("Category", "Verification")]
    public void Verify_AtMost_ShouldFailWhenConditionNotMet()
    {
        // Arrange
        var mock = Mock.Create<ITestService>();
        mock.Execute();
        mock.Execute();
        mock.Execute();

        // Act & Assert
        var exception = Assert.ThrowsAny<MockException>(() =>
            mock.Verify(x => x.Execute(), Times.AtMost(2)));

        exception.Message.Should().Contain("at most 2");
    }

    [Fact]
    [Trait("Category", "Verification")]
    public void Verify_Between_ShouldPassWhenInRange()
    {
        // Arrange
        var mock = Mock.Create<ITestService>();

        // Act
        mock.Execute();
        mock.Execute();
        mock.Execute();

        // Assert
        mock.Verify(x => x.Execute(), Times.Between(1, 5)); // Should pass (3 is in [1,5])
        mock.Verify(x => x.Execute(), Times.Between(3, 3)); // Should pass (3 is in [3,3])
    }

    [Fact]
    [Trait("Category", "Verification")]
    public void Verify_Between_ShouldFailWhenOutOfRange()
    {
        // Arrange
        var mock = Mock.Create<ITestService>();
        mock.Execute();

        // Act & Assert
        var exception = Assert.ThrowsAny<MockException>(() =>
            mock.Verify(x => x.Execute(), Times.Between(2, 5)));

        exception.Message.Should().Contain("between 2 and 5");
    }

    [Fact]
    [Trait("Category", "Verification")]
    public void Verify_MethodWithReturnValue_ShouldVerify()
    {
        // Arrange
        var mock = Mock.Create<ITestService>();
        mock.Setup(x => x.GetData()).Returns("test");

        // Act
        var result1 = mock.GetData();
        var result2 = mock.GetData();

        // Assert
        result1.Should().Be("test");
        result2.Should().Be("test");
        mock.Verify(x => x.GetData(), Times.Exactly(2));
    }

    [Fact]
    [Trait("Category", "Verification")]
    public void Verify_MethodWithMultipleArguments_ShouldVerifyExactMatch()
    {
        // Arrange
        var mock = Mock.Create<ITestService>();
        mock.Setup(x => x.Calculate(2, 3)).Returns(5);

        // Act
        mock.Calculate(2, 3);
        mock.Calculate(2, 3);
        mock.Calculate(5, 10);

        // Assert
        mock.Verify(x => x.Calculate(2, 3), Times.Exactly(2));
        mock.Verify(x => x.Calculate(5, 10), Times.Once());
        mock.Verify(x => x.Calculate(1, 1), Times.Never());
    }

    [Fact]
    [Trait("Category", "Verification")]
    public void Verify_MultipleMethodsIndependently_ShouldVerifyEachSeparately()
    {
        // Arrange
        var mock = Mock.Create<ITestService>();

        // Act
        mock.Execute();
        mock.Execute();
        mock.GetData();

        // Assert
        mock.Verify(x => x.Execute(), Times.Exactly(2));
        mock.Verify(x => x.GetData(), Times.Once());
        mock.Verify(x => x.ExecuteWithArgs(0), Times.Never());
    }

    [Fact]
    [Trait("Category", "Verification")]
    public void Verify_AfterReset_ShouldNotSeePreviousCalls()
    {
        // Arrange
        var mock = Mock.Create<ITestService>();
        mock.Execute();
        mock.Execute();

        // Verify first round
        mock.Verify(x => x.Execute(), Times.Exactly(2));

        // Create new mock (simulating reset)
        var newMock = Mock.Create<ITestService>();

        // Assert - new mock should have no history
        newMock.Verify(x => x.Execute(), Times.Never());
    }
}
