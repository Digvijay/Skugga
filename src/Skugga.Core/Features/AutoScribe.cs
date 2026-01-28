#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace Skugga.Core
{
    /// <summary>
    /// AutoScribe captures real method calls and generates test setup code automatically.
    /// This is a compile-time feature - the source generator creates recording proxies.
    /// </summary>
    /// <remarks>
    /// <para>
    /// AutoScribe helps with:
    /// </para>
    /// <list type="bullet">
    /// <item><description>Capturing production behavior for test replay</description></item>
    /// <item><description>Generating mock setups from real interactions</description></item>
    /// <item><description>Analyzing method call patterns and timings</description></item>
    /// <item><description>Creating regression tests from recorded behavior</description></item>
    /// </list>
    /// <para>
    /// <b>Important:</b> The Capture method is intercepted by the source generator at compile time.
    /// It generates a recording proxy that logs all calls and delegates to the real implementation.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Wrap real service with recording proxy
    /// var realService = new MyService();
    /// var recorded = AutoScribe.Capture(realService);
    ///
    /// // Use the service normally - calls are recorded
    /// recorded.Initialize();
    /// var result = recorded.GetData(42);
    /// recorded.Process(result);
    ///
    /// // Export recordings for analysis
    /// var json = AutoScribe.ExportToJson(recorded.Recordings);
    /// var csv = AutoScribe.ExportToCsv(recorded.Recordings);
    ///
    /// // Create replay context for verification
    /// var replay = AutoScribe.CreateReplayContext(recorded.Recordings);
    /// Assert.True(replay.VerifyNextCall("Initialize", Array.Empty&lt;object&gt;()));
    /// </code>
    /// </example>
    public static class AutoScribe
    {
        /// <summary>
        /// Wraps a real implementation with a recording proxy that logs all method calls.
        /// </summary>
        /// <typeparam name="T">Interface type to record</typeparam>
        /// <param name="realImplementation">The real object to wrap</param>
        /// <returns>A recording proxy that logs calls and delegates to the real implementation</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the source generator failed to intercept this call.
        /// Indicates project configuration issue.
        /// </exception>
        /// <remarks>
        /// <para>
        /// <b>Compile-Time Only:</b> This method is never executed at runtime.
        /// The source generator intercepts it and generates a recording proxy class.
        /// </para>
        /// <para>
        /// <b>Setup Requirements:</b>
        /// </para>
        /// <list type="number">
        /// <item><description>Reference Skugga.Generator with OutputItemType="Analyzer"</description></item>
        /// <item><description>Enable interceptors feature preview</description></item>
        /// <item><description>Clean build after generator changes</description></item>
        /// </list>
        /// </remarks>
        /// <example>
        /// <code>
        /// // Record real database calls
        /// var realDb = new SqlDatabase(connectionString);
        /// var recordedDb = AutoScribe.Capture&lt;IDatabase&gt;(realDb);
        ///
        /// // Use normally - all calls are logged with timing
        /// recordedDb.Connect();
        /// var users = recordedDb.Query("SELECT * FROM Users");
        /// recordedDb.Disconnect();
        ///
        /// // Analyze call patterns
        /// foreach (var call in recordedDb.Recordings)
        /// {
        ///     Console.WriteLine($"{call.MethodName}({string.Join(", ", call.Arguments)}) => {call.Result} [{call.DurationMilliseconds}ms]");
        /// }
        /// </code>
        /// </example>
        public static T Capture<T>(T realImplementation) where T : class
        {
            // NO FALLBACK: AutoScribe is compile-time only - source generator MUST intercept this call
            throw new InvalidOperationException(
                $"[Skugga] Source generator failed to intercept AutoScribe.Capture<{typeof(T).Name}>().\n" +
                "AutoScribe is a COMPILE-TIME feature that generates recording proxies.\n" +
                "Ensure your project references Skugga.Generator and enables interceptors.\n" +
                "See: https://github.com/Digvijay/Skugga/blob/main/README.md#autoscribe");
        }

        /// <summary>
        /// Exports recorded method calls to JSON format for analysis or replay.
        /// </summary>
        /// <param name="recordings">List of recorded method calls</param>
        /// <returns>JSON string representation of the recordings</returns>
        /// <remarks>
        /// Useful for storing recordings for later analysis, sharing test data, or
        /// creating regression test fixtures.
        /// </remarks>
        /// <example>
        /// <code>
        /// var json = AutoScribe.ExportToJson(recorded.Recordings);
        /// File.WriteAllText("api-calls.json", json);
        ///
        /// // Later, load and replay
        /// var loadedJson = File.ReadAllText("api-calls.json");
        /// // Parse and create replay context...
        /// </code>
        /// </example>
        public static string ExportToJson(IEnumerable<RecordedCall> recordings)
        {
            var items = recordings.Select(r =>
                $"{{\"Method\":\"{r.MethodName}\"," +
                $"\"Args\":[{string.Join(",", r.Arguments.Select(a => $"\"{a}\""))}]," +
                $"\"Result\":\"{r.Result}\"," +
                $"\"Duration\":{r.DurationMilliseconds}}}");
            return $"[{string.Join(",", items)}]";
        }

        /// <summary>
        /// Exports recorded method calls to CSV format for analysis in spreadsheets.
        /// </summary>
        /// <param name="recordings">List of recorded method calls</param>
        /// <returns>CSV string representation of the recordings</returns>
        /// <remarks>
        /// Useful for analyzing call patterns in Excel, Google Sheets, or other
        /// spreadsheet applications. Includes method name, arguments, result, and duration.
        /// </remarks>
        /// <example>
        /// <code>
        /// var csv = AutoScribe.ExportToCsv(recorded.Recordings);
        /// File.WriteAllText("api-calls.csv", csv);
        ///
        /// // Open in Excel for analysis:
        /// // - Which methods are called most frequently?
        /// // - What are the slowest operations?
        /// // - What argument patterns are common?
        /// </code>
        /// </example>
        public static string ExportToCsv(IEnumerable<RecordedCall> recordings)
        {
            var lines = new List<string> { "Method,Arguments,Result,Duration(ms)" };
            foreach (var r in recordings)
            {
                var args = string.Join(";", r.Arguments);
                lines.Add($"{r.MethodName},\"{args}\",{r.Result},{r.DurationMilliseconds}");
            }
            return string.Join(Environment.NewLine, lines);
        }

        /// <summary>
        /// Creates a replay context that can be used to replay recorded method calls.
        /// </summary>
        /// <param name="recordings">List of recorded method calls to replay</param>
        /// <returns>A replay context for verifying behavior matches recordings</returns>
        /// <remarks>
        /// Use replay contexts to verify that new implementations match recorded behavior
        /// from real implementations. Great for regression testing.
        /// </remarks>
        /// <example>
        /// <code>
        /// // Record real implementation
        /// var recorded = AutoScribe.Capture(realService);
        /// // ... use service ...
        /// var replay = AutoScribe.CreateReplayContext(recorded.Recordings);
        ///
        /// // Test new implementation matches recorded behavior
        /// var newService = new OptimizedService();
        /// foreach (var expectedCall in replay.Recordings)
        /// {
        ///     // Invoke same method with same args on new service
        ///     // Compare results...
        /// }
        /// </code>
        /// </example>
        public static ReplayContext CreateReplayContext(IEnumerable<RecordedCall> recordings)
        {
            return new ReplayContext(recordings.ToList());
        }
    }

    /// <summary>
    /// Represents a recorded method call with timing information.
    /// </summary>
    public class RecordedCall
    {
        /// <summary>
        /// Gets or sets the name of the method that was called.
        /// </summary>
        public string MethodName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the arguments passed to the method.
        /// </summary>
        public object?[] Arguments { get; set; } = Array.Empty<object?>();

        /// <summary>
        /// Gets or sets the result returned by the method.
        /// </summary>
        public object? Result { get; set; }

        /// <summary>
        /// Gets or sets the time taken to execute the method in milliseconds.
        /// </summary>
        /// <remarks>
        /// Includes the time for the real implementation to execute.
        /// Does not include recording overhead.
        /// </remarks>
        public long DurationMilliseconds { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the method was called.
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Context for replaying recorded method calls and verifying behavior matches.
    /// </summary>
    public class ReplayContext
    {
        private readonly List<RecordedCall> _recordings;
        private int _currentIndex = 0;

        /// <summary>
        /// Initializes a new replay context with the specified recordings.
        /// </summary>
        /// <param name="recordings">List of recorded calls to replay</param>
        public ReplayContext(List<RecordedCall> recordings)
        {
            _recordings = recordings;
        }

        /// <summary>
        /// Gets the next expected call in the replay sequence.
        /// </summary>
        /// <returns>The next recorded call, or null if at the end</returns>
        /// <remarks>
        /// Advances the internal pointer. Call Reset() to start over.
        /// </remarks>
        public RecordedCall? GetNextExpectedCall()
        {
            if (_currentIndex < _recordings.Count)
                return _recordings[_currentIndex++];
            return null;
        }

        /// <summary>
        /// Verifies that a method call matches the next expected recording.
        /// </summary>
        /// <param name="methodName">Name of the method being called</param>
        /// <param name="args">Arguments passed to the method</param>
        /// <returns>True if the call matches the recording; otherwise false</returns>
        /// <remarks>
        /// <para>
        /// Checks:
        /// </para>
        /// <list type="bullet">
        /// <item><description>Method name matches</description></item>
        /// <item><description>Argument count matches</description></item>
        /// <item><description>All arguments match using Equals()</description></item>
        /// </list>
        /// </remarks>
        /// <example>
        /// <code>
        /// var replay = AutoScribe.CreateReplayContext(recordings);
        ///
        /// Assert.True(replay.VerifyNextCall("Initialize", Array.Empty&lt;object&gt;()));
        /// Assert.True(replay.VerifyNextCall("GetData", new object[] { 42 }));
        /// Assert.True(replay.VerifyNextCall("Process", new object[] { "result" }));
        /// </code>
        /// </example>
        public bool VerifyNextCall(string methodName, object?[] args)
        {
            var expected = GetNextExpectedCall();
            if (expected == null) return false;

            if (expected.MethodName != methodName) return false;
            if (expected.Arguments.Length != args.Length) return false;

            for (int i = 0; i < args.Length; i++)
            {
                if (!Equals(expected.Arguments[i], args[i]))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Resets the replay context to the beginning of the recording sequence.
        /// </summary>
        /// <remarks>
        /// Use this to replay the same sequence multiple times.
        /// </remarks>
        public void Reset()
        {
            _currentIndex = 0;
        }

        /// <summary>
        /// Gets all recordings in this replay context.
        /// </summary>
        public IReadOnlyList<RecordedCall> Recordings => _recordings;
    }

    /// <summary>
    /// Factory for creating test harness instances.
    /// </summary>
    /// <remarks>
    /// Test harnesses provide a structured way to organize mocks and system under test (SUT).
    /// </remarks>
    public static class Harness
    {
        /// <summary>
        /// Creates a new test harness for the specified type.
        /// </summary>
        /// <typeparam name="T">The type of the system under test</typeparam>
        /// <returns>A new test harness instance</returns>
        public static TestHarness<T> Create<T>() => new TestHarness<T>();
    }

    /// <summary>
    /// Base class for test harnesses that organize mocks and system under test.
    /// </summary>
    /// <typeparam name="T">The type of the system under test</typeparam>
    /// <remarks>
    /// <para>
    /// Test harnesses help organize complex test setups by:
    /// </para>
    /// <list type="bullet">
    /// <item><description>Centralizing mock management</description></item>
    /// <item><description>Providing reusable test infrastructure</description></item>
    /// <item><description>Simplifying test arrangement</description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// public class UserServiceHarness : TestHarness&lt;UserService&gt;
    /// {
    ///     public IDatabase DatabaseMock { get; }
    ///     public ILogger LoggerMock { get; }
    ///
    ///     public UserServiceHarness()
    ///     {
    ///         DatabaseMock = Mock.Create&lt;IDatabase&gt;();
    ///         LoggerMock = Mock.Create&lt;ILogger&gt;();
    ///
    ///         SUT = new UserService(DatabaseMock, LoggerMock);
    ///     }
    ///
    ///     public void SetupValidUser(int userId, string name)
    ///     {
    ///         DatabaseMock.Setup(x => x.GetUser(userId)).Returns(new User { Id = userId, Name = name });
    ///     }
    /// }
    ///
    /// // Use in tests
    /// var harness = new UserServiceHarness();
    /// harness.SetupValidUser(42, "John");
    /// var user = harness.SUT.GetUser(42);
    /// Assert.Equal("John", user.Name);
    /// </code>
    /// </example>
    public class TestHarness<T>
    {
        /// <summary>
        /// Gets or sets the system under test.
        /// </summary>
        public T SUT { get; protected set; } = default!;

        /// <summary>
        /// Dictionary for storing mock instances by type.
        /// </summary>
        protected Dictionary<Type, object> _mocks = new();
    }
}
