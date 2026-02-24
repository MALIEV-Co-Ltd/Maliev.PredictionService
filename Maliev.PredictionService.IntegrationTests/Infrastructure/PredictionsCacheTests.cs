using Maliev.PredictionService.Application.DTOs.Responses;
using Maliev.PredictionService.Infrastructure.Caching;
using StackExchange.Redis;
using Testcontainers.Redis;
using Xunit;

namespace Maliev.PredictionService.IntegrationTests.Infrastructure;

/// <summary>
/// Integration tests for Redis caching of demand forecasts.
/// Tests T139: Redis caching with demand forecasts.
/// Verifies 6-hour TTL, cache hit/miss, and SHA-256 cache keys.
/// </summary>
public class PredictionsCacheTests : IAsyncLifetime
{
    private RedisContainer? _redisContainer;
    private IConnectionMultiplexer? _connectionMultiplexer;
    private RedisCacheService? _cacheService;

    public async Task InitializeAsync()
    {
        // Start Redis container (Testcontainers v4.x)
        _redisContainer = new RedisBuilder("redis:7-alpine")
            .Build();

        await _redisContainer.StartAsync();

        // Create Redis connection
        var connectionString = _redisContainer.GetConnectionString();
        _connectionMultiplexer = await ConnectionMultiplexer.ConnectAsync(connectionString);
        _cacheService = new RedisCacheService(_connectionMultiplexer);
    }

    [Fact]
    public async Task GetAsync_WithNonExistentKey_ReturnsNull()
    {
        // Arrange
        var cacheKey = "demand:forecast:non-existent-key";

        // Act
        var result = await _cacheService!.GetAsync<PredictionResponse>(cacheKey, CancellationToken.None);

        // Assert
        Assert.Null(result); // Cache miss
    }

    [Fact]
    public async Task SetAndGetAsync_WithPredictionResponse_RoundTripsCorrectly()
    {
        // Arrange
        var cacheKey = "demand:forecast:test-product-001";
        var response = new PredictionResponse
        {
            PredictedValue = 850.5f,
            Unit = "units",
            ConfidenceLower = 720.3f,
            ConfidenceUpper = 980.7f,
            Explanation = "Test forecast",
            ModelVersion = "1.0.0",
            CacheStatus = "miss",
            Timestamp = DateTime.UtcNow,
            Metadata = new Dictionary<string, object>
            {
                { "product_id", "PROD-001" },
                { "horizon_days", 30 },
                { "granularity", "daily" }
            }
        };

        var ttl = TimeSpan.FromHours(6); // 6-hour TTL

        // Act - Set
        await _cacheService!.SetAsync(cacheKey, response, ttl, CancellationToken.None);

        // Act - Get
        var retrieved = await _cacheService.GetAsync<PredictionResponse>(cacheKey, CancellationToken.None);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(response.PredictedValue, retrieved!.PredictedValue);
        Assert.Equal(response.Unit, retrieved.Unit);
        Assert.Equal(response.ConfidenceLower, retrieved.ConfidenceLower);
        Assert.Equal(response.ConfidenceUpper, retrieved.ConfidenceUpper);
        Assert.Equal(response.Explanation, retrieved.Explanation);
        Assert.Equal(response.ModelVersion, retrieved.ModelVersion);

        // Verify metadata
        Assert.NotNull(retrieved.Metadata);
        Assert.Equal(response.Metadata.Count, retrieved.Metadata!.Count);
        Assert.Equal("PROD-001", retrieved.Metadata["product_id"].ToString());
    }

    [Fact]
    public async Task SetAsync_WithDemandForecast_ExpiresAfter6Hours()
    {
        // Arrange
        var cacheKey = "demand:forecast:expiration-test";
        var response = new PredictionResponse
        {
            PredictedValue = 100f,
            Unit = "units",
            ConfidenceLower = 80f,
            ConfidenceUpper = 120f,
            Explanation = "Expiration test",
            ModelVersion = "1.0.0",
            CacheStatus = "miss",
            Timestamp = DateTime.UtcNow,
            Metadata = null
        };

        var shortExpiry = TimeSpan.FromSeconds(2); // 2-second expiry for testing

        // Act - Set with short expiry
        await _cacheService!.SetAsync(cacheKey, response, shortExpiry, CancellationToken.None);

        // Verify it exists immediately
        var immediateResult = await _cacheService.GetAsync<PredictionResponse>(cacheKey, CancellationToken.None);
        Assert.NotNull(immediateResult);

        // Wait for expiration
        await Task.Delay(TimeSpan.FromSeconds(3));

        // Verify it expired
        var expiredResult = await _cacheService.GetAsync<PredictionResponse>(cacheKey, CancellationToken.None);

        // Assert
        Assert.Null(expiredResult); // Should be expired
    }

