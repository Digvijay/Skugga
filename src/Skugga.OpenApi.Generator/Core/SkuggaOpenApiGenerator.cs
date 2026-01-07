using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;
using SharpYaml.Serialization;

namespace Skugga.OpenApi.Generator
{
    /// <summary>
    /// Roslyn source generator that creates mock implementations from OpenAPI specifications.
    /// Detects [SkuggaFromOpenApi] attributes and generates interfaces with realistic mock data.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This generator implements the "Doppelgänger" pattern - creating perfect digital twins
    /// of external APIs that are always in sync with the contract. Key features:
    /// </para>
    /// <list type="bullet">
    /// <item>Reads OpenAPI 2.0 (Swagger) and 3.0+ specifications</item>
    /// <item>Generates interfaces from API operations</item>
    /// <item>Creates mocks with realistic defaults from 'example' fields</item>
    /// <item>Caches remote specs with ETag support for offline builds</item>
    /// <item>Validates at compile-time - contract drift = build failure</item>
    /// </list>
    /// <para>
    /// Architecture:
    /// 1. Find all interfaces marked with [SkuggaFromOpenApi]
    /// 2. Load OpenAPI spec (from URL or local file, with caching)
    /// 3. Generate interface definition from spec operations
    /// 4. Generate mock class implementing the interface
    /// 5. Generate interceptor to redirect Mock.Create calls
    /// 6. Generate default values from spec examples/schemas
    /// </para>
    /// <para>
    /// Schema Validation Best Practices (per Microsoft docs):
    /// - Microsoft.OpenApi.Readers automatically converts Swagger 2.0 → OpenAPI 3.0
    /// - Content types may vary after conversion (e.g., application/octet-stream vs application/json)
    /// - Consider using OpenApiDocument.Validate() for schema validation before processing
    /// - Handle missing schemas gracefully with fallbacks to first available content
    /// - For production: validate document structure, required properties, and proper references
    /// - See: https://learn.microsoft.com/en-us/openapi/openapi.net/overview
    /// </para>
    /// </remarks>
    [Generator]
    public class SkuggaOpenApiGenerator : IIncrementalGenerator
    {
        /// <summary>
        /// Called to initialize the incremental generator.
        /// </summary>
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            // Find all interface declarations with [SkuggaFromOpenApi] attribute
            var provider = context.SyntaxProvider.CreateSyntaxProvider(
                predicate: (node, _) => node is InterfaceDeclarationSyntax,
                transform: (ctx, _) => GetOpenApiInterface(ctx)
            ).Where(i => i != null);

            // Combine with compilation, additional files, and register source output
            var compilationAndInterfaces = context.CompilationProvider
                .Combine(provider.Collect())
                .Combine(context.AdditionalTextsProvider.Collect());

            context.RegisterSourceOutput(compilationAndInterfaces, (spc, source) =>
            {
                var ((compilation, interfaces), additionalFiles) = source;

                foreach (var interfaceDecl in interfaces)
                {
                    if (interfaceDecl != null)
                    {
                        ProcessOpenApiInterface(spc, compilation, additionalFiles, interfaceDecl);
                    }
                }
            });
        }

