using AzureFunctions.Models;

namespace AzureFunctions.Services;

public class OrderService : IOrderService
{
    private static readonly Dictionary<string, Order> _orders = new();

    public Task<Order?> GetOrderAsync(string orderId)
    {
        _orders.TryGetValue(orderId, out var order);
        return Task.FromResult(order);
    }

    public Task<Order> CreateOrderAsync(string customerId, decimal amount)
    {
        var order = new Order
        {
            CustomerId = customerId,
            TotalAmount = amount,
            Status = OrderStatus.Pending
        };
        _orders[order.Id] = order;
        return Task.FromResult(order);
    }

    public Task<bool> UpdateOrderStatusAsync(string orderId, OrderStatus status)
    {
        if (!_orders.TryGetValue(orderId, out var order))
            return Task.FromResult(false);

        order.Status = status;
        return Task.FromResult(true);
    }

    public Task<bool> CancelOrderAsync(string orderId)
    {
        if (!_orders.TryGetValue(orderId, out var order))
            return Task.FromResult(false);

        if (order.Status != OrderStatus.Pending)
            return Task.FromResult(false);

        order.Status = OrderStatus.Cancelled;
        return Task.FromResult(true);
    }
}
