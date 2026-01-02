using Skugga.Core;
using System.Reflection;

namespace Skugga.Core.Tests;

/// <summary>
/// Tests for AutoScribe - self-writing test feature
/// Now using compile-time generated recording proxies instead of reflection-based DispatchProxy
/// </summary>
public class AutoScribeTests
{
    public interface ICalculator
    {
        int Add(int a, int b);
        string GetName();
    }

    public class RealCalculator : ICalculator
    {
        public int Add(int a, int b) => a + b;
        public string GetName() => "Calculator";
    }

    [Fact]
    public void Capture_ShouldWrapRealInstance()
    {
        // Arrange
        var real = new RealCalculator();

        // Act
        var recorder = AutoScribe.Capture<ICalculator>(real);

        // Assert
        recorder.Should().NotBeNull();
        recorder.Should().BeAssignableTo<ICalculator>();
    }

    [Fact]
    public void Capture_ShouldDelegateCallsToRealInstance()
    {
        // Arrange
        var real = new RealCalculator();
        var recorder = AutoScribe.Capture<ICalculator>(real);

        // Act
        var result = recorder.Add(2, 3);

        // Assert
        result.Should().Be(5);
    }

    [Fact]
    public void Capture_ShouldOutputRecordingToConsole()
    {
        // Arrange
        var real = new RealCalculator();
        var recorder = AutoScribe.Capture<ICalculator>(real);
        var output = new StringWriter();
        Console.SetOut(output);

        // Act
        _ = recorder.Add(5, 10);

        // Assert
        var consoleOutput = output.ToString();
        consoleOutput.Should().Contain("[AutoScribe]");
        consoleOutput.Should().Contain("Add");
        consoleOutput.Should().Contain("5");
        consoleOutput.Should().Contain("10");

        // Cleanup
        Console.SetOut(Console.Out);
    }

    [Fact]
    public void Capture_WithStringMethod_ShouldRecordStringParameter()
    {
        // Arrange
        var real = new RealCalculator();
        var recorder = AutoScribe.Capture<ICalculator>(real);
        var output = new StringWriter();
        Console.SetOut(output);

        // Act
        _ = recorder.GetName();

        // Assert
        var consoleOutput = output.ToString();
        consoleOutput.Should().Contain("[AutoScribe]");
        consoleOutput.Should().Contain("GetName");

        // Cleanup
        Console.SetOut(Console.Out);
    }

    // === Advanced Tests for Generics, Complex Types, Async, etc. ===

    public record Person(string Name, int Age);
    public record Address(string Street, string City);

    public interface IRepository<T>
    {
        T GetById(int id);
        List<T> GetAll();
        Task<T> GetByIdAsync(int id);
        Task<List<T>> GetAllAsync();
        void Save(T item);
        Task SaveAsync(T item);
    }

    public class FakePersonRepository : IRepository<Person>
    {
        public Person GetById(int id) => new Person($"Person{id}", id * 10);
        public List<Person> GetAll() => new List<Person> { new("Alice", 30), new("Bob", 25) };
        public Task<Person> GetByIdAsync(int id) => Task.FromResult(new Person($"PersonAsync{id}", id * 10));
        public Task<List<Person>> GetAllAsync() => Task.FromResult(new List<Person> { new("Alice", 30), new("Bob", 25) });
        public void Save(Person item) { }
        public Task SaveAsync(Person item) => Task.CompletedTask;
    }

    public interface IComplexService
    {
        Person? GetPerson(int id);
        Address GetAddress(Person person);
        Dictionary<string, int> GetScores();
        (bool success, string message) ProcessData(int[] data);
        IEnumerable<int> GetNumbers();
    }

    public class FakeComplexService : IComplexService
    {
        public Person? GetPerson(int id) => id > 0 ? new Person("Test", id) : null;
        public Address GetAddress(Person person) => new Address("123 Main", person.Name);
        public Dictionary<string, int> GetScores() => new Dictionary<string, int> { ["Alice"] = 100, ["Bob"] = 90 };
        public (bool success, string message) ProcessData(int[] data) => (true, $"Processed {data.Length} items");
        public IEnumerable<int> GetNumbers() => new[] { 1, 2, 3 };
    }

