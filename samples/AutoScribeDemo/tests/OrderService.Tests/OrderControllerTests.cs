namespace OrderService.Tests;

using Xunit;
using FluentAssertions;
using Skugga.Core;
using OrderService.Models;
using OrderService.Services;

/// <summary>
/// Standard unit tests using Skugga mocks (compile-time, AOT-compatible).
/// No reflection, no runtime code generation.
/// </summary>
public class OrderControllerTests
{
    [Fact]
    public async Task PlaceOrder_ValidUserAndStock_ReturnsOrder()
    {
        // Arrange - Create Skugga mocks (compile-time generated)
        var mockUserRepo = Mock.Create<IUserRepository>();
        var mockInventory = Mock.Create<IInventoryService>();

        // Setup mock behavior
        mockUserRepo.Setup(x => x.GetUserAsync(1))
            .ReturnsAsync(new User { Id = 1, Name = "John Doe", Email = "john@example.com" });
        
        mockInventory.Setup(x => x.CheckStockAsync(101, 2)).ReturnsAsync(true);
        mockInventory.Setup(x => x.ReserveStockAsync(101, 2)).Returns(Task.CompletedTask);

        var items = new List<OrderItem>
        {
            new() { ProductId = 101, Quantity = 2, Price = 29.99m }
        };

        // Act - Test the controller
        var controller = new OrderController(mockUserRepo, mockInventory);
        var order = await controller.PlaceOrderAsync(1, items);

        // Assert
        order.Should().NotBeNull();
        order.UserId.Should().Be(1);
        order.Items.Should().HaveCount(1);
        order.TotalAmount.Should().Be(59.98m);
        
        // Verify mock interactions
        mockUserRepo.Verify(x => x.GetUserAsync(1), Times.Once());
        mockInventory.Verify(x => x.CheckStockAsync(101, 2), Times.Once());
        mockInventory.Verify(x => x.ReserveStockAsync(101, 2), Times.Once());
    }

    [Fact]
    public async Task PlaceOrder_UserNotFound_ThrowsException()
    {
        // Arrange
        var mockUserRepo = Mock.Create<IUserRepository>();
        var mockInventory = Mock.Create<IInventoryService>();

        mockUserRepo.Setup(x => x.GetUserAsync(999))
            .ReturnsAsync(new User());  // Empty user = not found

        var items = new List<OrderItem> { new OrderItem { ProductId = 101, Quantity = 1, Price = 10m } };

        // Act & Assert
        var controller = new OrderController(mockUserRepo, mockInventory);
        await Assert.ThrowsAsync<ArgumentException>(
            () => controller.PlaceOrderAsync(999, items)
        );
    }

    [Fact]
    public async Task PlaceOrder_OutOfStock_ThrowsException()
    {
        // Arrange
        var mockUserRepo = Mock.Create<IUserRepository>();
        var mockInventory = Mock.Create<IInventoryService>();

        mockUserRepo.Setup(x => x.GetUserAsync(1))
            .ReturnsAsync(new User { Id = 1, Name = "John" });
        mockInventory.Setup(x => x.CheckStockAsync(101, 10)).ReturnsAsync(false);

        var items = new List<OrderItem> { new OrderItem { ProductId = 101, Quantity = 10, Price = 10m } };

        // Act & Assert
        var controller = new OrderController(mockUserRepo, mockInventory);
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => controller.PlaceOrderAsync(1, items)
        );
    }
}
