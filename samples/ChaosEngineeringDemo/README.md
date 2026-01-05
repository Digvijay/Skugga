# Chaos Engineering Demo 🔥

**Test resilience before production breaks.**

## The Problem

```csharp
// Works in dev, crashes in production
var data = await service.GetDataAsync();
ProcessData(data);

// Real world: TimeoutException, 503 errors, network issues!
// How do you KNOW your retry logic works?
```

## The Solution

```csharp
var mock = Mock.Create<IService>();

// Inject 30% random failures
mock.Chaos(policy => {
    policy.FailureRate = 0.3;
    policy.PossibleExceptions = new[] {
        new TimeoutException(),
        new HttpRequestException("503")
    };
});

// Now PROVE your retry logic works!
for (int i = 0; i < 100; i++) {
    await RetryPolicy.ExecuteAsync(() => mock.GetAsync());
}
// If this passes, resilience is real! ✅
```

## Quick Start

```bash
cd samples/ChaosEngineeringDemo

# See chaos testing in action
dotnet test --logger "console;verbosity=detailed"
```

## What You'll Learn

✅ How to test retry logic actually works  
✅ Circuit breaker patterns in action  
✅ Making flaky dependencies testable  
✅ Proving resilience before production  

## The Demos

All tests are in `tests/Skugga.Core.Tests/Advanced/ChaosTests.cs`:

1. **Without Resilience** - Crashes immediately ❌
2. **With Retry** - Survives 30% failures ✅
3. **Circuit Breaker** - Fails fast when degraded
4. **Statistics** - Analyze chaos patterns 📊

## Run It

```bash
dotnet test tests/Skugga.Core.Tests --filter "Chaos" --logger "console;verbosity=detailed"
```

See chaos injection in action and verify your error handling!

## Real Scenarios Tested

- Network timeouts (simulated delays)
- Service unavailability (503 errors)
- Intermittent failures (30% failure rate)
- Circuit breaker behavior (fail fast)
- Retry exhaustion (all attempts fail)

This is how you test resilience patterns actually work! 🚀
