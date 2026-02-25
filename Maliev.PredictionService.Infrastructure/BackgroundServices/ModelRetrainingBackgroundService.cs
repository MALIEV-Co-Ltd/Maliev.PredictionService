using Maliev.PredictionService.Domain.Enums;
using Maliev.PredictionService.Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading.Channels;

namespace Maliev.PredictionService.Infrastructure.BackgroundServices;

/// <summary>
/// Background service for scheduled model retraining.
/// Uses System.Threading.Channels for job queue management.
/// </summary>
public class ModelRetrainingBackgroundService : BackgroundService
{
    private readonly ILogger<ModelRetrainingBackgroundService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly Channel<RetrainingJob> _jobQueue;
    private readonly TimeSpan _retrainingCheckInterval = TimeSpan.FromHours(6); // Check every 6 hours

    public ModelRetrainingBackgroundService(
        ILogger<ModelRetrainingBackgroundService> _logger,
        IServiceProvider serviceProvider)
    {
        this._logger = _logger ?? throw new ArgumentNullException(nameof(_logger));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

        // Create unbounded channel for job queue
        _jobQueue = Channel.CreateUnbounded<RetrainingJob>(new UnboundedChannelOptions
        {
            SingleReader = true, // Only one background task processes jobs
            SingleWriter = false // Multiple sources can enqueue jobs
        });
    }

    /// <summary>
    /// Enqueues a model retraining job.
    /// Can be called from controllers, events, or other services.
    /// </summary>
    public async Task EnqueueRetrainingJobAsync(Guid modelId, ModelType modelType, CancellationToken cancellationToken = default)
    {
        var job = new RetrainingJob
        {
            ModelId = modelId,
            ModelType = modelType,
            EnqueuedAt = DateTime.UtcNow
        };

        await _jobQueue.Writer.WriteAsync(job, cancellationToken);

        _logger.LogInformation(
            "Model retraining job enqueued. ModelId: {ModelId}, ModelType: {ModelType}",
            modelId,
            modelType);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Model retraining background service started");

        // Start job processor task
        var processorTask = ProcessJobQueueAsync(stoppingToken);

        // Start scheduled check task
        var schedulerTask = ScheduledRetrainingCheckAsync(stoppingToken);

        // Wait for both tasks to complete
        await Task.WhenAll(processorTask, schedulerTask);

        _logger.LogInformation("Model retraining background service stopped");
    }

    /// <summary>
    /// Processes jobs from the queue.
    /// </summary>
    private async Task ProcessJobQueueAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Job queue processor started");

        try
        {
            await foreach (var job in _jobQueue.Reader.ReadAllAsync(stoppingToken))
            {
                try
                {
                    _logger.LogInformation(
                        "Processing retraining job. ModelId: {ModelId}, ModelType: {ModelType}, Queued: {QueuedAt}",
                        job.ModelId,
                        job.ModelType,
                        job.EnqueuedAt);

                    // Process the job using scoped services
                    using var scope = _serviceProvider.CreateScope();
                    var modelRepository = scope.ServiceProvider.GetRequiredService<IModelRepository>();

                    var model = await modelRepository.GetByIdAsync(job.ModelId, stoppingToken);

                    if (model == null)
                    {
                        _logger.LogWarning(
                            "Model not found for retraining. ModelId: {ModelId}",
                            job.ModelId);
                        continue;
                    }

                    var datasetRepository = scope.ServiceProvider.GetRequiredService<Maliev.PredictionService.Infrastructure.Persistence.Repositories.TrainingDatasetRepository>();
                    var dataset = (await datasetRepository.GetByModelTypeAsync(job.ModelType, 1, stoppingToken)).FirstOrDefault();

                    if (dataset != null)
                    {
                        var trainer = scope.ServiceProvider.GetRequiredService<Maliev.PredictionService.Domain.Interfaces.IModelTrainer>();
                        var updatedModel = await trainer.TrainModelAsync(dataset, null, stoppingToken);

                        model.PerformanceMetrics = updatedModel.PerformanceMetrics;
                        model.Status = ModelStatus.Active;
                        model.TrainingDate = DateTime.UtcNow;
                        model.DeploymentDate = DateTime.UtcNow;

                        await modelRepository.UpdateAsync(model, stoppingToken);

                        var storageService = scope.ServiceProvider.GetRequiredService<Maliev.PredictionService.Infrastructure.Storage.IModelStorageService>();
                        // Save dummy file for local storage mock
                        var tempPath = System.IO.Path.GetTempFileName();
                        System.IO.File.WriteAllText(tempPath, "dummy-model");
                        await storageService.UploadModelAsync(tempPath, model.Id, model.ModelType.ToString(), stoppingToken);
                        System.IO.File.Delete(tempPath);
                    }
                    else
                    {
                        _logger.LogWarning("No training dataset found for ModelType: {ModelType}", job.ModelType);
                    }

                    _logger.LogInformation(
                        "Model retraining job completed. ModelId: {ModelId}, Duration: {Duration}ms",
                        job.ModelId,
                        (DateTime.UtcNow - job.EnqueuedAt).TotalMilliseconds);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Error processing retraining job. ModelId: {ModelId}, ModelType: {ModelType}",
                        job.ModelId,
                        job.ModelType);

                    // Job failed - could implement retry logic here if needed
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Job queue processor cancelled");
        }
    }

    /// <summary>
    /// Scheduled check for models that need retraining.
    /// Runs periodically to check model staleness and performance degradation.
    /// </summary>
    private async Task ScheduledRetrainingCheckAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Scheduled retraining check started");

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("Running scheduled model retraining check");

                    using var scope = _serviceProvider.CreateScope();
                    var modelRepository = scope.ServiceProvider.GetRequiredService<IModelRepository>();

                    // Find models that need retraining
                    // Criteria:
                    // 1. Model is older than 30 days
                    // 2. Model accuracy has degraded (would need metrics tracking)
                    // 3. New training data is available

                    var staleThreshold = DateTime.UtcNow.AddDays(-30);
                    var staleModels = await modelRepository.GetStaleModelsAsync(staleThreshold, stoppingToken);

                    foreach (var model in staleModels)
                    {
                        _logger.LogInformation(
                            "Found stale model. ModelId: {ModelId}, ModelType: {ModelType}, TrainingDate: {TrainingDate}",
                            model.Id,
                            model.ModelType,
                            model.TrainingDate);

                        // Enqueue retraining job
                        await EnqueueRetrainingJobAsync(model.Id, model.ModelType, stoppingToken);
                    }

                    _logger.LogInformation(
                        "Scheduled retraining check completed. Found {Count} stale models",
                        staleModels.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during scheduled retraining check");
                }

                // Wait for next check
                await Task.Delay(_retrainingCheckInterval, stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Scheduled retraining check cancelled");
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping model retraining background service");

        // Signal no more jobs will be written
        _jobQueue.Writer.Complete();

        await base.StopAsync(cancellationToken);
    }
}

/// <summary>
/// Represents a model retraining job.
/// </summary>
public record RetrainingJob
{
    public required Guid ModelId { get; init; }
    public required ModelType ModelType { get; init; }
    public required DateTime EnqueuedAt { get; init; }
}
