namespace MinimalApiAot.Services;

public interface ITaskService
{
    Task<TodoTask?> GetByIdAsync(int id);
    Task<IEnumerable<TodoTask>> GetAllAsync();
    Task<TodoTask> CreateAsync(TodoTask task);
    Task<bool> UpdateAsync(TodoTask task);
    Task<bool> DeleteAsync(int id);
    Task<bool> CompleteTaskAsync(int id);
}
