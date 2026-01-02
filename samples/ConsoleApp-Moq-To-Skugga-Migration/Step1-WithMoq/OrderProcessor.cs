using Step1_WithMoq.Models;
using Step1_WithMoq.Services;

namespace Step1_WithMoq;

public class OrderProcessor
{
    private readonly IOrderService _orderService;
    private readonly IPaymentService _paymentService;
    private readonly INotificationService _notificationService;
    private readonly IInventoryService _inventoryService;

    public OrderProcessor(
        IOrderService orderService,
        IPaymentService paymentService,
        INotificationService notificationService,
        IInventoryService inventoryService)
    {
        _orderService = orderService;
        _paymentService = paymentService;
        _notificationService = notificationService;
        _inventoryService = inventoryService;
    }

    public async Task<bool> ProcessOrderAsync(Order order)
    {
        // Validate order
        if (!_orderService.ValidateOrder(order))
        {
            _notificationService.LogActivity($"Order validation failed: {order.Id}");
            return false;
        }

        // Check inventory
        foreach (var item in order.Items)
        {
            if (!_inventoryService.CheckStock(item.ProductId, item.Quantity))
            {
                _notificationService.LogActivity($"Insufficient stock for product: {item.ProductId}");
                return false;
            }
        }

        // Reserve inventory
        foreach (var item in order.Items)
        {
            _inventoryService.ReserveStock(item.ProductId, item.Quantity);
        }

        // Process payment
        bool paymentSuccess = _paymentService.ProcessPayment(order.TotalAmount, "CreditCard");
        
        if (!paymentSuccess)
        {
            // Release inventory if payment fails
            foreach (var item in order.Items)
            {
                _inventoryService.ReleaseStock(item.ProductId, item.Quantity);
            }
            _notificationService.LogActivity($"Payment failed for order: {order.Id}");
            return false;
        }

        // Send notifications
        _paymentService.SendReceipt(order.CustomerEmail);
        _notificationService.SendEmail(
            order.CustomerEmail,
            "Order Confirmed",
            $"Your order #{order.Id} has been confirmed.");

        _notificationService.LogActivity($"Order processed successfully: {order.Id}");
        return true;
    }

    public decimal CalculateOrderTotal(Order order)
    {
        decimal subtotal = 0;
        foreach (var item in order.Items)
        {
            var price = _orderService.GetPrice(item.ProductId);
            subtotal += price * item.Quantity;
        }
        return subtotal;
    }

    public async Task<Order?> GetOrderWithStatusAsync(int orderId)
    {
        var order = await _orderService.FetchOrderAsync(orderId);
        if (order != null)
        {
            var paymentStatus = await _paymentService.GetPaymentStatusAsync(orderId);
            _notificationService.LogActivity($"Fetched order {orderId} with status: {paymentStatus}");
        }
        return order;
    }
}
