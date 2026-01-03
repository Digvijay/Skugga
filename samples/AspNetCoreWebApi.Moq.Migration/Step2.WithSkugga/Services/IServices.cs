using Step2_WithSkugga.Models;

namespace Step2_WithSkugga.Services;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(int id);
    Task<IEnumerable<Product>> GetAllAsync();
    Task<IEnumerable<Product>> GetByCategoryAsync(string category);
    Task<Product> CreateAsync(Product product);
    Task<bool> UpdateAsync(Product product);
    Task<bool> DeleteAsync(int id);
}

public interface IInventoryService
{
    Task<bool> CheckStockAsync(int productId, int quantity);
    Task<bool> ReserveStockAsync(int productId, int quantity);
    Task<bool> ReleaseStockAsync(int productId, int quantity);
    Task<int> GetAvailableStockAsync(int productId);
}

public interface IPricingService
{
    decimal CalculateDiscount(decimal price, string category);
    Task<decimal> GetDynamicPriceAsync(int productId);
    bool ValidatePrice(decimal price);
}

public interface INotificationService
{
    Task SendLowStockAlertAsync(int productId, int currentStock);
    Task SendPriceChangeNotificationAsync(int productId, decimal oldPrice, decimal newPrice);
}
