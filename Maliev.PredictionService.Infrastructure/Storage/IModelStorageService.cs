namespace Maliev.PredictionService.Infrastructure.Storage;

/// <summary>
/// Service for storing and retrieving trained ML models.
/// Supports multiple backends (local file system, Google Cloud Storage, etc.).
/// </summary>
public interface IModelStorageService
{
    /// <summary>
    /// Uploads a trained model to storage.
    /// </summary>
    /// <param name="modelPath">Local path to the model file.</param>
    /// <param name="modelId">Unique identifier for the model.</param>
    /// <param name="modelType">Type of the model (e.g., "demand-forecast", "print-time").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The storage URI where the model was uploaded.</returns>
    Task<string> UploadModelAsync(string modelPath, Guid modelId, string modelType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads a trained model from storage to a local temporary path.
    /// </summary>
    /// <param name="modelId">Unique identifier for the model.</param>
    /// <param name="modelType">Type of the model (e.g., "demand-forecast", "print-time").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The local path where the model was downloaded.</returns>
    Task<string> DownloadModelAsync(Guid modelId, string modelType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a model exists in storage.
    /// </summary>
    /// <param name="modelId">Unique identifier for the model.</param>
    /// <param name="modelType">Type of the model (e.g., "demand-forecast", "print-time").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the model exists, false otherwise.</returns>
    Task<bool> ModelExistsAsync(Guid modelId, string modelType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a model from storage.
    /// </summary>
    /// <param name="modelId">Unique identifier for the model.</param>
    /// <param name="modelType">Type of the model (e.g., "demand-forecast", "print-time").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeleteModelAsync(Guid modelId, string modelType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all models of a specific type in storage.
    /// </summary>
    /// <param name="modelType">Type of the model (e.g., "demand-forecast", "print-time").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of model IDs.</returns>
    Task<IReadOnlyList<Guid>> ListModelsAsync(string modelType, CancellationToken cancellationToken = default);
}
