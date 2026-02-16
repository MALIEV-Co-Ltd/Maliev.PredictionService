using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Maliev.PredictionService.Infrastructure.Storage;

/// <summary>
/// Extension methods for registering model storage services.
/// </summary>
public static class StorageServiceExtensions
{
    /// <summary>
    /// Registers model storage services based on configuration.
    /// Uses UploadService (maliev.uploadservice microservice) in production, local file storage in development.
    /// </summary>
    public static IServiceCollection AddModelStorage(this IServiceCollection services, IConfiguration configuration)
    {
        var storageType = configuration["ModelStorage:Type"] ?? "LocalFile";

        switch (storageType.ToLowerInvariant())
        {
            case "uploadservice":
            case "cloud":
                services.AddUploadServiceStorage(configuration);
                break;

            case "localfile":
            case "local":
            default:
                services.AddLocalFileStorage(configuration);
                break;
        }

        return services;
    }

    /// <summary>
    /// Registers UploadService (maliev.uploadservice) for model storage.
    /// All file operations go through the centralized upload microservice.
    /// </summary>
    private static IServiceCollection AddUploadServiceStorage(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure options
        services.Configure<UploadServiceOptions>(options =>
        {
            var section = configuration.GetSection("UploadService");
            options.BaseUrl = section["BaseUrl"] ?? string.Empty;
            options.ApiKey = section["ApiKey"];
            if (int.TryParse(section["TimeoutSeconds"], out var timeout))
            {
                options.TimeoutSeconds = timeout;
            }
        });

        // Register HttpClient for UploadService
        services.AddHttpClient<IModelStorageService, UploadServiceModelStorageService>((sp, client) =>
        {
            var config = configuration.GetSection("UploadService");
            var baseUrl = config["BaseUrl"];
            var apiKey = config["ApiKey"];
            var timeoutSeconds = 300; // Default timeout
            if (int.TryParse(config["TimeoutSeconds"], out var timeout))
            {
                timeoutSeconds = timeout;
            }

            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                throw new InvalidOperationException("UploadService:BaseUrl configuration is required when using UploadService storage.");
            }

            client.BaseAddress = new Uri(baseUrl);
            client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);

            // Add API key header if provided
            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                client.DefaultRequestHeaders.Add("X-API-Key", apiKey);
            }

            // Add standard headers
            client.DefaultRequestHeaders.Add("User-Agent", "Maliev.PredictionService/1.0");
        });

        return services;
    }

    /// <summary>
    /// Registers local file storage for model storage (development/testing).
    /// </summary>
    private static IServiceCollection AddLocalFileStorage(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure options
        services.Configure<LocalFileStorageOptions>(options =>
        {
            var section = configuration.GetSection("LocalFileStorage");
            options.BasePath = section["BasePath"] ?? Path.Combine(Directory.GetCurrentDirectory(), "models");
        });

        // Register the local file implementation
        services.AddScoped<IModelStorageService, LocalFileModelStorageService>();

        return services;
    }
}
