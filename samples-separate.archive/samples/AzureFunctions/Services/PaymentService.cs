namespace AzureFunctions.Services;

public class PaymentService : IPaymentService
{
    public Task<bool> ProcessPaymentAsync(string orderId, decimal amount)
    {
        // Simulate payment processing
        var success = amount > 0 && amount < 10000;
        return Task.FromResult(success);
    }

    public Task<string> GetPaymentStatusAsync(string orderId)
    {
        return Task.FromResult("Completed");
    }

    public Task<bool> RefundPaymentAsync(string orderId)
    {
        return Task.FromResult(true);
    }
}
