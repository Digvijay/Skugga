#nullable enable
using System;

namespace Skugga.Core
{
    /// <summary>
    /// Configuration for chaos engineering mode in mocks.
    /// Enables testing application resilience by randomly introducing failures and delays.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Chaos mode helps verify that your application handles failures gracefully:
    /// </para>
    /// <list type="bullet">
    /// <item><description>Random exceptions to test error handling</description></item>
    /// <item><description>Delays to test timeout handling</description></item>
    /// <item><description>Configurable failure rates for realistic scenarios</description></item>
    /// <item><description>Reproducible chaos with seeds for consistent test runs</description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// var mock = Mock.Create&lt;IService&gt;();
    /// 
    /// // Configure chaos mode with 30% failure rate
    /// mock.Chaos(policy => {
    ///     policy.FailureRate = 0.3;  // 30% of calls will fail
    ///     policy.PossibleExceptions = new[] {
    ///         new TimeoutException(),
    ///         new InvalidOperationException()
    ///     };
    ///     policy.TimeoutMilliseconds = 100;  // Simulate slow responses
    ///     policy.Seed = 42;  // Reproducible chaos
    /// });
    /// 
    /// // Test your code handles failures
    /// for (int i = 0; i < 100; i++)
    /// {
    ///     try {
    ///         mock.GetData();
    ///     } catch (Exception ex) {
    ///         // Verify your error handling works
    ///     }
    /// }
    /// 
    /// // Check statistics
    /// var stats = mock.Handler.ChaosStatistics;
    /// Console.WriteLine($"Triggered {stats.ChaosTriggeredCount} failures out of {stats.TotalInvocations} calls");
    /// </code>
    /// </example>
    public class ChaosPolicy 
    { 
        /// <summary>
        /// Gets or sets the probability (0.0 to 1.0) that a mocked method will fail.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>0.0:</b> No failures (chaos disabled)
        /// </para>
        /// <para>
        /// <b>0.5:</b> 50% of calls will fail
        /// </para>
        /// <para>
        /// <b>1.0:</b> All calls will fail
        /// </para>
        /// <para>
        /// Use realistic failure rates (e.g., 0.1 for 10%) to test resilience
        /// without making tests too flaky.
        /// </para>
        /// </remarks>
        public double FailureRate { get; set; }
        
        /// <summary>
        /// Gets or sets the array of exceptions to randomly throw when chaos triggers.
        /// </summary>
        /// <remarks>
        /// <para>
        /// If null or empty, no exceptions are thrown (only delays are applied).
        /// When chaos triggers, one exception is randomly selected from this array and thrown.
        /// </para>
        /// <para>
        /// Common exceptions to test:
        /// </para>
        /// <list type="bullet">
        /// <item><description>TimeoutException - Network/service timeouts</description></item>
        /// <item><description>InvalidOperationException - Invalid state errors</description></item>
        /// <item><description>IOException - I/O failures</description></item>
        /// <item><description>HttpRequestException - HTTP failures</description></item>
        /// </list>
        /// </remarks>
        /// <example>
        /// <code>
        /// policy.PossibleExceptions = new Exception[] {
        ///     new TimeoutException("Service timed out"),
        ///     new HttpRequestException("503 Service Unavailable"),
        ///     new InvalidOperationException("Service in maintenance mode")
        /// };
        /// </code>
        /// </example>
        public Exception[]? PossibleExceptions { get; set; }
        
        /// <summary>
        /// Gets or sets the delay in milliseconds to simulate slow responses or timeouts.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>0:</b> No delay (default)
        /// </para>
        /// <para>
        /// Use to test:
        /// </para>
        /// <list type="bullet">
        /// <item><description>Timeout handling in your code</description></item>
        /// <item><description>Responsiveness under slow network conditions</description></item>
        /// <item><description>Cancellation token handling</description></item>
        /// </list>
        /// <para>
        /// The delay is applied via Thread.Sleep() on every invocation (not random).
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// // Simulate 500ms network latency
        /// policy.TimeoutMilliseconds = 500;
        /// 
        /// // Test timeout handling
        /// using var cts = new CancellationTokenSource(1000);
        /// await service.GetDataAsync(cts.Token);  // Should complete before timeout
        /// </code>
        /// </example>
        public int TimeoutMilliseconds { get; set; }
        
