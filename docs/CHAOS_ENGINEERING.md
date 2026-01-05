# Chaos Engineering - Resilience Testing

> **"Prove your retry logic works before production breaks."**

**Industry First:** Skugga is the **only .NET mocking library** with built-in chaos engineering for testing resilience patterns directly in your mocks.

## The Problem

You've implemented retry logic with Polly, but how do you know it actually works?

```csharp
// Your retry policy looks good...
var retryPolicy = Policy
    .Handle<HttpRequestException>()
    .WaitAndRetryAsync(3, retryAttempt => 
        TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

// But does it actually handle failures? 🤷
await retryPolicy.ExecuteAsync(() => api.CallExternalService());
```

**Problems:**
- ❌ Can't test retry logic without real failures
- ❌ Hard to reproduce edge cases (timeouts, 503s, network blips)
- ❌ Tests always pass (mocks never fail)
- ❌ Production failures surprise you

---

## The Solution: Built-In Chaos

Inject controlled chaos directly into your mocks:

```csharp
var mock = Mock.Create<IPaymentGateway>();

// 30% of calls will fail with realistic exceptions
mock.Chaos(policy => {
    policy.FailureRate = 0.3;
    policy.PossibleExceptions = new[] { 
        new TimeoutException(),
        new HttpRequestException("503 Service Unavailable")
    };
});

// Now test your retry logic!
for (int i = 0; i < 100; i++) {
    await retryPolicy.ExecuteAsync(() => mock.ProcessPayment(99.99m));
}

// Verify chaos was handled
var stats = mock.GetChaosStatistics();
Console.WriteLine($"Failures injected: {stats.ChaosTriggeredCount}");
Console.WriteLine($"Successfully handled: {stats.SuccessfulRetries}");
```

---

## Quick Start

### Step 1: Install Skugga

```bash
dotnet add package Skugga
```

### Step 2: Enable Chaos on Your Mock

```csharp
using Skugga.Core;

var mock = Mock.Create<IExternalApi>();

// Configure chaos behavior
mock.Chaos(policy => {
    policy.FailureRate = 0.5; // 50% failure rate
    policy.PossibleExceptions = new[] { 
        new TimeoutException("Service timeout") 
    };
});
```

### Step 3: Test Your Resilience

```csharp
[Fact]
public async Task RetryPolicy_HandlesTimeouts()
{
    // Arrange
    var mock = Mock.Create<IExternalApi>();
    mock.Chaos(policy => {
        policy.FailureRate = 0.7; // 70% failures
        policy.PossibleExceptions = new[] { new TimeoutException() };
        policy.Seed = 42; // Reproducible
    });
    
    var retryPolicy = Policy
        .Handle<TimeoutException>()
        .WaitAndRetryAsync(3, _ => TimeSpan.FromMilliseconds(10));
    
    // Act - should succeed despite 70% failure rate
    var result = await retryPolicy.ExecuteAsync(() => 
        mock.GetDataAsync("key"));
    
    // Assert
    Assert.NotNull(result);
    var stats = mock.GetChaosStatistics();
    Assert.True(stats.ChaosTriggeredCount > 0); // Chaos happened
}
```

---

## Core Features

### 1. Configurable Failure Rates

Control the percentage of calls that fail:

```csharp
mock.Chaos(policy => {
    policy.FailureRate = 0.0;  // 0% - No chaos (default)
    policy.FailureRate = 0.1;  // 10% - Occasional failures
    policy.FailureRate = 0.5;  // 50% - Heavy chaos
    policy.FailureRate = 1.0;  // 100% - Always fails
});
```

### 2. Multiple Exception Types

Test different failure scenarios:

```csharp
mock.Chaos(policy => {
    policy.PossibleExceptions = new Exception[] 
    {
        new TimeoutException("Connection timeout"),
        new HttpRequestException("503 Service Unavailable"),
        new SocketException(),
        new TaskCanceledException("Request cancelled")
    };
});

// Chaos randomly picks from these exceptions
```

