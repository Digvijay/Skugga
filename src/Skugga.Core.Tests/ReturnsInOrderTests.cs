using Skugga.Core;
using FluentAssertions;

namespace Skugga.Core.Tests;

public class ReturnsInOrderTests
{
    public interface ITestService
    {
        string GetNext();
        int GetNumber();
        object GetValue();
        string GetData(int id);
    }

    [Fact]
    public void ReturnsInOrder_WithThreeValues_ShouldReturnInSequence()
    {
        // Arrange
        var mock = Mock.Create<ITestService>();
        
        mock.Setup(x => x.GetNext()).ReturnsInOrder("first", "second", "third");
        
        // Act & Assert
        mock.GetNext().Should().Be("first");
        mock.GetNext().Should().Be("second");
        mock.GetNext().Should().Be("third");
    }

    [Fact]
    public void ReturnsInOrder_CalledMoreThanSequenceLength_ShouldReturnLastValue()
    {
        // Arrange
        var mock = Mock.Create<ITestService>();
        
        mock.Setup(x => x.GetNext()).ReturnsInOrder("first", "second", "third");
        
        // Act & Assert
        mock.GetNext().Should().Be("first");
        mock.GetNext().Should().Be("second");
        mock.GetNext().Should().Be("third");
        mock.GetNext().Should().Be("third"); // Should repeat last value
        mock.GetNext().Should().Be("third");
    }

    [Fact]
    public void ReturnsInOrder_WithSingleValue_ShouldAlwaysReturnThatValue()
    {
        // Arrange
        var mock = Mock.Create<ITestService>();
        
        mock.Setup(x => x.GetNext()).ReturnsInOrder("only");
        
        // Act & Assert
        mock.GetNext().Should().Be("only");
        mock.GetNext().Should().Be("only");
        mock.GetNext().Should().Be("only");
    }

    [Fact]
    public void ReturnsInOrder_WithNumbers_ShouldWorkCorrectly()
    {
        // Arrange
        var mock = Mock.Create<ITestService>();
        
        mock.Setup(x => x.GetNumber()).ReturnsInOrder(1, 2, 3, 4, 5);
        
        // Act & Assert
        mock.GetNumber().Should().Be(1);
        mock.GetNumber().Should().Be(2);
        mock.GetNumber().Should().Be(3);
        mock.GetNumber().Should().Be(4);
        mock.GetNumber().Should().Be(5);
    }

    [Fact]
    public void ReturnsInOrder_WithIEnumerable_ShouldWork()
    {
        // Arrange
        var mock = Mock.Create<ITestService>();
        var values = new List<string> { "a", "b", "c" };
        
        mock.Setup(x => x.GetNext()).ReturnsInOrder(values);
        
        // Act & Assert
        mock.GetNext().Should().Be("a");
        mock.GetNext().Should().Be("b");
        mock.GetNext().Should().Be("c");
    }

    [Fact]
    public void ReturnsInOrder_WithArray_ShouldWork()
    {
        // Arrange
        var mock = Mock.Create<ITestService>();
        var values = new[] { "x", "y", "z" };
        
        mock.Setup(x => x.GetNext()).ReturnsInOrder(values);
        
        // Act & Assert
        mock.GetNext().Should().Be("x");
        mock.GetNext().Should().Be("y");
        mock.GetNext().Should().Be("z");
    }

    [Fact]
    public void ReturnsInOrder_MultipleSetups_ShouldWorkIndependently()
    {
        // Arrange
        var mock = Mock.Create<ITestService>();
        
        mock.Setup(x => x.GetNext()).ReturnsInOrder("a", "b", "c");
        mock.Setup(x => x.GetNumber()).ReturnsInOrder(1, 2, 3);
        
        // Act & Assert
        mock.GetNext().Should().Be("a");
        mock.GetNumber().Should().Be(1);
        mock.GetNext().Should().Be("b");
        mock.GetNumber().Should().Be(2);
        mock.GetNext().Should().Be("c");
        mock.GetNumber().Should().Be(3);
    }

    [Fact]
    public void ReturnsInOrder_WithMethodArguments_ShouldWork()
    {
        // Arrange
        var mock = Mock.Create<ITestService>();
        
        mock.Setup(x => x.GetData(1)).ReturnsInOrder("first-1", "second-1", "third-1");
        mock.Setup(x => x.GetData(2)).ReturnsInOrder("first-2", "second-2");
        
        // Act & Assert
        mock.GetData(1).Should().Be("first-1");
        mock.GetData(2).Should().Be("first-2");
        mock.GetData(1).Should().Be("second-1");
        mock.GetData(2).Should().Be("second-2");
        mock.GetData(1).Should().Be("third-1");
    }

