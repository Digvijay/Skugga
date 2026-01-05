using Xunit;
using Skugga.Core;

namespace Skugga.Core.Tests;

// Test interfaces
public interface IUserProfile
{
    string Name { get; set; }
    int Age { get; set; }
    string Email { get; set; }
    bool IsActive { get; set; }
}

public interface IConfiguration
{
    string ConnectionString { get; set; }
    int Timeout { get; set; }
    bool EnableLogging { get; set; }
}

public class SetupPropertyTests
{
    [Fact]
    [Trait("Category", "Setup")]
    public void SetupProperty_SingleProperty_ShouldTrackGetAndSet()
    {
        // Arrange
        var mock = Mock.Create<IUserProfile>();
        mock.SetupProperty(x => x.Name);
        
        // Act
        mock.Name = "John";
        var result = mock.Name;
        
        // Assert
        Assert.Equal("John", result);
    }
    
    [Fact]
    [Trait("Category", "Setup")]
    public void SetupProperty_WithDefaultValue_ShouldReturnDefault()
    {
        // Arrange
        var mock = Mock.Create<IUserProfile>();
        mock.SetupProperty(x => x.Age, 25);
        
        // Act
        var result = mock.Age;
        
        // Assert
        Assert.Equal(25, result);
    }
    
    [Fact]
    [Trait("Category", "Setup")]
    public void SetupProperty_MultipleProperties_ShouldTrackIndependently()
    {
        // Arrange
        var mock = Mock.Create<IUserProfile>();
        mock.SetupProperty(x => x.Name);
        mock.SetupProperty(x => x.Age, 30);
        
        // Act
        mock.Name = "Alice";
        mock.Age = 35;
        
        // Assert
        Assert.Equal("Alice", mock.Name);
        Assert.Equal(35, mock.Age);
    }
    
    [Fact]
    [Trait("Category", "Setup")]
    public void SetupProperty_SetMultipleTimes_ShouldReturnLatestValue()
    {
        // Arrange
        var mock = Mock.Create<IUserProfile>();
        mock.SetupProperty(x => x.Name);
        
        // Act
        mock.Name = "First";
        mock.Name = "Second";
        mock.Name = "Third";
        var result = mock.Name;
        
        // Assert
        Assert.Equal("Third", result);
    }
    
    [Fact]
    [Trait("Category", "Setup")]
    public void SetupProperty_WithoutDefaultValue_ShouldReturnDefaultForType()
    {
        // Arrange
        var mock = Mock.Create<IUserProfile>();
        mock.SetupProperty(x => x.Name); // string - default is null
        mock.SetupProperty(x => x.Age);  // int - default is 0
        
        // Act
        var name = mock.Name;
        var age = mock.Age;
        
        // Assert
        Assert.Null(name);
        Assert.Equal(0, age);
    }
    
    [Fact]
    [Trait("Category", "Setup")]
    public void SetupProperty_BoolProperty_ShouldWork()
    {
        // Arrange
        var mock = Mock.Create<IUserProfile>();
        mock.SetupProperty(x => x.IsActive, true);
        
        // Act
        var initial = mock.IsActive;
        mock.IsActive = false;
        var updated = mock.IsActive;
        
        // Assert
        Assert.True(initial);
        Assert.False(updated);
    }
    
    [Fact]
    [Trait("Category", "Setup")]
    public void SetupProperty_DifferentInterfaces_ShouldWork()
    {
        // Arrange
        var mock = Mock.Create<IConfiguration>();
        mock.SetupProperty(x => x.ConnectionString, "Server=localhost");
        mock.SetupProperty(x => x.Timeout, 30);
        
        // Act
        var connString = mock.ConnectionString;
        mock.Timeout = 60;
        var timeout = mock.Timeout;
        
        // Assert
        Assert.Equal("Server=localhost", connString);
        Assert.Equal(60, timeout);
    }
    
    [Fact]
    [Trait("Category", "Setup")]
    public void SetupProperty_GetBeforeSet_ShouldReturnDefaultValue()
    {
        // Arrange
        var mock = Mock.Create<IUserProfile>();
        mock.SetupProperty(x => x.Age, 18);
        
        // Act - Get without setting
        var result = mock.Age;
        
        // Assert
        Assert.Equal(18, result);
    }
    
    [Fact]
    [Trait("Category", "Setup")]
    public void SetupProperty_ComplexScenario_ShouldMaintainState()
    {
        // Arrange
        var mock = Mock.Create<IUserProfile>();
        mock.SetupProperty(x => x.Name, "Initial");
        mock.SetupProperty(x => x.Age, 25);
        mock.SetupProperty(x => x.Email);
        
        // Act
        var name1 = mock.Name; // Should be "Initial"
        mock.Name = "Updated";
        var name2 = mock.Name; // Should be "Updated"
        
        mock.Email = "test@example.com";
        var email = mock.Email;
        
        mock.Age = 30;
        var age = mock.Age;
        
        // Assert
        Assert.Equal("Initial", name1);
        Assert.Equal("Updated", name2);
        Assert.Equal("test@example.com", email);
        Assert.Equal(30, age);
    }
    
    [Fact]
    [Trait("Category", "Setup")]
    public void SetupProperty_CanCoexistWithSetupReturns()
    {
        // Arrange
        var mock = Mock.Create<IUserProfile>();
        mock.SetupProperty(x => x.Name); // Use backing field
        mock.Setup(x => x.Email).Returns("fixed@example.com"); // Use setup
        
        // Act
        mock.Name = "Dynamic";
        var name = mock.Name;
        var email = mock.Email;
        
        // Assert
        Assert.Equal("Dynamic", name);
        Assert.Equal("fixed@example.com", email);
    }
}
