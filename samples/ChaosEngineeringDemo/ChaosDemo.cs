namespace ChaosEngineeringDemo;

using Xunit;
using Xunit.Abstractions;
using FluentAssertions;
using Skugga.Core;

/// <summary>
/// External service that can fail in production.
/// </summary>
public interface IPaymentGateway
{
    Task<bool> ProcessPaymentAsync(string orderId, decimal amount);
    Task<string> GetTransactionStatusAsync(string transactionId);
}

/// <summary>
/// Demonstrates Chaos Engineering with Skugga.
/// See how chaos mode helps test resilience patterns.
/// </summary>
public class ChaosDemo
{
    private readonly ITestOutputHelper _output;

    public ChaosDemo(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task Demo1_WithoutResilience_FailsUnderChaos()
    {
        _output.WriteLine("");
        _output.WriteLine("═══════════════════════════════════════════════════════════════");
        _output.WriteLine("  DEMO 1: Without Resilience - Crashes Under Chaos");
        _output.WriteLine("═══════════════════════════════════════════════════════════════");
        _output.WriteLine("");
        _output.WriteLine("This is what happens when you don't have retry logic...");
        _output.WriteLine("");

        // Create mock with 30% chaos
        var mockPayment = Mock.Create<IPaymentGateway>();
        
        mockPayment.Chaos(policy =>
        {
            policy.FailureRate = 0.3;  // 30% of calls fail
            policy.PossibleExceptions = new Exception[]
            {
                new TimeoutException("Payment gateway timeout"),
                new InvalidOperationException("Service unavailable")
            };
            policy.Seed = 42;  // Reproducible chaos
        });

        mockPayment.Setup(x => x.ProcessPaymentAsync(It.IsAny<string>(), It.IsAny<decimal>()))
            .ReturnsAsync(true);

        // Try to process payments - will hit failures
        var failureHit = false;
        try
        {
            for (int i = 0; i < 10; i++)
            {
                await mockPayment.ProcessPaymentAsync($"order-{i}", 99.99m);
            }
        }
        catch (Exception ex)
        {
            failureHit = true;
            _output.WriteLine($"❌ FAILED: {ex.Message}");
            _output.WriteLine("");
        }

        var stats = mockPayment.GetChaosStatistics();
        _output.WriteLine($"📊 Chaos injected {stats.ChaosTriggeredCount} failures out of {stats.TotalInvocations} calls");
        _output.WriteLine($"   Failure rate: {(double)stats.ChaosTriggeredCount / stats.TotalInvocations:P1}");
        _output.WriteLine("");
        _output.WriteLine("💡 Without retry logic, your service is fragile!");
        _output.WriteLine("");

        failureHit.Should().BeTrue("Chaos should cause failures without resilience");
    }

    [Fact]
    public async Task Demo2_WithRetryPolicy_SurvivesChaos()
    {
        _output.WriteLine("");
        _output.WriteLine("═══════════════════════════════════════════════════════════════");
        _output.WriteLine("  DEMO 2: With Retry Policy - Survives 30% Failures");
        _output.WriteLine("═══════════════════════════════════════════════════════════════");
        _output.WriteLine("");

        var mockPayment = Mock.Create<IPaymentGateway>();
        
        mockPayment.Chaos(policy =>
        {
            policy.FailureRate = 0.3;
            policy.PossibleExceptions = new Exception[]
            {
                new TimeoutException("Timeout"),
                new InvalidOperationException("Unavailable")
            };
            policy.Seed = 42;
        });

        mockPayment.Setup(x => x.ProcessPaymentAsync(It.IsAny<string>(), It.IsAny<decimal>()))
            .ReturnsAsync(true);

        // With retry logic
        var successCount = 0;
        for (int i = 0; i < 20; i++)
        {
            try
            {
                var result = await RetryAsync(() => 
                    mockPayment.ProcessPaymentAsync($"order-{i}", 99.99m), 
                    maxAttempts: 3);
                
                if (result)
                    successCount++;
            }
            catch
            {
                // Even with retries, some might fail if all 3 attempts hit chaos
            }
        }

        var stats = mockPayment.GetChaosStatistics();
        _output.WriteLine($"✅ Successfully processed {successCount}/20 payments!");
        _output.WriteLine($"📊 Chaos triggered {stats.ChaosTriggeredCount} times out of {stats.TotalInvocations} total calls");
        _output.WriteLine($"   But retries saved us! 🎉");
        _output.WriteLine("");
        _output.WriteLine("This proves your retry logic works under chaos!");
        _output.WriteLine("");

        successCount.Should().BeGreaterThan(15, "Retry policy should handle most failures");
    }

    [Fact]
    public async Task Demo3_ChaosWithDelay_TestsTimeoutHandling()
    {
        _output.WriteLine("");
        _output.WriteLine("═══════════════════════════════════════════════════════════════");
        _output.WriteLine("  DEMO 3: Chaos with Delays - Simulating Slow Services");
        _output.WriteLine("═══════════════════════════════════════════════════════════════");
        _output.WriteLine("");

        var mockPayment = Mock.Create<IPaymentGateway>();
        
        mockPayment.Chaos(policy =>
        {
            policy.TimeoutMilliseconds = 100;  // Every call is delayed 100ms
            policy.FailureRate = 0.0;  // No random failures, just delays
        });

        mockPayment.Setup(x => x.GetTransactionStatusAsync(It.IsAny<string>()))
            .ReturnsAsync("COMPLETED");

        _output.WriteLine("Testing with 100ms delay on every call...");
        _output.WriteLine("");

        var sw = System.Diagnostics.Stopwatch.StartNew();
        
        for (int i = 0; i < 5; i++)
        {
            await mockPayment.GetTransactionStatusAsync($"txn-{i}");
        }
        
        sw.Stop();

        _output.WriteLine($"⏱️  5 calls took {sw.ElapsedMilliseconds}ms");
        _output.WriteLine($"   Average: {sw.ElapsedMilliseconds / 5}ms per call");
        _output.WriteLine("");
        _output.WriteLine("Use this to:");
        _output.WriteLine("• Test timeout handling");
        _output.WriteLine("• Test cancellation tokens");
        _output.WriteLine("• Verify async/await patterns");
        _output.WriteLine("");

        sw.ElapsedMilliseconds.Should().BeGreaterThan(450, "Should take at least 450ms for 5 calls with 100ms delay each");
    }

    [Fact]
    public async Task Demo4_Statistics_ShowExactInjectionRates()
    {
        _output.WriteLine("");
        _output.WriteLine("═══════════════════════════════════════════════════════════════");
        _output.WriteLine("  DEMO 4: Chaos Statistics - Precise Metrics");
        _output.WriteLine("═══════════════════════════════════════════════════════════════");
        _output.WriteLine("");

        var mockPayment = Mock.Create<IPaymentGateway>();
        
        mockPayment.Chaos(policy =>
        {
            policy.FailureRate = 0.2;  // 20% failure rate
            policy.PossibleExceptions = new Exception[]
            {
                new TimeoutException("Timeout"),
                new HttpRequestException("503 Service Unavailable")
            };
            policy.Seed = 789;  // Reproducible
        });

        mockPayment.Setup(x => x.ProcessPaymentAsync(It.IsAny<string>(), It.IsAny<decimal>()))
            .ReturnsAsync(true);

        // Make 100 calls
        int successCount = 0;
        int failureCount = 0;

        for (int i = 0; i < 100; i++)
        {
            try
            {
                await mockPayment.ProcessPaymentAsync($"order-{i}", 99.99m);
                successCount++;
            }
            catch
            {
                failureCount++;
            }
        }

        var stats = mockPayment.GetChaosStatistics();

        _output.WriteLine("📊 CHAOS STATISTICS:");
        _output.WriteLine("");
        _output.WriteLine($"   Total invocations:   {stats.TotalInvocations}");
        _output.WriteLine($"   Chaos triggered:     {stats.ChaosTriggeredCount} ({(double)stats.ChaosTriggeredCount / stats.TotalInvocations:P1})");
        _output.WriteLine($"   Expected rate:       20%");
        _output.WriteLine($"   Successes:           {successCount}");
        _output.WriteLine($"   Failures:            {failureCount}");
        _output.WriteLine("");
        _output.WriteLine("✅ Chaos injection rate matches expected!");
        _output.WriteLine("");

        failureCount.Should().BeInRange(10, 30, "Should be around 20% with some variance");
        stats.ChaosTriggeredCount.Should().Be(failureCount);
    }

    // Simple retry helper
    private static async Task<T> RetryAsync<T>(Func<Task<T>> operation, int maxAttempts = 3)
    {
        int attempts = 0;
        while (true)
        {
            attempts++;
            try
            {
                return await operation();
            }
            catch when (attempts < maxAttempts)
            {
                await Task.Delay(50 * attempts);  // Simple backoff
            }
        }
    }
}
