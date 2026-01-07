using Skugga.Core;

namespace Skugga.Core.Tests;

/// <summary>
/// Tests for additional argument matchers: It.Is, It.IsIn, It.IsNotNull, It.IsRegex
/// </summary>
public class AdditionalMatchersTests
{
    public interface ITestService
    {
        string Process(int value);
        string ProcessString(string input);
        string ProcessTwo(int a, string b);
        void Execute(string command);
        string HandleObject(object? obj);
    }

    // === It.Is<T>(predicate) Tests ===

    [Fact]
    [Trait("Category", "Matchers")]
    public void Setup_WithItIs_MatchesValuesMatchingPredicate()
    {
        // Arrange
        var mock = Mock.Create<ITestService>();
        mock.Setup(x => x.Process(It.Is<int>(n => n > 0))).Returns("positive");

        // Act & Assert
        mock.Process(1).Should().Be("positive");
        mock.Process(42).Should().Be("positive");
        mock.Process(0).Should().BeNull(); // Doesn't match predicate
        mock.Process(-5).Should().BeNull(); // Doesn't match predicate
    }

    [Fact]
    [Trait("Category", "Matchers")]
    public void Setup_WithItIsString_MatchesStringsPredicate()
    {
        // Arrange
        var mock = Mock.Create<ITestService>();
        mock.Setup(x => x.ProcessString(It.Is<string>(s => s.StartsWith("test"))))
            .Returns("matched");

        // Act & Assert
        mock.ProcessString("test123").Should().Be("matched");
        mock.ProcessString("testing").Should().Be("matched");
        mock.ProcessString("other").Should().BeNull();
        mock.ProcessString("").Should().BeNull();
    }

    [Fact]
    [Trait("Category", "Matchers")]
    public void Setup_WithItIsComplexPredicate_WorksCorrectly()
    {
        // Arrange
        var mock = Mock.Create<ITestService>();
        mock.Setup(x => x.Process(It.Is<int>(n => n % 2 == 0 && n > 10)))
            .Returns("even and greater than 10");

        // Act & Assert
        mock.Process(12).Should().Be("even and greater than 10");
        mock.Process(20).Should().Be("even and greater than 10");
        mock.Process(10).Should().BeNull(); // Not > 10
        mock.Process(11).Should().BeNull(); // Not even
        mock.Process(2).Should().BeNull(); // Not > 10
    }

    [Fact]
    [Trait("Category", "Matchers")]
    public void Verify_WithItIs_MatchesCorrectly()
    {
        // Arrange
        var mock = Mock.Create<ITestService>();

        // Act
        mock.Process(5);
        mock.Process(10);
        mock.Process(-3);

        // Assert
        mock.Verify(x => x.Process(It.Is<int>(n => n > 0)), Times.Exactly(2));
        mock.Verify(x => x.Process(It.Is<int>(n => n < 0)), Times.Once());
    }

    // === It.IsIn<T>(values) Tests ===

    [Fact]
    [Trait("Category", "Matchers")]
    public void Setup_WithItIsIn_MatchesValuesInSet()
    {
        // Arrange
        var mock = Mock.Create<ITestService>();
        mock.Setup(x => x.Process(It.IsIn(1, 2, 3))).Returns("in set");

        // Act & Assert
        mock.Process(1).Should().Be("in set");
        mock.Process(2).Should().Be("in set");
        mock.Process(3).Should().Be("in set");
        mock.Process(4).Should().BeNull();
        mock.Process(0).Should().BeNull();
    }

    [Fact]
    [Trait("Category", "Matchers")]
    public void Setup_WithItIsInStrings_MatchesCorrectly()
    {
        // Arrange
        var mock = Mock.Create<ITestService>();
        mock.Setup(x => x.ProcessString(It.IsIn("alpha", "beta", "gamma")))
            .Returns("greek letter");

        // Act & Assert
        mock.ProcessString("alpha").Should().Be("greek letter");
        mock.ProcessString("beta").Should().Be("greek letter");
        mock.ProcessString("gamma").Should().Be("greek letter");
        mock.ProcessString("delta").Should().BeNull();
    }

