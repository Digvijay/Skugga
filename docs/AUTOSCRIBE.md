# AutoScribe - Self-Writing Test Code

> **"Stop writing mock setup code. Let AutoScribe record it for you."**

## The Problem

Writing mock setup code is tedious and error-prone:

```csharp
// Manually writing 9 mock dependencies = 15+ minutes
var orderRepoMock = new Mock<IOrderRepository>();
orderRepoMock.Setup(x => x.GetOrder(12345))
    .Returns(new Order { Id = 12345, Total = 99.99m, Status = "Pending" });
orderRepoMock.Setup(x => x.UpdateStatus(12345, "Shipped"))
    .Returns(true);

var inventoryMock = new Mock<IInventoryService>();
inventoryMock.Setup(x => x.CheckStock("WIDGET-001"))
    .Returns(new StockInfo { Available = 50, Reserved = 10 });

var shippingMock = new Mock<IShippingService>();
// ... 6 more mocks to setup manually! 😱
```

**Problems:**
- ❌ Time-consuming (15+ minutes per test)
- ❌ Error-prone (wrong return values, types)
- ❌ Hard to maintain when APIs change
- ❌ Guessing realistic test data

---

## The Solution: AutoScribe

AutoScribe **records real interactions** and generates the mock setup code for you:

```csharp
// 1. Wrap your real service with AutoScribe
var recorder = AutoScribe.Capture<IOrderRepository>(new RealOrderRepository());

// 2. Exercise your code manually or run your app
var order = recorder.GetOrder(12345);
recorder.UpdateStatus(12345, "Shipped");

// 3. AutoScribe generates the code to your console:
// [AutoScribe] mock.Setup(x => x.GetOrder(12345)).Returns(new Order { Id = 12345, Total = 99.99m, Status = "Pending" });
// [AutoScribe] mock.Setup(x => x.UpdateStatus(12345, "Shipped")).Returns(true);

// 4. Copy/paste into your test - done! ✅
```

**Time saved: 15 minutes → 30 seconds**

---

## Quick Start

### Step 1: Install Skugga

```bash
dotnet add package Skugga
```

### Step 2: Wrap Your Service

```csharp
using Skugga.Core;

// Wrap real implementation
var recorder = AutoScribe.Capture<IPaymentGateway>(new StripePaymentGateway());
```

### Step 3: Exercise the Code

```csharp
// Call methods as you normally would
var payment = recorder.CreatePayment(new PaymentRequest 
{
    Amount = 99.99m,
    Currency = "USD"
});

var status = recorder.GetPaymentStatus(payment.Id);
```

### Step 4: Get Generated Code

AutoScribe outputs to your console:

```
[AutoScribe] mock.Setup(x => x.CreatePayment(It.Is<PaymentRequest>(r => r.Amount == 99.99m && r.Currency == "USD")))
    .Returns(new Payment { Id = "pay_123", Status = "pending", Amount = 99.99m });

[AutoScribe] mock.Setup(x => x.GetPaymentStatus("pay_123"))
    .Returns("completed");
```

### Step 5: Copy to Your Test

```csharp
[Fact]
public async Task ProcessOrder_WithValidPayment_CompletesSuccessfully()
{
    // Arrange - paste AutoScribe code
    var mock = Mock.Create<IPaymentGateway>();
    mock.Setup(x => x.CreatePayment(It.Is<PaymentRequest>(r => r.Amount == 99.99m)))
        .Returns(new Payment { Id = "pay_123", Status = "pending" });
    
    var service = new OrderService(mock);
    
    // Act
    var result = await service.ProcessOrder(new Order { Total = 99.99m });
    
    // Assert
    Assert.Equal("Completed", result.Status);
}
```

---

## Advanced Features

### Recording Complex Objects

AutoScribe handles nested objects, collections, and complex types:

```csharp
var recorder = AutoScribe.Capture<IOrderRepository>(new RealOrderRepository());

var order = recorder.GetOrderWithItems(12345);
// Captures:
// new Order 
// { 
//     Id = 12345, 
//     Items = new[] 
//     { 
//         new OrderItem { ProductId = "P001", Quantity = 2, Price = 19.99m },
//         new OrderItem { ProductId = "P002", Quantity = 1, Price = 59.99m }
//     }
// }
```

### Recording Async Methods

Works seamlessly with `async`/`await`:

