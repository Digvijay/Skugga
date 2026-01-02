using Skugga.Core;
using Step2_WithSkugga;
using Step2_WithSkugga.Models;
using Step2_WithSkugga.Services;
using Xunit;

namespace Step2_WithSkugga.Tests;

/// <summary>
/// Comprehensive test suite demonstrating ALL major Skugga features
/// ✅ These tests work in BOTH JIT and Native AOT mode!
/// API is 98% identical to Moq for easy migration.
/// </summary>
public class OrderProcessorTests
{
    [Fact]
    public async Task ProcessOrderAsync_ValidOrder_Success()
    {
        // Arrange - Demonstrates basic Setup/Returns
        var orderServiceMock = Mock.Create<IOrderService>();
        var paymentServiceMock = Mock.Create<IPaymentService>();
        var notificationServiceMock = Mock.Create<INotificationService>();
        var inventoryServiceMock = Mock.Create<IInventoryService>();

        var order = new Order
        {
            Id = 1,
            CustomerEmail = "customer@example.com",
            TotalAmount = 99.99m,
            Items = new List<OrderItem>
            {
                new() { ProductId = 101, Quantity = 2, UnitPrice = 49.995m }
            }
        };

        // Setup - Basic Returns
        orderServiceMock.Setup(x => x.ValidateOrder(order)).Returns(true);
        orderServiceMock.Setup(x => x.GetPrice(101)).Returns(49.995m);
        
        // Setup - Returns with argument matchers
        inventoryServiceMock.Setup(x => x.CheckStock(It.IsAny<int>(), It.IsAny<int>())).Returns(true);
        paymentServiceMock.Setup(x => x.ProcessPayment(It.IsAny<decimal>(), It.IsAny<string>())).Returns(true);

        var processor = new OrderProcessor(
            orderServiceMock,
            paymentServiceMock,
            notificationServiceMock,
            inventoryServiceMock);

        // Act
        var result = await processor.ProcessOrderAsync(order);

        // Assert - Demonstrates Verify
        Assert.True(result);
        orderServiceMock.Verify(x => x.ValidateOrder(order), Times.Once());
        paymentServiceMock.Verify(x => x.ProcessPayment(99.99m, "CreditCard"), Times.Once());
        notificationServiceMock.Verify(x => x.SendEmail(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>()), Times.Once());
    }

    [Fact]
    public async Task ProcessOrderAsync_InsufficientStock_ReturnsFalse()
    {
        // Arrange
        var orderServiceMock = Mock.Create<IOrderService>();
        var paymentServiceMock = Mock.Create<IPaymentService>();
        var notificationServiceMock = Mock.Create<INotificationService>();
        var inventoryServiceMock = Mock.Create<IInventoryService>();

        var order = new Order
        {
            Id = 2,
            Items = new List<OrderItem> { new() { ProductId = 101, Quantity = 10 } }
        };

        orderServiceMock.Setup(x => x.ValidateOrder(It.IsAny<Order>())).Returns(true);
        
        // Demonstrates It.Is with predicate
        inventoryServiceMock
            .Setup(x => x.CheckStock(It.Is<int>(id => id == 101), It.Is<int>(qty => qty > 5)))
            .Returns(false);

        var processor = new OrderProcessor(
            orderServiceMock,
            paymentServiceMock,
            notificationServiceMock,
            inventoryServiceMock);

        // Act
        var result = await processor.ProcessOrderAsync(order);

        // Assert
        Assert.False(result);
        
        // Demonstrates Verify with Times.Never
        paymentServiceMock.Verify(x => x.ProcessPayment(It.IsAny<decimal>(), It.IsAny<string>()), Times.Never());
    }

    [Fact]
    public async Task ProcessOrderAsync_PaymentFails_ReleasesInventory()
    {
        // Arrange
        var orderServiceMock = Mock.Create<IOrderService>();
        var paymentServiceMock = Mock.Create<IPaymentService>();
        var notificationServiceMock = Mock.Create<INotificationService>();
        var inventoryServiceMock = Mock.Create<IInventoryService>();

        var order = new Order
        {
            Id = 3,
            Items = new List<OrderItem> { new() { ProductId = 101, Quantity = 2 } }
        };

        orderServiceMock.Setup(x => x.ValidateOrder(It.IsAny<Order>())).Returns(true);
        inventoryServiceMock.Setup(x => x.CheckStock(It.IsAny<int>(), It.IsAny<int>())).Returns(true);
        paymentServiceMock.Setup(x => x.ProcessPayment(It.IsAny<decimal>(), It.IsAny<string>())).Returns(false);

        var processor = new OrderProcessor(
            orderServiceMock,
            paymentServiceMock,
            notificationServiceMock,
            inventoryServiceMock);

        // Act
        var result = await processor.ProcessOrderAsync(order);

        // Assert
        Assert.False(result);
        
        // Demonstrates Verify with Times.Once - inventory should be released
        inventoryServiceMock.Verify(x => x.ReleaseStock(101, 2), Times.Once());
    }

