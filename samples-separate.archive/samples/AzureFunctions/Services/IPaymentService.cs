namespace AzureFunctions.Services;

public interface IPaymentService
{
    Task<bool> ProcessPaymentAsync(string orderId, decimal amount);
    Task<string> GetPaymentStatusAsync(string orderId);
    Task<bool> RefundPaymentAsync(string orderId);
}
