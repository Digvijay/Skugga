using OrdersApi.Models;

namespace OrdersApi.Services;

public interface IOrderService
{
    Task<Order?> GetOrderByIdAsync(string orderId);
    Task<IEnumerable<Order>> GetOrdersByCustomerIdAsync(string customerId);
    Task<Order> CreateOrderAsync(Order order);
    Task UpdateOrderStatusAsync(string orderId, string status);
    Task CancelOrderAsync(string orderId);
}