    [Fact]
    public void Capture_WithGenericInterface_ShouldWork()
    {
        // Arrange
        var real = new FakePersonRepository();

        // Act
        var recorder = AutoScribe.Capture<IRepository<Person>>(real);
        var result = recorder.GetById(5);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Person5");
        result.Age.Should().Be(50);
    }

    [Fact]
    public void Capture_WithGenericList_ShouldRecordCorrectly()
    {
        // Arrange
        var real = new FakePersonRepository();
        var recorder = AutoScribe.Capture<IRepository<Person>>(real);
        var output = new StringWriter();
        Console.SetOut(output);

        // Act
        var result = recorder.GetAll();

        // Assert
        result.Should().HaveCount(2);
        var consoleOutput = output.ToString();
        consoleOutput.Should().Contain("[AutoScribe]");
        consoleOutput.Should().Contain("GetAll");

        // Cleanup
        Console.SetOut(Console.Out);
    }

    [Fact]
    public async Task Capture_WithAsyncMethod_ShouldWork()
    {
        // Arrange
        var real = new FakePersonRepository();
        var recorder = AutoScribe.Capture<IRepository<Person>>(real);

        // Act
        var result = await recorder.GetByIdAsync(42);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("PersonAsync42");
        result.Age.Should().Be(420);
    }

    [Fact]
    public async Task Capture_WithAsyncListMethod_ShouldWork()
    {
        // Arrange
        var real = new FakePersonRepository();
        var recorder = AutoScribe.Capture<IRepository<Person>>(real);

        // Act
        var result = await recorder.GetAllAsync();

        // Assert
        result.Should().HaveCount(2);
        result[0].Name.Should().Be("Alice");
    }

    [Fact]
    public void Capture_WithVoidMethod_ShouldRecord()
    {
        // Arrange
        var real = new FakePersonRepository();
        var recorder = AutoScribe.Capture<IRepository<Person>>(real);
        var output = new StringWriter();
        Console.SetOut(output);

        // Act
        recorder.Save(new Person("Test", 25));

        // Assert
        var consoleOutput = output.ToString();
        consoleOutput.Should().Contain("[AutoScribe]");
        consoleOutput.Should().Contain("Save");

        // Cleanup
        Console.SetOut(Console.Out);
    }

    [Fact]
    public async Task Capture_WithAsyncVoidMethod_ShouldRecord()
    {
        // Arrange
        var real = new FakePersonRepository();
        var recorder = AutoScribe.Capture<IRepository<Person>>(real);
        var output = new StringWriter();
        Console.SetOut(output);

        // Act
        await recorder.SaveAsync(new Person("Test", 25));

        // Assert
        var consoleOutput = output.ToString();
        consoleOutput.Should().Contain("[AutoScribe]");
        consoleOutput.Should().Contain("SaveAsync");

        // Cleanup
        Console.SetOut(Console.Out);
    }

    [Fact]
    public void Capture_WithNullableType_ShouldHandleNull()
    {
        // Arrange
        var real = new FakeComplexService();
        var recorder = AutoScribe.Capture<IComplexService>(real);
        var output = new StringWriter();
        Console.SetOut(output);

        // Act
        var result = recorder.GetPerson(0);

        // Assert
        result.Should().BeNull();
        var consoleOutput = output.ToString();
        consoleOutput.Should().Contain("[AutoScribe]");
        consoleOutput.Should().Contain("null");

        // Cleanup
        Console.SetOut(Console.Out);
    }

    [Fact]
    public void Capture_WithNullableType_ShouldHandleValue()
    {
        // Arrange
        var real = new FakeComplexService();
        var recorder = AutoScribe.Capture<IComplexService>(real);

        // Act
        var result = recorder.GetPerson(5);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Test");
        result.Age.Should().Be(5);
    }

