using AspNetCoreWebApi.Models;
using AspNetCoreWebApi.Repositories;
using AspNetCoreWebApi.Services;
using FluentAssertions;
using Skugga.Core;
using Xunit;

namespace AspNetCoreWebApi.Tests.Services;

/// <summary>
/// Demonstrates testing business logic services with Skugga.
/// </summary>
public class InventoryServiceTests
{
    private readonly IProductRepository _repositoryMock;
    private readonly InventoryService _service;

    public InventoryServiceTests()
    {
        _repositoryMock = Mock.Create<IProductRepository>();
        _service = new InventoryService(_repositoryMock);
    }

    [Fact]
    public async Task IsInStockAsync_WithAvailableStock_ReturnsTrue()
    {
        // Arrange
        var product = new Product { Id = 1, StockQuantity = 10 };
        _repositoryMock.Setup(x => x.GetByIdAsync(1)).Returns(Task.FromResult<Product?>(product));

        // Act
        var result = await _service.IsInStockAsync(1);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsInStockAsync_WithZeroStock_ReturnsFalse()
    {
        // Arrange
        var product = new Product { Id = 1, StockQuantity = 0 };
        _repositoryMock.Setup(x => x.GetByIdAsync(1)).Returns(Task.FromResult<Product?>(product));

        // Act
        var result = await _service.IsInStockAsync(1);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsInStockAsync_WithNonExistentProduct_ReturnsFalse()
    {
        // Arrange
        _repositoryMock.Setup(x => x.GetByIdAsync(999)).Returns(Task.FromResult<Product?>(null));

        // Act
        var result = await _service.IsInStockAsync(999);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ReserveStockAsync_WithSufficientStock_ReturnsTrue()
    {
        // Arrange
        var product = new Product { Id = 1, StockQuantity = 10 };
        _repositoryMock.Setup(x => x.GetByIdAsync(1)).Returns(Task.FromResult<Product?>(product));
        _repositoryMock.Setup(x => x.UpdateAsync(It.IsAny<Product>())).Returns(Task.FromResult(true));

        // Act
        var result = await _service.ReserveStockAsync(1, 5);

        // Assert
        result.Should().BeTrue();
        product.StockQuantity.Should().Be(5); // Verify stock was reduced
        _repositoryMock.Verify(x => x.UpdateAsync(product), Times.Once());
    }

    [Fact]
    public async Task ReserveStockAsync_WithInsufficientStock_ReturnsFalse()
    {
        // Arrange
        var product = new Product { Id = 1, StockQuantity = 3 };
        _repositoryMock.Setup(x => x.GetByIdAsync(1)).Returns(Task.FromResult<Product?>(product));

        // Act
        var result = await _service.ReserveStockAsync(1, 5);

        // Assert
        result.Should().BeFalse();
        product.StockQuantity.Should().Be(3); // Stock should remain unchanged
        _repositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Product>()), Times.Never());
    }

    [Fact]
    public async Task GetAvailableQuantityAsync_WithExistingProduct_ReturnsQuantity()
    {
        // Arrange
        var product = new Product { Id = 1, StockQuantity = 42 };
        _repositoryMock.Setup(x => x.GetByIdAsync(1)).Returns(Task.FromResult<Product?>(product));

        // Act
        var result = await _service.GetAvailableQuantityAsync(1);

        // Assert
        result.Should().Be(42);
    }

    [Fact]
    public async Task GetAvailableQuantityAsync_WithNonExistentProduct_ReturnsZero()
    {
        // Arrange
        _repositoryMock.Setup(x => x.GetByIdAsync(999)).Returns(Task.FromResult<Product?>(null));

        // Act
        var result = await _service.GetAvailableQuantityAsync(999);

        // Assert
        result.Should().Be(0);
    }
}
