using System.Globalization;
using Maliev.PredictionService.Application.DTOs.ML;
using Maliev.PredictionService.Application.Interfaces;

namespace Maliev.PredictionService.Infrastructure.ML.FeatureEngineering;

/// <summary>
/// Transforms time-series sales data into ML features for demand forecasting.
/// Implements calendar features, lag features, rolling statistics, and promotion flags.
/// </summary>
public class TimeSeriesTransformer : ITimeSeriesTransformer
{
    private static readonly HashSet<DateTime> Holidays = LoadHolidays();

    /// <summary>
    /// Transforms raw time-series data for a specific date into a feature vector.
    /// Implements T124: Time-series transformation logic.
    /// </summary>
    public Dictionary<string, float> Transform(DateTime date, IEnumerable<dynamic> historicalData, bool isPromotion)
    {
        // For dynamic inputs from Application layer (which shouldn't know Infrastructure types)
        var dataList = new List<TimeSeriesDataPoint>();
        foreach (var item in historicalData)
        {
            dataList.Add(new TimeSeriesDataPoint
            {
                Date = item.Date,
                Demand = (decimal)item.Demand,
                ProductId = item.ProductId,
                IsPromotion = item.IsPromotion
            });
        }

        return Transform(date, dataList, isPromotion);
    }

    /// <summary>
    /// Core transformation logic for a single point in time.
    /// </summary>
    public Dictionary<string, float> Transform(DateTime date, List<TimeSeriesDataPoint> historicalData, bool isPromotion)
    {
        var sortedData = historicalData.OrderBy(d => d.Date).ToList();
        var currentIndex = sortedData.FindIndex(d => d.Date.Date == date.Date);

        // If date not in history, append it temporarily for feature calculation
        if (currentIndex == -1)
        {
            sortedData.Add(new TimeSeriesDataPoint { Date = date, Demand = 0, ProductId = "temp", IsPromotion = isPromotion });
            sortedData = sortedData.OrderBy(d => d.Date).ToList();
            currentIndex = sortedData.FindIndex(d => d.Date.Date == date.Date);
        }

        var features = new Dictionary<string, float>
        {
            ["DayOfWeek"] = (float)date.DayOfWeek,
            ["Month"] = (float)date.Month,
            ["Quarter"] = (float)GetQuarter(date),
            ["DayOfMonth"] = (float)date.Day,
            ["IsWeekend"] = date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday ? 1f : 0f,
            ["IsHoliday"] = Holidays.Contains(date.Date) ? 1f : 0f,
            ["IsPromotion"] = isPromotion ? 1f : 0f
        };

        // Add lag features
        var lag1 = GetLagDemand(sortedData, currentIndex, 1);
        if (lag1.HasValue) features["Lag1Day"] = lag1.Value;

        var lag7 = GetLagDemand(sortedData, currentIndex, 7);
        if (lag7.HasValue) features["Lag7Days"] = lag7.Value;

        // Add rolling features
        var mean7 = CalculateRollingMean(sortedData, currentIndex, 7);
        if (mean7.HasValue) features["Rolling7DayMean"] = mean7.Value;

        return features;
    }

    /// <summary>
    /// Time-series data point for transformation.
    /// </summary>
    public record TimeSeriesDataPoint
    {
        public required DateTime Date { get; init; }
        public required decimal Demand { get; init; }
        public required string ProductId { get; init; }
        public bool IsPromotion { get; init; }
    }

    /// <summary>
    /// Transformed features for ML model input.
    /// </summary>
    public record TransformedFeatures
    {
        // Calendar features
        public required int DayOfWeek { get; init; } // 0-6 (Sunday-Saturday)
        public required int Month { get; init; } // 1-12
        public required int Quarter { get; init; } // 1-4
        public required int DayOfMonth { get; init; } // 1-31
        public required int WeekOfYear { get; init; } // 1-53
        public required bool IsWeekend { get; init; }
        public required bool IsHoliday { get; init; }
        public required bool IsMonthStart { get; init; } // First 3 days
        public required bool IsMonthEnd { get; init; } // Last 3 days
        public required bool IsQuarterStart { get; init; }
        public required bool IsQuarterEnd { get; init; }

