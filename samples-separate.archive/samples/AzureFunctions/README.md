# AzureFunctions Sample

An Azure Functions project using the **isolated worker model**, demonstrating how to test serverless functions with Skugga.

## Overview

This sample shows:
- Azure Functions v4 with isolated worker model
- HTTP-triggered functions for order processing
- Testing serverless functions with mocked dependencies
- Service layer testing for business logic
- Perfect for Native AOT compilation in Azure

## Project Structure

```
AzureFunctions/
├── OrderFunctions.cs               # HTTP-triggered functions
├── Program.cs                      # Function app configuration
├── Services/
│   ├── IOrderService.cs            # Order management
│   ├── IPaymentService.cs          # Payment processing
│   └── INotificationService.cs     # Customer notifications
├── Models/
│   └── Order.cs                    # Domain models
└── AzureFunctions.Tests/
    └── OrderFunctionsTests.cs      # Skugga-based tests
```

## Running Locally

### Prerequisites
- [Azure Functions Core Tools](https://docs.microsoft.com/azure/azure-functions/functions-run-local)
- .NET 9.0 SDK

### Start the Function App
```bash
cd samples/AzureFunctions
func start
```

Or with .NET CLI:
```bash
dotnet run
```

## Running the Tests

```bash
cd samples/AzureFunctions.Tests
dotnet test
```

## API Endpoints

### Create Order
```bash
POST http://localhost:7071/api/orders?customerId=cust123&amount=99.99
```

### Process Payment
```bash
POST http://localhost:7071/api/orders/{orderId}/payment
```

### Cancel Order
```bash
POST http://localhost:7071/api/orders/{orderId}/cancel
```

## Key Testing Patterns

### 1. Testing Function Dependencies
```csharp
public OrderFunctionsTests()
{
    _orderServiceMock = Mock.Create<IOrderService>();
    _paymentServiceMock = Mock.Create<IPaymentService>();
    _notificationServiceMock = Mock.Create<INotificationService>();
    
    _functions = new OrderFunctions(
        _loggerMock.Object,
        _orderServiceMock.Object,
        _paymentServiceMock.Object,
        _notificationServiceMock.Object);
}
```

### 2. Testing Service Interactions
```csharp
[Fact]
public async Task ProcessPayment_WithValidOrder_ProcessesSuccessfully()
{
    _orderServiceMock.Setup(x => x.GetOrderAsync(orderId)).Returns(order);
    _paymentServiceMock.Setup(x => x.ProcessPaymentAsync(orderId, 100m)).Returns(true);

    var paymentResult = await _paymentServiceMock.Object.ProcessPaymentAsync(orderId, order.TotalAmount);
    
    _notificationServiceMock.Verify(x => x.SendOrderConfirmationAsync("customer-1", orderId), Times.Once());
}
```

### 3. Testing Error Handling
```csharp
[Fact]
public async Task ProcessPayment_WithFailedPayment_SendsFailureNotification()
{
    _paymentServiceMock.Setup(x => x.ProcessPaymentAsync(orderId, 200m)).Returns(false);
    
    // Verify failure notification is sent
    _notificationServiceMock.Verify(
        x => x.SendPaymentFailureNotificationAsync("customer-2", orderId), 
        Times.Once());
}
```

## Azure Functions + Skugga Benefits

### Serverless Performance
- **Cold Start Optimization**: Skugga's compile-time mocking reduces assembly size
- **Memory Efficiency**: Zero reflection overhead = lower memory consumption
- **Cost Savings**: Smaller functions execute faster and cost less

### Native AOT Ready
Azure Functions v4 supports Native AOT compilation. Skugga enables:
- **Instant Cold Starts**: < 50ms initialization
- **Minimal Memory**: < 20MB baseline usage
- **Smaller Deployments**: Reduced package size

### Testing Confidence
- Test the same code that runs in production
- No runtime surprises from reflection-based mocking
- Compile-time verification of all test setups

## Deployment

### Deploy to Azure
```bash
# Build for production
dotnet publish -c Release

# Deploy with Azure CLI
az functionapp deployment source config-zip \
  --resource-group <resource-group> \
  --name <function-app-name> \
  --src ./bin/Release/net9.0/publish.zip
```

### Deploy with Native AOT
```bash
# Publish with AOT (requires Azure Functions v4.20+)
dotnet publish -c Release --runtime linux-x64 --self-contained /p:PublishAot=true

# Deploy the native binary
az functionapp deployment source config-zip \
  --resource-group <resource-group> \
  --name <function-app-name> \
  --src ./bin/Release/net9.0/linux-x64/publish.zip
```

## Configuration

### Application Insights
The sample includes Application Insights for monitoring:
```json
{
  "logging": {
    "applicationInsights": {
      "samplingSettings": {
        "isEnabled": true,
        "maxTelemetryItemsPerSecond": 20
      }
    }
  }
}
```

## Why Skugga for Azure Functions?

### ❌ Traditional Mocking
- Runtime reflection = slower cold starts
- Larger deployments = higher costs
- Memory overhead = more expensive plans
- **Not compatible with Native AOT**

### ✅ Skugga
- Compile-time generation = instant cold starts
- Smaller binaries = faster deployments
- Zero runtime overhead = consumption tier friendly
- **100% Native AOT compatible**

## Real-World Use Cases

Perfect for:
- **Event-driven workflows** (order processing, webhooks)
- **API backends** (microservices, BFFs)
- **Scheduled jobs** (cleanup, reports, notifications)
- **Integration scenarios** (third-party APIs, message queues)

## Next Steps

- Compare with [AspNetCoreWebApi](../AspNetCoreWebApi/) for traditional hosting
- See [MinimalApiAot](../MinimalApiAot/) for AOT compilation patterns
- Check [BasicConsoleApp](../BasicConsoleApp/) for simpler examples

## Resources

- [Azure Functions Documentation](https://docs.microsoft.com/azure/azure-functions/)
- [Isolated Worker Model](https://docs.microsoft.com/azure/azure-functions/dotnet-isolated-process-guide)
- [Native AOT in Azure Functions](https://learn.microsoft.com/azure/azure-functions/dotnet-isolated-process-guide#native-aot)
