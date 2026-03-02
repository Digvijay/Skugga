using Skugga.Core;
using System.Threading.Tasks;

namespace Skugga.Core.Tests;

/// <summary>
/// Tests for async callback type inference on methods returning Task&lt;T&gt; and ValueTask&lt;T&gt;.
/// Verifies the fix for the bug where .Callback&lt;T&gt; on an async setup incorrectly resolved
/// TResult as the full Task wrapper instead of the inner type.
/// </summary>
public class CallbackAsyncTests
{
    public interface IAsyncService
    {
        Task<int> GetValueAsync();
        Task<string> FetchDataAsync(string key);
        Task<bool> ProcessAsync(int a, int b);
        Task<int> ComputeAsync(int a, int b, int c);
        ValueTask<int> GetValueValueTaskAsync();
        ValueTask<string> FetchDataValueTaskAsync(string key);
        ValueTask<bool> ProcessValueTaskAsync(int a, int b);
        ValueTask<int> ComputeValueTaskAsync(int a, int b, int c);
    }

    #region Task<TResult> Callback Tests

    [Fact]
    [Trait("Category", "Advanced")]
    public async Task Callback_OnTaskMethod_ParameterlessCallback_ShouldExecute()
    {
        // Arrange
        var mock = Mock.Create<IAsyncService>();
        var callbackExecuted = false;

        mock.Setup(x => x.GetValueAsync())
            .Callback(() => callbackExecuted = true)
            .ReturnsAsync(42);

        // Act
        var result = await mock.GetValueAsync();

        // Assert
        result.Should().Be(42);
        callbackExecuted.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "Advanced")]
    public async Task Callback_OnTaskMethod_SingleArg_ShouldInferTypeCorrectly()
    {
        // Arrange
        var mock = Mock.Create<IAsyncService>();
        string? capturedKey = null;

        // This is the core bug scenario: .Callback((string key) => ...) on a Task<string> method
        // Previously TResult resolved to Task<string> instead of string
        mock.Setup(x => x.FetchDataAsync(It.IsAny<string>()))
            .Callback((string key) => capturedKey = key)
            .ReturnsAsync("result");

        // Act
        var result = await mock.FetchDataAsync("my-key");

        // Assert
        result.Should().Be("result");
        capturedKey.Should().Be("my-key");
    }

    [Fact]
    [Trait("Category", "Advanced")]
    public async Task Callback_OnTaskMethod_TwoArgs_ShouldInferTypesCorrectly()
    {
        // Arrange
        var mock = Mock.Create<IAsyncService>();
        int capturedA = 0, capturedB = 0;

        mock.Setup(x => x.ProcessAsync(It.IsAny<int>(), It.IsAny<int>()))
            .Callback((int a, int b) =>
            {
                capturedA = a;
                capturedB = b;
            })
            .ReturnsAsync(true);

        // Act
        var result = await mock.ProcessAsync(10, 20);

        // Assert
        result.Should().BeTrue();
        capturedA.Should().Be(10);
        capturedB.Should().Be(20);
    }

    [Fact]
    [Trait("Category", "Advanced")]
    public async Task Callback_OnTaskMethod_ThreeArgs_ShouldInferTypesCorrectly()
    {
        // Arrange
        var mock = Mock.Create<IAsyncService>();
        int capturedA = 0, capturedB = 0, capturedC = 0;

        mock.Setup(x => x.ComputeAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()))
            .Callback((int a, int b, int c) =>
            {
                capturedA = a;
                capturedB = b;
                capturedC = c;
            })
            .ReturnsAsync(999);

        // Act
        var result = await mock.ComputeAsync(1, 2, 3);

        // Assert
        result.Should().Be(999);
        capturedA.Should().Be(1);
        capturedB.Should().Be(2);
        capturedC.Should().Be(3);
    }

    [Fact]
    [Trait("Category", "Advanced")]
    public async Task Callback_OnTaskMethod_CallbackBeforeReturnsAsync_ShouldChainCorrectly()
    {
        // Arrange
        var mock = Mock.Create<IAsyncService>();
        var executionOrder = new List<string>();

        mock.Setup(x => x.FetchDataAsync("test"))
            .Callback(() => executionOrder.Add("callback"))
            .ReturnsAsync("data");

        // Act
        executionOrder.Add("before");
        var result = await mock.FetchDataAsync("test");
        executionOrder.Add("after");

        // Assert
        executionOrder.Should().Equal("before", "callback", "after");
        result.Should().Be("data");
    }

    [Fact]
    [Trait("Category", "Advanced")]
    public async Task Callback_OnTaskMethod_MultipleInvocations_ShouldExecuteEachTime()
    {
        // Arrange
        var mock = Mock.Create<IAsyncService>();
        int callCount = 0;

        mock.Setup(x => x.GetValueAsync())
            .Callback(() => callCount++)
            .ReturnsAsync(1);

        // Act
        await mock.GetValueAsync();
        await mock.GetValueAsync();
        await mock.GetValueAsync();

        // Assert
        callCount.Should().Be(3);
    }

    #endregion

    #region ValueTask<TResult> Callback Tests

    [Fact]
    [Trait("Category", "Advanced")]
    public async Task Callback_OnValueTaskMethod_ParameterlessCallback_ShouldExecute()
    {
        // Arrange
        var mock = Mock.Create<IAsyncService>();
        var callbackExecuted = false;

        mock.Setup(x => x.GetValueValueTaskAsync())
            .Callback(() => callbackExecuted = true)
            .ReturnsAsync(42);

        // Act
        var result = await mock.GetValueValueTaskAsync();

        // Assert
        result.Should().Be(42);
        callbackExecuted.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "Advanced")]
    public async Task Callback_OnValueTaskMethod_SingleArg_ShouldInferTypeCorrectly()
    {
        // Arrange
        var mock = Mock.Create<IAsyncService>();
        string? capturedKey = null;

        mock.Setup(x => x.FetchDataValueTaskAsync(It.IsAny<string>()))
            .Callback((string key) => capturedKey = key)
            .ReturnsAsync("vt-result");

        // Act
        var result = await mock.FetchDataValueTaskAsync("vt-key");

        // Assert
        result.Should().Be("vt-result");
        capturedKey.Should().Be("vt-key");
    }

    [Fact]
    [Trait("Category", "Advanced")]
    public async Task Callback_OnValueTaskMethod_TwoArgs_ShouldInferTypesCorrectly()
    {
        // Arrange
        var mock = Mock.Create<IAsyncService>();
        int capturedA = 0, capturedB = 0;

        mock.Setup(x => x.ProcessValueTaskAsync(It.IsAny<int>(), It.IsAny<int>()))
            .Callback((int a, int b) =>
            {
                capturedA = a;
                capturedB = b;
            })
            .ReturnsAsync(true);

        // Act
        var result = await mock.ProcessValueTaskAsync(5, 15);

        // Assert
        result.Should().BeTrue();
        capturedA.Should().Be(5);
        capturedB.Should().Be(15);
    }

    [Fact]
    [Trait("Category", "Advanced")]
    public async Task Callback_OnValueTaskMethod_ThreeArgs_ShouldInferTypesCorrectly()
    {
        // Arrange
        var mock = Mock.Create<IAsyncService>();
        int capturedA = 0, capturedB = 0, capturedC = 0;

        mock.Setup(x => x.ComputeValueTaskAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()))
            .Callback((int a, int b, int c) =>
            {
                capturedA = a;
                capturedB = b;
                capturedC = c;
            })
            .ReturnsAsync(777);

        // Act
        var result = await mock.ComputeValueTaskAsync(10, 20, 30);

        // Assert
        result.Should().Be(777);
        capturedA.Should().Be(10);
        capturedB.Should().Be(20);
        capturedC.Should().Be(30);
    }

    #endregion
}
