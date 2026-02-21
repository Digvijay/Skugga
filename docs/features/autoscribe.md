# AutoScribe — Self-Writing Tests

> **"Stop writing mock setup code. Let AutoScribe record it for you."**

## The Problem

Writing mock setup code for complex services is tedious and error-prone. A controller with 9 dependencies can take **15+ minutes** of manual mock setup.

## The Solution

AutoScribe **records real interactions** and generates the mock setup code:

```csharp
// 1. Wrap your real service
var recorder = AutoScribe.Capture<IOrderRepository>(new RealOrderRepository());

// 2. Exercise your code
var order = recorder.GetOrder(12345);
recorder.UpdateStatus(12345, "Shipped");

// 3. AutoScribe generates the code:
// [AutoScribe] mock.Setup(x => x.GetOrder(12345))
//     .Returns(new Order { Id = 12345, Status = "Pending" });
// [AutoScribe] mock.Setup(x => x.UpdateStatus(12345, "Shipped"))
//     .Returns(true);

// 4. Copy/paste into your test — done!
```

**Time saved: 15 minutes → 30 seconds**

## Features

### Recording Complex Objects

```csharp
var recorder = AutoScribe.Capture<IOrderRepository>(new RealOrderRepository());
var order = recorder.GetOrderWithItems(12345);
// Captures nested objects, collections, complex types
```

### Async Methods

```csharp
var recorder = AutoScribe.Capture<IEmailService>(new SendGridEmailService());
await recorder.SendEmailAsync("user@example.com", "Welcome!", "Hello!");
// [AutoScribe] mock.Setup(x => x.SendEmailAsync(...)).ReturnsAsync(true);
```

### Output Configuration

```csharp
var recorder = AutoScribe.Capture<IService>(new RealService(), options =>
{
   options.OutputTo = AutoScribeOutput.Console;  // or File, InMemory
   options.OutputPath = "GeneratedMocks.cs";
   options.ArgumentMatching = ArgumentMatchMode.Smart;
   options.FormatMode = CodeFormatMode.Pretty;
});
```

### Filtering

```csharp
var recorder = AutoScribe.Capture<IRepository>(new RealRepository(), options =>
{
   options.IncludeMethods = new[] { "GetById", "Update" };
   options.ExcludeMethods = new[] { "Log", "Audit" };
});
```

## Impact

| Aspect | Manual Mocking | AutoScribe |
|--------|---------------|------------|
| **Time to Setup** | 15+ minutes | 30 seconds |
| **Lines of Code** | 50+ lines | Copy/paste |
| **Data Accuracy** | Guessed values | Real data |
| **Maintenance** | Update manually | Re-record |
| **Error Rate** | High | Zero |

[Full AutoScribe guide →](https://github.com/Digvijay/Skugga/blob/master/docs/AUTOSCRIBE.md) |  [Demo code →](https://github.com/Digvijay/Skugga/tree/master/samples/AutoScribeDemo)
