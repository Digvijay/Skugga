using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Skugga.OpenApi.Generator
{
    /// <summary>
    /// Handles loading OpenAPI specifications from AdditionalFiles in a source generator context.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Source generators cannot do direct file I/O. Instead, we use MSBuild's AdditionalFiles mechanism:
    /// 1. OpenAPI specs are added to the project with AdditionalFiles item group
    /// 2. MSBuild passes these files to the generator via context.AdditionalFiles
    /// 3. Generator reads them using GetText() which is safe in generator context
    /// </para>
    /// <para>
    /// For URLs, a pre-build MSBuild task downloads and caches specs, then adds them to AdditionalFiles.
    /// This keeps the generator pure (no I/O) and enables offline builds.
    /// </para>
    /// <para>
    /// Project setup:
    /// <code>
    /// &lt;ItemGroup&gt;
    ///   &lt;AdditionalFiles Include="specs/api.json" /&gt;
    ///   &lt;SkuggaOpenApiUrl Include="https://api.example.com/spec.json" /&gt;
    /// &lt;/ItemGroup&gt;
    /// </code>
    /// </para>
    /// </remarks>
    internal class OpenApiSpecLoader
    {
        private readonly IEnumerable<AdditionalText> _additionalFiles;

        /// <summary>
        /// Initializes a new instance of OpenApiSpecLoader.
        /// </summary>
        /// <param name="additionalFiles">Additional files provided by MSBuild (context.AdditionalFiles)</param>
        public OpenApiSpecLoader(IEnumerable<AdditionalText> additionalFiles)
        {
            _additionalFiles = additionalFiles ?? throw new ArgumentNullException(nameof(additionalFiles));
        }

        /// <summary>
        /// Loads an OpenAPI specification from AdditionalFiles by matching the source path.
        /// </summary>
        /// <param name="source">File path or identifier of the OpenAPI spec</param>
        /// <returns>The OpenAPI specification content as a string, or null if not found</returns>
        /// <remarks>
        /// Matches against the file path of AdditionalFiles. For URLs, the MSBuild task
        /// should have downloaded the spec and added it with a recognizable path.
        /// </remarks>
        public string? TryLoad(string source)
        {
            if (string.IsNullOrWhiteSpace(source))
                return null;

            // Normalize the source for comparison
            var normalizedSource = NormalizePath(source);

            // For URLs, look for any cached file (since URLs are cached with hash filenames)
            bool isUrl = source.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                         source.StartsWith("https://", StringComparison.OrdinalIgnoreCase);

            // Try to find a matching additional file
            foreach (var file in _additionalFiles)
            {
                var filePath = file.Path;
                var normalizedFilePath = NormalizePath(filePath);

                // For URLs: accept any file in the cache directory (JSON or YAML)
                if (isUrl && filePath.Contains("/skugga-openapi-cache/") && 
                    (filePath.EndsWith(".json") || filePath.EndsWith(".yaml") || filePath.EndsWith(".yml")))
                {
                    var text = file.GetText();
                    return text?.ToString();
                }

                // For local files: check if paths match or if the file ends with the source
                if (normalizedFilePath.Equals(normalizedSource, StringComparison.OrdinalIgnoreCase) ||
                    normalizedFilePath.EndsWith(normalizedSource, StringComparison.OrdinalIgnoreCase))
                {
                    var text = file.GetText();
                    return text?.ToString();
                }
            }

            return null;
        }

        /// <summary>
        /// Normalizes a path for comparison (forward slashes, trimmed).
        /// </summary>
        private string NormalizePath(string path)
        {
            return path.Replace('\\', '/').Trim().TrimStart('/');
        }
    }
}
