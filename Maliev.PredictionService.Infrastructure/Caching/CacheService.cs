using Maliev.PredictionService.Domain.Enums;

namespace Maliev.PredictionService.Infrastructure.Caching;

/// <summary>
/// Application service wrapping RedisCacheService with domain-specific cache logic
/// </summary>
public class CacheService
{
    private readonly RedisCacheService _redisCacheService;
    private readonly CacheKeyGenerator _cacheKeyGenerator;

    public CacheService(RedisCacheService redisCacheService, CacheKeyGenerator cacheKeyGenerator)
    {
        _redisCacheService = redisCacheService ?? throw new ArgumentNullException(nameof(redisCacheService));
        _cacheKeyGenerator = cacheKeyGenerator ?? throw new ArgumentNullException(nameof(cacheKeyGenerator));
    }

    /// <summary>
    /// Gets a cached prediction result
    /// </summary>
    public async Task<T?> GetPredictionAsync<T>(
        ModelType modelType,
        Dictionary<string, object> inputParameters,
        string modelVersion,
        CancellationToken cancellationToken = default)
        where T : class
    {
        var cacheKey = _cacheKeyGenerator.GenerateCacheKey(modelType, inputParameters, modelVersion);
        return await _redisCacheService.GetAsync<T>(cacheKey, cancellationToken);
    }

    /// <summary>
    /// Caches a prediction result with TTL based on model type
    /// </summary>
    public async Task SetPredictionAsync<T>(
        ModelType modelType,
        Dictionary<string, object> inputParameters,
        string modelVersion,
        T predictionResult,
        CancellationToken cancellationToken = default)
        where T : class
    {
        var cacheKey = _cacheKeyGenerator.GenerateCacheKey(modelType, inputParameters, modelVersion);
        await _redisCacheService.SetAsync(cacheKey, predictionResult, modelType, cancellationToken);
    }

    /// <summary>
    /// Invalidates all cache entries for a specific model type and version
    /// Called when a new model version is deployed
    /// </summary>
    public async Task InvalidateModelCacheAsync(
        ModelType modelType,
        string modelVersion,
        CancellationToken cancellationToken = default)
    {
        var pattern = _cacheKeyGenerator.GenerateInvalidationPattern(modelType, modelVersion);
        await _redisCacheService.InvalidatePatternAsync(pattern, cancellationToken);
    }

    /// <summary>
    /// Invalidates all cache entries for a model type (all versions)
    /// </summary>
    public async Task InvalidateAllModelCacheAsync(
        ModelType modelType,
        CancellationToken cancellationToken = default)
    {
        var pattern = _cacheKeyGenerator.GenerateInvalidationPattern(modelType);
        await _redisCacheService.InvalidatePatternAsync(pattern, cancellationToken);
    }

    /// <summary>
    /// Gets cached prediction by pre-generated cache key
    /// </summary>
    public async Task<T?> GetByKeyAsync<T>(string cacheKey, CancellationToken cancellationToken = default)
        where T : class
    {
        return await _redisCacheService.GetAsync<T>(cacheKey, cancellationToken);
    }

    /// <summary>
    /// Sets cached prediction with pre-generated cache key
    /// </summary>
    public async Task SetByKeyAsync<T>(
        string cacheKey,
        T predictionResult,
        ModelType modelType,
        CancellationToken cancellationToken = default)
        where T : class
    {
        await _redisCacheService.SetAsync(cacheKey, predictionResult, modelType, cancellationToken);
    }

    /// <summary>
    /// Gets cache statistics
    /// </summary>
    public async Task<object> GetStatisticsAsync(CancellationToken cancellationToken = default)
    {
        return await _redisCacheService.GetStatisticsAsync(cancellationToken);
    }
}
