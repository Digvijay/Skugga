using Xunit;
using FluentAssertions;
using Skugga.Core;
using System;

namespace Skugga.Core.Tests;

public class ItIsAnyTests
{
    public interface ITestService
    {
        string Process(int value);
        string ProcessTwo(int a, string b);
        string ProcessThree(int a, string b, bool c);
        void Execute(string command);
        int Calculate(int x, int y);
    }

    [Fact]
    [Trait("Category", "Matchers")]
    public void Setup_WithItIsAny_MatchesAnyValue()
    {
        // Arrange
        var mock = Mock.Create<ITestService>();
        mock.Setup(x => x.Process(It.IsAny<int>())).Returns("matched");

        // Act & Assert
        mock.Process(1).Should().Be("matched");
        mock.Process(42).Should().Be("matched");
        mock.Process(-100).Should().Be("matched");
        mock.Process(0).Should().Be("matched");
    }

    [Fact]
    [Trait("Category", "Matchers")]
    public void Setup_WithItIsAnyString_MatchesAnyStringValue()
    {
        // Arrange
        var mock = Mock.Create<ITestService>();
        mock.Setup(x => x.Execute(It.IsAny<string>())).Callback((string s) => { });

        // Act - should not throw with any string
        mock.Execute("test");
        mock.Execute("");
        mock.Execute("another value");
    }

    [Fact]
    [Trait("Category", "Matchers")]
    public void Setup_WithItIsAnyInMultipleArgs_MatchesAllCombinations()
    {
        // Arrange
        var mock = Mock.Create<ITestService>();
        mock.Setup(x => x.ProcessTwo(It.IsAny<int>(), It.IsAny<string>())).Returns("matched");

        // Act & Assert
        mock.ProcessTwo(1, "a").Should().Be("matched");
        mock.ProcessTwo(42, "test").Should().Be("matched");
        mock.ProcessTwo(-5, "").Should().Be("matched");
    }

    [Fact]
    [Trait("Category", "Matchers")]
    public void Setup_WithMixedItIsAnyAndSpecificValue_MatchesCorrectly()
    {
        // Arrange
        var mock = Mock.Create<ITestService>();
        mock.Setup(x => x.ProcessTwo(42, It.IsAny<string>())).Returns("specific int");
        mock.Setup(x => x.ProcessTwo(It.IsAny<int>(), "test")).Returns("specific string");

        // Act & Assert
        mock.ProcessTwo(42, "anything").Should().Be("specific int");
        mock.ProcessTwo(42, "test").Should().Be("specific int"); // First match wins
        mock.ProcessTwo(100, "test").Should().Be("specific string");
        mock.ProcessTwo(100, "other").Should().BeNull(); // No match
    }

    [Fact]
    [Trait("Category", "Matchers")]
    public void Setup_WithItIsAnyThreeArgs_MatchesCorrectly()
    {
        // Arrange
        var mock = Mock.Create<ITestService>();
        mock.Setup(x => x.ProcessThree(It.IsAny<int>(), "fixed", It.IsAny<bool>())).Returns("matched");

        // Act & Assert
        mock.ProcessThree(1, "fixed", true).Should().Be("matched");
        mock.ProcessThree(42, "fixed", false).Should().Be("matched");
        mock.ProcessThree(1, "different", true).Should().BeNull(); // Middle arg doesn't match
    }

    [Fact]
    [Trait("Category", "Matchers")]
    public void Verify_WithItIsAny_MatchesAnyValue()
    {
        // Arrange
        var mock = Mock.Create<ITestService>();

        // Act
        mock.Process(1);
        mock.Process(42);
        mock.Process(100);

        // Assert
        mock.Verify(x => x.Process(It.IsAny<int>()), Times.Exactly(3));
    }

    [Fact]
    [Trait("Category", "Matchers")]
    public void Verify_WithItIsAny_FailsWhenNotCalled()
    {
        // Arrange
        var mock = Mock.Create<ITestService>();

        // Act
        mock.Process(42); // Called with 42

        // Assert
        var exception = Assert.Throws<MockException>(() => 
            mock.Verify(x => x.Execute(It.IsAny<string>()), Times.Once())
        );
        exception.Message.Should().Contain("Expected exactly 1 call(s)");
    }

    [Fact]
    [Trait("Category", "Matchers")]
    public void Verify_WithItIsAnyMultipleArgs_MatchesCorrectly()
    {
        // Arrange
        var mock = Mock.Create<ITestService>();

        // Act
        mock.ProcessTwo(1, "a");
        mock.ProcessTwo(2, "b");

        // Assert
        mock.Verify(x => x.ProcessTwo(It.IsAny<int>(), It.IsAny<string>()), Times.Exactly(2));
    }

