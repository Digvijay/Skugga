using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Skugga.Core; // Brings in extension methods (.Setup)
using Moq;
using NSubstitute;

public interface ICalculator
{
    int Add(int a, int b);
}

[MemoryDiagnoser]
public class MockingBenchmarks
{
    // 1. Skugga (Compile-Time)
    [Benchmark(Baseline = true)]
    public int Skugga_Invoke()
    {
        // Explicitly use Skugga.Core.Mock to avoid conflict with Moq
        var mock = Skugga.Core.Mock.Create<ICalculator>();
        
        // This .Setup works because of 'using Skugga.Core'
        mock.Setup(x => x.Add(1, 1)).Returns(2);
        
        return mock.Add(1, 1);
    }

    // 2. Moq (Runtime)
    [Benchmark]
    public int Moq_Invoke()
    {
        // Explicitly use Moq.Mock
        var mock = new Moq.Mock<ICalculator>();
        mock.Setup(x => x.Add(1, 1)).Returns(2);
        return mock.Object.Add(1, 1);
    }

    // 3. NSubstitute (Runtime)
    [Benchmark]
    public int NSubstitute_Invoke()
    {
        var sub = Substitute.For<ICalculator>();
        sub.Add(1, 1).Returns(2);
        return sub.Add(1, 1);
    }
}