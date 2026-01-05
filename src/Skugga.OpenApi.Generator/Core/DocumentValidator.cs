using Microsoft.CodeAnalysis;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Any;
using System.Collections.Generic;
using System.Linq;

namespace Skugga.OpenApi.Generator
{
    /// <summary>
    /// Performs comprehensive build-time validation of OpenAPI documents.
    /// Catches common issues like invalid references, circular dependencies, type mismatches, and best practice violations.
    /// </summary>
    internal class DocumentValidator
    {
        private readonly SourceProductionContext _context;
        private readonly OpenApiDocument _document;
        private readonly HashSet<string> _visitedSchemas = new HashSet<string>();

        public DocumentValidator(SourceProductionContext context, OpenApiDocument document)
        {
            _context = context;
            _document = document;
        }

        /// <summary>
        /// Validates the entire OpenAPI document and reports all issues as diagnostics.
        /// </summary>
        public void Validate()
        {
            ValidateInfo();
            ValidateSchemas();
            ValidatePaths();
            ValidateReferences();
        }

        /// <summary>
        /// Validates the info section has required fields.
        /// </summary>
        private void ValidateInfo()
        {
            if (_document.Info == null)
            {
                ReportWarning("SKUGGA_OPENAPI_014", "Missing Info Section",
                    "OpenAPI document is missing 'info' section.\n\n" +
                    "💡 Fix: Add info section:\n" +
                    "   info:\n" +
                    "     title: My API\n" +
                    "     version: 1.0.0");
                return;
            }

            if (string.IsNullOrEmpty(_document.Info.Title))
            {
                ReportWarning("SKUGGA_OPENAPI_015", "Missing API Title",
                    "API title is missing. This helps document the purpose of your API.\n\n" +
                    "💡 Fix: Add title to info section:\n" +
                    "   info:\n" +
                    "     title: My API Name");
            }

            if (string.IsNullOrEmpty(_document.Info.Version))
            {
                ReportWarning("SKUGGA_OPENAPI_016", "Missing API Version",
                    "API version is missing. Version helps track API changes.\n\n" +
                    "💡 Fix: Add version to info section:\n" +
                    "   info:\n" +
                    "     version: 1.0.0");
            }
        }

        /// <summary>
        /// Validates all schemas for common issues.
        /// </summary>
        private void ValidateSchemas()
        {
            if (_document.Components?.Schemas == null) return;

            foreach (var schemaEntry in _document.Components.Schemas)
            {
                var schemaName = schemaEntry.Key;
                var schema = schemaEntry.Value;

                if (schema == null)
                {
                    ReportWarning("SKUGGA_OPENAPI_012", "Null Schema",
                        $"Schema '{schemaName}' is null and will be skipped.");
                    continue;
                }

                ValidateSchema(schemaName, schema);
            }
        }

