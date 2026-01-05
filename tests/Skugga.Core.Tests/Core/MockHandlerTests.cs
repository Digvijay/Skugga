using Skugga.Core;

namespace Skugga.Core.Tests;

/// <summary>
/// Tests for MockHandler - the core mock execution engine
/// </summary>
public class MockHandlerTests
{
    [Fact]
    [Trait("Category", "Core")]
    public void AddSetup_ShouldStoreConfiguration()
    {
        // Arrange
        var handler = new MockHandler();

        // Act
        handler.AddSetup("GetData", new object[] { 1 }, "result");
        var result = handler.Invoke("GetData", new object[] { 1 });

        // Assert
        result.Should().Be("result");
    }

    [Fact]
    [Trait("Category", "Core")]
    public void Invoke_WithoutSetup_InLooseMode_ShouldReturnNull()
    {
        // Arrange
        var handler = new MockHandler { Behavior = MockBehavior.Loose };

        // Act
        var result = handler.Invoke("GetData", new object[] { 1 });

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    [Trait("Category", "Core")]
    public void Invoke_WithoutSetup_InStrictMode_ShouldThrowException()
    {
        // Arrange
        var handler = new MockHandler { Behavior = MockBehavior.Strict };

        // Act
        var act = () => handler.Invoke("GetData", new object[] { 1 });

        // Assert
        act.Should().Throw<MockException>()
            .WithMessage("*Strict Mode*")
            .WithMessage("*GetData*");
    }

    [Fact]
    [Trait("Category", "Core")]
    public void Invoke_WithMatchingSetup_ShouldReturnConfiguredValue()
    {
        // Arrange
        var handler = new MockHandler();
        handler.AddSetup("Calculate", new object[] { 2, 3 }, 5);

        // Act
        var result = handler.Invoke("Calculate", new object[] { 2, 3 });

        // Assert
        result.Should().Be(5);
    }

    [Fact]
    [Trait("Category", "Core")]
    public void Invoke_WithNonMatchingParameters_ShouldReturnNull()
    {
        // Arrange
        var handler = new MockHandler();
        handler.AddSetup("GetData", new object[] { 1 }, "data-1");

        // Act
        var result = handler.Invoke("GetData", new object[] { 2 });

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    [Trait("Category", "Core")]
    public void Invoke_WithMultipleSetups_ShouldReturnCorrectValue()
    {
        // Arrange
        var handler = new MockHandler();
        handler.AddSetup("GetData", new object[] { 1 }, "data-1");
        handler.AddSetup("GetData", new object[] { 2 }, "data-2");
        handler.AddSetup("GetData", new object[] { 3 }, "data-3");

        // Act & Assert
        handler.Invoke("GetData", new object[] { 1 }).Should().Be("data-1");
        handler.Invoke("GetData", new object[] { 2 }).Should().Be("data-2");
        handler.Invoke("GetData", new object[] { 3 }).Should().Be("data-3");
    }

    [Fact]
    [Trait("Category", "Core")]
    public void AddSetup_WithNullParameter_ShouldMatchNull()
    {
        // Arrange
        var handler = new MockHandler();
        handler.AddSetup("Process", new object?[] { null }, "null-result");

        // Act
        var result = handler.Invoke("Process", new object?[] { null });

        // Assert
        result.Should().Be("null-result");
    }

    [Fact]
    [Trait("Category", "Core")]
    public void AddSetup_WithEmptyParameters_ShouldMatch()
    {
        // Arrange
        var handler = new MockHandler();
        handler.AddSetup("GetValue", Array.Empty<object>(), 42);

        // Act
        var result = handler.Invoke("GetValue", Array.Empty<object>());

        // Assert
        result.Should().Be(42);
    }

    [Fact]
    [Trait("Category", "Core")]
    public void Behavior_DefaultValue_ShouldBeLoose()
    {
        // Arrange & Act
        var handler = new MockHandler();

        // Assert
        handler.Behavior.Should().Be(MockBehavior.Loose);
    }

    [Fact]
    [Trait("Category", "Core")]
    public void Behavior_CanBeChanged_FromLooseToStrict()
    {
        // Arrange
        var handler = new MockHandler();

        // Act
        handler.Behavior = MockBehavior.Strict;

        // Assert
        handler.Behavior.Should().Be(MockBehavior.Strict);
    }
}
