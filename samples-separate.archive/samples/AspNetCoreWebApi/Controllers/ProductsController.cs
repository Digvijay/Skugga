using AspNetCoreWebApi.Models;
using AspNetCoreWebApi.Repositories;
using AspNetCoreWebApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace AspNetCoreWebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IProductRepository _repository;
    private readonly IInventoryService _inventoryService;
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(
        IProductRepository repository,
        IInventoryService inventoryService,
        ILogger<ProductsController> logger)
    {
        _repository = repository;
        _inventoryService = inventoryService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Product>>> GetAll()
    {
        var products = await _repository.GetAllAsync();
        return Ok(products);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Product>> GetById(int id)
    {
        var product = await _repository.GetByIdAsync(id);
        if (product == null)
        {
            _logger.LogWarning("Product {ProductId} not found", id);
            return NotFound();
        }

        return Ok(product);
    }

    [HttpPost]
    public async Task<ActionResult<Product>> Create([FromBody] Product product)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var created = await _repository.CreateAsync(product);
        _logger.LogInformation("Product {ProductId} created", created.Id);

        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] Product product)
    {
        if (id != product.Id)
            return BadRequest("ID mismatch");

        var exists = await _repository.ExistsAsync(id);
        if (!exists)
            return NotFound();

        var updated = await _repository.UpdateAsync(product);
        if (!updated)
            return StatusCode(500, "Failed to update product");

        _logger.LogInformation("Product {ProductId} updated", id);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _repository.DeleteAsync(id);
        if (!deleted)
            return NotFound();

        _logger.LogInformation("Product {ProductId} deleted", id);
        return NoContent();
    }

    [HttpGet("{id}/stock")]
    public async Task<ActionResult<int>> GetStock(int id)
    {
        var exists = await _repository.ExistsAsync(id);
        if (!exists)
            return NotFound();

        var quantity = await _inventoryService.GetAvailableQuantityAsync(id);
        return Ok(new { productId = id, availableQuantity = quantity });
    }

    [HttpPost("{id}/reserve")]
    public async Task<IActionResult> ReserveStock(int id, [FromBody] ReserveStockRequest request)
    {
        var reserved = await _inventoryService.ReserveStockAsync(id, request.Quantity);
        if (!reserved)
        {
            _logger.LogWarning("Failed to reserve {Quantity} units of product {ProductId}", request.Quantity, id);
            return BadRequest("Insufficient stock or product not found");
        }

        _logger.LogInformation("Reserved {Quantity} units of product {ProductId}", request.Quantity, id);
        return Ok(new { message = $"Reserved {request.Quantity} units" });
    }
}

public record ReserveStockRequest(int Quantity);
