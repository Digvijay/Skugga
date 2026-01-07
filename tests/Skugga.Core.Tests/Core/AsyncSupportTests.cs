using FluentAssertions;
using Skugga.Core;
using Xunit;

namespace Skugga.Core.Tests;

/// <summary>
/// Tests for async/await support in Skugga mocking.
/// </summary>
public class AsyncSupportTests
{
    public interface IAsyncService
    {
        Task<string> GetDataAsync(int id);
        Task<int> CalculateAsync(int a, int b);
        Task ProcessAsync(string data);
        Task<List<string>> GetItemsAsync();
    }

    [Fact]
    [Trait("Category", "Core")]
    public async Task ReturnsAsync_WithValue_ReturnsCompletedTask()
    {
        // Arrange
        var mock = Mock.Create<IAsyncService>();
        mock.Setup(x => x.GetDataAsync(42)).ReturnsAsync("test data");

        // Act
        var result = await mock.GetDataAsync(42);

        // Assert
        result.Should().Be("test data");
    }

    [Fact]
    [Trait("Category", "Core")]
    public async Task ReturnsAsync_WithFunction_ReturnsComputedValue()
    {
        // Arrange
        var mock = Mock.Create<IAsyncService>();
        int counter = 0;
        mock.Setup(x => x.GetDataAsync(42)).ReturnsAsync(() => $"call-{++counter}");

        // Act
        var result1 = await mock.GetDataAsync(42);
        var result2 = await mock.GetDataAsync(42);

        // Assert
        result1.Should().Be("call-1");
        result2.Should().Be("call-2");
    }

    [Fact]
    [Trait("Category", "Core")]
    public async Task ReturnsAsync_WithArgFunction_UsesArgumentValue()
    {
        // Arrange
        var mock = Mock.Create<IAsyncService>();
        mock.Setup(x => x.CalculateAsync(It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync((int a, int b) => a + b);

        // Act
        var result = await mock.CalculateAsync(10, 32);

        // Assert
        result.Should().Be(42);
    }

    [Fact]
    [Trait("Category", "Core")]
    public async Task UnsetupTaskMethod_ReturnsCompletedTask()
    {
        // Arrange - Loose mode (default)
        var mock = Mock.Create<IAsyncService>();

        // Act & Assert - Should not throw NullReferenceException
        await mock.ProcessAsync("test");
    }

    [Fact]
    [Trait("Category", "Core")]
    public async Task UnsetupTaskOfT_ReturnsDefault()
    {
        // Arrange - Loose mode (default)
        var mock = Mock.Create<IAsyncService>();

        // Act
        var result = await mock.GetDataAsync(42);

        // Assert - Should return null (default for string) not throw
        result.Should().BeNull();
    }

    [Fact]
    [Trait("Category", "Core")]
    public async Task UnsetupTaskOfList_ReturnsDefault()
    {
        // Arrange
        var mock = Mock.Create<IAsyncService>();

        // Act
        var result = await mock.GetItemsAsync();

        // Assert - Should return null (default for List<string>) not throw
        result.Should().BeNull();
    }

    [Fact]
    [Trait("Category", "Core")]
    public async Task TraditionalReturnsWithTaskFromResult_StillWorks()
    {
        // Arrange - Ensure backwards compatibility
        var mock = Mock.Create<IAsyncService>();
        mock.Setup(x => x.GetDataAsync(42)).Returns(Task.FromResult("test"));

        // Act
        var result = await mock.GetDataAsync(42);

        // Assert
        result.Should().Be("test");
    }

    [Fact]
    [Trait("Category", "Core")]
    public void Returns_WithFourArguments_WorksCorrectly()
    {
        // Arrange - Test extended overload support
        var mock = Mock.Create<ICalculator>();
        mock.Setup(x => x.Compute(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()))
            .Returns((int a, int b, int c, int d) => a + b + c + d);

        // Act
        var result = mock.Compute(1, 2, 3, 4);

        // Assert
        result.Should().Be(10);
    }

    [Fact]
    [Trait("Category", "Core")]
    public async Task ReturnsAsync_WithFourArguments_WorksCorrectly()
    {
        // Arrange - Test extended async overload support
        var mock = Mock.Create<ICalculator>();
        mock.Setup(x => x.ComputeAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync((int a, int b, int c, int d) => a * b * c * d);

        // Act
        var result = await mock.ComputeAsync(2, 3, 4, 5);

        // Assert
        result.Should().Be(120);
    }

    public interface ICalculator
    {
        int Compute(int a, int b, int c, int d);
        Task<int> ComputeAsync(int a, int b, int c, int d);
    }
}
