using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using FakeItEasy;
using Moq;
using NSubstitute;
using Skugga.Core;

// Test interfaces for different scenarios
public interface ICalculator
{
    int Add(int a, int b);
    int Multiply(int a, int b);
}

public interface IRepository
{
    string GetData(int id);
    void SaveData(int id, string data);
    bool Exists(int id);
}

public interface IService
{
    string Process(int value);
    void Execute(string command);
}

[MemoryDiagnoser]
[SimpleJob(iterationCount: 10, warmupCount: 5)]
public class MockCreationBenchmarks
{
    [Benchmark(Baseline = true)]
    public object Skugga_CreateMock()
    {
        return Skugga.Core.Mock.Create<ICalculator>();
    }

    [Benchmark]
    public object Moq_CreateMock()
    {
        return new Moq.Mock<ICalculator>().Object;
    }

    [Benchmark]
    public object NSubstitute_CreateMock()
    {
        return Substitute.For<ICalculator>();
    }

    [Benchmark]
    public object FakeItEasy_CreateMock()
    {
        return A.Fake<ICalculator>();
    }
}

[MemoryDiagnoser]
[SimpleJob(iterationCount: 10, warmupCount: 5)]
public class SimpleSetupBenchmarks
{
    [Benchmark(Baseline = true)]
    public int Skugga_SimpleSetup()
    {
        var mock = Skugga.Core.Mock.Create<ICalculator>();
        mock.Setup(x => x.Add(1, 1)).Returns(2);
        return mock.Add(1, 1);
    }

    [Benchmark]
    public int Moq_SimpleSetup()
    {
        var mock = new Moq.Mock<ICalculator>();
        mock.Setup(x => x.Add(1, 1)).Returns(2);
        return mock.Object.Add(1, 1);
    }

    [Benchmark]
    public int NSubstitute_SimpleSetup()
    {
        var sub = Substitute.For<ICalculator>();
        sub.Add(1, 1).Returns(2);
        return sub.Add(1, 1);
    }

    [Benchmark]
    public int FakeItEasy_SimpleSetup()
    {
        var fake = A.Fake<ICalculator>();
        A.CallTo(() => fake.Add(1, 1)).Returns(2);
        return fake.Add(1, 1);
    }
}

[MemoryDiagnoser]
[SimpleJob(iterationCount: 10, warmupCount: 5)]
public class MultipleSetupsBenchmarks
{
    [Benchmark(Baseline = true)]
    public int Skugga_MultipleSetups()
    {
        var mock = Skugga.Core.Mock.Create<ICalculator>();
        mock.Setup(x => x.Add(1, 1)).Returns(2);
        mock.Setup(x => x.Add(2, 2)).Returns(4);
        mock.Setup(x => x.Multiply(3, 3)).Returns(9);
        return mock.Add(1, 1) + mock.Add(2, 2) + mock.Multiply(3, 3);
    }

    [Benchmark]
    public int Moq_MultipleSetups()
    {
        var mock = new Moq.Mock<ICalculator>();
        mock.Setup(x => x.Add(1, 1)).Returns(2);
        mock.Setup(x => x.Add(2, 2)).Returns(4);
        mock.Setup(x => x.Multiply(3, 3)).Returns(9);
        var obj = mock.Object;
        return obj.Add(1, 1) + obj.Add(2, 2) + obj.Multiply(3, 3);
    }

    [Benchmark]
    public int NSubstitute_MultipleSetups()
    {
        var sub = Substitute.For<ICalculator>();
        sub.Add(1, 1).Returns(2);
        sub.Add(2, 2).Returns(4);
        sub.Multiply(3, 3).Returns(9);
        return sub.Add(1, 1) + sub.Add(2, 2) + sub.Multiply(3, 3);
    }

    [Benchmark]
    public int FakeItEasy_MultipleSetups()
    {
        var fake = A.Fake<ICalculator>();
        A.CallTo(() => fake.Add(1, 1)).Returns(2);
        A.CallTo(() => fake.Add(2, 2)).Returns(4);
        A.CallTo(() => fake.Multiply(3, 3)).Returns(9);
        return fake.Add(1, 1) + fake.Add(2, 2) + fake.Multiply(3, 3);
    }
}

[MemoryDiagnoser]
[SimpleJob(iterationCount: 10, warmupCount: 5)]
public class ArgumentMatchingBenchmarks
{
    [Benchmark(Baseline = true)]
    public string Skugga_ArgumentMatching()
    {
        var mock = Skugga.Core.Mock.Create<IRepository>();
        mock.Setup(x => x.GetData(Skugga.Core.It.IsAny<int>())).Returns("data");
        return mock.GetData(42);
    }

    [Benchmark]
    public string Moq_ArgumentMatching()
    {
        var mock = new Moq.Mock<IRepository>();
        mock.Setup(x => x.GetData(Moq.It.IsAny<int>())).Returns("data");
        return mock.Object.GetData(42);
    }

