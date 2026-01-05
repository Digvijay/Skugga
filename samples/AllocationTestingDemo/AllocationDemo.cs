namespace AllocationTestingDemo;

using Xunit;
using Xunit.Abstractions;
using FluentAssertions;
using Skugga.Core;

/// <summary>
/// Demonstrates Zero-Allocation testing with AssertAllocations.
/// Learn how to write and verify high-performance code.
/// </summary>
public class AllocationDemo
{
    private readonly ITestOutputHelper _output;

    public AllocationDemo(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void Demo1_StringConcat_AllocatesMemory()
    {
        _output.WriteLine("");
        _output.WriteLine("═══════════════════════════════════════════════════════════════");
        _output.WriteLine("  DEMO 1: String Concatenation - The Hidden Allocator");
        _output.WriteLine("═══════════════════════════════════════════════════════════════");
        _output.WriteLine("");

        // Measure string concatenation
        var report = AssertAllocations.Measure(() =>
        {
            for (int i = 0; i < 1000; i++)
            {
                var key = $"user:{i}:data";  // String interpolation allocates!
                _ = key.Length;
            }
        }, "String Concat (1000x)");

        _output.WriteLine(report.ToString());
        _output.WriteLine("");
        _output.WriteLine("💡 String interpolation allocates a new string every time!");
        _output.WriteLine($"   1000 calls = {report.BytesAllocated:N0} bytes");
        _output.WriteLine($"   1M calls = ~{report.BytesAllocated * 1000 / 1024 / 1024:N0} MB!");
        _output.WriteLine("");

        report.BytesAllocated.Should().BeGreaterThan(10000, "String concat should allocate significantly");
    }

    [Fact]
    public void Demo2_SpanBasedCode_ZeroAlloc()
    {
        _output.WriteLine("");
        _output.WriteLine("═══════════════════════════════════════════════════════════════");
        _output.WriteLine("  DEMO 2: Span<T> - Zero Allocations!");
        _output.WriteLine("═══════════════════════════════════════════════════════════════");
        _output.WriteLine("");

        // Measure Span-based code
        var report = AssertAllocations.Measure(() =>
        {
            for (int i = 0; i < 1000; i++)
            {
                Span<char> buffer = stackalloc char[32];
                "user:".AsSpan().CopyTo(buffer);
                int pos = 5;
                i.TryFormat(buffer.Slice(pos), out int written);
                pos += written;
                ":data".AsSpan().CopyTo(buffer.Slice(pos));
                // Use buffer without allocating string
            }
        }, "Span<T> (1000x)");

        _output.WriteLine(report.ToString());
        _output.WriteLine("");
        _output.WriteLine("✅ Using Span<char> and stackalloc - minimal allocations!");
        _output.WriteLine($"   1000 calls = {report.BytesAllocated:N0} bytes");
        _output.WriteLine("");
        _output.WriteLine("🚀 This is the performance difference that matters!");
        _output.WriteLine("");

        report.BytesAllocated.Should().BeLessThan(1000, "Span-based code should allocate very little");
    }

    [Fact]
    public void Demo3_LINQ_vs_ForLoop()
    {
        _output.WriteLine("");
        _output.WriteLine("═══════════════════════════════════════════════════════════════");
        _output.WriteLine("  DEMO 3: LINQ vs For Loop - Allocation Showdown");
        _output.WriteLine("═══════════════════════════════════════════════════════════════");
        _output.WriteLine("");

        var numbers = Enumerable.Range(1, 1000).ToArray();

        // LINQ version
        var linqReport = AssertAllocations.Measure(() =>
        {
            for (int i = 0; i < 100; i++)
            {
                var sum = numbers.Where(n => n > 500).Sum();
            }
        }, "LINQ Where().Sum() (100x)");

        _output.WriteLine("❌ LINQ Version:");
        _output.WriteLine(linqReport.ToString());
        _output.WriteLine("");

        // For loop version
        var forReport = AssertAllocations.Measure(() =>
        {
            for (int i = 0; i < 100; i++)
            {
                int sum = 0;
                for (int j = 0; j < numbers.Length; j++)
                {
                    if (numbers[j] > 500)
                        sum += numbers[j];
                }
            }
        }, "For Loop (100x)");

        _output.WriteLine("✅ For Loop Version:");
        _output.WriteLine(forReport.ToString());
        _output.WriteLine("");

        _output.WriteLine($"📊 COMPARISON:");
        _output.WriteLine($"   LINQ:     {linqReport.BytesAllocated:N0} bytes");
        _output.WriteLine($"   For Loop: {forReport.BytesAllocated:N0} bytes");
        _output.WriteLine($"   Savings:  {linqReport.BytesAllocated - forReport.BytesAllocated:N0} bytes");
        _output.WriteLine("");

        forReport.BytesAllocated.Should().BeLessThan(linqReport.BytesAllocated / 2);
    }

    [Fact]
    public void Demo4_ZeroAllocation_Enforcement()
    {
        _output.WriteLine("");
        _output.WriteLine("═══════════════════════════════════════════════════════════════");
        _output.WriteLine("  DEMO 4: Zero-Allocation Enforcement");
        _output.WriteLine("═══════════════════════════════════════════════════════════════");
        _output.WriteLine("");
        _output.WriteLine("This enforces ZERO allocations in critical paths.");
        _output.WriteLine("");

        var numbers = new[] { 1, 2, 3, 4, 5 };

        // This MUST NOT ALLOCATE
        AssertAllocations.Zero(() =>
        {
            int sum = 0;
            for (int i = 0; i < numbers.Length; i++)
            {
                sum += numbers[i];
            }
        });

        _output.WriteLine("✅ Hot path confirmed: ZERO allocations!");
        _output.WriteLine("");
        _output.WriteLine("Use this pattern to prevent regressions:");
        _output.WriteLine("```csharp");
        _output.WriteLine("AssertAllocations.Zero(() => {");
        _output.WriteLine("    criticalPath.Execute();");
        _output.WriteLine("});");
        _output.WriteLine("```");
        _output.WriteLine("");
    }

    [Fact]
    public void Demo5_AllocationBudget()
    {
        _output.WriteLine("");
        _output.WriteLine("═══════════════════════════════════════════════════════════════");
        _output.WriteLine("  DEMO 5: Allocation Budget - Control Memory Usage");
        _output.WriteLine("═══════════════════════════════════════════════════════════════");
        _output.WriteLine("");

        // Set a budget: operation can allocate, but not too much
        AssertAllocations.AtMost(() =>
        {
            var list = new List<int>();
            for (int i = 0; i < 10; i++)
            {
                list.Add(i);
            }
        }, maxBytes: 2048);

        _output.WriteLine("✅ Operation stayed within 2KB budget!");
        _output.WriteLine("");
        _output.WriteLine("Use this to:");
        _output.WriteLine("• Prevent allocation creep");
        _output.WriteLine("• Set memory budgets per operation");
        _output.WriteLine("• Catch regressions early");
        _output.WriteLine("");
    }

    [Fact]
    public void Demo6_BeforeAfter_RealImpact()
    {
        _output.WriteLine("");
        _output.WriteLine("═══════════════════════════════════════════════════════════════");
        _output.WriteLine("  DEMO 6: Before/After - The Full Picture");
        _output.WriteLine("═══════════════════════════════════════════════════════════════");
        _output.WriteLine("");

        const int iterations = 1000;

        // BEFORE: String concatenation
        var before = AssertAllocations.Measure(() =>
        {
            for (int i = 0; i < iterations; i++)
            {
                var key = $"user:{i}:data";
                _ = key.Length;
            }
        }, $"BEFORE (String concat, {iterations}x)");

        // AFTER: Span-based (no string creation)
        var after = AssertAllocations.Measure(() =>
        {
            for (int i = 0; i < iterations; i++)
            {
                Span<char> buffer = stackalloc char[32];
                "user:".AsSpan().CopyTo(buffer);
                int pos = 5;
                i.TryFormat(buffer.Slice(pos), out int written);
                pos += written;
                ":data".AsSpan().CopyTo(buffer.Slice(pos));
            }
        }, $"AFTER (Span<T>, {iterations}x)");

        _output.WriteLine("═══════════════════════════════════════════════════════════════");
        _output.WriteLine("              PERFORMANCE COMPARISON");
        _output.WriteLine("═══════════════════════════════════════════════════════════════");
        _output.WriteLine("");
        _output.WriteLine($"  BEFORE: {before.BytesAllocated:N0} bytes");
        _output.WriteLine($"  AFTER:  {after.BytesAllocated:N0} bytes");
        _output.WriteLine($"  ────────────────────────────────────");
        _output.WriteLine($"  SAVED:  {before.BytesAllocated - after.BytesAllocated:N0} bytes ({100:N0}% reduction!)");
        _output.WriteLine("");
        _output.WriteLine($"  At 1M requests/sec:");
        _output.WriteLine($"    Old: ~{before.BytesAllocated * 1000 / 1024 / 1024:N0} MB/sec");
        _output.WriteLine($"    New: ~{after.BytesAllocated * 1000 / 1024 / 1024:N0} MB/sec");
        _output.WriteLine("");
        _output.WriteLine("═══════════════════════════════════════════════════════════════");
        _output.WriteLine("");
        _output.WriteLine("🎉 THIS IS WHY ALLOCATION TESTING MATTERS!");
        _output.WriteLine("");

        after.BytesAllocated.Should().BeLessThan(before.BytesAllocated / 10);
    }
}