        /// <summary>
        /// Extracts interface declaration if it has [SkuggaFromOpenApi] attribute.
        /// </summary>
        private static InterfaceDeclarationSyntax? GetOpenApiInterface(GeneratorSyntaxContext context)
        {
            var interfaceDecl = (InterfaceDeclarationSyntax)context.Node;

            // Check if interface has [SkuggaFromOpenApi] attribute
            foreach (var attributeList in interfaceDecl.AttributeLists)
            {
                foreach (var attribute in attributeList.Attributes)
                {
                    var symbol = context.SemanticModel.GetSymbolInfo(attribute).Symbol;
                    if (symbol is IMethodSymbol attrSymbol)
                    {
                        var attrType = attrSymbol.ContainingType;
                        if (attrType.Name == "SkuggaFromOpenApiAttribute" ||
                            attrType.Name == "SkuggaFromOpenApi")
                        {
                            return interfaceDecl;
                        }
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Processes a single interface marked with [SkuggaFromOpenApi].
        /// </summary>
        private void ProcessOpenApiInterface(SourceProductionContext context, Compilation compilation, System.Collections.Immutable.ImmutableArray<AdditionalText> additionalFiles, InterfaceDeclarationSyntax interfaceDecl)
        {
            var semanticModel = compilation.GetSemanticModel(interfaceDecl.SyntaxTree);
            var interfaceSymbol = semanticModel.GetDeclaredSymbol(interfaceDecl) as INamedTypeSymbol;

            if (interfaceSymbol == null)
                return;

            // Find the [SkuggaFromOpenApi] attribute
            var attribute = FindOpenApiAttribute(interfaceSymbol);
            if (attribute == null)
                return;

            // Extract attribute parameters
            var source = GetAttributeSource(attribute);
            if (string.IsNullOrEmpty(source))
            {
                context.ReportDiagnostic(DiagnosticHelper.Create(
                    DiagnosticHelper.EmptySource,
                    interfaceDecl.GetLocation()));
                return;
            }

            // Load and parse the OpenAPI spec
            var loader = new OpenApiSpecLoader(additionalFiles);
            var specContent = loader.TryLoad(source!);  // source is guaranteed non-null after check above

            if (string.IsNullOrEmpty(specContent))
            {
                // source is guaranteed non-null here due to check above
                var sourcePath = source ?? string.Empty;
                context.ReportDiagnostic(DiagnosticHelper.Create(
                    DiagnosticHelper.SpecNotFound,
                    interfaceDecl.GetLocation(),
                    sourcePath));
                return;
            }

            OpenApiDocument document;
            try
            {
                // Detect if input is YAML
                bool isYaml = source?.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase) == true ||
                             source?.EndsWith(".yml", StringComparison.OrdinalIgnoreCase) == true;

                if (!isYaml && specContent != null)
                {
                    var trimmed = specContent.TrimStart();
                    if (!trimmed.StartsWith("{") && !trimmed.StartsWith("["))
                    {
                        isYaml = true;
                    }
                }

                string jsonContent = specContent ?? "";

                // Convert YAML to JSON if needed using Microsoft's System.Text.Json
                if (isYaml)
                {
                    try
                    {
                        var yamlSerializer = new Serializer();
                        var yamlObject = yamlSerializer.Deserialize(new StringReader(jsonContent));
                        jsonContent = JsonSerializer.Serialize(yamlObject, new JsonSerializerOptions
                        {
                            WriteIndented = false
                        });
                    }
                    catch (Exception yamlEx)
                    {
                        context.ReportDiagnostic(DiagnosticHelper.Create(
                            DiagnosticHelper.ParseError,
                            interfaceDecl.GetLocation(),
                            $"Failed to parse YAML from '{source}': {yamlEx.Message}"));
                        return;
                    }
                }

                // Parse OpenAPI document from JSON
                using (var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(jsonContent)))
                {
                    var reader = new OpenApiStreamReader();
                    document = reader.Read(memoryStream, out var diagnostic);

                    // Check if document is null (parsing completely failed)
                    if (document == null)
                    {
                        context.ReportDiagnostic(DiagnosticHelper.Create(
                            DiagnosticHelper.ParseError,
                            interfaceDecl.GetLocation(),
                            $"Failed to parse OpenAPI document from '{source}'. Document is null after reading."));
                        return;
                    }

                    if (diagnostic.Errors.Any())
                    {
                        var errors = string.Join("; ", diagnostic.Errors.Select(e => e.Message));
                        context.ReportDiagnostic(DiagnosticHelper.Create(
                            DiagnosticHelper.ParseError,
                            interfaceDecl.GetLocation(),
                            errors));
                        return;
                    }

                    // Check if this is OpenAPI 2.0 (Swagger) and report it
                    if (diagnostic.SpecificationVersion == Microsoft.OpenApi.OpenApiSpecVersion.OpenApi2_0)
                    {
                        // Microsoft.OpenApi library automatically converts 2.0 to 3.0
                        // Just inform the user that conversion happened
                        var descriptor = new DiagnosticDescriptor(
                            id: "SKUGGA_OPENAPI_006",
                            title: "OpenAPI 2.0 Detected",
                            messageFormat: "Detected OpenAPI 2.0 (Swagger) spec. Automatically converted to OpenAPI 3.0 for processing.",
                            category: "Skugga.OpenApi",
                            defaultSeverity: DiagnosticSeverity.Info,
                            isEnabledByDefault: true);
                        context.ReportDiagnostic(Diagnostic.Create(descriptor, Location.None));
                    }
                }

                // Validate the document structure and schema
                ValidateOpenApiDocument(context, interfaceDecl, document);

                // Perform comprehensive build-time validation
                var validator = new DocumentValidator(context, document);
                validator.Validate();

                // Perform Spectral-inspired linting
                var lintingRulesConfig = GetAttributeNamedParameter(attribute, "LintingRules");
                var lintingConfig = Validation.LintingConfiguration.Parse(lintingRulesConfig);
                var lintingRules = new Validation.OpenApiLintingRules(context, document, lintingConfig);
                lintingRules.Lint();

                // Check for paths - this is a critical error
                if (document.Paths == null || document.Paths.Count == 0)
                {
                    context.ReportDiagnostic(DiagnosticHelper.Create(
                        DiagnosticHelper.NoPathsDefined,
                        interfaceDecl.GetLocation()));
                    return;
                }
            }
            catch (Exception ex)
            {
                context.ReportDiagnostic(DiagnosticHelper.Create(
                    DiagnosticHelper.ParseError,
                    interfaceDecl.GetLocation(),
                    ex.Message));
                return;
            }

            // Generate the code
            GenerateCode(context, interfaceSymbol, document, attribute, source!);
        }

        /// <summary>
        /// Validates the OpenAPI document structure and reports issues as diagnostics.
        /// </summary>
        private void ValidateOpenApiDocument(SourceProductionContext context, InterfaceDeclarationSyntax interfaceDecl, OpenApiDocument document)
        {
            // Check for required document properties (critical errors reported in ProcessOpenApiInterface)

            if (document.Components?.Schemas != null)
            {
                // Validate schemas have required properties
                foreach (var schema in document.Components.Schemas)
                {
                    if (schema.Value == null)
                    {
                        var descriptor = new DiagnosticDescriptor(
                            id: "SKUGGA_OPENAPI_012",
                            title: "Null Schema Definition",
                            messageFormat: "Schema '{0}' is defined but has null value. This will be skipped.\n\n" +
                                          "💡 Fix: Remove the null schema or provide a valid definition:\n" +
                                          "   components:\n" +
                                          "     schemas:\n" +
                                          "       {0}:  # <- Define this schema\n" +
                                          "         type: object\n" +
                                          "         properties:\n" +
                                          "           id:\n" +
                                          "             type: integer",
                            category: "Skugga.OpenApi",
                            defaultSeverity: DiagnosticSeverity.Warning,
                            isEnabledByDefault: true);
                        context.ReportDiagnostic(Diagnostic.Create(descriptor, Location.None, schema.Key));
                    }
                }
            }

            // Validate operations have proper responses and warn about missing operationIds
            if (document.Paths != null)
            {
                foreach (var path in document.Paths)
                {
                    if (path.Value?.Operations == null) continue;

                    foreach (var operation in path.Value.Operations)
                    {
                        if (operation.Value == null) continue;

                        var op = operation.Value;
                        var opId = op.OperationId ?? $"{operation.Key} {path.Key}";

                        // Warn if operationId is missing (method name will be auto-generated)
                        if (string.IsNullOrEmpty(op.OperationId))
                        {
                            context.ReportDiagnostic(DiagnosticHelper.Create(
                                DiagnosticHelper.MissingOperationId,
                                null,
                                $"{operation.Key.ToString().ToUpper()} {path.Key}"));
                        }

                        // Check if operation has responses
                        if (op.Responses == null || op.Responses.Count == 0)
                        {
                            var descriptor = new DiagnosticDescriptor(
                                id: "SKUGGA_OPENAPI_013",
                                title: "No Responses Defined",
                                messageFormat: "Operation '{0}' has no responses defined.\n\n" +
                                              "💡 Fix: Add at least one response:\n" +
                                              "   responses:\n" +
                                              "     '200':\n" +
                                              "       description: Success",
                                category: "Skugga.OpenApi",
                                defaultSeverity: DiagnosticSeverity.Warning,
                                isEnabledByDefault: true);
                            context.ReportDiagnostic(Diagnostic.Create(descriptor, Location.None, opId));
                        }
                        else
                        {
                            // Check for at least one success response
                            var hasSuccessResponse = op.Responses.Any(r =>
                                r.Key == "200" || r.Key == "201" || r.Key == "202" || r.Key == "204" || r.Key == "default");

                            if (!hasSuccessResponse)
                            {
                                context.ReportDiagnostic(DiagnosticHelper.Create(
                                    DiagnosticHelper.NoSuccessResponse,
                                    null,
                                    opId));
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Finds the SkuggaFromOpenApi attribute on an interface symbol.
        /// </summary>
        private AttributeData? FindOpenApiAttribute(INamedTypeSymbol interfaceSymbol)
        {
            return interfaceSymbol.GetAttributes()
                .FirstOrDefault(a => a.AttributeClass?.Name == "SkuggaFromOpenApiAttribute");
        }

        /// <summary>
        /// Extracts the source parameter from the attribute.
        /// </summary>
        private string? GetAttributeSource(AttributeData attribute)
        {
            if (attribute.ConstructorArguments.Length > 0)
            {
                var arg = attribute.ConstructorArguments[0];
                return arg.Value?.ToString();
            }
            return null;
        }

        /// <summary>
        /// Generates the interface and schemas from the OpenAPI document.
        /// </summary>
        private void GenerateCode(SourceProductionContext context, INamedTypeSymbol interfaceSymbol,
            OpenApiDocument document, AttributeData attribute, string source)
        {
            var namespaceName = interfaceSymbol.ContainingNamespace?.ToDisplayString() ?? "Generated";
            var interfaceName = interfaceSymbol.Name;

            // Check if interface is nested and build containing type info
            var containingTypes = new System.Collections.Generic.List<(string Name, string Modifiers)>();
            var containingType = interfaceSymbol.ContainingType;
            while (containingType != null)
            {
                var modifiers = "public";
                if (containingType.IsStatic) modifiers += " static";
                containingTypes.Insert(0, (containingType.Name, modifiers + " partial class"));
                containingType = containingType.ContainingType;
            }

            // Get attribute parameters
            var operationFilter = GetAttributeNamedParameter(attribute, "OperationFilter");
            var generateAsync = GetAttributeNamedParameter(attribute, "GenerateAsync");
            var shouldGenerateAsync = string.IsNullOrEmpty(generateAsync) ? true : bool.Parse(generateAsync);

            // Get schema prefix - defaults to null for backward compatibility
            var schemaPrefix = GetAttributeNamedParameter(attribute, "SchemaPrefix");

            // Get example set name for selecting specific named examples
            var useExampleSet = GetAttributeNamedParameter(attribute, "UseExampleSet");

            // Enhancement : Stateful behavior
            var statefulBehavior = GetAttributeNamedParameter(attribute, "StatefulBehavior");
            var enableStateful = !string.IsNullOrEmpty(statefulBehavior) && bool.Parse(statefulBehavior);

            // Enhancement : Contract verification (placeholder for future implementation)
            var validateContracts = GetAttributeNamedParameter(attribute, "ValidateContracts");
            var enableContractValidation = !string.IsNullOrEmpty(validateContracts) && bool.Parse(validateContracts);

            // Feature: Authentication handling
            var automaticallyHandleAuth = GetAttributeNamedParameter(attribute, "AutomaticallyHandleAuth");
            var enableAuthHandling = !string.IsNullOrEmpty(automaticallyHandleAuth) && bool.Parse(automaticallyHandleAuth);

            // Extract security schemes from the document for auth generation
            var securitySchemes = enableAuthHandling && document.Components?.SecuritySchemes != null
                ? document.Components.SecuritySchemes
                : new Dictionary<string, OpenApiSecurityScheme>();

            // Create generators with shared TypeMapper
            var typeMapper = new TypeMapper(document);
            if (!string.IsNullOrEmpty(schemaPrefix))
            {
                typeMapper.SetInterfaceNamePrefix(schemaPrefix);
            }
            var interfaceGenerator = new InterfaceGenerator(document, typeMapper, shouldGenerateAsync);
            var schemaGenerator = new SchemaGenerator(typeMapper);
            var exampleGenerator = new ExampleGenerator(typeMapper, useExampleSet);
            var mockGenerator = new MockGenerator(document, typeMapper, exampleGenerator, shouldGenerateAsync, enableStateful, enableContractValidation, enableAuthHandling, securitySchemes);

            // Generate the interface (with containing types for nested interfaces)
            var interfaceCode = interfaceGenerator.GenerateInterface(interfaceName, namespaceName, operationFilter, containingTypes);

            context.AddSource($"{interfaceName}.g.cs", SourceText.From(interfaceCode, Encoding.UTF8));

            // Generate schema classes with optional prefix from attribute (null = no prefix for backward compatibility)
            var schemasCode = schemaGenerator.GenerateSchemas(namespaceName, schemaPrefix);
            context.AddSource($"{interfaceName}_Schemas.g.cs", SourceText.From(schemasCode, Encoding.UTF8));

            // Generate mock implementation (with containing types for nested interfaces)
            try
            {
                var mockCode = mockGenerator.GenerateMock(interfaceName, namespaceName, operationFilter, containingTypes);
                context.AddSource($"{interfaceName}_Mock.g.cs", SourceText.From(mockCode, Encoding.UTF8));
            }
            catch (Exception ex)
            {
                // Report mock generation errors with troubleshooting guidance
                context.ReportDiagnostic(DiagnosticHelper.Create(
                    DiagnosticHelper.MockGenerationError,
                    null,
                    ex.Message));
            }
        }

        /// <summary>
        /// Gets a named parameter value from an attribute.
        /// </summary>
        private string? GetAttributeNamedParameter(AttributeData attribute, string parameterName)
        {
            var namedArg = attribute.NamedArguments.FirstOrDefault(a => a.Key == parameterName);
            return namedArg.Value.Value?.ToString();
        }

        /// <summary>
        /// Reports a diagnostic error.
        /// </summary>
        private void ReportError(SourceProductionContext context, SyntaxNode node, string id, string message)
        {
            var descriptor = new DiagnosticDescriptor(
                id: id,
                title: "OpenAPI Generator Error",
                messageFormat: message,
                category: "Skugga.OpenApi",
                defaultSeverity: DiagnosticSeverity.Error,
                isEnabledByDefault: true);

            context.ReportDiagnostic(Diagnostic.Create(descriptor, node.GetLocation()));
        }
    }
}

