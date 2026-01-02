using AspNetCoreWebApi.Controllers;
using AspNetCoreWebApi.Models;
using AspNetCoreWebApi.Repositories;
using AspNetCoreWebApi.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Skugga.Core;
using Xunit;

namespace AspNetCoreWebApi.Tests.Controllers;

/// <summary>
/// Demonstrates testing ASP.NET Core controllers with Skugga.
/// Shows how to mock repositories, services, and loggers.
/// </summary>
public class ProductsControllerTests
{
    private readonly IProductRepository _repositoryMock;
    private readonly IInventoryService _inventoryMock;
    private readonly ILogger<ProductsController> _loggerMock;
    private readonly ProductsController _controller;

    public ProductsControllerTests()
    {
        _repositoryMock = Mock.Create<IProductRepository>();
        _inventoryMock = Mock.Create<IInventoryService>();
        _loggerMock = Mock.Create<ILogger<ProductsController>>();
        _controller = new ProductsController(_repositoryMock, _inventoryMock, _loggerMock);
    }

    [Fact]
    public async Task GetById_WhenProductExists_ReturnsOk()
    {
        // Arrange
        var product = new Product
        {
            Id = 1,
            Name = "Test Product",
            Description = "Test Description",
            Price = 99.99m,
            StockQuantity = 10
        };
        _repositoryMock.Setup(x => x.GetByIdAsync(1)).Returns(Task.FromResult<Product?>(product));

        // Act
        var result = await _controller.GetById(1);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        okResult!.Value.Should().Be(product);
    }

    [Fact]
    public async Task GetById_WhenProductNotFound_ReturnsNotFound()
    {
        // Arrange
        _repositoryMock.Setup(x => x.GetByIdAsync(999)).Returns(Task.FromResult<Product?>(null));

        // Act
        var result = await _controller.GetById(999);

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
        _repositoryMock.Verify(x => x.GetByIdAsync(999), Times.Once());
    }

    [Fact]
    public async Task Create_WithValidProduct_ReturnsCreatedAtAction()
    {
        // Arrange
        var newProduct = new Product
        {
            Name = "New Product",
            Description = "New Description",
            Price = 49.99m,
            StockQuantity = 5
        };

        var createdProduct = new Product
        {
            Id = 10,
            Name = newProduct.Name,
            Description = newProduct.Description,
            Price = newProduct.Price,
            StockQuantity = newProduct.StockQuantity,
            CreatedAt = DateTime.UtcNow
        };

        _repositoryMock.Setup(x => x.CreateAsync(newProduct)).Returns(Task.FromResult<Product?>(createdProduct));

        // Act
        var result = await _controller.Create(newProduct);

        // Assert
        result.Result.Should().BeOfType<CreatedAtActionResult>();
        var createdResult = result.Result as CreatedAtActionResult;
        createdResult!.Value.Should().Be(createdProduct);
        createdResult.ActionName.Should().Be(nameof(_controller.GetById));
    }

    [Fact]
    public async Task Delete_WhenProductExists_ReturnsNoContent()
    {
        // Arrange
        _repositoryMock.Setup(x => x.DeleteAsync(1)).Returns(Task.FromResult(true));

        // Act
        var result = await _controller.Delete(1);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        _repositoryMock.Verify(x => x.DeleteAsync(1), Times.Once());
    }

    [Fact]
    public async Task Delete_WhenProductNotFound_ReturnsNotFound()
    {
        // Arrange
        _repositoryMock.Setup(x => x.DeleteAsync(999)).Returns(Task.FromResult(false));

        // Act
        var result = await _controller.Delete(999);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetStock_WhenProductExists_ReturnsAvailableQuantity()
    {
        // Arrange
        _repositoryMock.Setup(x => x.ExistsAsync(1)).Returns(Task.FromResult(true));
        _inventoryMock.Setup(x => x.GetAvailableQuantityAsync(1)).Returns(Task.FromResult(42));

        // Act
        var result = await _controller.GetStock(1);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(new { productId = 1, availableQuantity = 42 });
    }

    [Fact]
    public async Task ReserveStock_WithSufficientStock_ReturnsOk()
    {
        // Arrange
        var request = new ReserveStockRequest(5);
        _inventoryMock.Setup(x => x.ReserveStockAsync(1, 5)).Returns(Task.FromResult(true));

        // Act
        var result = await _controller.ReserveStock(1, request);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        _inventoryMock.Verify(x => x.ReserveStockAsync(1, 5), Times.Once());
    }

    [Fact]
    public async Task ReserveStock_WithInsufficientStock_ReturnsBadRequest()
    {
        // Arrange
        var request = new ReserveStockRequest(100);
        _inventoryMock.Setup(x => x.ReserveStockAsync(1, 100)).Returns(Task.FromResult(false));

        // Act
        var result = await _controller.ReserveStock(1, request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequest = result as BadRequestObjectResult;
        badRequest!.Value.Should().Be("Insufficient stock or product not found");
    }

    [Fact]
    public async Task Update_WithMismatchedId_ReturnsBadRequest()
    {
        // Arrange
        var product = new Product { Id = 2, Name = "Test" };

        // Act
        var result = await _controller.Update(1, product);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Update_WhenProductNotExists_ReturnsNotFound()
    {
        // Arrange
        var product = new Product { Id = 999, Name = "Test" };
        _repositoryMock.Setup(x => x.ExistsAsync(999)).Returns(Task.FromResult(false));

        // Act
        var result = await _controller.Update(999, product);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetAll_ReturnsAllProducts()
    {
        // Arrange
        var products = new List<Product>
        {
            new() { Id = 1, Name = "Product 1", Price = 10m },
            new() { Id = 2, Name = "Product 2", Price = 20m },
            new() { Id = 3, Name = "Product 3", Price = 30m }
        };
        _repositoryMock.Setup(x => x.GetAllAsync()).Returns(Task.FromResult<IEnumerable<Product>>(products));

        // Act
        var result = await _controller.GetAll();

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(products);
    }
}
