# ASP.NET Core Web API - Moq to Skugga Migration Tutorial

This sample demonstrates migrating an ASP.NET Core Web API application from **Moq** to **Skugga** for AOT-compatible unit testing.

## 📂 Project Structure

```
AspNetCoreWebApi-Moq-To-Skugga-Migration/
├── Step1-WithMoq/              # Original API using Moq (JIT mode)
│   ├── Controllers/
│   │   └── ProductsController.cs    # REST API with 6 endpoints
│   ├── Models/
│   │   └── Product.cs              # Domain models (Product, ProductDto, CreateProductRequest)
│   ├── Services/
│   │   └── IServices.cs            # 4 service interfaces (Repository, Inventory, Pricing, Notifications)
│   └── Step1-WithMoq.csproj        # .NET 10.0, AOT enabled
│
├── Step1-WithMoq.Tests/        # Tests using Moq
│   ├── ProductsControllerTests.cs   # 12 comprehensive controller tests
│   └── Step1-WithMoq.Tests.csproj  # References Moq 4.20.72
│
├── Step2-WithSkugga/           # Migrated API using Skugga (AOT mode)
│   ├── Controllers/
│   │   └── ProductsController.cs    # Same controller code
│   ├── Models/
│   │   └── Product.cs              # Same models
│   ├── Services/
│   │   └── IServices.cs            # Same services
│   └── Step2-WithSkugga.csproj     # .NET 8.0, AOT enabled
│
└── Step2-WithSkugga.Tests/     # Tests using Skugga
    ├── ProductsControllerTests.cs   # 12 tests (98% identical API)
    └── Step2-WithSkugga.Tests.csproj # References Skugga
```

## 🎯 What This Sample Demonstrates

### REST API Features
- **6 HTTP Endpoints**: GET (all, by-id, by-category), POST, PUT, DELETE, ReserveStock
- **4 Service Dependencies**: Repository, Inventory, Pricing, Notifications
- **Complex Business Logic**: Stock validation, price calculations, automatic notifications
- **DTO Mapping**: Converting entities to DTOs with calculated fields
- **Error Handling**: 404 responses, validation, bad request scenarios

### Testing Scenarios
✅ **Happy Paths**: Successful CRUD operations  
✅ **Error Cases**: 404s, validation failures, insufficient stock  
✅ **Argument Matchers**: `It.IsAny<T>()`, `It.Is<T>(predicate)`  
✅ **Verification**: `Times.Once()`, `Times.Never()`  
✅ **Async Methods**: `ReturnsAsync`, `Setup` with async interfaces  
✅ **Callbacks**: Response transformation with lambda functions  

## 🚀 Getting Started

### Prerequisites
- .NET 8.0 SDK or later
- Visual Studio 2022+ or VS Code with C# extension

### Running Step 1 (Moq)
```bash
cd Step1-WithMoq.Tests
dotnet test
```
**Result**: ✅ 12/12 tests pass

### Running Step 2 (Skugga)
```bash
cd Step2-WithSkugga.Tests
dotnet test
```
**Result**: ✅ 12/12 tests pass

## 📋 Migration Guide

### Step 1: Update Project File

**Before (Moq)**:
```xml
<ItemGroup>
  <PackageReference Include="Moq" Version="4.20.72" />
</ItemGroup>
```

**After (Skugga)**:
```xml
<PropertyGroup>
  <TargetFramework>net8.0</TargetFramework>
  <InterceptorsPreviewNamespaces>$(InterceptorsPreviewNamespaces);Skugga.Generated</InterceptorsPreviewNamespaces>
</PropertyGroup>

<ItemGroup>
  <ProjectReference Include="..\..\..\src\Skugga.Core\Skugga.Core.csproj" />
  <ProjectReference Include="..\..\..\src\Skugga.Generator\Skugga.Generator.csproj" 
                    OutputItemType="Analyzer" 
                    ReferenceOutputAssembly="false" />
</ItemGroup>
```

### Step 2: Update Using Statements

**Before (Moq)**:
```csharp
using Moq;
```

**After (Skugga)**:
```csharp
using Skugga.Core;
```

### Step 3: Convert Mock Creation

**Before (Moq)**:
```csharp
var mockRepo = new Mock<IProductRepository>();
var mockInventory = new Mock<IInventoryService>();
var mockPricing = new Mock<IPricingService>();
var mockNotifications = new Mock<INotificationService>();

// Use .Object to get the instance
var controller = new ProductsController(
    mockRepo.Object, 
    mockInventory.Object, 
    mockPricing.Object, 
    mockNotifications.Object,
    mockLogger.Object);
```

**After (Skugga)**:
```csharp
var mockRepo = Mock.Create<IProductRepository>();
var mockInventory = Mock.Create<IInventoryService>();
var mockPricing = Mock.Create<IPricingService>();
var mockNotifications = Mock.Create<INotificationService>();

// Use mocks directly (no .Object needed)
var controller = new ProductsController(
    mockRepo, 
    mockInventory, 
    mockPricing, 
    mockNotifications,
    NullLogger<ProductsController>.Instance);
```

**Key Difference**: Skugga returns the mock directly; Moq requires `.Object`.

### Step 4: Setup and Verify Syntax