        /// <summary>
        /// Gets or sets the seed for the random number generator.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>null:</b> Random seed (default) - chaos is non-deterministic
        /// </para>
        /// <para>
        /// <b>Fixed value:</b> Reproducible chaos - same sequence of failures on each run
        /// </para>
        /// <para>
        /// Use a fixed seed for:
        /// </para>
        /// <list type="bullet">
        /// <item><description>Debugging specific failure scenarios</description></item>
        /// <item><description>Creating reproducible test cases</description></item>
        /// <item><description>CI/CD environments where consistency is needed</description></item>
        /// </list>
        /// </remarks>
        /// <example>
        /// <code>
        /// // Reproducible chaos for debugging
        /// policy.Seed = 12345;
        /// 
        /// // Run test multiple times - same failures occur
        /// for (int run = 0; run < 5; run++)
        /// {
        ///     // Same chaos pattern every time
        /// }
        /// </code>
        /// </example>
        public int? Seed { get; set; }
    }
    
    /// <summary>
    /// Statistics about chaos mode behavior during test execution.
    /// Provides insights into failure rates and patterns.
    /// </summary>
    /// <remarks>
    /// Access via <c>mockHandler.ChaosStatistics</c> to analyze chaos behavior.
    /// </remarks>
    /// <example>
    /// <code>
    /// var stats = mock.Handler.ChaosStatistics;
    /// 
    /// Console.WriteLine($"Total invocations: {stats.TotalInvocations}");
    /// Console.WriteLine($"Chaos triggered: {stats.ChaosTriggeredCount}");
    /// Console.WriteLine($"Timeouts applied: {stats.TimeoutTriggeredCount}");
    /// Console.WriteLine($"Actual failure rate: {stats.ActualFailureRate:P2}");
    /// 
    /// // Assert chaos is working as expected
    /// Assert.InRange(stats.ActualFailureRate, 0.25, 0.35);  // Expected ~30%
    /// </code>
    /// </example>
    public class ChaosStatistics
    {
        /// <summary>
        /// Gets or sets the total number of method invocations on the mock.
        /// </summary>
        /// <remarks>
        /// Incremented on every method call, regardless of whether chaos triggered.
        /// </remarks>
        public int TotalInvocations { get; set; }
        
        /// <summary>
        /// Gets or sets the number of times chaos mode triggered a failure.
        /// </summary>
        /// <remarks>
        /// Incremented when random failure triggers and an exception is thrown.
        /// </remarks>
        public int ChaosTriggeredCount { get; set; }
        
        /// <summary>
        /// Gets or sets the number of times a timeout/delay was applied.
        /// </summary>
        /// <remarks>
        /// Incremented on every invocation if TimeoutMilliseconds > 0.
        /// Note: This counts delays, not failures.
        /// </remarks>
        public int TimeoutTriggeredCount { get; set; }
        
        /// <summary>
        /// Gets the actual failure rate observed during execution.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Calculated as: ChaosTriggeredCount / TotalInvocations
        /// </para>
        /// <para>
        /// Due to randomness, this may differ slightly from the configured FailureRate.
        /// Over many invocations, it should converge to the configured rate.
        /// </para>
        /// </remarks>
        public double ActualFailureRate => TotalInvocations > 0 ? (double)ChaosTriggeredCount / TotalInvocations : 0;
        
        /// <summary>
        /// Resets all statistics to zero.
        /// </summary>
        /// <remarks>
        /// Call this between test runs to get fresh statistics.
        /// </remarks>
        public void Reset()
        {
            TotalInvocations = 0;
            ChaosTriggeredCount = 0;
            TimeoutTriggeredCount = 0;
        }
    }
}
