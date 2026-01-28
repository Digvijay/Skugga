using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Skugga.Generator;

/// <summary>
/// Helper for generating AutoScribe recording proxy code.
/// Handles value serialization using compile-time reflection for AOT compatibility.
/// </summary>
internal static class AutoScribeCodeGenerator
{
    /// <summary>
    /// Generates code to serialize a value for use in test setup.
    /// Uses compile-time type information to generate proper object construction.
    /// </summary>
    public static string GenerateValueSerialization(ITypeSymbol type, string valueExpression)
    {
        // Primitives and well-known types
        if (type.SpecialType != SpecialType.None)
        {
            return GeneratePrimitiveValue(type.SpecialType, valueExpression);
        }

        // Strings
        if (type.SpecialType == SpecialType.System_String)
        {
            return $"\\\"{{SerializeValue({valueExpression})}}\\\"";
        }

        // Check if type has custom ToString (not from System.Object)
        var toStringMethod = type.GetMembers("ToString")
            .OfType<IMethodSymbol>()
            .FirstOrDefault(m => m.Parameters.Length == 0);

        if (toStringMethod != null && toStringMethod.ContainingType.SpecialType != SpecialType.System_Object)
        {
            // Type has custom ToString - use it directly
            return $"{{SerializeValue({valueExpression})}}";
        }

        // For complex types, generate object initializer using properties
        return GenerateObjectInitializer(type, valueExpression);
    }

    private static string GeneratePrimitiveValue(SpecialType specialType, string valueExpression)
    {
        return specialType switch
        {
            SpecialType.System_Boolean => $"{{{valueExpression}.ToString().ToLower()}}",
            SpecialType.System_Char => $"'{{SerializeValue({valueExpression})}}'",
            SpecialType.System_Decimal => $"{{{valueExpression}}}m",
            SpecialType.System_Single => $"{{{valueExpression}}}f",
            SpecialType.System_Double => $"{{{valueExpression}}}d",
            SpecialType.System_Int32 or SpecialType.System_Int64 or
            SpecialType.System_Int16 or SpecialType.System_Byte => $"{{{valueExpression}}}",
            _ => $"{{SerializeValue({valueExpression})}}"
        };
    }

    private static string GenerateObjectInitializer(ITypeSymbol type, string valueExpression)
    {
        var sb = new StringBuilder();
        sb.Append("new ");
        sb.Append(type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));
        sb.Append(" { ");

        // Get all public properties
        var properties = type.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(p => p.DeclaredAccessibility == Accessibility.Public && p.GetMethod != null)
            .ToList();

        for (int i = 0; i < properties.Count; i++)
        {
            var prop = properties[i];
            if (i > 0) sb.Append(", ");

            sb.Append(prop.Name);
            sb.Append(" = ");

            // Recursively generate value for property type
            sb.Append($"{{{valueExpression}.{prop.Name}}}");
        }