### 3. Latency Injection

Simulate slow responses:

```csharp
mock.Chaos(policy => {
    policy.InjectLatency = true;
    policy.MinLatencyMs = 100;  // 100ms minimum
    policy.MaxLatencyMs = 5000; // 5 second maximum
});

// Calls will randomly take 100ms-5s
var start = DateTime.UtcNow;
await mock.CallAsync();
var elapsed = DateTime.UtcNow - start;
Console.WriteLine($"Took {elapsed.TotalMilliseconds}ms");
```

### 4. Reproducible Chaos

Use seeds for deterministic failures:

```csharp
mock.Chaos(policy => {
    policy.Seed = 42; // Same seed = same failures
    policy.FailureRate = 0.3;
});

// Run #1: Fails on calls 2, 5, 8
// Run #2: Fails on calls 2, 5, 8 (same!)
// Run #3 (different seed): Fails on calls 1, 4, 9
```

Perfect for CI/CD - failures are consistent across test runs.

### 5. Statistics Tracking

Monitor chaos impact:

```csharp
var mock = Mock.Create<IApi>();
mock.Chaos(policy => { 
    policy.FailureRate = 0.4;
    policy.TrackStatistics = true; 
});

// Exercise the mock
for (int i = 0; i < 100; i++) {
    try {
        await mock.CallAsync();
    } catch { }
}

// Get statistics
var stats = mock.GetChaosStatistics();
Console.WriteLine($"Total calls: {stats.TotalCalls}");
Console.WriteLine($"Chaos triggered: {stats.ChaosTriggeredCount}");
Console.WriteLine($"Success rate: {stats.SuccessRate:P}");
Console.WriteLine($"Avg latency: {stats.AverageLatencyMs}ms");
```

---

## Advanced Scenarios

### Testing Circuit Breakers

Verify circuit breakers open after failures:

```csharp
[Fact]
public async Task CircuitBreaker_OpensAfterConsecutiveFailures()
{
    // Arrange
    var mock = Mock.Create<IDatabase>();
    mock.Chaos(policy => {
        policy.FailureRate = 1.0; // Always fail
        policy.PossibleExceptions = new[] { new TimeoutException() };
    });
    
    var circuitBreaker = Policy
        .Handle<TimeoutException>()
        .CircuitBreakerAsync(
            exceptionsAllowedBeforeBreaking: 3,
            durationOfBreak: TimeSpan.FromSeconds(10)
        );
    
    // Act - trigger circuit breaker
    for (int i = 0; i < 3; i++) {
        await Assert.ThrowsAsync<TimeoutException>(
            () => circuitBreaker.ExecuteAsync(() => mock.QueryAsync("SELECT * FROM Users"))
        );
    }
    
    // Circuit should be open now
    await Assert.ThrowsAsync<BrokenCircuitException>(
        () => circuitBreaker.ExecuteAsync(() => mock.QueryAsync("SELECT * FROM Users"))
    );
}
```

### Testing Fallback Mechanisms

Ensure fallbacks activate on failures:

```csharp
[Fact]
public async Task Fallback_ActivatesOnTimeout()
{
    // Arrange
    var primaryMock = Mock.Create<IPrimaryApi>();
    primaryMock.Chaos(policy => {
        policy.FailureRate = 1.0; // Always fail
        policy.PossibleExceptions = new[] { new TimeoutException() };
    });
    
    var fallbackMock = Mock.Create<IFallbackCache>();
    fallbackMock.Setup(x => x.GetCachedData("key"))
        .Returns("cached_value");
    
    var fallbackPolicy = Policy<string>
        .Handle<TimeoutException>()
        .FallbackAsync("cached_value", 
            async (result, context) => await fallbackMock.GetCachedData("key"));
    
    // Act
    var result = await fallbackPolicy.ExecuteAsync(() => 
        primaryMock.GetDataAsync("key"));
    
    // Assert
    Assert.Equal("cached_value", result);
    fallbackMock.Verify(x => x.GetCachedData("key"), Times.Once);
}
```