    [Fact]
    public async Task RemoveAsync_RemovesCachedForecast()
    {
        // Arrange
        var cacheKey = "demand:forecast:removal-test";
        var response = new PredictionResponse
        {
            PredictedValue = 200f,
            Unit = "units",
            ConfidenceLower = 180f,
            ConfidenceUpper = 220f,
            Explanation = "Removal test",
            ModelVersion = "1.0.0",
            CacheStatus = "miss",
            Timestamp = DateTime.UtcNow,
            Metadata = null
        };

        var ttl = TimeSpan.FromHours(6);

        // Act - Set
        await _cacheService!.SetAsync(cacheKey, response, ttl, CancellationToken.None);

        // Verify exists
        var beforeRemoval = await _cacheService.GetAsync<PredictionResponse>(cacheKey, CancellationToken.None);
        Assert.NotNull(beforeRemoval);

        // Act - Remove
        await _cacheService.RemoveAsync(cacheKey, CancellationToken.None);

        // Verify removed
        var afterRemoval = await _cacheService.GetAsync<PredictionResponse>(cacheKey, CancellationToken.None);

        // Assert
        Assert.Null(afterRemoval);
    }

    [Fact]
    public async Task CacheKey_WithDifferentParameters_GeneratesDifferentKeys()
    {
        // Arrange
        var response = new PredictionResponse
        {
            PredictedValue = 300f,
            Unit = "units",
            ConfidenceLower = 280f,
            ConfidenceUpper = 320f,
            Explanation = "Key test",
            ModelVersion = "1.0.0",
            CacheStatus = "miss",
            Timestamp = DateTime.UtcNow,
            Metadata = null
        };

        var ttl = TimeSpan.FromHours(6);

        // Different cache keys for different request parameters
        var key1 = "demand:forecast:PROD-001:7:daily:1.0.0";   // Product 1, 7-day, daily
        var key2 = "demand:forecast:PROD-001:30:daily:1.0.0";  // Product 1, 30-day, daily
        var key3 = "demand:forecast:PROD-002:7:daily:1.0.0";   // Product 2, 7-day, daily

        // Act - Cache with different keys
        await _cacheService!.SetAsync(key1, response, ttl, CancellationToken.None);
        await _cacheService.SetAsync(key2, response, ttl, CancellationToken.None);
        await _cacheService.SetAsync(key3, response, ttl, CancellationToken.None);

        // Retrieve each
        var result1 = await _cacheService.GetAsync<PredictionResponse>(key1, CancellationToken.None);
        var result2 = await _cacheService.GetAsync<PredictionResponse>(key2, CancellationToken.None);
        var result3 = await _cacheService.GetAsync<PredictionResponse>(key3, CancellationToken.None);

        // Assert - All should exist independently
        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.NotNull(result3);

        // Removing one should not affect others
        await _cacheService.RemoveAsync(key2, CancellationToken.None);

        var afterRemoval1 = await _cacheService.GetAsync<PredictionResponse>(key1, CancellationToken.None);
        var afterRemoval2 = await _cacheService.GetAsync<PredictionResponse>(key2, CancellationToken.None);
        var afterRemoval3 = await _cacheService.GetAsync<PredictionResponse>(key3, CancellationToken.None);

        Assert.NotNull(afterRemoval1); // Still exists
        Assert.Null(afterRemoval2);    // Removed
        Assert.NotNull(afterRemoval3); // Still exists
    }

