using System;
using Microsoft.CodeAnalysis;

namespace Skugga.OpenApi.Generator
{
    /// <summary>
    /// Provides helpful diagnostic messages with guidance for common OpenAPI generation issues.
    /// Each diagnostic includes clear problem description, actionable fix suggestions, and documentation links.
    /// </summary>
    public static class DiagnosticHelper
    {
        private const string DocsBaseUrl = "https://github.com/Digvijay/Skugga/blob/main/docs/DOPPELGANGER.md";

        // Diagnostic IDs and descriptors for common issues

        public static DiagnosticDescriptor EmptySource => new DiagnosticDescriptor(
            id: "SKUGGA_OPENAPI_002",
            title: "Missing OpenAPI Source Path",
            messageFormat: "The 'source' parameter in [SkuggaFromOpenApi] cannot be empty.\n\n" +
                          "💡 Fix: Provide a path to your OpenAPI spec:\n" +
                          "   [SkuggaFromOpenApi(\"specs/api.json\")]  // Local file\n" +
                          "   [SkuggaFromOpenApi(\"https://api.example.com/swagger.json\")]  // Remote URL\n\n" +
                          "📖 Docs: {0}#specifying-the-spec-source",
            category: "Skugga.OpenApi",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            helpLinkUri: $"{DocsBaseUrl}#specifying-the-spec-source");

        public static DiagnosticDescriptor SpecNotFound => new DiagnosticDescriptor(
            id: "SKUGGA_OPENAPI_003",
            title: "OpenAPI Specification Not Found",
            messageFormat: "Could not load OpenAPI spec from '{0}'.\n\n" +
                          "💡 Possible fixes:\n" +
                          "   1. For local files: Add the spec to your project's AdditionalFiles:\n" +
                          "      <ItemGroup>\n" +
                          "        <AdditionalFiles Include=\"specs\\api.json\" />\n" +
                          "      </ItemGroup>\n\n" +
                          "   2. For remote URLs: Ensure the URL is accessible and returns valid JSON/YAML\n" +
                          "   3. Check file path spelling and location relative to project root\n" +
                          "   4. Verify network connectivity for remote specs\n\n" +
                          "📖 Docs: {1}#troubleshooting",
            category: "Skugga.OpenApi",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            helpLinkUri: $"{DocsBaseUrl}#troubleshooting");

        public static DiagnosticDescriptor ParseError => new DiagnosticDescriptor(
            id: "SKUGGA_OPENAPI_004",
            title: "OpenAPI Parsing Failed",
            messageFormat: "Failed to parse OpenAPI specification: {0}\n\n" +
                          "💡 Common issues:\n" +
                          "   • Invalid JSON/YAML syntax - validate with a linter\n" +
                          "   • Missing required OpenAPI fields (openapi, info, paths)\n" +
                          "   • Incorrect schema references ($ref)\n" +
                          "   • Circular dependencies in schemas\n\n" +
                          "🔍 Validation tools:\n" +
                          "   • Online: https://editor.swagger.io/\n" +
                          "   • CLI: npx @stoplight/spectral-cli lint spec.json\n\n" +
                          "📖 Docs: {1}#spec-validation",
            category: "Skugga.OpenApi",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            helpLinkUri: $"{DocsBaseUrl}#spec-validation");

        public static DiagnosticDescriptor MockGenerationError => new DiagnosticDescriptor(
            id: "SKUGGA_OPENAPI_005",
            title: "Mock Generation Failed",
            messageFormat: "Failed to generate mock implementation: {0}\n\n" +
                          "💡 Troubleshooting:\n" +
                          "   • Check that all schema references resolve correctly\n" +
                          "   • Ensure response schemas are properly defined\n" +
                          "   • Verify example values match schema types\n" +
                          "   • Look for unsupported schema features (oneOf, anyOf may have limited support)\n\n" +
                          "🐛 If this is unexpected, please report at:\n" +
                          "   https://github.com/Digvijay/Skugga/issues\n\n" +
                          "📖 Docs: {1}#mock-generation",
            category: "Skugga.OpenApi",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            helpLinkUri: $"{DocsBaseUrl}#mock-generation");

        public static DiagnosticDescriptor NoPathsDefined => new DiagnosticDescriptor(
            id: "SKUGGA_OPENAPI_008",
            title: "Empty OpenAPI Specification",
            messageFormat: "OpenAPI document has no paths defined.\n\n" +
                          "💡 Fix: Add at least one operation to your spec:\n" +
                          "   paths:\n" +
                          "     /users:\n" +
                          "       get:\n" +
                          "         operationId: getUsers\n" +
                          "         responses:\n" +
                          "           '200':\n" +
                          "             description: List of users\n\n" +
                          "📖 Docs: {0}#minimal-spec",
            category: "Skugga.OpenApi",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            helpLinkUri: $"{DocsBaseUrl}#minimal-spec");

        public static DiagnosticDescriptor MissingOperationId => new DiagnosticDescriptor(
            id: "SKUGGA_OPENAPI_009",
            title: "Missing operationId",
            messageFormat: "Operation '{0}' is missing an operationId.\n\n" +
                          "💡 Fix: Add an operationId to the operation:\n" +
                          "   paths:\n" +
                          "     /users:\n" +
                          "       get:\n" +
                          "         operationId: getUsers  # <- Add this\n" +
                          "         ...\n\n" +
                          "ℹ️  operationId is used to generate method names in the interface.\n" +
                          "   Without it, Skugga will generate a name from the HTTP method and path.\n\n" +
                          "📖 Docs: {1}#operation-ids",
            category: "Skugga.OpenApi",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            helpLinkUri: $"{DocsBaseUrl}#operation-ids");

