# Chaos Engineering — Resilience Testing

> **"Prove your retry logic works before production breaks."**

**Industry First:** Skugga is the **only .NET mocking library** with built-in chaos engineering for testing resilience patterns directly in your mocks.

## Quick Start

```csharp
var mock = Mock.Create<IPaymentGateway>();

mock.Chaos(policy => {
   policy.FailureRate = 0.3; // 30% of calls fail
   policy.PossibleExceptions = new[] { 
       new TimeoutException(),
       new HttpRequestException("503")
   };
   policy.Seed = 42; // Reproducible chaos
});

// Test retry logic survives!
for (int i = 0; i < 100; i++) {
   await retryPolicy.ExecuteAsync(() => mock.ProcessPayment(99.99m));
}

// Verify chaos statistics
var stats = mock.GetChaosStatistics();
Console.WriteLine($"Failures injected: {stats.ChaosTriggeredCount}");
```

## Core Features

### Configurable Failure Rates

```csharp
mock.Chaos(policy => {
   policy.FailureRate = 0.0;  // 0% – No chaos (default)
   policy.FailureRate = 0.1;  // 10% – Occasional failures
   policy.FailureRate = 0.5;  // 50% – Heavy chaos
   policy.FailureRate = 1.0;  // 100% – Always fail
});
```

### Latency Injection

```csharp
mock.Chaos(policy => {
   policy.InjectLatency = true;
   policy.MinLatencyMs = 100;
   policy.MaxLatencyMs = 5000; // Up to 5 seconds
});
```

### Reproducible Seeds

```csharp
mock.Chaos(policy => {
   policy.Seed = 42;         // Same seed = same failures
   policy.FailureRate = 0.3;
});
// Run #1: Fails on calls 2, 5, 8
// Run #2: Fails on calls 2, 5, 8 (same!)
```

### Statistics Tracking

```csharp
var stats = mock.GetChaosStatistics();
Console.WriteLine($"Total calls: {stats.TotalCalls}");
Console.WriteLine($"Chaos triggered: {stats.ChaosTriggeredCount}");
Console.WriteLine($"Avg latency: {stats.AverageLatencyMs}ms");
```

## Advanced Scenarios

### Testing Circuit Breakers

```csharp
[Fact]
public async Task CircuitBreaker_OpensAfterConsecutiveFailures()
{
   var mock = Mock.Create<IDatabase>();
   mock.Chaos(policy => {
       policy.FailureRate = 1.0;
       policy.PossibleExceptions = new[] { new TimeoutException() };
   });
   
   var circuitBreaker = Policy
       .Handle<TimeoutException>()
       .CircuitBreakerAsync(3, TimeSpan.FromSeconds(30));

   // Circuit opens after 3 failures
   await Assert.ThrowsAsync<BrokenCircuitException>(
       () => circuitBreaker.ExecuteAsync(
           () => mock.QueryAsync("SELECT * FROM Users"))
   );
}
```

### Per-Method Chaos

```csharp
mock.Chaos(policy => {
   policy.ForMethod("ProcessPayment").FailureRate = 0.5;
   policy.ForMethod("RefundPayment").FailureRate = 0.1;
   policy.ForMethod("GetBalance").InjectLatency = true;
});
```

### Conditional Chaos

```csharp
mock.Chaos(policy => {
   policy.When(call => {
       var amount = call.GetArgument<decimal>("amount");
       return amount > 1000m; // Only fail on large amounts
   }).FailureRate = 0.8;
});
```

## vs Polly + Simmy

While **Polly** + **Simmy** provide chaos for production infrastructure, Skugga brings chaos directly to **test-time mock behavior** — no production chaos agents needed.

[Full Chaos Engineering guide →](https://github.com/Digvijay/Skugga/blob/master/docs/CHAOS_ENGINEERING.md) |  [Demo code →](https://github.com/Digvijay/Skugga/tree/master/samples/ChaosEngineeringDemo)
