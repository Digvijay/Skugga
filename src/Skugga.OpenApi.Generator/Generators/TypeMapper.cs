using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.OpenApi.Models;

namespace Skugga.OpenApi.Generator
{
    /// <summary>
    /// Maps OpenAPI schema types to C# types.
    /// Handles primitives, arrays, objects, references, and complex types (allOf, oneOf, anyOf).
    /// </summary>
    internal class TypeMapper
    {
        private readonly OpenApiDocument _document;
        private readonly Dictionary<string, string> _generatedTypes = new Dictionary<string, string>();
        private string? _interfaceNamePrefix;

        public TypeMapper(OpenApiDocument document)
        {
            _document = document ?? throw new ArgumentNullException(nameof(document));
        }

        /// <summary>
        /// Sets the interface name prefix to use for schema class names to prevent collisions
        /// </summary>
        public void SetInterfaceNamePrefix(string? interfaceName)
        {
            _interfaceNamePrefix = interfaceName;
        }

        /// <summary>
        /// Maps an OpenAPI schema to a C# type name.
        /// </summary>
        /// <param name="schema">The OpenAPI schema to map</param>
        /// <param name="typeName">Suggested type name for object schemas</param>
        /// <param name="isNullable">Whether the type should be nullable</param>
        /// <returns>C# type name (e.g., "string", "int", "Pet", "List&lt;Pet&gt;")</returns>
        public string MapType(OpenApiSchema schema, string? typeName = null, bool isNullable = false)
        {
            if (schema == null)
                return "object";

            // Check OpenAPI 3.0 nullable field
            var schemaIsNullable = schema.Nullable || isNullable;

            // Handle $ref (reference to component schema)
            if (schema.Reference != null)
            {
                var refName = GetTypeNameFromReference(schema.Reference.Id);
                var prefixedName = _interfaceNamePrefix != null ? $"{_interfaceNamePrefix}_{refName}" : refName;
                return schemaIsNullable && IsValueType(prefixedName) ? $"{prefixedName}?" : prefixedName;
            }

            // Handle allOf (inheritance/composition)
            if (schema.AllOf != null && schema.AllOf.Any())
            {
                // For discriminated unions, use the base type
                var firstSchema = schema.AllOf.FirstOrDefault(s => s.Reference != null);
                if (firstSchema != null)
                    return MapType(firstSchema, typeName, schemaIsNullable);

                // If no reference, it's a composition - use provided type name or generate one
                return typeName ?? "object";
            }

            // Handle oneOf/anyOf (polymorphism with discriminator support)
            if (schema.OneOf != null && schema.OneOf.Any())
            {
                // Check for discriminator
                if (schema.Discriminator != null && !string.IsNullOrEmpty(schema.Discriminator.PropertyName))
                {
                    // Use provided type name for discriminated union base
                    return typeName ?? "object";
                }
                // Return the first option if no discriminator
                return MapType(schema.OneOf.First(), typeName, schemaIsNullable);
            }

            if (schema.AnyOf != null && schema.AnyOf.Any())
            {
                // Similar logic for anyOf
                if (schema.Discriminator != null && !string.IsNullOrEmpty(schema.Discriminator.PropertyName))
                {
                    return typeName ?? "object";
                }
                return MapType(schema.AnyOf.First(), typeName, schemaIsNullable);
            }

            // Handle arrays
            if (schema.Type == "array" && schema.Items != null)
            {
                var itemType = MapType(schema.Items, null, false);
                return $"{itemType}[]";
            }

            // Handle objects
            if (schema.Type == "object" || schema.Properties?.Any() == true)
            {
                // If it has properties, it should be a generated class
                return typeName ?? "object";
            }

            // Handle primitives
            var csharpType = MapPrimitiveType(schema);

            // Make nullable if requested and it's a value type
            if (schemaIsNullable && IsValueType(csharpType))
                return $"{csharpType}?";

            return csharpType;
        }

        /// <summary>
        /// Maps OpenAPI primitive types to C# types.
        /// </summary>
        private string MapPrimitiveType(OpenApiSchema schema)
        {
            var type = schema.Type?.ToLowerInvariant();
            var format = schema.Format?.ToLowerInvariant();

            switch (type)
            {
                case "string":
                    return format switch
                    {
                        "date" => "DateOnly",
                        "date-time" => "DateTime",
                        "byte" => "byte[]",
                        "binary" => "byte[]",
                        "uuid" => "Guid",
                        _ => "string"
                    };

                case "integer":
                    return format switch
                    {
                        "int64" => "long",
                        "int32" => "int",
                        _ => "int"
                    };

                case "number":
                    return format switch
                    {
                        "float" => "float",
                        "double" => "double",
                        "decimal" => "decimal",
                        _ => "double"
                    };

                case "boolean":
                    return "bool";

                case "file":
                    return "byte[]";

                default:
                    return "object";
            }
        }

        /// <summary>
        /// Extracts the type name from an OpenAPI reference ID.
        /// Example: "#/components/schemas/Pet" → "Pet"
        /// </summary>
        private string GetTypeNameFromReference(string referenceId)
        {
            if (string.IsNullOrEmpty(referenceId))
                return "object";

            var parts = referenceId.Split('/');
            return parts.Last();
        }

        /// <summary>
        /// Determines if a C# type name represents a value type (needs ? for nullable).
        /// </summary>
        private bool IsValueType(string typeName)
        {
            return typeName switch
            {
                "int" or "long" or "short" or "byte" or "sbyte" or
                "uint" or "ulong" or "ushort" or
                "float" or "double" or "decimal" or
                "bool" or "char" or
                "DateTime" or "DateOnly" or "TimeOnly" or "Guid" or "TimeSpan"
                    => true,
                _ => typeName.EndsWith("?") || false
            };
        }

        /// <summary>
        /// Gets all component schemas that need to be generated as classes.
        /// </summary>
        public IEnumerable<KeyValuePair<string, OpenApiSchema>> GetComponentSchemas()
        {
            if (_document.Components?.Schemas == null)
                return Enumerable.Empty<KeyValuePair<string, OpenApiSchema>>();

            return _document.Components.Schemas;
        }

        /// <summary>
        /// Determines if a schema should generate a class (has properties or is an object).
        /// </summary>
        public bool ShouldGenerateClass(OpenApiSchema schema)
        {
            if (schema == null)
                return false;

            // Generate class if it has properties
            if (schema.Properties?.Any() == true)
                return true;

            // Generate class if it's an object type (even without explicit properties)
            if (schema.Type == "object")
                return true;

            // Generate class for allOf compositions
            if (schema.AllOf?.Any() == true)
                return true;

            // Generate class for oneOf/anyOf (polymorphic types)
            if (schema.OneOf?.Any() == true || schema.AnyOf?.Any() == true)
                return true;

            return false;
        }
    }
}