The Setup/Verify API is **98% identical**:

**Both Work Identically**:
```csharp
// Setup with Returns
mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(product);
mockPricing.Setup(p => p.CalculateDiscount(It.IsAny<decimal>(), It.IsAny<string>())).Returns(0m);

// Setup with callbacks
mockRepo.Setup(r => r.CreateAsync(It.IsAny<Product>()))
    .ReturnsAsync((Product p) => { p.Id = 3; return p; });

// Verification
mockRepo.Verify(r => r.DeleteAsync(1), Times.Once());
mockInventory.Verify(i => i.ReserveStockAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never());

// Argument matchers
mockPricing.Setup(p => p.CalculateDiscount(
    It.IsAny<decimal>(), 
    It.Is((string cat) => cat == "Electronics")))
    .Returns(100m);
```

### Step 5: Special Cases

#### ILogger Mocking
For `ILogger<T>`, we recommend using `NullLogger<T>.Instance` instead of mocking:

```csharp
using Microsoft.Extensions.Logging.Abstractions;

// Instead of: var mockLogger = Mock.Create<ILogger<ProductsController>>();
var logger = NullLogger<ProductsController>.Instance;
```

This avoids generic constraint complexities and is simpler for logging scenarios.

## 📊 API Comparison

| Feature | Moq | Skugga |
|---------|-----|--------|
| Mock Creation | `new Mock<T>()` | `Mock.Create<T>()` |
| Get Instance | `.Object` | Direct use |
| Setup | `mock.Setup(...)` | `mock.Setup(...)` |
| Verify | `mock.Verify(...)` | `mock.Verify(...)` |
| Argument Matchers | `It.IsAny<T>()`, `It.Is<T>(...)` | `It.IsAny<T>()`, `It.Is<T>(...)` |
| Times | `Times.Once()`, `Times.Never()` | `Times.Once()`, `Times.Never()` |
| Callbacks | `.Callback<T>(x => ...)` | `.Callback((T x) => ...)` |
| AOT Support | ⚠️ Runtime reflection | ✅ Compile-time generation |

## 🏗️ Architecture Highlights

### ProductsController Endpoints

```csharp
[HttpGet]                          // Get all products with discounts
[HttpGet("{id}")]                  // Get product by ID (404 if not found)
[HttpGet("category/{category}")]   // Filter by category
[HttpPost]                         // Create product (validates price)
[HttpPut("{id}")]                  // Update product (sends price change notification)
[HttpDelete("{id}")]               // Delete product
[HttpPost("{id}/reserve")]         // Reserve stock (sends low stock alert)
```

### Service Layer

```csharp
IProductRepository     // CRUD operations (GetByIdAsync, CreateAsync, UpdateAsync, DeleteAsync)
IInventoryService      // Stock management (CheckStockAsync, ReserveStockAsync)
IPricingService        // Pricing logic (CalculateDiscount, ValidatePrice)
INotificationService   // Alerts (SendLowStockAlertAsync, SendPriceChangeNotificationAsync)
```

## 🧪 Test Coverage

All 12 tests cover:
1. `GetAll_ReturnsAllProducts` - List all products
2. `GetById_ExistingId_ReturnsProduct` - Retrieve specific product
3. `GetById_NonExistingId_ReturnsNotFound` - Handle missing product
4. `GetByCategory_ReturnsFilteredProducts` - Category filtering
5. `Create_ValidProduct_ReturnsCreated` - Successful creation
6. `Create_InvalidPrice_ReturnsBadRequest` - Price validation
7. `Update_PriceChanged_SendsNotification` - Update with notification
8. `Update_NonExistingProduct_ReturnsNotFound` - Update missing product
9. `ReserveStock_SufficientStock_SendsLowStockAlert` - Stock reservation
10. `ReserveStock_InsufficientStock_ReturnsBadRequest` - Stock validation
11. `Delete_ExistingProduct_ReturnsNoContent` - Successful deletion
12. `PricingService_CalculatesDifferentDiscountsByCategory` - Advanced argument matching

## 🎓 Key Takeaways

1. **Minimal API Changes**: Only 2-3 lines per test need modification
2. **Same Test Logic**: All business logic and assertions remain identical
3. **AOT Compatible**: Skugga works in Native AOT scenarios where Moq may have limitations
4. **No Runtime Overhead**: Skugga generates code at compile-time (zero reflection)
5. **Familiar API**: If you know Moq, you already know 98% of Skugga

## 🔗 Related Resources

- [Main Skugga Repository](https://github.com/yourusername/Skugga)
- [ConsoleApp Migration Sample](../ConsoleApp-Moq-To-Skugga-Migration/README.md)
- [Skugga Documentation](../../docs/)
- [ASP.NET Core Testing Documentation](https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests)

## 📝 Notes

- This sample uses **NullLogger** instead of mocking `ILogger<T>` for simplicity
- Both Step1 (Moq) and Step2 (Skugga) maintain **identical controller code**
- Test logic is **98% identical** between Moq and Skugga
- The sample demonstrates realistic e-commerce REST API patterns
- All async/await patterns are fully supported in both frameworks

---

**Ready to migrate your ASP.NET Core API tests?** Start with Step1, verify it works, then migrate to Step2 using this guide! 🚀