    [Benchmark]
    public string NSubstitute_ArgumentMatching()
    {
        var sub = Substitute.For<IRepository>();
        sub.GetData(Arg.Any<int>()).Returns("data");
        return sub.GetData(42);
    }

    [Benchmark]
    public string FakeItEasy_ArgumentMatching()
    {
        var fake = A.Fake<IRepository>();
        A.CallTo(() => fake.GetData(A<int>._)).Returns("data");
        return fake.GetData(42);
    }
}

[MemoryDiagnoser]
[SimpleJob(iterationCount: 10, warmupCount: 5)]
public class VerifyBenchmarks
{
    [Benchmark(Baseline = true)]
    public void Skugga_Verify()
    {
        var mock = Skugga.Core.Mock.Create<IService>();
        mock.Execute("test");
        mock.Verify(x => x.Execute("test"), Skugga.Core.Times.Once());
    }

    [Benchmark]
    public void Moq_Verify()
    {
        var mock = new Moq.Mock<IService>();
        mock.Object.Execute("test");
        mock.Verify(x => x.Execute("test"), Moq.Times.Once());
    }

    [Benchmark]
    public void NSubstitute_Verify()
    {
        var sub = Substitute.For<IService>();
        sub.Execute("test");
        sub.Received(1).Execute("test");
    }

    [Benchmark]
    public void FakeItEasy_Verify()
    {
        var fake = A.Fake<IService>();
        fake.Execute("test");
        A.CallTo(() => fake.Execute("test")).MustHaveHappenedOnceExactly();
    }
}

// === COMPREHENSIVE FEATURE BENCHMARKS ===

// Test interfaces for new features
public interface IDataService
{
    string Name { get; set; }
    int Count { get; }
    event EventHandler DataChanged;
}

// Abstract class for protected members testing
public abstract class AbstractProcessor
{
    public string Process(string input)
    {
        return ProcessCore(input);
    }

    protected abstract string ProcessCore(string input);
    protected abstract int MaxRetries { get; }
}

[MemoryDiagnoser]
[SimpleJob(iterationCount: 10, warmupCount: 5)]
public class ProtectedMembersBenchmarks
{
    [Benchmark(Baseline = true)]
    public string Skugga_ProtectedMembers()
    {
        var mock = Skugga.Core.Mock.Create<AbstractProcessor>();
        mock.Protected()
            .Setup<string>("ProcessCore", Skugga.Core.It.IsAny<string>())
            .Returns("processed");
        return mock.Process("test");
    }

    [Benchmark]
    public string Moq_ProtectedMembers()
    {
        var mock = new Moq.Mock<AbstractProcessor>();
        mock.Protected()
            .Setup<string>("ProcessCore", Moq.It.IsAny<string>())
            .Returns("processed");
        return mock.Object.Process("test");
    }
}

[MemoryDiagnoser]
[SimpleJob(iterationCount: 10, warmupCount: 5)]
public class CallbackBenchmarks
{
    [Benchmark(Baseline = true)]
    public void Skugga_Callbacks()
    {
        var mock = Skugga.Core.Mock.Create<IService>();
        int counter = 0;
        mock.Setup(x => x.Execute(Skugga.Core.It.IsAny<string>()))
            .Callback((string s) => counter++);
        mock.Execute("test1");
        mock.Execute("test2");
    }

    [Benchmark]
    public void Moq_Callbacks()
    {
        var mock = new Moq.Mock<IService>();
        int counter = 0;
        mock.Setup(x => x.Execute(Moq.It.IsAny<string>()))
            .Callback<string>(x => counter++);
        mock.Object.Execute("test1");
        mock.Object.Execute("test2");
    }

    [Benchmark]
    public void NSubstitute_Callbacks()
    {
        var sub = Substitute.For<IService>();
        int counter = 0;
        sub.When(x => x.Execute(Arg.Any<string>())).Do(x => counter++);
        sub.Execute("test1");
        sub.Execute("test2");
    }

    [Benchmark]
    public void FakeItEasy_Callbacks()
    {
        var fake = A.Fake<IService>();
        int counter = 0;
        A.CallTo(() => fake.Execute(A<string>._))
            .Invokes((string x) => counter++);
        fake.Execute("test1");
        fake.Execute("test2");
    }
}

[MemoryDiagnoser]
[SimpleJob(iterationCount: 10, warmupCount: 5)]
public class PropertySetupBenchmarks
{
    [Benchmark(Baseline = true)]
    public string Skugga_Properties()
    {
        var mock = Skugga.Core.Mock.Create<IDataService>();
        mock.Setup(x => x.Name).Returns("Test");
        mock.Setup(x => x.Count).Returns(42);
        return mock.Name + mock.Count;
    }

    [Benchmark]
    public string Moq_Properties()
    {
        var mock = new Moq.Mock<IDataService>();
        mock.Setup(x => x.Name).Returns("Test");
        mock.Setup(x => x.Count).Returns(42);
        return mock.Object.Name + mock.Object.Count;
    }

