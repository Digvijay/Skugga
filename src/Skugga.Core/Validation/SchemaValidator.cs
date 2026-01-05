using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Skugga.Core.Exceptions;

namespace Skugga.Core.Validation
{
    /// <summary>
    /// Runtime validator for OpenAPI schema compliance.
    /// Validates response objects against OpenAPI schema definitions.
    /// </summary>
    public static class SchemaValidator
    {
        /// <summary>
        /// Validates an object against expected type and required properties.
        /// </summary>
        /// <param name="value">The value to validate.</param>
        /// <param name="expectedType">The expected .NET type.</param>
        /// <param name="fieldPath">The field path for error reporting.</param>
        /// <param name="requiredProperties">List of required property names (for objects).</param>
        /// <param name="enumValues">Valid enum values (if applicable).</param>
        public static void ValidateValue(
            object? value, 
            Type expectedType, 
            string fieldPath, 
            string[]? requiredProperties = null,
            string[]? enumValues = null)
        {
            if (value == null)
            {
                throw new ContractViolationException(
                    $"Field '{fieldPath}' is required but was null",
                    fieldPath,
                    expectedType.Name,
                    "null");
            }

            // Type validation
            if (!expectedType.IsInstanceOfType(value))
            {
                throw new ContractViolationException(
                    $"Field '{fieldPath}' expected type '{expectedType.Name}', got '{value.GetType().Name}'",
                    fieldPath,
                    expectedType.Name,
                    value.GetType().Name);
            }

            // Enum validation
            if (enumValues != null && enumValues.Length > 0)
            {
                var stringValue = value.ToString();
                if (!enumValues.Contains(stringValue))
                {
                    throw new ContractViolationException(
                        $"Field '{fieldPath}' has invalid enum value '{stringValue}'. Valid values: {string.Join(", ", enumValues)}",
                        fieldPath,
                        $"One of: {string.Join(", ", enumValues)}",
                        stringValue);
                }
            }

            // Required properties validation (for objects)
            if (requiredProperties != null && requiredProperties.Length > 0)
            {
                ValidateRequiredProperties(value, fieldPath, requiredProperties);
            }
        }

        /// <summary>
        /// Validates that all required properties are present on an object.
        /// </summary>
        public static void ValidateRequiredProperties(object obj, string fieldPath, string[] requiredProperties)
        {
            var type = obj.GetType();
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            
            foreach (var requiredProp in requiredProperties)
            {
                var prop = properties.FirstOrDefault(p => 
                    string.Equals(p.Name, requiredProp, StringComparison.OrdinalIgnoreCase));
                
                if (prop == null)
                {
                    throw new ContractViolationException(
                        $"Required property '{requiredProp}' not found on object at '{fieldPath}'",
                        $"{fieldPath}.{requiredProp}",
                        $"Property '{requiredProp}'",
                        "missing");
                }

                var value = prop.GetValue(obj);
                if (value == null)
                {
                    throw new ContractViolationException(
                        $"Required property '{requiredProp}' at '{fieldPath}' is null",
                        $"{fieldPath}.{requiredProp}",
                        $"Non-null value",
                        "null");
                }
            }
        }

        /// <summary>
        /// Validates an array/list against item schema.
        /// </summary>
        public static void ValidateArray<T>(
            IEnumerable<T>? array, 
            string fieldPath,
            Type expectedItemType,
            string[]? itemRequiredProperties = null)
        {
            if (array == null)
            {
                throw new ContractViolationException(
                    $"Array field '{fieldPath}' is null",
                    fieldPath,
                    "Array",
                    "null");
            }

            int index = 0;
            foreach (var item in array)
            {
                var itemPath = $"{fieldPath}[{index}]";
                
                if (item == null)
                {
                    throw new ContractViolationException(
                        $"Array item at '{itemPath}' is null",
                        itemPath,
                        expectedItemType.Name,
                        "null");
                }

                if (!expectedItemType.IsInstanceOfType(item))
                {
                    throw new ContractViolationException(
                        $"Array item at '{itemPath}' expected type '{expectedItemType.Name}', got '{item.GetType().Name}'",
                        itemPath,
                        expectedItemType.Name,
                        item.GetType().Name);
                }

                if (itemRequiredProperties != null && itemRequiredProperties.Length > 0)
                {
                    ValidateRequiredProperties(item, itemPath, itemRequiredProperties);
                }

                index++;
            }
        }

        /// <summary>
        /// Validates string format constraints (email, uri, date-time, etc.).
        /// </summary>
        public static void ValidateStringFormat(string? value, string format, string fieldPath)
        {
            if (string.IsNullOrEmpty(value))
                return;

            bool isValid = format.ToLowerInvariant() switch
            {
                "email" => value.Contains("@") && value.Contains("."),
                "uri" or "url" => Uri.TryCreate(value, UriKind.Absolute, out _),
                "date-time" => DateTime.TryParse(value, out _),
                "date" => DateTime.TryParseExact(value, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out _),
                "uuid" => Guid.TryParse(value, out _),
                _ => true // Unknown format, skip validation
            };

            if (!isValid)
            {
                throw new ContractViolationException(
                    $"Field '{fieldPath}' does not match format '{format}'. Value: '{value}'",
                    fieldPath,
                    $"Format: {format}",
                    value);
            }
        }

        /// <summary>
        /// Validates numeric constraints (minimum, maximum, etc.).
        /// </summary>
        public static void ValidateNumericConstraints(
            double value, 
            string fieldPath,
            double? minimum = null,
            double? maximum = null,
            bool exclusiveMinimum = false,
            bool exclusiveMaximum = false)
        {
            if (minimum.HasValue)
            {
                if (exclusiveMinimum && value <= minimum.Value)
                {
                    throw new ContractViolationException(
                        $"Field '{fieldPath}' must be > {minimum.Value}, got {value}",
                        fieldPath,
                        $"> {minimum.Value}",
                        value.ToString());
                }
                else if (!exclusiveMinimum && value < minimum.Value)
                {
                    throw new ContractViolationException(
                        $"Field '{fieldPath}' must be >= {minimum.Value}, got {value}",
                        fieldPath,
                        $">= {minimum.Value}",
                        value.ToString());
                }
            }

            if (maximum.HasValue)
            {
                if (exclusiveMaximum && value >= maximum.Value)
                {
                    throw new ContractViolationException(
                        $"Field '{fieldPath}' must be < {maximum.Value}, got {value}",
                        fieldPath,
                        $"< {maximum.Value}",
                        value.ToString());
                }
                else if (!exclusiveMaximum && value > maximum.Value)
                {
                    throw new ContractViolationException(
                        $"Field '{fieldPath}' must be <= {maximum.Value}, got {value}",
                        fieldPath,
                        $"<= {maximum.Value}",
                        value.ToString());
                }
            }
        }
    }
}
