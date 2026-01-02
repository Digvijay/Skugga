using AspNetCoreWebApi.Models;

namespace AspNetCoreWebApi.Repositories;

/// <summary>
/// In-memory implementation for demo purposes.
/// In a real app, this would be EF Core or Dapper.
/// </summary>
public class InMemoryProductRepository : IProductRepository
{
    private readonly Dictionary<int, Product> _products = new();
    private int _nextId = 1;

    public InMemoryProductRepository()
    {
        // Seed some data
        var seedProducts = new[]
        {
            new Product { Id = _nextId++, Name = "Laptop", Description = "High-performance laptop", Price = 1299.99m, StockQuantity = 15, CreatedAt = DateTime.UtcNow },
            new Product { Id = _nextId++, Name = "Mouse", Description = "Wireless mouse", Price = 29.99m, StockQuantity = 50, CreatedAt = DateTime.UtcNow },
            new Product { Id = _nextId++, Name = "Keyboard", Description = "Mechanical keyboard", Price = 89.99m, StockQuantity = 30, CreatedAt = DateTime.UtcNow }
        };

        foreach (var product in seedProducts)
        {
            _products[product.Id] = product;
        }
    }

    public Task<Product?> GetByIdAsync(int id)
    {
        _products.TryGetValue(id, out var product);
        return Task.FromResult(product);
    }

    public Task<IEnumerable<Product>> GetAllAsync()
    {
        return Task.FromResult<IEnumerable<Product>>(_products.Values.ToList());
    }

    public Task<Product> CreateAsync(Product product)
    {
        product.Id = _nextId++;
        product.CreatedAt = DateTime.UtcNow;
        _products[product.Id] = product;
        return Task.FromResult(product);
    }

    public Task<bool> UpdateAsync(Product product)
    {
        if (!_products.ContainsKey(product.Id))
            return Task.FromResult(false);

        _products[product.Id] = product;
        return Task.FromResult(true);
    }

    public Task<bool> DeleteAsync(int id)
    {
        return Task.FromResult(_products.Remove(id));
    }

    public Task<bool> ExistsAsync(int id)
    {
        return Task.FromResult(_products.ContainsKey(id));
    }
}
