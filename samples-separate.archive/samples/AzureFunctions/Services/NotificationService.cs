namespace AzureFunctions.Services;

public class NotificationService : INotificationService
{
    public Task SendOrderConfirmationAsync(string customerId, string orderId)
    {
        Console.WriteLine($"Confirmation sent to {customerId} for order {orderId}");
        return Task.CompletedTask;
    }

    public Task SendPaymentFailureNotificationAsync(string customerId, string orderId)
    {
        Console.WriteLine($"Payment failure notification sent to {customerId} for order {orderId}");
        return Task.CompletedTask;
    }

    public Task SendCancellationNotificationAsync(string customerId, string orderId)
    {
        Console.WriteLine($"Cancellation notification sent to {customerId} for order {orderId}");
        return Task.CompletedTask;
    }
}
