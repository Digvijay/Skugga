namespace OrderService;

using OrderService.Models;
using OrderService.Services;

/// <summary>
/// Order controller - this is what we want to test
/// </summary>
public class OrderController
{
    private readonly IUserRepository _userRepository;
    private readonly IInventoryService _inventoryService;

    public OrderController(IUserRepository userRepository, IInventoryService inventoryService)
    {
        _userRepository = userRepository;
        _inventoryService = inventoryService;
    }

    public async Task<Order> PlaceOrderAsync(int userId, List<OrderItem> items)
    {
        // Validate user exists
        var user = await _userRepository.GetUserAsync(userId);
        if (user == null || user.Id == 0)
            throw new ArgumentException("User not found", nameof(userId));

        // Check stock for all items
        foreach (var item in items)
        {
            var inStock = await _inventoryService.CheckStockAsync(item.ProductId, item.Quantity);
            if (!inStock)
                throw new InvalidOperationException($"Product {item.ProductId} out of stock");
        }

        // Reserve stock
        foreach (var item in items)
        {
            await _inventoryService.ReserveStockAsync(item.ProductId, item.Quantity);
        }

        // Create order
        var order = new Order
        {
            Id = new Random().Next(1000, 9999),
            UserId = userId,
            Items = items,
            TotalAmount = items.Sum(i => i.Price * i.Quantity),
            OrderDate = DateTime.UtcNow
        };

        return order;
    }
}
