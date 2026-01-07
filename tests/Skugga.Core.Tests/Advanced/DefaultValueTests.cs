using System.Collections.Generic;
using System.Linq;
using Skugga.Core;
using Xunit;

namespace Skugga.Core.Tests;

public interface IDataService
{
    string GetName();
    int GetCount();
    List<string> GetItems();
    IEnumerable<int> GetNumbers();
    int[] GetArray();
    Dictionary<string, int> GetDictionary();
    ILogger GetLogger();
    IProcessor GetProcessor();
}

public interface ILogger
{
    void Log(string message);
    string GetLevel();
}

public interface IProcessor
{
    int Process(int value);
}

public class DefaultValueTests
{
    [Fact]
    [Trait("Category", "Advanced")]
    public void DefaultValue_Empty_StringReturnsEmptyString()
    {
        // Arrange
        var mock = Mock.Create<IDataService>(DefaultValue.Empty);

        // Act
        var result = mock.GetName();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    [Trait("Category", "Advanced")]
    public void DefaultValue_Empty_ListReturnsEmptyList()
    {
        // Arrange
        var mock = Mock.Create<IDataService>(DefaultValue.Empty);

        // Act
        var result = mock.GetItems();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
        Assert.IsType<List<string>>(result);
    }

    [Fact]
    [Trait("Category", "Advanced")]
    public void DefaultValue_Empty_IEnumerableReturnsEmptyList()
    {
        // Arrange
        var mock = Mock.Create<IDataService>(DefaultValue.Empty);

        // Act
        var result = mock.GetNumbers();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    [Trait("Category", "Advanced")]
    public void DefaultValue_Empty_ArrayReturnsEmptyArray()
    {
        // Arrange
        var mock = Mock.Create<IDataService>(DefaultValue.Empty);

        // Act
        var result = mock.GetArray();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
        Assert.IsType<int[]>(result);
    }

    [Fact]
    [Trait("Category", "Advanced")]
    public void DefaultValue_Empty_DictionaryReturnsEmptyDictionary()
    {
        // Arrange
        var mock = Mock.Create<IDataService>(DefaultValue.Empty);

        // Act
        var result = mock.GetDictionary();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
        Assert.IsType<Dictionary<string, int>>(result);
    }

    [Fact]
    [Trait("Category", "Advanced")]
    public void DefaultValue_Empty_ValueTypeReturnsDefault()
    {
        // Arrange
        var mock = Mock.Create<IDataService>(DefaultValue.Empty);

        // Act
        var result = mock.GetCount();

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    [Trait("Category", "Advanced")]
    public void DefaultValue_Mock_InterfaceReturnsMockInstance()
    {
        // Arrange
        // Force generation of mock for ILogger by referencing it
        var _ = Mock.Create<ILogger>();
        var mock = Mock.Create<IDataService>(DefaultValue.Mock);

        // Act
        var logger = mock.GetLogger();

        // Assert
        Assert.NotNull(logger);
        Assert.IsAssignableFrom<ILogger>(logger);
    }

    [Fact]
    [Trait("Category", "Advanced")]
    public void DefaultValue_Mock_NestedInterfaceReturnsMockInstance()
    {
        // Arrange
        // Force generation of mock for IProcessor by referencing it
        var _ = Mock.Create<IProcessor>();
        var mock = Mock.Create<IDataService>(DefaultValue.Mock);

        // Act
        var processor = mock.GetProcessor();

        // Assert
        Assert.NotNull(processor);
        Assert.IsAssignableFrom<IProcessor>(processor);
    }

    [Fact]
    [Trait("Category", "Advanced")]
    public void DefaultValue_Mock_RecursiveMocking_CanCallNestedMethods()
    {
        // Arrange
        // Force generation of mock for ILogger by referencing it
        var _ = Mock.Create<ILogger>();
        var mock = Mock.Create<IDataService>(DefaultValue.Mock);

        // Act
        var logger = mock.GetLogger();
        var level = logger.GetLevel(); // Should not throw

        // Assert
        Assert.NotNull(logger);
        Assert.NotNull(level); // Should return empty string
        Assert.Equal(string.Empty, level);
    }

    [Fact]
    [Trait("Category", "Advanced")]
    public void DefaultValue_Mock_ListsStillReturnEmpty()
    {
        // Arrange
        var mock = Mock.Create<IDataService>(DefaultValue.Mock);

        // Act
        var items = mock.GetItems();

        // Assert
        Assert.NotNull(items);
        Assert.Empty(items);
    }

    [Fact]
    [Trait("Category", "Advanced")]
    public void DefaultValue_Mock_StringReturnsEmptyString()
    {
        // Arrange
        var mock = Mock.Create<IDataService>(DefaultValue.Mock);

        // Act
        var name = mock.GetName();

        // Assert
        Assert.NotNull(name);
        Assert.Equal(string.Empty, name);
    }

    [Fact]
    [Trait("Category", "Advanced")]
    public void DefaultValue_WithBehavior_Empty_Works()
    {
        // Arrange
        var mock = Mock.Create<IDataService>(MockBehavior.Loose, DefaultValue.Empty);

        // Act
        var items = mock.GetItems();

        // Assert
        Assert.NotNull(items);
        Assert.Empty(items);
    }

    [Fact]
    [Trait("Category", "Advanced")]
    public void DefaultValue_WithBehavior_Mock_Works()
    {
        // Arrange
        // Force generation of mock for ILogger by referencing it
        var _ = Mock.Create<ILogger>();
        var mock = Mock.Create<IDataService>(MockBehavior.Loose, DefaultValue.Mock);

        // Act
        var logger = mock.GetLogger();

        // Assert
        Assert.NotNull(logger);
    }

    [Fact]
    [Trait("Category", "Advanced")]
    public void DefaultValue_SetupOverrides_Empty()
    {
        // Arrange
        var mock = Mock.Create<IDataService>(DefaultValue.Empty);
        var specificList = new List<string> { "item1", "item2" };
        mock.Setup(x => x.GetItems()).Returns(specificList);

        // Act
        var result = mock.GetItems();

        // Assert
        Assert.Same(specificList, result);
        Assert.Equal(2, result.Count);
    }

    [Fact]
    [Trait("Category", "Advanced")]
    public void DefaultValue_SetupOverrides_Mock()
    {
        // Arrange
        var mock = Mock.Create<IDataService>(DefaultValue.Mock);
        var specificLogger = Mock.Create<ILogger>();
        mock.Setup(x => x.GetLogger()).Returns(specificLogger);

        // Act
        var result = mock.GetLogger();

        // Assert
        Assert.Same(specificLogger, result);
    }

    [Fact]
    [Trait("Category", "Advanced")]
    public void CustomDefaultValueProvider_CanBeSet()
    {
        // Arrange
        var mock = Mock.Create<IDataService>();
        var customProvider = new CustomStringProvider();

        if (mock is IMockSetup setup)
        {
            setup.Handler.DefaultValueProvider = customProvider;
        }

        // Act
        var result = mock.GetName();

        // Assert
        Assert.Equal("CUSTOM", result);
    }

    [Fact]
    [Trait("Category", "Advanced")]
    public void EmptyDefaultValueProvider_DirectUsage()
    {
        // Arrange
        var provider = new EmptyDefaultValueProvider();
        var mock = new object();

        // Act
        var stringResult = provider.GetDefaultValue(typeof(string), mock);
        var listResult = provider.GetDefaultValue(typeof(List<int>), mock);
        var arrayResult = provider.GetDefaultValue(typeof(int[]), mock);

        // Assert
        Assert.Equal(string.Empty, stringResult);
        Assert.NotNull(listResult);
        Assert.IsType<List<int>>(listResult);
        Assert.NotNull(arrayResult);
        Assert.IsType<int[]>(arrayResult);
    }
}

/// <summary>
/// Custom provider that returns "CUSTOM" for all strings
/// </summary>
public class CustomStringProvider : DefaultValueProvider
{
    public override object? GetDefaultValue(Type type, object mock)
    {
        if (type == typeof(string))
        {
            return "CUSTOM";
        }

        // Fall back to empty provider for other types
        return new EmptyDefaultValueProvider().GetDefaultValue(type, mock);
    }
}
