using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Moq;
using Skugga.Core;

public interface IUserService
{
    // Method with Args
    string GetRole(int userId);
    
    // Property
    string TenantName { get; set; }
}

[MemoryDiagnoser] 
public class MockBenchmarks
{
    [Benchmark(Baseline = true)]
    public string Moq_Benchmark()
    {
        var moq = new Mock<IUserService>();
        moq.Setup(x => x.GetRole(1)).Returns("Admin");
        return moq.Object.GetRole(1);
    }

    [Benchmark]
    public string Skugga_Benchmark()
    {
        var skugga = Skugga.Core.Mock.Create<IUserService>();
        skugga.Setup(x => x.GetRole(1)).Returns("Admin");
        return skugga.GetRole(1);
    }
}

public class Program
{
    public static void Main(string[] args)
    {
        Console.WriteLine("--- Functional Test (New Features) ---");
        var mock = Skugga.Core.Mock.Create<IUserService>();

        // 1. Test Argument Matching
        mock.Setup(x => x.GetRole(1)).Returns("Admin");
        mock.Setup(x => x.GetRole(99)).Returns("Guest");

        var role1 = mock.GetRole(1);
        var role99 = mock.GetRole(99);
        var roleUnknown = mock.GetRole(500); // Should be null

        Console.WriteLine($"Args Match (1): {role1} (Expected: Admin)");
        Console.WriteLine($"Args Match (99): {role99} (Expected: Guest)");
        Console.WriteLine($"Args Match (500): '{roleUnknown}' (Expected: '')");

        if (role1 == "Admin" && role99 == "Guest") 
            Console.WriteLine("✅ SUCCESS: Argument Matching Works!");
        else 
            Console.WriteLine("❌ FAIL: Argument Matching Failed.");

        // 2. Test Property Support
        mock.Setup(x => x.TenantName).Returns("Microsoft");
        var tenant = mock.TenantName;
        
        Console.WriteLine($"Property: {tenant} (Expected: Microsoft)");

        if (tenant == "Microsoft") 
            Console.WriteLine("✅ SUCCESS: Property Mocking Works!");
        else 
            Console.WriteLine("❌ FAIL: Property Mocking Failed.");

        // Uncomment to run benchmarks:
        BenchmarkRunner.Run<MockBenchmarks>();
    }
}