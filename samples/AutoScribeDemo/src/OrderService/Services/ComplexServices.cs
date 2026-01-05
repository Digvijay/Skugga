namespace OrderService.Services;

/// <summary>
/// Additional service interfaces for the complex controller demo.
/// In real apps, you'd have many more of these across different assemblies.
/// </summary>

public interface IPaymentGateway
{
    Task<bool> ChargeAsync(string paymentMethod, decimal amount);
}

public interface IShippingService
{
    Task<decimal> GetShippingCostAsync(string address, int totalItems);
}

public interface IEmailService
{
    Task SendOrderConfirmationAsync(string email, decimal amount);
}

public interface ITaxCalculator
{
    Task<decimal> CalculateTaxAsync(decimal amount, string state);
}

public interface IDiscountService
{
    Task<decimal> CalculateDiscountAsync(string promoCode, decimal subtotal);
}

public interface IAuditLogger
{
    Task LogOrderCreatedAsync(int userId, decimal amount);
}

public interface INotificationService
{
    Task SendPushNotificationAsync(int userId, string message);
}
