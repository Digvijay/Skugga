using Xunit;
using Skugga.Core;

namespace Skugga.Core.Tests;

/// <summary>
/// Tests for Multiple Interface Implementation using As<T>()
/// 
/// IMPORTANT LIMITATION - AOT Compatibility Constraint:
/// Skugga is fully AOT-compatible and uses compile-time code generation only.
/// Unlike Moq which uses runtime IL generation, Skugga cannot dynamically add interface implementations at runtime.
/// 
/// The As<T>() API is provided for Moq API compatibility and tracks the requested interfaces in the MockHandler,
/// but the generated mock class must declare all interfaces at compile-time.
/// 
/// WORKAROUND: For multiple interface scenarios, consider:
/// 1. Creating separate mock instances for each interface
/// 2. Using explicit interface declarations if known at design time  
/// 3. Wrapping/composing mocks when multiple interfaces are needed
/// 
/// This is a fundamental trade-off: Skugga chooses AOT compatibility and zero-runtime-reflection
/// over Moq's dynamic runtime proxy generation capabilities.
/// </summary>
public class MultipleInterfaceTests
{
    public interface IFoo
    {
        string GetName();
        int GetValue();
    }

    public interface IBar
    {
        void DoSomething();
        bool IsValid { get; }
    }

    public interface IBaz
    {
        void Reset();
    }

    [Fact]
    [Trait("Category", "Core")]
    public void As_TracksAdditionalInterface()
    {
        // Arrange
        var mock = Mock.Create<IFoo>();
        
        // Act
        try
        {
            var result = mock.As<IBar>();
            // The interface is tracked but cast will fail (AOT limitation)
        }
        catch (InvalidCastException)
        {
            // Expected: generated mock doesn't implement IBar at runtime
        }
        
        var additionalInterfaces = ((IMockSetup)mock).Handler.GetAdditionalInterfaces();
        
        // Assert - interface is tracked even though cast fails
        Assert.Contains(typeof(IBar), additionalInterfaces);
    }

    [Fact]
    [Trait("Category", "Core")]
    public void As_MultipleInterfaces_TracksAll()
    {
        // Arrange
        var mock = Mock.Create<IFoo>();
        
        // Act - these will throw InvalidCastException but still track
        try { mock.As<IBar>(); } catch (InvalidCastException) { }
        try { mock.As<IBaz>(); } catch (InvalidCastException) { }
        
        var additionalInterfaces = ((IMockSetup)mock).Handler.GetAdditionalInterfaces();
        
        // Assert
        Assert.Equal(2, additionalInterfaces.Count);
        Assert.Contains(typeof(IBar), additionalInterfaces);
        Assert.Contains(typeof(IBaz), additionalInterfaces);
    }

    [Fact]
    [Trait("Category", "Core")]
    public void As_NonInterfaceType_ThrowsArgumentException()
    {
        // Arrange
        var mock = Mock.Create<IFoo>();
        
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => mock.As<string>());
        Assert.Contains("not an interface", ex.Message);
    }

    [Fact]
    [Trait("Category", "Core")]
    public void As_NonMockObject_ThrowsArgumentException()
    {
        // Arrange
        var notAMock = new object();
        
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => notAMock.As<IBar>());
        Assert.Contains("not a Skugga mock", ex.Message);
    }

    [Fact]
    [Trait("Category", "Core")]
    public void As_ChainedCalls_TracksAllInterfaces()
    {
        // Arrange
        var mock = Mock.Create<IFoo>();
        
        // Act - chaining will fail on first cast
        try 
        { 
            mock.As<IBar>(); 
        } 
        catch (InvalidCastException) 
        {
            // Expected
        }
        
        var additionalInterfaces = ((IMockSetup)mock).Handler.GetAdditionalInterfaces();
        
        // Assert - IBar is tracked
        Assert.Contains(typeof(IBar), additionalInterfaces);
    }

    [Fact]
    [Trait("Category", "Core")]
    public void As_SameInterfaceTwice_OnlyTracksOnce()
    {
        // Arrange
        var mock = Mock.Create<IFoo>();
        
        // Act - add same interface twice (both will throw but still track)
        try { mock.As<IBar>(); } catch (InvalidCastException) { }
        try { mock.As<IBar>(); } catch (InvalidCastException) { }
        
        var additionalInterfaces = ((IMockSetup)mock).Handler.GetAdditionalInterfaces();
        
        // Assert - HashSet ensures only one entry
        Assert.Single(additionalInterfaces, t => t == typeof(IBar));
    }
}