        // Lag features (previous demand)
        public float? Lag1Day { get; init; } // Previous day
        public float? Lag7Days { get; init; } // Same day last week
        public float? Lag14Days { get; init; } // 2 weeks ago
        public float? Lag30Days { get; init; } // Same day last month

        // Rolling statistics
        public float? Rolling7DayMean { get; init; } // 7-day moving average
        public float? Rolling7DayStd { get; init; } // 7-day volatility
        public float? Rolling30DayMean { get; init; } // 30-day moving average
        public float? Rolling30DayStd { get; init; } // 30-day volatility

        // Promotion flags
        public required bool IsPromotion { get; init; }
        public bool PromoLag1Day { get; init; } // Was yesterday a promo?
        public bool PromoLag7Days { get; init; } // Was same day last week a promo?

        // Target
        public required float Demand { get; init; }
    }

    /// <summary>
    /// Transforms a time-series dataset into ML features.
    /// </summary>
    /// <param name="dataPoints">Historical time-series data sorted by date ascending.</param>
    /// <returns>Transformed feature set ready for ML training/prediction.</returns>
    public List<TransformedFeatures> Transform(List<TimeSeriesDataPoint> dataPoints)
    {
        if (dataPoints == null || dataPoints.Count == 0)
            throw new ArgumentException("Data points cannot be null or empty", nameof(dataPoints));

        // Sort by date to ensure correct lag calculation
        var sortedData = dataPoints.OrderBy(d => d.Date).ToList();

        var features = new List<TransformedFeatures>();

        for (int i = 0; i < sortedData.Count; i++)
        {
            var current = sortedData[i];

            var transformed = new TransformedFeatures
            {
                // Calendar features
                DayOfWeek = (int)current.Date.DayOfWeek,
                Month = current.Date.Month,
                Quarter = GetQuarter(current.Date),
                DayOfMonth = current.Date.Day,
                WeekOfYear = GetWeekOfYear(current.Date),
                IsWeekend = current.Date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday,
                IsHoliday = Holidays.Contains(current.Date.Date),
                IsMonthStart = current.Date.Day <= 3,
                IsMonthEnd = current.Date.Day >= DateTime.DaysInMonth(current.Date.Year, current.Date.Month) - 2,
                IsQuarterStart = IsQuarterStartMonth(current.Date) && current.Date.Day <= 3,
                IsQuarterEnd = IsQuarterEndMonth(current.Date) && current.Date.Day >= DateTime.DaysInMonth(current.Date.Year, current.Date.Month) - 2,

                // Lag features
                Lag1Day = GetLagDemand(sortedData, i, lagDays: 1),
                Lag7Days = GetLagDemand(sortedData, i, lagDays: 7),
                Lag14Days = GetLagDemand(sortedData, i, lagDays: 14),
                Lag30Days = GetLagDemand(sortedData, i, lagDays: 30),

                // Rolling statistics
                Rolling7DayMean = CalculateRollingMean(sortedData, i, windowDays: 7),
                Rolling7DayStd = CalculateRollingStd(sortedData, i, windowDays: 7),
                Rolling30DayMean = CalculateRollingMean(sortedData, i, windowDays: 30),
                Rolling30DayStd = CalculateRollingStd(sortedData, i, windowDays: 30),

                // Promotion flags
                IsPromotion = current.IsPromotion,
                PromoLag1Day = GetLagPromotion(sortedData, i, lagDays: 1),
                PromoLag7Days = GetLagPromotion(sortedData, i, lagDays: 7),

                // Target
                Demand = (float)current.Demand
            };

            features.Add(transformed);
        }

        return features;
    }

    /// <summary>
    /// Gets demand value from N days ago.
    /// </summary>
    private static float? GetLagDemand(List<TimeSeriesDataPoint> data, int currentIndex, int lagDays)
    {
        if (currentIndex < lagDays)
            return null; // Not enough history

        var lagIndex = currentIndex - lagDays;
        if (lagIndex >= 0 && lagIndex < data.Count)
        {
            // Verify it's actually N days ago (handle missing dates)
            var expectedDate = data[currentIndex].Date.AddDays(-lagDays);
            if ((data[lagIndex].Date - expectedDate).TotalDays < 1) // Allow 1-day tolerance
            {
                return (float)data[lagIndex].Demand;
            }
        }

        return null;
    }

