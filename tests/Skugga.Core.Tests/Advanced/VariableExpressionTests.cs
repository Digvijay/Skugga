using Xunit;
using Skugga.Core;

namespace Skugga.Core.Tests;

public class VariableExpressionTests
{
    public interface ICalculator
    {
        int Add(int a, int b);
        string Process(string value);
        bool Compare(int x, int y);
        void Execute(int value);
    }
    
    [Fact]
    [Trait("Category", "Advanced")]
    public void Setup_WithLocalVariable_ShouldWork()
    {
        // Arrange
        var mock = Mock.Create<ICalculator>();
        int expectedValue = 42;
        
        // Act - Setup with variable
        mock.Setup(x => x.Add(expectedValue, 10)).Returns(100);
        
        // Assert
        var result = mock.Add(42, 10);
        Assert.Equal(100, result);
    }
    
    [Fact]
    [Trait("Category", "Advanced")]
    public void Setup_WithFieldValue_ShouldWork()
    {
        // Arrange
        var mock = Mock.Create<ICalculator>();
        var testData = new TestData { Value = 5 };
        
        // Act - Setup with field access
        mock.Setup(x => x.Add(testData.Value, 3)).Returns(8);
        
        // Assert
        var result = mock.Add(5, 3);
        Assert.Equal(8, result);
    }
    
    [Fact]
    [Trait("Category", "Advanced")]
    public void Setup_WithCalculation_ShouldWork()
    {
        // Arrange
        var mock = Mock.Create<ICalculator>();
        int baseValue = 10;
        
        // Act - Setup with calculation
        mock.Setup(x => x.Add(baseValue * 2, 5)).Returns(25);
        
        // Assert
        var result = mock.Add(20, 5);
        Assert.Equal(25, result);
    }
    
    [Fact]
    [Trait("Category", "Advanced")]
    public void Setup_WithStringVariable_ShouldWork()
    {
        // Arrange
        var mock = Mock.Create<ICalculator>();
        string input = "test";
        
        // Act
        mock.Setup(x => x.Process(input)).Returns("result");
        
        // Assert
        var result = mock.Process("test");
        Assert.Equal("result", result);
    }
    
    [Fact]
    [Trait("Category", "Advanced")]
    public void Verify_WithVariable_ShouldWork()
    {
        // Arrange
        var mock = Mock.Create<ICalculator>();
        int value = 42;
        
        // Act
        mock.Add(42, 10);
        
        // Assert - Verify with variable
        mock.Verify(x => x.Add(value, 10), Times.Once());
    }
    
    [Fact]
    [Trait("Category", "Advanced")]
    public void Setup_VoidMethod_WithVariable_ShouldWork()
    {
        // Arrange
        var mock = Mock.Create<ICalculator>();
        int value = 123;
        int callbackValue = 0;
        
        // Act
        mock.Setup(x => x.Execute(value)).Callback(() => callbackValue = value);
        mock.Execute(123);
        
        // Assert
        Assert.Equal(123, callbackValue);
    }
    
    [Fact]
    [Trait("Category", "Advanced")]
    public void Setup_WithMixedConstantAndVariable_ShouldWork()
    {
        // Arrange
        var mock = Mock.Create<ICalculator>();
        int x = 10;
        
        // Act - Mix of variable and constant
        mock.Setup(m => m.Add(x, 20)).Returns(30);
        
        // Assert
        var result = mock.Add(10, 20);
        Assert.Equal(30, result);
    }
    
    [Fact]
    [Trait("Category", "Advanced")]
    public void Setup_WithTernaryExpression_ShouldWork()
    {
        // Arrange
        var mock = Mock.Create<ICalculator>();
        bool useHighValue = true;
        
        // Act - Ternary in argument
        mock.Setup(x => x.Add(useHighValue ? 100 : 10, 5)).Returns(105);
        
        // Assert
        var result = mock.Add(100, 5);
        Assert.Equal(105, result);
    }
    
    [Fact]
    [Trait("Category", "Advanced")]
    public void Setup_WithArrayAccess_ShouldWork()
    {
        // Arrange
        var mock = Mock.Create<ICalculator>();
        int[] values = { 1, 2, 3, 4, 5 };
        
        // Act - Array indexer
        mock.Setup(x => x.Add(values[0], values[1])).Returns(3);
        
        // Assert
        var result = mock.Add(1, 2);
        Assert.Equal(3, result);
    }
    
    [Fact]
    [Trait("Category", "Advanced")]
    public void Setup_WithComplexExpression_ShouldWork()
    {
        // Arrange
        var mock = Mock.Create<ICalculator>();
        int baseValue = 5;
        int multiplier = 3;
        
        // Act - Complex calculation
        mock.Setup(x => x.Add((baseValue + 2) * multiplier, 10)).Returns(31);
        
        // Assert
        var result = mock.Add(21, 10);
        Assert.Equal(31, result);
    }
    
    private class TestData
    {
        public int Value { get; set; }
    }
}
