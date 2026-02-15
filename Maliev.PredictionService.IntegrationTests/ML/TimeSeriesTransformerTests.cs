using Maliev.PredictionService.Infrastructure.ML.FeatureEngineering;
using Xunit;

namespace Maliev.PredictionService.IntegrationTests.ML;

/// <summary>
/// Integration tests for TimeSeriesTransformer.
/// Tests T138: Time-series data transformation and feature engineering.
/// </summary>
public class TimeSeriesTransformerTests
{
    private readonly TimeSeriesTransformer _transformer;

    public TimeSeriesTransformerTests()
    {
        _transformer = new TimeSeriesTransformer();
    }

    [Fact]
    public void Transform_WithHistoricalData_CreatesCalendarFeatures()
    {
        // Arrange - 30 days of demand data
        var dataPoints = new List<TimeSeriesTransformer.TimeSeriesDataPoint>();

        var startDate = new DateTime(2026, 2, 1); // February 1, 2026 (Sunday)
        for (int i = 0; i < 30; i++)
        {
            dataPoints.Add(new TimeSeriesTransformer.TimeSeriesDataPoint
            {
                ProductId = "PROD-001",
                Date = startDate.AddDays(i),
                Demand = 100m + i, // Trending demand
                IsPromotion = false
            });
        }

        // Act
        var features = _transformer.Transform(dataPoints);

        // Assert
        Assert.NotNull(features);
        Assert.Equal(30, features.Count);

        // Verify calendar features for first record (Feb 1, Sunday)
        var firstFeatures = features[0];
        Assert.Equal(0, firstFeatures.DayOfWeek); // Sunday
        Assert.Equal(2, firstFeatures.Month); // February
        Assert.Equal(1, firstFeatures.Quarter); // Q1
        Assert.True(firstFeatures.IsWeekend); // Sunday is weekend
    }

    [Fact]
    public void Transform_CreatesLagFeatures()
    {
        // Arrange - 15 days of demand with linear growth
        var dataPoints = new List<TimeSeriesTransformer.TimeSeriesDataPoint>();

        var startDate = new DateTime(2026, 2, 1);
        for (int i = 0; i < 15; i++)
        {
            dataPoints.Add(new TimeSeriesTransformer.TimeSeriesDataPoint
            {
                ProductId = "PROD-002",
                Date = startDate.AddDays(i),
                Demand = 100m + i * 10m, // 100, 110, 120, ...
                IsPromotion = false
            });
        }

        // Act
        var features = _transformer.Transform(dataPoints);

        // Assert
        Assert.NotNull(features);
        Assert.Equal(15, features.Count);

        // Day 2 (index 1) should have Lag1Day = Day 1's demand (100)
        Assert.NotNull(features[1].Lag1Day);
        Assert.Equal(100f, features[1].Lag1Day!.Value);

        // Day 8 (index 7) should have Lag7Days = Day 1's demand (100)
        Assert.NotNull(features[7].Lag7Days);
        Assert.Equal(100f, features[7].Lag7Days!.Value);

        // Day 1 (index 0) should have no lag features (no history)
        Assert.Null(features[0].Lag1Day);
        Assert.Null(features[0].Lag7Days);
    }

    [Fact]
    public void Transform_CreatesRollingStatistics()
    {
        // Arrange - 30 days with constant demand for easy calculation
        var dataPoints = new List<TimeSeriesTransformer.TimeSeriesDataPoint>();

        var startDate = new DateTime(2026, 2, 1);
        for (int i = 0; i < 30; i++)
        {
            dataPoints.Add(new TimeSeriesTransformer.TimeSeriesDataPoint
            {
                ProductId = "PROD-003",
                Date = startDate.AddDays(i),
                Demand = 100m, // Constant
                IsPromotion = false
            });
        }

        // Act
        var features = _transformer.Transform(dataPoints);

        // Assert
        Assert.NotNull(features);

        // Day 8 (index 7) should have 7-day rolling mean = 100 (constant demand)
        Assert.NotNull(features[7].Rolling7DayMean);
        Assert.Equal(100f, features[7].Rolling7DayMean!.Value);

        // Rolling std should be ~0 for constant demand
        Assert.NotNull(features[7].Rolling7DayStd);
        Assert.True(features[7].Rolling7DayStd!.Value < 0.01f, "Std should be near 0 for constant demand");

        // Day 30 (index 29) should have 30-day rolling mean = 100
        Assert.NotNull(features[29].Rolling30DayMean);
        Assert.Equal(100f, features[29].Rolling30DayMean!.Value);
    }

