using System;
using System.Diagnostics;
using FakeItEasy;
using Moq;
using NSubstitute;
using Skugga.Core;

// Unique interfaces for 4-framework comparison
public interface ICounter { int Increment(); int GetValue(); }
public interface IMessageBroker { string SendMessage(string msg); bool IsConnected(); }

/// <summary>
/// Benchmarks comparing Skugga vs Moq vs NSubstitute vs FakeItEasy
/// Focuses on most common scenarios that work across all frameworks
/// </summary>
public class FourFrameworkBenchmarks
{
    private const int Iterations = 50_000;
    private const int Warmup = 5_000;

    public static void RunAll()
    {
        var output = new System.Text.StringBuilder();
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        output.AppendLine("# Four-Framework Comparison Benchmarks\n");
        output.AppendLine($"**Test Date:** {timestamp}  ");
        output.AppendLine($"**Frameworks:** Skugga vs Moq vs NSubstitute vs FakeItEasy  ");
        output.AppendLine($"**Iterations:** {Iterations:N0} | **Warmup:** {Warmup:N0}  ");
        output.AppendLine($"**Hardware:** Intel Core i7-4980HQ @ 2.80GHz, 16GB RAM  ");
        output.AppendLine($"**OS:** macOS 15.7  ");
        output.AppendLine($"**Runtime:** .NET 10.0.1\n");
        output.AppendLine("---\n");

        Console.WriteLine(output.ToString());

        var results = new (string Name, double Skugga, double Moq, double NSub, double Fake)[]
        {
            Benchmark("1. Mock Creation",
                CreateMock_Skugga, CreateMock_Moq, CreateMock_NSubstitute, CreateMock_FakeItEasy),
            Benchmark("2. Simple Setup + Invoke",
                SimpleSetup_Skugga, SimpleSetup_Moq, SimpleSetup_NSubstitute, SimpleSetup_FakeItEasy),
            Benchmark("3. Multiple Setups",
                MultipleSetups_Skugga, MultipleSetups_Moq, MultipleSetups_NSubstitute, MultipleSetups_FakeItEasy),
            Benchmark("4. Property-like Method",
                PropertyMethod_Skugga, PropertyMethod_Moq, PropertyMethod_NSubstitute, PropertyMethod_FakeItEasy)
        };

        PrintResults(results, output);

        // Save to file
        var benchmarkDir = Path.Combine(Directory.GetCurrentDirectory(), "benchmarks");
        Directory.CreateDirectory(benchmarkDir);
        var filePath = Path.Combine(benchmarkDir, "FourFramework.md");
        File.WriteAllText(filePath, output.ToString());
        Console.WriteLine($"\n📊 Results saved to: {filePath}");
    }

    private static (string Name, double Skugga, double Moq, double NSub, double Fake) Benchmark(
        string name,
        Action<int> skuggaAction,
        Action<int> moqAction,
        Action<int> nsubAction,
        Action<int> fakeAction)
    {
        // Warmup
        skuggaAction(Warmup);
        moqAction(Warmup);
        nsubAction(Warmup);
        fakeAction(Warmup);
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        // Measure Skugga
        var swSkugga = Stopwatch.StartNew();
        skuggaAction(Iterations);
        swSkugga.Stop();

        // Measure Moq
        var swMoq = Stopwatch.StartNew();
        moqAction(Iterations);
        swMoq.Stop();

        // Measure NSubstitute
        var swNSub = Stopwatch.StartNew();
        nsubAction(Iterations);
        swNSub.Stop();

        // Measure FakeItEasy
        var swFake = Stopwatch.StartNew();
        fakeAction(Iterations);
        swFake.Stop();

        return (name, swSkugga.Elapsed.TotalMilliseconds, swMoq.Elapsed.TotalMilliseconds,
                swNSub.Elapsed.TotalMilliseconds, swFake.Elapsed.TotalMilliseconds);
    }

    private static void PrintResults((string Name, double Skugga, double Moq, double NSub, double Fake)[] results, System.Text.StringBuilder output)
    {
        output.AppendLine("\n" + "=".PadRight(120, '='));
        output.AppendLine("RESULTS");
        output.AppendLine("=".PadRight(120, '='));
        output.AppendLine();
        output.AppendLine($"{"Scenario",-30} {"Skugga (ms)",15} {"Moq (ms)",15} {"NSub (ms)",15} {"Fake (ms)",15}");
        output.AppendLine("-".PadRight(120, '-'));

        double totalSkugga = 0, totalMoq = 0, totalNSub = 0, totalFake = 0;
        foreach (var (name, skugga, moq, nsub, fake) in results)
        {
            output.AppendLine($"{name,-30} {skugga,15:F2} {moq,15:F2} {nsub,15:F2} {fake,15:F2}");
            totalSkugga += skugga;
            totalMoq += moq;
            totalNSub += nsub;
            totalFake += fake;
        }

        output.AppendLine("-".PadRight(120, '-'));
        output.AppendLine($"{"TOTAL",-30} {totalSkugga,15:F2} {totalMoq,15:F2} {totalNSub,15:F2} {totalFake,15:F2}");
        output.AppendLine();

        output.AppendLine("SPEEDUP vs Skugga (baseline):");
        output.AppendLine($"  Moq:           {(totalMoq / totalSkugga),6:F2}x slower");
        output.AppendLine($"  NSubstitute:   {(totalNSub / totalSkugga),6:F2}x slower");
        output.AppendLine($"  FakeItEasy:    {(totalFake / totalSkugga),6:F2}x slower");
        output.AppendLine();
        output.AppendLine("=".PadRight(120, '='));

        Console.Write(output.ToString());
    }

