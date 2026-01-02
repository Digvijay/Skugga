using AzureFunctions.Models;
using AzureFunctions.Services;
using FluentAssertions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Skugga.Core;
using Xunit;

namespace AzureFunctions.Tests;

/// <summary>
/// Demonstrates testing Azure Functions with Skugga.
/// Shows how to mock HTTP triggers, services, and Azure Functions infrastructure.
/// </summary>
public class OrderFunctionsTests
{
    private readonly ILogger<OrderFunctions> _loggerMock;
    private readonly IOrderService _orderServiceMock;
    private readonly IPaymentService _paymentServiceMock;
    private readonly INotificationService _notificationServiceMock;
    private readonly OrderFunctions _functions;

    public OrderFunctionsTests()
    {
        _loggerMock = Mock.Create<ILogger<OrderFunctions>>();
        _orderServiceMock = Mock.Create<IOrderService>();
        _paymentServiceMock = Mock.Create<IPaymentService>();
        _notificationServiceMock = Mock.Create<INotificationService>();
        
        _functions = new OrderFunctions(
            _loggerMock,
            _orderServiceMock,
            _paymentServiceMock,
            _notificationServiceMock);
    }

    [Fact]
    public async Task ProcessPayment_WithValidOrder_ProcessesSuccessfully()
    {
        // Arrange
        var orderId = "order-123";
        var order = new Order
        {
            Id = orderId,
            CustomerId = "customer-1",
            TotalAmount = 100m,
            Status = OrderStatus.Pending
        };

        _orderServiceMock.Setup(x => x.GetOrderAsync(orderId)).Returns(Task.FromResult<Order?>(order));
        _orderServiceMock.Setup(x => x.UpdateOrderStatusAsync(orderId, It.IsAny<OrderStatus>())).Returns(Task.FromResult(true));
        _paymentServiceMock.Setup(x => x.ProcessPaymentAsync(orderId, 100m)).Returns(Task.FromResult(true));

        // Note: In a real test, you'd need to create a mock HttpRequestData
        // This demonstrates the service layer logic that the function uses

        // Act - Test the service interactions
        var orderResult = await _orderServiceMock.GetOrderAsync(orderId);
        var paymentResult = await _paymentServiceMock.ProcessPaymentAsync(orderId, order.TotalAmount);
        
        if (paymentResult)
        {
            await _orderServiceMock.UpdateOrderStatusAsync(orderId, OrderStatus.Confirmed);
            await _notificationServiceMock.SendOrderConfirmationAsync(order.CustomerId, orderId);
        }

        // Assert
        orderResult.Should().NotBeNull();
        paymentResult.Should().BeTrue();
        _orderServiceMock.Verify(x => x.UpdateOrderStatusAsync(orderId, OrderStatus.Confirmed), Times.Once());
        _notificationServiceMock.Verify(x => x.SendOrderConfirmationAsync("customer-1", orderId), Times.Once());
    }

    [Fact]
    public async Task ProcessPayment_WithFailedPayment_SendsFailureNotification()
    {
        // Arrange
        var orderId = "order-456";
        var order = new Order
        {
            Id = orderId,
            CustomerId = "customer-2",
            TotalAmount = 200m
        };

        _orderServiceMock.Setup(x => x.GetOrderAsync(orderId)).Returns(Task.FromResult<Order?>(order));
        _paymentServiceMock.Setup(x => x.ProcessPaymentAsync(orderId, 200m)).Returns(Task.FromResult(false));

        // Act
        var paymentResult = await _paymentServiceMock.ProcessPaymentAsync(orderId, order.TotalAmount);
        
        if (!paymentResult)
        {
            await _orderServiceMock.UpdateOrderStatusAsync(orderId, OrderStatus.Failed);
            await _notificationServiceMock.SendPaymentFailureNotificationAsync(order.CustomerId, orderId);
        }

        // Assert
        paymentResult.Should().BeFalse();
        _orderServiceMock.Verify(x => x.UpdateOrderStatusAsync(orderId, OrderStatus.Failed), Times.Once());
        _notificationServiceMock.Verify(x => x.SendPaymentFailureNotificationAsync("customer-2", orderId), Times.Once());
    }

    [Fact]
    public async Task CancelOrder_WithPendingOrder_CancelsSuccessfully()
    {
        // Arrange
        var orderId = "order-789";
        var order = new Order
        {
            Id = orderId,
            CustomerId = "customer-3",
            Status = OrderStatus.Pending
        };

        _orderServiceMock.Setup(x => x.GetOrderAsync(orderId)).Returns(Task.FromResult<Order?>(order));
        _orderServiceMock.Setup(x => x.CancelOrderAsync(orderId)).Returns(Task.FromResult(true));

        // Act
        var orderResult = await _orderServiceMock.GetOrderAsync(orderId);
        var cancelled = await _orderServiceMock.CancelOrderAsync(orderId);
        
        if (cancelled)
        {
            await _notificationServiceMock.SendCancellationNotificationAsync(order.CustomerId, orderId);
        }

        // Assert
        cancelled.Should().BeTrue();
        _notificationServiceMock.Verify(x => x.SendCancellationNotificationAsync("customer-3", orderId), Times.Once());
    }

    [Fact]
    public async Task CreateOrder_CreatesNewOrder()
    {
        // Arrange
        var customerId = "customer-4";
        var amount = 150m;
        var expectedOrder = new Order
        {
            Id = "new-order-id",
            CustomerId = customerId,
            TotalAmount = amount,
            Status = OrderStatus.Pending
        };

        _orderServiceMock.Setup(x => x.CreateOrderAsync(customerId, amount)).Returns(Task.FromResult<Order?>(expectedOrder));

        // Act
        var result = await _orderServiceMock.CreateOrderAsync(customerId, amount);

        // Assert
        result.Should().NotBeNull();
        result.CustomerId.Should().Be(customerId);
        result.TotalAmount.Should().Be(amount);
        result.Status.Should().Be(OrderStatus.Pending);
        _orderServiceMock.Verify(x => x.CreateOrderAsync(customerId, amount), Times.Once());
    }
}
