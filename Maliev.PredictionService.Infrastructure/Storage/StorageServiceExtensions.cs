using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

// Import extension methods from Aspire ServiceDefaults
// AddAuthenticatedServiceClient is in Microsoft.Extensions.Hosting namespace

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
    public static IHostApplicationBuilder AddModelStorage(this IHostApplicationBuilder builder)
    {
        var storageType = builder.Configuration["ModelStorage:Type"] ?? "LocalFile";

        switch (storageType.ToLowerInvariant())
        {
            case "uploadservice":
            case "cloud":
                builder.AddUploadServiceStorage();
                break;

            case "localfile":
            case "local":
            default:
                builder.Services.AddLocalFileStorage(builder.Configuration);
                break;
        }

        return builder;
    }

    /// <summary>
    /// Registers UploadService (maliev.uploadservice) for model storage.
    /// All file operations go through the centralized upload microservice.
    /// Uses AddAuthenticatedServiceClient for automatic JWT token management.
    /// </summary>
    private static IHostApplicationBuilder AddUploadServiceStorage(this IHostApplicationBuilder builder)
    {
        // Configure options
        builder.Services.Configure<UploadServiceOptions>(options =>
        {
            var section = builder.Configuration.GetSection("UploadService");
            if (int.TryParse(section["TimeoutSeconds"], out var timeout))
            {
                options.TimeoutSeconds = timeout;
            }
        });

        // Register authenticated HttpClient for UploadService
        // This automatically handles JWT token generation and injection via ServiceAccountAuthenticationHandler
        builder.AddAuthenticatedServiceClient<IModelStorageService, UploadServiceModelStorageService>(
            serviceName: "UploadService",
            sourceServiceName: "prediction")
        .ConfigurePrimaryHttpMessageHandler(() =>
        {
            return new HttpClientHandler
            {
                MaxRequestContentBufferSize = 100 * 1024 * 1024, // 100MB for large model files
                AllowAutoRedirect = true
            };
        })
        .ConfigureHttpClient(client =>
        {
            var timeoutSeconds = builder.Configuration.GetValue<int>("UploadService:TimeoutSeconds", 600);
            client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
            client.DefaultRequestHeaders.Add("User-Agent", "Maliev.PredictionService/1.0");
        });

        return builder;
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
