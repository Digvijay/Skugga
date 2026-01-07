using FluentAssertions;
using Skugga.Core;

namespace Skugga.Core.Tests;

public class ReturnsWithFunctionTests
{
    public interface ITestService
    {
        int GetValue();
        string GetData();
        int Double(int x);
        string Format(int value);
        int Add(int a, int b);
        string Concat(string a, string b);
        int Sum(int a, int b, int c);
        string Join(string a, string b, string c);
    }

    [Fact]
    [Trait("Category", "Advanced")]
    public void Returns_WithFuncNoArgs_ShouldEvaluateFunctionOnEachCall()
    {
        // Arrange
        var mock = Mock.Create<ITestService>();
        var counter = 0;

        mock.Setup(x => x.GetValue()).Returns(() => ++counter);

        // Act & Assert
        mock.GetValue().Should().Be(1);
        mock.GetValue().Should().Be(2);
        mock.GetValue().Should().Be(3);
    }

    [Fact]
    [Trait("Category", "Advanced")]
    public void Returns_WithFuncNoArgs_MultipleSetups_ShouldWorkIndependently()
    {
        // Arrange
        var mock = Mock.Create<ITestService>();
        var valueCounter = 0;
        var dataCounter = 0;

        mock.Setup(x => x.GetValue()).Returns(() => ++valueCounter);
        mock.Setup(x => x.GetData()).Returns(() => $"data-{++dataCounter}");

        // Act & Assert
        mock.GetValue().Should().Be(1);
        mock.GetData().Should().Be("data-1");
        mock.GetValue().Should().Be(2);
        mock.GetData().Should().Be("data-2");
    }

    [Fact]
    [Trait("Category", "Advanced")]
    public void Returns_WithFuncOneArg_ShouldPassArgumentToFunction()
    {
        // Arrange
        var mock = Mock.Create<ITestService>();

        mock.Setup(x => x.Double(5)).Returns((int x) => x * 2);

        // Act
        var result = mock.Double(5);

        // Assert
        result.Should().Be(10);
    }

    [Fact]
    [Trait("Category", "Advanced")]
    public void Returns_WithFuncOneArg_DifferentArguments_ShouldUseSeparateSetups()
    {
        // Arrange
        var mock = Mock.Create<ITestService>();

        mock.Setup(x => x.Double(5)).Returns((int x) => x * 2);
        mock.Setup(x => x.Double(10)).Returns((int x) => x * 3);

        // Act & Assert
        mock.Double(5).Should().Be(10);
        mock.Double(10).Should().Be(30);
    }

    [Fact]
    [Trait("Category", "Advanced")]
    public void Returns_WithFuncOneArg_StringProcessing_ShouldWork()
    {
        // Arrange
        var mock = Mock.Create<ITestService>();

        mock.Setup(x => x.Format(42)).Returns((int value) => $"Value: {value}");

        // Act
        var result = mock.Format(42);

        // Assert
        result.Should().Be("Value: 42");
    }

    [Fact]
    [Trait("Category", "Advanced")]
    public void Returns_WithFuncTwoArgs_ShouldPassBothArgumentsToFunction()
    {
        // Arrange
        var mock = Mock.Create<ITestService>();

        mock.Setup(x => x.Add(10, 20)).Returns((int a, int b) => a + b);

        // Act
        var result = mock.Add(10, 20);

        // Assert
        result.Should().Be(30);
    }

    [Fact]
    [Trait("Category", "Advanced")]
    public void Returns_WithFuncTwoArgs_ComplexLogic_ShouldWork()
    {
        // Arrange
        var mock = Mock.Create<ITestService>();

        mock.Setup(x => x.Add(5, 3)).Returns((int a, int b) =>
        {
            if (a > b) return a - b;
            return a + b;
        });

        // Act
        var result = mock.Add(5, 3);

        // Assert
        result.Should().Be(2); // 5 > 3, so 5 - 3 = 2
    }

    [Fact]
    [Trait("Category", "Advanced")]
    public void Returns_WithFuncTwoArgs_StringConcatenation_ShouldWork()
    {
        // Arrange
        var mock = Mock.Create<ITestService>();

        mock.Setup(x => x.Concat("Hello", "World")).Returns((string a, string b) => $"{a} {b}");

        // Act
        var result = mock.Concat("Hello", "World");

        // Assert
        result.Should().Be("Hello World");
    }

    [Fact]
    [Trait("Category", "Advanced")]
    public void Returns_WithFuncThreeArgs_ShouldPassAllArgumentsToFunction()
    {
        // Arrange
        var mock = Mock.Create<ITestService>();

        mock.Setup(x => x.Sum(10, 20, 30)).Returns((int a, int b, int c) => a + b + c);

        // Act
        var result = mock.Sum(10, 20, 30);

        // Assert
        result.Should().Be(60);
    }

