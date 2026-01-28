#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace Skugga.Core
{
    /// <summary>
    /// Utilities for asserting and monitoring memory allocations during tests.
    /// Useful for performance testing and ensuring zero-allocation code paths.
    /// </summary>
    /// <remarks>
    /// <para>
    /// These tools help verify that performance-critical code doesn't allocate
    /// unnecessary heap memory, which is important for:
    /// </para>
    /// <list type="bullet">
    /// <item><description>High-performance APIs</description></item>
    /// <item><description>Real-time systems</description></item>
    /// <item><description>Low-latency applications</description></item>
    /// <item><description>Memory-constrained environments</description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Assert zero allocations
    /// AssertAllocations.Zero(() => {
    ///     mock.GetValue();  // Should not allocate
    /// });
    ///
    /// // Assert at most 1KB allocated
    /// AssertAllocations.AtMost(() => {
    ///     mock.ProcessData(largeArray);
    /// }, maxBytes: 1024);
    ///
    /// // Measure and analyze
    /// var report = AssertAllocations.Measure(() => {
    ///     for (int i = 0; i < 1000; i++)
    ///         mock.GetNext();
    /// }, "GetNext 1000x");
    ///
    /// Console.WriteLine(report);  // Detailed allocation info
    /// </code>
    /// </example>
    public static class AssertAllocations
    {
        /// <summary>
        /// Asserts that an action allocates zero bytes on the heap.
        /// </summary>
        /// <param name="action">Action to execute and measure</param>
        /// <exception cref="Exception">Thrown if any heap allocations are detected</exception>
        /// <remarks>
        /// <para>
        /// Uses GC.GetAllocatedBytesForCurrentThread() to measure allocations.
        /// This is very precise but only measures heap allocations (not stack).
        /// </para>
        /// <para>
        /// Common sources of allocations to avoid:
        /// </para>
        /// <list type="bullet">
        /// <item><description>Boxing value types</description></item>
        /// <item><description>String concatenation (use StringBuilder or interpolation carefully)</description></item>
        /// <item><description>LINQ queries (use for loops instead)</description></item>
        /// <item><description>Closures capturing variables</description></item>
        /// </list>
        /// </remarks>
        /// <example>
        /// <code>
        /// // This should pass - no allocations
        /// int result = 0;
        /// AssertAllocations.Zero(() => {
        ///     result = 42 + 58;  // Pure value type arithmetic
        /// });
        ///
        /// // This will fail - string concatenation allocates
        /// AssertAllocations.Zero(() => {
        ///     var s = "hello" + "world";  // Allocates new string
        /// });
        /// </code>
        /// </example>
        public static void Zero(Action action)
        {
            long before = GC.GetAllocatedBytesForCurrentThread();
            action();
            long after = GC.GetAllocatedBytesForCurrentThread();

            if (after - before > 0)
                throw new Exception($"Allocated {after - before} bytes (Expected 0).");
        }

        /// <summary>
        /// Asserts that an action allocates at most the specified number of bytes.
        /// </summary>
        /// <param name="action">Action to execute and measure</param>
        /// <param name="maxBytes">Maximum allowed allocation in bytes</param>
        /// <exception cref="Exception">Thrown if allocations exceed the threshold</exception>
        /// <remarks>
        /// <para>
        /// Use this when you know some allocation is necessary, but want to
        /// ensure it doesn't exceed a reasonable limit.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// // Allow up to 100 bytes (e.g., for a small string)
        /// AssertAllocations.AtMost(() => {
        ///     var name = $"User_{userId}";  // String interpolation
        /// }, maxBytes: 100);
        ///
        /// // Ensure array pooling is working
        /// AssertAllocations.AtMost(() => {
        ///     var buffer = ArrayPool&lt;byte&gt;.Shared.Rent(1024);
        ///     // Use buffer...
        ///     ArrayPool&lt;byte&gt;.Shared.Return(buffer);
        /// }, maxBytes: 0);  // Should not allocate if pooling works
        /// </code>
        /// </example>
        public static void AtMost(Action action, long maxBytes)
        {
            long before = GC.GetAllocatedBytesForCurrentThread();
            action();
            long after = GC.GetAllocatedBytesForCurrentThread();
            long allocated = after - before;

            if (allocated > maxBytes)
                throw new Exception($"Allocated {allocated} bytes (Expected at most {maxBytes}).");
        }

        /// <summary>
        /// Measures allocation of an action and returns a detailed report.
        /// </summary>
        /// <param name="action">Action to execute and measure</param>
        /// <param name="actionName">Optional name for the action being measured</param>
        /// <returns>Allocation report with detailed statistics</returns>
        /// <remarks>
        /// <para>
        /// Forces garbage collection before measurement to get accurate baseline.
        /// Also measures duration and GC collections across all generations.
        /// </para>
        /// <para>
        /// Use this for:
        /// </para>
        /// <list type="bullet">
        /// <item><description>Performance benchmarking</description></item>
        /// <item><description>Comparing allocation patterns</description></item>
        /// <item><description>Identifying allocation hotspots</description></item>
        /// </list>
        /// </remarks>
        /// <example>
        /// <code>
        /// var report = AssertAllocations.Measure(() => {
        ///     var list = new List&lt;int&gt;(1000);
        ///     for (int i = 0; i < 1000; i++)
        ///         list.Add(i);
        /// }, "List initialization");
        ///
        /// Console.WriteLine(report);
        /// // Output: [List initialization] 4096 bytes allocated, 2ms duration, GC: Gen0=0, Gen1=0, Gen2=0
        ///
        /// // Use in assertions
        /// Assert.True(report.BytesAllocated < 10000, "Too much memory allocated");
        /// Assert.True(report.DurationMilliseconds < 10, "Too slow");
        /// </code>
        /// </example>
        public static AllocationReport Measure(Action action, string actionName = "Action")
        {
            // Force GC to get accurate baseline
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            // Record GC collection counts before execution
            long gen0Before = GC.CollectionCount(0);
            long gen1Before = GC.CollectionCount(1);
            long gen2Before = GC.CollectionCount(2);
            long bytesBefore = GC.GetAllocatedBytesForCurrentThread();

            // Execute and time the action
            var sw = System.Diagnostics.Stopwatch.StartNew();
            action();
            sw.Stop();

            // Record metrics after execution
            long bytesAfter = GC.GetAllocatedBytesForCurrentThread();
            long gen0After = GC.CollectionCount(0);
            long gen1After = GC.CollectionCount(1);
            long gen2After = GC.CollectionCount(2);

            return new AllocationReport
            {
                ActionName = actionName,
                BytesAllocated = bytesAfter - bytesBefore,
                DurationMilliseconds = sw.ElapsedMilliseconds,
                Gen0Collections = (int)(gen0After - gen0Before),
                Gen1Collections = (int)(gen1After - gen1Before),
                Gen2Collections = (int)(gen2After - gen2Before)
            };
        }

        /// <summary>
        /// Configures a performance threshold for a specific action.
        /// </summary>
        /// <param name="actionName">Name of the action to monitor</param>
        /// <param name="maxBytes">Maximum allowed allocation in bytes</param>
        /// <param name="maxMilliseconds">Maximum allowed duration in milliseconds</param>
        /// <returns>Performance threshold configuration</returns>
        /// <remarks>
        /// Use with MeetsThreshold() to validate performance requirements.
        /// </remarks>
        /// <example>
        /// <code>
        /// var threshold = AssertAllocations.Threshold(
        ///     "ProcessBatch",
        ///     maxBytes: 10_000,
        ///     maxMilliseconds: 100
        /// );
        ///
        /// AssertAllocations.MeetsThreshold(() => {
        ///     service.ProcessBatch(items);
        /// }, threshold);
        /// </code>
        /// </example>
        public static PerformanceThreshold Threshold(string actionName, long maxBytes, long maxMilliseconds)
        {
            return new PerformanceThreshold
            {
                ActionName = actionName,
                MaxBytes = maxBytes,
                MaxMilliseconds = maxMilliseconds
            };
        }

        /// <summary>
        /// Validates that an action meets a performance threshold.
        /// </summary>
        /// <param name="action">Action to execute and measure</param>
        /// <param name="threshold">Performance threshold to validate against</param>
        /// <exception cref="Exception">Thrown if the action exceeds the threshold</exception>
        /// <remarks>
        /// <para>
        /// Measures the action and throws if either bytes allocated or duration
        /// exceeds the configured threshold.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// var threshold = AssertAllocations.Threshold("Query", 1024, 50);
        ///
        /// AssertAllocations.MeetsThreshold(() => {
        ///     var results = database.Query("SELECT * FROM users");
        /// }, threshold);
        /// // Throws if > 1KB allocated or > 50ms duration
        /// </code>
        /// </example>
        public static void MeetsThreshold(Action action, PerformanceThreshold threshold)
        {
            var report = Measure(action, threshold.ActionName);

            if (report.BytesAllocated > threshold.MaxBytes)
                throw new Exception($"[{threshold.ActionName}] Allocated {report.BytesAllocated} bytes (Threshold: {threshold.MaxBytes}).");

            if (report.DurationMilliseconds > threshold.MaxMilliseconds)
                throw new Exception($"[{threshold.ActionName}] Took {report.DurationMilliseconds}ms (Threshold: {threshold.MaxMilliseconds}ms).");
        }
    }

    /// <summary>
    /// Detailed report of memory allocations and performance metrics.
    /// </summary>
    public class AllocationReport
    {
        /// <summary>
        /// Gets or sets the name of the action that was measured.
        /// </summary>
        public string ActionName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the total bytes allocated on the heap during execution.
        /// </summary>
        public long BytesAllocated { get; set; }

        /// <summary>
        /// Gets or sets the time taken to execute the action in milliseconds.
        /// </summary>
        public long DurationMilliseconds { get; set; }

        /// <summary>
        /// Gets or sets the number of generation 0 garbage collections during execution.
        /// </summary>
        /// <remarks>
        /// Gen0 collections are fast and frequent. High count may indicate many short-lived allocations.
        /// </remarks>
        public int Gen0Collections { get; set; }

        /// <summary>
        /// Gets or sets the number of generation 1 garbage collections during execution.
        /// </summary>
        /// <remarks>
        /// Gen1 collections are slower. Indicates objects surviving initial collection.
        /// </remarks>
        public int Gen1Collections { get; set; }

        /// <summary>
        /// Gets or sets the number of generation 2 garbage collections during execution.
        /// </summary>
        /// <remarks>
        /// Gen2 collections are slowest and most expensive. Should be rare.
        /// </remarks>
        public int Gen2Collections { get; set; }

        /// <summary>
        /// Formats the report as a human-readable string.
        /// </summary>
        public override string ToString()
        {
            return $"[{ActionName}] {BytesAllocated} bytes allocated, {DurationMilliseconds}ms duration, " +
                   $"GC: Gen0={Gen0Collections}, Gen1={Gen1Collections}, Gen2={Gen2Collections}";
        }
    }

    /// <summary>
    /// Performance threshold configuration for monitoring action performance.
    /// </summary>
    public class PerformanceThreshold
    {
        /// <summary>
        /// Gets or sets the name of the action being monitored.
        /// </summary>
        public string ActionName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the maximum allowed allocation in bytes.
        /// </summary>
        public long MaxBytes { get; set; }

        /// <summary>
        /// Gets or sets the maximum allowed duration in milliseconds.
        /// </summary>
        public long MaxMilliseconds { get; set; }
    }
}
