using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Skugga.OpenApi.Tasks
{
    /// <summary>
    /// MSBuild task that downloads OpenAPI specifications from URLs and caches them locally.
    /// Runs before source generation to make specs available as AdditionalFiles.
    /// </summary>
    public class DownloadOpenApiSpecsTask : Microsoft.Build.Utilities.Task
    {
        /// <summary>
        /// URLs to download. Each should be a full HTTP/HTTPS URL to an OpenAPI spec.
        /// </summary>
        [Required]
        public ITaskItem[]? Urls { get; set; }

        /// <summary>
        /// Output directory for cached specs. Typically $(MSBuildProjectDirectory)/obj/skugga-openapi-cache/
        /// </summary>
        [Required]
        public string? CacheDirectory { get; set; }

        /// <summary>
        /// Files that were downloaded or found in cache. These should be added to AdditionalFiles.
        /// </summary>
        [Output]
        public ITaskItem[]? CachedFiles { get; set; }

        public override bool Execute()
        {
            try
            {
                // Run async download task
                return ExecuteAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Log.LogError($"Unexpected error downloading OpenAPI specs: {ex.Message}");
                Log.LogMessage(MessageImportance.Low, ex.ToString());
                return false;
            }
        }

        private async Task<bool> ExecuteAsync()
        {
            if (Urls == null || Urls.Length == 0)
            {
                Log.LogMessage(MessageImportance.Low, "No OpenAPI URLs to download");
                CachedFiles = Array.Empty<ITaskItem>();
                return true;
            }

            // Ensure cache directory exists
            Directory.CreateDirectory(CacheDirectory!);

            var cachedFiles = new List<ITaskItem>();

            using (var httpClient = new HttpClient())
            {
                httpClient.Timeout = TimeSpan.FromSeconds(30);
                httpClient.DefaultRequestHeaders.Add("User-Agent", "Skugga-OpenApi-Tasks/1.0");

                foreach (var urlItem in Urls)
                {
                    var url = urlItem.ItemSpec;

                    if (string.IsNullOrWhiteSpace(url))
                    {
                        Log.LogWarning($"Empty URL in SkuggaOpenApiUrl item, skipping");
                        continue;
                    }

                    if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
                        (uri.Scheme != "http" && uri.Scheme != "https"))
                    {
                        Log.LogError($"Invalid URL: {url}. Must be HTTP or HTTPS.");
                        return false;
                    }

                    Log.LogMessage(MessageImportance.Normal, $"Processing OpenAPI URL: {url}");

                    // Generate cache file name from URL hash
                    var urlHash = ComputeHash(url);
                    var cacheFilePath = Path.Combine(CacheDirectory, $"{urlHash}.json");
                    var metaFilePath = Path.Combine(CacheDirectory, $"{urlHash}.meta");

                    try
                    {
                        // Check if we have cached version with ETag
                        string? cachedETag = null;
                        if (File.Exists(metaFilePath))
                        {
                            var metaLines = File.ReadAllLines(metaFilePath);
                            var etagLine = metaLines.FirstOrDefault(l => l.StartsWith("ETag:"));
                            if (etagLine != null)
                            {
                                cachedETag = etagLine.Substring("ETag:".Length).Trim();
                            }
                        }

                        // Try to download with ETag revalidation
                        var request = new HttpRequestMessage(HttpMethod.Get, url);
                        if (!string.IsNullOrEmpty(cachedETag) && File.Exists(cacheFilePath))
                        {
                            request.Headers.TryAddWithoutValidation("If-None-Match", cachedETag);
                        }

                        var response = await httpClient.SendAsync(request);

                        if (response.StatusCode == HttpStatusCode.NotModified)
                        {
                            // Cache is still valid
                            Log.LogMessage(MessageImportance.Normal, $"  ✓ Using cached spec (up-to-date): {cacheFilePath}");
                        }
                        else if (response.IsSuccessStatusCode)
                        {
                            // Download new/updated content
                            var content = await response.Content.ReadAsStringAsync();
                            File.WriteAllText(cacheFilePath, content, Encoding.UTF8);

                            // Save metadata
                            var etag = response.Headers.ETag?.Tag ?? "";
                            var meta = new[]
                            {
                                $"URL:{url}",
                                $"ETag:{etag}",
                                $"Downloaded:{DateTime.UtcNow:O}",
                                $"StatusCode:{(int)response.StatusCode}"
                            };
                            File.WriteAllLines(metaFilePath, meta);

                            Log.LogMessage(MessageImportance.Normal, $"  ✓ Downloaded and cached: {cacheFilePath}");
                        }
                        else
                        {
                            // Download failed
                            if (File.Exists(cacheFilePath))
                            {
                                // Use stale cache
                                Log.LogWarning($"Failed to download {url} (HTTP {response.StatusCode}), using stale cache");
                            }
                            else
                            {
                                // No cache available
                                Log.LogError($"Failed to download {url}: HTTP {response.StatusCode}");
                                return false;
                            }
                        }

                        // Add to output with SourceUrl metadata
                        var item = new TaskItem(cacheFilePath);
                        item.SetMetadata("SourceUrl", url);
                        item.SetMetadata("SkuggaOpenApiUrl", url); // Also set with this name for clarity
                        cachedFiles.Add(item);
                    }
                    catch (HttpRequestException ex)
                    {
                        // Network error
                        if (File.Exists(cacheFilePath))
                        {
                            Log.LogWarning($"Network error downloading {url}: {ex.Message}. Using stale cache.");
                            var item = new TaskItem(cacheFilePath);
                            item.SetMetadata("SourceUrl", url);
                            item.SetMetadata("Stale", "true");
                            cachedFiles.Add(item);
                        }
                        else
                        {
                            Log.LogError($"Network error downloading {url} and no cache available: {ex.Message}");
                            return false;
                        }
                    }
                    catch (TaskCanceledException)
                    {
                        // Timeout
                        if (File.Exists(cacheFilePath))
                        {
                            Log.LogWarning($"Timeout downloading {url}. Using stale cache.");
                            var item = new TaskItem(cacheFilePath);
                            item.SetMetadata("SourceUrl", url);
                            item.SetMetadata("SkuggaOpenApiUrl", url);
                            item.SetMetadata("Stale", "true");
                            cachedFiles.Add(item);
                        }
                        else
                        {
                            Log.LogError($"Timeout downloading {url} and no cache available");
                            return false;
                        }
                    }
                }
            }

            CachedFiles = cachedFiles.ToArray();
            return true;
        }

        private static string ComputeHash(string input)
        {
            using (var sha256 = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(input);
                var hash = sha256.ComputeHash(bytes);
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant().Substring(0, 16);
            }
        }
    }
}