        /// <summary>
        /// Validates a single schema for type consistency and required properties.
        /// </summary>
        private void ValidateSchema(string schemaName, OpenApiSchema schema)
        {
            // Check for circular references
            if (_visitedSchemas.Contains(schemaName))
            {
                ReportWarning("SKUGGA_OPENAPI_017", "Circular Schema Reference",
                    $"Schema '{schemaName}' has circular reference. This may cause issues.\n\n" +
                    "💡 Consider: Break the cycle using oneOf/anyOf or restructure your schemas.");
                return;
            }

            _visitedSchemas.Add(schemaName);

            // Validate object schemas have properties defined
            if (schema.Type == "object")
            {
                if ((schema.Properties == null || schema.Properties.Count == 0) && 
                    (schema.AllOf == null || schema.AllOf.Count == 0))
                {
                    ReportInfo("SKUGGA_OPENAPI_018", "Empty Object Schema",
                        $"Schema '{schemaName}' is an object but has no properties defined.\n\n" +
                        "💡 Consider: Add properties or use allOf to compose from other schemas.");
                }

                // Check if required properties exist in properties collection
                if (schema.Required != null && schema.Required.Count > 0 && schema.Properties != null)
                {
                    foreach (var requiredProp in schema.Required)
                    {
                        if (!schema.Properties.ContainsKey(requiredProp))
                        {
                            ReportWarning("SKUGGA_OPENAPI_019", "Required Property Not Defined",
                                $"Schema '{schemaName}' marks '{requiredProp}' as required but it's not in properties.\n\n" +
                                "💡 Fix: Add the property or remove it from required list.");
                        }
                    }
                }
            }

            // Validate array schemas have items defined
            if (schema.Type == "array" && schema.Items == null)
            {
                ReportWarning("SKUGGA_OPENAPI_020", "Array Without Items",
                    $"Schema '{schemaName}' is an array but has no 'items' defined.\n\n" +
                    "💡 Fix: Specify the array item type:\n" +
                    "   type: array\n" +
                    "   items:\n" +
                    "     type: string");
            }

            // Validate enum values match the schema type
            if (schema.Enum != null && schema.Enum.Count > 0 && !string.IsNullOrEmpty(schema.Type))
            {
                ValidateEnumValues(schemaName, schema);
            }

            // Validate example values against enum constraints
            if (schema.Enum != null && schema.Enum.Count > 0 && schema.Example != null)
            {
                ValidateExampleAgainstEnum(schemaName, schema, schema.Example, "schema example");
            }

            // Validate properties with enum constraints
            if (schema.Properties != null)
            {
                foreach (var property in schema.Properties)
                {
                    var propSchema = property.Value;
                    if (propSchema?.Enum != null && propSchema.Enum.Count > 0)
                    {
                        // Validate property's enum type matches
                        if (!string.IsNullOrEmpty(propSchema.Type))
                        {
                            ValidateEnumValues($"{schemaName}.{property.Key}", propSchema);
                        }

                        // Validate property's example against enum
                        if (propSchema.Example != null)
                        {
                            ValidateExampleAgainstEnum($"property '{property.Key}' in schema '{schemaName}'",
                                propSchema, propSchema.Example, "property example");
                        }
                    }
                }
            }

            _visitedSchemas.Remove(schemaName);
        }

        /// <summary>
        /// Validates that enum values match the declared schema type.
        /// </summary>
        private void ValidateEnumValues(string schemaName, OpenApiSchema schema)
        {
            var firstEnum = schema.Enum.FirstOrDefault();
            if (firstEnum == null) return;

            var enumTypeName = firstEnum.GetType().Name;
            
            // Check for type mismatches
            if (schema.Type == "integer")
            {
                if (!enumTypeName.Contains("Int") && !enumTypeName.Contains("Long"))
                {
                    ReportWarning("SKUGGA_OPENAPI_021", "Enum Type Mismatch",
                        $"Schema '{schemaName}' type is 'integer' but enum values appear to be {enumTypeName}.\n\n" +
                        "💡 Fix: Ensure enum values match the schema type:\n" +
                        "   type: integer\n" +
                        "   enum: [1, 2, 3]  # ✓ Correct\n" +
                        "   enum: [\"1\", \"2\", \"3\"]  # ✗ Wrong (strings, not integers)");
                }
            }
            else if (schema.Type == "string")
            {
                if (!enumTypeName.Contains("String"))
                {
                    ReportWarning("SKUGGA_OPENAPI_021", "Enum Type Mismatch",
                        $"Schema '{schemaName}' type is 'string' but enum values appear to be {enumTypeName}.\n\n" +
                        "💡 Fix: Ensure enum values are strings:\n" +
                        "   type: string\n" +
                        "   enum: [\"active\", \"inactive\"]  # ✓ Correct\n" +
                        "   enum: [1, 2]  # ✗ Wrong (numbers, not strings)");
                }
            }
            else if (schema.Type == "number")
            {
                if (!enumTypeName.Contains("Double") && !enumTypeName.Contains("Float") && !enumTypeName.Contains("Int"))
                {
                    ReportWarning("SKUGGA_OPENAPI_021", "Enum Type Mismatch",
                        $"Schema '{schemaName}' type is 'number' but enum values appear to be {enumTypeName}.\n\n" +
                        "💡 Fix: Ensure enum values are numeric.");
                }
            }
        }

