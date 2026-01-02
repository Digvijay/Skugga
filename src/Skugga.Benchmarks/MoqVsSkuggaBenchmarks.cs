using System;
using System.Diagnostics;
using Skugga.Core;
using Moq;

// Interfaces for comprehensive testing  
public interface IMathCalc { int Add(int a, int b); int Multiply(int a, int b); int Divide(int a, int b); }
public interface IDataStore { string GetData(int id); void SaveData(int id, string data); bool Exists(int id); }
public interface IWorkflow { string Process(int value); void Execute(string command); int Calculate(int x, int y); }
public interface ITraceLog { void Log(string message); void LogError(string message); }

/// <summary>
/// Comprehensive benchmarks comparing Moq vs Skugga across all features
/// </summary>
public class MoqVsSkuggaBenchmarks
{
    private const int Iterations = 50_000;
    private const int Warmup = 5_000;

    public static void RunAll()
    {
        var output = new System.Text.StringBuilder();
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        
        output.AppendLine("# Comprehensive Moq vs Skugga Benchmarks\n");
        output.AppendLine($"**Test Date:** {timestamp}  ");
        output.AppendLine($"**Iterations:** {Iterations:N0} | **Warmup:** {Warmup:N0}  ");
        output.AppendLine($"**Hardware:** Intel Core i7-4980HQ @ 2.80GHz, 16GB RAM  ");
        output.AppendLine($"**OS:** macOS 15.7  ");
        output.AppendLine($"**Runtime:** .NET 10.0.1\n");
        output.AppendLine("---\n");
        
        Console.WriteLine(output.ToString());

        var results = new (string Name, double Skugga, double Moq)[]
        {
            Benchmark("1. Simple Mock Creation", CreateMock_Skugga, CreateMock_Moq),
            Benchmark("2. Setup with Returns", SetupReturns_Skugga, SetupReturns_Moq),
            Benchmark("3. Multiple Setups", MultipleSetups_Skugga, MultipleSetups_Moq),
            Benchmark("4. Argument Matching (It.IsAny)", ArgumentMatching_Skugga, ArgumentMatching_Moq),
            Benchmark("5. Verify Once", VerifyOnce_Skugga, VerifyOnce_Moq),
            Benchmark("6. Verify Never", VerifyNever_Skugga, VerifyNever_Moq),
            Benchmark("7. Callback Execution", Callback_Skugga, Callback_Moq),
            Benchmark("8. Property Setup", PropertySetup_Skugga, PropertySetup_Moq),
            Benchmark("9. Sequential Returns", SequentialReturns_Skugga, SequentialReturns_Moq),
            Benchmark("10. Advanced Matchers (It.Is)", AdvancedMatchers_Skugga, AdvancedMatchers_Moq),
            Benchmark("11. Void Method Setup", VoidMethod_Skugga, VoidMethod_Moq),
            Benchmark("12. Complex Scenario", ComplexScenario_Skugga, ComplexScenario_Moq)
        };

        PrintResults(results, output);
        
        // Save to file
        var benchmarkDir = Path.Combine(Directory.GetCurrentDirectory(), "benchmarks");
        Directory.CreateDirectory(benchmarkDir);
        var filePath = Path.Combine(benchmarkDir, "MoqVsSkugga.md");
        File.WriteAllText(filePath, output.ToString());
        Console.WriteLine($"\n📊 Results saved to: {filePath}");
    }

    private static (string Name, double Skugga, double Moq) Benchmark(string name, Action<int> skuggaAction, Action<int> moqAction)
    {
        var (skugga, moq) = RunBenchmark(skuggaAction, moqAction);
        return (name, skugga, moq);
    }

    private static (double Skugga, double Moq) RunBenchmark(Action<int> skuggaAction, Action<int> moqAction)
    {
        // Warmup
        skuggaAction(Warmup);
        moqAction(Warmup);
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

        return (swSkugga.Elapsed.TotalMilliseconds, swMoq.Elapsed.TotalMilliseconds);
    }

