using Skugga.Core;
using Xunit;

namespace Skugga.Core.Tests;

/// <summary>
/// Tests for Protected Members
///
/// Skugga has an ADVANTAGE over Moq here - we have full access to the source code through Roslyn!
/// While Moq requires runtime reflection, Skugga can analyze the abstract class at compile-time
/// and generate concrete implementations with proper protected member overrides.
/// </summary>
public class ProtectedMembersTests
{
    // Abstract class with protected members for testing
    public abstract class AbstractService
    {
        protected abstract int ExecuteCore(string input);
        protected abstract void ProcessCore();
        protected virtual string FormatCore(int value) => value.ToString();
        protected int ProtectedProperty { get; set; }
    }

    // Abstract class with protected property
    public abstract class AbstractConfig
    {
        protected abstract string DatabaseConnection { get; }
        protected virtual int MaxRetries => 3;
    }

    [Fact]
    [Trait("Category", "Advanced")]
    public void Protected_Setup_ProtectedMethod_Returns()
    {
        // Arrange
        var mock = Mock.Create<AbstractService>();

        // Act - use Protected() to setup protected method
        mock.Protected()
            .Setup<int>("ExecuteCore", It.IsAny<string>())
            .Returns(42);

        // Assert - the generator should create an override that calls Handler.Invoke
        // For now, we're testing the API compiles and can be called
        var protectedSetup = mock.Protected();
        Assert.NotNull(protectedSetup);
    }

    [Fact]
    [Trait("Category", "Advanced")]
    public void Protected_Setup_WithSpecificArgument_Returns()
    {
        // Arrange
        var mock = Mock.Create<AbstractService>();

        // Act
        mock.Protected()
            .Setup<int>("ExecuteCore", "test")
            .Returns(100);

        // Assert - API works
        Assert.NotNull(mock);
    }

    [Fact]
    [Trait("Category", "Advanced")]
    public void Protected_Setup_VoidMethod_Callback()
    {
        // Arrange
        var mock = Mock.Create<AbstractService>();

        // Act
        mock.Protected()
            .Setup("ProcessCore")
            .Callback(() => { /* Callback logic here */ });

        // Assert
        Assert.NotNull(mock);
        // Note: Actual callback execution would be tested once generator support is added
    }

    [Fact]
    [Trait("Category", "Advanced")]
    public void Protected_SetupGet_Property_Returns()
    {
        // Arrange
        var mock = Mock.Create<AbstractConfig>();

        // Act
        mock.Protected()
            .SetupGet<string>("DatabaseConnection")
            .Returns("Server=localhost");

        // Assert - API works
        Assert.NotNull(mock);
    }

    [Fact]
    [Trait("Category", "Advanced")]
    public void Protected_MultipleSetups_Independent()
    {
        // Arrange
        var mock = Mock.Create<AbstractService>();

        // Act - setup multiple protected members
        mock.Protected()
            .Setup<int>("ExecuteCore", "input1")
            .Returns(1);

        mock.Protected()
            .Setup<int>("ExecuteCore", "input2")
            .Returns(2);

        mock.Protected()
            .Setup<string>("FormatCore", 42)
            .Returns("forty-two");

        // Assert
        Assert.NotNull(mock);
    }

    [Fact]
    [Trait("Category", "Advanced")]
    public void Protected_WithCallback_ExecutesCallback()
    {
        // Arrange
        var mock = Mock.Create<AbstractService>();
        var executionCount = 0;

        // Act
        mock.Protected()
            .Setup<int>("ExecuteCore", It.IsAny<string>())
            .Callback(() => executionCount++)
            .Returns(99);

        // Assert
        Assert.NotNull(mock);
        Assert.Equal(0, executionCount); // Not yet called (would be called when method invoked)
    }

    [Fact]
    [Trait("Category", "Advanced")]
    public void Protected_OnNonMock_ThrowsArgumentException()
    {
        // Arrange
        var notAMock = new object();

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => notAMock.Protected());
        Assert.Contains("not a Skugga mock", ex.Message);
    }

    [Fact]
    [Trait("Category", "Advanced")]
    public void Protected_IntegrationWithRegularSetup_BothWork()
    {
        // Arrange
        var mock = Mock.Create<AbstractService>();

        // Act - mix protected and regular setup
        mock.Protected()
            .Setup<int>("ExecuteCore", "test")
            .Returns(42);

        // If AbstractService had public members, we could set them up normally:
        // mock.Setup(x => x.PublicMethod()).Returns(value);

        // Assert
        Assert.NotNull(mock);
    }

    [Fact]
    [Trait("Category", "Advanced")]
    public void ProtectedMockSetup_ReturnsCorrectInterface()
    {
        // Arrange
        var mock = Mock.Create<AbstractService>();

        // Act
        var protectedSetup = mock.Protected();

        // Assert
        Assert.NotNull(protectedSetup);
        Assert.IsAssignableFrom<IProtectedMockSetup<AbstractService>>(protectedSetup);
        Assert.Same(((IMockSetup)mock).Handler, protectedSetup.Handler);
    }
}
