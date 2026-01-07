using System;
using System.ComponentModel;
using Xunit;

namespace Skugga.Core.Tests;

// Standard .NET EventHandler pattern
public interface IStandardEvents
{
    event EventHandler? StandardEvent;
    event EventHandler<CustomEventArgs>? GenericEvent;
}

// Custom EventArgs
public class CustomEventArgs : EventArgs
{
    public string Message { get; set; } = string.Empty;
    public int Code { get; set; }
}

// PropertyChanged pattern (very common in MVVM)
public interface INotifyPropertyChangedMock
{
    event PropertyChangedEventHandler? PropertyChanged;
    string Name { get; set; }
    int Age { get; set; }
}

// Custom delegate event
public delegate void CustomEventHandler(object sender, string message);

public interface ICustomDelegateEvents
{
    event CustomEventHandler? CustomEvent;
}

// Interface with methods and events
public interface IServiceWithEvents
{
    event EventHandler? Completed;
    event EventHandler<CustomEventArgs>? StatusChanged;

    void Start();
    void Stop();
}

public class EventTests
{
    #region Raise - Manual Event Triggering

    [Fact]
    [Trait("Category", "Advanced")]
    public void Raise_StandardEvent_InvokesSubscribers()
    {
        // Arrange
        var mock = Mock.Create<IStandardEvents>();
        var eventRaised = false;
        EventArgs? capturedArgs = null;

        mock.StandardEvent += (sender, args) =>
        {
            eventRaised = true;
            capturedArgs = args;
        };

        var expectedArgs = new EventArgs();

        // Act
        mock.Raise(nameof(IStandardEvents.StandardEvent), null, expectedArgs);

        // Assert
        Assert.True(eventRaised);
        Assert.Same(expectedArgs, capturedArgs);
    }

    [Fact]
    [Trait("Category", "Advanced")]
    public void Raise_GenericEvent_InvokesSubscribers()
    {
        // Arrange
        var mock = Mock.Create<IStandardEvents>();
        var eventRaised = false;
        CustomEventArgs? capturedArgs = null;

        mock.GenericEvent += (sender, args) =>
        {
            eventRaised = true;
            capturedArgs = args;
        };

        var expectedArgs = new CustomEventArgs { Message = "Test", Code = 42 };

        // Act
        mock.Raise(nameof(IStandardEvents.GenericEvent), null, expectedArgs);

        // Assert
        Assert.True(eventRaised);
        Assert.Same(expectedArgs, capturedArgs);
        Assert.Equal("Test", capturedArgs!.Message);
        Assert.Equal(42, capturedArgs.Code);
    }

    [Fact]
    [Trait("Category", "Advanced")]
    public void Raise_PropertyChangedEvent_InvokesSubscribers()
    {
        // Arrange
        var mock = Mock.Create<INotifyPropertyChangedMock>();
        var eventRaised = false;
        string? capturedPropertyName = null;

        mock.PropertyChanged += (sender, args) =>
        {
            eventRaised = true;
            capturedPropertyName = args.PropertyName;
        };

        var expectedArgs = new PropertyChangedEventArgs("Name");

        // Act
        mock.Raise(nameof(INotifyPropertyChangedMock.PropertyChanged), null, expectedArgs);

        // Assert
        Assert.True(eventRaised);
        Assert.Equal("Name", capturedPropertyName);
    }

    [Fact]
    [Trait("Category", "Advanced")]
    public void Raise_CustomDelegateEvent_InvokesSubscribers()
    {
        // Arrange
        var mock = Mock.Create<ICustomDelegateEvents>();
        var eventRaised = false;
        string? capturedMessage = null;

        mock.CustomEvent += (sender, message) =>
        {
            eventRaised = true;
            capturedMessage = message;
        };

        // Act
        mock.Raise(nameof(ICustomDelegateEvents.CustomEvent), null, "Test Message");

        // Assert
        Assert.True(eventRaised);
        Assert.Equal("Test Message", capturedMessage);
    }

    [Fact]
    [Trait("Category", "Advanced")]
    public void Raise_MultipleSubscribers_InvokesAll()
    {
        // Arrange
        var mock = Mock.Create<IStandardEvents>();
        var count = 0;

        mock.StandardEvent += (s, e) => count++;
        mock.StandardEvent += (s, e) => count++;
        mock.StandardEvent += (s, e) => count++;

        // Act
        mock.Raise(nameof(IStandardEvents.StandardEvent), null, EventArgs.Empty);

        // Assert
        Assert.Equal(3, count);
    }

