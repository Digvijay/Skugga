using Skugga.Core;
using Xunit;

namespace Skugga.Core.Tests;

public class GenericTypeParameterTests
{
    // Test generic interface mocking
    public interface IRepository<T>
    {
        T? Get(int id);
        void Save(T item);
        IEnumerable<T> GetAll();
    }

    public interface IGenericService<TKey, TValue>
    {
        TValue? Find(TKey key);
        void Store(TKey key, TValue value);
    }

    // Generic methods
    public interface IConverter
    {
        TOutput Convert<TInput, TOutput>(TInput input);
        T Process<T>(T value);
    }

    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }

    [Fact]
    [Trait("Category", "Core")]
    public void Mock_GenericInterface_SingleTypeParameter_ShouldWork()
    {
        // Arrange & Act
        var mock = Mock.Create<IRepository<string>>();

        // Assert - mock should be created successfully
        Assert.NotNull(mock);
    }

    [Fact]
    [Trait("Category", "Core")]
    public void Mock_GenericInterface_Setup_ShouldWork()
    {
        // Arrange
        var mock = Mock.Create<IRepository<string>>();

        // Act
        mock.Setup(x => x.Get(1)).Returns("test");

        // Assert
        var result = mock.Get(1);
        Assert.Equal("test", result);
    }

    [Fact]
    [Trait("Category", "Core")]
    public void Mock_GenericInterface_MultipleTypeParameters_ShouldWork()
    {
        // Arrange & Act
        var mock = Mock.Create<IGenericService<int, string>>();
        mock.Setup(x => x.Find(42)).Returns("found");

        // Assert
        var result = mock.Find(42);
        Assert.Equal("found", result);
    }

    [Fact]
    [Trait("Category", "Core")]
    public void Mock_GenericMethod_ShouldWork()
    {
        // Arrange
        var mock = Mock.Create<IConverter>();

        // Act - Setup generic method
        // Note: This tests generic METHOD (not generic interface)
        mock.Setup(x => x.Process(42)).Returns(84);

        // Assert
        var result = mock.Process(42);
        Assert.Equal(84, result);
    }

    // Skip ILogger test for now - requires additional package reference
    // Will be tested separately once we add Microsoft.Extensions.Logging
    /*
    [Fact]
    [Trait("Category", "Core")]
    public void Mock_ILogger_ShouldWork()
    {
        // This is the critical test - ILogger<T> uses generic type parameters in methods
        // Arrange & Act
        var mock = Mock.Create<ILogger<GenericTypeParameterTests>>();
        
        // Assert - Should not throw during mock creation
        Assert.NotNull(mock);
    }
    
    [Fact]
    [Trait("Category", "Core")]
    public void Mock_ILogger_Log_Method_ShouldWork()
    {
        // Arrange
        var mock = Mock.Create<ILogger<GenericTypeParameterTests>>();
        bool logged = false;
        
        // Act - ILogger.Log has a generic TState parameter
        mock.Setup(x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.IsAny<object>(),
            It.IsAny<Exception>(),
            It.IsAny<Func<object, Exception?, string>>()
        )).Callback(() => logged = true);
        
        mock.Log(LogLevel.Information, new EventId(1), "test", null, (state, ex) => state?.ToString() ?? "");
        
        // Assert
        Assert.True(logged);
    }
    */

    [Fact]
    [Trait("Category", "Core")]
    public void Mock_GenericRepository_WithComplexType_ShouldWork()
    {
        // Arrange
        var mock = Mock.Create<IRepository<User>>();
        var user = new User { Id = 1, Name = "John" };

        // Act
        mock.Setup(x => x.Get(1)).Returns(user);

        // Assert
        var result = mock.Get(1);
        Assert.Equal(user.Name, result?.Name);
    }

    [Fact]
    [Trait("Category", "Core")]
    public void Mock_GenericRepository_VoidMethod_ShouldWork()
    {
        // Arrange
        var mock = Mock.Create<IRepository<User>>();
        bool saveCalled = false;

        // Act - Use simple Callback without argument access for now
        mock.Setup(x => x.Save(It.IsAny<User>()))
            .Callback(() => saveCalled = true);

        var user = new User { Id = 1, Name = "Jane" };
        mock.Save(user);

        // Assert
        Assert.True(saveCalled);
    }

    [Fact]
    [Trait("Category", "Core")]
    public void Mock_NestedGenerics_ShouldWork()
    {
        // Arrange
        var mock = Mock.Create<IRepository<List<string>>>();

        // Act
        mock.Setup(x => x.Get(1)).Returns(new List<string> { "a", "b", "c" });

        // Assert
        var result = mock.Get(1);
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
    }
}