    private static void PrintResults((string Name, double Skugga, double Moq)[] results, System.Text.StringBuilder output)
    {
        output.AppendLine("\n" + "=".PadRight(100, '='));
        output.AppendLine("RESULTS");
        output.AppendLine("=".PadRight(100, '='));
        output.AppendLine();
        output.AppendLine($"{"Benchmark",-45} {"Skugga (ms)",15} {"Moq (ms)",15} {"Speedup",12}");
        output.AppendLine("-".PadRight(100, '-'));

        double totalSkugga = 0, totalMoq = 0;
        foreach (var (name, skugga, moq) in results)
        {
            var speedup = moq / skugga;
            output.AppendLine($"{name,-45} {skugga,15:F2} {moq,15:F2} {speedup,11:F2}x");
            totalSkugga += skugga;
            totalMoq += moq;
        }

        output.AppendLine("-".PadRight(100, '-'));
        output.AppendLine($"{"TOTAL",-45} {totalSkugga,15:F2} {totalMoq,15:F2} {(totalMoq/totalSkugga),11:F2}x");
        output.AppendLine();
        output.AppendLine("=".PadRight(100, '='));
        output.AppendLine($"✓ Overall: Skugga is {(totalMoq/totalSkugga):F2}x faster than Moq");
        output.AppendLine("=".PadRight(100, '='));
        
        Console.Write(output.ToString());
    }

    // ============================================================================
    // BENCHMARK 1: Simple Mock Creation
    // ============================================================================
    private static void CreateMock_Skugga(int iterations)
    {
        for (int i = 0; i < iterations; i++)
        {
            _ = Skugga.Core.Mock.Create<IMathCalc>();
        }
    }

    private static void CreateMock_Moq(int iterations)
    {
        for (int i = 0; i < iterations; i++)
        {
            _ = new Moq.Mock<IMathCalc>().Object;
        }
    }

    // ============================================================================
    // BENCHMARK 2: Setup with Returns
    // ============================================================================
    private static void SetupReturns_Skugga(int iterations)
    {
        for (int i = 0; i < iterations; i++)
        {
            var mock = Skugga.Core.Mock.Create<IMathCalc>();
            mock.Setup(x => x.Add(1, 2)).Returns(3);
            _ = mock.Add(1, 2);
        }
    }

    private static void SetupReturns_Moq(int iterations)
    {
        for (int i = 0; i < iterations; i++)
        {
            var mock = new Moq.Mock<IMathCalc>();
            mock.Setup(x => x.Add(1, 2)).Returns(3);
            _ = mock.Object.Add(1, 2);
        }
    }

    // ============================================================================
    // BENCHMARK 3: Multiple Setups
    // ============================================================================
    private static void MultipleSetups_Skugga(int iterations)
    {
        for (int i = 0; i < iterations; i++)
        {
            var mock = Skugga.Core.Mock.Create<IMathCalc>();
            mock.Setup(x => x.Add(1, 2)).Returns(3);
            mock.Setup(x => x.Multiply(2, 3)).Returns(6);
            mock.Setup(x => x.Divide(10, 2)).Returns(5);
            _ = mock.Add(1, 2);
            _ = mock.Multiply(2, 3);
            _ = mock.Divide(10, 2);
        }
    }

    private static void MultipleSetups_Moq(int iterations)
    {
        for (int i = 0; i < iterations; i++)
        {
            var mock = new Moq.Mock<IMathCalc>();
            mock.Setup(x => x.Add(1, 2)).Returns(3);
            mock.Setup(x => x.Multiply(2, 3)).Returns(6);
            mock.Setup(x => x.Divide(10, 2)).Returns(5);
            _ = mock.Object.Add(1, 2);
            _ = mock.Object.Multiply(2, 3);
            _ = mock.Object.Divide(10, 2);
        }
    }

