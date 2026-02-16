using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace Maliev.PredictionService.Infrastructure.Storage;

/// <summary>
/// Model storage implementation that uses Maliev.UploadService microservice
/// for all file upload/download operations.
///
/// Authentication: Service-to-service JWT tokens
/// Base Route: /upload/v1/
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
        var storagePath = GetStoragePath(modelType, modelId);

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
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/zip");
            content.Add(fileContent, "File", fileName);

            // Add form fields (matches UploadFileRequest)
            content.Add(new StringContent(storagePath), "Path");
            content.Add(new StringContent("PredictionService"), "ServiceName");
            content.Add(new StringContent("true"), "Overwrite"); // Allow overwriting for model updates

            // POST /upload/v1/uploads
            var response = await _httpClient.PostAsync("/upload/v1/uploads", content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<UploadResponse>(cancellationToken: cancellationToken);

            if (result == null || string.IsNullOrEmpty(result.UploadId))
            {
                throw new InvalidOperationException("Upload service returned invalid response.");
            }

            _logger.LogInformation(
                "Model uploaded successfully. UploadId: {UploadId}, StoragePath: {StoragePath}, Size: {Size} bytes",
                result.UploadId,
                result.StoragePath,
                result.FileSize);

            // Return storage path as URI for consistency with interface
            return $"upload://{result.UploadId}";
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
        var storagePath = GetStoragePath(modelType, modelId);
        var tempPath = Path.Combine(Path.GetTempPath(), "predictionservice", fileName);
        Directory.CreateDirectory(Path.GetDirectoryName(tempPath)!);

        _logger.LogInformation(
            "Downloading model from UploadService. ModelId: {ModelId}, Path: {Path}",
            modelId,
            storagePath);

        try
        {
            // Step 1: Query to find UploadId by storage path
            var queryResponse = await _httpClient.GetAsync(
                $"/upload/v1/files?pathPrefix={Uri.EscapeDataString(storagePath)}&pageSize=1",
                cancellationToken);

            if (!queryResponse.IsSuccessStatusCode)
            {
                _logger.LogWarning("Model not found in UploadService. Path: {Path}", storagePath);
                throw new FileNotFoundException($"Model not found in storage: {modelId}");
            }

            var queryResult = await queryResponse.Content.ReadFromJsonAsync<QueryFilesResponse>(cancellationToken: cancellationToken);
            var fileMetadata = queryResult?.Files?.FirstOrDefault();

            if (fileMetadata == null)
            {
                _logger.LogWarning("Model not found in UploadService. Path: {Path}", storagePath);
                throw new FileNotFoundException($"Model not found in storage: {modelId}");
            }

            // Step 2: Generate signed URL for download
            var signedUrlRequest = new GenerateSignedUrlRequest { ExpirationMinutes = 60 };
            var signedUrlResponse = await _httpClient.PostAsJsonAsync(
                $"/upload/v1/files/{fileMetadata.UploadId}/signed-url",
                signedUrlRequest,
                cancellationToken);

            signedUrlResponse.EnsureSuccessStatusCode();

            var signedUrlResult = await signedUrlResponse.Content.ReadFromJsonAsync<SignedUrlResponse>(cancellationToken: cancellationToken);

            if (signedUrlResult == null || string.IsNullOrEmpty(signedUrlResult.SignedUrl))
            {
                throw new InvalidOperationException("Failed to generate signed URL for download.");
            }

            // Step 3: Download from GCS using signed URL
            using var downloadClient = new HttpClient(); // Don't use authenticated client for GCS
            var downloadResponse = await downloadClient.GetAsync(signedUrlResult.SignedUrl, cancellationToken);
            downloadResponse.EnsureSuccessStatusCode();

            await using var fileStream = File.Create(tempPath);
            await downloadResponse.Content.CopyToAsync(fileStream, cancellationToken);

            _logger.LogInformation(
                "Model downloaded successfully. ModelId: {ModelId}, LocalPath: {LocalPath}, Size: {Size} bytes",
                modelId,
                tempPath,
                fileStream.Length);

            return tempPath;
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Model not found in UploadService. ModelId: {ModelId}", modelId);
            throw new FileNotFoundException($"Model not found in storage: {modelId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to download model from UploadService. ModelId: {ModelId}",
                modelId);
            throw;
        }
    }

    public async Task<bool> ModelExistsAsync(Guid modelId, string modelType, CancellationToken cancellationToken = default)
    {
        var storagePath = GetStoragePath(modelType, modelId);

        try
        {
            var response = await _httpClient.GetAsync(
                $"/upload/v1/files?pathPrefix={Uri.EscapeDataString(storagePath)}&pageSize=1",
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return false;
            }

            var result = await response.Content.ReadFromJsonAsync<QueryFilesResponse>(cancellationToken: cancellationToken);
            return result?.Files?.Any() ?? false;
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error checking if model exists in UploadService. ModelId: {ModelId}",
                modelId);
            throw;
        }
    }

    public async Task DeleteModelAsync(Guid modelId, string modelType, CancellationToken cancellationToken = default)
    {
        var storagePath = GetStoragePath(modelType, modelId);

        _logger.LogInformation(
            "Deleting model from UploadService. Path: {Path}, ModelId: {ModelId}",
            storagePath,
            modelId);

        try
        {
            // Step 1: Find UploadId by storage path
            var queryResponse = await _httpClient.GetAsync(
                $"/upload/v1/files?pathPrefix={Uri.EscapeDataString(storagePath)}&pageSize=1",
                cancellationToken);

            if (!queryResponse.IsSuccessStatusCode)
            {
                _logger.LogWarning("Model not found for deletion. Path: {Path}", storagePath);
                return; // Deletion is idempotent
            }

            var queryResult = await queryResponse.Content.ReadFromJsonAsync<QueryFilesResponse>(cancellationToken: cancellationToken);
            var fileMetadata = queryResult?.Files?.FirstOrDefault();

            if (fileMetadata == null)
            {
                _logger.LogWarning("Model not found for deletion. Path: {Path}", storagePath);
                return; // Deletion is idempotent
            }

            // Step 2: Delete by UploadId
            var deleteResponse = await _httpClient.DeleteAsync($"/upload/v1/files/{fileMetadata.UploadId}", cancellationToken);

            if (deleteResponse.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Model not found for deletion. UploadId: {UploadId}", fileMetadata.UploadId);
                return; // Deletion is idempotent
            }

            deleteResponse.EnsureSuccessStatusCode();

            _logger.LogInformation("Model deleted successfully. ModelId: {ModelId}", modelId);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Model not found for deletion. ModelId: {ModelId}", modelId);
            // Don't throw - deletion is idempotent
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to delete model from UploadService. ModelId: {ModelId}",
                modelId);
            throw;
        }
    }

    public async Task<IReadOnlyList<Guid>> ListModelsAsync(string modelType, CancellationToken cancellationToken = default)
    {
        var prefix = $"models/{modelType}/";
        var modelIds = new List<Guid>();

        _logger.LogInformation("Listing models in UploadService. Prefix: {Prefix}", prefix);

        try
        {
            // Query with pagination (fetch all pages if needed)
            int page = 1;
            const int pageSize = 100;
            int totalPages;

            do
            {
                var response = await _httpClient.GetAsync(
                    $"/upload/v1/files?pathPrefix={Uri.EscapeDataString(prefix)}&page={page}&pageSize={pageSize}",
                    cancellationToken);

                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadFromJsonAsync<QueryFilesResponse>(cancellationToken: cancellationToken);

                if (result?.Files != null)
                {
                    foreach (var file in result.Files)
                    {
                        // Extract modelId from path: models/{modelType}/{modelId}.zip
                        var fileName = Path.GetFileNameWithoutExtension(file.StoragePath);
                        if (Guid.TryParse(fileName, out var modelId))
                        {
                            modelIds.Add(modelId);
                        }
                    }
                }

                totalPages = result?.TotalPages ?? 1;
                page++;

            } while (page <= totalPages);

            _logger.LogInformation("Listed {Count} models. ModelType: {ModelType}", modelIds.Count, modelType);

            return modelIds.AsReadOnly();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list models from UploadService. Prefix: {Prefix}", prefix);
            throw;
        }
    }

    /// <summary>
    /// Constructs the storage path for a model.
    /// Format: models/{modelType}/{modelId}.zip
    /// </summary>
    private static string GetStoragePath(string modelType, Guid modelId)
    {
        return $"models/{modelType}/{modelId}.zip";
    }
}