### Testing Bulkhead Isolation

Verify bulkheads prevent cascading failures:

```csharp
[Fact]
public async Task Bulkhead_LimitsParallelExecution()
{
    // Arrange
    var mock = Mock.Create<ISlowService>();
    mock.Chaos(policy => {
        policy.InjectLatency = true;
        policy.MinLatencyMs = 1000; // 1 second per call
        policy.MaxLatencyMs = 1000;
    });
    
    var bulkhead = Policy.BulkheadAsync(
        maxParallelization: 5,
        maxQueuingActions: 10
    );
    
    // Act - try to execute 20 tasks
    var tasks = Enumerable.Range(0, 20)
        .Select(i => bulkhead.ExecuteAsync(() => mock.ProcessAsync(i)))
        .ToList();
    
    // Some will be rejected due to bulkhead limit
    var results = await Task.WhenAll(
        tasks.Select(async t => {
            try { await t; return true; }
            catch (BulkheadRejectedException) { return false; }
        })
    );
    
    var accepted = results.Count(r => r);
    var rejected = results.Count(r => !r);
    
    Assert.Equal(15, accepted); // 5 parallel + 10 queued
    Assert.Equal(5, rejected);
}
```

### Testing Hedging Strategies

Test parallel requests with fastest-wins:

```csharp
[Fact]
public async Task Hedging_UsesFirstSuccessfulResponse()
{
    // Arrange
    var slowMock = Mock.Create<ISlowApi>();
    slowMock.Chaos(policy => {
        policy.InjectLatency = true;
        policy.MinLatencyMs = 5000; // Very slow
    });
    
    var fastMock = Mock.Create<IFastApi>();
    fastMock.Setup(x => x.GetDataAsync())
        .ReturnsAsync("fast_result");
    
    // Act - race both services
    var slowTask = slowMock.GetDataAsync();
    var fastTask = fastMock.GetDataAsync();
    
    var winner = await Task.WhenAny(slowTask, fastTask);
    var result = await winner;
    
    // Assert - fast service won
    Assert.Equal("fast_result", result);
}
```

---

## Chaos Policies

### Global Chaos Configuration

Apply chaos to all mocks:

```csharp
ChaosConfiguration.Global(policy => {
    policy.FailureRate = 0.1; // 10% failures globally
    policy.Seed = 42;
});

// All mocks now have 10% chaos
var mock1 = Mock.Create<IService1>();
var mock2 = Mock.Create<IService2>();
```

### Per-Method Chaos

Different chaos for different methods:

```csharp
var mock = Mock.Create<IPaymentGateway>();

mock.Chaos(policy => {
    policy.ForMethod("ProcessPayment").FailureRate = 0.5;
    policy.ForMethod("RefundPayment").FailureRate = 0.1;
    policy.ForMethod("GetBalance").InjectLatency = true;
});
```

### Conditional Chaos

Chaos based on arguments:

```csharp
mock.Chaos(policy => {
    policy.When(call => {
        var amount = call.GetArgument<decimal>("amount");
        return amount > 1000m; // Only fail on large amounts
    }).FailureRate = 0.8;
});
```

### Time-Based Chaos

Chaos during specific time windows:

```csharp
mock.Chaos(policy => {
    policy.ScheduleChaos(
        startTime: TimeSpan.FromHours(9),  // 9 AM
        endTime: TimeSpan.FromHours(17),   // 5 PM
        failureRate: 0.3
    );
});
```

---

## Demo and Example Code

See Chaos Engineering in action:

**[→ View Demo and Example Code](../samples/ChaosEngineeringDemo)**

The demo shows:
- ✅ Testing retry policies with chaos
- ✅ Circuit breaker activation
- ✅ Fallback mechanism testing
- ✅ Statistics and reproducibility

