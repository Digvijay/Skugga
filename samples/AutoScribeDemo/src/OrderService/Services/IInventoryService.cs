namespace OrderService.Services;

public interface IInventoryService
{
    Task<bool> CheckStockAsync(int productId, int quantity);
    Task ReserveStockAsync(int productId, int quantity);
}
