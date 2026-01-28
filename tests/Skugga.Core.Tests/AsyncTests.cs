#nullable enable
using System;
using System.Threading.Tasks;
using Xunit;
using Skugga.Core;

namespace Skugga.Core.Tests
{
    public interface IAsyncService
    {
        Task<int> GetDataAsync();
        ValueTask<string> GetValueAsync();
        Task ExecuteAsync();
    }

    public class AsyncTests
    {
        [Fact]
        public async Task Mock_ReturnsAsync_Task()
        {
            var mock = Mock.Create<IAsyncService>();
            mock.Setup(x => x.GetDataAsync()).ReturnsAsync(42);

            var result = await mock.GetDataAsync();
            Assert.Equal(42, result);
        }

        [Fact]
        public async Task Mock_ReturnsAsync_ValueTask()
        {
            var mock = Mock.Create<IAsyncService>();
            mock.Setup(x => x.GetValueAsync()).ReturnsAsync("hello");

            var result = await mock.GetValueAsync();
            Assert.Equal("hello", result);
        }

        [Fact]
        public async Task Mock_ThrowsAsync_Task()
        {
            var mock = Mock.Create<IAsyncService>();
            mock.Setup(x => x.GetDataAsync()).ThrowsAsync(new InvalidOperationException("async error"));

            await Assert.ThrowsAsync<InvalidOperationException>(() => mock.GetDataAsync());
        }

        [Fact]
        public async Task Mock_ThrowsAsync_ValueTask()
        {
            var mock = Mock.Create<IAsyncService>();
            mock.Setup(x => x.GetValueAsync()).ThrowsAsync(new InvalidOperationException("value task error"));

            await Assert.ThrowsAsync<InvalidOperationException>(async () => await mock.GetValueAsync());
        }

        [Fact]
        public async Task Mock_ThrowsAsync_TaskVoid()
        {
            var mock = Mock.Create<IAsyncService>();
            mock.Setup(x => x.ExecuteAsync()).ThrowsAsync(new InvalidOperationException("void task error"));

            await Assert.ThrowsAsync<InvalidOperationException>(() => mock.ExecuteAsync());
        }
    }
}