        sb.Append(" }");
        return sb.ToString();
    }

    /// <summary>
    /// Generates the SerializeValue helper method for the recording proxy.
    /// This method handles runtime value serialization at test-time (not source generation time).
    /// </summary>
    public static void GenerateSerializeValueMethod(StringBuilder sb)
    {
        sb.AppendLine("        private static string SerializeValue(object? value)");
        sb.AppendLine("        {");
        sb.AppendLine("            if (value == null) return \"null\";");
        sb.AppendLine("            if (value is string s) return $\"\\\"{s.Replace(\"\\\"\", \"\\\\\\\"\")}\\\"\";");
        sb.AppendLine("            if (value is bool b) return b ? \"true\" : \"false\";");
        sb.AppendLine("            if (value is char c) return $\"'{c}'\";");
        sb.AppendLine("            if (value is decimal d) return $\"{d}m\";");
        sb.AppendLine("            if (value is float f) return $\"{f}f\";");
        sb.AppendLine("            if (value is double dbl) return $\"{dbl}d\";");
        sb.AppendLine("            if (value is int || value is long || value is short || value is byte) return value.ToString();");
        sb.AppendLine("            if (value is System.Enum) return $\"{value.GetType().Name}.{value}\";");
        sb.AppendLine("            if (value is System.Collections.IEnumerable enumerable && value is not string)");
        sb.AppendLine("            {");
        sb.AppendLine("                var items = new System.Collections.Generic.List<string>();");
        sb.AppendLine("                var count = 0;");
        sb.AppendLine("                foreach (var item in enumerable)");
        sb.AppendLine("                {");
        sb.AppendLine("                    if (count < 5) items.Add(SerializeValue(item));");
        sb.AppendLine("                    count++;");
        sb.AppendLine("                }");
        sb.AppendLine("                if (count > 5) items.Add($\"/* ... +{count - 5} more */\");");
        sb.AppendLine("                return count == 0 ? \"Array.Empty<object>()\" : $\"new[] {{ {string.Join(\", \", items)} }}\";");
        sb.AppendLine("            }");
        sb.AppendLine("            // For complex objects, try ToString() first");
        sb.AppendLine("            var typeName = value.GetType().Name;");
        sb.AppendLine("            var toStringValue = value.ToString();");
        sb.AppendLine("            if (toStringValue != null && !toStringValue.StartsWith(value.GetType().FullName ?? \"\"))");
        sb.AppendLine("            {");
        sb.AppendLine("                // ToString() was customized, return it directly");
        sb.AppendLine("                return toStringValue;");
        sb.AppendLine("            }");
        sb.AppendLine("            // Fallback: use type name");
        sb.AppendLine("            return $\"/* {typeName} instance */\";");
        sb.AppendLine("        }");
        sb.AppendLine();
    }

    /// <summary>
    /// Generates the call recording code for a method.
    /// </summary>
    public static void GenerateMethodRecording(StringBuilder sb, IMethodSymbol method, string returnType)
    {
        var returnTypeSymbol = method.ReturnType;
        var isTask = returnTypeSymbol.Name == "Task" || returnTypeSymbol.Name == "ValueTask";
        var isGenericTask = isTask && returnTypeSymbol is INamedTypeSymbol nt && nt.TypeArguments.Length > 0;
        var isVoidTask = isTask && !isGenericTask;

        var hasReturnValue = !method.ReturnsVoid && !isTask;

        // Build parameter array for recording
        var paramArray = method.Parameters.Length == 0
            ? "Array.Empty<object?>()"
            : $"new object?[] {{ {string.Join(", ", method.Parameters.Select(p => p.Name))} }}";

        sb.AppendLine("            // Record the call");
        sb.AppendLine($"            var callArgs = {paramArray};");

        if (isGenericTask)
        {
            sb.AppendLine($"            var result = await _real.{method.Name}({string.Join(", ", method.Parameters.Select(p => p.Name))});");
            sb.AppendLine($"            var setupCode = $\"mock.Setup(x => x.{method.Name}({string.Join(", ", method.Parameters.Select(p => "{" + p.Name + "}"))})).ReturnsAsync({{SerializeValue(result)}})\";");
            sb.AppendLine("            Console.WriteLine($\"// [AutoScribe] Call {_callCount}: {setupCode};\");");
            sb.AppendLine("            _callLog.Add(setupCode);");
            sb.AppendLine("            _callCount++;");
            sb.AppendLine("            return result!;");
        }
        else if (hasReturnValue)
        {
            sb.AppendLine($"            var result = _real.{method.Name}({string.Join(", ", method.Parameters.Select(p => p.Name))});");
            sb.AppendLine($"            var setupCode = $\"mock.Setup(x => x.{method.Name}({string.Join(", ", method.Parameters.Select(p => "{" + p.Name + "}"))})).Returns({{SerializeValue(result)}})\";");
            sb.AppendLine("            Console.WriteLine($\"// [AutoScribe] Call {_callCount}: {setupCode};\");");
            sb.AppendLine("            _callLog.Add(setupCode);");
            sb.AppendLine("            _callCount++;");
            sb.AppendLine("            return result!;");
        }
        else if (isVoidTask)
        {
            sb.AppendLine($"            await _real.{method.Name}({string.Join(", ", method.Parameters.Select(p => p.Name))});");
            sb.AppendLine($"            var setupCode = $\"mock.Setup(x => x.{method.Name}({string.Join(", ", method.Parameters.Select(p => "{" + p.Name + "}"))}));\";");
            sb.AppendLine("            Console.WriteLine($\"// [AutoScribe] Call {_callCount}: {setupCode}\");");
            sb.AppendLine("            _callLog.Add(setupCode);");
            sb.AppendLine("            _callCount++;");
        }
        else
        {
            sb.AppendLine($"            _real.{method.Name}({string.Join(", ", method.Parameters.Select(p => p.Name))});");
            sb.AppendLine($"            var setupCode = $\"mock.Setup(x => x.{method.Name}({string.Join(", ", method.Parameters.Select(p => "{" + p.Name + "}"))}));\";");
            sb.AppendLine("            Console.WriteLine($\"// [AutoScribe] Call {_callCount}: {setupCode}\");");
            sb.AppendLine("            _callLog.Add(setupCode);");
            sb.AppendLine("            _callCount++;");
        }
    }

    /// <summary>
    /// Generates the PrintTestMethod implementation.
    /// </summary>
    public static void GeneratePrintTestMethod(StringBuilder sb, string interfaceName)
    {
        sb.AppendLine("        public void PrintTestMethod(string testName = \"GeneratedTest\")");
        sb.AppendLine("        {");
        sb.AppendLine("            if (_callLog.Count == 0) return;");
        sb.AppendLine("            Console.WriteLine(\"\");");
        sb.AppendLine("            Console.WriteLine(\"[Fact]\");");
        sb.AppendLine("            Console.WriteLine($\"public async Task {testName}()\");");
        sb.AppendLine("            Console.WriteLine(\"{\");");
        sb.AppendLine("            Console.WriteLine(\"    // Arrange\");");
        sb.AppendLine($"            Console.WriteLine(\"    var mock{interfaceName} = Mock.Create<{interfaceName}>();\");");
        sb.AppendLine("            Console.WriteLine(\"\");");
        sb.AppendLine("            foreach (var call in _callLog)");
        sb.AppendLine("            {");
        sb.AppendLine($"                Console.WriteLine($\"    {{call.Replace(\"mock\", \"mock{interfaceName}\")}}\");");
        sb.AppendLine("            }");
        sb.AppendLine("            Console.WriteLine(\"\");");
        sb.AppendLine("            Console.WriteLine(\"    // Act\");");
        sb.AppendLine($"            Console.WriteLine(\"    var sut = new YourSystemUnderTest(mock{interfaceName}); // Inject the mock into your SUT\");");
        sb.AppendLine("            Console.WriteLine(\"    var result = await sut.YourMethod(); // Call the SUT method that uses the mock\");");
        sb.AppendLine("            Console.WriteLine(\"\");");
        sb.AppendLine("            Console.WriteLine(\"    // Assert\");");
        sb.AppendLine("            Console.WriteLine(\"    result.Should().NotBeNull();\");");
        sb.AppendLine($"            Console.WriteLine(\"    mock{interfaceName}.Verify(x => x.SomeMethod(...), Times.Once);\");");
        sb.AppendLine("            Console.WriteLine(\"}\");");
        sb.AppendLine("            Console.WriteLine(\"\");");
        sb.AppendLine("        }");
        sb.AppendLine();
    }
}
