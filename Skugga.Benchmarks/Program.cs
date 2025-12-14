using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Moq;
using Skugga.Core;

// THIS INTERFACE IS IN THE GLOBAL NAMESPACE
// The generator now handles this via FullyQualifiedFormat
public interface IEmailService
{
    string GetEmailAddress(int userId);
}

[MemoryDiagnoser] 
public class MockBenchmarks
{
    [Benchmark(Baseline = true)]
    public string Moq_Benchmark()
    {
        var moq = new Mock<IEmailService>();
        moq.Setup(x => x.GetEmailAddress(It.IsAny<int>())).Returns("david@microsoft.com");
        return moq.Object.GetEmailAddress(1);
    }

    [Benchmark]
    public string Skugga_Benchmark()
    {
        var skugga = Skugga.Core.Mock.Create<IEmailService>();
        skugga.Setup(x => x.GetEmailAddress(0)).Returns("david@microsoft.com");
        return skugga.GetEmailAddress(1);
    }
}

public class Program
{
    public static void Main(string[] args)
    {
        Console.WriteLine("--- Functional Test ---");
        var service = Skugga.Core.Mock.Create<IEmailService>();
        service.Setup(x => x.GetEmailAddress(0)).Returns("fowler@asp.net");
        var result = service.GetEmailAddress(99);
        
        if (result == "fowler@asp.net") Console.WriteLine("✅ SUCCESS: Skugga mock returned correct value.");
        else Console.WriteLine($"❌ FAIL: Got '{result}'");

        // Uncomment to run benchmarks:
        // BenchmarkRunner.Run<MockBenchmarks>();
    }
}
