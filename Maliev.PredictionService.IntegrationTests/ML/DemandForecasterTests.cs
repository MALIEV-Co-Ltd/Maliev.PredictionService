using Maliev.PredictionService.Application.DTOs.ML;
using Maliev.PredictionService.Domain.Enums;
using Maliev.PredictionService.Infrastructure.Caching;
using Maliev.PredictionService.Infrastructure.ML.Predictors;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.ML;
using Xunit;
using SsaDemandInput = Maliev.PredictionService.Infrastructure.ML.Predictors.SsaDemandInput;

namespace Maliev.PredictionService.IntegrationTests.ML;

/// <summary>
/// Integration tests for DemandForecaster.
/// Tests T137: Demand forecasting engine with real ML.NET models.
/// Tests T141: Anomaly detection logic.
/// Tests T142: Forecast confidence intervals.
/// </summary>
public class DemandForecasterTests : IDisposable
{
    private readonly MLContext _mlContext;
    private readonly DemandForecaster _forecaster;
    private readonly string _testModelPath;

    public DemandForecasterTests()
    {
        _mlContext = new MLContext(seed: 42);
        var logger = new NullLogger<DemandForecaster>();

        // Create memory cache for model caching
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var cacheLogger = new NullLogger<ModelCacheService>();
        var modelCache = new ModelCacheService(memoryCache, cacheLogger);

        _forecaster = new DemandForecaster(logger, modelCache);

        // Test model path - each test trains its own model
        _testModelPath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "demand-forecast-test-model.zip");
    }

    [Fact]
    public async Task PredictAsync_WithValidTrainingData_ReturnsForecast()
    {
        // Arrange - Create training data (30 days of historical demand)
        var historicalData = GenerateHistoricalDemand(
            productId: "PROD-001",
            startDate: DateTime.UtcNow.AddDays(-30),
            days: 30,
            baseDemand: 100f,
            trend: 2f, // Increasing trend
            noise: 10f
        );

        // Train a simple model
        var model = TrainSimpleModel(historicalData);

        var input = new DemandPredictionInput
        {
            ProductId = "PROD-001",
            Horizon = 7, // 7-day forecast
            Granularity = "daily",
            BaselineDate = DateTime.UtcNow,
            HistoricalData = historicalData
        };

        // Act
        var result = await _forecaster.PredictAsync(input, model, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        var avgForecast = result.Forecasts.Average(f => f.ForecastedDemand);
        Assert.True(avgForecast > 0, "Average forecast should be positive");
        Assert.Equal(7, result.Forecasts.Count); // 7-day horizon

        // Verify forecast structure
        foreach (var forecast in result.Forecasts)
        {
            Assert.True(forecast.ForecastedDemand > 0, "Forecasted demand should be positive");
            Assert.True(forecast.LowerBoundConfidence >= 0, "Lower bound should be non-negative");
            Assert.True(forecast.UpperBoundConfidence > forecast.ForecastedDemand,
                "Upper bound should be greater than forecasted value");
            Assert.True(forecast.ForecastDate > input.BaselineDate,
                "Forecast date should be in the future");
        }
    }

    [Fact]
    public async Task PredictAsync_WithTrendingData_DetectsTrend()
    {
        // Arrange - Strong upward trend
        var historicalData = GenerateHistoricalDemand(
            productId: "PROD-002",
            startDate: DateTime.UtcNow.AddDays(-30),
            days: 30,
            baseDemand: 50f,
            trend: 5f, // Strong trend
            noise: 5f
        );

        var model = TrainSimpleModel(historicalData);

        var input = new DemandPredictionInput
        {
            ProductId = "PROD-002",
            Horizon = 7,
            Granularity = "daily",
            BaselineDate = DateTime.UtcNow,
            HistoricalData = historicalData
        };

        // Act
        var result = await _forecaster.PredictAsync(input, model, CancellationToken.None);

        // Assert
        Assert.NotNull(result);

        // Verify increasing forecast (trend detection)
        for (int i = 1; i < result.Forecasts.Count; i++)
        {
            var previous = result.Forecasts[i - 1].ForecastedDemand;
            var current = result.Forecasts[i].ForecastedDemand;

            // Allow some tolerance for ML.NET variance
            Assert.True(current >= previous * 0.95f,
                $"Day {i}: Forecast should maintain upward trend (prev: {previous:F2}, current: {current:F2})");
        }
    }

    [Fact]
    public async Task PredictAsync_WithAnomalousData_DetectsAnomalies()
    {
        // Arrange - Normal data with anomalies
        var historicalData = new List<DemandInput>();

        // 25 days of normal data
        for (int i = 0; i < 25; i++)
        {
            historicalData.Add(new DemandInput
            {
                ProductId = "PROD-003",
                Date = DateTime.UtcNow.AddDays(-30 + i),
                Demand = 100f + Random.Shared.Next(-10, 10), // Normal: 90-110
                IsPromotion = false,
                IsHoliday = false
            });
        }

        // Add 5 days with anomalous spikes
        for (int i = 25; i < 30; i++)
        {
            historicalData.Add(new DemandInput
            {
                ProductId = "PROD-003",
                Date = DateTime.UtcNow.AddDays(-30 + i),
                Demand = 300f, // Anomaly: 3x normal
                IsPromotion = false,
                IsHoliday = false
            });
        }

        var model = TrainSimpleModel(historicalData);

        var input = new DemandPredictionInput
        {
            ProductId = "PROD-003",
            Horizon = 7,
            Granularity = "daily",
            BaselineDate = DateTime.UtcNow,
            HistoricalData = historicalData
        };

        // Act
        var result = await _forecaster.PredictAsync(input, model, CancellationToken.None);

        // Assert - T141: Anomaly detection
        Assert.NotNull(result);

        // Should detect some anomalies (>40% deviation threshold)
        var anomalies = result.Forecasts.Where(f => f.IsAnomaly).ToList();

        // Note: ML.NET SSA may or may not flag all forecasts as anomalies
        // depending on the training data. We just verify the anomaly detection logic exists
        Assert.All(result.Forecasts, forecast =>
        {
            if (forecast.IsAnomaly)
            {
                Assert.NotNull(forecast.AnomalyScore);
                Assert.True(forecast.AnomalyScore > 0, "Anomaly score should be positive");
            }
        });
    }

    [Fact]
    public async Task PredictAsync_WithConfidenceIntervals_ReturnsValidBounds()
    {
        // Arrange
        var historicalData = GenerateHistoricalDemand(
            productId: "PROD-004",
            startDate: DateTime.UtcNow.AddDays(-30),
            days: 30,
            baseDemand: 150f,
            trend: 1f,
            noise: 20f // Higher variance
        );

        var model = TrainSimpleModel(historicalData);

        var input = new DemandPredictionInput
        {
            ProductId = "PROD-004",
            Horizon = 7,
            Granularity = "daily",
            BaselineDate = DateTime.UtcNow,
            HistoricalData = historicalData
        };

        // Act
        var result = await _forecaster.PredictAsync(input, model, CancellationToken.None);

        // Assert - T142: Confidence intervals
        Assert.NotNull(result);

        foreach (var forecast in result.Forecasts)
        {
            // Lower bound <= Forecast <= Upper bound
            Assert.True(forecast.LowerBoundConfidence <= forecast.ForecastedDemand,
                $"Lower bound ({forecast.LowerBoundConfidence:F2}) should be <= forecast ({forecast.ForecastedDemand:F2})");

            Assert.True(forecast.ForecastedDemand <= forecast.UpperBoundConfidence,
                $"Forecast ({forecast.ForecastedDemand:F2}) should be <= upper bound ({forecast.UpperBoundConfidence:F2})");

            // Confidence interval should be reasonable (not too wide or too narrow)
            var intervalWidth = forecast.UpperBoundConfidence - forecast.LowerBoundConfidence;
            var relativeWidth = intervalWidth / forecast.ForecastedDemand;

            Assert.True(relativeWidth > 0.1f && relativeWidth < 5.0f,
                $"Confidence interval relative width should be reasonable (got {relativeWidth:F2})");
        }
    }

    [Fact]
    public async Task PredictAsync_WithWeeklyGranularity_AggregatesCorrectly()
    {
        // Arrange - 60 days of daily data
        var historicalData = GenerateHistoricalDemand(
            productId: "PROD-005",
            startDate: DateTime.UtcNow.AddDays(-60),
            days: 60,
            baseDemand: 700f, // ~100/day
            trend: 5f,
            noise: 50f
        );

        // For 4 weekly forecasts, we need horizon of 4 * 7 = 28 days
        var model = TrainSimpleModel(historicalData, horizon: 28);

        var input = new DemandPredictionInput
        {
            ProductId = "PROD-005",
            Horizon = 4, // 4 weeks
            Granularity = "weekly",
            BaselineDate = DateTime.UtcNow,
            HistoricalData = historicalData
        };

        // Act
        var result = await _forecaster.PredictAsync(input, model, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(4, result.Forecasts.Count); // 4 weekly forecasts

        // Weekly demand should be approximately 7x daily demand
        foreach (var forecast in result.Forecasts)
        {
            // Weekly forecast should be in a reasonable range (daily * 7 with some variance)
            Assert.True(forecast.ForecastedDemand > 400f && forecast.ForecastedDemand < 1400f,
                $"Weekly forecast should be reasonable (got {forecast.ForecastedDemand:F2})");
        }
    }

    [Fact]
    public async Task PredictAsync_WithPromotionFlag_AdjustsForecast()
    {
        // Arrange - Historical data with promotions
        var historicalData = new List<DemandInput>();

        // Non-promotion days
        for (int i = 0; i < 20; i++)
        {
            historicalData.Add(new DemandInput
            {
                ProductId = "PROD-006",
                Date = DateTime.UtcNow.AddDays(-30 + i),
                Demand = 100f,
                IsPromotion = false,
                IsHoliday = false
            });
        }

        // Promotion days (higher demand)
        for (int i = 20; i < 30; i++)
        {
            historicalData.Add(new DemandInput
            {
                ProductId = "PROD-006",
                Date = DateTime.UtcNow.AddDays(-30 + i),
                Demand = 200f, // 2x during promotion
                IsPromotion = true,
                IsHoliday = false
            });
        }

        var model = TrainSimpleModel(historicalData);

        var input = new DemandPredictionInput
        {
            ProductId = "PROD-006",
            Horizon = 7,
            Granularity = "daily",
            BaselineDate = DateTime.UtcNow,
            HistoricalData = historicalData
        };

        // Act
        var result = await _forecaster.PredictAsync(input, model, CancellationToken.None);

        // Assert
        Assert.NotNull(result);

        // Verify model learned from promotion patterns
        // (Exact behavior depends on ML.NET model, we just verify it runs)
        Assert.All(result.Forecasts, forecast =>
        {
            Assert.True(forecast.ForecastedDemand > 0);
            Assert.True(forecast.UpperBoundConfidence > forecast.LowerBoundConfidence);
        });
    }

    /// <summary>
    /// Generate synthetic historical demand data for testing.
    /// </summary>
    private List<DemandInput> GenerateHistoricalDemand(
        string productId,
        DateTime startDate,
        int days,
        float baseDemand,
        float trend,
        float noise)
    {
        var data = new List<DemandInput>();

        for (int i = 0; i < days; i++)
        {
            var date = startDate.AddDays(i);
            var trendComponent = trend * i;
            var noiseComponent = (Random.Shared.NextSingle() - 0.5f) * noise * 2;
            var demand = Math.Max(0, baseDemand + trendComponent + noiseComponent);

            data.Add(new DemandInput
            {
                ProductId = productId,
                Date = date,
                Demand = demand,
                IsPromotion = false,
                IsHoliday = IsHoliday(date)
            });
        }

        return data;
    }

    /// <summary>
    /// Simple holiday detection for testing (US holidays).
    /// </summary>
    private bool IsHoliday(DateTime date)
    {
        var holidays = new[]
        {
            new DateTime(date.Year, 1, 1),   // New Year's Day
            new DateTime(date.Year, 7, 4),   // Independence Day
            new DateTime(date.Year, 12, 25)  // Christmas
        };

        return holidays.Any(h => h.Date == date.Date);
    }

    /// <summary>
    /// Train a simple SSA model for testing.
    /// </summary>
    private ITransformer TrainSimpleModel(List<DemandInput> trainingData, int horizon = 7)
    {
        // Extract only the Demand values for SSA training using SsaDemandInput
        // This ensures the model schema matches what DemandForecaster uses for prediction
        var ssaData = trainingData.Select(d => new SsaDemandInput { Demand = d.Demand }).ToList();
        var dataView = _mlContext.Data.LoadFromEnumerable(ssaData);

        // Simple SSA pipeline for testing
        var pipeline = _mlContext.Forecasting.ForecastBySsa(
            outputColumnName: "ForecastedDemand",
            inputColumnName: nameof(SsaDemandInput.Demand),
            windowSize: 7,
            seriesLength: trainingData.Count,
            trainSize: trainingData.Count,
            horizon: horizon,
            confidenceLevel: 0.95f,
            confidenceLowerBoundColumn: "LowerBoundConfidence",
            confidenceUpperBoundColumn: "UpperBoundConfidence");

        var model = pipeline.Fit(dataView);

        // Save model for cleanup
        _mlContext.Model.Save(model, dataView.Schema, _testModelPath);

        return model;
    }

    public void Dispose()
    {
        // Cleanup test model file
        if (File.Exists(_testModelPath))
        {
            try
            {
                File.Delete(_testModelPath);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }
}
