using Maliev.PredictionService.Application.DTOs.ML;

namespace Maliev.PredictionService.Application.Interfaces;

/// <summary>
/// Interface for demand forecasting predictor
/// </summary>
public interface IDemandForecaster
{
    /// <summary>
    /// Predict demand for the specified horizon with anomaly detection.
    /// </summary>
    Task<DemandPredictionResult> PredictAsync(DemandPredictionInput input, string modelPath, CancellationToken cancellationToken = default);
}
