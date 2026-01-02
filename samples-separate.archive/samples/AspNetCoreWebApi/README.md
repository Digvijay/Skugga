# AspNetCoreWebApi Sample

A full-featured ASP.NET Core Web API demonstrating real-world usage of Skugga for testing controllers, services, and repositories.

## Overview

This sample shows:
- Testing ASP.NET Core controllers with mocked dependencies
- Mocking repository and service layers
- Mocking ILogger for testing logging behavior
- Using FluentAssertions for readable test assertions
- Testing business logic in service classes
- Complete CRUD operations with inventory management

## Project Structure

```
AspNetCoreWebApi/
├── Controllers/
│   └── ProductsController.cs       # REST API controller
├── Services/
│   ├── IInventoryService.cs        # Business logic interface
│   └── InventoryService.cs         # Business logic implementation
├── Repositories/
│   ├── IProductRepository.cs       # Data access interface
│   └── InMemoryProductRepository.cs # In-memory data store
├── Models/
│   └── Product.cs                  # Domain model
└── AspNetCoreWebApi.Tests/
    ├── Controllers/
    │   └── ProductsControllerTests.cs # Controller tests with Skugga
    └── Services/
        └── InventoryServiceTests.cs   # Service tests with Skugga
```

## Running the Application

```bash
cd samples/AspNetCoreWebApi
dotnet run
```

The API will be available at `https://localhost:5001` with Swagger UI at `https://localhost:5001/swagger`

## Running the Tests

```bash
cd samples/AspNetCoreWebApi.Tests
dotnet test
```

## API Endpoints

- `GET /api/products` - Get all products
- `GET /api/products/{id}` - Get product by ID
- `POST /api/products` - Create new product
- `PUT /api/products/{id}` - Update product
- `DELETE /api/products/{id}` - Delete product
- `GET /api/products/{id}/stock` - Get available stock
- `POST /api/products/{id}/reserve` - Reserve stock

## Key Testing Patterns

### 1. Controller Testing with Multiple Dependencies
```csharp
private readonly Mock<IProductRepository> _repositoryMock;
private readonly Mock<IInventoryService> _inventoryMock;
private readonly Mock<ILogger<ProductsController>> _loggerMock;

public ProductsControllerTests()
{
    _repositoryMock = Mock.Create<IProductRepository>();
    _inventoryMock = Mock.Create<IInventoryService>();
    _loggerMock = Mock.Create<ILogger<ProductsController>>();
    _controller = new ProductsController(_repositoryMock.Object, _inventoryMock.Object, _loggerMock.Object);
}
```

### 2. Testing HTTP Response Types
```csharp
[Fact]
public async Task GetById_WhenProductExists_ReturnsOk()
{
    // Arrange
    _repositoryMock.Setup(x => x.GetByIdAsync(1)).Returns(product);

    // Act
    var result = await _controller.GetById(1);

    // Assert
    result.Result.Should().BeOfType<OkObjectResult>();
}
```

### 3. Service Layer Testing
```csharp
[Fact]
public async Task ReserveStockAsync_WithSufficientStock_ReturnsTrue()
{
    _repositoryMock.Setup(x => x.GetByIdAsync(1)).Returns(product);
    _repositoryMock.Setup(x => x.UpdateAsync(It.IsAny<Product>())).Returns(true);

    var result = await _service.ReserveStockAsync(1, 5);

    result.Should().BeTrue();
    _repositoryMock.Verify(x => x.UpdateAsync(product), Times.Once());
}
```

## Benefits Demonstrated

- **✅ Zero Reflection**: All mocks generated at compile-time
- **✅ AOT Compatible**: Works with Native AOT compilation
- **✅ Real-World Patterns**: Controller → Service → Repository architecture
- **✅ Type-Safe**: Compile-time checking of mock setups
- **✅ Fast Tests**: No runtime proxy generation overhead

## Next Steps

- Check the [MinimalApiAot](../MinimalApiAot/) sample for AOT compilation
- See the [AzureFunctions](../AzureFunctions/) sample for serverless testing
- Explore the [BasicConsoleApp](../BasicConsoleApp/) for simpler examples
