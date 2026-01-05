using System;
using System.Linq;
using System.Text;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;

namespace Skugga.OpenApi.Generator
{
    /// <summary>
    /// Generates realistic default values from OpenAPI examples.
    /// Used to create mock return values that match the API contract.
    /// </summary>
    internal class ExampleGenerator
    {
        private readonly TypeMapper _typeMapper;
        private readonly string? _exampleSetName;

        public ExampleGenerator(TypeMapper typeMapper, string? exampleSetName = null)
        {
            _typeMapper = typeMapper ?? throw new ArgumentNullException(nameof(typeMapper));
            _exampleSetName = exampleSetName;
        }

        /// <summary>
        /// Generates C# code for a default value from an OpenAPI schema and examples.
        /// </summary>
        /// <param name="schema">The schema to generate a value for</param>
        /// <param name="typeName">The C# type name</param>
        /// <param name="mediaType">Media type containing examples</param>
        /// <returns>C# code string (e.g., "new Pet { Id = 1, Name = \"Fluffy\" }")</returns>
        public string GenerateDefaultValue(OpenApiSchema schema, string typeName, OpenApiMediaType? mediaType = null)
        {
            // Try to get named example from media type if UseExampleSet is specified
            if (!string.IsNullOrEmpty(_exampleSetName) && mediaType?.Examples != null && mediaType.Examples.Any())
            {
                // Look for the specific named example
                if (mediaType.Examples.TryGetValue(_exampleSetName, out var namedExample) && namedExample?.Value != null)
                {
                    return GenerateFromExample(namedExample.Value, typeName, schema);
                }
            }
            
            // Try to get example from media type (first example or direct example)
            if (mediaType?.Examples != null && mediaType.Examples.Any())
            {
                var example = mediaType.Examples.First().Value;
                if (example.Value != null)
                {
                    return GenerateFromExample(example.Value, typeName, schema);
                }
            }

            // Try to get example from media type directly
            if (mediaType?.Example != null)
            {
                return GenerateFromExample(mediaType.Example, typeName, schema);
            }

            // Try to get example from schema
            if (schema.Example != null)
            {
                return GenerateFromExample(schema.Example, typeName, schema);
            }

            // Generate from schema definition
            return GenerateFromSchema(schema, typeName);
        }

        /// <summary>
        /// Generates a value from an OpenAPI example.
        /// </summary>
        private string GenerateFromExample(IOpenApiAny example, string typeName, OpenApiSchema schema)
        {
            switch (example)
            {
                case OpenApiObject obj:
                    return GenerateObjectFromExample(obj, typeName);

                case OpenApiArray arr:
                    return GenerateArrayFromExample(arr, typeName);

                case OpenApiString str:
                    return $"\"{EscapeString(str.Value)}\"";

                case OpenApiInteger intVal:
                    return intVal.Value.ToString();

                case OpenApiLong longVal:
                    return $"{longVal.Value}L";

                case OpenApiDouble doubleVal:
                    return $"{doubleVal.Value}";

                case OpenApiFloat floatVal:
                    return $"{floatVal.Value}f";

                case OpenApiBoolean boolVal:
                    return boolVal.Value ? "true" : "false";

                case OpenApiNull:
                    return "null!";

                default:
                    return GenerateFromSchema(schema, typeName);
            }
        }

        /// <summary>
        /// Generates an object instantiation from an OpenAPI object example.
        /// </summary>
        private string GenerateObjectFromExample(OpenApiObject obj, string typeName)
        {
            var sb = new StringBuilder();
            sb.Append($"new {typeName} {{ ");

            var properties = obj.Select(kvp =>
            {
                var propName = ToPascalCase(kvp.Key);
                var propValue = kvp.Value switch
                {
                    OpenApiString s => $"\"{EscapeString(s.Value)}\"",
                    OpenApiInteger i => i.Value.ToString(),
                    OpenApiLong l => $"{l.Value}L",
                    OpenApiDouble d => $"{d.Value}",
                    OpenApiFloat f => $"{f.Value}f",
                    OpenApiBoolean b => b.Value ? "true" : "false",
                    OpenApiNull => "null!",
                    _ => "default"
                };

                return $"{propName} = {propValue}";
            });

            sb.Append(string.Join(", ", properties));
            sb.Append(" }");

            return sb.ToString();
        }

