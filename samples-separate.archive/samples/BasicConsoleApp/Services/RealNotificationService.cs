namespace BasicConsoleApp.Services;

/// <summary>
/// Real implementation (in production, this would send actual notifications).
/// </summary>
public class RealNotificationService : INotificationService
{
    public Task SendAlertAsync(string message)
    {
        Console.WriteLine($"[ALERT] {message}");
        return Task.CompletedTask;
    }

    public Task<bool> SendEmailAsync(string recipient, string subject, string body)
    {
        Console.WriteLine($"[EMAIL] To: {recipient}, Subject: {subject}");
        return Task.FromResult(true);
    }
}