    [Fact]
    [Trait("Category", "Matchers")]
    public void Setup_WithItIsInSingleValue_WorksCorrectly()
    {
        // Arrange
        var mock = Mock.Create<ITestService>();
        mock.Setup(x => x.Process(It.IsIn(42))).Returns("answer");

        // Act & Assert
        mock.Process(42).Should().Be("answer");
        mock.Process(41).Should().BeNull();
        mock.Process(43).Should().BeNull();
    }

    [Fact]
    [Trait("Category", "Matchers")]
    public void Verify_WithItIsIn_MatchesCorrectly()
    {
        // Arrange
        var mock = Mock.Create<ITestService>();

        // Act
        mock.Process(1);
        mock.Process(2);
        mock.Process(5);
        mock.Process(2);

        // Assert
        mock.Verify(x => x.Process(It.IsIn(1, 2, 3)), Times.Exactly(3));
        mock.Verify(x => x.Process(It.IsIn(5, 6, 7)), Times.Once());
    }

    // === It.IsNotNull<T>() Tests ===

    [Fact]
    [Trait("Category", "Matchers")]
    public void Setup_WithItIsNotNull_MatchesNonNullValues()
    {
        // Arrange
        var mock = Mock.Create<ITestService>();
        mock.Setup(x => x.HandleObject(It.IsNotNull<object>())).Returns("not null");

        // Act & Assert
        mock.HandleObject("test").Should().Be("not null");
        mock.HandleObject(42).Should().Be("not null");
        mock.HandleObject(new object()).Should().Be("not null");
        mock.HandleObject(null).Should().BeNull(); // Doesn't match
    }

    [Fact]
    [Trait("Category", "Matchers")]
    public void Setup_WithItIsNotNullString_MatchesCorrectly()
    {
        // Arrange
        var mock = Mock.Create<ITestService>();
        mock.Setup(x => x.ProcessString(It.IsNotNull<string>())).Returns("has value");

        // Act & Assert
        mock.ProcessString("test").Should().Be("has value");
        mock.ProcessString("").Should().Be("has value"); // Empty string is not null
        mock.ProcessString(null!).Should().BeNull(); // null doesn't match
    }

    [Fact]
    [Trait("Category", "Matchers")]
    public void Verify_WithItIsNotNull_MatchesCorrectly()
    {
        // Arrange
        var mock = Mock.Create<ITestService>();

        // Act
        mock.HandleObject("test");
        mock.HandleObject(null);
        mock.HandleObject(42);

        // Assert
        mock.Verify(x => x.HandleObject(It.IsNotNull<object>()), Times.Exactly(2));
    }

    // === It.IsRegex(pattern) Tests ===

    [Fact]
    [Trait("Category", "Matchers")]
    public void Setup_WithItIsRegex_MatchesPattern()
    {
        // Arrange
        var mock = Mock.Create<ITestService>();
        mock.Setup(x => x.ProcessString(It.IsRegex(@"^\d{3}$"))).Returns("three digits");

        // Act & Assert
        mock.ProcessString("123").Should().Be("three digits");
        mock.ProcessString("456").Should().Be("three digits");
        mock.ProcessString("12").Should().BeNull(); // Too short
        mock.ProcessString("1234").Should().BeNull(); // Too long
        mock.ProcessString("abc").Should().BeNull(); // Not digits
    }

    [Fact]
    [Trait("Category", "Matchers")]
    public void Setup_WithItIsRegexEmail_MatchesCorrectly()
    {
        // Arrange
        var mock = Mock.Create<ITestService>();
        mock.Setup(x => x.ProcessString(It.IsRegex(@"^[\w\.-]+@[\w\.-]+\.\w+$")))
            .Returns("valid email");

        // Act & Assert
        mock.ProcessString("test@example.com").Should().Be("valid email");
        mock.ProcessString("user.name@domain.co.uk").Should().Be("valid email");
        mock.ProcessString("invalid-email").Should().BeNull();
        mock.ProcessString("@example.com").Should().BeNull();
    }