    [Fact]
    [Trait("Category", "Matchers")]
    public void Verify_WithMixedItIsAnyAndSpecificValue_MatchesCorrectly()
    {
        // Arrange
        var mock = Mock.Create<ITestService>();

        // Act
        mock.ProcessTwo(42, "test");
        mock.ProcessTwo(42, "other");
        mock.ProcessTwo(100, "test");

        // Assert
        mock.Verify(x => x.ProcessTwo(42, It.IsAny<string>()), Times.Exactly(2));
        mock.Verify(x => x.ProcessTwo(It.IsAny<int>(), "test"), Times.Exactly(2));
    }

    [Fact]
    [Trait("Category", "Matchers")]
    public void Setup_WithItIsAnyAndReturnsFunc_WorksTogether()
    {
        // Arrange
        var mock = Mock.Create<ITestService>();
        mock.Setup(x => x.Calculate(It.IsAny<int>(), It.IsAny<int>()))
            .Returns((int a, int b) => a + b);

        // Act & Assert
        mock.Calculate(1, 2).Should().Be(3);
        mock.Calculate(10, 20).Should().Be(30);
        mock.Calculate(-5, 5).Should().Be(0);
    }

    [Fact]
    [Trait("Category", "Matchers")]
    public void Setup_WithItIsAnyAndCallback_WorksTogether()
    {
        // Arrange
        var mock = Mock.Create<ITestService>();
        var callbackInvoked = false;
        mock.Setup(x => x.Execute(It.IsAny<string>()))
            .Callback((string s) => callbackInvoked = true);

        // Act
        mock.Execute("test");

        // Assert
        callbackInvoked.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "Matchers")]
    public void Setup_WithItIsAnyAndReturnsInOrder_WorksTogether()
    {
        // Arrange
        var mock = Mock.Create<ITestService>();
        mock.Setup(x => x.Process(It.IsAny<int>()))
            .ReturnsInOrder("first", "second", "third");

        // Act & Assert
        mock.Process(1).Should().Be("first");
        mock.Process(2).Should().Be("second");
        mock.Process(3).Should().Be("third");
        mock.Process(4).Should().Be("third"); // Repeats last
    }

    [Fact]
    [Trait("Category", "Matchers")]
    public void Setup_MultipleSetupsWithItIsAny_FirstMatchWins()
    {
        // Arrange
        var mock = Mock.Create<ITestService>();
        mock.Setup(x => x.Process(It.IsAny<int>())).Returns("any");
        mock.Setup(x => x.Process(42)).Returns("specific");

        // Act & Assert
        mock.Process(1).Should().Be("any");
        mock.Process(42).Should().Be("any"); // First setup (It.IsAny) matches first
        mock.Process(100).Should().Be("any");
    }

    [Fact]
    [Trait("Category", "Matchers")]
    public void Setup_SpecificValueThenItIsAny_SpecificMatchesFirst()
    {
        // Arrange
        var mock = Mock.Create<ITestService>();
        mock.Setup(x => x.Process(42)).Returns("specific");
        mock.Setup(x => x.Process(It.IsAny<int>())).Returns("any");

        // Act & Assert
        mock.Process(42).Should().Be("specific"); // Specific setup matches first
        mock.Process(1).Should().Be("any");
        mock.Process(100).Should().Be("any");
    }

    [Fact]
    [Trait("Category", "Matchers")]
    public void StrictMock_WithItIsAny_AllowsAnyMatchingValue()
    {
        // Arrange
        var mock = Mock.Create<ITestService>(MockBehavior.Strict);
        mock.Setup(x => x.Process(It.IsAny<int>())).Returns("allowed");

        // Act & Assert
        mock.Process(1).Should().Be("allowed");
        mock.Process(42).Should().Be("allowed");
        
        // But unsetup method should still throw
        Assert.Throws<MockException>(() => mock.Execute("test"));
    }

    [Fact]
    [Trait("Category", "Matchers")]
    public void Verify_WithItIsAnyForVoidMethod_WorksCorrectly()
    {
        // Arrange
        var mock = Mock.Create<ITestService>();

        // Act
        mock.Execute("command1");
        mock.Execute("command2");

        // Assert
        mock.Verify(x => x.Execute(It.IsAny<string>()), Times.Exactly(2));
        mock.Verify(x => x.Execute("command1"), Times.Once());
    }

    [Fact]
    [Trait("Category", "Matchers")]
    public void Setup_WithItIsAnyForDifferentTypes_WorksIndependently()
    {
        // Arrange
        var mock = Mock.Create<ITestService>();
        mock.Setup(x => x.Process(It.IsAny<int>())).Returns("int matched");
        mock.Setup(x => x.Execute(It.IsAny<string>())).Callback(() => { });

        // Act & Assert
        mock.Process(42).Should().Be("int matched");
        mock.Execute("test"); // Should not throw
    }
}