    [Fact]
    public void CalculateOrderTotal_MultipleItems_CorrectSum()
    {
        // Arrange
        var orderServiceMock = Mock.Create<IOrderService>();
        var paymentServiceMock = Mock.Create<IPaymentService>();
        var notificationServiceMock = Mock.Create<INotificationService>();
        var inventoryServiceMock = Mock.Create<IInventoryService>();

        // Demonstrates Setup with different return values for different arguments
        orderServiceMock.Setup(x => x.GetPrice(101)).Returns(10.00m);
        orderServiceMock.Setup(x => x.GetPrice(102)).Returns(20.00m);
        orderServiceMock.Setup(x => x.GetPrice(103)).Returns(30.00m);

        var order = new Order
        {
            Items = new List<OrderItem>
            {
                new() { ProductId = 101, Quantity = 2 },  // 20.00
                new() { ProductId = 102, Quantity = 1 },  // 20.00
                new() { ProductId = 103, Quantity = 3 }   // 90.00
            }
        };

        var processor = new OrderProcessor(
            orderServiceMock,
            paymentServiceMock,
            notificationServiceMock,
            inventoryServiceMock);

        // Act
        var total = processor.CalculateOrderTotal(order);

        // Assert
        Assert.Equal(130.00m, total);
    }

    [Fact]
    public async Task GetOrderWithStatusAsync_ValidOrder_ReturnsOrder()
    {
        // Arrange
        var orderServiceMock = Mock.Create<IOrderService>();
        var paymentServiceMock = Mock.Create<IPaymentService>();
        var notificationServiceMock = Mock.Create<INotificationService>();
        var inventoryServiceMock = Mock.Create<IInventoryService>();

        var expectedOrder = new Order { Id = 100, Status = OrderStatus.Completed };

        // Demonstrates async method mocking
        orderServiceMock
            .Setup(x => x.FetchOrderAsync(100))
            .ReturnsAsync(expectedOrder);

        paymentServiceMock
            .Setup(x => x.GetPaymentStatusAsync(100))
            .ReturnsAsync("Completed");

        var processor = new OrderProcessor(
            orderServiceMock,
            paymentServiceMock,
            notificationServiceMock,
            inventoryServiceMock);

        // Act
        var order = await processor.GetOrderWithStatusAsync(100);

        // Assert
        Assert.NotNull(order);
        Assert.Equal(100, order.Id);
        
        // Demonstrates Verify with async methods
        orderServiceMock.Verify(x => x.FetchOrderAsync(100), Times.Once());
        paymentServiceMock.Verify(x => x.GetPaymentStatusAsync(100), Times.Once());
    }

    [Fact]
    public void PropertyMocking_TotalOrders_ReturnsValue()
    {
        // Arrange
        var orderServiceMock = Mock.Create<IOrderService>();
        var paymentServiceMock = Mock.Create<IPaymentService>();
        var notificationServiceMock = Mock.Create<INotificationService>();
        var inventoryServiceMock = Mock.Create<IInventoryService>();

        // Demonstrates property mocking
        orderServiceMock.Setup(x => x.TotalOrders).Returns(42);

        // Act
        var totalOrders = orderServiceMock.TotalOrders;

        // Assert
        Assert.Equal(42, totalOrders);
    }

    [Fact]
    public void CallbackDemonstration_TrackMethodCalls()
    {
        // Arrange
        var orderServiceMock = Mock.Create<IOrderService>();
        var paymentServiceMock = Mock.Create<IPaymentService>();
        var notificationServiceMock = Mock.Create<INotificationService>();
        var inventoryServiceMock = Mock.Create<IInventoryService>();

        var loggedActivities = new List<string>();

        // Demonstrates Callback
        notificationServiceMock
            .Setup(x => x.LogActivity(It.IsAny<string>()))
            .Callback<string>(activity => loggedActivities.Add(activity));

        var processor = new OrderProcessor(
            orderServiceMock,
            paymentServiceMock,
            notificationServiceMock,
            inventoryServiceMock);

        // Act
        notificationServiceMock.LogActivity("Test activity 1");
        notificationServiceMock.LogActivity("Test activity 2");

        // Assert
        Assert.Equal(2, loggedActivities.Count);
        Assert.Contains("Test activity 1", loggedActivities);
        Assert.Contains("Test activity 2", loggedActivities);
    }

    [Fact]
    public void SetupSequence_MultipleReturns_ReturnsInOrder()
    {
        // Arrange
        var inventoryServiceMock = Mock.Create<IInventoryService>();

        // Demonstrates SetupSequence
        inventoryServiceMock
            .SetupSequence(x => x.GetAvailableStock(101))
            .Returns(100)
            .Returns(50)
            .Returns(0);

        // Act & Assert
        Assert.Equal(100, inventoryServiceMock.GetAvailableStock(101));
        Assert.Equal(50, inventoryServiceMock.GetAvailableStock(101));
        Assert.Equal(0, inventoryServiceMock.GetAvailableStock(101));
        Assert.Equal(0, inventoryServiceMock.GetAvailableStock(101)); // Repeats last
    }