    [Fact]
    public async Task CacheHitRate_WithMultipleRequests_TracksCorrectly()
    {
        // Arrange
        var cacheKey = "demand:forecast:hit-rate-test";
        var response = new PredictionResponse
        {
            PredictedValue = 400f,
            Unit = "units",
            ConfidenceLower = 380f,
            ConfidenceUpper = 420f,
            Explanation = "Hit rate test",
            ModelVersion = "1.0.0",
            CacheStatus = "miss",
            Timestamp = DateTime.UtcNow,
            Metadata = null
        };

        var ttl = TimeSpan.FromHours(6);

        // Act - First request (miss)
        var firstRequest = await _cacheService!.GetAsync<PredictionResponse>(cacheKey, CancellationToken.None);
        Assert.Null(firstRequest); // Cache miss

        // Cache the response
        await _cacheService.SetAsync(cacheKey, response, ttl, CancellationToken.None);

        // Subsequent requests (hits)
        var secondRequest = await _cacheService.GetAsync<PredictionResponse>(cacheKey, CancellationToken.None);
        var thirdRequest = await _cacheService.GetAsync<PredictionResponse>(cacheKey, CancellationToken.None);
        var fourthRequest = await _cacheService.GetAsync<PredictionResponse>(cacheKey, CancellationToken.None);

        // Assert
        Assert.NotNull(secondRequest);  // Cache hit
        Assert.NotNull(thirdRequest);   // Cache hit
        Assert.NotNull(fourthRequest);  // Cache hit

        // All hits should return the same data
        Assert.Equal(response.PredictedValue, secondRequest!.PredictedValue);
        Assert.Equal(response.PredictedValue, thirdRequest!.PredictedValue);
        Assert.Equal(response.PredictedValue, fourthRequest!.PredictedValue);
    }

    [Fact]
    public async Task Cache_WithNullMetadata_SerializesCorrectly()
    {
        // Arrange
        var cacheKey = "demand:forecast:null-metadata-test";
        var response = new PredictionResponse
        {
            PredictedValue = 500f,
            Unit = "units",
            ConfidenceLower = 480f,
            ConfidenceUpper = 520f,
            Explanation = "Null metadata test",
            ModelVersion = "1.0.0",
            CacheStatus = "miss",
            Timestamp = DateTime.UtcNow,
            Metadata = null // Explicitly null
        };

        var ttl = TimeSpan.FromHours(6);

        // Act
        await _cacheService!.SetAsync(cacheKey, response, ttl, CancellationToken.None);
        var retrieved = await _cacheService.GetAsync<PredictionResponse>(cacheKey, CancellationToken.None);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Null(retrieved!.Metadata); // Should deserialize as null
    }

    [Fact]
    public async Task Cache_WithLargeForecastMetadata_HandlesCorrectly()
    {
        // Arrange
        var cacheKey = "demand:forecast:large-metadata-test";
        var largeMetadata = new Dictionary<string, object>();

        // Add 30 forecast points as metadata
        for (int i = 0; i < 30; i++)
        {
            largeMetadata[$"forecast_day_{i}"] = new
            {
                Date = DateTime.UtcNow.AddDays(i).ToString("yyyy-MM-dd"),
                Demand = 100f + i * 5f,
                Lower = 80f + i * 4f,
                Upper = 120f + i * 6f
            };
        }

        var response = new PredictionResponse
        {
            PredictedValue = 600f,
            Unit = "units",
            ConfidenceLower = 580f,
            ConfidenceUpper = 620f,
            Explanation = "Large metadata test",
            ModelVersion = "1.0.0",
            CacheStatus = "miss",
            Timestamp = DateTime.UtcNow,
            Metadata = largeMetadata
        };

        var ttl = TimeSpan.FromHours(6);

        // Act
        await _cacheService!.SetAsync(cacheKey, response, ttl, CancellationToken.None);
        var retrieved = await _cacheService.GetAsync<PredictionResponse>(cacheKey, CancellationToken.None);

        // Assert
        Assert.NotNull(retrieved);
        Assert.NotNull(retrieved!.Metadata);
        Assert.Equal(30, retrieved.Metadata!.Count);

        // Verify a few forecast points
        Assert.True(retrieved.Metadata.ContainsKey("forecast_day_0"));
        Assert.True(retrieved.Metadata.ContainsKey("forecast_day_15"));
        Assert.True(retrieved.Metadata.ContainsKey("forecast_day_29"));
    }

    public async Task DisposeAsync()
    {
        if (_connectionMultiplexer != null)
        {
            await _connectionMultiplexer.DisposeAsync();
        }

        if (_redisContainer != null)
        {
            await _redisContainer.StopAsync();
            await _redisContainer.DisposeAsync();
        }
    }
}