```csharp
var recorder = AutoScribe.Capture<IEmailService>(new SendGridEmailService());

await recorder.SendEmailAsync("user@example.com", "Welcome!", "Hello!");
// [AutoScribe] mock.Setup(x => x.SendEmailAsync("user@example.com", "Welcome!", "Hello!"))
//     .ReturnsAsync(true);
```

### Recording Multiple Calls

Captures sequences of interactions:

```csharp
var recorder = AutoScribe.Capture<ICache>(new RedisCache());

recorder.Set("key1", "value1");
recorder.Set("key2", "value2");
var val = recorder.Get("key1");

// Generates setup for all 3 calls
```

### Filtering Recorded Calls

Only record specific methods:

```csharp
var recorder = AutoScribe.Capture<IRepository>(new RealRepository(), options =>
{
    options.IncludeMethods = new[] { "GetById", "Update" };
    options.ExcludeMethods = new[] { "Log", "Audit" };
});
```

---

## Configuration Options

### Output Format

Choose how AutoScribe outputs the generated code:

```csharp
var recorder = AutoScribe.Capture<IService>(new RealService(), options =>
{
    // Console output (default)
    options.OutputTo = AutoScribeOutput.Console;
    
    // File output
    options.OutputTo = AutoScribeOutput.File;
    options.OutputPath = "GeneratedMocks.cs";
    
    // In-memory (access via recorder.GeneratedCode)
    options.OutputTo = AutoScribeOutput.InMemory;
});

// Access generated code
string code = recorder.GetGeneratedCode();
```

### Argument Matching

Control how arguments are matched:

```csharp
var recorder = AutoScribe.Capture<IService>(new RealService(), options =>
{
    // Exact value matching (default)
    options.ArgumentMatching = ArgumentMatchMode.Exact;
    
    // Use It.IsAny<T>() for flexibility
    options.ArgumentMatching = ArgumentMatchMode.Any;
    
    // Smart matching (It.Is<T> with predicates)
    options.ArgumentMatching = ArgumentMatchMode.Smart;
});
```

### Return Value Formatting

Customize how return values are serialized:

```csharp
var recorder = AutoScribe.Capture<IService>(new RealService(), options =>
{
    // Compact (single line)
    options.FormatMode = CodeFormatMode.Compact;
    
    // Pretty (multi-line with indentation)
    options.FormatMode = CodeFormatMode.Pretty;
});
```

---

## Real-World Example

### Before AutoScribe (Manual Setup)

**Time: 15 minutes** | **Lines: 50+** | **Errors: Common**

```csharp
[Fact]
public async Task CompleteCheckout_WithValidOrder_ProcessesPaymentAndShipsOrder()
{
    // Manual setup of 9 dependencies... 😰
    var orderRepoMock = new Mock<IOrderRepository>();
    orderRepoMock.Setup(x => x.GetOrder(12345))
        .Returns(new Order { /* guessing these values */ });
    
    var inventoryMock = new Mock<IInventoryService>();
    inventoryMock.Setup(x => x.CheckStock(It.IsAny<string>()))
        .Returns(new StockInfo { /* more guesses */ });
    
    var paymentMock = new Mock<IPaymentGateway>();
    // ... 6 more mocks!
    
    // Act & Assert
    // ...
}
```

### After AutoScribe

**Time: 30 seconds** | **Lines: Generated** | **Errors: Zero**

```csharp
// Step 1: Record real interactions
var recorder = AutoScribe.CaptureAll(new 
{
    Orders = new RealOrderRepository(),
    Inventory = new RealInventoryService(),
    Payment = new StripePaymentGateway(),
    // ... all 9 services
});

// Step 2: Exercise the real code
await CheckoutService.CompleteCheckout(12345);

// Step 3: Copy generated code to test
[Fact]
public async Task CompleteCheckout_WithValidOrder_ProcessesPaymentAndShipsOrder()
{
    // Arrange - paste AutoScribe code (accurate, real data)
    var orderMock = Mock.Create<IOrderRepository>();
    orderMock.Setup(x => x.GetOrder(12345))
        .Returns(new Order { Id = 12345, Total = 99.99m, Status = "Pending" });
    
    // ... all setups generated accurately
    
    // Act & Assert
    var result = await CheckoutService.CompleteCheckout(12345);
    Assert.Equal("Completed", result.Status);
}
```