        /// <summary>
        /// Generates an array from an OpenAPI array example.
        /// </summary>
        private string GenerateArrayFromExample(OpenApiArray arr, string typeName)
        {
            // Extract item type from array type (e.g., "Pet[]" -> "Pet")
            var itemType = typeName.TrimEnd('[', ']');

            var sb = new StringBuilder();
            sb.Append("new[] { ");

            var items = arr.Select(item =>
            {
                return item switch
                {
                    OpenApiObject obj => GenerateObjectFromExample(obj, itemType),
                    OpenApiString s => $"\"{EscapeString(s.Value)}\"",
                    OpenApiInteger i => i.Value.ToString(),
                    OpenApiLong l => $"{l.Value}L",
                    _ => "default"
                };
            });

            sb.Append(string.Join(", ", items));
            sb.Append(" }");

            return sb.ToString();
        }

        /// <summary>
        /// Generates a default value from a schema definition (when no example is available).
        /// </summary>
        private string GenerateFromSchema(OpenApiSchema schema, string typeName)
        {
            if (schema == null)
                return "default";

            // Handle allOf - merge examples from composed schemas
            if (schema.AllOf != null && schema.AllOf.Any())
            {
                return GenerateFromAllOf(schema, typeName);
            }

            // Handle arrays
            if (schema.Type == "array")
            {
                return $"Array.Empty<{typeName.TrimEnd('[', ']')}>()";
            }

            // Handle objects
            if (schema.Type == "object" || schema.Properties?.Any() == true)
            {
                return $"new {typeName}()";
            }

            // Handle primitives
            return schema.Type?.ToLowerInvariant() switch
            {
                "string" => "\"\"",
                "integer" => "0",
                "number" => "0.0",
                "boolean" => "false",
                _ => "default"
            };
        }

        /// <summary>
        /// Generates a default value for allOf schemas by merging examples from composed schemas.
        /// </summary>
        private string GenerateFromAllOf(OpenApiSchema schema, string typeName)
        {
            var properties = new System.Collections.Generic.Dictionary<string, string>();

            // Collect examples from all schemas in allOf
            foreach (var subSchema in schema.AllOf)
            {
                // Check if subSchema has an example
                if (subSchema.Example != null && subSchema.Example is OpenApiObject obj)
                {
                    foreach (var kvp in obj)
                    {
                        var propName = ToPascalCase(kvp.Key);
                        var propValue = kvp.Value switch
                        {
                            OpenApiString s => $"\"{EscapeString(s.Value)}\"",
                            OpenApiInteger i => i.Value.ToString(),
                            OpenApiLong l => $"{l.Value}L",
                            OpenApiDouble d => $"{d.Value}",
                            OpenApiFloat f => $"{f.Value}f",
                            OpenApiBoolean b => b.Value ? "true" : "false",
                            _ => "default!"
                        };
                        properties[propName] = propValue;
                    }
                }

                // Also collect from properties if available
                if (subSchema.Properties != null)
                {
                    foreach (var prop in subSchema.Properties)
                    {
                        var propName = ToPascalCase(prop.Key);
                        if (!properties.ContainsKey(propName) && prop.Value.Example != null)
                        {
                            var propValue = prop.Value.Example switch
                            {
                                OpenApiString s => $"\"{EscapeString(s.Value)}\"",
                                OpenApiInteger i => i.Value.ToString(),
                                OpenApiLong l => $"{l.Value}L",
                                OpenApiDouble d => $"{d.Value}",
                                OpenApiFloat f => $"{f.Value}f",
                                OpenApiBoolean b => b.Value ? "true" : "false",
                                _ => "default!"
                            };
                            properties[propName] = propValue;
                        }
                    }
                }
            }

            // If we collected examples, generate object initializer
            if (properties.Any())
            {
                var sb = new StringBuilder();
                sb.Append($"new {typeName} {{ ");
                sb.Append(string.Join(", ", properties.Select(kvp => $"{kvp.Key} = {kvp.Value}")));
                sb.Append(" }");
                return sb.ToString();
            }

            // Fallback to default object creation
            return $"new {typeName}()";
        }

        /// <summary>
        /// Converts a string to PascalCase for property names.
        /// </summary>
        private string ToPascalCase(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            var parts = input.Split(new[] { '_', '-', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var sb = new StringBuilder();

            foreach (var part in parts)
            {
                if (part.Length > 0)
                {
                    sb.Append(char.ToUpperInvariant(part[0]));
                    if (part.Length > 1)
                        sb.Append(part.Substring(1));
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Escapes a string for C# code generation.
        /// </summary>
        private string EscapeString(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            return value.Replace("\\", "\\\\")
                        .Replace("\"", "\\\"")
                        .Replace("\n", "\\n")
                        .Replace("\r", "\\r")
                        .Replace("\t", "\\t");
        }
    }
}
