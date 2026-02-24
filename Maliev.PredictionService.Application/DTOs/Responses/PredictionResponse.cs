namespace Maliev.PredictionService.Application.DTOs.Responses;

/// <summary>
/// Generic prediction response DTO.
/// Used for all prediction types (print time, demand forecast, price optimization, etc.).
/// </summary>
public record PredictionResponse
{
    /// <summary>
    /// Predicted value (interpretation depends on prediction type).
    /// - Print Time: minutes
    /// - Demand Forecast: unit count
    /// - Price Optimization: optimal price in USD
    /// </summary>
    public required float PredictedValue { get; init; }

    /// <summary>
    /// Unit of measurement for the predicted value.
    /// Examples: "minutes", "units", "USD", "percentage".
    /// </summary>
    public required string Unit { get; init; }

    /// <summary>
    /// 95% confidence interval lower bound.
    /// </summary>
    public required float ConfidenceLower { get; init; }

    /// <summary>
    /// 95% confidence interval upper bound.
    /// </summary>
    public required float ConfidenceUpper { get; init; }

    /// <summary>
    /// Human-readable explanation of the prediction and key contributing factors.
    /// Example: "Estimated print time: 2h 15m. Key factors: Volume: 12500 mm³, Layer Count: 450, Complex geometry (score: 75/100)."
    /// </summary>
    public required string Explanation { get; init; }

    /// <summary>
    /// Model version used for this prediction (semantic version).
    /// Example: "1.2.0"
    /// </summary>
    public required string ModelVersion { get; init; }

    /// <summary>
    /// Cache status indicator.
    /// - "hit": Prediction served from cache
    /// - "miss": Fresh prediction computed
    /// - "bypass": Cache disabled for this request
    /// </summary>
    public required string CacheStatus { get; init; }

    /// <summary>
    /// Timestamp when prediction was generated (UTC).
    /// </summary>
    public required DateTime Timestamp { get; init; }

    /// <summary>
    /// Additional metadata specific to the prediction type (optional).
    /// Example for print time: { "geometry_volume_mm3": 12500, "support_percentage": 15.5, "complexity_score": 75 }
    /// </summary>
    public Dictionary<string, object>? Metadata { get; init; }
}
