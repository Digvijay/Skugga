using Skugga.Core;

namespace Skugga.Core.Tests;

// Test interfaces
public interface IOrderedService
{
    void First();
    void Second();
    void Third();
    int GetValue();
    string Process(int value);
}

public interface IAnotherService
{
    void Step1();
    void Step2();
}

public class SequenceTests
{
    [Fact]
    public void InSequence_CallsInOrder_Succeeds()
    {
        // Arrange
        var mock = Mock.Create<IOrderedService>();
        var sequence = new MockSequence();
        
        mock.Setup(m => m.First()).InSequence(sequence);
        mock.Setup(m => m.Second()).InSequence(sequence);
        mock.Setup(m => m.Third()).InSequence(sequence);
        
        // Act - call in correct order
        mock.First();
        mock.Second();
        mock.Third();
        
        // Assert - no exception thrown
        true.Should().BeTrue();
    }
    
    [Fact]
    public void InSequence_CallsOutOfOrder_ThrowsException()
    {
        // Arrange
        var mock = Mock.Create<IOrderedService>();
        var sequence = new MockSequence();
        
        mock.Setup(m => m.First()).InSequence(sequence);
        mock.Setup(m => m.Second()).InSequence(sequence);
        mock.Setup(m => m.Third()).InSequence(sequence);
        
        // Act & Assert
        mock.First();
        
        var act = () => mock.Third(); // Skip Second
        act.Should().Throw<MockException>()
            .WithMessage("*out of sequence*");
    }
    
    [Fact]
    public void InSequence_WithReturnValues_WorksCorrectly()
    {
        // Arrange
        var mock = Mock.Create<IOrderedService>();
        var sequence = new MockSequence();
        
        mock.Setup(m => m.GetValue())
            .Returns(1)
            .InSequence(sequence);
        mock.Setup(m => m.Process(It.IsAny<int>()))
            .Returns("result")
            .InSequence(sequence);
        
        // Act
        var value = mock.GetValue();
        var result = mock.Process(value);
        
        // Assert
        value.Should().Be(1);
        result.Should().Be("result");
    }
    
    [Fact]
    public void InSequence_CrossMock_TracksOrder()
    {
        // Arrange
        var mock1 = Mock.Create<IOrderedService>();
        var mock2 = Mock.Create<IAnotherService>();
        var sequence = new MockSequence();
        
        mock1.Setup(m => m.First()).InSequence(sequence);
        mock2.Setup(m => m.Step1()).InSequence(sequence);
        mock1.Setup(m => m.Second()).InSequence(sequence);
        mock2.Setup(m => m.Step2()).InSequence(sequence);
        
        // Act - call in correct order across mocks
        mock1.First();
        mock2.Step1();
        mock1.Second();
        mock2.Step2();
        
        // Assert - no exception
        true.Should().BeTrue();
    }
    
    [Fact]
    public void InSequence_CrossMock_WrongOrder_Throws()
    {
        // Arrange
        var mock1 = Mock.Create<IOrderedService>();
        var mock2 = Mock.Create<IAnotherService>();
        var sequence = new MockSequence();
        
        mock1.Setup(m => m.First()).InSequence(sequence);
        mock2.Setup(m => m.Step1()).InSequence(sequence);
        mock1.Setup(m => m.Second()).InSequence(sequence);
        
        // Act & Assert
        mock1.First();
        
        var act = () => mock1.Second(); // Skip mock2.Step1()
        act.Should().Throw<MockException>()
            .WithMessage("*out of sequence*");
    }
    
    [Fact]
    public void InSequence_MultipleSequences_Independent()
    {
        // Arrange
        var mock = Mock.Create<IOrderedService>();
        var sequence1 = new MockSequence();
        var sequence2 = new MockSequence();
        
        // Setup two independent sequences
        mock.Setup(m => m.First()).InSequence(sequence1);
        mock.Setup(m => m.Second()).InSequence(sequence1);
        
        mock.Setup(m => m.Process(1)).Returns("A").InSequence(sequence2);
        mock.Setup(m => m.Process(2)).Returns("B").InSequence(sequence2);
        
        // Act - interleave calls from different sequences
        mock.First();
        mock.Process(1);
        mock.Second();
        mock.Process(2);
        
        // Assert - no exception, sequences are independent
        true.Should().BeTrue();
    }
    
    [Fact]
    public void InSequence_WithCallback_CallbackExecutesInOrder()
    {
        // Arrange
        var mock = Mock.Create<IOrderedService>();
        var sequence = new MockSequence();
        var callOrder = new List<string>();
        
        mock.Setup(m => m.First())
            .Callback(() => callOrder.Add("First"))
            .InSequence(sequence);
        mock.Setup(m => m.Second())
            .Callback(() => callOrder.Add("Second"))
            .InSequence(sequence);
        mock.Setup(m => m.Third())
            .Callback(() => callOrder.Add("Third"))
            .InSequence(sequence);
        
        // Act
        mock.First();
        mock.Second();
        mock.Third();
        
        // Assert
        callOrder.Should().Equal("First", "Second", "Third");
    }
    
    [Fact]
    public void InSequence_RepeatedCalls_FollowsSequenceOnce()
    {
        // Arrange
        var mock = Mock.Create<IOrderedService>();
        var sequence = new MockSequence();
        
        mock.Setup(m => m.First()).InSequence(sequence);
        mock.Setup(m => m.Second()).InSequence(sequence);
        
        // Act - First call follows sequence
        mock.First();
        mock.Second();
        
        // Second sequence started - should still enforce order
        var act = () => mock.Second(); // Can't call Second before First in next sequence
        act.Should().Throw<MockException>();
    }
    
    [Fact]
    public void InSequence_WithParameters_MatchesCorrectly()
    {
        // Arrange
        var mock = Mock.Create<IOrderedService>();
        var sequence = new MockSequence();
        
        mock.Setup(m => m.Process(1)).Returns("A").InSequence(sequence);
        mock.Setup(m => m.Process(2)).Returns("B").InSequence(sequence);
        mock.Setup(m => m.Process(3)).Returns("C").InSequence(sequence);
        
        // Act
        var r1 = mock.Process(1);
        var r2 = mock.Process(2);
        var r3 = mock.Process(3);
        
        // Assert
        r1.Should().Be("A");
        r2.Should().Be("B");
        r3.Should().Be("C");
    }
    
    [Fact]
    public void InSequence_SkipToLater_ThrowsWithStepInfo()
    {
        // Arrange
        var mock = Mock.Create<IOrderedService>();
        var sequence = new MockSequence();
        
        mock.Setup(m => m.First()).InSequence(sequence);
        mock.Setup(m => m.Second()).InSequence(sequence);
        mock.Setup(m => m.Third()).InSequence(sequence);
        
        // Act & Assert
        var act = () => mock.Second(); // Try to call step 1 when at step 0
        act.Should().Throw<MockException>()
            .WithMessage("*Expected step 0*")
            .WithMessage("*step 1*");
    }
    
    [Fact]
    public void InSequence_EmptySequence_NoSetups_NoError()
    {
        // Arrange
        var sequence = new MockSequence();
        
        // Act & Assert - just creating a sequence should not error
        sequence.Should().NotBeNull();
    }
}
