using Maliev.PredictionService.Domain.Entities;
using Maliev.PredictionService.Domain.Enums;

namespace Maliev.PredictionService.Domain.Repositories;

/// <summary>
/// Repository interface for MLModel entity operations.
/// </summary>
public interface IModelRepository
{
    /// <summary>
    /// Gets the active model for a specific model type.
    /// </summary>
    Task<MLModel?> GetActiveModelByTypeAsync(ModelType modelType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a model by its unique identifier.
    /// </summary>
    Task<MLModel?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all models for a specific model type.
    /// </summary>
    Task<List<MLModel>> GetByTypeAsync(ModelType modelType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new model to the repository.
    /// </summary>
    Task<MLModel> AddAsync(MLModel model, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing model.
    /// </summary>
    Task UpdateAsync(MLModel model, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a model (soft delete by setting status to Archived).
    /// </summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
