using Maliev.PredictionService.Domain.Enums;

namespace Maliev.PredictionService.Infrastructure.BackgroundServices;

/// <summary>
/// Abstraction for enqueuing model retraining jobs.
/// Allows consumers and controllers to trigger retraining without depending on the concrete background service.
/// </summary>
public interface IModelRetrainingService
{
    Task EnqueueRetrainingJobAsync(Guid modelId, ModelType modelType, CancellationToken cancellationToken = default);
}
