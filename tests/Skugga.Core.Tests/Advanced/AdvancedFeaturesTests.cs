using FluentAssertions;
using Skugga.Core;
using Xunit;

namespace Skugga.Core.Tests;

public class AdvancedFeaturesTests
{
    // Test interfaces
    public interface ISequenceService
    {
        int GetNext();
        string GetMessage();
        int Counter { get; }
    }

    #region SetupSequence Tests

    [Fact]
    [Trait("Category", "Advanced")]
    public void SetupSequence_WithMultipleReturns_ReturnsValuesInOrder()
    {
        // Arrange
        var mock = Mock.Create<ISequenceService>();
        mock.SetupSequence(x => x.GetNext())
            .Returns(1)
            .Returns(2)
            .Returns(3);

        // Act & Assert
        mock.GetNext().Should().Be(1);
        mock.GetNext().Should().Be(2);
        mock.GetNext().Should().Be(3);
        mock.GetNext().Should().Be(3); // Repeats last value
    }

    [Fact]
    [Trait("Category", "Advanced")]
    public void SetupSequence_WithSingleReturn_ReturnsValueAndRepeats()
    {
        // Arrange
        var mock = Mock.Create<ISequenceService>();
        mock.SetupSequence(x => x.GetNext())
            .Returns(42);

        // Act & Assert
        mock.GetNext().Should().Be(42);
        mock.GetNext().Should().Be(42);
        mock.GetNext().Should().Be(42);
    }

    [Fact]
    [Trait("Category", "Advanced")]
    public void SetupSequence_WithStrings_ReturnsValuesInOrder()
    {
        // Arrange
        var mock = Mock.Create<ISequenceService>();
        mock.SetupSequence(x => x.GetMessage())
            .Returns("first")
            .Returns("second")
            .Returns("third");

        // Act & Assert
        mock.GetMessage().Should().Be("first");
        mock.GetMessage().Should().Be("second");
        mock.GetMessage().Should().Be("third");
        mock.GetMessage().Should().Be("third");
    }

    [Fact]
    [Trait("Category", "Advanced")]
    public void SetupSequence_WithException_ThrowsOnSpecifiedInvocation()
    {
        // Arrange
        var mock = Mock.Create<ISequenceService>();
        mock.SetupSequence(x => x.GetNext())
            .Returns(1)
            .Returns(2)
            .Throws(new InvalidOperationException("Sequence failed"));

        // Act & Assert
        mock.GetNext().Should().Be(1);
        mock.GetNext().Should().Be(2);
        Assert.Throws<InvalidOperationException>(() => mock.GetNext())
            .Message.Should().Be("Sequence failed");
    }

    [Fact]
    [Trait("Category", "Advanced")]
    public void SetupSequence_WithMixedReturnsAndThrows_WorksCorrectly()
    {
        // Arrange
        var mock = Mock.Create<ISequenceService>();
        mock.SetupSequence(x => x.GetNext())
            .Returns(1)
            .Throws(new InvalidOperationException("Error"))
            .Returns(2)
            .Returns(3);

        // Act & Assert
        mock.GetNext().Should().Be(1);
        Assert.Throws<InvalidOperationException>(() => mock.GetNext());
        mock.GetNext().Should().Be(2);
        mock.GetNext().Should().Be(3);
        mock.GetNext().Should().Be(3); // Repeats last value
    }

    [Fact]
    [Trait("Category", "Advanced")]
    public void SetupSequence_OnProperty_ReturnsValuesInOrder()
    {
        // Arrange
        var mock = Mock.Create<ISequenceService>();
        mock.SetupSequence(x => x.Counter)
            .Returns(0)
            .Returns(1)
            .Returns(2);

        // Act & Assert
        mock.Counter.Should().Be(0);
        mock.Counter.Should().Be(1);
        mock.Counter.Should().Be(2);
        mock.Counter.Should().Be(2); // Repeats last value
    }

    [Fact]
    [Trait("Category", "Advanced")]
    public void SetupSequence_MultipleSequencesOnDifferentMethods_WorkIndependently()
    {
        // Arrange
        var mock = Mock.Create<ISequenceService>();
        mock.SetupSequence(x => x.GetNext())
            .Returns(1)
            .Returns(2);
        mock.SetupSequence(x => x.GetMessage())
            .Returns("A")
            .Returns("B");

        // Act & Assert
        mock.GetNext().Should().Be(1);
        mock.GetMessage().Should().Be("A");
        mock.GetNext().Should().Be(2);
        mock.GetMessage().Should().Be("B");
        mock.GetNext().Should().Be(2);
        mock.GetMessage().Should().Be("B");
    }

    [Fact]
    [Trait("Category", "Advanced")]
    public void SetupSequence_WithZeroReturns_ReturnsDefault()
    {
        // Arrange
        var mock = Mock.Create<ISequenceService>();
        mock.SetupSequence(x => x.GetNext()); // No Returns() calls

        // Act & Assert
        mock.GetNext().Should().Be(0); // default(int)
    }

    [Fact]
    [Trait("Category", "Advanced")]
    public void SetupSequence_WithLongSequence_HandlesAllValues()
    {
        // Arrange
        var mock = Mock.Create<ISequenceService>();
        var sequence = mock.SetupSequence(x => x.GetNext());
        for (int i = 1; i <= 10; i++)
        {
            sequence.Returns(i);
        }

        // Act & Assert
        for (int i = 1; i <= 10; i++)
        {
            mock.GetNext().Should().Be(i);
        }
        mock.GetNext().Should().Be(10); // Repeats last
    }

    #endregion
}
