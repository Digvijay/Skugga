using Xunit;
using Skugga.Core;

namespace Skugga.Core.Tests;

// Test interfaces
public interface IProduct
{
    int Id { get; set; }
    string Name { get; set; }
    decimal Price { get; set; }
    bool InStock { get; set; }
}

public interface IComplexEntity
{
    string Name { get; set; }
    int Count { get; set; }
    double Value { get; set; }
    bool IsActive { get; set; }
    string Description { get; set; }
}

public class SetupAllPropertiesTests
{
    [Fact]
    [Trait("Category", "Setup")]
    public void SetupAllProperties_ShouldTrackAllInterfaceProperties()
    {
        // Arrange
        var mock = Mock.Create<IProduct>();
        mock.SetupAllProperties();
        
        // Act
        mock.Id = 1;
        mock.Name = "Widget";
        mock.Price = 9.99m;
        mock.InStock = true;
        
        // Assert
        Assert.Equal(1, mock.Id);
        Assert.Equal("Widget", mock.Name);
        Assert.Equal(9.99m, mock.Price);
        Assert.True(mock.InStock);
    }
    
    [Fact]
    [Trait("Category", "Setup")]
    public void SetupAllProperties_DefaultValues_ShouldBeTypeDefaults()
    {
        // Arrange
        var mock = Mock.Create<IProduct>();
        mock.SetupAllProperties();
        
        // Act - read without setting
        var id = mock.Id;
        var name = mock.Name;
        var price = mock.Price;
        var inStock = mock.InStock;
        
        // Assert
        Assert.Equal(0, id); // int default
        Assert.Null(name); // reference type default
        Assert.Equal(0m, price); // decimal default
        Assert.False(inStock); // bool default
    }
    
    [Fact]
    [Trait("Category", "Setup")]
    public void SetupAllProperties_MultipleUpdates_ShouldMaintainLatestValue()
    {
        // Arrange
        var mock = Mock.Create<IProduct>();
        mock.SetupAllProperties();
        
        // Act
        mock.Name = "First";
        mock.Name = "Second";
        mock.Name = "Third";
        
        mock.Price = 10.00m;
        mock.Price = 20.00m;
        
        // Assert
        Assert.Equal("Third", mock.Name);
        Assert.Equal(20.00m, mock.Price);
    }
    
    [Fact]
    [Trait("Category", "Setup")]
    public void SetupAllProperties_WithMixedPropertyAccess_ShouldWork()
    {
        // Arrange
        var mock = Mock.Create<IComplexEntity>();
        mock.SetupAllProperties();
        
        // Act - mix of set and get operations
        mock.Name = "Test";
        var name = mock.Name;
        
        mock.Count = 5;
        mock.Count += 3; // Read and write
        var finalCount = mock.Count;
        
        // Assert
        Assert.Equal("Test", name);
        Assert.Equal(8, finalCount);
    }
    
    [Fact]
    [Trait("Category", "Setup")]
    public void SetupAllProperties_AllPropertiesIndependent_ShouldWork()
    {
        // Arrange
        var mock = Mock.Create<IComplexEntity>();
        mock.SetupAllProperties();
        
        // Act
        mock.Name = "Alice";
        mock.Count = 42;
        mock.Value = 3.14;
        mock.IsActive = true;
        mock.Description = "Test description";
        
        // Assert - each property maintains its own value
        Assert.Equal("Alice", mock.Name);
        Assert.Equal(42, mock.Count);
        Assert.Equal(3.14, mock.Value);
        Assert.True(mock.IsActive);
        Assert.Equal("Test description", mock.Description);
    }
    
    [Fact]
    [Trait("Category", "Setup")]
    public void SetupAllProperties_CanBeCalledAfterIndividualSetupProperty()
    {
        // Arrange
        var mock = Mock.Create<IProduct>();
        mock.SetupProperty(x => x.Name, "Initial");
        
        // Act - SetupAllProperties should not overwrite existing property setups
        mock.SetupAllProperties();
        
        // Assert - Name should keep its setup value
        Assert.Equal("Initial", mock.Name);
        
        // Other properties should work
        mock.Id = 5;
        Assert.Equal(5, mock.Id);
    }
    
    [Fact]
    [Trait("Category", "Setup")]
    public void SetupAllProperties_WithComplexPropertyInteraction_ShouldWork()
    {
        // Arrange
        var mock = Mock.Create<IProduct>();
        mock.SetupAllProperties();
        
        // Act - complex scenario
        mock.Id = 1;
        mock.Name = "Product";
        mock.Price = 100.00m;
        
        // Update based on other properties
        mock.Price = mock.Price * 1.1m; // 10% increase
        
        // Assert
        Assert.Equal(1, mock.Id);
        Assert.Equal("Product", mock.Name);
        Assert.Equal(110.00m, mock.Price);
    }
    
    [Fact]
    [Trait("Category", "Setup")]
    public void SetupAllProperties_CanCoexistWithSetup_ShouldWork()
    {
        // Arrange
        var mock = Mock.Create<IProduct>();
        mock.SetupAllProperties();
        
        // Setup a specific property behavior (should override SetupAllProperties for that property)
        mock.Setup(x => x.Name).Returns("Fixed Name");
        
        // Act
        mock.Id = 10;
        var name = mock.Name; // Should use Setup
        
        // Assert
        Assert.Equal(10, mock.Id); // SetupAllProperties
        Assert.Equal("Fixed Name", name); // Specific Setup takes precedence
    }
    
    [Fact]
    [Trait("Category", "Setup")]
    public void SetupAllProperties_StringProperties_ShouldHandleEmptyAndNonEmpty()
    {
        // Arrange
        var mock = Mock.Create<IComplexEntity>();
        mock.SetupAllProperties();
        
        // Act
        mock.Description = string.Empty;
        var result = mock.Description;
        
        // Assert
        Assert.Equal(string.Empty, result);
        
        // Now set to non-empty
        mock.Description = "Not empty";
        Assert.Equal("Not empty", mock.Description);
    }
    
    [Fact]
    [Trait("Category", "Setup")]
    public void SetupAllProperties_MultipleInterfaceInstances_ShouldBeIndependent()
    {
        // Arrange
        var mock1 = Mock.Create<IProduct>();
        var mock2 = Mock.Create<IProduct>();
        mock1.SetupAllProperties();
        mock2.SetupAllProperties();
        
        // Act
        mock1.Name = "Product 1";
        mock2.Name = "Product 2";
        
        // Assert
        Assert.Equal("Product 1", mock1.Name);
        Assert.Equal("Product 2", mock2.Name);
    }
}