        /// <summary>
        /// Validates that an example value is in the enum's allowed values.
        /// </summary>
        private void ValidateExampleAgainstEnum(string context, OpenApiSchema schema, IOpenApiAny example, string exampleLocation)
        {
            if (schema.Enum == null || schema.Enum.Count == 0) return;

            // Check if the example value matches any enum value
            var exampleMatches = schema.Enum.Any(enumValue => AreOpenApiValuesEqual(enumValue, example));
            
            if (!exampleMatches)
            {
                var exampleValueStr = GetOpenApiValueString(example);
                var allowedValues = string.Join(", ", schema.Enum.Select(GetOpenApiValueString));
                var firstAllowed = GetOpenApiValueString(schema.Enum.First());
                
                _context.ReportDiagnostic(DiagnosticHelper.Create(
                    DiagnosticHelper.InvalidEnumValue,
                    Location.None,
                    exampleValueStr,
                    context,
                    allowedValues,
                    firstAllowed));
            }
        }

        /// <summary>
        /// Compares two IOpenApiAny values for equality.
        /// </summary>
        private bool AreOpenApiValuesEqual(IOpenApiAny value1, IOpenApiAny value2)
        {
            if (value1 == null && value2 == null) return true;
            if (value1 == null || value2 == null) return false;
            
            // Compare by type and value
            return value1.GetType() == value2.GetType() && 
                   GetOpenApiValueString(value1) == GetOpenApiValueString(value2);
        }

        /// <summary>
        /// Converts an IOpenApiAny value to a string representation.
        /// </summary>
        private string GetOpenApiValueString(IOpenApiAny value)
        {
            return value switch
            {
                Microsoft.OpenApi.Any.OpenApiString s => s.Value,
                Microsoft.OpenApi.Any.OpenApiInteger i => i.Value.ToString(),
                Microsoft.OpenApi.Any.OpenApiLong l => l.Value.ToString(),
                Microsoft.OpenApi.Any.OpenApiDouble d => d.Value.ToString(),
                Microsoft.OpenApi.Any.OpenApiFloat f => f.Value.ToString(),
                Microsoft.OpenApi.Any.OpenApiBoolean b => b.Value.ToString().ToLowerInvariant(),
                _ => value?.ToString() ?? "null"
            };
        }

        /// <summary>
        /// Validates parameter enum usage - checks if parameter references a schema with enums.
        /// </summary>
        private void ValidateParameterEnumUsage(OpenApiParameter parameter, string operationId)
        {
            // If parameter schema has a reference, check if it points to a schema with enums
            if (parameter.Schema?.Reference != null && _document.Components?.Schemas != null)
            {
                var refId = parameter.Schema.Reference.Id;
                if (_document.Components.Schemas.TryGetValue(refId, out var referencedSchema))
                {
                    if (referencedSchema.Enum != null && referencedSchema.Enum.Count > 0)
                    {
                        // Parameter references an enum schema - inform developer
                        var allowedValues = string.Join(", ", referencedSchema.Enum.Select(GetOpenApiValueString));
                        ReportInfo("SKUGGA_OPENAPI_029", "Parameter Uses Enum Type",
                            $"Parameter '{parameter.Name}' in '{operationId}' references schema '{refId}' which has enum constraints.\n\n" +
                            $"Allowed values: {allowedValues}");
                    }
                }
            }
        }

