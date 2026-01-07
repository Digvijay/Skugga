using Skugga.Core;
using Xunit;

namespace Skugga.Core.Tests;

// Test interfaces for property verification
public interface ISettings
{
    string Theme { get; set; }
    int FontSize { get; set; }
    bool DarkMode { get; set; }
}

public interface ICounter
{
    int Value { get; set; }
    string Name { get; set; }
}

public class VerifyPropertyTests
{
    [Fact]
    [Trait("Category", "Verification")]
    public void VerifyGet_PropertyAccessed_ShouldPass()
    {
        // Arrange
        var mock = Mock.Create<ISettings>();
        mock.Setup(x => x.Theme).Returns("Dark");

        // Act
        _ = mock.Theme;

        // Assert
        mock.VerifyGet(x => x.Theme, Times.Once());
    }

    [Fact]
    [Trait("Category", "Verification")]
    public void VerifyGet_PropertyNotAccessed_ShouldThrow()
    {
        // Arrange
        var mock = Mock.Create<ISettings>();
        mock.Setup(x => x.Theme).Returns("Dark");

        // Act - don't access property

        // Assert
        var ex = Assert.Throws<MockException>(() =>
            mock.VerifyGet(x => x.Theme, Times.Once()));
        Assert.Contains("Expected exactly 1 call(s) to 'get_Theme'", ex.Message);
    }

    [Fact]
    [Trait("Category", "Verification")]
    public void VerifyGet_PropertyAccessedMultipleTimes_ShouldVerifyCorrectCount()
    {
        // Arrange
        var mock = Mock.Create<ISettings>();
        mock.Setup(x => x.Theme).Returns("Dark");

        // Act
        _ = mock.Theme;
        _ = mock.Theme;
        _ = mock.Theme;

        // Assert
        mock.VerifyGet(x => x.Theme, Times.Exactly(3));
    }

    [Fact]
    [Trait("Category", "Verification")]
    public void VerifyGet_WithSetupProperty_ShouldTrackAccess()
    {
        // Arrange
        var mock = Mock.Create<ISettings>();
        mock.SetupProperty(x => x.Theme, "Light");

        // Act
        _ = mock.Theme;
        _ = mock.Theme;

        // Assert
        mock.VerifyGet(x => x.Theme, Times.Exactly(2));
    }

    [Fact]
    [Trait("Category", "Verification")]
    public void VerifySet_PropertySet_ShouldPass()
    {
        // Arrange
        var mock = Mock.Create<ISettings>();
        mock.SetupProperty(x => x.Theme);

        // Act
        mock.Theme = "Dark";

        // Assert
        mock.VerifySet(x => x.Theme, () => "Dark", Times.Once());
    }

    [Fact]
    [Trait("Category", "Verification")]
    public void VerifySet_PropertyNotSet_ShouldThrow()
    {
        // Arrange
        var mock = Mock.Create<ISettings>();
        mock.SetupProperty(x => x.Theme);

        // Act - don't set property

        // Assert
        var ex = Assert.Throws<MockException>(() =>
            mock.VerifySet(x => x.Theme, () => "Dark", Times.Once()));
        Assert.Contains("Expected exactly 1 call(s) to 'set_Theme'", ex.Message);
    }

    [Fact]
    [Trait("Category", "Verification")]
    public void VerifySet_PropertySetMultipleTimes_ShouldVerifyCorrectCount()
    {
        // Arrange
        var mock = Mock.Create<ISettings>();
        mock.SetupProperty(x => x.Theme);

        // Act
        mock.Theme = "Dark";
        mock.Theme = "Light";
        mock.Theme = "Dark";

        // Assert
        mock.VerifySet(x => x.Theme, () => "Dark", Times.Exactly(2));
        mock.VerifySet(x => x.Theme, () => "Light", Times.Once());
    }

    [Fact]
    [Trait("Category", "Verification")]
    public void VerifySet_WithDifferentValue_ShouldNotMatch()
    {
        // Arrange
        var mock = Mock.Create<ISettings>();
        mock.SetupProperty(x => x.Theme);

        // Act
        mock.Theme = "Dark";

        // Assert
        var ex = Assert.Throws<MockException>(() =>
            mock.VerifySet(x => x.Theme, () => "Light", Times.Once()));
        Assert.Contains("was called 0 time(s)", ex.Message);
    }

