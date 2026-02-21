# Troubleshooting

Common issues and solutions when using Skugga.

## "Cannot mock sealed classes" (SKUGGA001)

```csharp
//  Won't work -- sealed class
public sealed class EmailService { }
var mock = Mock.Create<EmailService>(); // Error!

//  Use interfaces instead
public interface IEmailService { }
var mock = Mock.Create<IEmailService>(); // Works!
```

## "Class has no virtual members" (SKUGGA002)

```csharp
//  Won't work -- non-virtual members
public class EmailService {
   public string GetEmail() => "";
}

//  Make members virtual
public class EmailService {
   public virtual string GetEmail() => "";
}
```

## Generated Code Not Updating

```bash
# Clean and rebuild
dotnet clean && dotnet build
```

The source generator caches output. A clean build forces regeneration.

## Setup Not Matching

```csharp
//  Exact match required
mock.Setup(x => x.GetData(1)).Returns("one");
mock.GetData(2); // Returns null -- no match

//  Use It.IsAny<T>() for flexible matching
mock.Setup(x => x.GetData(It.IsAny<int>())).Returns("any");
mock.GetData(2); // Returns "any"
```

## Interceptors Not Working

Ensure your `.csproj` has:

```xml
<PropertyGroup>
   <InterceptorsPreviewNamespaces>
       $(InterceptorsPreviewNamespaces);Skugga
   </InterceptorsPreviewNamespaces>
</PropertyGroup>
```

## IDE Not Showing Generated Code

Some IDEs need a restart after adding Skugga. Try:
1. Close IDE
2. `dotnet clean && dotnet build`
3. Reopen IDE

## Async Methods Returning null

In Loose mode, un-setup async methods return completed `Task`/`Task<T>` with default values (not `null` Task). If you're getting `NullReferenceException`:

```csharp
// Ensure setup returns a Task
mock.Setup(x => x.GetDataAsync()).ReturnsAsync("value");
```

## Native AOT Build Errors

Skugga is designed for AOT. If you see trimming warnings:
1. Ensure you're using the latest Skugga version
2. Check that `IsAotCompatible` is set correctly in your project
3. Run `dotnet publish -r <rid> /p:PublishAot=true` to verify
