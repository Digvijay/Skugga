using Skugga.Core;
using Xunit;

namespace Skugga.Core.Tests;

// Test interfaces with different property accessor combinations
public interface IReadOnlyProperty
{
    string Name { get; }
    int Count { get; }
}

public interface IWriteOnlyProperty
{
    string Value { set; }
}

public interface IMixedAccessors
{
    string ReadOnly { get; }
    int ReadWrite { get; set; }
}

public class PropertyAccessorTests
{
    [Fact]
    [Trait("Category", "Setup")]
    public void ReadOnlyProperty_CanRead_CannotWrite()
    {
        // Arrange
        var mock = Mock.Create<IReadOnlyProperty>();
        mock.Setup(x => x.Name).Returns("Test");

        // Act
        var name = mock.Name;

        // Assert
        Assert.Equal("Test", name);
    }

    [Fact]
    [Trait("Category", "Setup")]
    public void ReadOnlyProperty_WithSetupProperty_ShouldWork()
    {
        // Arrange
        var mock = Mock.Create<IReadOnlyProperty>();

        // This should work for read-only properties too (for testing scenarios)
        // SetupProperty provides a backing field even for read-only interface properties
        mock.SetupProperty(x => x.Name, "Initial");

        // Act
        var name = mock.Name;

        // Assert
        Assert.Equal("Initial", name);
    }

    [Fact]
    [Trait("Category", "Setup")]
    public void ReadOnlyProperty_VerifyGet_ShouldWork()
    {
        // Arrange
        var mock = Mock.Create<IReadOnlyProperty>();
        mock.Setup(x => x.Count).Returns(42);

        // Act
        _ = mock.Count;
        _ = mock.Count;

        // Assert
        mock.VerifyGet(x => x.Count, Times.Exactly(2));
    }

    [Fact]
    [Trait("Category", "Setup")]
    public void MixedAccessors_ReadOnlyProperty_ShouldOnlyHaveGetter()
    {
        // Arrange
        var mock = Mock.Create<IMixedAccessors>();
        mock.Setup(x => x.ReadOnly).Returns("ReadOnly");
        mock.SetupProperty(x => x.ReadWrite, 10);

        // Act
        var readOnly = mock.ReadOnly;
        mock.ReadWrite = 20;
        var readWrite = mock.ReadWrite;

        // Assert
        Assert.Equal("ReadOnly", readOnly);
        Assert.Equal(20, readWrite);
    }

    [Fact]
    [Trait("Category", "Setup")]
    public void MixedAccessors_ReadWriteProperty_ShouldHaveBoth()
    {
        // Arrange
        var mock = Mock.Create<IMixedAccessors>();
        mock.SetupProperty(x => x.ReadWrite);

        // Act
        mock.ReadWrite = 100;
        var result = mock.ReadWrite;

        // Assert
        Assert.Equal(100, result);
    }

    [Fact]
    [Trait("Category", "Setup")]
    public void MixedAccessors_VerifyBothProperties()
    {
        // Arrange
        var mock = Mock.Create<IMixedAccessors>();
        mock.Setup(x => x.ReadOnly).Returns("Test");
        mock.SetupProperty(x => x.ReadWrite);

        // Act
        _ = mock.ReadOnly;
        mock.ReadWrite = 50;

        // Assert
        mock.VerifyGet(x => x.ReadOnly, Times.Once());
        mock.VerifySet(x => x.ReadWrite, () => 50, Times.Once());
    }
}
