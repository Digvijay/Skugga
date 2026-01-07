using Step2_WithSkugga.Models;

namespace Step2_WithSkugga.Services;

public interface IOrderService
{
    decimal GetPrice(int orderId);
    bool ValidateOrder(Order order);
    Task<Order> FetchOrderAsync(int orderId);
    int TotalOrders { get; }
    Order? GetOrderById(int orderId);
    IEnumerable<Order> GetRecentOrders(int count);
}

public interface IPaymentService
{
    bool ProcessPayment(decimal amount, string paymentMethod);
    void SendReceipt(string email);
    Task<string> GetPaymentStatusAsync(int transactionId);
    decimal GetRefundAmount(int orderId);
}

public interface INotificationService
{
    void SendEmail(string recipientEmail, string subject, string message);
    void LogActivity(string activity);
    Task SendSmsAsync(string phoneNumber, string message);
    bool IsServiceAvailable();
}

public interface IInventoryService
{
    bool CheckStock(int productId, int quantity);
    void ReserveStock(int productId, int quantity);
    void ReleaseStock(int productId, int quantity);
    int GetAvailableStock(int productId);
}
