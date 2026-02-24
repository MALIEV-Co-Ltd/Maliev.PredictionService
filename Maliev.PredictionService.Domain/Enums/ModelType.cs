namespace Maliev.PredictionService.Domain.Enums;

/// <summary>
/// Types of ML models supported by the prediction service
/// </summary>
public enum ModelType
{
    /// <summary>
    /// 3D print time estimation model (regression)
    /// </summary>
    PrintTime,

    /// <summary>
    /// Sales demand forecasting model (time-series)
    /// </summary>
    DemandForecast,

    /// <summary>
    /// Dynamic pricing optimization model (regression + business logic)
    /// </summary>
    PriceOptimization,

    /// <summary>
    /// Customer churn prediction model (binary classification)
    /// </summary>
    ChurnPrediction,

    /// <summary>
    /// Material demand forecasting model (time-series)
    /// </summary>
    MaterialDemand,

    /// <summary>
    /// Production bottleneck detection model (regression)
    /// </summary>
    BottleneckDetection
}
