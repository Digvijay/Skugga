using AzureFunctions.Models;

namespace AzureFunctions.Services;

public interface IOrderService
{
    Task<Order?> GetOrderAsync(string orderId);
    Task<Order> CreateOrderAsync(string customerId, decimal amount);
    Task<bool> UpdateOrderStatusAsync(string orderId, OrderStatus status);
    Task<bool> CancelOrderAsync(string orderId);
}
