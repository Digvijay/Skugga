namespace OrdersApi.Services;

public class NotificationService : INotificationService
{
    public Task SendOrderConfirmationAsync(string orderId, string customerEmail)
    {
        // Simulated implementation
        return Task.CompletedTask;
    }

    public Task SendOrderCancellationAsync(string orderId, string customerEmail)
    {
        return Task.CompletedTask;
    }
}
