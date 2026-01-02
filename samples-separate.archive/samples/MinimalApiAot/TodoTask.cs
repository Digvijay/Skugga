using System.Text.Json.Serialization;

namespace MinimalApiAot;

public class TodoTask
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

/// <summary>
/// JSON source generation for AOT compatibility
/// </summary>
[JsonSerializable(typeof(TodoTask))]
[JsonSerializable(typeof(TodoTask[]))]
[JsonSerializable(typeof(List<TodoTask>))]
internal partial class AppJsonSerializerContext : JsonSerializerContext
{
}