    [Fact]
    [Trait("Category", "Advanced")]
    public void Raise_NoSubscribers_DoesNotThrow()
    {
        // Arrange
        var mock = Mock.Create<IStandardEvents>();

        // Act & Assert - should not throw
        mock.Raise(nameof(IStandardEvents.StandardEvent), null, EventArgs.Empty);
    }

    #endregion

    #region Raises - Auto-Trigger Events on Method Calls

    [Fact]
    [Trait("Category", "Advanced")]
    public void Raises_TriggersEventWhenMethodCalled()
    {
        // Arrange
        var mock = Mock.Create<IServiceWithEvents>();
        var eventRaised = false;

        mock.Completed += (s, e) => eventRaised = true;

        mock.Setup(m => m.Start())
            .Raises(nameof(IServiceWithEvents.Completed), null, EventArgs.Empty);

        // Act
        mock.Start();

        // Assert
        Assert.True(eventRaised);
    }

    [Fact]
    [Trait("Category", "Advanced")]
    public void Raises_WithCustomArgs_PassesCorrectArgs()
    {
        // Arrange
        var mock = Mock.Create<IServiceWithEvents>();
        CustomEventArgs? capturedArgs = null;

        mock.StatusChanged += (s, e) => capturedArgs = e;

        var expectedArgs = new CustomEventArgs { Message = "Started", Code = 100 };
        mock.Setup(m => m.Start())
            .Raises(nameof(IServiceWithEvents.StatusChanged), null, expectedArgs);

        // Act
        mock.Start();

        // Assert
        Assert.NotNull(capturedArgs);
        Assert.Equal("Started", capturedArgs!.Message);
        Assert.Equal(100, capturedArgs.Code);
    }

    [Fact]
    [Trait("Category", "Advanced")]
    public void Raises_MultipleCallsToMethod_RaisesEventEachTime()
    {
        // Arrange
        var mock = Mock.Create<IServiceWithEvents>();
        var eventCount = 0;

        mock.Completed += (s, e) => eventCount++;

        mock.Setup(m => m.Start())
            .Raises(nameof(IServiceWithEvents.Completed), null, EventArgs.Empty);

        // Act
        mock.Start();
        mock.Start();
        mock.Start();

        // Assert
        Assert.Equal(3, eventCount);
    }

    #endregion

    #region VerifyAdd - Verify Event Subscription

    [Fact]
    [Trait("Category", "Advanced")]
    public void VerifyAdd_SubscriptionOccurred_Passes()
    {
        // Arrange
        var mock = Mock.Create<IStandardEvents>();
        EventHandler handler = (s, e) => { };

        // Act
        mock.StandardEvent += handler;

        // Assert
        mock.VerifyAdd(nameof(IStandardEvents.StandardEvent), Times.Once());
    }

    [Fact]
    [Trait("Category", "Advanced")]
    public void VerifyAdd_MultipleSubscriptions_CountsCorrectly()
    {
        // Arrange
        var mock = Mock.Create<IStandardEvents>();
        EventHandler handler1 = (s, e) => { };
        EventHandler handler2 = (s, e) => { };
        EventHandler handler3 = (s, e) => { };

        // Act
        mock.StandardEvent += handler1;
        mock.StandardEvent += handler2;
        mock.StandardEvent += handler3;

        // Assert
        mock.VerifyAdd(nameof(IStandardEvents.StandardEvent), Times.Exactly(3));
    }

    [Fact]
    [Trait("Category", "Advanced")]
    public void VerifyAdd_NoSubscription_ThrowsVerificationException()
    {
        // Arrange
        var mock = Mock.Create<IStandardEvents>();

        // Act & Assert
        var ex = Assert.Throws<VerificationException>(() =>
            mock.VerifyAdd(nameof(IStandardEvents.StandardEvent), Times.Once()));

        Assert.Contains("StandardEvent", ex.Message);
    }