    [Fact]
    [Trait("Category", "Matchers")]
    public void Setup_WithItIsRegexStartsWith_WorksCorrectly()
    {
        // Arrange
        var mock = Mock.Create<ITestService>();
        mock.Setup(x => x.ProcessString(It.IsRegex(@"^test"))).Returns("starts with test");

        // Act & Assert
        mock.ProcessString("test123").Should().Be("starts with test");
        mock.ProcessString("testing").Should().Be("starts with test");
        mock.ProcessString("other").Should().BeNull();
    }

    [Fact]
    [Trait("Category", "Matchers")]
    public void Verify_WithItIsRegex_MatchesCorrectly()
    {
        // Arrange
        var mock = Mock.Create<ITestService>();

        // Act
        mock.ProcessString("123");
        mock.ProcessString("456");
        mock.ProcessString("abc");

        // Assert
        mock.Verify(x => x.ProcessString(It.IsRegex(@"^\d+$")), Times.Exactly(2));
        mock.Verify(x => x.ProcessString(It.IsRegex(@"^[a-z]+$")), Times.Once());
    }

    // === Mixed Matcher Tests ===

    [Fact]
    [Trait("Category", "Matchers")]
    public void Setup_WithMixedItIsAndSpecific_WorksCorrectly()
    {
        // Arrange
        var mock = Mock.Create<ITestService>();
        mock.Setup(x => x.ProcessTwo(It.Is<int>(n => n > 0), "test")).Returns("matched");

        // Act & Assert
        mock.ProcessTwo(5, "test").Should().Be("matched");
        mock.ProcessTwo(10, "test").Should().Be("matched");
        mock.ProcessTwo(-5, "test").Should().BeNull(); // Predicate doesn't match
        mock.ProcessTwo(5, "other").Should().BeNull(); // String doesn't match
    }

    [Fact]
    [Trait("Category", "Matchers")]
    public void Setup_WithItIsAndItIsIn_WorksTogether()
    {
        // Arrange
        var mock = Mock.Create<ITestService>();
        mock.Setup(x => x.ProcessTwo(It.Is<int>(n => n > 0), It.IsIn("a", "b")))
            .Returns("both matched");

        // Act & Assert
        mock.ProcessTwo(5, "a").Should().Be("both matched");
        mock.ProcessTwo(10, "b").Should().Be("both matched");
        mock.ProcessTwo(-5, "a").Should().BeNull();
        mock.ProcessTwo(5, "c").Should().BeNull();
    }

    [Fact]
    [Trait("Category", "Matchers")]
    public void Setup_MultipleSetupsWithDifferentMatchers_FirstMatchWins()
    {
        // Arrange
        var mock = Mock.Create<ITestService>();
        mock.Setup(x => x.Process(It.Is<int>(n => n > 10))).Returns("greater than 10");
        mock.Setup(x => x.Process(It.IsIn(1, 2, 3))).Returns("in set");

        // Act & Assert
        mock.Process(15).Should().Be("greater than 10");
        mock.Process(2).Should().Be("in set");
        mock.Process(5).Should().BeNull(); // No match
    }

    [Fact]
    [Trait("Category", "Matchers")]
    public void Verify_WithDifferentMatcherTypes_AllWork()
    {
        // Arrange
        var mock = Mock.Create<ITestService>();

        // Act
        mock.Process(5);
        mock.Process(10);
        mock.ProcessString("test123");
        mock.HandleObject("not null");
        mock.HandleObject(null);

        // Assert
        mock.Verify(x => x.Process(It.Is<int>(n => n > 0)), Times.Exactly(2));
        mock.Verify(x => x.ProcessString(It.IsRegex(@"^test")), Times.Once());
        mock.Verify(x => x.HandleObject(It.IsNotNull<object>()), Times.Once());
    }

    [Fact]
    [Trait("Category", "Matchers")]
    public void StrictMock_WithItIsMatchers_WorksCorrectly()
    {
        // Arrange
        var mock = Mock.Create<ITestService>(MockBehavior.Strict);
        mock.Setup(x => x.Process(It.Is<int>(n => n > 0))).Returns("allowed");

        // Act & Assert
        mock.Process(5).Should().Be("allowed");

        // Unmatched call should throw in strict mode
        Assert.Throws<MockException>(() => mock.Process(-5));
    }
}
