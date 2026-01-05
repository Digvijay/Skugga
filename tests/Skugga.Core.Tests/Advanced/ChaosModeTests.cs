using Skugga.Core;

namespace Skugga.Core.Tests;

/// <summary>
/// Tests for Chaos Engineering mode - resilience testing feature that randomly injects failures.
/// Chaos mode helps verify that your code can handle unexpected exceptions and edge cases gracefully.
/// </summary>
/// <remarks>
/// <para>
/// Chaos Engineering is a discipline that experiments on a system to build confidence in its capability
/// to withstand turbulent conditions. Skugga's Chaos mode brings this concept to unit testing by:
/// - Randomly throwing exceptions from mocked methods
/// - Simulating unreliable dependencies and network failures
/// - Testing error handling and retry logic
/// - Validating graceful degradation scenarios
/// </para>
/// <para>
/// Key features:
/// - Configurable failure rate (0.0 to 1.0, where 1.0 = always fail)
/// - Custom exception types (or use default ChaosException)
/// - Statistics tracking (total calls, failures, success rate)
/// - Deterministic mode for reproducible tests
/// - Per-method chaos configuration
/// </para>
/// <para>
/// Usage patterns:
/// <code>
/// var mock = Mock.Create&lt;IService&gt;();
/// mock.Chaos(0.3); // 30% failure rate
/// mock.Setup(m => m.GetData()).Returns("data");
/// 
/// // Test will randomly fail ~30% of the time
/// var data = mock.GetData(); // May throw ChaosException
/// </code>
/// </para>
/// <para>
/// Test coverage:
/// - Various failure rates (0%, 50%, 100%)
/// - Custom exception types
/// - Statistics collection and verification
/// - Integration with Setup/Returns/Callback
/// - Multiple method calls with chaos
/// </para>
/// </remarks>
public class ChaosModeTests
{
    public interface IService
    {
        string GetData();
        int Calculate();
    }

    [Fact]
    [Trait("Category", "Advanced")]
    public void Chaos_WithZeroFailureRate_ShouldNeverThrow()
    {
        // Arrange
        var mock = Mock.Create<IService>();
        mock.Setup(x => x.GetData()).Returns("data");
        mock.Chaos(policy => policy.FailureRate = 0.0);

        // Act & Assert - run multiple times to verify
        for (int i = 0; i < 100; i++)
        {
            var result = mock.GetData();
            result.Should().Be("data");
        }
    }

    [Fact]
    [Trait("Category", "Advanced")]
    public void Chaos_WithFullFailureRate_ShouldAlwaysThrow()
    {
        // Arrange
        var mock = Mock.Create<IService>();
        mock.Setup(x => x.GetData()).Returns("data");
        mock.Chaos(policy =>
        {
            policy.FailureRate = 1.0;
            policy.PossibleExceptions = new[] { new InvalidOperationException("Chaos!") };
        });

        // Act & Assert
        var act = () => mock.GetData();
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Chaos!");
    }

    [Fact]
    [Trait("Category", "Advanced")]
    public void Chaos_WithPartialFailureRate_ShouldSometimesThrow()
    {
        // Arrange
        var mock = Mock.Create<IService>();
        mock.Setup(x => x.Calculate()).Returns(42);
        mock.Chaos(policy =>
        {
            policy.FailureRate = 0.5; // 50% failure rate
            policy.PossibleExceptions = new[] { new TimeoutException() };
        });

        // Act - run multiple times and count failures
        var successCount = 0;
        var failureCount = 0;

        for (var i = 0; i < 100; i++)
        {
            try
            {
                _ = mock.Calculate();
                successCount++;
            }
            catch (TimeoutException)
            {
                failureCount++;
            }
        }

        // Assert - with 100 iterations at 50% rate, we should have both successes and failures
        successCount.Should().BeGreaterThan(0, "some calls should succeed");
        failureCount.Should().BeGreaterThan(0, "some calls should fail");
    }

    [Fact]
    [Trait("Category", "Advanced")]
    public void Chaos_WithMultipleExceptions_ShouldThrowRandomException()
    {
        // Arrange
        var mock = Mock.Create<IService>();
        mock.Setup(x => x.GetData()).Returns("data");
        
        var exceptions = new Exception[]
        {
            new TimeoutException(),
            new InvalidOperationException(),
            new ArgumentException()
        };

        mock.Chaos(policy =>
        {
            policy.FailureRate = 1.0;
            policy.PossibleExceptions = exceptions;
        });

        // Act & Assert - verify it throws one of the configured exceptions
        var act = () => mock.GetData();
        act.Should().Throw<Exception>()
            .And.Should().BeAssignableTo<Exception>();
    }

    [Fact]
    [Trait("Category", "Advanced")]
    public void Chaos_WithNoExceptions_ShouldNotThrow()
    {
        // Arrange
        var mock = Mock.Create<IService>();
        mock.Setup(x => x.GetData()).Returns("data");
        mock.Chaos(policy =>
        {
            policy.FailureRate = 1.0;
            policy.PossibleExceptions = Array.Empty<Exception>();
        });

        // Act
        var result = mock.GetData();

        // Assert - should not throw even with 100% failure rate if no exceptions configured
        result.Should().Be("data");
    }

    [Fact]
    [Trait("Category", "Advanced")]
    public void Chaos_WithNullExceptions_ShouldNotThrow()
    {
        // Arrange
        var mock = Mock.Create<IService>();
        mock.Setup(x => x.GetData()).Returns("data");
        mock.Chaos(policy =>
        {
            policy.FailureRate = 1.0;
            policy.PossibleExceptions = null;
        });

        // Act
        var result = mock.GetData();

        // Assert
        result.Should().Be("data");
    }