    [Benchmark]
    public string NSubstitute_Properties()
    {
        var sub = Substitute.For<IDataService>();
        sub.Name.Returns("Test");
        sub.Count.Returns(42);
        return sub.Name + sub.Count;
    }

    [Benchmark]
    public string FakeItEasy_Properties()
    {
        var fake = A.Fake<IDataService>();
        A.CallTo(() => fake.Name).Returns("Test");
        A.CallTo(() => fake.Count).Returns(42);
        return fake.Name + fake.Count;
    }
}

[MemoryDiagnoser]
[SimpleJob(iterationCount: 10, warmupCount: 5)]
public class SequenceSetupBenchmarks
{
    [Benchmark(Baseline = true)]
    public int Skugga_Sequences()
    {
        var mock = Skugga.Core.Mock.Create<ICalculator>();
        mock.SetupSequence(x => x.Add(1, 1))
            .Returns(1)
            .Returns(2)
            .Returns(3);
        return mock.Add(1, 1) + mock.Add(1, 1) + mock.Add(1, 1);
    }

    [Benchmark]
    public int Moq_Sequences()
    {
        var mock = new Moq.Mock<ICalculator>();
        mock.SetupSequence(x => x.Add(1, 1))
            .Returns(1)
            .Returns(2)
            .Returns(3);
        var obj = mock.Object;
        return obj.Add(1, 1) + obj.Add(1, 1) + obj.Add(1, 1);
    }

    [Benchmark]
    public int NSubstitute_Sequences()
    {
        var sub = Substitute.For<ICalculator>();
        sub.Add(1, 1).Returns(1, 2, 3);
        return sub.Add(1, 1) + sub.Add(1, 1) + sub.Add(1, 1);
    }

    [Benchmark]
    public int FakeItEasy_Sequences()
    {
        var fake = A.Fake<ICalculator>();
        A.CallTo(() => fake.Add(1, 1)).ReturnsNextFromSequence(1, 2, 3);
        return fake.Add(1, 1) + fake.Add(1, 1) + fake.Add(1, 1);
    }
}

[MemoryDiagnoser]
[SimpleJob(iterationCount: 10, warmupCount: 5)]
public class AdvancedMatchersBenchmarks
{
    [Benchmark(Baseline = true)]
    public string Skugga_AdvancedMatchers()
    {
        var mock = Skugga.Core.Mock.Create<IRepository>();
        mock.Setup(x => x.GetData(Skugga.Core.It.Is<int>(n => n > 0))).Returns("positive");
        mock.Setup(x => x.GetData(Skugga.Core.It.IsIn(100, 200, 300))).Returns("special");
        return mock.GetData(5) + mock.GetData(100);
    }

    [Benchmark]
    public string Moq_AdvancedMatchers()
    {
        var mock = new Moq.Mock<IRepository>();
        mock.Setup(x => x.GetData(Moq.It.Is<int>(n => n > 0))).Returns("positive");
        mock.Setup(x => x.GetData(Moq.It.IsIn(100, 200, 300))).Returns("special");
        return mock.Object.GetData(5) + mock.Object.GetData(100);
    }

    [Benchmark]
    public string NSubstitute_AdvancedMatchers()
    {
        var sub = Substitute.For<IRepository>();
        sub.GetData(Arg.Is<int>(n => n > 0)).Returns("positive");
        sub.GetData(Arg.Is<int>(n => n == 100 || n == 200 || n == 300)).Returns("special");
        return sub.GetData(5) + sub.GetData(100);
    }

    [Benchmark]
    public string FakeItEasy_AdvancedMatchers()
    {
        var fake = A.Fake<IRepository>();
        A.CallTo(() => fake.GetData(A<int>.That.Matches(n => n > 0))).Returns("positive");
        A.CallTo(() => fake.GetData(A<int>.That.Matches(n => n == 100 || n == 200 || n == 300))).Returns("special");
        return fake.GetData(5) + fake.GetData(100);
    }
}

// Keep the original for backwards compatibility
[MemoryDiagnoser]
public class MockingBenchmarks
{
    [Benchmark(Baseline = true)]
    public int Skugga_Invoke()
    {
        var mock = Skugga.Core.Mock.Create<ICalculator>();
        mock.Setup(x => x.Add(1, 1)).Returns(2);
        return mock.Add(1, 1);
    }

    [Benchmark]
    public int Moq_Invoke()
    {
        var mock = new Moq.Mock<ICalculator>();
        mock.Setup(x => x.Add(1, 1)).Returns(2);
        return mock.Object.Add(1, 1);
    }

    [Benchmark]
    public int NSubstitute_Invoke()
    {
        var sub = Substitute.For<ICalculator>();
        sub.Add(1, 1).Returns(2);
        return sub.Add(1, 1);
    }

    [Benchmark]
    public int FakeItEasy_Invoke()
    {
        var fake = A.Fake<ICalculator>();
        A.CallTo(() => fake.Add(1, 1)).Returns(2);
        return fake.Add(1, 1);
    }
}