        /// <summary>
        /// Validates all API paths and operations.
        /// </summary>
        private void ValidatePaths()
        {
            if (_document.Paths == null) return;

            foreach (var pathEntry in _document.Paths)
            {
                var pathKey = pathEntry.Key;
                var pathItem = pathEntry.Value;

                if (pathItem?.Operations == null) continue;

                foreach (var operationEntry in pathItem.Operations)
                {
                    var method = operationEntry.Key;
                    var operation = operationEntry.Value;

                    if (operation == null) continue;

                    var operationId = operation.OperationId ?? $"{method} {pathKey}";

                    // Validate operation has description
                    if (string.IsNullOrEmpty(operation.Summary) && string.IsNullOrEmpty(operation.Description))
                    {
                        ReportInfo("SKUGGA_OPENAPI_022", "Missing Operation Documentation",
                            $"Operation '{operationId}' has no summary or description.\n\n" +
                            "💡 Consider: Add documentation to help developers understand the operation:\n" +
                            "   summary: Brief description\n" +
                            "   description: Detailed explanation");
                    }

                    // Validate parameters
                    if (operation.Parameters != null)
                    {
                        foreach (var parameter in operation.Parameters)
                        {
                            if (parameter == null) continue;

                            // Check required parameters have description
                            if (parameter.Required && string.IsNullOrEmpty(parameter.Description))
                            {
                                ReportInfo("SKUGGA_OPENAPI_023", "Required Parameter Needs Description",
                                    $"Required parameter '{parameter.Name}' in '{operationId}' has no description.\n\n" +
                                    "💡 Consider: Document what this parameter does.");
                            }

                            // Check parameter has schema
                            if (parameter.Schema == null)
                            {
                                ReportWarning("SKUGGA_OPENAPI_024", "Parameter Missing Schema",
                                    $"Parameter '{parameter.Name}' in '{operationId}' has no schema defined.\n\n" +
                                    "💡 Fix: Add schema to define parameter type:\n" +
                                    "   schema:\n" +
                                    "     type: string");
                            }
                            else
                            {
                                // Validate parameter example against enum constraint
                                if (parameter.Schema.Enum != null && parameter.Schema.Enum.Count > 0 && parameter.Example != null)
                                {
                                    ValidateExampleAgainstEnum($"parameter '{parameter.Name}' in '{operationId}'", 
                                        parameter.Schema, parameter.Example, "parameter example");
                                }

                                // Check if parameter references a schema with enums
                                ValidateParameterEnumUsage(parameter, operationId);
                            }
                        }
                    }

                    // Validate response codes are standard HTTP codes
                    if (operation.Responses != null)
                    {
                        foreach (var responseEntry in operation.Responses)
                        {
                            var statusCode = responseEntry.Key;
                            var response = responseEntry.Value;
                            
                            // Allow "default" and numeric codes
                            if (statusCode != "default" && !int.TryParse(statusCode, out var parsedCode))
                            {
                                ReportWarning("SKUGGA_OPENAPI_025", "Invalid Response Code",
                                    $"Response code '{statusCode}' in '{operationId}' is not a valid HTTP status code.\n\n" +
                                    "💡 Use: Standard HTTP codes like 200, 400, 404, or 'default'");
                            }
                            else if (statusCode != "default")
                            {
                                var code = int.Parse(statusCode);
                                // Warn about unusual status codes
                                if (code < 100 || code > 599)
                                {
                                    ReportWarning("SKUGGA_OPENAPI_026", "Unusual HTTP Status Code",
                                        $"Response code '{statusCode}' in '{operationId}' is outside typical range (100-599).\n\n" +
                                        "💡 Verify: This is the intended status code.");
                                }
                            }

                            // Validate response content examples against enum constraints
                            if (response?.Content != null)
                            {
                                foreach (var contentEntry in response.Content)
                                {
                                    var mediaType = contentEntry.Value;
                                    if (mediaType?.Schema != null)
                                    {
                                        // Validate examples against enum
                                        if (mediaType.Examples != null)
                                        {
                                            foreach (var exampleEntry in mediaType.Examples)
                                            {
                                                if (exampleEntry.Value?.Value != null && mediaType.Schema.Enum != null && mediaType.Schema.Enum.Count > 0)
                                                {
                                                    ValidateExampleAgainstEnum(
                                                        $"response {statusCode} example '{exampleEntry.Key}' in '{operationId}'",
                                                        mediaType.Schema,
                                                        exampleEntry.Value.Value,
                                                        "response example");
                                                }
                                            }
                                        }

                                        // Validate direct example
                                        if (mediaType.Example != null && mediaType.Schema.Enum != null && mediaType.Schema.Enum.Count > 0)
                                        {
                                            ValidateExampleAgainstEnum(
                                                $"response {statusCode} in '{operationId}'",
                                                mediaType.Schema,
                                                mediaType.Example,
                                                "response example");
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Validates all $ref references resolve correctly.
        /// </summary>
        private void ValidateReferences()
        {
            if (_document.Components?.Schemas == null) return;

            // Check that all schema references in operations resolve
            if (_document.Paths != null)
            {
                foreach (var pathEntry in _document.Paths)
                {
                    if (pathEntry.Value?.Operations == null) continue;

                    foreach (var operationEntry in pathEntry.Value.Operations)
                    {
                        var operation = operationEntry.Value;
                        if (operation?.Responses == null) continue;

                        foreach (var responseEntry in operation.Responses)
                        {
                            var response = responseEntry.Value;
                            if (response?.Content == null) continue;

                            foreach (var contentEntry in response.Content)
                            {
                                var schema = contentEntry.Value?.Schema;
                                if (schema != null)
                                {
                                    ValidateSchemaReferences(schema, operation.OperationId ?? "unknown");
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Validates that schema references ($ref) resolve to existing schemas.
        /// </summary>
        private void ValidateSchemaReferences(OpenApiSchema schema, string context)
        {
            if (schema.Reference != null)
            {
                var refId = schema.Reference.Id;
                if (!string.IsNullOrEmpty(refId) && 
                    _document.Components?.Schemas != null &&
                    !_document.Components.Schemas.ContainsKey(refId))
                {
                    ReportWarning("SKUGGA_OPENAPI_027", "Unresolved Schema Reference",
                        $"Schema reference '{refId}' in '{context}' does not exist in components/schemas.\n\n" +
                        "💡 Fix: Define the schema or correct the reference:\n" +
                        "   components:\n" +
                        "     schemas:\n" +
                        $"       {refId}:\n" +
                        "         type: object");
                }
            }

            // Check nested schemas
            if (schema.Items != null)
            {
                ValidateSchemaReferences(schema.Items, context);
            }

            if (schema.Properties != null)
            {
                foreach (var prop in schema.Properties.Values)
                {
                    ValidateSchemaReferences(prop, context);
                }
            }

            if (schema.AllOf != null)
            {
                foreach (var subSchema in schema.AllOf)
                {
                    ValidateSchemaReferences(subSchema, context);
                }
            }
        }

        /// <summary>
        /// Reports a warning diagnostic.
        /// </summary>
        private void ReportWarning(string id, string title, string message)
        {
            var descriptor = new DiagnosticDescriptor(
                id: id,
                title: title,
                messageFormat: message,
                category: "Skugga.OpenApi.Validation",
                defaultSeverity: DiagnosticSeverity.Warning,
                isEnabledByDefault: true);

            _context.ReportDiagnostic(Diagnostic.Create(descriptor, Location.None));
        }

        /// <summary>
        /// Reports an informational diagnostic.
        /// </summary>
        private void ReportInfo(string id, string title, string message)
        {
            var descriptor = new DiagnosticDescriptor(
                id: id,
                title: title,
                messageFormat: message,
                category: "Skugga.OpenApi.Validation",
                defaultSeverity: DiagnosticSeverity.Info,
                isEnabledByDefault: true);

            _context.ReportDiagnostic(Diagnostic.Create(descriptor, Location.None));
        }
    }
}
