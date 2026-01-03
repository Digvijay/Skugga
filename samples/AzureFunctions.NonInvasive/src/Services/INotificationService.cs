namespace OrdersApi.Services;

public interface INotificationService
{
    Task SendOrderConfirmationAsync(string orderId, string customerEmail);
    Task SendOrderCancellationAsync(string orderId, string customerEmail);
}
