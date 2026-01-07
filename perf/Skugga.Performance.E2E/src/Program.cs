using System.Diagnostics;

var stopwatch = Stopwatch.StartNew();
Console.WriteLine($"Skugga.Performance.E2E starting at {DateTime.UtcNow:O}");

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

if (args.Length > 0 && args[0] == "--benchmark")
{
    stopwatch.Stop();
    Console.WriteLine($"Skugga.Performance.E2E benchmark mode ready in {stopwatch.ElapsedMilliseconds} ms.");
    return;
}

app.MapGet("/", () => "Skugga Pilot Running");

app.Lifetime.ApplicationStarted.Register(() =>
{
    stopwatch.Stop();
    Console.WriteLine($"Skugga.Performance.E2E started in {stopwatch.ElapsedMilliseconds} ms.");
});

app.Run();
