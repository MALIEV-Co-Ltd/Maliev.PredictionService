using Google.Apis.Auth.OAuth2;
using Google.Cloud.Storage.V1;
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
    /// Uses Google Cloud Storage in production, local file storage in development.
    /// </summary>
    public static IServiceCollection AddModelStorage(this IServiceCollection services, IConfiguration configuration)
    {
        var storageType = configuration["ModelStorage:Type"] ?? "LocalFile";

        switch (storageType.ToLowerInvariant())
        {
            case "googlecloud":
            case "gcs":
                services.AddGoogleCloudStorage(configuration);
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
    /// Registers Google Cloud Storage for model storage with sandboxed service account access.
    /// </summary>
    private static IServiceCollection AddGoogleCloudStorage(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure options
        services.Configure<GoogleCloudStorageOptions>(options =>
        {
            var section = configuration.GetSection("GoogleCloudStorage");
            options.BucketName = section["BucketName"] ?? string.Empty;
            options.ServiceAccountKeyPath = section["ServiceAccountKeyPath"];
        });

        // Register StorageClient with service account authentication
        services.AddSingleton<StorageClient>(sp =>
        {
            var keyPath = configuration["GoogleCloudStorage:ServiceAccountKeyPath"];

            if (!string.IsNullOrWhiteSpace(keyPath) && File.Exists(keyPath))
            {
                // Use service account JSON key file (sandboxed access)
                // Set environment variable for Google Application Default Credentials
                Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", keyPath);
                return StorageClient.Create();
            }
            else
            {
                // Use Application Default Credentials (ADC)
                // In production, this will use the Compute Engine/GKE service account
                return StorageClient.Create();
            }
        });

        // Register the GCS implementation
        services.AddScoped<IModelStorageService, GoogleCloudModelStorageService>();

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
