using Maliev.PredictionService.Domain.Enums;

namespace Maliev.PredictionService.Infrastructure.BackgroundServices;

/// <summary>
/// No-op implementation of <see cref="IModelRetrainingService"/> used when background services are disabled (e.g. in tests).
/// </summary>
public sealed class NoOpModelRetrainingService : IModelRetrainingService
{
    public Task EnqueueRetrainingJobAsync(Guid modelId, ModelType modelType, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
