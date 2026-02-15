using Maliev.PredictionService.Application.Interfaces;
using StackExchange.Redis;
using System.Text.Json;
using Maliev.PredictionService.Domain.Enums;

namespace Maliev.PredictionService.Infrastructure.Caching;

/// <summary>
/// Redis cache service implementing IDistributedCache pattern
/// Supports Get, Set, Remove operations with TTL per model type
/// </summary>
public class RedisCacheService : IDistributedCacheService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase _cacheDb;
    private const int CacheDatabase = 0; // DB 0 for prediction cache

    // TTL by model type (in seconds)
    private static readonly Dictionary<ModelType, TimeSpan> TtlByModelType = new()
    {
        { ModelType.PrintTime, TimeSpan.FromHours(24) },           // 24 hours
        { ModelType.DemandForecast, TimeSpan.FromHours(6) },       // 6 hours
        { ModelType.PriceOptimization, TimeSpan.FromHours(1) },    // 1 hour
        { ModelType.ChurnPrediction, TimeSpan.FromHours(24) },     // 24 hours
        { ModelType.MaterialDemand, TimeSpan.FromHours(12) },      // 12 hours
        { ModelType.BottleneckDetection, TimeSpan.FromHours(6) }   // 6 hours
    };

    public RedisCacheService(IConnectionMultiplexer redis)
    {
        _redis = redis ?? throw new ArgumentNullException(nameof(redis));
        _cacheDb = _redis.GetDatabase(CacheDatabase);
    }

    /// <summary>
    /// Gets a cached value by key
    /// </summary>
    public async Task<T?> GetAsync<T>(string cacheKey, CancellationToken cancellationToken = default)
        where T : class
    {
        var cachedValue = await _cacheDb.StringGetAsync(cacheKey);

        if (cachedValue.IsNullOrEmpty)
            return null;

        return JsonSerializer.Deserialize<T>(cachedValue.ToString());
    }

    /// <summary>
    /// Sets a value in cache with explicit TTL
    /// </summary>
    public async Task SetAsync<T>(
        string cacheKey,
        T value,
        TimeSpan ttl,
        CancellationToken cancellationToken = default)
        where T : class
    {
        var serializedValue = JsonSerializer.Serialize(value);
        await _cacheDb.StringSetAsync(cacheKey, serializedValue, ttl);
    }

    /// <summary>
    /// Sets a value in cache with TTL based on model type
    /// </summary>
    public async Task SetAsync<T>(
        string cacheKey,
        T value,
        ModelType modelType,
        CancellationToken cancellationToken = default)
        where T : class
    {
        var ttl = GetTtl(modelType);
        await SetAsync(cacheKey, value, ttl, cancellationToken);
    }

    /// <summary>
    /// Removes a specific cache key
    /// </summary>
    public async Task RemoveAsync(string cacheKey, CancellationToken cancellationToken = default)
    {
        await _cacheDb.KeyDeleteAsync(cacheKey);
    }

    /// <summary>
    /// Invalidates all cache keys matching a pattern (e.g., "print_time:*:2.1.0")
    /// WARNING: Uses KEYS command which can be slow on large datasets
    /// Consider using SCAN in production for large keyspaces
    /// </summary>
    public async Task InvalidatePatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        var endpoints = _redis.GetEndPoints();
        var server = _redis.GetServer(endpoints.First());

        var keys = server.Keys(database: CacheDatabase, pattern: pattern);

        foreach (var key in keys)
        {
            await _cacheDb.KeyDeleteAsync(key);
        }
    }

    /// <summary>
    /// Checks if a key exists in cache
    /// </summary>
    public async Task<bool> ExistsAsync(string cacheKey, CancellationToken cancellationToken = default)
    {
        return await _cacheDb.KeyExistsAsync(cacheKey);
    }

    /// <summary>
    /// Gets TTL (time-to-live) for a model type
    /// </summary>
    public TimeSpan GetTtl(ModelType modelType)
    {
        return TtlByModelType.TryGetValue(modelType, out var ttl)
            ? ttl
            : TimeSpan.FromHours(1); // Default 1 hour
    }

    /// <summary>
    /// Gets cache statistics
    /// </summary>
    public async Task<CacheStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default)
    {
        var endpoints = _redis.GetEndPoints();
        var server = _redis.GetServer(endpoints.First());
        var info = await server.InfoAsync("stats");

        // Parse Redis INFO stats response
        var statsDict = info
            .Where(g => g.Key == "Stats")
            .SelectMany(g => g)
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        var keyspaceHits = statsDict.TryGetValue("keyspace_hits", out var hits) ? hits : "0";
        var keyspaceMisses = statsDict.TryGetValue("keyspace_misses", out var misses) ? misses : "0";

        var hitsCount = long.Parse(keyspaceHits);
        var missesCount = long.Parse(keyspaceMisses);
        var total = hitsCount + missesCount;
        var hitRate = total > 0 ? (double)hitsCount / total : 0;

        var dbSizeResult = await _cacheDb.ExecuteAsync("DBSIZE");

        return new CacheStatistics
        {
            Hits = hitsCount,
            Misses = missesCount,
            HitRate = hitRate,
            TotalKeys = (long)dbSizeResult
        };
    }
}

/// <summary>
/// Cache statistics
/// </summary>
public class CacheStatistics
{
    public long Hits { get; init; }
    public long Misses { get; init; }
    public double HitRate { get; init; }
    public long TotalKeys { get; init; }
}
