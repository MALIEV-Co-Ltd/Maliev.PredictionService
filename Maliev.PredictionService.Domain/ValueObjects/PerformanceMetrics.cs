namespace Maliev.PredictionService.Domain.ValueObjects;

/// <summary>
/// Performance metrics for ML models
/// Immutable value object
/// </summary>
public record PerformanceMetrics
{
    /// <summary>
    /// R-squared (coefficient of determination) for regression models
    /// Range: [0, 1], higher is better
    /// </summary>
    public double? RSquared { get; init; }

    /// <summary>
    /// Mean Absolute Error for regression models
    /// Lower is better
    /// </summary>
    public double? MAE { get; init; }

    /// <summary>
    /// Root Mean Squared Error for regression models
    /// Lower is better
    /// </summary>
    public double? RMSE { get; init; }

    /// <summary>
    /// Mean Absolute Percentage Error for forecasting models
    /// Range: [0, 100], lower is better
    /// </summary>
    public double? MAPE { get; init; }

    /// <summary>
    /// Precision for classification models
    /// Range: [0, 1], higher is better
    /// </summary>
    public double? Precision { get; init; }

    /// <summary>
    /// Recall for classification models
    /// Range: [0, 1], higher is better
    /// </summary>
    public double? Recall { get; init; }

    /// <summary>
    /// F1 Score for classification models
    /// Range: [0, 1], higher is better
    /// </summary>
    public double? F1Score { get; init; }

    /// <summary>
    /// Area Under ROC Curve for classification models
    /// Range: [0, 1], higher is better
    /// </summary>
    public double? AUC { get; init; }
}