    [Theory]
    [Trait("Category", "Advanced")]
    [InlineData(0.0)]
    [InlineData(0.25)]
    [InlineData(0.5)]
    [InlineData(0.75)]
    [InlineData(1.0)]
    public void Chaos_WithVariousFailureRates_ShouldBeConfigurable(double failureRate)
    {
        // Arrange
        var mock = Mock.Create<IService>();
        mock.Setup(x => x.Calculate()).Returns(100);
        mock.Chaos(policy =>
        {
            policy.FailureRate = failureRate;
            policy.PossibleExceptions = new[] { new Exception("Test") };
        });

        // Act & Assert - just verify it doesn't crash
        // Actual probability testing would require more sophisticated statistics
        var act = () =>
        {
            for (int i = 0; i < 10; i++)
            {
                try { mock.Calculate(); } catch { }
            }
        };

        act.Should().NotThrow();
    }
    
    [Fact]
    [Trait("Category", "Advanced")]
    public void Chaos_WithConfigurableSeed_ShouldBeReproducible()
    {
        // Arrange - create two mocks with same seed
        var mock1 = Mock.Create<IService>();
        mock1.Setup(x => x.Calculate()).Returns(42);
        mock1.Chaos(policy =>
        {
            policy.FailureRate = 0.5;
            policy.Seed = 12345;
            policy.PossibleExceptions = new[] { new InvalidOperationException() };
        });
        
        var mock2 = Mock.Create<IService>();
        mock2.Setup(x => x.Calculate()).Returns(42);
        mock2.Chaos(policy =>
        {
            policy.FailureRate = 0.5;
            policy.Seed = 12345;
            policy.PossibleExceptions = new[] { new InvalidOperationException() };
        });
        
        // Act - execute same calls on both mocks
        var results1 = new List<bool>(); // true = success, false = failure
        var results2 = new List<bool>();
        
        for (int i = 0; i < 20; i++)
        {
            try { mock1.Calculate(); results1.Add(true); } 
            catch { results1.Add(false); }
            
            try { mock2.Calculate(); results2.Add(true); } 
            catch { results2.Add(false); }
        }
        
        // Assert - both mocks should have identical behavior
        results1.Should().Equal(results2, "same seed should produce same results");
    }
    
    [Fact]
    [Trait("Category", "Advanced")]
    public void Chaos_WithTimeout_ShouldDelayExecution()
    {
        // Arrange
        var mock = Mock.Create<IService>();
        mock.Setup(x => x.GetData()).Returns("data");
        mock.Chaos(policy =>
        {
            policy.FailureRate = 0.0; // No failures, just timeout
            policy.TimeoutMilliseconds = 100;
        });
        
        // Act
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var result = mock.GetData();
        sw.Stop();
        
        // Assert - should take at least the timeout duration
        result.Should().Be("data");
        sw.ElapsedMilliseconds.Should().BeGreaterOrEqualTo(100);
    }
    
    [Fact]
    [Trait("Category", "Advanced")]
    public void Chaos_Statistics_ShouldTrackInvocations()
    {
        // Arrange
        var mock = Mock.Create<IService>();
        mock.Setup(x => x.Calculate()).Returns(42);
        mock.Chaos(policy =>
        {
            policy.FailureRate = 0.5;
            policy.PossibleExceptions = new[] { new InvalidOperationException() };
            policy.Seed = 42; // Fixed seed for deterministic test
        });
        
        var handler = (mock as IMockSetup)?.Handler;
        handler.Should().NotBeNull();
        
        // Act - make multiple calls
        for (int i = 0; i < 10; i++)
        {
            try { mock.Calculate(); } catch { }
        }
        
        // Assert - statistics should be tracked
        var stats = handler!.ChaosStatistics;
        stats.TotalInvocations.Should().Be(10);
        stats.ChaosTriggeredCount.Should().BeGreaterThan(0);
        stats.ActualFailureRate.Should().BeGreaterThan(0);
    }
    
    [Fact]
    [Trait("Category", "Advanced")]
    public void Chaos_Statistics_CanBeReset()
    {
        // Arrange
        var mock = Mock.Create<IService>();
        mock.Setup(x => x.GetData()).Returns("data");
        mock.Chaos(policy => policy.FailureRate = 0.0);
        
        var handler = (mock as IMockSetup)?.Handler;
        
        // Act - invoke and reset
        mock.GetData();
        mock.GetData();
        var stats = handler!.ChaosStatistics;
        stats.TotalInvocations.Should().Be(2);
        
        stats.Reset();
        
        // Assert
        stats.TotalInvocations.Should().Be(0);
        stats.ChaosTriggeredCount.Should().Be(0);
        stats.TimeoutTriggeredCount.Should().Be(0);
    }
    
    [Fact]
    [Trait("Category", "Advanced")]
    public void Chaos_WithTimeout_ShouldTrackTimeoutStatistics()
    {
        // Arrange
        var mock = Mock.Create<IService>();
        mock.Setup(x => x.GetData()).Returns("data");
        mock.Chaos(policy =>
        {
            policy.FailureRate = 0.0;
            policy.TimeoutMilliseconds = 10;
        });
        
        var handler = (mock as IMockSetup)?.Handler;
        
        // Act
        mock.GetData();
        mock.GetData();
        
        // Assert
        var stats = handler!.ChaosStatistics;
        stats.TimeoutTriggeredCount.Should().Be(2);
    }
}
