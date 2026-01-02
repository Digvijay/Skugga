using System;
using Skugga.Core;
using System.Collections.Generic;
using BenchmarkDotNet.Running; // <--- REQUIRED for benchmarks

// --- DEFINITIONS ---
public interface IRepo { string GetData(int id); }
public class RealRepo : IRepo { 
    public string GetData(int id) => $"Real_Data_{id}"; 
}

public class Program
{
    public static void Main(string[] args)
    {
        #if DEBUG
            // In DEBUG mode, we run the Feature Demo showcasing Skugga's unique capabilities
            Console.WriteLine("=========================================");
            Console.WriteLine("   SKUGGA FEATURE SHOWCASE (v1.0)   ");
            Console.WriteLine("=========================================");
            Console.WriteLine("Demonstrating Skugga's unique features:");
            Console.WriteLine("  ✍️  AutoScribe - Self-writing tests");
            Console.WriteLine("  💥 Chaos Mode - Resilience testing");
            Console.WriteLine("  📉 ZeroAlloc Guard - Allocation detection");
            Console.WriteLine("=========================================\n");

            RunAutoScribeDemo();
            Console.WriteLine();
            RunChaosDemo();
            Console.WriteLine();
            RunZeroAllocDemo();
            
            Console.WriteLine("\n" + "=".PadRight(60, '='));
            Console.WriteLine("ℹ️  To run PERFORMANCE BENCHMARKS, use Release mode:");
            Console.WriteLine("   dotnet run --project src/Skugga.Benchmarks/Skugga.Benchmarks.csproj -c Release");
            Console.WriteLine("=".PadRight(60, '='));

        #else
            // In RELEASE mode, run comprehensive performance benchmarks
            Console.WriteLine("═".PadRight(100, '═'));
            Console.WriteLine("SKUGGA PERFORMANCE BENCHMARKS");
            Console.WriteLine("═".PadRight(100, '═'));
            Console.WriteLine();
            
            Console.WriteLine("PART 1: Comprehensive Moq vs Skugga Feature Comparison");
            MoqVsSkuggaBenchmarks.RunAll();
            
            Console.WriteLine("\n\nPART 2: Common Scenarios Across All 4 Frameworks");
            FourFrameworkBenchmarks.RunAll();
            
            Console.WriteLine("\n" + "═".PadRight(100, '═'));
            Console.WriteLine("ℹ️  Benchmark results saved to /benchmarks directory");
            Console.WriteLine("ℹ️  To run FEATURE DEMOS, use Debug mode:");
            Console.WriteLine("   dotnet run --project src/Skugga.Benchmarks/Skugga.Benchmarks.csproj");
            Console.WriteLine("═".PadRight(100, '═'));
        #endif
    }

    // --- FEATURE 1: AUTO-SCRIBE ---
    static void RunAutoScribeDemo()
    {
        Console.WriteLine("--- ✍️  Feature 1: Auto-Scribe (Self-Writing Tests) ---");
        var realRepo = new RealRepo();
        var recorder = AutoScribe.Capture<IRepo>(realRepo);

        Console.WriteLine("[Action] Running real code with recorder...");
        recorder.GetData(101);
        recorder.GetData(500);
        Console.WriteLine("[Result] Copy the lines above into your unit test!");
    }

    // --- FEATURE 2: CHAOS MODE ---
    static void RunChaosDemo()
    {
        Console.WriteLine("--- 💥 Feature 2: Chaos Mode (Resilience) ---");
        var mock = Mock.Create<IRepo>();
        mock.Setup(x => x.GetData(1)).Returns("Success!");

        Console.WriteLine("[Config] Injecting 50% failure rate...");
        mock.Chaos(policy => {
            policy.FailureRate = 0.5;
            policy.PossibleExceptions = new Exception[] { new TimeoutException("DB Timeout") };
        });

        int success = 0; int failures = 0;
        Console.Write("[Action] Invoking mock 20 times: ");
        for(int i=0; i<20; i++)
        {
            try { mock.GetData(1); success++; Console.Write("."); }
            catch(TimeoutException) { failures++; Console.Write("X"); }
        }
        Console.WriteLine($"\n[Result] Success: {success}, Failures: {failures}");
        Console.WriteLine(failures > 0 ? "✅ Resilience test passed." : "⚠️ RNG bad luck, try again.");
    }

    // --- FEATURE 3: ZERO-ALLOC GUARD ---
    static void RunZeroAllocDemo()
    {
        Console.WriteLine("--- 📉 Feature 3: Zero-Alloc Guard ---");

        Console.WriteLine("[Test 1] Testing a zero-allocation method...");
        try {
            AssertAllocations.Zero(() => { int a=10; int b=20; int c=a+b; });
            Console.WriteLine("✅ Success: No allocations detected.");
        } catch(Exception ex) { Console.WriteLine($"❌ Failed: {ex.Message}"); }

        Console.WriteLine("[Test 2] Testing a method that allocates...");
        try {
            AssertAllocations.Zero(() => { var list = new List<int>(); });
            Console.WriteLine("❌ Failed: Should have caught allocation.");
        } catch(Exception ex) { Console.WriteLine($"✅ Caught Expected Allocation: {ex.Message}"); }
    }
}