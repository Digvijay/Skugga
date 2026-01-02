using BasicConsoleApp.Services;

namespace BasicConsoleApp;

public class Program
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("=== Skugga Basic Console App Sample ===\n");

        // In a real app, you'd use dependency injection
        // This sample shows the interfaces that would be mocked in tests
        IWeatherService weatherService = new RealWeatherService();
        INotificationService notificationService = new RealNotificationService();

        var app = new WeatherApp(weatherService, notificationService);
        await app.RunAsync("Seattle");

        Console.WriteLine("\nNote: In tests, IWeatherService and INotificationService");
        Console.WriteLine("would be mocked using Skugga. See the test project for examples.");
    }
}

public class WeatherApp
{
    private readonly IWeatherService _weatherService;
    private readonly INotificationService _notificationService;

    public WeatherApp(IWeatherService weatherService, INotificationService notificationService)
    {
        _weatherService = weatherService;
        _notificationService = notificationService;
    }

    public async Task RunAsync(string city)
    {
        Console.WriteLine($"Fetching weather for {city}...");
        
        var temperature = await _weatherService.GetTemperatureAsync(city);
        var condition = await _weatherService.GetConditionAsync(city);

        Console.WriteLine($"Temperature: {temperature}°C");
        Console.WriteLine($"Condition: {condition}");

        if (temperature < 0)
        {
            await _notificationService.SendAlertAsync($"Freezing conditions in {city}!");
        }
        else if (temperature > 30)
        {
            await _notificationService.SendAlertAsync($"High temperature alert in {city}!");
        }
    }
}
