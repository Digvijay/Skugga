using BasicConsoleApp.Services;
using FluentAssertions;
using Skugga.Core;
using Xunit;

namespace BasicConsoleApp.Tests;

/// <summary>
/// Demonstrates basic Skugga usage for mocking dependencies.
/// Shows setup, verification, and assertion patterns.
/// </summary>
public class WeatherAppTests
{
    [Fact]
    public async Task RunAsync_WithFreezingTemperature_SendsAlert()
    {
        // Arrange - Create mocks using Skugga
        var weatherMock = Mock.Create<IWeatherService>();
        var notificationMock = Mock.Create<INotificationService>();

        // Setup mock behavior
        weatherMock.Setup(x => x.GetTemperatureAsync("Seattle")).Returns(Task.FromResult(-5.0));
        weatherMock.Setup(x => x.GetConditionAsync("Seattle")).Returns(Task.FromResult("Snowy"));
        notificationMock.Setup(x => x.SendAlertAsync(It.IsAny<string>())).Returns(Task.CompletedTask);

        var app = new WeatherApp(weatherMock, notificationMock);

        // Act
        await app.RunAsync("Seattle");

        // Assert - Verify the alert was sent
        notificationMock.Verify(x => x.SendAlertAsync("Freezing conditions in Seattle!"), Times.Once());
    }

    [Fact]
    public async Task RunAsync_WithHighTemperature_SendsAlert()
    {
        // Arrange
        var weatherMock = Mock.Create<IWeatherService>();
        var notificationMock = Mock.Create<INotificationService>();

        weatherMock.Setup(x => x.GetTemperatureAsync("Phoenix")).Returns(Task.FromResult(35.5));
        weatherMock.Setup(x => x.GetConditionAsync("Phoenix")).Returns(Task.FromResult("Sunny"));
        notificationMock.Setup(x => x.SendAlertAsync(It.IsAny<string>())).Returns(Task.CompletedTask);

        var app = new WeatherApp(weatherMock, notificationMock);

        // Act
        await app.RunAsync("Phoenix");

        // Assert
        notificationMock.Verify(x => x.SendAlertAsync("High temperature alert in Phoenix!"), Times.Once());
    }

    [Fact]
    public async Task RunAsync_WithNormalTemperature_DoesNotSendAlert()
    {
        // Arrange
        var weatherMock = Mock.Create<IWeatherService>();
        var notificationMock = Mock.Create<INotificationService>();

        weatherMock.Setup(x => x.GetTemperatureAsync("Seattle")).Returns(Task.FromResult(20.0));
        weatherMock.Setup(x => x.GetConditionAsync("Seattle")).Returns(Task.FromResult("Cloudy"));

        var app = new WeatherApp(weatherMock, notificationMock);

        // Act
        await app.RunAsync("Seattle");

        // Assert - Verify no alert was sent
        notificationMock.Verify(x => x.SendAlertAsync(It.IsAny<string>()), Times.Never());
    }

    [Fact]
    public async Task RunAsync_CallsWeatherServiceForBothTemperatureAndCondition()
    {
        // Arrange
        var weatherMock = Mock.Create<IWeatherService>();
        var notificationMock = Mock.Create<INotificationService>();

        weatherMock.Setup(x => x.GetTemperatureAsync("Boston")).Returns(Task.FromResult(15.0));
        weatherMock.Setup(x => x.GetConditionAsync("Boston")).Returns(Task.FromResult("Rainy"));

        var app = new WeatherApp(weatherMock, notificationMock);

        // Act
        await app.RunAsync("Boston");

        // Assert - Verify both methods were called
        weatherMock.Verify(x => x.GetTemperatureAsync("Boston"), Times.Once());
        weatherMock.Verify(x => x.GetConditionAsync("Boston"), Times.Once());
    }

    [Fact]
    public async Task RunAsync_WithDifferentTemperatures_BehavesCorrectly()
    {
        // Arrange - Testing multiple scenarios
        var testCases = new[]
        {
            new { City = "Alaska", Temp = -15.0, ShouldAlert = true },
            new { City = "Hawaii", Temp = 28.0, ShouldAlert = false },
            new { City = "Desert", Temp = 45.0, ShouldAlert = true }
        };

        foreach (var testCase in testCases)
        {
            var weatherMock = Mock.Create<IWeatherService>();
            var notificationMock = Mock.Create<INotificationService>();

            // Use It.IsAny<string>() for dynamic values - Skugga requires constant values or argument matchers
            weatherMock.Setup(x => x.GetTemperatureAsync(It.IsAny<string>())).Returns(Task.FromResult(testCase.Temp));
            weatherMock.Setup(x => x.GetConditionAsync(It.IsAny<string>())).Returns(Task.FromResult("Clear"));
            notificationMock.Setup(x => x.SendAlertAsync(It.IsAny<string>())).Returns(Task.CompletedTask);

            var app = new WeatherApp(weatherMock, notificationMock);

            // Act
            await app.RunAsync(testCase.City);

            // Assert
            var expectedTimes = testCase.ShouldAlert ? Times.Once() : Times.Never();
            notificationMock.Verify(x => x.SendAlertAsync(It.IsAny<string>()), expectedTimes);
        }
    }
}
