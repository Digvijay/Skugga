namespace MinimalApiAot.Services;

public class InMemoryTaskService : ITaskService
{
    private readonly Dictionary<int, TodoTask> _tasks = new();
    private int _nextId = 1;

    public InMemoryTaskService()
    {
        // Seed data
        var seedTasks = new[]
        {
            new TodoTask { Id = _nextId++, Title = "Learn Skugga", Description = "Study Native AOT mocking", IsCompleted = false, CreatedAt = DateTime.UtcNow },
            new TodoTask { Id = _nextId++, Title = "Build API", Description = "Create minimal API with AOT", IsCompleted = false, CreatedAt = DateTime.UtcNow },
            new TodoTask { Id = _nextId++, Title = "Write Tests", Description = "Test with Skugga", IsCompleted = true, CreatedAt = DateTime.UtcNow, CompletedAt = DateTime.UtcNow }
        };

        foreach (var task in seedTasks)
        {
            _tasks[task.Id] = task;
        }
    }

    public Task<TodoTask?> GetByIdAsync(int id)
    {
        _tasks.TryGetValue(id, out var task);
        return Task.FromResult(task);
    }

    public Task<IEnumerable<TodoTask>> GetAllAsync()
    {
        return Task.FromResult<IEnumerable<TodoTask>>(_tasks.Values.ToList());
    }

    public Task<TodoTask> CreateAsync(TodoTask task)
    {
        task.Id = _nextId++;
        task.CreatedAt = DateTime.UtcNow;
        _tasks[task.Id] = task;
        return Task.FromResult(task);
    }

    public Task<bool> UpdateAsync(TodoTask task)
    {
        if (!_tasks.ContainsKey(task.Id))
            return Task.FromResult(false);

        _tasks[task.Id] = task;
        return Task.FromResult(true);
    }

    public Task<bool> DeleteAsync(int id)
    {
        return Task.FromResult(_tasks.Remove(id));
    }

    public Task<bool> CompleteTaskAsync(int id)
    {
        if (!_tasks.TryGetValue(id, out var task))
            return Task.FromResult(false);

        task.IsCompleted = true;
        task.CompletedAt = DateTime.UtcNow;
        return Task.FromResult(true);
    }
}
