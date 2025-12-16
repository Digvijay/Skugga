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
            // In DEBUG mode, we run the Functional Demo
            Console.WriteLine("=========================================");
            Console.WriteLine("      SKUGGA TRINITY DEMO (v1.0)      ");
            Console.WriteLine("=========================================");

            RunAutoScribeDemo();
            Console.WriteLine();
            RunChaosDemo();
            Console.WriteLine();
            RunZeroAllocDemo();
            
            Console.WriteLine("\n---------------------------------------------");
            Console.WriteLine("ℹ️  To run BENCHMARKS, use Release mode:");
            Console.WriteLine("   dotnet run -c Release --project Skugga.Benchmarks/Skugga.Benchmarks.csproj");
            Console.WriteLine("---------------------------------------------");

        #else
            // In RELEASE mode, we run the Benchmarks
            Console.WriteLine("🚀 Running Benchmarks in Release Mode...");
            BenchmarkRunner.Run<MockingBenchmarks>();
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