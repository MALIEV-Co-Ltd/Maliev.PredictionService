using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Maliev.PredictionService.Infrastructure.Storage;

/// <summary>
/// Model storage implementation that uses maliev.uploadservice microservice
/// for all file upload/download operations.
/// </summary>
public class UploadServiceModelStorageService : IModelStorageService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<UploadServiceModelStorageService> _logger;
    private readonly UploadServiceOptions _options;

    public UploadServiceModelStorageService(
        HttpClient httpClient,
        IOptions<UploadServiceOptions> options,
        ILogger<UploadServiceModelStorageService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        if (string.IsNullOrWhiteSpace(_options.BaseUrl))
        {
            throw new InvalidOperationException("UploadService:BaseUrl configuration is required.");
        }
    }

    public async Task<string> UploadModelAsync(string modelPath, Guid modelId, string modelType, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(modelPath))
        {
            throw new FileNotFoundException($"Model file not found: {modelPath}");
        }

        var fileName = $"{modelId}.zip";
        var storagePath = GetStoragePath(modelType, fileName);

        _logger.LogInformation(
            "Uploading model via UploadService. Path: {Path}, ModelId: {ModelId}, ModelType: {ModelType}",
            storagePath,
            modelId,
            modelType);

        try
        {
            using var fileStream = File.OpenRead(modelPath);
            using var content = new MultipartFormDataContent();

            // Add file content
            var fileContent = new StreamContent(fileStream);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            content.Add(fileContent, "file", fileName);

            // Add metadata
            content.Add(new StringContent(storagePath), "path");
            content.Add(new StringContent(modelType), "category");
            content.Add(new StringContent("ml-model"), "type");

            var response = await _httpClient.PostAsync("/api/v1/files/upload", content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<UploadResponse>(cancellationToken: cancellationToken);

            if (result == null || string.IsNullOrEmpty(result.Url))
            {
                throw new InvalidOperationException("Upload service returned invalid response.");
            }

            _logger.LogInformation(
                "Model uploaded successfully. StorageUri: {StorageUri}, Size: {Size} bytes",
                result.Url,
                new FileInfo(modelPath).Length);

            return result.Url;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex,
                "Failed to upload model to UploadService. Path: {Path}, ModelId: {ModelId}, StatusCode: {StatusCode}",
                storagePath,
                modelId,
                ex.StatusCode);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to upload model to UploadService. Path: {Path}, ModelId: {ModelId}",
                storagePath,
                modelId);
            throw;
        }
    }

    public async Task<string> DownloadModelAsync(Guid modelId, string modelType, CancellationToken cancellationToken = default)
    {
        var fileName = $"{modelId}.zip";
        var storagePath = GetStoragePath(modelType, fileName);
        var tempPath = Path.Combine(Path.GetTempPath(), fileName);

        _logger.LogInformation(
            "Downloading model from UploadService. Path: {Path}, TempPath: {TempPath}",
            storagePath,
            tempPath);

        try
        {
            // URL encode the storage path
            var encodedPath = Uri.EscapeDataString(storagePath);
            var response = await _httpClient.GetAsync($"/api/v1/files/download?path={encodedPath}", cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning(
                    "Model not found in UploadService. Path: {Path}, ModelId: {ModelId}",
                    storagePath,
                    modelId);
                throw new FileNotFoundException($"Model not found in storage: {modelId}");
            }

            response.EnsureSuccessStatusCode();

            await using var fileStream = File.Create(tempPath);
            await response.Content.CopyToAsync(fileStream, cancellationToken);

            _logger.LogInformation(
                "Model downloaded successfully. ModelId: {ModelId}, LocalPath: {LocalPath}",
                modelId,
                tempPath);

            return tempPath;
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning(
                "Model not found in UploadService. Path: {Path}, ModelId: {ModelId}",
                storagePath,
                modelId);
            throw new FileNotFoundException($"Model not found in storage: {modelId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to download model from UploadService. Path: {Path}, ModelId: {ModelId}",
                storagePath,
                modelId);
            throw;
        }
    }

    public async Task<bool> ModelExistsAsync(Guid modelId, string modelType, CancellationToken cancellationToken = default)
    {
        var fileName = $"{modelId}.zip";
        var storagePath = GetStoragePath(modelType, fileName);

        try
        {
            var encodedPath = Uri.EscapeDataString(storagePath);
            var response = await _httpClient.GetAsync($"/api/v1/files/exists?path={encodedPath}", cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return false;
            }

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<ExistsResponse>(cancellationToken: cancellationToken);
            return result?.Exists ?? false;
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error checking if model exists in UploadService. Path: {Path}",
                storagePath);
            throw;
        }
    }

    public async Task DeleteModelAsync(Guid modelId, string modelType, CancellationToken cancellationToken = default)
    {
        var fileName = $"{modelId}.zip";
        var storagePath = GetStoragePath(modelType, fileName);

        _logger.LogInformation(
            "Deleting model from UploadService. Path: {Path}, ModelId: {ModelId}",
            storagePath,
            modelId);

        try
        {
            var encodedPath = Uri.EscapeDataString(storagePath);
            var response = await _httpClient.DeleteAsync($"/api/v1/files?path={encodedPath}", cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning(
                    "Model not found for deletion. Path: {Path}, ModelId: {ModelId}",
                    storagePath,
                    modelId);
                // Don't throw - deletion is idempotent
                return;
            }

            response.EnsureSuccessStatusCode();

            _logger.LogInformation(
                "Model deleted successfully. ModelId: {ModelId}",
                modelId);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning(
                "Model not found for deletion. Path: {Path}, ModelId: {ModelId}",
                storagePath,
                modelId);
            // Don't throw - deletion is idempotent
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to delete model from UploadService. Path: {Path}, ModelId: {ModelId}",
                storagePath,
                modelId);
            throw;
        }
    }

    public async Task<IReadOnlyList<Guid>> ListModelsAsync(string modelType, CancellationToken cancellationToken = default)
    {
        var prefix = $"models/{modelType}/";
        var modelIds = new List<Guid>();

        _logger.LogInformation(
            "Listing models in UploadService. Prefix: {Prefix}",
            prefix);

        try
        {
            var encodedPrefix = Uri.EscapeDataString(prefix);
            var response = await _httpClient.GetAsync($"/api/v1/files/list?prefix={encodedPrefix}", cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<ListResponse>(cancellationToken: cancellationToken);

            if (result?.Files != null)
            {
                foreach (var file in result.Files)
                {
                    // File path format: models/{modelType}/{modelId}.zip
                    var fileName = Path.GetFileNameWithoutExtension(file.Path);
                    if (Guid.TryParse(fileName, out var modelId))
                    {
                        modelIds.Add(modelId);
                    }
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
                "Failed to list models from UploadService. Prefix: {Prefix}",
                prefix);
            throw;
        }
    }

    /// <summary>
    /// Constructs the storage path for a model.
    /// Format: models/{modelType}/{fileName}
    /// </summary>
    private static string GetStoragePath(string modelType, string fileName)
    {
        return $"models/{modelType}/{fileName}";
    }
}

/// <summary>
/// Configuration options for UploadService.
/// </summary>
public class UploadServiceOptions
{
    /// <summary>
    /// Base URL for the UploadService API.
    /// Example: https://upload.maliev.com
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Optional: API key for authenticating with UploadService.
    /// If not provided, assumes authentication is handled by infrastructure (e.g., service mesh).
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Request timeout in seconds. Default is 300 (5 minutes).
    /// </summary>
    public int TimeoutSeconds { get; set; } = 300;
}

/// <summary>
/// Response model for file upload operations.
/// </summary>
internal class UploadResponse
{
    public string Url { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public long Size { get; set; }
}

/// <summary>
/// Response model for file exists check.
/// </summary>
internal class ExistsResponse
{
    public bool Exists { get; set; }
    public string? Path { get; set; }
}

/// <summary>
/// Response model for file listing operations.
/// </summary>
internal class ListResponse
{
    public List<FileInfo> Files { get; set; } = new();

    public class FileInfo
    {
        public string Path { get; set; } = string.Empty;
        public long Size { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