    // ============================================================================
    // BENCHMARK 4: Argument Matching (Skugga.Core.It.IsAny)
    // ============================================================================
    private static void ArgumentMatching_Skugga(int iterations)
    {
        for (int i = 0; i < iterations; i++)
        {
            var mock = Skugga.Core.Mock.Create<IDataStore>();
            mock.Setup(x => x.GetData(Skugga.Core.It.IsAny<int>())).Returns("data");
            _ = mock.GetData(42);
        }
    }

    private static void ArgumentMatching_Moq(int iterations)
    {
        for (int i = 0; i < iterations; i++)
        {
            var mock = new Moq.Mock<IDataStore>();
            mock.Setup(x => x.GetData(Moq.It.IsAny<int>())).Returns("data");
            _ = mock.Object.GetData(42);
        }
    }

    // ============================================================================
    // BENCHMARK 5: Verify Once
    // ============================================================================
    private static void VerifyOnce_Skugga(int iterations)
    {
        for (int i = 0; i < iterations; i++)
        {
            var mock = Skugga.Core.Mock.Create<IWorkflow>();
            mock.Execute("test");
            mock.Verify(x => x.Execute("test"), Skugga.Core.Times.Once());
        }
    }

    private static void VerifyOnce_Moq(int iterations)
    {
        for (int i = 0; i < iterations; i++)
        {
            var mock = new Moq.Mock<IWorkflow>();
            mock.Object.Execute("test");
            mock.Verify(x => x.Execute("test"), Moq.Times.Once());
        }
    }

    // ============================================================================
    // BENCHMARK 6: Verify Never
    // ============================================================================
    private static void VerifyNever_Skugga(int iterations)
    {
        for (int i = 0; i < iterations; i++)
        {
            var mock = Skugga.Core.Mock.Create<IWorkflow>();
            mock.Verify(x => x.Execute("test"), Skugga.Core.Times.Never());
        }
    }

    private static void VerifyNever_Moq(int iterations)
    {
        for (int i = 0; i < iterations; i++)
        {
            var mock = new Moq.Mock<IWorkflow>();
            mock.Verify(x => x.Execute("test"), Moq.Times.Never());
        }
    }

    // ============================================================================
    // BENCHMARK 7: Callback Execution
    // ============================================================================
    private static void Callback_Skugga(int iterations)
    {
        int counter = 0;
        for (int i = 0; i < iterations; i++)
        {
            var mock = Skugga.Core.Mock.Create<IWorkflow>();
            mock.Setup(x => x.Process(Skugga.Core.It.IsAny<int>())).Returns("result").Callback(() => counter++);
            _ = mock.Process(42);
        }
    }

    private static void Callback_Moq(int iterations)
    {
        int counter = 0;
        for (int i = 0; i < iterations; i++)
        {
            var mock = new Moq.Mock<IWorkflow>();
            mock.Setup(x => x.Process(Moq.It.IsAny<int>())).Returns("result").Callback(() => counter++);
            _ = mock.Object.Process(42);
        }
    }

    // ============================================================================
    // BENCHMARK 8: Property Setup (using methods as proxies)
    // ============================================================================
    private static void PropertySetup_Skugga(int iterations)
    {
        for (int i = 0; i < iterations; i++)
        {
            var mock = Skugga.Core.Mock.Create<IWorkflow>();
            mock.Setup(x => x.Calculate(1, 2)).Returns(3);
            _ = mock.Calculate(1, 2);
        }
    }

    private static void PropertySetup_Moq(int iterations)
    {
        for (int i = 0; i < iterations; i++)
        {
            var mock = new Moq.Mock<IWorkflow>();
            mock.Setup(x => x.Calculate(1, 2)).Returns(3);
            _ = mock.Object.Calculate(1, 2);
        }
    }

