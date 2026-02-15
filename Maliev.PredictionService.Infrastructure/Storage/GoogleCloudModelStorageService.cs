using Google.Cloud.Storage.V1;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Maliev.PredictionService.Infrastructure.Storage;

/// <summary>
/// Google Cloud Storage implementation for model storage with sandboxed access.
/// Uses service account authentication with limited bucket permissions.
/// </summary>
public class GoogleCloudModelStorageService : IModelStorageService
{
    private readonly StorageClient _storageClient;
    private readonly ILogger<GoogleCloudModelStorageService> _logger;
    private readonly GoogleCloudStorageOptions _options;

    public GoogleCloudModelStorageService(
        StorageClient storageClient,
        IOptions<GoogleCloudStorageOptions> options,
        ILogger<GoogleCloudModelStorageService> logger)
    {
        _storageClient = storageClient ?? throw new ArgumentNullException(nameof(storageClient));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        if (string.IsNullOrWhiteSpace(_options.BucketName))
        {
            throw new InvalidOperationException("GoogleCloudStorage:BucketName configuration is required.");
        }
    }

    public async Task<string> UploadModelAsync(string modelPath, Guid modelId, string modelType, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(modelPath))
        {
            throw new FileNotFoundException($"Model file not found: {modelPath}");
        }

        var objectName = GetObjectName(modelId, modelType);
        var contentType = "application/octet-stream";

        _logger.LogInformation(
            "Uploading model to GCS. Bucket: {Bucket}, Object: {Object}, ModelId: {ModelId}, ModelType: {ModelType}",
            _options.BucketName,
            objectName,
            modelId,
            modelType);

        try
        {
            await using var fileStream = File.OpenRead(modelPath);
            var uploadedObject = await _storageClient.UploadObjectAsync(
                _options.BucketName,
                objectName,
                contentType,
                fileStream,
                cancellationToken: cancellationToken);

            var storageUri = $"gs://{_options.BucketName}/{objectName}";

            _logger.LogInformation(
                "Model uploaded successfully. StorageUri: {StorageUri}, Size: {Size} bytes",
                storageUri,
                uploadedObject.Size);

            return storageUri;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to upload model to GCS. Bucket: {Bucket}, Object: {Object}, ModelId: {ModelId}",
                _options.BucketName,
                objectName,
                modelId);
            throw;
        }
    }

    public async Task<string> DownloadModelAsync(Guid modelId, string modelType, CancellationToken cancellationToken = default)
    {
        var objectName = GetObjectName(modelId, modelType);
        var tempPath = Path.Combine(Path.GetTempPath(), $"{modelId}.zip");

        _logger.LogInformation(
            "Downloading model from GCS. Bucket: {Bucket}, Object: {Object}, TempPath: {TempPath}",
            _options.BucketName,
            objectName,
            tempPath);

        try
        {
            await using var fileStream = File.Create(tempPath);
            await _storageClient.DownloadObjectAsync(
                _options.BucketName,
                objectName,
                fileStream,
                cancellationToken: cancellationToken);

            _logger.LogInformation(
                "Model downloaded successfully. ModelId: {ModelId}, LocalPath: {LocalPath}",
                modelId,
                tempPath);

            return tempPath;
        }
        catch (Google.GoogleApiException ex) when (ex.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning(
                "Model not found in GCS. Bucket: {Bucket}, Object: {Object}, ModelId: {ModelId}",
                _options.BucketName,
                objectName,
                modelId);
            throw new FileNotFoundException($"Model not found in storage: {modelId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to download model from GCS. Bucket: {Bucket}, Object: {Object}, ModelId: {ModelId}",
                _options.BucketName,
                objectName,
                modelId);
            throw;
        }
    }

    public async Task<bool> ModelExistsAsync(Guid modelId, string modelType, CancellationToken cancellationToken = default)
    {
        var objectName = GetObjectName(modelId, modelType);

        try
        {
            var obj = await _storageClient.GetObjectAsync(
                _options.BucketName,
                objectName,
                cancellationToken: cancellationToken);

            return obj != null;
        }
        catch (Google.GoogleApiException ex) when (ex.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error checking if model exists in GCS. Bucket: {Bucket}, Object: {Object}",
                _options.BucketName,
                objectName);
            throw;
        }
    }

    public async Task DeleteModelAsync(Guid modelId, string modelType, CancellationToken cancellationToken = default)
    {
        var objectName = GetObjectName(modelId, modelType);

        _logger.LogInformation(
            "Deleting model from GCS. Bucket: {Bucket}, Object: {Object}, ModelId: {ModelId}",
            _options.BucketName,
            objectName,
            modelId);

        try
        {
            await _storageClient.DeleteObjectAsync(
                _options.BucketName,
                objectName,
                cancellationToken: cancellationToken);

            _logger.LogInformation(
                "Model deleted successfully. ModelId: {ModelId}",
                modelId);
        }
        catch (Google.GoogleApiException ex) when (ex.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning(
                "Model not found for deletion. Bucket: {Bucket}, Object: {Object}, ModelId: {ModelId}",
                _options.BucketName,
                objectName,
                modelId);
            // Don't throw - deletion is idempotent
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to delete model from GCS. Bucket: {Bucket}, Object: {Object}, ModelId: {ModelId}",
                _options.BucketName,
                objectName,
                modelId);
            throw;
        }
    }

    public async Task<IReadOnlyList<Guid>> ListModelsAsync(string modelType, CancellationToken cancellationToken = default)
    {
        var prefix = $"models/{modelType}/";
        var modelIds = new List<Guid>();

        _logger.LogInformation(
            "Listing models in GCS. Bucket: {Bucket}, Prefix: {Prefix}",
            _options.BucketName,
            prefix);

        try
        {
            var objects = _storageClient.ListObjectsAsync(_options.BucketName, prefix);

            await foreach (var obj in objects.WithCancellation(cancellationToken))
            {
                // Object name format: models/{modelType}/{modelId}.zip
                var fileName = Path.GetFileNameWithoutExtension(obj.Name);
                if (Guid.TryParse(fileName, out var modelId))
                {
                    modelIds.Add(modelId);
                }
            }

            _logger.LogInformation(
                "Listed {Count} models. ModelType: {ModelType}",
                modelIds.Count,
                modelType);

            return modelIds.AsReadOnly();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to list models from GCS. Bucket: {Bucket}, Prefix: {Prefix}",
                _options.BucketName,
                prefix);
            throw;
        }
    }

    /// <summary>
    /// Constructs the GCS object name for a model.
    /// Format: models/{modelType}/{modelId}.zip
    /// </summary>
    private static string GetObjectName(Guid modelId, string modelType)
    {
        return $"models/{modelType}/{modelId}.zip";
    }
}

/// <summary>
/// Configuration options for Google Cloud Storage.
/// </summary>
public class GoogleCloudStorageOptions
{
    /// <summary>
    /// GCS bucket name for model storage.
    /// </summary>
    public string BucketName { get; set; } = string.Empty;

    /// <summary>
    /// Optional: Path to service account JSON key file.
    /// If not provided, Application Default Credentials will be used.
    /// </summary>
    public string? ServiceAccountKeyPath { get; set; }
}
