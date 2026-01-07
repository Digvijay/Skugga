using Microsoft.AspNetCore.Mvc;
using Step1_WithMoq.Models;
using Step1_WithMoq.Services;

namespace Step1_WithMoq.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IProductRepository _repository;
    private readonly IInventoryService _inventory;
    private readonly IPricingService _pricing;
    private readonly INotificationService _notifications;
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(
        IProductRepository repository,
        IInventoryService inventory,
        IPricingService pricing,
        INotificationService notifications,
        ILogger<ProductsController> logger)
    {
        _repository = repository;
        _inventory = inventory;
        _pricing = pricing;
        _notifications = notifications;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProductDto>>> GetAll()
    {
        var products = await _repository.GetAllAsync();
        return Ok(products.Select(MapToDto));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ProductDto>> GetById(int id)
    {
        var product = await _repository.GetByIdAsync(id);
        if (product == null)
            return NotFound();

        return Ok(MapToDto(product));
    }

    [HttpGet("category/{category}")]
    public async Task<ActionResult<IEnumerable<ProductDto>>> GetByCategory(string category)
    {
        var products = await _repository.GetByCategoryAsync(category);
        return Ok(products.Select(MapToDto));
    }

    [HttpPost]
    public async Task<ActionResult<ProductDto>> Create(CreateProductRequest request)
    {
        if (!_pricing.ValidatePrice(request.Price))
            return BadRequest("Invalid price");

        var product = new Product
        {
            Name = request.Name,
            Price = request.Price,
            StockQuantity = request.StockQuantity,
            Category = request.Category
        };

        var created = await _repository.CreateAsync(product);
        _logger.LogInformation("Created product {ProductId}", created.Id);

        return CreatedAtAction(nameof(GetById), new { id = created.Id }, MapToDto(created));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, CreateProductRequest request)
    {
        var existing = await _repository.GetByIdAsync(id);
        if (existing == null)
            return NotFound();

        var oldPrice = existing.Price;

        existing.Name = request.Name;
        existing.Price = request.Price;
        existing.StockQuantity = request.StockQuantity;
        existing.Category = request.Category;

        var updated = await _repository.UpdateAsync(existing);
        if (!updated)
            return StatusCode(500);

        if (Math.Abs(oldPrice - request.Price) > 0.01m)
        {
            await _notifications.SendPriceChangeNotificationAsync(id, oldPrice, request.Price);
        }

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var product = await _repository.GetByIdAsync(id);
        if (product == null)
            return NotFound();

        var deleted = await _repository.DeleteAsync(id);
        if (!deleted)
            return StatusCode(500);

        return NoContent();
    }

    [HttpPost("{id}/reserve")]
    public async Task<IActionResult> ReserveStock(int id, [FromBody] int quantity)
    {
        var product = await _repository.GetByIdAsync(id);
        if (product == null)
            return NotFound();

        var available = await _inventory.CheckStockAsync(id, quantity);
        if (!available)
            return BadRequest("Insufficient stock");

        var reserved = await _inventory.ReserveStockAsync(id, quantity);
        if (!reserved)
            return StatusCode(500);

        var remainingStock = await _inventory.GetAvailableStockAsync(id);
        if (remainingStock < 10)
        {
            await _notifications.SendLowStockAlertAsync(id, remainingStock);
        }

        return Ok();
    }

    private ProductDto MapToDto(Product product)
    {
        var discount = _pricing.CalculateDiscount(product.Price, product.Category);

        return new ProductDto
        {
            Id = product.Id,
            Name = product.Name,
            Price = product.Price - discount,
            StockQuantity = product.StockQuantity,
            Category = product.Category,
            InStock = product.StockQuantity > 0
        };
    }
}
