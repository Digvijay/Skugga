# BasicConsoleApp Sample

A simple console application demonstrating basic Skugga usage for mocking dependencies in tests.

## Overview

This sample shows:
- How to create mocks using `Mock.Create<T>()`
- Setting up method behaviors with `.Setup()`
- Verifying method calls with `.Verify()`
- Using `It.IsAny<T>()` for argument matching
- Testing different scenarios with parameterized tests

## Project Structure

```
BasicConsoleApp/
├── Program.cs                    # Main application entry point
├── Services/
│   ├── IWeatherService.cs        # Interface to be mocked
│   ├── RealWeatherService.cs     # Real implementation
│   ├── INotificationService.cs   # Interface to be mocked
│   └── RealNotificationService.cs # Real implementation
└── BasicConsoleApp.Tests/
    └── WeatherAppTests.cs        # Skugga-based tests
```

## Running the Application

```bash
cd samples/BasicConsoleApp
dotnet run
```

## Running the Tests

```bash
cd samples/BasicConsoleApp.Tests
dotnet test
```

## Key Concepts Demonstrated

### 1. Creating Mocks
```csharp
var weatherMock = Mock.Create<IWeatherService>();
var notificationMock = Mock.Create<INotificationService>();
```

### 2. Setting Up Behavior
```csharp
weatherMock.Setup(x => x.GetTemperatureAsync("Seattle")).Returns(-5.0);
weatherMock.Setup(x => x.GetConditionAsync("Seattle")).Returns("Snowy");
```

### 3. Verifying Calls
```csharp
notificationMock.Verify(x => x.SendAlertAsync("Freezing conditions in Seattle!"), Times.Once());
```

### 4. Argument Matching
```csharp
notificationMock.Verify(x => x.SendAlertAsync(It.IsAny<string>()), Times.Never());
```

## Why Skugga?

- **✅ Native AOT Compatible**: Works with ahead-of-time compilation
- **✅ Zero Reflection**: All code generated at compile-time
- **✅ Fast**: No runtime proxy generation overhead
- **✅ Simple API**: Familiar mocking syntax similar to popular frameworks

## Next Steps

- Explore the [AspNetCoreWebApi](../AspNetCoreWebApi/) sample for API testing
- See the [MinimalApiAot](../MinimalApiAot/) sample for AOT compilation
- Check the [AzureFunctions](../AzureFunctions/) sample for serverless testing
