using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Skugga.Core;
using Step2_WithSkugga.Controllers;
using Step2_WithSkugga.Models;
using Step2_WithSkugga.Services;

namespace Step2_WithSkugga.Tests;

public class ProductsControllerTests
{
    [Fact]
    public async Task GetAll_ReturnsAllProducts()
    {
        // Arrange
        var products = new List<Product>
        {
            new() { Id = 1, Name = "Laptop", Price = 999.99m, StockQuantity = 10, Category = "Electronics" },
            new() { Id = 2, Name = "Mouse", Price = 29.99m, StockQuantity = 50, Category = "Electronics" }
        };

        var mockRepo = Mock.Create<IProductRepository>();
        var mockInventory = Mock.Create<IInventoryService>();
        var mockPricing = Mock.Create<IPricingService>();
        var mockNotifications = Mock.Create<INotificationService>();
        var mockLogger = NullLogger<ProductsController>.Instance;

        mockRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(products);
        mockPricing.Setup(p => p.CalculateDiscount(It.IsAny<decimal>(), It.IsAny<string>())).Returns(0m);

        var controller = new ProductsController(mockRepo, mockInventory, 
            mockPricing, mockNotifications, mockLogger);

        // Act
        var result = await controller.GetAll();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedProducts = Assert.IsAssignableFrom<IEnumerable<ProductDto>>(okResult.Value);
        Assert.Equal(2, returnedProducts.Count());
    }

    [Fact]
    public async Task GetById_ExistingId_ReturnsProduct()
    {
        // Arrange
        var product = new Product 
        { 
            Id = 1, 
            Name = "Laptop", 
            Price = 999.99m, 
            StockQuantity = 10, 
            Category = "Electronics" 
        };

        var mockRepo = Mock.Create<IProductRepository>();
        var mockInventory = Mock.Create<IInventoryService>();
        var mockPricing = Mock.Create<IPricingService>();
        var mockNotifications = Mock.Create<INotificationService>();
        var mockLogger = NullLogger<ProductsController>.Instance;

        mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(product);
        mockPricing.Setup(p => p.CalculateDiscount(999.99m, "Electronics")).Returns(50m);

        var controller = new ProductsController(mockRepo, mockInventory, 
            mockPricing, mockNotifications, mockLogger);

        // Act
        var result = await controller.GetById(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedProduct = Assert.IsType<ProductDto>(okResult.Value);
        Assert.Equal("Laptop", returnedProduct.Name);
        Assert.Equal(949.99m, returnedProduct.Price); // With discount
    }

    [Fact]
    public async Task GetById_NonExistingId_ReturnsNotFound()
    {
        // Arrange
        var mockRepo = Mock.Create<IProductRepository>();
        var mockInventory = Mock.Create<IInventoryService>();
        var mockPricing = Mock.Create<IPricingService>();
        var mockNotifications = Mock.Create<INotificationService>();
        var mockLogger = NullLogger<ProductsController>.Instance;

        mockRepo.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Product?)null);

        var controller = new ProductsController(mockRepo, mockInventory, 
            mockPricing, mockNotifications, mockLogger);

        // Act
        var result = await controller.GetById(999);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetByCategory_ReturnsFilteredProducts()
    {
        // Arrange
        var products = new List<Product>
        {
            new() { Id = 1, Name = "Laptop", Price = 999.99m, StockQuantity = 10, Category = "Electronics" },
            new() { Id = 2, Name = "Mouse", Price = 29.99m, StockQuantity = 50, Category = "Electronics" }
        };

        var mockRepo = Mock.Create<IProductRepository>();
        var mockInventory = Mock.Create<IInventoryService>();
        var mockPricing = Mock.Create<IPricingService>();
        var mockNotifications = Mock.Create<INotificationService>();
        var mockLogger = NullLogger<ProductsController>.Instance;

        mockRepo.Setup(r => r.GetByCategoryAsync("Electronics")).ReturnsAsync(products);
        mockPricing.Setup(p => p.CalculateDiscount(It.IsAny<decimal>(), "Electronics")).Returns(0m);

        var controller = new ProductsController(mockRepo, mockInventory, 
            mockPricing, mockNotifications, mockLogger);

        // Act
        var result = await controller.GetByCategory("Electronics");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedProducts = Assert.IsAssignableFrom<IEnumerable<ProductDto>>(okResult.Value);
        Assert.Equal(2, returnedProducts.Count());
        Assert.All(returnedProducts, p => Assert.Equal("Electronics", p.Category));
    }

