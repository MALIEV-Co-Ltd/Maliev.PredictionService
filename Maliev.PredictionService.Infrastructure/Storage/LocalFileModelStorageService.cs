using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Maliev.PredictionService.Infrastructure.Storage;

/// <summary>
/// Local file system implementation for model storage.
/// Used for development and testing environments.
/// </summary>
public class LocalFileModelStorageService : IModelStorageService
{
    private readonly ILogger<LocalFileModelStorageService> _logger;
    private readonly LocalFileStorageOptions _options;

    public LocalFileModelStorageService(
        IOptions<LocalFileStorageOptions> options,
        ILogger<LocalFileModelStorageService> logger)
    {
        _options = options?.Value ?? new LocalFileStorageOptions();
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Use default path if not configured
        if (string.IsNullOrWhiteSpace(_options.BasePath))
        {
            _options.BasePath = Path.Combine(Path.GetTempPath(), "prediction-service-models");
            _logger.LogInformation(
                "LocalFileStorage:BasePath not configured, using default: {BasePath}",
                _options.BasePath);
        }

        // Ensure base directory exists
        Directory.CreateDirectory(_options.BasePath);
    }

    public Task<string> UploadModelAsync(string modelPath, Guid modelId, string modelType, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(modelPath))
        {
            throw new FileNotFoundException($"Model file not found: {modelPath}");
        }

        var destinationPath = GetModelPath(modelId, modelType);
        var destinationDir = Path.GetDirectoryName(destinationPath);

        if (!string.IsNullOrEmpty(destinationDir))
        {
            Directory.CreateDirectory(destinationDir);
        }

        _logger.LogInformation(
            "Copying model to local storage. Source: {Source}, Destination: {Destination}, ModelId: {ModelId}",
            modelPath,
            destinationPath,
            modelId);

        File.Copy(modelPath, destinationPath, overwrite: true);

        _logger.LogInformation(
            "Model uploaded successfully. ModelId: {ModelId}, Path: {Path}",
            modelId,
            destinationPath);

        return Task.FromResult($"file://{destinationPath}");
    }

    public Task<string> DownloadModelAsync(Guid modelId, string modelType, CancellationToken cancellationToken = default)
    {
        var modelPath = GetModelPath(modelId, modelType);

        if (!File.Exists(modelPath))
        {
            _logger.LogWarning(
                "Model not found in local storage. Path: {Path}, ModelId: {ModelId}",
                modelPath,
                modelId);
            throw new FileNotFoundException($"Model not found: {modelId}");
        }

        _logger.LogInformation(
            "Model retrieved from local storage. ModelId: {ModelId}, Path: {Path}",
            modelId,
            modelPath);

        // For local storage, we return the path directly (no copy needed)
        return Task.FromResult(modelPath);
    }

    public Task<bool> ModelExistsAsync(Guid modelId, string modelType, CancellationToken cancellationToken = default)
    {
        var modelPath = GetModelPath(modelId, modelType);
        var exists = File.Exists(modelPath);

        _logger.LogDebug(
            "Checked model existence. ModelId: {ModelId}, Exists: {Exists}",
            modelId,
            exists);

        return Task.FromResult(exists);
    }

    public Task DeleteModelAsync(Guid modelId, string modelType, CancellationToken cancellationToken = default)
    {
        var modelPath = GetModelPath(modelId, modelType);

        if (File.Exists(modelPath))
        {
            _logger.LogInformation(
                "Deleting model from local storage. Path: {Path}, ModelId: {ModelId}",
                modelPath,
                modelId);

            File.Delete(modelPath);

            _logger.LogInformation(
                "Model deleted successfully. ModelId: {ModelId}",
                modelId);
        }
        else
        {
            _logger.LogWarning(
                "Model not found for deletion. Path: {Path}, ModelId: {ModelId}",
                modelPath,
                modelId);
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<Guid>> ListModelsAsync(string modelType, CancellationToken cancellationToken = default)
    {
        var modelDir = GetModelTypeDirectory(modelType);
        var modelIds = new List<Guid>();

        if (Directory.Exists(modelDir))
        {
            var files = Directory.GetFiles(modelDir, "*.zip");

            foreach (var file in files)
            {
                var fileName = Path.GetFileNameWithoutExtension(file);
                if (Guid.TryParse(fileName, out var modelId))
                {
                    modelIds.Add(modelId);
                }
            }
        }

        _logger.LogInformation(
            "Listed {Count} models from local storage. ModelType: {ModelType}",
            modelIds.Count,
            modelType);

        return Task.FromResult<IReadOnlyList<Guid>>(modelIds.AsReadOnly());
    }

    /// <summary>
    /// Gets the full path for a model file.
    /// Format: {BasePath}/models/{modelType}/{modelId}.zip
    /// </summary>
    private string GetModelPath(Guid modelId, string modelType)
    {
        return Path.Combine(_options.BasePath, "models", modelType, $"{modelId}.zip");
    }

    /// <summary>
    /// Gets the directory path for a specific model type.
    /// </summary>
    private string GetModelTypeDirectory(string modelType)
    {
        return Path.Combine(_options.BasePath, "models", modelType);
    }
}

/// <summary>
/// Configuration options for local file storage.
/// </summary>
public class LocalFileStorageOptions
{
    /// <summary>
    /// Base path for local model storage.
    /// </summary>
    public string BasePath { get; set; } = string.Empty;
}