/// <summary>
/// Configuration options for UploadService.
/// </summary>
public class UploadServiceOptions
{
    /// <summary>
    /// Base URL for the UploadService API.
    /// Example: http://upload-service (Kubernetes internal DNS)
    /// Production: Injected via environment variable from Google Secret Manager
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// JWT token for service-to-service authentication.
    /// Auto-populated by ServiceDefaults from AuthService.
    /// </summary>
    public string? JwtToken { get; set; }

    /// <summary>
    /// Request timeout in seconds. Default is 600 (10 minutes) for large file uploads.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 600;
}

/// <summary>
/// Response model from POST /upload/v1/uploads
/// </summary>
internal class UploadResponse
{
    [JsonPropertyName("uploadId")]
    public string UploadId { get; set; } = string.Empty;

    [JsonPropertyName("storagePath")]
    public string StoragePath { get; set; } = string.Empty;

    [JsonPropertyName("fileName")]
    public string FileName { get; set; } = string.Empty;

    [JsonPropertyName("fileSize")]
    public long FileSize { get; set; }

    [JsonPropertyName("uploadedAt")]
    public DateTime UploadedAt { get; set; }
}

/// <summary>
/// Response model from GET /upload/v1/files (query)
/// </summary>
internal class QueryFilesResponse
{
    [JsonPropertyName("files")]
    public List<FileMetadataResponse>? Files { get; set; }