---

## Best Practices

### 1. Start with Low Failure Rates

Begin with 10% chaos and increase gradually:

```csharp
// ✅ Good - start gentle
policy.FailureRate = 0.1;

// ❌ Bad - too aggressive initially
policy.FailureRate = 0.9;
```

### 2. Use Seeds in CI/CD

Make test failures reproducible:

```csharp
mock.Chaos(policy => {
    policy.Seed = Environment.GetEnvironmentVariable("CHAOS_SEED") 
        ?? DateTime.UtcNow.Ticks;
    policy.FailureRate = 0.3;
});
```

### 3. Test Realistic Exception Types

Use exceptions your code actually encounters:

```csharp
// ✅ Good - realistic exceptions
policy.PossibleExceptions = new[] {
    new HttpRequestException("503"),
    new TimeoutException(),
    new SocketException()
};

// ❌ Bad - unrealistic
policy.PossibleExceptions = new[] {
    new FileNotFoundException() // Your API doesn't throw this!
};
```

### 4. Monitor Statistics

Always check chaos was actually triggered:

```csharp
var stats = mock.GetChaosStatistics();
Assert.True(stats.ChaosTriggeredCount > 0, "Chaos never triggered!");
```

### 5. Combine with Real Polly Policies

Test your actual production policies:

```csharp
// Use your real policy from production
var policy = YourProductionClass.GetRetryPolicy();

// Test it with chaos
await policy.ExecuteAsync(() => chaoticMock.CallAsync());
```

---

## Comparison with Simmy

| Feature | Skugga Chaos | Polly + Simmy |
|---------|-------------|---------------|
| **Target** | Test mocks | Production code |
| **Setup** | One line | Multiple packages |
| **Integration** | Built into mocks | Separate policies |
| **Statistics** | Built-in tracking | Manual |
| **Reproducibility** | Seeds included | Manual |
| **Use Case** | Test time | Runtime |

**Use Both:**
- **Simmy** for production chaos engineering
- **Skugga Chaos** for testing resilience in unit/integration tests

---

## Troubleshooting

### Issue: "Chaos never triggers"

**Solution:** Verify failure rate and statistics:

```csharp
mock.Chaos(policy => {
    policy.FailureRate = 0.5;
    policy.TrackStatistics = true;
});

// After tests
var stats = mock.GetChaosStatistics();
Console.WriteLine($"Chaos triggered: {stats.ChaosTriggeredCount} times");
```

### Issue: "Tests are flaky"

**Solution:** Use seeds for reproducibility:

```csharp
mock.Chaos(policy => {
    policy.Seed = 42; // Fixed seed = consistent failures
});
```

### Issue: "Latency too high"

**Solution:** Adjust latency ranges:

```csharp
mock.Chaos(policy => {
    policy.MinLatencyMs = 10;   // Lower minimum
    policy.MaxLatencyMs = 100;  // Lower maximum
});
```

---

## FAQ

**Q: Should I use Chaos in production?**  
A: No! Skugga Chaos is for testing only. Use [Polly + Simmy](https://github.com/Polly-Contrib/Simmy) for production chaos.

**Q: Can I use Chaos with real services?**  
A: No, Chaos only works with Skugga mocks. For real services, use Simmy or network-level chaos tools.

**Q: Is Chaos deterministic?**  
A: Yes, when using seeds. Without seeds, it's random.

**Q: Does Chaos work with async methods?**  
A: Yes! Chaos works seamlessly with `async`/`await`.

---

## Related Features

- **[Zero-Allocation Testing](ALLOCATION_TESTING.md)** - Performance enforcement
- **[API Reference](API_REFERENCE.md#chaos-engineering)** - Complete Chaos API docs

---

**Built with ❤️ by [Digvijay](https://github.com/Digvijay) | Contributions welcome!**

*Break things in tests, not production.*