        public static DiagnosticDescriptor NoSuccessResponse => new DiagnosticDescriptor(
            id: "SKUGGA_OPENAPI_007",
            title: "No Success Response Defined",
            messageFormat: "Operation '{0}' has no success response (2xx or default).\n\n" +
                          "💡 Fix: Add a success response:\n" +
                          "   responses:\n" +
                          "     '200':  # or 201, 202, 204, default\n" +
                          "       description: Success\n" +
                          "       content:\n" +
                          "         application/json:\n" +
                          "           schema:\n" +
                          "             $ref: '#/components/schemas/User'\n\n" +
                          "⚠️  Generated method will return void/Task without a success response.\n\n" +
                          "📖 Docs: {1}#response-definitions",
            category: "Skugga.OpenApi",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            helpLinkUri: $"{DocsBaseUrl}#response-definitions");

        public static DiagnosticDescriptor UnsupportedSchemaType => new DiagnosticDescriptor(
            id: "SKUGGA_OPENAPI_010",
            title: "Unsupported Schema Type",
            messageFormat: "Schema '{0}' uses unsupported type '{1}'.\n\n" +
                          "💡 Supported types:\n" +
                          "   • Primitives: string, integer, number, boolean\n" +
                          "   • Complex: object, array\n" +
                          "   • References: $ref to components/schemas\n" +
                          "   • Composition: allOf (oneOf/anyOf have limited support)\n\n" +
                          "💡 Possible fix:\n" +
                          "   Convert to a supported type or use $ref to reference an existing schema.\n\n" +
                          "📖 Docs: {2}#supported-schema-types",
            category: "Skugga.OpenApi",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            helpLinkUri: $"{DocsBaseUrl}#supported-schema-types");

        public static DiagnosticDescriptor InvalidExampleValue => new DiagnosticDescriptor(
            id: "SKUGGA_OPENAPI_011",
            title: "Invalid Example Value",
            messageFormat: "Example value for '{0}' doesn't match schema type.\n\n" +
                          "💡 Fix: Ensure example values match the schema type:\n" +
                          "   type: integer\n" +
                          "   example: 123  # ✓ Correct\n" +
                          "   example: \"123\"  # ✗ Wrong (string, not integer)\n\n" +
                          "   type: string\n" +
                          "   format: date-time\n" +
                          "   example: \"2024-01-15T10:30:00Z\"  # ✓ Correct\n\n" +
                          "⚠️  Invalid examples will be ignored and defaults used instead.\n\n" +
                          "📖 Docs: {1}#examples-and-defaults",
            category: "Skugga.OpenApi",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            helpLinkUri: $"{DocsBaseUrl}#examples-and-defaults");

        public static DiagnosticDescriptor InvalidEnumValue => new DiagnosticDescriptor(
            id: "SKUGGA_OPENAPI_028",
            title: "Invalid Enum Value",
            messageFormat: "Example value '{0}' for '{1}' is not in the allowed enum values.\n\n" +
                          "💡 Fix: Use one of the allowed enum values:\n" +
                          "   Allowed: {2}\n" +
                          "   Example: {3}\n\n" +
                          "📖 Docs: {4}#enum-validation",
            category: "Skugga.OpenApi",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            helpLinkUri: $"{DocsBaseUrl}#enum-validation");

        public static DiagnosticDescriptor EnumParameterWithoutConstraint => new DiagnosticDescriptor(
            id: "SKUGGA_OPENAPI_029",
            title: "Enum Parameter Without Constraint",
            messageFormat: "Parameter '{0}' in '{1}' uses type '{2}' which has enum values, but parameter doesn't constrain values.\n\n" +
                          "💡 Consider: Add enum constraint to parameter or ensure callers provide valid values.\n" +
                          "   Allowed values: {3}\n\n" +
                          "📖 Docs: {4}#enum-validation",
            category: "Skugga.OpenApi",
            defaultSeverity: DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            helpLinkUri: $"{DocsBaseUrl}#enum-validation");

        public static DiagnosticDescriptor UnexpectedError => new DiagnosticDescriptor(
            id: "SKUGGA_OPENAPI_001",
            title: "Unexpected Generator Error",
            messageFormat: "An unexpected error occurred in the OpenAPI generator:\n{0}\n\n" +
                          "🐛 This might be a bug. Please report it at:\n" +
                          "   https://github.com/Digvijay/Skugga/issues\n\n" +
                          "Include:\n" +
                          "   • This error message\n" +
                          "   • Your OpenAPI spec (or a minimal reproduction)\n" +
                          "   • Skugga version\n\n" +
                          "📖 Docs: {1}#troubleshooting",
            category: "Skugga.OpenApi",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            helpLinkUri: $"{DocsBaseUrl}#troubleshooting");

        /// <summary>
        /// Creates a diagnostic with formatted message including documentation URL.
        /// </summary>
        public static Diagnostic Create(DiagnosticDescriptor descriptor, Location? location, params object[] args)
        {
            // Append docs URL to arguments for formatting
            var argsWithUrl = new object[args.Length + 1];
            if (args.Length > 0)
            {
                Array.Copy(args, argsWithUrl, args.Length);
            }
            argsWithUrl[args.Length] = DocsBaseUrl;

            return Diagnostic.Create(descriptor, location ?? Location.None, argsWithUrl);
        }
    }
}