    [Fact]
    public void ArgumentMatchers_ItIsAny_MatchesAnyValue()
    {
        // Arrange
        var paymentServiceMock = Mock.Create<IPaymentService>();

        // Demonstrates It.IsAny<T>()
        paymentServiceMock
            .Setup(x => x.ProcessPayment(It.IsAny<decimal>(), It.IsAny<string>()))
            .Returns(true);

        // Act & Assert
        Assert.True(paymentServiceMock.ProcessPayment(10.00m, "Visa"));
        Assert.True(paymentServiceMock.ProcessPayment(99.99m, "MasterCard"));
        Assert.True(paymentServiceMock.ProcessPayment(0.01m, "AmEx"));
    }

    [Fact]
    public void ArgumentMatchers_ItIs_MatchesWithPredicate()
    {
        // Arrange
        var paymentServiceMock = Mock.Create<IPaymentService>();

        // Demonstrates It.Is<T>(predicate)
        paymentServiceMock
            .Setup(x => x.ProcessPayment(
                It.Is<decimal>(amount => amount > 0 && amount < 1000),
                It.Is<string>(method => method == "CreditCard")))
            .Returns(true);

        // Act & Assert
        Assert.True(paymentServiceMock.ProcessPayment(50.00m, "CreditCard"));
        Assert.False(paymentServiceMock.ProcessPayment(1500.00m, "CreditCard")); // Too high
        Assert.False(paymentServiceMock.ProcessPayment(50.00m, "Cash")); // Wrong method
    }

    [Fact]
    public void VerifyWithTimes_AtLeast_ChecksMinimumCalls()
    {
        // Arrange
        var notificationServiceMock = Mock.Create<INotificationService>();

        // Act
        notificationServiceMock.LogActivity("Activity 1");
        notificationServiceMock.LogActivity("Activity 2");
        notificationServiceMock.LogActivity("Activity 3");

        // Assert - Demonstrates Times.AtLeast
        notificationServiceMock.Verify(x => x.LogActivity(It.IsAny<string>()), Times.AtLeast(2));
        notificationServiceMock.Verify(x => x.LogActivity(It.IsAny<string>()), Times.Exactly(3));
    }

    [Fact]
    public async Task ComplexScenario_FullOrderFlow_AllFeaturesWorking()
    {
        // This test demonstrates multiple Moq features working together
        var orderServiceMock = Mock.Create<IOrderService>();
        var paymentServiceMock = Mock.Create<IPaymentService>();
        var notificationServiceMock = Mock.Create<INotificationService>();
        var inventoryServiceMock = Mock.Create<IInventoryService>();

        var order = new Order
        {
            Id = 999,
            CustomerEmail = "vip@example.com",
            TotalAmount = 499.99m,
            Items = new List<OrderItem>
            {
                new() { ProductId = 201, Quantity = 5 },
                new() { ProductId = 202, Quantity = 3 }
            }
        };

        var loggedActivities = new List<string>();

        // Multiple setup styles
        orderServiceMock.Setup(x => x.ValidateOrder(It.Is<Order>(o => o.TotalAmount > 100))).Returns(true);
        orderServiceMock.Setup(x => x.GetPrice(201)).Returns(59.99m);
        orderServiceMock.Setup(x => x.GetPrice(202)).Returns(79.99m);
        
        inventoryServiceMock.Setup(x => x.CheckStock(It.IsAny<int>(), It.IsAny<int>())).Returns(true);
        paymentServiceMock.Setup(x => x.ProcessPayment(It.IsAny<decimal>(), "CreditCard")).Returns(true);
        
        notificationServiceMock
            .Setup(x => x.LogActivity(It.IsAny<string>()))
            .Callback<string>(activity => loggedActivities.Add(activity));

        var processor = new OrderProcessor(
            orderServiceMock,
            paymentServiceMock,
            notificationServiceMock,
            inventoryServiceMock);

        // Act
        var result = await processor.ProcessOrderAsync(order);

        // Assert - Multiple verification styles
        Assert.True(result);
        Assert.Contains(loggedActivities, a => a.Contains("processed successfully"));
        
        orderServiceMock.Verify(x => x.ValidateOrder(order), Times.Once());
        inventoryServiceMock.Verify(x => x.ReserveStock(It.IsAny<int>(), It.IsAny<int>()), Times.Exactly(2));
        paymentServiceMock.Verify(x => x.SendReceipt("vip@example.com"), Times.Once());
        notificationServiceMock.Verify(x => x.SendEmail(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once());
    }
}
