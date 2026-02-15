using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Maliev.PredictionService.Application.Interfaces;
using Maliev.PredictionService.Domain.Enums;

namespace Maliev.PredictionService.Infrastructure.Caching;

/// <summary>
/// Generates consistent cache keys using SHA-256 hashing of sorted input parameters
/// Cache key format: {modelType}:{sha256Hash}:{modelVersion}
/// </summary>
public class CacheKeyGenerator : ICacheKeyGenerator
{
    /// <summary>
    /// Generates a cache key for prediction inputs
    /// </summary>
    public string GenerateCacheKey(
        ModelType modelType,
        Dictionary<string, object> inputParameters,
        string modelVersion)
    {
        var hash = GenerateSHA256Hash(inputParameters);
        return $"{modelType.ToString().ToLowerInvariant()}:{hash}:{modelVersion}";
    }

    /// <summary>
    /// Generates a wildcard pattern for cache invalidation
    /// Example: "print_time:*:2.1.0" to invalidate all print-time predictions for version 2.1.0
    /// </summary>
    public string GenerateInvalidationPattern(ModelType modelType, string? modelVersion = null)
    {
        if (string.IsNullOrEmpty(modelVersion))
        {
            return $"{modelType.ToString().ToLowerInvariant()}:*";
        }

        return $"{modelType.ToString().ToLowerInvariant()}:*:{modelVersion}";
    }

    /// <summary>
    /// Generates SHA-256 hash of sorted input parameters
    /// Ensures consistent hashing regardless of parameter order
    /// </summary>
    private string GenerateSHA256Hash(Dictionary<string, object> inputParameters)
    {
        // Sort parameters alphabetically by key for consistency
        var sortedParams = inputParameters
            .OrderBy(kvp => kvp.Key)
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        // Serialize to JSON with minimal formatting
        var json = JsonSerializer.Serialize(sortedParams, new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        // Generate SHA-256 hash
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(json));

        // Convert to hex string
        return BitConverter.ToString(hashBytes)
            .Replace("-", string.Empty)
            .ToLowerInvariant();
    }
}
