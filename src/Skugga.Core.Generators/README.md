# Skugga.Core.Generators

This project contains source generators that generate boilerplate code for Skugga.Core itself at compile time.

## SetupExtensionsGenerator

Generates extension method overloads for `Returns` and `ReturnsAsync` to support methods with 3-8 arguments.

### Why Generate These Methods?

While extension methods run at runtime and the mock generator runs at compile time, these extension methods still need to be type-safe. They need to accept `Func<T1, T2, T3, TResult>` instead of `Func<object[], TResult>` to provide:

1. **Compile-time type safety** - Catches type mismatches at compile time
2. **IntelliSense support** - Shows correct parameter types in IDE
3. **.NET conventions** - Matches the patterns used by Func<> and Action<> delegates

However, writing these manually for 4-8 arguments would require ~200 lines of repetitive boilerplate code. By using a source generator to generate these methods, we:

- **Eliminate manual maintenance** - No need to copy-paste and modify for each argument count
- **Enable easy extension** - Change `maxArgs` constant to support more arguments
- **Follow DRY principles** - Single source of truth for the pattern
- **Demonstrate compile-time philosophy** - Use Skugga's own compile-time approach for Skugga itself

### Generated Code

The generator creates methods like:

```csharp
public static SetupContext<TMock, TResult> Returns<TMock, TResult, TArg1, TArg2, TArg3, TArg4>(
    this SetupContext<TMock, TResult> context, 
    Func<TArg1, TArg2, TArg3, TArg4, TResult> valueFunction)
{
    if (context.Setup == null)
    {
        context.Setup = context.Handler.AddSetup(context.Signature, context.Args, default(TResult), null);
        context.Setup.ValueFactory = args => valueFunction((TArg1)args[0]!, (TArg2)args[1]!, (TArg3)args[2]!, (TArg4)args[3]!);
    }
    else
    {
        context.Setup.ValueFactory = args => valueFunction((TArg1)args[0]!, (TArg2)args[1]!, (TArg3)args[2]!, (TArg4)args[3]!);
    }
    return context;
}
```

And similar methods for:
- `Returns<T1..T8>` (4-8 arguments)
- `ReturnsAsync<T1..T8>` (3-8 arguments)

### Output

Generated file: `obj/Generated/Skugga.Core.Generators/Skugga.Core.Generators.SetupExtensionsGenerator/SetupContextExtensions.Generated.g.cs`

- **11 extension methods** (5 Returns + 6 ReturnsAsync)
- **~209 lines** of code
- **Zero maintenance burden**

## Usage

This generator is referenced by Skugga.Core as an analyzer:

```xml
<ProjectReference Include="..\Skugga.Core.Generators\Skugga.Core.Generators.csproj" 
                  OutputItemType="Analyzer" 
                  ReferenceOutputAssembly="false" />
```

The generated code merges with the manual code in `SetupContextExtensions` (which is declared as a `partial class`).