    // ============================================================================
    // BENCHMARK 1: Mock Creation
    // ============================================================================
    private static void CreateMock_Skugga(int iterations)
    {
        for (int i = 0; i < iterations; i++)
        {
            _ = Skugga.Core.Mock.Create<ICounter>();
        }
    }

    private static void CreateMock_Moq(int iterations)
    {
        for (int i = 0; i < iterations; i++)
        {
            _ = new Moq.Mock<ICounter>().Object;
        }
    }

    private static void CreateMock_NSubstitute(int iterations)
    {
        for (int i = 0; i < iterations; i++)
        {
            _ = Substitute.For<ICounter>();
        }
    }

    private static void CreateMock_FakeItEasy(int iterations)
    {
        for (int i = 0; i < iterations; i++)
        {
            _ = A.Fake<ICounter>();
        }
    }

    // ============================================================================
    // BENCHMARK 2: Simple Setup + Invoke
    // ============================================================================
    private static void SimpleSetup_Skugga(int iterations)
    {
        for (int i = 0; i < iterations; i++)
        {
            var mock = Skugga.Core.Mock.Create<ICounter>();
            mock.Setup(x => x.Increment()).Returns(1);
            _ = mock.Increment();
        }
    }

    private static void SimpleSetup_Moq(int iterations)
    {
        for (int i = 0; i < iterations; i++)
        {
            var mock = new Moq.Mock<ICounter>();
            mock.Setup(x => x.Increment()).Returns(1);
            _ = mock.Object.Increment();
        }
    }

    private static void SimpleSetup_NSubstitute(int iterations)
    {
        for (int i = 0; i < iterations; i++)
        {
            var sub = Substitute.For<ICounter>();
            sub.Increment().Returns(1);
            _ = sub.Increment();
        }
    }

    private static void SimpleSetup_FakeItEasy(int iterations)
    {
        for (int i = 0; i < iterations; i++)
        {
            var fake = A.Fake<ICounter>();
            A.CallTo(() => fake.Increment()).Returns(1);
            _ = fake.Increment();
        }
    }

    // ============================================================================
    // BENCHMARK 3: Multiple Setups
    // ============================================================================
    private static void MultipleSetups_Skugga(int iterations)
    {
        for (int i = 0; i < iterations; i++)
        {
            var mock = Skugga.Core.Mock.Create<IMessageBroker>();
            mock.Setup(x => x.SendMessage("hello")).Returns("ok");
            mock.Setup(x => x.IsConnected()).Returns(true);
            _ = mock.SendMessage("hello");
            _ = mock.IsConnected();
        }
    }

    private static void MultipleSetups_Moq(int iterations)
    {
        for (int i = 0; i < iterations; i++)
        {
            var mock = new Moq.Mock<IMessageBroker>();
            mock.Setup(x => x.SendMessage("hello")).Returns("ok");
            mock.Setup(x => x.IsConnected()).Returns(true);
            _ = mock.Object.SendMessage("hello");
            _ = mock.Object.IsConnected();
        }
    }

    private static void MultipleSetups_NSubstitute(int iterations)
    {
        for (int i = 0; i < iterations; i++)
        {
            var sub = Substitute.For<IMessageBroker>();
            sub.SendMessage("hello").Returns("ok");
            sub.IsConnected().Returns(true);
            _ = sub.SendMessage("hello");
            _ = sub.IsConnected();
        }
    }

    private static void MultipleSetups_FakeItEasy(int iterations)
    {
        for (int i = 0; i < iterations; i++)
        {
            var fake = A.Fake<IMessageBroker>();
            A.CallTo(() => fake.SendMessage("hello")).Returns("ok");
            A.CallTo(() => fake.IsConnected()).Returns(true);
            _ = fake.SendMessage("hello");
            _ = fake.IsConnected();
        }
    }

    // ============================================================================
    // BENCHMARK 4: Property-like Method (commonly used pattern)
    // ============================================================================
    private static void PropertyMethod_Skugga(int iterations)
    {
        for (int i = 0; i < iterations; i++)
        {
            var mock = Skugga.Core.Mock.Create<ICounter>();
            mock.Setup(x => x.GetValue()).Returns(42);
            _ = mock.GetValue();
        }
    }

    private static void PropertyMethod_Moq(int iterations)
    {
        for (int i = 0; i < iterations; i++)
        {
            var mock = new Moq.Mock<ICounter>();
            mock.Setup(x => x.GetValue()).Returns(42);
            _ = mock.Object.GetValue();
        }
    }

    private static void PropertyMethod_NSubstitute(int iterations)
    {
        for (int i = 0; i < iterations; i++)
        {
            var sub = Substitute.For<ICounter>();
            sub.GetValue().Returns(42);
            _ = sub.GetValue();
        }
    }

    private static void PropertyMethod_FakeItEasy(int iterations)
    {
        for (int i = 0; i < iterations; i++)
        {
            var fake = A.Fake<ICounter>();
            A.CallTo(() => fake.GetValue()).Returns(42);
            _ = fake.GetValue();
        }
    }
}
