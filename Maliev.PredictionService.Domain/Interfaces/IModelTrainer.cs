using Maliev.PredictionService.Domain.Entities;

namespace Maliev.PredictionService.Domain.Interfaces;

/// <summary>
/// Interface for ML model training engines
/// </summary>
public interface IModelTrainer
{
    /// <summary>
    /// Trains a new model using the provided training dataset
    /// </summary>
    /// <param name="trainingDataset">Dataset to use for training</param>
    /// <param name="hyperparameters">Optional hyperparameters for the training algorithm</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Trained model entity with performance metrics</returns>
    Task<MLModel> TrainModelAsync(
        TrainingDataset trainingDataset,
        Dictionary<string, object>? hyperparameters = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a trained model using a holdout test set
    /// </summary>
    /// <param name="model">Model to validate</param>
    /// <param name="validationDataset">Dataset to use for validation</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation results with performance metrics</returns>
    Task<Dictionary<string, object>> ValidateModelAsync(
        MLModel model,
        TrainingDataset validationDataset,
        CancellationToken cancellationToken = default);
}