    [Fact]
    public void Transform_PreservesPromotionFlags()
    {
        // Arrange - Mix of promotion and non-promotion days
        var dataPoints = new List<TimeSeriesTransformer.TimeSeriesDataPoint>();

        var startDate = new DateTime(2026, 2, 1);
        for (int i = 0; i < 20; i++)
        {
            dataPoints.Add(new TimeSeriesTransformer.TimeSeriesDataPoint
            {
                ProductId = "PROD-004",
                Date = startDate.AddDays(i),
                Demand = 100m,
                IsPromotion = i >= 10 && i < 15 // Days 10-14 are promotion
            });
        }

        // Act
        var features = _transformer.Transform(dataPoints);

        // Assert
        Assert.NotNull(features);

        // Verify promotion flags
        for (int i = 0; i < features.Count; i++)
        {
            var expected = i >= 10 && i < 15;
            Assert.Equal(expected, features[i].IsPromotion);
        }

        // Day 11 should have PromoLag1Day = true (Day 10 was promo)
        Assert.True(features[11].PromoLag1Day);
    }

    [Fact]
    public void Transform_DetectsHolidays()
    {
        // Arrange - Data including New Year's Day 2026
        var dataPoints = new List<TimeSeriesTransformer.TimeSeriesDataPoint>
        {
            new()
            {
                ProductId = "PROD-005",
                Date = new DateTime(2026, 1, 1), // New Year's Day
                Demand = 100m,
                IsPromotion = false
            },
            new()
            {
                ProductId = "PROD-005",
                Date = new DateTime(2026, 1, 2), // Not a holiday
                Demand = 100m,
                IsPromotion = false
            }
        };

        // Act
        var features = _transformer.Transform(dataPoints);

        // Assert
        Assert.NotNull(features);
        Assert.Equal(2, features.Count);

        // Jan 1 should be marked as holiday
        Assert.True(features[0].IsHoliday, "Jan 1 should be holiday");

        // Jan 2 should not be holiday
        Assert.False(features[1].IsHoliday, "Jan 2 should not be holiday");
    }

    [Fact]
    public void Transform_WithEmptyData_ThrowsArgumentException()
    {
        // Arrange
        var emptyData = new List<TimeSeriesTransformer.TimeSeriesDataPoint>();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            _transformer.Transform(emptyData));

        Assert.Contains("null or empty", exception.Message.ToLower());
    }

    [Fact]
    public void Transform_SortsDataChronologically()
    {
        // Arrange - Unordered data
        var dataPoints = new List<TimeSeriesTransformer.TimeSeriesDataPoint>
        {
            new()
            {
                ProductId = "PROD-006",
                Date = new DateTime(2026, 2, 5),
                Demand = 150m,
                IsPromotion = false
            },
            new()
            {
                ProductId = "PROD-006",
                Date = new DateTime(2026, 2, 1), // Earlier date
                Demand = 100m,
                IsPromotion = false
            },
            new()
            {
                ProductId = "PROD-006",
                Date = new DateTime(2026, 2, 3),
                Demand = 120m,
                IsPromotion = false
            }
        };

        // Act
        var features = _transformer.Transform(dataPoints);

        // Assert
        Assert.NotNull(features);
        Assert.Equal(3, features.Count);

        // Verify chronological order
        Assert.Equal(100f, features[0].Demand); // Feb 1
        Assert.Equal(120f, features[1].Demand); // Feb 3
        Assert.Equal(150f, features[2].Demand); // Feb 5
    }

    [Fact]
    public void Transform_CalculatesQuarterFeatures()
    {
        // Arrange - Data spanning quarters
        var dataPoints = new List<TimeSeriesTransformer.TimeSeriesDataPoint>
        {
            new()
            {
                ProductId = "PROD-007",
                Date = new DateTime(2026, 1, 2), // Q1
                Demand = 100m,
                IsPromotion = false
            },
            new()
            {
                ProductId = "PROD-007",
                Date = new DateTime(2026, 4, 2), // Q2
                Demand = 110m,
                IsPromotion = false
            },
            new()
            {
                ProductId = "PROD-007",
                Date = new DateTime(2026, 7, 2), // Q3
                Demand = 120m,
                IsPromotion = false
            },
            new()
            {
                ProductId = "PROD-007",
                Date = new DateTime(2026, 10, 2), // Q4
                Demand = 130m,
                IsPromotion = false
            }
        };

        // Act
        var features = _transformer.Transform(dataPoints);

        // Assert
        Assert.NotNull(features);
        Assert.Equal(4, features.Count);

        Assert.Equal(1, features[0].Quarter); // Q1
        Assert.Equal(2, features[1].Quarter); // Q2
        Assert.Equal(3, features[2].Quarter); // Q3
        Assert.Equal(4, features[3].Quarter); // Q4
    }
}
