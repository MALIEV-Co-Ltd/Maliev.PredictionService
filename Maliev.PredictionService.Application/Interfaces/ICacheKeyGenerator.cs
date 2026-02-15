using Maliev.PredictionService.Domain.Enums;

namespace Maliev.PredictionService.Application.Interfaces;

/// <summary>
/// Interface for generating consistent cache keys for prediction results.
/// </summary>
public interface ICacheKeyGenerator
{
    /// <summary>
    /// Generates a cache key for prediction inputs.
    /// </summary>
    string GenerateCacheKey(
        ModelType modelType,
        Dictionary<string, object> inputParameters,
        string modelVersion);

    /// <summary>
    /// Generates a wildcard pattern for cache invalidation.
    /// </summary>
    string GenerateInvalidationPattern(ModelType modelType, string? modelVersion = null);
}