    /// <summary>
    /// Gets promotion flag from N days ago.
    /// </summary>
    private static bool GetLagPromotion(List<TimeSeriesDataPoint> data, int currentIndex, int lagDays)
    {
        if (currentIndex < lagDays)
            return false;

        var lagIndex = currentIndex - lagDays;
        if (lagIndex >= 0 && lagIndex < data.Count)
        {
            var expectedDate = data[currentIndex].Date.AddDays(-lagDays);
            if ((data[lagIndex].Date - expectedDate).TotalDays < 1)
            {
                return data[lagIndex].IsPromotion;
            }
        }

        return false;
    }

    /// <summary>
    /// Calculates rolling mean over N-day window.
    /// </summary>
    private static float? CalculateRollingMean(List<TimeSeriesDataPoint> data, int currentIndex, int windowDays)
    {
        if (currentIndex < windowDays - 1)
            return null; // Not enough history

        var startIndex = Math.Max(0, currentIndex - windowDays + 1);
        var windowData = data.Skip(startIndex).Take(windowDays).ToList();

        if (windowData.Count < windowDays)
            return null;

        return (float)windowData.Average(d => d.Demand);
    }

    /// <summary>
    /// Calculates rolling standard deviation over N-day window.
    /// </summary>
    private static float? CalculateRollingStd(List<TimeSeriesDataPoint> data, int currentIndex, int windowDays)
    {
        if (currentIndex < windowDays - 1)
            return null;

        var startIndex = Math.Max(0, currentIndex - windowDays + 1);
        var windowData = data.Skip(startIndex).Take(windowDays).Select(d => (double)d.Demand).ToList();

        if (windowData.Count < windowDays)
            return null;

        var mean = windowData.Average();
        var variance = windowData.Select(d => Math.Pow(d - mean, 2)).Average();
        return (float)Math.Sqrt(variance);
    }

    /// <summary>
    /// Gets quarter (1-4) from date.
    /// </summary>
    private static int GetQuarter(DateTime date)
    {
        return (date.Month - 1) / 3 + 1;
    }

    /// <summary>
    /// Gets week of year (ISO 8601).
    /// </summary>
    private static int GetWeekOfYear(DateTime date)
    {
        var calendar = CultureInfo.CurrentCulture.Calendar;
        return calendar.GetWeekOfYear(date, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
    }

    /// <summary>
    /// Checks if month is quarter start (Jan, Apr, Jul, Oct).
    /// </summary>
    private static bool IsQuarterStartMonth(DateTime date)
    {
        return date.Month is 1 or 4 or 7 or 10;
    }

    /// <summary>
    /// Checks if month is quarter end (Mar, Jun, Sep, Dec).
    /// </summary>
    private static bool IsQuarterEndMonth(DateTime date)
    {
        return date.Month is 3 or 6 or 9 or 12;
    }

    /// <summary>
    /// Loads standard US holidays for 2024-2026.
    /// In production, this should be configurable per region.
    /// </summary>
    private static HashSet<DateTime> LoadHolidays()
    {
        return new HashSet<DateTime>
        {
            // 2024 holidays
            new DateTime(2024, 1, 1),   // New Year's Day
            new DateTime(2024, 7, 4),   // Independence Day
            new DateTime(2024, 11, 28), // Thanksgiving
            new DateTime(2024, 12, 25), // Christmas

            // 2025 holidays
            new DateTime(2025, 1, 1),   // New Year's Day
            new DateTime(2025, 7, 4),   // Independence Day
            new DateTime(2025, 11, 27), // Thanksgiving
            new DateTime(2025, 12, 25), // Christmas

            // 2026 holidays
            new DateTime(2026, 1, 1),   // New Year's Day
            new DateTime(2026, 7, 4),   // Independence Day
            new DateTime(2026, 11, 26), // Thanksgiving
            new DateTime(2026, 12, 25), // Christmas
        };
    }
}
