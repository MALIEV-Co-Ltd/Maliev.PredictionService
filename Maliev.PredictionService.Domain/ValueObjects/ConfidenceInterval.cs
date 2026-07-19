namespace Maliev.PredictionService.Domain.ValueObjects;

/// <summary>
/// Confidence interval for prediction results
/// Immutable value object
/// </summary>
public record ConfidenceInterval
{
    /// <summary>
    /// Lower bound of the confidence interval
    /// </summary>
    public double Lower { get; init; }

    /// <summary>
    /// Upper bound of the confidence interval
    /// </summary>
    public double Upper { get; init; }

    /// <summary>
    /// Confidence level (e.g., 0.80 for 80%, 0.95 for 95%)
    /// Range: [0, 1]
    /// </summary>
    public double ConfidenceLevel { get; init; }

    /// <summary>
    /// Validates that the confidence interval is well-formed
    /// </summary>
    public bool IsValid()
    {
        return Lower <= Upper
            && ConfidenceLevel > 0
            && ConfidenceLevel <= 1;
    }
}