    [Fact]
    public void Capture_WithComplexReturnType_ShouldWork()
    {
        // Arrange
        var real = new FakeComplexService();
        var recorder = AutoScribe.Capture<IComplexService>(real);

        // Act
        var result = recorder.GetAddress(new Person("Alice", 30));

        // Assert
        result.Should().NotBeNull();
        result.Street.Should().Be("123 Main");
        result.City.Should().Be("Alice");
    }

    [Fact]
    public void Capture_WithDictionaryReturnType_ShouldWork()
    {
        // Arrange
        var real = new FakeComplexService();
        var recorder = AutoScribe.Capture<IComplexService>(real);

        // Act
        var result = recorder.GetScores();

        // Assert
        result.Should().NotBeNull();
        result.Should().ContainKey("Alice");
        result["Alice"].Should().Be(100);
    }

    [Fact]
    public void Capture_WithTupleReturnType_ShouldWork()
    {
        // Arrange
        var real = new FakeComplexService();
        var recorder = AutoScribe.Capture<IComplexService>(real);

        // Act
        var result = recorder.ProcessData(new[] { 1, 2, 3 });

        // Assert
        result.success.Should().BeTrue();
        result.message.Should().Contain("Processed 3 items");
    }

    [Fact]
    public void Capture_WithArrayParameter_ShouldWork()
    {
        // Arrange
        var real = new FakeComplexService();
        var recorder = AutoScribe.Capture<IComplexService>(real);
        var output = new StringWriter();
        Console.SetOut(output);

        // Act
        _ = recorder.ProcessData(new[] { 10, 20, 30 });

        // Assert
        var consoleOutput = output.ToString();
        consoleOutput.Should().Contain("[AutoScribe]");
        consoleOutput.Should().Contain("ProcessData");

        // Cleanup
        Console.SetOut(Console.Out);
    }

    [Fact]
    public void Capture_WithIEnumerableReturnType_ShouldWork()
    {
        // Arrange
        var real = new FakeComplexService();
        var recorder = AutoScribe.Capture<IComplexService>(real);

        // Act
        var result = recorder.GetNumbers();

        // Assert
        result.Should().NotBeNull();
        result.Should().Equal(1, 2, 3);
    }

    [Fact]
    public void Capture_WithComplexParameter_ShouldSerialize()
    {
        // Arrange
        var real = new FakeComplexService();
        var recorder = AutoScribe.Capture<IComplexService>(real);
        var output = new StringWriter();
        Console.SetOut(output);

        // Act
        _ = recorder.GetAddress(new Person("Bob", 40));

        // Assert
        var consoleOutput = output.ToString();
        consoleOutput.Should().Contain("[AutoScribe]");
        consoleOutput.Should().Contain("GetAddress");
        // Complex objects will be serialized via ToString()

        // Cleanup
        Console.SetOut(Console.Out);
    }
    
    [Fact]
    public void ExportToJson_ShouldFormatRecordingsProperly()
    {
        // Arrange
        var recordings = new List<RecordedCall>
        {
            new RecordedCall 
            { 
                MethodName = "Add", 
                Arguments = new object?[] { 1, 2 }, 
                Result = 3,
                DurationMilliseconds = 10
            },
            new RecordedCall 
            { 
                MethodName = "GetName", 
                Arguments = Array.Empty<object?>(), 
                Result = "Test",
                DurationMilliseconds = 5
            }
        };
        
        // Act
        var json = AutoScribe.ExportToJson(recordings);
        
        // Assert
        json.Should().Contain("\"Method\":\"Add\"");
        json.Should().Contain("\"Args\":[\"1\",\"2\"]");
        json.Should().Contain("\"Result\":\"3\"");
        json.Should().Contain("\"Duration\":10");
        json.Should().Contain("\"Method\":\"GetName\"");
    }
    