    [Fact]
    public async Task Create_ValidProduct_ReturnsCreated()
    {
        // Arrange
        var request = new CreateProductRequest
        {
            Name = "Keyboard",
            Price = 79.99m,
            StockQuantity = 25,
            Category = "Electronics"
        };

        var mockRepo = Mock.Create<IProductRepository>();
        var mockInventory = Mock.Create<IInventoryService>();
        var mockPricing = Mock.Create<IPricingService>();
        var mockNotifications = Mock.Create<INotificationService>();
        var mockLogger = NullLogger<ProductsController>.Instance;

        mockPricing.Setup(p => p.ValidatePrice(79.99m)).Returns(true);
        mockRepo.Setup(r => r.CreateAsync(It.IsAny<Product>()))
            .ReturnsAsync((Product p) => { p.Id = 3; return p; });
        mockPricing.Setup(p => p.CalculateDiscount(It.IsAny<decimal>(), It.IsAny<string>())).Returns(0m);

        var controller = new ProductsController(mockRepo, mockInventory, 
            mockPricing, mockNotifications, mockLogger);

        // Act
        var result = await controller.Create(request);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var product = Assert.IsType<ProductDto>(createdResult.Value);
        Assert.Equal("Keyboard", product.Name);
        Assert.Equal(3, product.Id);
    }

    [Fact]
    public async Task Create_InvalidPrice_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateProductRequest
        {
            Name = "Invalid Product",
            Price = -10m,
            StockQuantity = 5,
            Category = "Electronics"
        };

        var mockRepo = Mock.Create<IProductRepository>();
        var mockInventory = Mock.Create<IInventoryService>();
        var mockPricing = Mock.Create<IPricingService>();
        var mockNotifications = Mock.Create<INotificationService>();
        var mockLogger = NullLogger<ProductsController>.Instance;

        mockPricing.Setup(p => p.ValidatePrice(-10m)).Returns(false);

        var controller = new ProductsController(mockRepo, mockInventory, 
            mockPricing, mockNotifications, mockLogger);

        // Act
        var result = await controller.Create(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Invalid price", badRequestResult.Value);
    }

    [Fact]
    public async Task Update_PriceChanged_SendsNotification()
    {
        // Arrange
        var existingProduct = new Product 
        { 
            Id = 1, 
            Name = "Laptop", 
            Price = 999.99m, 
            StockQuantity = 10, 
            Category = "Electronics" 
        };

        var request = new CreateProductRequest
        {
            Name = "Laptop",
            Price = 899.99m, // Price reduced
            StockQuantity = 10,
            Category = "Electronics"
        };

        var mockRepo = Mock.Create<IProductRepository>();
        var mockInventory = Mock.Create<IInventoryService>();
        var mockPricing = Mock.Create<IPricingService>();
        var mockNotifications = Mock.Create<INotificationService>();
        var mockLogger = NullLogger<ProductsController>.Instance;

        mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existingProduct);
        mockRepo.Setup(r => r.UpdateAsync(It.IsAny<Product>())).ReturnsAsync(true);

        var controller = new ProductsController(mockRepo, mockInventory, 
            mockPricing, mockNotifications, mockLogger);

        // Act
        await controller.Update(1, request);

