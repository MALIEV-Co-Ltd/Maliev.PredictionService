using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.ML;

namespace Maliev.PredictionService.Infrastructure.Caching;

/// <summary>
/// Service for caching ML.NET models in memory to avoid repeated disk I/O.
/// Uses IMemoryCache with size-based and time-based eviction policies.
/// </summary>
public class ModelCacheService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<ModelCacheService> _logger;
    private readonly MLContext _mlContext;

    // Cache configuration
    private static readonly TimeSpan DefaultAbsoluteExpiration = TimeSpan.FromHours(24);
    private static readonly TimeSpan DefaultSlidingExpiration = TimeSpan.FromHours(1);
    private const long ModelCacheEntrySize = 10; // Arbitrary size units per model

    public ModelCacheService(IMemoryCache cache, ILogger<ModelCacheService> logger)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _mlContext = new MLContext(seed: 42);
    }

    /// <summary>
    /// Gets a model from cache, or loads it from disk and caches it.
    /// </summary>
    /// <param name="modelPath">Path to the model file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The cached or newly loaded model.</returns>
    public async Task<ITransformer> GetOrLoadModelAsync(string modelPath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(modelPath))
        {
            throw new ArgumentException("Model path cannot be null or empty", nameof(modelPath));
        }

        if (!File.Exists(modelPath))
        {
            throw new FileNotFoundException($"Model file not found: {modelPath}");
        }

        var cacheKey = GetCacheKey(modelPath);

        // Try to get from cache
        if (_cache.TryGetValue(cacheKey, out ITransformer? cachedModel) && cachedModel != null)
        {
            _logger.LogDebug("Model cache hit for {ModelPath}", modelPath);
            return cachedModel;
        }

        _logger.LogInformation("Model cache miss for {ModelPath}, loading from disk", modelPath);

        // Load model from disk
        var model = await LoadModelFromDiskAsync(modelPath, cancellationToken);

        // Cache the model with eviction policies
        var cacheEntryOptions = new MemoryCacheEntryOptions()
            .SetSize(ModelCacheEntrySize)
            .SetAbsoluteExpiration(DefaultAbsoluteExpiration)
            .SetSlidingExpiration(DefaultSlidingExpiration)
            .RegisterPostEvictionCallback((key, value, reason, state) =>
            {
                _logger.LogInformation(
                    "Model evicted from cache. Key: {Key}, Reason: {Reason}",
                    key,
                    reason);
            });

        _cache.Set(cacheKey, model, cacheEntryOptions);

        _logger.LogInformation("Model cached successfully. Path: {ModelPath}", modelPath);

        return model;
    }

    /// <summary>
    /// Invalidates (removes) a model from the cache.
    /// </summary>
    /// <param name="modelPath">Path to the model file.</param>
    public void InvalidateModel(string modelPath)
    {
        if (string.IsNullOrWhiteSpace(modelPath))
        {
            return;
        }

        var cacheKey = GetCacheKey(modelPath);
        _cache.Remove(cacheKey);

        _logger.LogInformation("Model invalidated from cache. Path: {ModelPath}", modelPath);
    }

    /// <summary>
    /// Clears all cached models.
    /// </summary>
    public void ClearAll()
    {
        if (_cache is MemoryCache memoryCache)
        {
            memoryCache.Compact(1.0); // Compact 100% of cache
            _logger.LogInformation("All models cleared from cache");
        }
        else
        {
            _logger.LogWarning("Cache does not support bulk clear operation");
        }
    }

    /// <summary>
    /// Loads a model from disk asynchronously.
    /// </summary>
    private async Task<ITransformer> LoadModelFromDiskAsync(string modelPath, CancellationToken cancellationToken)
    {
        return await Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var stream = File.OpenRead(modelPath);
            var model = _mlContext.Model.Load(stream, out var modelInputSchema);

            _logger.LogDebug(
                "Model loaded from disk. Path: {ModelPath}, Schema columns: {ColumnCount}",
                modelPath,
                modelInputSchema.Count);

            return model;
        }, cancellationToken);
    }

    /// <summary>
    /// Generates a cache key for a model path.
    /// Uses file path and last write time to detect model updates.
    /// </summary>
    private static string GetCacheKey(string modelPath)
    {
        var fileInfo = new FileInfo(modelPath);
        var lastWriteTime = fileInfo.LastWriteTimeUtc.Ticks;
        return $"model:{modelPath}:{lastWriteTime}";
    }
}
