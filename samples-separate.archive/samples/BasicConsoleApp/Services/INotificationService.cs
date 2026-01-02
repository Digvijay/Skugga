namespace BasicConsoleApp.Services;

/// <summary>
/// Service for sending notifications.
/// This interface will be mocked in tests using Skugga.
/// </summary>
public interface INotificationService
{
    Task SendAlertAsync(string message);
    Task<bool> SendEmailAsync(string recipient, string subject, string body);
}
