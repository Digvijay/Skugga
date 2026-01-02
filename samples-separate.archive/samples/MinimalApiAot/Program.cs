using MinimalApiAot;
using MinimalApiAot.Services;

var builder = WebApplication.CreateSlimBuilder(args);

// Configure JSON serialization for AOT
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
});

// Register services
builder.Services.AddSingleton<ITaskService, InMemoryTaskService>();

var app = builder.Build();

// Minimal API endpoints
var tasks = app.MapGroup("/api/tasks");

tasks.MapGet("/", async (ITaskService service) =>
{
    var allTasks = await service.GetAllAsync();
    return Results.Ok(allTasks);
});

tasks.MapGet("/{id:int}", async (int id, ITaskService service) =>
{
    var task = await service.GetByIdAsync(id);
    return task is not null ? Results.Ok(task) : Results.NotFound();
});

tasks.MapPost("/", async (TodoTask task, ITaskService service) =>
{
    var created = await service.CreateAsync(task);
    return Results.Created($"/api/tasks/{created.Id}", created);
});

tasks.MapPut("/{id:int}", async (int id, TodoTask task, ITaskService service) =>
{
    if (id != task.Id)
        return Results.BadRequest("ID mismatch");

    var updated = await service.UpdateAsync(task);
    return updated ? Results.NoContent() : Results.NotFound();
});

tasks.MapDelete("/{id:int}", async (int id, ITaskService service) =>
{
    var deleted = await service.DeleteAsync(id);
    return deleted ? Results.NoContent() : Results.NotFound();
});

tasks.MapPatch("/{id:int}/complete", async (int id, ITaskService service) =>
{
    var completed = await service.CompleteTaskAsync(id);
    return completed ? Results.Ok() : Results.NotFound();
});

app.Run();

// Make Program accessible for testing
public partial class Program { }