        // Assert
        mockNotifications.Verify(
            n => n.SendPriceChangeNotificationAsync(1, 999.99m, 899.99m), 
            Times.Once());
    }

    [Fact]
    public async Task Update_NonExistingProduct_ReturnsNotFound()
    {
        // Arrange
        var mockRepo = Mock.Create<IProductRepository>();
        var mockInventory = Mock.Create<IInventoryService>();
        var mockPricing = Mock.Create<IPricingService>();
        var mockNotifications = Mock.Create<INotificationService>();
        var mockLogger = NullLogger<ProductsController>.Instance;

        mockRepo.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Product?)null);

        var request = new CreateProductRequest
        {
            Name = "Product",
            Price = 100m,
            StockQuantity = 5,
            Category = "Test"
        };

        var controller = new ProductsController(mockRepo, mockInventory, 
            mockPricing, mockNotifications, mockLogger);

        // Act
        var result = await controller.Update(999, request);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task ReserveStock_SufficientStock_SendsLowStockAlert()
    {
        // Arrange
        var product = new Product 
        { 
            Id = 1, 
            Name = "Laptop", 
            Price = 999.99m, 
            StockQuantity = 15, 
            Category = "Electronics" 
        };

        var mockRepo = Mock.Create<IProductRepository>();
        var mockInventory = Mock.Create<IInventoryService>();
        var mockPricing = Mock.Create<IPricingService>();
        var mockNotifications = Mock.Create<INotificationService>();
        var mockLogger = NullLogger<ProductsController>.Instance;

        mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(product);
        mockInventory.Setup(i => i.CheckStockAsync(1, 10)).ReturnsAsync(true);
        mockInventory.Setup(i => i.ReserveStockAsync(1, 10)).ReturnsAsync(true);
        mockInventory.Setup(i => i.GetAvailableStockAsync(1)).ReturnsAsync(5); // Low stock

        var controller = new ProductsController(mockRepo, mockInventory, 
            mockPricing, mockNotifications, mockLogger);

        // Act
        var result = await controller.ReserveStock(1, 10);

        // Assert
        Assert.IsType<OkResult>(result);
        mockNotifications.Verify(
            n => n.SendLowStockAlertAsync(1, 5), 
            Times.Once());
    }

    [Fact]
    public async Task ReserveStock_InsufficientStock_ReturnsBadRequest()
    {
        // Arrange
        var product = new Product 
        { 
            Id = 1, 
            Name = "Laptop", 
            Price = 999.99m, 
            StockQuantity = 3, 
            Category = "Electronics" 
        };

        var mockRepo = Mock.Create<IProductRepository>();
        var mockInventory = Mock.Create<IInventoryService>();
        var mockPricing = Mock.Create<IPricingService>();
        var mockNotifications = Mock.Create<INotificationService>();
        var mockLogger = NullLogger<ProductsController>.Instance;

        mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(product);
        mockInventory.Setup(i => i.CheckStockAsync(1, 10)).ReturnsAsync(false);

        var controller = new ProductsController(mockRepo, mockInventory, 
            mockPricing, mockNotifications, mockLogger);

        // Act
        var result = await controller.ReserveStock(1, 10);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Insufficient stock", badRequestResult.Value);
        mockInventory.Verify(i => i.ReserveStockAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never());
    }

    [Fact]
    public async Task Delete_ExistingProduct_ReturnsNoContent()
    {
        // Arrange
        var product = new Product 
        { 
            Id = 1, 
            Name = "Laptop", 
            Price = 999.99m, 
            StockQuantity = 10, 
            Category = "Electronics" 
        };

        var mockRepo = Mock.Create<IProductRepository>();
        var mockInventory = Mock.Create<IInventoryService>();
        var mockPricing = Mock.Create<IPricingService>();
        var mockNotifications = Mock.Create<INotificationService>();
        var mockLogger = NullLogger<ProductsController>.Instance;

        mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(product);
        mockRepo.Setup(r => r.DeleteAsync(1)).ReturnsAsync(true);

        var controller = new ProductsController(mockRepo, mockInventory, 
            mockPricing, mockNotifications, mockLogger);

        // Act
        var result = await controller.Delete(1);

        // Assert
        Assert.IsType<NoContentResult>(result);
        mockRepo.Verify(r => r.DeleteAsync(1), Times.Once());
    }

    [Fact]
    public async Task PricingService_CalculatesDifferentDiscountsByCategory()
    {
        // Arrange - Demonstrates It.Is with predicates
        var product = new Product 
        { 
            Id = 1, 
            Name = "Laptop", 
            Price = 1000m, 
            StockQuantity = 10, 
            Category = "Electronics" 
        };

        var mockRepo = Mock.Create<IProductRepository>();
        var mockInventory = Mock.Create<IInventoryService>();
        var mockPricing = Mock.Create<IPricingService>();
        var mockNotifications = Mock.Create<INotificationService>();
        var mockLogger = NullLogger<ProductsController>.Instance;

        mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(product);
        
        // Different discounts for different categories
        mockPricing.Setup(p => p.CalculateDiscount(
            It.IsAny<decimal>(), 
            It.Is((string cat) => cat == "Electronics")))
            .Returns(100m);

        var controller = new ProductsController(mockRepo, mockInventory, 
            mockPricing, mockNotifications, mockLogger);

        // Act
        var result = await controller.GetById(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedProduct = Assert.IsType<ProductDto>(okResult.Value);
        Assert.Equal(900m, returnedProduct.Price); // 1000 - 100 discount
    }
}