    // ============================================================================
    // BENCHMARK 9: Sequential Returns
    // ============================================================================
    private static void SequentialReturns_Skugga(int iterations)
    {
        for (int i = 0; i < iterations; i++)
        {
            var mock = Skugga.Core.Mock.Create<IMathCalc>();
            mock.Setup(x => x.Add(1, 1)).ReturnsInOrder(2, 3, 4);
            _ = mock.Add(1, 1);
            _ = mock.Add(1, 1);
            _ = mock.Add(1, 1);
        }
    }

    private static void SequentialReturns_Moq(int iterations)
    {
        for (int i = 0; i < iterations; i++)
        {
            var mock = new Moq.Mock<IMathCalc>();
            mock.SetupSequence(x => x.Add(1, 1)).Returns(2).Returns(3).Returns(4);
            _ = mock.Object.Add(1, 1);
            _ = mock.Object.Add(1, 1);
            _ = mock.Object.Add(1, 1);
        }
    }

    // ============================================================================
    // BENCHMARK 10: Advanced Matchers (It.Is)
    // ============================================================================
    private static void AdvancedMatchers_Skugga(int iterations)
    {
        for (int i = 0; i < iterations; i++)
        {
            var mock = Skugga.Core.Mock.Create<IDataStore>();
            mock.Setup(x => x.GetData(Skugga.Core.It.Is<int>(n => n > 10))).Returns("large");
            _ = mock.GetData(42);
        }
    }

    private static void AdvancedMatchers_Moq(int iterations)
    {
        for (int i = 0; i < iterations; i++)
        {
            var mock = new Moq.Mock<IDataStore>();
            mock.Setup(x => x.GetData(Moq.It.Is<int>(n => n > 10))).Returns("large");
            _ = mock.Object.GetData(42);
        }
    }

    // ============================================================================
    // BENCHMARK 11: Void Method Setup
    // ============================================================================
    private static void VoidMethod_Skugga(int iterations)
    {
        for (int i = 0; i < iterations; i++)
        {
            var mock = Skugga.Core.Mock.Create<ITraceLog>();
            mock.Setup(x => x.Log(Skugga.Core.It.IsAny<string>()));
            mock.Log("test message");
        }
    }

    private static void VoidMethod_Moq(int iterations)
    {
        for (int i = 0; i < iterations; i++)
        {
            var mock = new Moq.Mock<ITraceLog>();
            mock.Setup(x => x.Log(Moq.It.IsAny<string>()));
            mock.Object.Log("test message");
        }
    }

    // ============================================================================
    // BENCHMARK 12: Complex Scenario (Multiple features combined)
    // ============================================================================
    private static void ComplexScenario_Skugga(int iterations)
    {
        for (int i = 0; i < iterations; i++)
        {
            var mock = Skugga.Core.Mock.Create<IDataStore>();
            mock.Setup(x => x.Exists(Skugga.Core.It.IsAny<int>())).Returns(true);
            mock.Setup(x => x.GetData(Skugga.Core.It.Is<int>(n => n > 0))).Returns("data");
            
            _ = mock.Exists(1);
            _ = mock.GetData(42);
            mock.SaveData(1, "test");
            
            mock.Verify(x => x.Exists(Skugga.Core.It.IsAny<int>()), Skugga.Core.Times.Once());
            mock.Verify(x => x.SaveData(1, "test"), Skugga.Core.Times.Once());
        }
    }

    private static void ComplexScenario_Moq(int iterations)
    {
        for (int i = 0; i < iterations; i++)
        {
            var mock = new Moq.Mock<IDataStore>();
            mock.Setup(x => x.Exists(Moq.It.IsAny<int>())).Returns(true);
            mock.Setup(x => x.GetData(Moq.It.Is<int>(n => n > 0))).Returns("data");
            
            _ = mock.Object.Exists(1);
            _ = mock.Object.GetData(42);
            mock.Object.SaveData(1, "test");
            
            mock.Verify(x => x.Exists(Moq.It.IsAny<int>()), Moq.Times.Once());
            mock.Verify(x => x.SaveData(1, "test"), Moq.Times.Once());
        }
    }
}