    [Fact]
    [Trait("Category", "Advanced")]
    public void Returns_WithFuncThreeArgs_StringJoin_ShouldWork()
    {
        // Arrange
        var mock = Mock.Create<ITestService>();

        mock.Setup(x => x.Join("A", "B", "C")).Returns((string a, string b, string c) => $"{a}-{b}-{c}");

        // Act
        var result = mock.Join("A", "B", "C");

        // Assert
        result.Should().Be("A-B-C");
    }

    [Fact]
    [Trait("Category", "Advanced")]
    public void Returns_WithFuncThreeArgs_ComplexCalculation_ShouldWork()
    {
        // Arrange
        var mock = Mock.Create<ITestService>();

        mock.Setup(x => x.Sum(2, 3, 4)).Returns((int a, int b, int c) =>
        {
            var sum = a + b + c;
            return sum * 10;
        });

        // Act
        var result = mock.Sum(2, 3, 4);

        // Assert
        result.Should().Be(90);
    }

    [Fact]
    [Trait("Category", "Advanced")]
    public void Returns_WithFunc_AndCallback_ShouldExecuteBoth()
    {
        // Arrange
        var mock = Mock.Create<ITestService>();
        var callbackExecuted = false;

        mock.Setup(x => x.GetValue())
            .Callback(() => callbackExecuted = true)
            .Returns(() => 42);

        // Act
        var result = mock.GetValue();

        // Assert
        result.Should().Be(42);
        callbackExecuted.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "Advanced")]
    public void Returns_WithFunc_ThenStaticValue_ShouldUseStaticValue()
    {
        // Arrange
        var mock = Mock.Create<ITestService>();
        var counter = 0;

        mock.Setup(x => x.GetValue())
            .Returns(() => ++counter)
            .Returns(100); // Override with static value

        // Act & Assert
        mock.GetValue().Should().Be(100);
        mock.GetValue().Should().Be(100); // Should not increment
    }

    [Fact]
    [Trait("Category", "Advanced")]
    public void Returns_StaticValue_ThenFunc_ShouldUseFunc()
    {
        // Arrange
        var mock = Mock.Create<ITestService>();
        var counter = 0;

        mock.Setup(x => x.GetValue())
            .Returns(100)
            .Returns(() => ++counter); // Override with function

        // Act & Assert
        mock.GetValue().Should().Be(1);
        mock.GetValue().Should().Be(2);
    }

    [Fact]
    [Trait("Category", "Advanced")]
    public void Returns_WithFunc_CalledMultipleTimes_ShouldEvaluateEachTime()
    {
        // Arrange
        var mock = Mock.Create<ITestService>();
        var values = new List<int> { 10, 20, 30 };
        var index = 0;

        mock.Setup(x => x.GetValue()).Returns(() => values[index++]);

        // Act & Assert
        mock.GetValue().Should().Be(10);
        mock.GetValue().Should().Be(20);
        mock.GetValue().Should().Be(30);
    }

    [Fact]
    [Trait("Category", "Advanced")]
    public void Returns_WithFunc_AccessingExternalState_ShouldWork()
    {
        // Arrange
        var mock = Mock.Create<ITestService>();
        var multiplier = 2;

        mock.Setup(x => x.Double(5)).Returns((int x) => x * multiplier);

        // Act
        var result1 = mock.Double(5);

        multiplier = 3;
        var result2 = mock.Double(5);

        // Assert
        result1.Should().Be(10);
        result2.Should().Be(15);
    }

    [Fact]
    [Trait("Category", "Advanced")]
    public void Returns_WithFunc_ThrowingException_ShouldThrow()
    {
        // Arrange
        var mock = Mock.Create<ITestService>();

        mock.Setup(x => x.GetValue()).Returns(() => throw new InvalidOperationException("Test exception"));

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => mock.GetValue());
        exception.Message.Should().Be("Test exception");
    }

    [Fact]
    [Trait("Category", "Advanced")]
    public void Returns_WithFunc_AndVerify_ShouldBothWork()
    {
        // Arrange
        var mock = Mock.Create<ITestService>();
        var counter = 0;

        mock.Setup(x => x.GetValue()).Returns(() => ++counter);

        // Act
        mock.GetValue();
        mock.GetValue();

        // Assert
        counter.Should().Be(2);
        mock.Verify(x => x.GetValue(), Times.Exactly(2));
    }
}
