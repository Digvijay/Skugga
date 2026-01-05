namespace OrderService;

using OrderService.Models;
using OrderService.Services;

/// <summary>
/// A more realistic controller with many dependencies - the kind you see in real enterprise apps.
/// This demonstrates where AutoScribe really shines: complex setup with lots of mocks.
/// </summary>
public class ComplexOrderController
{
    private readonly IUserRepository _userRepo;
    private readonly IInventoryService _inventory;
    private readonly IPaymentGateway _payment;
    private readonly IShippingService _shipping;
    private readonly IEmailService _email;
    private readonly ITaxCalculator _taxCalc;
    private readonly IDiscountService _discount;
    private readonly IAuditLogger _audit;
    private readonly INotificationService _notifications;

    public ComplexOrderController(
        IUserRepository userRepo,
        IInventoryService inventory,
        IPaymentGateway payment,
        IShippingService shipping,
        IEmailService email,
        ITaxCalculator taxCalc,
        IDiscountService discount,
        IAuditLogger audit,
        INotificationService notifications)
    {
        _userRepo = userRepo;
        _inventory = inventory;
        _payment = payment;
        _shipping = shipping;
        _email = email;
        _taxCalc = taxCalc;
        _discount = discount;
        _audit = audit;
        _notifications = notifications;
    }

    public async Task<Order> ProcessPremiumOrderAsync(int userId, List<OrderItem> items, string promoCode)
    {
        // 1. Validate user
        var user = await _userRepo.GetUserAsync(userId);
        if (user.Id == 0) throw new ArgumentException("User not found");

        // 2. Check inventory
        foreach (var item in items)
        {
            var inStock = await _inventory.CheckStockAsync(item.ProductId, item.Quantity);
            if (!inStock) throw new InvalidOperationException($"Product {item.ProductId} out of stock");
        }

        // 3. Calculate discount
        var subtotal = items.Sum(i => i.Price * i.Quantity);
        var discountAmount = await _discount.CalculateDiscountAsync(promoCode, subtotal);

        // 4. Calculate tax
        var taxAmount = await _taxCalc.CalculateTaxAsync(subtotal - discountAmount, user.State);

        // 5. Calculate shipping
        var shippingCost = await _shipping.GetShippingCostAsync(user.Address, items.Sum(i => i.Quantity));

        // 6. Process payment
        var total = subtotal - discountAmount + taxAmount + shippingCost;
        var paymentSuccess = await _payment.ChargeAsync(user.PaymentMethod, total);
        if (!paymentSuccess) throw new InvalidOperationException("Payment failed");

        // 7. Reserve inventory
        foreach (var item in items)
        {
            await _inventory.ReserveStockAsync(item.ProductId, item.Quantity);
        }

        // 8. Send notifications
        await _email.SendOrderConfirmationAsync(user.Email, total);
        await _notifications.SendPushNotificationAsync(userId, "Order confirmed!");

        // 9. Audit log
        await _audit.LogOrderCreatedAsync(userId, total);

        return new Order
        {
            UserId = userId,
            Items = items,
            TotalAmount = total,
            OrderDate = DateTime.UtcNow
        };
    }
}
