namespace BasicConsoleApp.Services;

/// <summary>
/// Real implementation (in production, this would call a weather API).
/// </summary>
public class RealWeatherService : IWeatherService
{
    public Task<double> GetTemperatureAsync(string city)
    {
        // Simulate API call
        var random = new Random();
        return Task.FromResult(random.Next(-10, 35) + random.NextDouble());
    }

    public Task<string> GetConditionAsync(string city)
    {
        var conditions = new[] { "Sunny", "Cloudy", "Rainy", "Snowy" };
        var random = new Random();
        return Task.FromResult(conditions[random.Next(conditions.Length)]);
    }

    public Task<bool> IsRainingAsync(string city)
    {
        var random = new Random();
        return Task.FromResult(random.Next(0, 2) == 1);
    }
}
