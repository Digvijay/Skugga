using OrdersApi.Services;
using OrdersApi.Models;
using Skugga.Core;

namespace OrdersApi.Tests;

/// <summary>
/// Demonstrates testing Azure Functions services with Skugga.
/// Production code (OrdersApi project) has ZERO Skugga dependencies!
/// </summary>
public class OrderServiceTests
{
    [Fact]
    public async Task GetOrderByIdAsync_ReturnsOrder()
    {
        // Arrange
        var mockService = Mock.Create<IOrderService>();
        var expectedOrder = new Order { Id = "order-123", CustomerId = "cust-1", TotalAmount = 100 };

        mockService.Setup(x => x.GetOrderByIdAsync("order-123"))
            .ReturnsAsync(expectedOrder);

        // Act
        var result = await mockService.GetOrderByIdAsync("order-123");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("order-123", result.Id);
        Assert.Equal(100, result.TotalAmount);
        mockService.Verify(x => x.GetOrderByIdAsync("order-123"), Times.Once());
    }

    [Fact]
    public async Task CreateOrderAsync_CreatesNewOrder()
    {
        // Arrange
        var mockService = Mock.Create<IOrderService>();
        var order = new Order { CustomerId = "cust-1", TotalAmount = 150 };
        var createdOrder = new Order { Id = "order-456", CustomerId = "cust-1", TotalAmount = 150 };

        mockService.Setup(x => x.CreateOrderAsync(It.IsAny<Order>()))
            .ReturnsAsync(createdOrder);

        // Act
        var result = await mockService.CreateOrderAsync(order);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("order-456", result.Id);
        mockService.Verify(x => x.CreateOrderAsync(It.IsAny<Order>()), Times.Once());
    }

    [Fact]
    public async Task CancelOrderAsync_CallsServiceMethod()
    {
        // Arrange
        var mockService = Mock.Create<IOrderService>();
        mockService.Setup(x => x.CancelOrderAsync("order-789"))
            .Returns(Task.CompletedTask);

        // Act
        await mockService.CancelOrderAsync("order-789");

        // Assert
        mockService.Verify(x => x.CancelOrderAsync("order-789"), Times.Once());
    }
}

public class CustomerServiceTests
{
    [Fact]
    public async Task GetCustomerByIdAsync_ReturnsCustomer()
    {
        // Arrange
        var mockService = Mock.Create<ICustomerService>();
        var expectedCustomer = new Customer { Id = "cust-1", Name = "John Doe", Email = "john@example.com" };

        mockService.Setup(x => x.GetCustomerByIdAsync("cust-1"))
            .ReturnsAsync(expectedCustomer);

        // Act
        var result = await mockService.GetCustomerByIdAsync("cust-1");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("John Doe", result.Name);
        Assert.Equal("john@example.com", result.Email);
    }
}

public class NotificationServiceTests
{
    [Fact]
    public async Task SendOrderConfirmationAsync_CallsService()
    {
        // Arrange
        var mockService = Mock.Create<INotificationService>();
        mockService.Setup(x => x.SendOrderConfirmationAsync("order-123", "test@example.com"))
            .Returns(Task.CompletedTask);

        // Act
        await mockService.SendOrderConfirmationAsync("order-123", "test@example.com");

        // Assert
        mockService.Verify(x => x.SendOrderConfirmationAsync("order-123", "test@example.com"), Times.Once());
    }

    [Fact]
    public async Task SendOrderCancellationAsync_CallsService()
    {
        // Arrange
        var mockService = Mock.Create<INotificationService>();
        mockService.Setup(x => x.SendOrderCancellationAsync("order-456", "customer@example.com"))
            .Returns(Task.CompletedTask);

        // Act
        await mockService.SendOrderCancellationAsync("order-456", "customer@example.com");

        // Assert
        mockService.Verify(x => x.SendOrderCancellationAsync("order-456", "customer@example.com"), Times.Once());
    }
}

/// <summary>
/// Demonstrates integration testing with multiple mocked services
/// </summary>
public class IntegrationTests
{
    [Fact]
    public async Task OrderWorkflow_WithMultipleMocks_WorksCorrectly()
    {
        // Arrange - Mock all dependencies
        var mockOrderService = Mock.Create<IOrderService>();
        var mockCustomerService = Mock.Create<ICustomerService>();
        var mockNotificationService = Mock.Create<INotificationService>();

        var order = new Order { Id = "order-999", CustomerId = "cust-1", TotalAmount = 200 };
        var customer = new Customer { Id = "cust-1", Email = "customer@example.com" };

        mockOrderService.Setup(x => x.GetOrderByIdAsync("order-999"))
            .ReturnsAsync(order);
        
        mockCustomerService.Setup(x => x.GetCustomerByIdAsync("cust-1"))
            .ReturnsAsync(customer);
        
        mockNotificationService.Setup(x => x.SendOrderConfirmationAsync("order-999", "customer@example.com"))
            .Returns(Task.CompletedTask);

        // Act - Simulate workflow
        var retrievedOrder = await mockOrderService.GetOrderByIdAsync("order-999");
        Assert.NotNull(retrievedOrder);
        
        var retrievedCustomer = await mockCustomerService.GetCustomerByIdAsync(retrievedOrder.CustomerId);
        Assert.NotNull(retrievedCustomer);
        
        await mockNotificationService.SendOrderConfirmationAsync(retrievedOrder.Id, retrievedCustomer.Email);

        // Assert - Verify all interactions
        mockOrderService.Verify(x => x.GetOrderByIdAsync("order-999"), Times.Once());
        mockCustomerService.Verify(x => x.GetCustomerByIdAsync("cust-1"), Times.Once());
        mockNotificationService.Verify(x => x.SendOrderConfirmationAsync("order-999", "customer@example.com"), Times.Once());
    }
}
