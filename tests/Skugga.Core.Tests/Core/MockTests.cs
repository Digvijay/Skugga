using Skugga.Core;

namespace Skugga.Core.Tests;

/// <summary>
/// Tests for Mock.Create functionality
/// Following AAA pattern (Arrange-Act-Assert)
/// </summary>
public class MockTests
{
    public interface ITestService
    {
        string GetData(int id);
        int Calculate(int a, int b);
        bool IsValid();
    }

    public interface IPropertyService
    {
        string Name { get; }
        int Count { get; }
    }

    [Fact]
    [Trait("Category", "Core")]
    public void Create_WithLooseBehavior_ShouldReturnMockInstance()
    {
        // Act
        var mock = Mock.Create<ITestService>();

        // Assert
        mock.Should().NotBeNull();
        mock.Should().BeAssignableTo<ITestService>();
    }

    [Fact]
    [Trait("Category", "Core")]
    public void Create_WithStrictBehavior_ShouldReturnMockInstance()
    {
        // Act
        var mock = Mock.Create<ITestService>(MockBehavior.Strict);

        // Assert
        mock.Should().NotBeNull();
        mock.Should().BeAssignableTo<ITestService>();
    }

    [Fact]
    [Trait("Category", "Core")]
    public void Create_CallingUnconfiguredMethod_WithLooseBehavior_ShouldReturnDefault()
    {
        // Arrange
        var mock = Mock.Create<ITestService>(MockBehavior.Loose);

        // Act
        var result = mock.GetData(1);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    [Trait("Category", "Core")]
    public void Create_CallingUnconfiguredMethod_WithStrictBehavior_ShouldThrowMockException()
    {
        // Arrange
        var mock = Mock.Create<ITestService>(MockBehavior.Strict);

        // Act
        var act = () => mock.GetData(1);

        // Assert
        act.Should().Throw<MockException>()
            .WithMessage("*Strict Mode*");
    }

    [Fact]
    [Trait("Category", "Core")]
    public void Setup_WithReturnValue_ShouldReturnConfiguredValue()
    {
        // Arrange
        var mock = Mock.Create<ITestService>();
        mock.Setup(x => x.GetData(1)).Returns("test-data");

        // Act
        var result = mock.GetData(1);

        // Assert
        result.Should().Be("test-data");
    }

    [Fact]
    [Trait("Category", "Core")]
    public void Setup_WithDifferentParameters_ShouldReturnDifferentValues()
    {
        // Arrange
        var mock = Mock.Create<ITestService>();
        mock.Setup(x => x.GetData(1)).Returns("data-1");
        mock.Setup(x => x.GetData(2)).Returns("data-2");

        // Act & Assert
        mock.GetData(1).Should().Be("data-1");
        mock.GetData(2).Should().Be("data-2");
    }

    [Fact]
    [Trait("Category", "Core")]
    public void Setup_WithValueTypeReturn_ShouldReturnConfiguredValue()
    {
        // Arrange
        var mock = Mock.Create<ITestService>();
        mock.Setup(x => x.Calculate(2, 3)).Returns(5);

        // Act
        var result = mock.Calculate(2, 3);

        // Assert
        result.Should().Be(5);
    }

    [Fact]
    [Trait("Category", "Core")]
    public void Setup_CallingWithDifferentParameters_ShouldReturnDefault()
    {
        // Arrange
        var mock = Mock.Create<ITestService>();
        mock.Setup(x => x.GetData(1)).Returns("test");

        // Act
        var result = mock.GetData(999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    [Trait("Category", "Core")]
    public void Setup_PropertyGetter_ShouldReturnConfiguredValue()
    {
        // Arrange
        var mock = Mock.Create<IPropertyService>();
        // Property setup now uses unified Setup method with type inference
        mock.Setup(x => x.Name).Returns("TestName");

        // Act
        var result = mock.Name;

        // Assert
        result.Should().Be("TestName");
    }

    [Fact]
    [Trait("Category", "Core")]
    public void Setup_MultipleProperties_ShouldReturnConfiguredValues()
    {
        // Arrange
        var mock = Mock.Create<IPropertyService>();
        mock.Setup(x => x.Name).Returns("TestName");
        mock.Setup(x => x.Count).Returns(42);

        // Act & Assert
        mock.Name.Should().Be("TestName");
        mock.Count.Should().Be(42);
    }

    [Theory]
    [Trait("Category", "Core")]
    [InlineData(true)]
    [InlineData(false)]
    public void Setup_BooleanReturn_ShouldReturnConfiguredValue(bool expected)
    {
        // Arrange
        var mock = Mock.Create<ITestService>();
        mock.Setup(x => x.IsValid()).Returns(expected);

        // Act
        var result = mock.IsValid();

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    [Trait("Category", "Core")]
    public void Create_ShouldUseFallbackWhenInterceptorNotAvailable()
    {
        // This test verifies the runtime fallback works
        // In production, interceptors would be used, but the fallback ensures compatibility
        
        // Arrange & Act
        var mock = Mock.Create<ITestService>();

        // Assert
        mock.Should().NotBeNull();
        mock.Should().BeAssignableTo<IMockSetup>();
    }
}
