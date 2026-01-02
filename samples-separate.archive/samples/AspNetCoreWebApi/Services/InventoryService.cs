using AspNetCoreWebApi.Repositories;

namespace AspNetCoreWebApi.Services;

public class InventoryService : IInventoryService
{
    private readonly IProductRepository _productRepository;

    public InventoryService(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<bool> IsInStockAsync(int productId)
    {
        var product = await _productRepository.GetByIdAsync(productId);
        return product != null && product.StockQuantity > 0;
    }

    public async Task<bool> ReserveStockAsync(int productId, int quantity)
    {
        var product = await _productRepository.GetByIdAsync(productId);
        if (product == null || product.StockQuantity < quantity)
            return false;

        product.StockQuantity -= quantity;
        return await _productRepository.UpdateAsync(product);
    }

    public async Task<int> GetAvailableQuantityAsync(int productId)
    {
        var product = await _productRepository.GetByIdAsync(productId);
        return product?.StockQuantity ?? 0;
    }
}