    [JsonPropertyName("totalCount")]
    public int TotalCount { get; set; }

    [JsonPropertyName("page")]
    public int Page { get; set; }

    [JsonPropertyName("pageSize")]
    public int PageSize { get; set; }

    [JsonPropertyName("totalPages")]
    public int TotalPages { get; set; }
}

/// <summary>
/// File metadata response model
/// </summary>
internal class FileMetadataResponse
{
    [JsonPropertyName("uploadId")]
    public string UploadId { get; set; } = string.Empty;

    [JsonPropertyName("storagePath")]
    public string StoragePath { get; set; } = string.Empty;

    [JsonPropertyName("fileName")]
    public string FileName { get; set; } = string.Empty;

    [JsonPropertyName("fileSize")]
    public long FileSize { get; set; }

    [JsonPropertyName("uploadedAt")]
    public DateTime UploadedAt { get; set; }

    [JsonPropertyName("lastAccessedAt")]
    public DateTime? LastAccessedAt { get; set; }
}

/// <summary>
/// Request model for generating signed download URL
/// </summary>
internal class GenerateSignedUrlRequest
{
    [JsonPropertyName("expirationMinutes")]
    public int ExpirationMinutes { get; set; } = 60;
}

/// <summary>
/// Response model from POST /upload/v1/files/{uploadId}/signed-url
/// </summary>
internal class SignedUrlResponse
{
    [JsonPropertyName("signedUrl")]
    public string SignedUrl { get; set; } = string.Empty;

    [JsonPropertyName("expiresAt")]
    public DateTime ExpiresAt { get; set; }

    [JsonPropertyName("uploadId")]
    public string UploadId { get; set; } = string.Empty;

    [JsonPropertyName("storagePath")]
    public string StoragePath { get; set; } = string.Empty;
}
