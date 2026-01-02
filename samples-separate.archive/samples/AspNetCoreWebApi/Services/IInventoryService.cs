using AspNetCoreWebApi.Models;

namespace AspNetCoreWebApi.Services;

public interface IInventoryService
{
    Task<bool> IsInStockAsync(int productId);
    Task<bool> ReserveStockAsync(int productId, int quantity);
    Task<int> GetAvailableQuantityAsync(int productId);
}