    [Fact]
    [Trait("Category", "Verification")]
    public void VerifySet_IntProperty_ShouldWork()
    {
        // Arrange
        var mock = Mock.Create<ISettings>();
        mock.SetupProperty(x => x.FontSize);

        // Act
        mock.FontSize = 12;
        mock.FontSize = 14;
        mock.FontSize = 12;

        // Assert
        mock.VerifySet(x => x.FontSize, () => 12, Times.Exactly(2));
        mock.VerifySet(x => x.FontSize, () => 14, Times.Once());
    }

    [Fact]
    [Trait("Category", "Verification")]
    public void VerifySet_BoolProperty_ShouldWork()
    {
        // Arrange
        var mock = Mock.Create<ISettings>();
        mock.SetupProperty(x => x.DarkMode);

        // Act
        mock.DarkMode = true;
        mock.DarkMode = false;

        // Assert
        mock.VerifySet(x => x.DarkMode, () => true, Times.Once());
        mock.VerifySet(x => x.DarkMode, () => false, Times.Once());
    }

    [Fact]
    [Trait("Category", "Verification")]
    public void VerifySet_WithItIsAny_ShouldMatchAnyValue()
    {
        // Arrange
        var mock = Mock.Create<ISettings>();
        mock.SetupProperty(x => x.Theme);

        // Act
        mock.Theme = "Dark";
        mock.Theme = "Light";

        // Assert - any value should match
        mock.VerifySet(x => x.Theme, () => It.IsAny<string>(), Times.Exactly(2));
    }

    [Fact]
    [Trait("Category", "Verification")]
    public void VerifySet_WithItIs_ShouldMatchPredicate()
    {
        // Arrange
        var mock = Mock.Create<ISettings>();
        mock.SetupProperty(x => x.FontSize);

        // Act
        mock.FontSize = 10;
        mock.FontSize = 12;
        mock.FontSize = 14;
        mock.FontSize = 8;

        // Assert - match values >= 12
        mock.VerifySet(x => x.FontSize, () => It.Is<int>(size => size >= 12), Times.Exactly(2));
    }

    [Fact]
    [Trait("Category", "Verification")]
    public void VerifyGetAndSet_SameProperty_ShouldTrackSeparately()
    {
        // Arrange
        var mock = Mock.Create<ICounter>();
        mock.SetupProperty(x => x.Value);

        // Act
        mock.Value = 1;    // set
        _ = mock.Value;    // get
        mock.Value = 2;    // set
        _ = mock.Value;    // get
        _ = mock.Value;    // get

        // Assert - get and set tracked separately
        mock.VerifyGet(x => x.Value, Times.Exactly(3));
        mock.VerifySet(x => x.Value, () => It.IsAny<int>(), Times.Exactly(2));
    }

    [Fact]
    [Trait("Category", "Verification")]
    public void VerifyGet_MultipleProperties_ShouldTrackIndependently()
    {
        // Arrange
        var mock = Mock.Create<ICounter>();
        mock.SetupProperty(x => x.Value);
        mock.SetupProperty(x => x.Name);

        // Act
        _ = mock.Value;
        _ = mock.Value;
        _ = mock.Name;

        // Assert
        mock.VerifyGet(x => x.Value, Times.Exactly(2));
        mock.VerifyGet(x => x.Name, Times.Once());
    }

    [Fact]
    [Trait("Category", "Verification")]
    public void VerifySet_MultipleProperties_ShouldTrackIndependently()
    {
        // Arrange
        var mock = Mock.Create<ICounter>();
        mock.SetupProperty(x => x.Value);
        mock.SetupProperty(x => x.Name);

        // Act
        mock.Value = 5;
        mock.Value = 10;
        mock.Name = "Counter1";

        // Assert
        mock.VerifySet(x => x.Value, () => It.IsAny<int>(), Times.Exactly(2));
        mock.VerifySet(x => x.Name, () => It.IsAny<string>(), Times.Once());
    }

    [Fact]
    [Trait("Category", "Verification")]
    public void VerifySet_WithVariable_ShouldWork()
    {
        // Arrange
        var mock = Mock.Create<ISettings>();
        mock.SetupProperty(x => x.FontSize);
        int expectedSize = 14;

        // Act
        mock.FontSize = expectedSize;

        // Assert
        mock.VerifySet(x => x.FontSize, () => expectedSize, Times.Once());
    }
}
