namespace Maliev.PredictionService.Domain.ValueObjects;

/// <summary>
/// Contribution of a feature to a prediction (for explainability)
/// Immutable value object
/// </summary>
public record FeatureContribution
{
    /// <summary>
    /// Name of the feature (e.g., "Volume", "CustomerLifetime")
    /// </summary>
    public required string FeatureName { get; init; }

    /// <summary>
    /// Impact weight of this feature on the prediction
    /// Range: [0, 1], normalized across all features
    /// </summary>
    public required double ImpactWeight { get; init; }

    /// <summary>
    /// Trend direction of the feature's influence
    /// </summary>
    public required TrendDirection TrendDirection { get; init; }

    /// <summary>
    /// Human-readable explanation of the feature's contribution
    /// Example: "Volume is in top 10% of historical data"
    /// </summary>
    public string? Explanation { get; init; }
}

/// <summary>
/// Direction of a feature's influence on the prediction
/// </summary>
public enum TrendDirection
{
    /// <summary>
    /// Feature increases the predicted value
    /// </summary>
    Positive,

    /// <summary>
    /// Feature decreases the predicted value
    /// </summary>
    Negative,

    /// <summary>
    /// Feature has neutral or negligible impact
    /// </summary>
    Neutral
}