---

## Demo and Example Code

See AutoScribe in action with a complete example:

**[→ View Demo and Example Code](../samples/AutoScribeDemo)**

The demo shows:
- ✅ Complex 9-dependency controller
- ✅ Before/after comparison (15 min → 30 sec)
- ✅ Side-by-side code comparison
- ✅ Real vs guessed data accuracy

---

## Best Practices

### 1. Use AutoScribe for Integration Test Setup

Perfect for recording interactions with real databases, APIs, or services:

```csharp
// Record real database interactions
var recorder = AutoScribe.Capture<IOrderRepository>(
    new EntityFrameworkOrderRepository(realDbContext)
);

// Exercise real database
var orders = recorder.GetActiveOrders();
recorder.UpdateOrderStatus(12345, "Shipped");

// Use recorded data in unit tests
```

### 2. Record Happy Paths, Then Modify

Let AutoScribe handle the tedious setup, then customize for edge cases:

```csharp
// AutoScribe generates:
mock.Setup(x => x.ProcessPayment(It.IsAny<decimal>()))
    .Returns(new PaymentResult { Success = true });

// You modify for edge case:
mock.Setup(x => x.ProcessPayment(0))
    .Returns(new PaymentResult { Success = false, Error = "Invalid amount" });
```

### 3. Use in Development, Not CI/CD

AutoScribe is a development tool. Don't run it in automated tests - use the generated code instead.

### 4. Version Control Generated Code

Commit the generated mock setup code to your repo so your team can review it.

---

## Troubleshooting

### Issue: "No output generated"

**Solution:** Ensure you're calling methods on the recorder, not the original object:

```csharp
// ❌ Wrong - calls original
var service = new RealService();
var recorder = AutoScribe.Capture<IService>(service);
service.DoSomething(); // Not recorded!

// ✅ Correct - calls recorder
var recorder = AutoScribe.Capture<IService>(new RealService());
recorder.DoSomething(); // Recorded ✓
```

### Issue: "Complex objects not serialized correctly"

**Solution:** Add `[AutoScribeSerializable]` to your DTOs or use custom serializers:

```csharp
[AutoScribeSerializable]
public class Payment
{
    public string Id { get; set; }
    public decimal Amount { get; set; }
}
```

### Issue: "Too much output"

**Solution:** Filter methods or disable verbose mode:

```csharp
var recorder = AutoScribe.Capture<IService>(service, options =>
{
    options.ExcludeMethods = new[] { "Log", "Trace", "Debug" };
    options.VerboseOutput = false;
});
```

---

## Comparison with Manual Mocking

| Aspect | Manual Mocking | AutoScribe |
|--------|---------------|------------|
| **Time to Setup** | 15+ minutes | 30 seconds |
| **Lines of Code** | 50+ lines | Copy/paste |
| **Data Accuracy** | Guessed values | Real data |
| **Maintenance** | Update manually | Re-record |
| **Learning Curve** | High | Low |
| **Error Rate** | High | Zero |

---

## Limitations

- **Not for Production**: AutoScribe is a development tool, not runtime mocking
- **Single-Threaded**: Records one interaction at a time (no concurrent calls)
- **Serialization**: Some complex types may need custom serializers
- **Side Effects**: Be careful when recording methods with side effects (email sends, payments, etc.)

---

## FAQ

**Q: Can I use AutoScribe in production?**  
A: No, AutoScribe is for development only. Use the generated mock code in your tests.

**Q: Does AutoScribe work with sealed classes?**  
A: No, AutoScribe requires an interface. Extract an interface from sealed classes first.

**Q: Can I record from a real APIs?**  
A: Yes! You can attach AutoScribe to a running service and record production-like interactions (be careful with side effects).

**Q: Is the generated code compatible with Moq?**  
A: Yes! AutoScribe generates standard Moq-style `mock.Setup()` calls that work with Skugga and Moq.

---

## Related Features

- **[Doppelgänger](DOPPELGANGER.md)** - Generate mocks from OpenAPI specs (no recording needed)
- **[API Reference](API_REFERENCE.md#autoscribe)** - Complete AutoScribe API documentation

---

**Built with ❤️ by [Digvijay](https://github.com/Digvijay) | Contributions welcome!**

*Stop writing mocks. Start recording them.*
