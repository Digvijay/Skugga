using Skugga.Core;
using Xunit;

namespace Skugga.Core.Tests;

/// <summary>
/// Tests for LINQ to Mocks - Mock.Get() support
/// 
/// IMPORTANT: Mock.Of<T>() is NOT IMPLEMENTED due to AOT/interceptor constraints.
/// 
/// Skugga's compile-time approach means interceptors only work on direct call sites in user code.
/// When Mock.Of() internally calls Mock.Create(), that internal call is already compiled and
/// cannot be intercepted. This is a fundamental limitation of the interceptor-based approach.
/// 
/// WORKAROUND: Use Mock.Create() + explicit Setup() calls instead of Mock.Of():
/// 
/// // Moq LINQ to Mocks (not supported in Skugga):
/// // var foo = Mock.Of<IFoo>(f => f.Name == "bar" && f.GetCount() == 42);
/// 
/// // Skugga equivalent:
/// var foo = Mock.Create<IFoo>();
/// foo.Setup(f => f.Name).Returns("bar");
/// foo.Setup(f => f.GetCount()).Returns(42);
/// 
/// Mock.Get() IS SUPPORTED for retrieving the IMockSetup interface from mocked objects.
/// </summary>
public class LinqToMocksTests
{
    public interface IFoo
    {
        string Name { get; }
        int Count { get; }
        int GetValue();
    }

    [Fact]
    [Trait("Category", "Advanced")]
    public void Get_WithMockCreate_ReturnsSetup()
    {
        // Arrange
        var foo = Mock.Create<IFoo>();

        // Act
        var mock = Mock.Get(foo);

        // Assert
        Assert.NotNull(mock);
        Assert.Same(((IMockSetup)foo).Handler, mock.Handler);
    }

    [Fact]
    [Trait("Category", "Advanced")]
    public void Get_AllowsVerification()
    {
        // Arrange
        var foo = Mock.Create<IFoo>();
        foo.Setup(f => f.Name).Returns("test");

        // Act
        var name = foo.Name; // Access property

        // Assert - can verify via Mock.Get
        var mock = Mock.Get(foo);
        Assert.NotNull(mock);
        foo.Verify(f => f.Name, Times.Once());
    }

    [Fact]
    [Trait("Category", "Advanced")]
    public void Get_AllowsAdditionalSetup()
    {
        // Arrange
        var foo = Mock.Create<IFoo>();
        foo.Setup(f => f.Name).Returns("initial");

        // Act - get mock and add more setup
        var mock = Mock.Get(foo);
        Assert.NotNull(mock);
        foo.Setup(f => f.Count).Returns(99);

        // Assert
        Assert.Equal("initial", foo.Name);
        Assert.Equal(99, foo.Count);
    }

    [Fact]
    [Trait("Category", "Advanced")]
    public void Get_WithNonMock_ThrowsArgumentException()
    {
        // Arrange
        var notAMock = new object();

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => Mock.Get(notAMock));
        Assert.Contains("not a Skugga mock", ex.Message);
    }

    [Fact]
    [Trait("Category", "Advanced")]
    public void Get_ReturnsSameInterfaceAsIMockSetup()
    {
        // Arrange
        var foo = Mock.Create<IFoo>();

        // Act
        var mockViaGet = Mock.Get(foo);
        var mockViaCast = (IMockSetup)foo;

        // Assert - both ways give same Handler
        Assert.Same(mockViaGet.Handler, mockViaCast.Handler);
    }

    [Fact]
    [Trait("Category", "Advanced")]
    public void Workaround_CreateWithExplicitSetup_WorksLikeMockOf()
    {
        // Arrange & Act - this is the Skugga way instead of Mock.Of
        var foo = Mock.Create<IFoo>();
        foo.Setup(f => f.Name).Returns("bar");
        foo.Setup(f => f.Count).Returns(42);
        foo.Setup(f => f.GetValue()).Returns(100);

        // Assert - same result as Mock.Of would provide
        Assert.Equal("bar", foo.Name);
        Assert.Equal(42, foo.Count);
        Assert.Equal(100, foo.GetValue());
    }

    [Fact]
    [Trait("Category", "Advanced")]
    public void Get_IntegrationTest_CreateSetupGetVerify()
    {
        // This test demonstrates the complete workflow that replaces Mock.Of usage

        // Step 1: Create mock
        var foo = Mock.Create<IFoo>();

        // Step 2: Setup behavior
        foo.Setup(f => f.Name).Returns("integration");
        foo.Setup(f => f.Count).Returns(999);
        foo.Setup(f => f.GetValue()).Returns(777);

        // Step 3: Use the mock
        var name = foo.Name;
        var count = foo.Count;
        var value = foo.GetValue();

        // Step 4: Retrieve via Mock.Get for verification (proves it's the same mock)
        var mockSetup = Mock.Get(foo);
        Assert.NotNull(mockSetup);
        Assert.Same(((IMockSetup)foo).Handler, mockSetup.Handler);

        // Step 5: Verify calls (use original mock, not mockSetup)
        foo.Verify(f => f.Name, Times.Once());
        foo.Verify(f => f.Count, Times.Once());
        foo.Verify(f => f.GetValue(), Times.Once());

        // Step 6: Verify values
        Assert.Equal("integration", name);
        Assert.Equal(999, count);
        Assert.Equal(777, value);
    }

    [Fact]
    [Trait("Category", "Advanced")]
    public void Get_WithMultipleMocks_TracksIndependently()
    {
        // Arrange
        var foo1 = Mock.Create<IFoo>();
        var foo2 = Mock.Create<IFoo>();

        foo1.Setup(f => f.Name).Returns("first");
        foo2.Setup(f => f.Name).Returns("second");

        // Act
        var mock1 = Mock.Get(foo1);
        var mock2 = Mock.Get(foo2);

        // Assert - each has independent handler
        Assert.NotSame(mock1.Handler, mock2.Handler);
        Assert.Equal("first", foo1.Name);
        Assert.Equal("second", foo2.Name);
    }
}
