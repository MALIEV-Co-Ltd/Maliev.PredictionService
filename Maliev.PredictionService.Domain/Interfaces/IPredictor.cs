namespace Maliev.PredictionService.Domain.Interfaces;

/// <summary>
/// Generic interface for ML prediction engines
/// </summary>
/// <typeparam name="TInput">Input feature type</typeparam>
/// <typeparam name="TOutput">Prediction output type</typeparam>
public interface IPredictor<TInput, TOutput>
    where TInput : class
    where TOutput : class
{
    /// <summary>
    /// Makes a prediction based on input features
    /// </summary>
    Task<TOutput> PredictAsync(TInput input, CancellationToken cancellationToken = default);

    /// <summary>
    /// Makes predictions for a batch of inputs
    /// </summary>
    Task<List<TOutput>> PredictBatchAsync(List<TInput> inputs, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads a specific model version
    /// </summary>
    Task LoadModelAsync(string modelPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns whether a model is currently loaded and ready for predictions
    /// </summary>
    bool IsModelLoaded { get; }
}
