namespace Maliev.PredictionService.Domain.Enums;

/// <summary>
/// Status of a prediction request
/// </summary>
public enum PredictionStatus
{
    /// <summary>
    /// Prediction completed successfully with model inference
    /// </summary>
    Success,

    /// <summary>
    /// Prediction failed due to error
    /// </summary>
    Failure,

    /// <summary>
    /// Prediction result served from cache (no model inference)
    /// </summary>
    CachedHit
}
