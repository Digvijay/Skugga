using Microsoft.CodeAnalysis;
using Microsoft.OpenApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Skugga.OpenApi.Generator.Validation
{
    /// <summary>
    /// Spectral-inspired OpenAPI linting rules for build-time quality checks.
    /// Implements industry-standard best practices from https://stoplight.io/open-source/spectral
    /// 
    /// Rule Categories:
    /// - oas3-schema: OpenAPI 3.0 schema validation
    /// - operation-*: Operation-level rules
    /// - info-*: Info section rules
    /// - path-*: Path-level rules
    /// - tag-*: Tag-level rules
    /// </summary>
    internal class OpenApiLintingRules
    {
        private readonly SourceProductionContext _context;
        private readonly OpenApiDocument _document;
        private readonly LintingConfiguration _config;

        public OpenApiLintingRules(SourceProductionContext context, OpenApiDocument document, LintingConfiguration config)
        {
            _context = context;
            _document = document;
            _config = config;
        }

        /// <summary>
        /// Runs all enabled linting rules against the OpenAPI document.
        /// </summary>
        public void Lint()
        {
            if (!_config.EnableLinting)
                return;

            // Info section rules
            LintInfoContact();
            LintInfoDescription();
            LintInfoLicense();

            // Operation rules
            LintOperationOperationId();
            LintOperationTags();
            LintOperationDescription();
            LintOperationSummary();
            LintOperationSuccessResponse();
            LintOperationParameters();

            // Path rules
            LintPathParameters();
            LintNoIdenticalPaths();

            // Tag rules
            LintTagDescription();
            LintOrphanTags();

            // Schema rules
            LintTypedEnum();
            LintSchemaDescription();

            // Security rules
            LintNoUnusedComponents();
        }

        #region Info Section Rules

        /// <summary>
        /// Rule: info-contact
        /// Severity: warn
        /// Info object must have "contact" object with at least one of url, email, or name.
        /// </summary>
        private void LintInfoContact()
        {
            if (!_config.IsRuleEnabled("info-contact")) return;

            if (_document.Info?.Contact == null)
            {
                ReportDiagnostic("SKUGGA_LINT_001", DiagnosticSeverity.Warning,
                    "OpenAPI Info: Missing Contact Information",
                    "Info object should have a contact object with URL, email, or name.\n\n" +
                    "💡 Fix: Add contact information:\n" +
                    "   info:\n" +
                    "     contact:\n" +
                    "       name: API Support\n" +
                    "       email: support@example.com\n" +
                    "       url: https://example.com/support\n\n" +
                    "🔗 Spectral Rule: info-contact");
            }
        }

        /// <summary>
        /// Rule: info-description
        /// Severity: warn
        /// Info object must have "description" field.
        /// </summary>
        private void LintInfoDescription()
        {
            if (!_config.IsRuleEnabled("info-description")) return;

            if (string.IsNullOrWhiteSpace(_document.Info?.Description))
            {
                ReportDiagnostic("SKUGGA_LINT_002", DiagnosticSeverity.Warning,
                    "OpenAPI Info: Missing Description",
                    "Info object should have a description explaining the API's purpose.\n\n" +
                    "💡 Fix: Add description:\n" +
                    "   info:\n" +
                    "     description: |\n" +
                    "       This API provides endpoints for managing...\n\n" +
                    "🔗 Spectral Rule: info-description");
            }
        }

        /// <summary>
        /// Rule: info-license
        /// Severity: warn
        /// Info object must have "license" object.
        /// </summary>
        private void LintInfoLicense()
        {
            if (!_config.IsRuleEnabled("info-license")) return;

            if (_document.Info?.License == null)
            {
                ReportDiagnostic("SKUGGA_LINT_003", DiagnosticSeverity.Info,
                    "OpenAPI Info: Missing License Information",
                    "Consider adding license information to your API.\n\n" +
                    "💡 Fix: Add license:\n" +
                    "   info:\n" +
                    "     license:\n" +
                    "       name: Apache 2.0\n" +
                    "       url: https://www.apache.org/licenses/LICENSE-2.0.html\n\n" +
                    "🔗 Spectral Rule: info-license");
            }
        }

        #endregion

        #region Operation Rules

        /// <summary>
        /// Rule: operation-operationId
        /// Severity: warn
        /// Operation must have "operationId" defined.
        /// </summary>
        private void LintOperationOperationId()
        {
            if (!_config.IsRuleEnabled("operation-operationId")) return;

            if (_document.Paths == null) return;

            foreach (var path in _document.Paths)
            {
                if (path.Value?.Operations == null) continue;

                foreach (var operation in path.Value.Operations)
                {
                    if (string.IsNullOrWhiteSpace(operation.Value?.OperationId))
                    {
                        ReportDiagnostic("SKUGGA_LINT_004", DiagnosticSeverity.Warning,
                            $"Operation: Missing operationId for {operation.Key} {path.Key}",
                            $"Operation '{operation.Key} {path.Key}' should have an operationId for SDK generation and documentation.\n\n" +
                            "💡 Fix: Add operationId:\n" +
                            $"   {path.Key}:\n" +
                            $"     {operation.Key.ToString().ToLowerInvariant()}:\n" +
                            $"       operationId: getUserById\n\n" +
                            "🔗 Spectral Rule: operation-operationId");
                    }
                }
            }
        }

        /// <summary>
        /// Rule: operation-tags
        /// Severity: warn
        /// Operation must have at least one "tag" defined.
        /// </summary>
        private void LintOperationTags()
        {
            if (!_config.IsRuleEnabled("operation-tags")) return;

            if (_document.Paths == null) return;

            foreach (var path in _document.Paths)
            {
                if (path.Value?.Operations == null) continue;

                foreach (var operation in path.Value.Operations)
                {
                    if (operation.Value?.Tags == null || !operation.Value.Tags.Any())
                    {
                        var operationId = operation.Value?.OperationId ?? $"{operation.Key} {path.Key}";
                        ReportDiagnostic("SKUGGA_LINT_005", DiagnosticSeverity.Warning,
                            $"Operation: Missing tags for {operationId}",
                            $"Operation '{operationId}' should have at least one tag for organization.\n\n" +
                            "💡 Fix: Add tags:\n" +
                            $"   {path.Key}:\n" +
                            $"     {operation.Key.ToString().ToLowerInvariant()}:\n" +
                            "       tags:\n" +
                            "         - Users\n\n" +
                            "🔗 Spectral Rule: operation-tags");
                    }
                }
            }
        }

        /// <summary>
        /// Rule: operation-description
        /// Severity: warn
        /// Operation must have "description" field.
        /// </summary>
        private void LintOperationDescription()
        {
            if (!_config.IsRuleEnabled("operation-description")) return;

            if (_document.Paths == null) return;

            foreach (var path in _document.Paths)
            {
                if (path.Value?.Operations == null) continue;

                foreach (var operation in path.Value.Operations)
                {
                    if (string.IsNullOrWhiteSpace(operation.Value?.Description))
                    {
                        var operationId = operation.Value?.OperationId ?? $"{operation.Key} {path.Key}";
                        ReportDiagnostic("SKUGGA_LINT_006", DiagnosticSeverity.Info,
                            $"Operation: Missing description for {operationId}",
                            $"Consider adding a description to operation '{operationId}'.\n\n" +
                            "💡 Fix: Add description:\n" +
                            $"   {path.Key}:\n" +
                            $"     {operation.Key.ToString().ToLowerInvariant()}:\n" +
                            "       description: Retrieves a user by their ID\n\n" +
                            "🔗 Spectral Rule: operation-description");
                    }
                }
            }
        }

        /// <summary>
        /// Rule: operation-summary
        /// Severity: warn
        /// Operation should have "summary" field.
        /// </summary>
        private void LintOperationSummary()
        {
            if (!_config.IsRuleEnabled("operation-summary")) return;

            if (_document.Paths == null) return;

            foreach (var path in _document.Paths)
            {
                if (path.Value?.Operations == null) continue;

                foreach (var operation in path.Value.Operations)
                {
                    if (string.IsNullOrWhiteSpace(operation.Value?.Summary))
                    {
                        var operationId = operation.Value?.OperationId ?? $"{operation.Key} {path.Key}";
                        ReportDiagnostic("SKUGGA_LINT_007", DiagnosticSeverity.Info,
                            $"Operation: Missing summary for {operationId}",
                            $"Consider adding a summary to operation '{operationId}'.\n\n" +
                            "💡 Fix: Add summary:\n" +
                            $"   {path.Key}:\n" +
                            $"     {operation.Key.ToString().ToLowerInvariant()}:\n" +
                            "       summary: Get user by ID\n\n" +
                            "🔗 Spectral Rule: operation-summary");
                    }
                }
            }
        }

        /// <summary>
        /// Rule: operation-success-response
        /// Severity: warn
        /// Operation must have at least one "2xx" or "3xx" response.
        /// </summary>
        private void LintOperationSuccessResponse()
        {
            if (!_config.IsRuleEnabled("operation-success-response")) return;

            if (_document.Paths == null) return;

            foreach (var path in _document.Paths)
            {
                if (path.Value?.Operations == null) continue;

                foreach (var operation in path.Value.Operations)
                {
                    if (operation.Value?.Responses == null || !operation.Value.Responses.Any())
                    {
                        var operationId = operation.Value?.OperationId ?? $"{operation.Key} {path.Key}";
                        ReportDiagnostic("SKUGGA_LINT_008", DiagnosticSeverity.Error,
                            $"Operation: No responses defined for {operationId}",
                            $"Operation '{operationId}' must have at least one response defined.\n\n" +
                            "💡 Fix: Add success response:\n" +
                            $"   {path.Key}:\n" +
                            $"     {operation.Key.ToString().ToLowerInvariant()}:\n" +
                            "       responses:\n" +
                            "         '200':\n" +
                            "           description: Success\n\n" +
                            "🔗 Spectral Rule: operation-success-response");
                        continue;
                    }

                    var hasSuccessResponse = operation.Value.Responses.Keys.Any(statusCode =>
                    {
                        if (statusCode.StartsWith("2") || statusCode.StartsWith("3"))
                            return true;
                        if (statusCode == "default")
                            return true; // Assume default is success unless proven otherwise
                        return false;
                    });

                    if (!hasSuccessResponse)
                    {
                        var operationId = operation.Value?.OperationId ?? $"{operation.Key} {path.Key}";
                        ReportDiagnostic("SKUGGA_LINT_009", DiagnosticSeverity.Warning,
                            $"Operation: No success response for {operationId}",
                            $"Operation '{operationId}' should have at least one 2xx or 3xx response.\n\n" +
                            "💡 Fix: Add success response:\n" +
                            "       responses:\n" +
                            "         '200':\n" +
                            "           description: Success\n\n" +
                            "🔗 Spectral Rule: operation-success-response");
                    }
                }
            }
        }

        /// <summary>
        /// Rule: operation-parameters
        /// Severity: warn
        /// Operation parameters must have descriptions.
        /// </summary>
        private void LintOperationParameters()
        {
            if (!_config.IsRuleEnabled("operation-parameters")) return;

            if (_document.Paths == null) return;

            foreach (var path in _document.Paths)
            {
                if (path.Value?.Operations == null) continue;

                foreach (var operation in path.Value.Operations)
                {
                    if (operation.Value?.Parameters == null) continue;

                    foreach (var parameter in operation.Value.Parameters)
                    {
                        if (string.IsNullOrWhiteSpace(parameter.Description))
                        {
                            var operationId = operation.Value?.OperationId ?? $"{operation.Key} {path.Key}";
                            ReportDiagnostic("SKUGGA_LINT_010", DiagnosticSeverity.Info,
                                $"Operation: Parameter '{parameter.Name}' in {operationId} missing description",
                                $"Parameter '{parameter.Name}' should have a description.\n\n" +
                                "💡 Fix: Add description:\n" +
                                "       parameters:\n" +
                                $"         - name: {parameter.Name}\n" +
                                $"           in: {parameter.In}\n" +
                                "           description: Explanation of this parameter\n\n" +
                                "🔗 Spectral Rule: operation-parameters");
                        }
                    }
                }
            }
        }

        #endregion

        #region Path Rules

        /// <summary>
        /// Rule: path-parameters
        /// Severity: warn
        /// Path parameters must be defined in the path string and operation.
        /// </summary>
        private void LintPathParameters()
        {
            if (!_config.IsRuleEnabled("path-parameters")) return;

            if (_document.Paths == null) return;

            foreach (var path in _document.Paths)
            {
                // Extract path parameters from the path string (e.g., /users/{userId})
                var pathParams = new List<string>();
                var startIndex = 0;
                while ((startIndex = path.Key.IndexOf('{', startIndex)) != -1)
                {
                    var endIndex = path.Key.IndexOf('}', startIndex);
                    if (endIndex == -1) break;
                    pathParams.Add(path.Key.Substring(startIndex + 1, endIndex - startIndex - 1));
                    startIndex = endIndex + 1;
                }

                if (!pathParams.Any()) continue;

                if (path.Value?.Operations == null) continue;

                foreach (var operation in path.Value.Operations)
                {
                    var operationParams = operation.Value?.Parameters?
                        .Where(p => p.In == ParameterLocation.Path)
                        .Select(p => p.Name)
                        .ToList() ?? new List<string>();

                    foreach (var pathParam in pathParams)
                    {
                        if (!operationParams.Contains(pathParam))
                        {
                            var operationId = operation.Value?.OperationId ?? $"{operation.Key} {path.Key}";
                            ReportDiagnostic("SKUGGA_LINT_011", DiagnosticSeverity.Error,
                                $"Path: Missing parameter definition for '{pathParam}' in {operationId}",
                                $"Path parameter '{pathParam}' in path '{path.Key}' must be defined in operation parameters.\n\n" +
                                "💡 Fix: Add parameter definition:\n" +
                                "       parameters:\n" +
                                $"         - name: {pathParam}\n" +
                                "           in: path\n" +
                                "           required: true\n" +
                                "           schema:\n" +
                                "             type: string\n\n" +
                                "🔗 Spectral Rule: path-parameters");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Rule: no-identical-paths
        /// Severity: warn
        /// Paths should not be identical except for trailing slashes.
        /// </summary>
        private void LintNoIdenticalPaths()
        {
            if (!_config.IsRuleEnabled("no-identical-paths")) return;

            if (_document.Paths == null) return;

            var normalizedPaths = new Dictionary<string, string>();
            foreach (var path in _document.Paths.Keys)
            {
                var normalized = path.TrimEnd('/');
                if (normalizedPaths.ContainsKey(normalized))
                {
                    ReportDiagnostic("SKUGGA_LINT_012", DiagnosticSeverity.Warning,
                        $"Path: Duplicate path '{path}' and '{normalizedPaths[normalized]}'",
                        $"Paths '{path}' and '{normalizedPaths[normalized]}' are effectively identical.\n\n" +
                        "💡 Fix: Remove duplicate or use different paths.\n\n" +
                        "🔗 Spectral Rule: no-identical-paths");
                }
                else
                {
                    normalizedPaths[normalized] = path;
                }
            }
        }

        #endregion

        #region Tag Rules

        /// <summary>
        /// Rule: tag-description
        /// Severity: warn
        /// Tags must have descriptions.
        /// </summary>
        private void LintTagDescription()
        {
            if (!_config.IsRuleEnabled("tag-description")) return;

            if (_document.Tags == null) return;

            foreach (var tag in _document.Tags)
            {
                if (string.IsNullOrWhiteSpace(tag.Description))
                {
                    ReportDiagnostic("SKUGGA_LINT_013", DiagnosticSeverity.Info,
                        $"Tag: Missing description for tag '{tag.Name}'",
                        $"Tag '{tag.Name}' should have a description.\n\n" +
                        "💡 Fix: Add description:\n" +
                        "   tags:\n" +
                        $"     - name: {tag.Name}\n" +
                        "       description: Operations related to...\n\n" +
                        "🔗 Spectral Rule: tag-description");
                }
            }
        }

        /// <summary>
        /// Rule: openapi-tags-alphabetical
        /// Severity: off (can be enabled)
        /// OpenAPI object should have alphabetical tags.
        /// </summary>
        private void LintOrphanTags()
        {
            if (!_config.IsRuleEnabled("openapi-tags")) return;

            if (_document.Paths == null || _document.Tags == null) return;

            // Collect all tags used in operations
            var usedTags = new HashSet<string>();
            foreach (var path in _document.Paths)
            {
                if (path.Value?.Operations == null) continue;
                foreach (var operation in path.Value.Operations)
                {
                    if (operation.Value?.Tags == null) continue;
                    foreach (var tag in operation.Value.Tags)
                    {
                        usedTags.Add(tag.Name);
                    }
                }
            }

            // Check for orphan tags (defined but not used)
            foreach (var tag in _document.Tags)
            {
                if (!usedTags.Contains(tag.Name))
                {
                    ReportDiagnostic("SKUGGA_LINT_014", DiagnosticSeverity.Info,
                        $"Tag: Unused tag '{tag.Name}'",
                        $"Tag '{tag.Name}' is defined but not used by any operation.\n\n" +
                        "💡 Fix: Remove unused tag or add to operations.\n\n" +
                        "🔗 Spectral Rule: openapi-tags");
                }
            }
        }

        #endregion

        #region Schema Rules

        /// <summary>
        /// Rule: typed-enum
        /// Severity: warn
        /// Enum values must match the schema type.
        /// </summary>
        private void LintTypedEnum()
        {
            if (!_config.IsRuleEnabled("typed-enum")) return;

            if (_document.Components?.Schemas == null) return;

            foreach (var schema in _document.Components.Schemas)
            {
                LintTypedEnumRecursive(schema.Key, schema.Value);
            }
        }

        private void LintTypedEnumRecursive(string schemaName, OpenApiSchema schema)
        {
            if (schema == null) return;

            // Check current schema
            if (schema.Enum != null && schema.Enum.Any() && !string.IsNullOrEmpty(schema.Type))
            {
                var firstEnum = schema.Enum.First();
                var enumTypeName = firstEnum.GetType().Name;

                bool typeMismatch = false;
                string expectedType = "";

                if (schema.Type == "string" && !enumTypeName.Contains("String"))
                {
                    typeMismatch = true;
                    expectedType = "string values like \"active\"";
                }
                else if (schema.Type == "integer" && !enumTypeName.Contains("Int") && !enumTypeName.Contains("Long"))
                {
                    typeMismatch = true;
                    expectedType = "integer values like 1, 2, 3";
                }
                else if (schema.Type == "number" && !enumTypeName.Contains("Double") && !enumTypeName.Contains("Float") && !enumTypeName.Contains("Int"))
                {
                    typeMismatch = true;
                    expectedType = "numeric values";
                }

                if (typeMismatch)
                {
                    ReportDiagnostic("SKUGGA_LINT_015", DiagnosticSeverity.Warning,
                        $"Schema: Enum type mismatch in '{schemaName}'",
                        $"Schema '{schemaName}' declares type '{schema.Type}' but enum values appear to be {enumTypeName}.\n\n" +
                        $"💡 Fix: Ensure enum values match type '{schema.Type}' ({expectedType}).\n\n" +
                        "🔗 Spectral Rule: typed-enum");
                }
            }

            // Recurse into properties
            if (schema.Properties != null)
            {
                foreach (var prop in schema.Properties)
                {
                    LintTypedEnumRecursive($"{schemaName}.{prop.Key}", prop.Value);
                }
            }

            // Recurse into array items
            if (schema.Items != null)
            {
                LintTypedEnumRecursive($"{schemaName}[]", schema.Items);
            }

            // Recurse into allOf/oneOf/anyOf
            if (schema.AllOf != null)
            {
                for (int i = 0; i < schema.AllOf.Count; i++)
                {
                    LintTypedEnumRecursive($"{schemaName}.allOf[{i}]", schema.AllOf[i]);
                }
            }
        }

        /// <summary>
        /// Rule: schema-description
        /// Severity: info
        /// Schemas should have descriptions.
        /// </summary>
        private void LintSchemaDescription()
        {
            if (!_config.IsRuleEnabled("schema-description")) return;

            if (_document.Components?.Schemas == null) return;

            foreach (var schema in _document.Components.Schemas)
            {
                if (string.IsNullOrWhiteSpace(schema.Value?.Description))
                {
                    ReportDiagnostic("SKUGGA_LINT_016", DiagnosticSeverity.Info,
                        $"Schema: Missing description for '{schema.Key}'",
                        $"Schema '{schema.Key}' should have a description.\n\n" +
                        "💡 Fix: Add description:\n" +
                        "   components:\n" +
                        "     schemas:\n" +
                        $"       {schema.Key}:\n" +
                        "         description: Represents a...\n\n" +
                        "🔗 Spectral Rule: schema-description");
                }
            }
        }

        #endregion

        #region Component Rules

        /// <summary>
        /// Rule: no-unused-components
        /// Severity: warn
        /// Components should not be defined but unused.
        /// </summary>
        private void LintNoUnusedComponents()
        {
            if (!_config.IsRuleEnabled("no-unused-components")) return;

            if (_document.Components?.Schemas == null) return;

            // Collect all referenced schema names
            var referencedSchemas = new HashSet<string>();
            CollectReferences(_document, referencedSchemas);

            // Check for unused schemas
            foreach (var schema in _document.Components.Schemas.Keys)
            {
                if (!referencedSchemas.Contains(schema))
                {
                    ReportDiagnostic("SKUGGA_LINT_017", DiagnosticSeverity.Info,
                        $"Component: Unused schema '{schema}'",
                        $"Schema '{schema}' is defined but not referenced anywhere.\n\n" +
                        "💡 Fix: Remove unused schema or add reference.\n\n" +
                        "🔗 Spectral Rule: no-unused-components");
                }
            }
        }

        private void CollectReferences(OpenApiDocument document, HashSet<string> referencedSchemas)
        {
            if (document.Paths != null)
            {
                foreach (var path in document.Paths.Values)
                {
                    if (path?.Operations == null) continue;
                    foreach (var operation in path.Operations.Values)
                    {
                        CollectSchemaReferences(operation, referencedSchemas);
                    }
                }
            }
        }

        private void CollectSchemaReferences(OpenApiOperation operation, HashSet<string> referencedSchemas)
        {
            // Check request body
            if (operation.RequestBody?.Content != null)
            {
                foreach (var content in operation.RequestBody.Content.Values)
                {
                    if (content.Schema?.Reference != null)
                    {
                        referencedSchemas.Add(content.Schema.Reference.Id);
                    }
                }
            }

            // Check responses
            if (operation.Responses != null)
            {
                foreach (var response in operation.Responses.Values)
                {
                    if (response?.Content != null)
                    {
                        foreach (var content in response.Content.Values)
                        {
                            if (content.Schema?.Reference != null)
                            {
                                referencedSchemas.Add(content.Schema.Reference.Id);
                            }
                        }
                    }
                }
            }

            // Check parameters
            if (operation.Parameters != null)
            {
                foreach (var param in operation.Parameters)
                {
                    if (param.Schema?.Reference != null)
                    {
                        referencedSchemas.Add(param.Schema.Reference.Id);
                    }
                }
            }
        }

        #endregion

        private void ReportDiagnostic(string id, DiagnosticSeverity severity, string title, string message)
        {
            var descriptor = new DiagnosticDescriptor(
                id: id,
                title: title,
                messageFormat: message,
                category: "Skugga.OpenApi.Linting",
                defaultSeverity: severity,
                isEnabledByDefault: true);

            _context.ReportDiagnostic(Diagnostic.Create(descriptor, Location.None));
        }
    }

    /// <summary>
    /// Configuration for OpenAPI linting rules.
    /// </summary>
    internal class LintingConfiguration
    {
        public bool EnableLinting { get; set; } = true;
        private readonly HashSet<string> _disabledRules = new HashSet<string>();
        private readonly Dictionary<string, DiagnosticSeverity> _severityOverrides = new Dictionary<string, DiagnosticSeverity>();

        public bool IsRuleEnabled(string ruleName)
        {
            return !_disabledRules.Contains(ruleName);
        }

        public void DisableRule(string ruleName)
        {
            _disabledRules.Add(ruleName);
        }

        public void SetSeverity(string ruleName, DiagnosticSeverity severity)
        {
            _severityOverrides[ruleName] = severity;
        }

        public DiagnosticSeverity GetSeverity(string ruleName, DiagnosticSeverity defaultSeverity)
        {
            return _severityOverrides.TryGetValue(ruleName, out var severity) ? severity : defaultSeverity;
        }

        /// <summary>
        /// Parse linting configuration from attribute parameter.
        /// Format: "rule1:off,rule2:error,rule3:warn"
        /// </summary>
        public static LintingConfiguration Parse(string? configString)
        {
            var config = new LintingConfiguration();
            
            if (string.IsNullOrWhiteSpace(configString))
                return config;

            // At this point configString is guaranteed non-null
            var rules = configString!.Split(',');
            foreach (var rule in rules)
            {
                var parts = rule.Trim().Split(':');
                if (parts.Length != 2) continue;

                var ruleName = parts[0]?.Trim();
                var severityStr = parts[1]?.Trim();

                // Skip if either part is null or empty
                if (string.IsNullOrEmpty(ruleName) || string.IsNullOrEmpty(severityStr))
                    continue;

                // At this point, both are guaranteed non-null due to the check above
                var severityLower = severityStr!.ToLowerInvariant();

                if (severityLower == "off")
                {
                    config.DisableRule(ruleName!);
                }
                else if (Enum.TryParse<DiagnosticSeverity>(severityLower, true, out var severity))
                {
                    config.SetSeverity(ruleName!, severity);
                }
            }

            return config;
        }
    }
}