    [Fact]
    public void ReturnsInOrder_WithNullValues_ShouldWork()
    {
        // Arrange
        var mock = Mock.Create<ITestService>();
        
        mock.Setup(x => x.GetNext()).ReturnsInOrder("first", null!, "third");
        
        // Act & Assert
        mock.GetNext().Should().Be("first");
        mock.GetNext().Should().BeNull();
        mock.GetNext().Should().Be("third");
    }

    [Fact]
    public void ReturnsInOrder_WithCallback_ShouldExecuteCallbackEachTime()
    {
        // Arrange
        var mock = Mock.Create<ITestService>();
        var callCount = 0;
        
        mock.Setup(x => x.GetNext())
            .Callback(() => callCount++)
            .ReturnsInOrder("first", "second", "third");
        
        // Act
        mock.GetNext();
        mock.GetNext();
        mock.GetNext();
        
        // Assert
        callCount.Should().Be(3);
    }

    [Fact]
    public void ReturnsInOrder_WithVerify_ShouldWork()
    {
        // Arrange
        var mock = Mock.Create<ITestService>();
        
        mock.Setup(x => x.GetNext()).ReturnsInOrder("first", "second", "third");
        
        // Act
        mock.GetNext();
        mock.GetNext();
        mock.GetNext();
        
        // Assert
        mock.Verify(x => x.GetNext(), Times.Exactly(3));
    }

    [Fact]
    public void ReturnsInOrder_OverridingReturns_ShouldUseSequentialValues()
    {
        // Arrange
        var mock = Mock.Create<ITestService>();
        
        mock.Setup(x => x.GetNext())
            .Returns("static")
            .ReturnsInOrder("first", "second"); // Override static with sequential
        
        // Act & Assert
        mock.GetNext().Should().Be("first");
        mock.GetNext().Should().Be("second");
    }

    [Fact]
    public void ReturnsInOrder_OverridingReturnsWithFunc_ShouldUseSequentialValues()
    {
        // Arrange
        var mock = Mock.Create<ITestService>();
        var counter = 0;
        
        mock.Setup(x => x.GetNumber())
            .Returns(() => ++counter)
            .ReturnsInOrder(10, 20, 30); // Override function with sequential
        
        // Act & Assert
        mock.GetNumber().Should().Be(10);
        mock.GetNumber().Should().Be(20);
        mock.GetNumber().Should().Be(30);
        counter.Should().Be(0); // Function should not have been called
    }

    [Fact]
    public void ReturnsInOrder_WithStrictMock_ShouldWork()
    {
        // Arrange
        var mock = Mock.Create<ITestService>(MockBehavior.Strict);
        
        mock.Setup(x => x.GetNext()).ReturnsInOrder("first", "second");
        
        // Act & Assert
        mock.GetNext().Should().Be("first");
        mock.GetNext().Should().Be("second");
    }

    [Fact]
    public void ReturnsInOrder_WithMixedTypes_ShouldWork()
    {
        // Arrange
        var mock = Mock.Create<ITestService>();
        
        mock.Setup(x => x.GetValue()).ReturnsInOrder(
            (object)1,
            (object)"text",
            (object)true,
            (object)3.14
        );
        
        // Act & Assert
        mock.GetValue().Should().Be(1);
        mock.GetValue().Should().Be("text");
        mock.GetValue().Should().Be(true);
        mock.GetValue().Should().Be(3.14);
    }

    [Fact]
    public void ReturnsInOrder_CallbackBeforeReturns_ShouldExecuteCallbackThenReturnSequential()
    {
        // Arrange
        var mock = Mock.Create<ITestService>();
        var executionOrder = new List<string>();
        
        mock.Setup(x => x.GetNext())
            .Callback(() => executionOrder.Add("callback"))
            .ReturnsInOrder("first", "second");
        
        // Act
        executionOrder.Add("before");
        var result1 = mock.GetNext();
        executionOrder.Add("after-1");
        var result2 = mock.GetNext();
        executionOrder.Add("after-2");
        
        // Assert
        executionOrder.Should().Equal("before", "callback", "after-1", "callback", "after-2");
        result1.Should().Be("first");
        result2.Should().Be("second");
    }

    [Fact]
    public void ReturnsInOrder_EmptySequence_ShouldReturnNull()
    {
        // Arrange
        var mock = Mock.Create<ITestService>();
        
        mock.Setup(x => x.GetNext()).ReturnsInOrder();
        
        // Act
        var result = mock.GetNext();
        
        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void ReturnsInOrder_CalledOnceOnly_ShouldReturnFirstValue()
    {
        // Arrange
        var mock = Mock.Create<ITestService>();
        
        mock.Setup(x => x.GetNext()).ReturnsInOrder("first", "second", "third");
        
        // Act
        var result = mock.GetNext();
        
        // Assert
        result.Should().Be("first");
    }
}
