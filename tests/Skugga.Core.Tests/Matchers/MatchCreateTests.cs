using System.Linq;
using Xunit;
using Skugga.Core;

namespace Skugga.Core.Tests;

public interface IValidator
{
    bool Validate(string input);
    int Process(int value);
    string Format(string text);
}

public class MatchCreateTests
{
    [Fact]
    [Trait("Category", "Matchers")]
    public void MatchCreate_WithSimplePredicate_MatchesCorrectly()
    {
        // Arrange
        var mock = Mock.Create<IValidator>();
        mock.Setup(x => x.Validate(Match.Create<string>(s => s != null && s.Length > 10))).Returns(true);

        // Act & Assert
        Assert.True(mock.Validate("this is a long string"));
        Assert.False(mock.Validate("short"));
        Assert.False(mock.Validate("tiny"));
    }

    [Fact]
    [Trait("Category", "Matchers")]
    public void MatchCreate_WithDescription_MatchesCorrectly()
    {
        // Arrange
        var mock = Mock.Create<IValidator>();
        mock.Setup(x => x.Validate(Match.Create<string>(s => s != null && s.Length > 10, "string longer than 10 chars"))).Returns(true);

        // Act & Assert
        Assert.True(mock.Validate("this is a very long string indeed"));
        Assert.False(mock.Validate("short"));
    }

    [Fact]
    [Trait("Category", "Matchers")]
    public void MatchCreate_WithNumericPredicate_MatchesCorrectly()
    {
        // Arrange
        var mock = Mock.Create<IValidator>();
        mock.Setup(x => x.Process(Match.Create<int>(i => i > 0))).Returns(100);

        // Act & Assert
        Assert.Equal(100, mock.Process(5));
        Assert.Equal(100, mock.Process(999));
        Assert.Equal(0, mock.Process(0));      // Doesn't match
        Assert.Equal(0, mock.Process(-5));     // Doesn't match
    }

    [Fact]
    [Trait("Category", "Matchers")]
    public void MatchCreate_WithParameterizedPredicate_MatchesCorrectly()
    {
        // Arrange
        var mock = Mock.Create<IValidator>();
        int min = 10, max = 20;
        mock.Setup(x => x.Process(Match.Create<int>(i => i >= min && i <= max, $"value between {min} and {max}"))).Returns(42);

        // Act & Assert
        Assert.Equal(42, mock.Process(10));
        Assert.Equal(42, mock.Process(15));
        Assert.Equal(42, mock.Process(20));
        Assert.Equal(0, mock.Process(9));      // Below range
        Assert.Equal(0, mock.Process(21));     // Above range
    }

    [Fact]
    [Trait("Category", "Matchers")]
    public void MatchCreate_CanBeUsedWithVerify()
    {
        // Arrange
        var mock = Mock.Create<IValidator>();

        // Act
        mock.Validate("this is a long string");
        mock.Validate("short");

        // Assert
        mock.Verify(x => x.Validate(Match.Create<string>(s => s != null && s.Length > 10)), Times.Once());
    }

    [Fact]
    [Trait("Category", "Matchers")]
    public void MatchCreate_WithCombinedMatchers_Works()
    {
        // Arrange
        var mock = Mock.Create<IValidator>();
        
        // Setup with Match.Create
        mock.Setup(x => x.Format(Match.Create<string>(s => s.StartsWith("prefix")))).Returns("matched");
        
        // Setup with It.IsAny for comparison
        mock.Setup(x => x.Process(It.IsAny<int>())).Returns(999);

        // Act & Assert
        Assert.Equal("matched", mock.Format("prefix_test"));
        Assert.Null(mock.Format("no_prefix"));
        Assert.Equal(999, mock.Process(42));
    }

    [Fact]
    [Trait("Category", "Matchers")]
    public void MatchCreate_MultipleSetupsWithDifferentMatchers_Work()
    {
        // Arrange
        var mock = Mock.Create<IValidator>();
        
        // Multiple range matchers
        mock.Setup(x => x.Process(Match.Create<int>(i => i >= 0 && i <= 10, "0-10"))).Returns(1);
        mock.Setup(x => x.Process(Match.Create<int>(i => i >= 11 && i <= 20, "11-20"))).Returns(2);
        mock.Setup(x => x.Process(Match.Create<int>(i => i >= 21 && i <= 30, "21-30"))).Returns(3);

        // Act & Assert
        Assert.Equal(1, mock.Process(5));
        Assert.Equal(2, mock.Process(15));
        Assert.Equal(3, mock.Process(25));
        Assert.Equal(0, mock.Process(35));
    }

    [Fact]
    [Trait("Category", "Matchers")]
    public void MatchCreate_WithComplexPredicate_Works()
    {
        // Arrange
        var mock = Mock.Create<IValidator>();
        
        // Complex predicate: uppercase AND contains digits
        mock.Setup(x => x.Format(Match.Create<string>(s => 
            s != null && 
            s == s.ToUpper() && 
            s.Any(char.IsDigit)))).Returns("complex match");

        // Act & Assert
        Assert.Equal("complex match", mock.Format("ABC123"));
        Assert.Null(mock.Format("abc123")); // Not uppercase
        Assert.Null(mock.Format("ABC"));    // No digits
    }
}
