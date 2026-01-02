namespace BasicConsoleApp.Services;

/// <summary>
/// Service for retrieving weather information.
/// This interface will be mocked in tests using Skugga.
/// </summary>
public interface IWeatherService
{
    Task<double> GetTemperatureAsync(string city);
    Task<string> GetConditionAsync(string city);
    Task<bool> IsRainingAsync(string city);
}
