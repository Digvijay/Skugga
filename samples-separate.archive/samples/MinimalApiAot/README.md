# MinimalApiAot Sample

A Minimal API with **Native AOT** compilation enabled, demonstrating Skugga's perfect compatibility with ahead-of-time compiled applications.

## Overview

This sample shows:
- Native AOT compilation with `PublishAot=true`
- JSON source generation for AOT-compatible serialization
- Testing AOT-compiled code with Skugga
- Minimal API patterns with dependency injection
- How Skugga eliminates the "Reflection Wall" for Native AOT

## Project Structure

```
MinimalApiAot/
├── Program.cs                      # Minimal API endpoints
├── TodoTask.cs                     # Domain model with JSON source gen
├── Services/
│   ├── ITaskService.cs             # Service interface
│   └── InMemoryTaskService.cs      # Implementation
└── MinimalApiAot.Tests/
    └── TaskServiceTests.cs         # Skugga-based tests
```

## Running the Application

### Standard JIT Compilation
```bash
cd samples/MinimalApiAot
dotnet run
```

### Native AOT Compilation
```bash
cd samples/MinimalApiAot
dotnet publish -c Release
./bin/Release/net9.0/linux-x64/publish/MinimalApiAot
```

The API will be available at `http://localhost:5000`

## Running the Tests

```bash
cd samples/MinimalApiAot.Tests
dotnet test
```

## API Endpoints

- `GET /api/tasks` - Get all tasks
- `GET /api/tasks/{id}` - Get task by ID
- `POST /api/tasks` - Create new task
- `PUT /api/tasks/{id}` - Update task
- `DELETE /api/tasks/{id}` - Delete task
- `PATCH /api/tasks/{id}/complete` - Mark task as complete

## Native AOT Configuration

The project includes key AOT settings:

```xml
<PropertyGroup>
  <PublishAot>true</PublishAot>
  <InvariantGlobalization>true</InvariantGlobalization>
  <JsonSerializerIsReflectionEnabledByDefault>false</JsonSerializerIsReflectionEnabledByDefault>
</PropertyGroup>
```

## JSON Source Generation

Instead of reflection-based JSON serialization:

```csharp
[JsonSerializable(typeof(TodoTask))]
[JsonSerializable(typeof(TodoTask[]))]
[JsonSerializable(typeof(List<TodoTask>))]
internal partial class AppJsonSerializerContext : JsonSerializerContext
{
}
```

## Why This Matters

### The "Reflection Wall" Problem
Traditional mocking libraries like Moq use `System.Reflection.Emit` to generate proxy objects at runtime. Native AOT removes the JIT compiler, making these libraries crash immediately.

### Skugga's Solution
Skugga generates all mock code at **compile-time** using C# source generators, producing static classes that compile directly to native machine code.

```mermaid
graph LR
    A[Write Test] --> B[Skugga Generator]
    B --> C[Generated Mock Code]
    C --> D[Compile to Native]
    D --> E[✅ AOT Binary]
    
    style E fill:#006600,stroke:#333,color:#fff
```

## Performance Benefits

Native AOT with Skugga delivers:
- **Instant startup** (< 50ms)
- **Smaller binaries** (< 10MB)
- **Lower memory usage** (< 20MB)
- **No warmup time** (no JIT compilation)

Perfect for:
- Serverless functions (Azure Functions, AWS Lambda)
- Microservices (Kubernetes, containers)
- Edge computing
- Resource-constrained environments

## Testing AOT Code

The tests work identically whether compiled with JIT or AOT:

```csharp
var mockService = Mock.Create<ITaskService>();
mockService.Setup(x => x.GetByIdAsync(1)).Returns(expectedTask);

var result = await mockService.Object.GetByIdAsync(1);
```

**Zero reflection. Zero runtime overhead. 100% AOT compatible.**

## Next Steps

- Compare with the [AspNetCoreWebApi](../AspNetCoreWebApi/) sample (JIT-compiled)
- See the [AzureFunctions](../AzureFunctions/) sample for serverless AOT
- Check the [BasicConsoleApp](../BasicConsoleApp/) for simpler examples

## Building for Production

```bash
# Linux x64
dotnet publish -c Release -r linux-x64

# Windows x64
dotnet publish -c Release -r win-x64

# macOS ARM64
dotnet publish -c Release -r osx-arm64
```

The output will be a single, self-contained native executable.
