namespace Maliev.PredictionService.Application.DTOs.ML;

/// <summary>
/// Prediction input for demand forecasting with time-series data.
/// ML.NET input class - requires parameterless constructor.
/// </summary>
public class DemandInput
{
    public string ProductId { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public float Demand { get; set; }
    public bool IsPromotion { get; set; }
    public bool IsHoliday { get; set; }
}

/// <summary>
/// Forecast output with multi-horizon predictions and confidence bands.
/// ML.NET output class - requires parameterless constructor.
/// </summary>
public class DemandForecast
{
    public float ForecastedDemand { get; set; }
    public float LowerBoundConfidence { get; set; }
    public float UpperBoundConfidence { get; set; }
    public DateTime ForecastDate { get; set; }
    public bool IsAnomaly { get; set; }
    public float? AnomalyScore { get; set; }
}

/// <summary>
/// Prediction result containing forecasts for the requested horizon.
/// </summary>
public record DemandPredictionResult
{
    public required IReadOnlyList<DemandForecast> Forecasts { get; init; }
    public required int Horizon { get; init; }
    public required string Granularity { get; init; } // "daily" or "weekly"
    public required DateTime ForecastGeneratedAt { get; init; }
}

/// <summary>
/// Input for demand forecasting request.
/// </summary>
public record DemandPredictionInput
{
    public required string ProductId { get; init; }
    public required int Horizon { get; init; } // 7, 30, or 90 days
    public required string Granularity { get; init; } // "daily" or "weekly"
    public required DateTime BaselineDate { get; init; } // Last known demand date
    public required IReadOnlyList<DemandInput> HistoricalData { get; init; }
}
