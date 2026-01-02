namespace AzureFunctions.Services;

public interface INotificationService
{
    Task SendOrderConfirmationAsync(string customerId, string orderId);
    Task SendPaymentFailureNotificationAsync(string customerId, string orderId);
    Task SendCancellationNotificationAsync(string customerId, string orderId);
}