    [Fact]
    public void ExportToCsv_ShouldFormatRecordingsProperly()
    {
        // Arrange
        var recordings = new List<RecordedCall>
        {
            new RecordedCall 
            { 
                MethodName = "Add", 
                Arguments = new object?[] { 1, 2 }, 
                Result = 3,
                DurationMilliseconds = 10
            },
            new RecordedCall 
            { 
                MethodName = "Multiply", 
                Arguments = new object?[] { 3, 4 }, 
                Result = 12,
                DurationMilliseconds = 8
            }
        };
        
        // Act
        var csv = AutoScribe.ExportToCsv(recordings);
        
        // Assert
        csv.Should().Contain("Method,Arguments,Result,Duration(ms)");
        csv.Should().Contain("Add,\"1;2\",3,10");
        csv.Should().Contain("Multiply,\"3;4\",12,8");
    }
    
    [Fact]
    public void ReplayContext_ShouldVerifyCallSequence()
    {
        // Arrange
        var recordings = new List<RecordedCall>
        {
            new RecordedCall { MethodName = "Add", Arguments = new object?[] { 1, 2 }, Result = 3 },
            new RecordedCall { MethodName = "Add", Arguments = new object?[] { 5, 7 }, Result = 12 }
        };
        var replay = AutoScribe.CreateReplayContext(recordings);
        
        // Act & Assert
        replay.VerifyNextCall("Add", new object?[] { 1, 2 }).Should().BeTrue();
        replay.VerifyNextCall("Add", new object?[] { 5, 7 }).Should().BeTrue();
        replay.VerifyNextCall("Add", new object?[] { 1, 1 }).Should().BeFalse();
    }
    
    [Fact]
    public void ReplayContext_GetNextExpectedCall_ShouldReturnInOrder()
    {
        // Arrange
        var recordings = new List<RecordedCall>
        {
            new RecordedCall { MethodName = "First", Arguments = Array.Empty<object?>() },
            new RecordedCall { MethodName = "Second", Arguments = Array.Empty<object?>() },
            new RecordedCall { MethodName = "Third", Arguments = Array.Empty<object?>() }
        };
        var replay = AutoScribe.CreateReplayContext(recordings);
        
        // Act & Assert
        replay.GetNextExpectedCall()?.MethodName.Should().Be("First");
        replay.GetNextExpectedCall()?.MethodName.Should().Be("Second");
        replay.GetNextExpectedCall()?.MethodName.Should().Be("Third");
        replay.GetNextExpectedCall().Should().BeNull();
    }
    
    [Fact]
    public void ReplayContext_Reset_ShouldRestartSequence()
    {
        // Arrange
        var recordings = new List<RecordedCall>
        {
            new RecordedCall { MethodName = "Method1", Arguments = Array.Empty<object?>() },
            new RecordedCall { MethodName = "Method2", Arguments = Array.Empty<object?>() }
        };
        var replay = AutoScribe.CreateReplayContext(recordings);
        
        // Act - consume all calls
        replay.GetNextExpectedCall();
        replay.GetNextExpectedCall();
        replay.GetNextExpectedCall().Should().BeNull();
        
        // Reset and verify
        replay.Reset();
        replay.GetNextExpectedCall()?.MethodName.Should().Be("Method1");
    }
    
    [Fact]
    public void RecordedCall_ShouldIncludeTimestamp()
    {
        // Arrange & Act
        var call = new RecordedCall
        {
            MethodName = "Test",
            Arguments = Array.Empty<object?>(),
            Result = null,
            DurationMilliseconds = 100
        };
        
        // Assert
        call.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }
    
    [Fact]
    public void ReplayContext_Recordings_ShouldBeReadOnly()
    {
        // Arrange
        var recordings = new List<RecordedCall>
        {
            new RecordedCall { MethodName = "Test", Arguments = Array.Empty<object?>() }
        };
        var replay = AutoScribe.CreateReplayContext(recordings);
        
        // Act & Assert
        replay.Recordings.Should().NotBeNull();
        replay.Recordings.Count.Should().Be(1);
        replay.Recordings.Should().BeAssignableTo<IReadOnlyList<RecordedCall>>();
    }
}
