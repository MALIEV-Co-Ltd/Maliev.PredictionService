using Maliev.PredictionService.Domain.Enums;

namespace Maliev.PredictionService.Application.Interfaces;

/// <summary>
/// Interface for distributed caching of prediction results.
/// </summary>
public interface IDistributedCacheService
{
    /// <summary>
    /// Gets a cached value by key.
    /// </summary>
    Task<T?> GetAsync<T>(string cacheKey, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>
    /// Sets a value in cache with TTL.
    /// </summary>
    Task SetAsync<T>(
        string cacheKey,
        T value,
        TimeSpan ttl,
        CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>
    /// Removes a specific cache key.
    /// </summary>
    Task RemoveAsync(string cacheKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Invalidates all cache keys matching a pattern.
    /// </summary>
    Task InvalidatePatternAsync(string pattern, CancellationToken cancellationToken = default);
}
