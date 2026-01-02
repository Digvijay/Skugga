using FluentAssertions;
using MinimalApiAot;
using MinimalApiAot.Services;
using Skugga.Core;
using Xunit;

namespace MinimalApiAot.Tests;

/// <summary>
/// Demonstrates testing Minimal API with Skugga.
/// These tests work identically whether the app is compiled with JIT or Native AOT.
/// </summary>
public class TaskServiceTests
{
    [Fact]
    public async Task GetByIdAsync_WhenTaskExists_ReturnsTask()
    {
        // Arrange
        var mockService = Mock.Create<ITaskService>();
        var expectedTask = new TodoTask
        {
            Id = 1,
            Title = "Test Task",
            Description = "Test Description",
            IsCompleted = false,
            CreatedAt = DateTime.UtcNow
        };
        mockService.Setup(x => x.GetByIdAsync(1)).Returns(Task.FromResult<TodoTask?>(expectedTask));

        // Act
        var result = await mockService.GetByIdAsync(1);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
        result.Title.Should().Be("Test Task");
    }

    [Fact]
    public async Task CreateAsync_CreatesNewTask()
    {
        // Arrange
        var mockService = Mock.Create<ITaskService>();
        var newTask = new TodoTask { Title = "New Task", Description = "Description" };
        var createdTask = new TodoTask
        {
            Id = 10,
            Title = newTask.Title,
            Description = newTask.Description,
            CreatedAt = DateTime.UtcNow
        };
        mockService.Setup(x => x.CreateAsync(It.IsAny<TodoTask>())).Returns(Task.FromResult(createdTask));

        // Act
        var result = await mockService.CreateAsync(newTask);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(10);
        result.Title.Should().Be("New Task");
    }

    [Fact]
    public async Task CompleteTaskAsync_WhenTaskExists_ReturnsTrue()
    {
        // Arrange
        var mockService = Mock.Create<ITaskService>();
        mockService.Setup(x => x.CompleteTaskAsync(1)).Returns(Task.FromResult(true));

        // Act
        var result = await mockService.CompleteTaskAsync(1);

        // Assert
        result.Should().BeTrue();
        mockService.Verify(x => x.CompleteTaskAsync(1), Times.Once());
    }

    [Fact]
    public async Task CompleteTaskAsync_WhenTaskNotFound_ReturnsFalse()
    {
        // Arrange
        var mockService = Mock.Create<ITaskService>();
        mockService.Setup(x => x.CompleteTaskAsync(999)).Returns(Task.FromResult(false));

        // Act
        var result = await mockService.CompleteTaskAsync(999);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_WhenTaskExists_ReturnsTrue()
    {
        // Arrange
        var mockService = Mock.Create<ITaskService>();
        mockService.Setup(x => x.DeleteAsync(1)).Returns(Task.FromResult(true));

        // Act
        var result = await mockService.DeleteAsync(1);

        // Assert
        result.Should().BeTrue();
        mockService.Verify(x => x.DeleteAsync(1), Times.Once());
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllTasks()
    {
        // Arrange
        var mockService = Mock.Create<ITaskService>();
        var tasks = new List<TodoTask>
        {
            new() { Id = 1, Title = "Task 1" },
            new() { Id = 2, Title = "Task 2" },
            new() { Id = 3, Title = "Task 3" }
        };
        mockService.Setup(x => x.GetAllAsync()).Returns(Task.FromResult<IEnumerable<TodoTask>>(tasks));

        // Act
        var result = await mockService.GetAllAsync();

        // Assert
        result.Should().HaveCount(3);
        result.Should().BeEquivalentTo(tasks);
    }

    [Fact]
    public async Task UpdateAsync_WithValidTask_ReturnsTrue()
    {
        // Arrange
        var mockService = Mock.Create<ITaskService>();
        var task = new TodoTask { Id = 1, Title = "Updated Task" };
        mockService.Setup(x => x.UpdateAsync(It.IsAny<TodoTask>())).Returns(Task.FromResult(true));

        // Act
        var result = await mockService.UpdateAsync(task);

        // Assert
        result.Should().BeTrue();
        mockService.Verify(x => x.UpdateAsync(It.IsAny<TodoTask>()), Times.Once());
    }
}