    [Fact]
    [Trait("Category", "Advanced")]
    public void VerifyAdd_GenericEventHandler_TracksCorrectly()
    {
        // Arrange
        var mock = Mock.Create<IStandardEvents>();
        EventHandler<CustomEventArgs> handler = (s, e) => { };

        // Act
        mock.GenericEvent += handler;

        // Assert
        mock.VerifyAdd(nameof(IStandardEvents.GenericEvent), Times.Once());
    }

    #endregion

    #region VerifyRemove - Verify Event Unsubscription

    [Fact]
    [Trait("Category", "Advanced")]
    public void VerifyRemove_UnsubscriptionOccurred_Passes()
    {
        // Arrange
        var mock = Mock.Create<IStandardEvents>();
        EventHandler handler = (s, e) => { };

        // Act
        mock.StandardEvent += handler;
        mock.StandardEvent -= handler;

        // Assert
        mock.VerifyRemove(nameof(IStandardEvents.StandardEvent), Times.Once());
    }

    [Fact]
    [Trait("Category", "Advanced")]
    public void VerifyRemove_MultipleUnsubscriptions_CountsCorrectly()
    {
        // Arrange
        var mock = Mock.Create<IStandardEvents>();
        EventHandler handler1 = (s, e) => { };
        EventHandler handler2 = (s, e) => { };

        // Act
        mock.StandardEvent += handler1;
        mock.StandardEvent += handler2;
        mock.StandardEvent -= handler1;
        mock.StandardEvent -= handler2;

        // Assert
        mock.VerifyRemove(nameof(IStandardEvents.StandardEvent), Times.Exactly(2));
    }

    [Fact]
    [Trait("Category", "Advanced")]
    public void VerifyRemove_NoUnsubscription_ThrowsVerificationException()
    {
        // Arrange
        var mock = Mock.Create<IStandardEvents>();
        EventHandler handler = (s, e) => { };
        mock.StandardEvent += handler;

        // Act & Assert
        var ex = Assert.Throws<VerificationException>(() =>
            mock.VerifyRemove(nameof(IStandardEvents.StandardEvent), Times.Once()));

        Assert.Contains("StandardEvent", ex.Message);
    }

    #endregion

    #region Combined Scenarios

    [Fact]
    [Trait("Category", "Advanced")]
    public void Events_AddAndRemove_BothVerifiable()
    {
        // Arrange
        var mock = Mock.Create<IStandardEvents>();
        EventHandler handler = (s, e) => { };

        // Act
        mock.StandardEvent += handler;
        mock.StandardEvent -= handler;

        // Assert
        mock.VerifyAdd(nameof(IStandardEvents.StandardEvent), Times.Once());
        mock.VerifyRemove(nameof(IStandardEvents.StandardEvent), Times.Once());
    }

    [Fact]
    [Trait("Category", "Advanced")]
    public void Events_RaiseAfterSetup_WorksTogether()
    {
        // Arrange
        var mock = Mock.Create<IServiceWithEvents>();
        var manualRaiseCount = 0;
        var autoRaiseCount = 0;

        mock.Completed += (s, e) => manualRaiseCount++;
        mock.StatusChanged += (s, e) => autoRaiseCount++;

        mock.Setup(m => m.Start())
            .Raises(nameof(IServiceWithEvents.StatusChanged), null, new CustomEventArgs { Message = "Auto" });

        // Act
        mock.Raise(nameof(IServiceWithEvents.Completed), null, EventArgs.Empty); // Manual
        mock.Start(); // Auto-raises StatusChanged

        // Assert
        Assert.Equal(1, manualRaiseCount);
        Assert.Equal(1, autoRaiseCount);
    }

    [Fact]
    [Trait("Category", "Advanced")]
    public void Events_VerifyAddRemoveWithRaise_AllWorkTogether()
    {
        // Arrange
        var mock = Mock.Create<IStandardEvents>();
        var eventRaiseCount = 0;
        EventHandler handler = (s, e) => eventRaiseCount++;

        // Act
        mock.StandardEvent += handler; // Add
        mock.Raise(nameof(IStandardEvents.StandardEvent), null, EventArgs.Empty); // Raise
        mock.StandardEvent -= handler; // Remove

        // Assert
        Assert.Equal(1, eventRaiseCount);
        mock.VerifyAdd(nameof(IStandardEvents.StandardEvent), Times.Once());
        mock.VerifyRemove(nameof(IStandardEvents.StandardEvent), Times.Once());
    }

    #endregion
}
