using Microsoft.EntityFrameworkCore;
using Maliev.PredictionService.Infrastructure.BackgroundServices;
using Maliev.PredictionService.Infrastructure.Persistence;
using Maliev.PredictionService.Infrastructure.Persistence.Repositories;
using Maliev.PredictionService.Infrastructure.Caching;
using Maliev.PredictionService.Infrastructure.ML.FeatureEngineering;
using Maliev.PredictionService.Infrastructure.ML.Predictors;
using Maliev.PredictionService.Infrastructure.ML.Trainers;
using Maliev.PredictionService.Infrastructure.Services;
using Maliev.PredictionService.Infrastructure.Storage;
using Maliev.PredictionService.Domain.Interfaces;
using Maliev.PredictionService.Domain.Repositories;
using Maliev.PredictionService.Domain.Services;
using Maliev.PredictionService.Application.Interfaces;
using Maliev.PredictionService.Application.Services;
using Maliev.PredictionService.Application.Validators;
using StackExchange.Redis;

namespace Maliev.PredictionService.Api.Extensions;

/// <summary>
/// Extension methods for service registration
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds all PredictionService dependencies to the service collection
    /// </summary>
    public static IServiceCollection AddPredictionService(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // MediatR for CQRS
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(
                typeof(Maliev.PredictionService.Application.AssemblyReference).Assembly);
        });

        // EF Core DbContext with PostgreSQL and Connection Pooling
        services.AddDbContext<PredictionDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("PredictionDatabase")
                ?? throw new InvalidOperationException("PredictionDatabase connection string not found");

            // Add connection pooling parameters if not already in connection string
            var poolingConnectionString = EnsureConnectionPooling(connectionString);

            options.UseNpgsql(poolingConnectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsAssembly(typeof(PredictionDbContext).Assembly.FullName);
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorCodesToAdd: null);

                // Connection pooling settings
                npgsqlOptions.CommandTimeout(30); // 30 second timeout
            });

            // Performance optimizations
            options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking); // Disable tracking by default for read queries

            // Enable sensitive data logging in development
            if (configuration.GetValue<bool>("Logging:EnableSensitiveDataLogging"))
            {
                options.EnableSensitiveDataLogging();
            }
        });

        // In-Memory Cache for ML models
        services.AddMemoryCache(options =>
        {
            options.SizeLimit = 100; // Max 100 size units (10 models with size 10 each)
            options.CompactionPercentage = 0.25; // Compact 25% when size limit is reached
            options.ExpirationScanFrequency = TimeSpan.FromMinutes(5);
        });
        services.AddSingleton<ModelCacheService>();

        // Redis Cache with Connection Pooling
        var redisConnectionString = configuration.GetConnectionString("Redis")
            ?? throw new InvalidOperationException("Redis connection string not found");

        services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var configurationOptions = ConfigurationOptions.Parse(redisConnectionString);

            // Connection pooling and resilience settings
            configurationOptions.AbortOnConnectFail = false;
            configurationOptions.ConnectRetry = 3;
            configurationOptions.ConnectTimeout = 5000; // 5 seconds
            configurationOptions.SyncTimeout = 5000; // 5 seconds
            configurationOptions.AsyncTimeout = 5000; // 5 seconds
            configurationOptions.ReconnectRetryPolicy = new ExponentialRetry(5000);

            // Connection pool settings
            configurationOptions.KeepAlive = 60; // Send keepalive packets every 60 seconds
            configurationOptions.AllowAdmin = false; // Security: disable admin commands

            // Performance optimizations
            configurationOptions.DefaultDatabase = 0;
            configurationOptions.Ssl = false; // Enable if using SSL/TLS

            return ConnectionMultiplexer.Connect(configurationOptions);
        });

        services.AddSingleton<RedisCacheService>();
        services.AddSingleton<CacheKeyGenerator>();
        services.AddSingleton<ICacheKeyGenerator>(sp => sp.GetRequiredService<CacheKeyGenerator>());
        services.AddSingleton<IDistributedCacheService>(sp => sp.GetRequiredService<RedisCacheService>());

        // Repositories
        services.AddScoped<IModelRepository, ModelRepository>();
        services.AddScoped<IPredictionAuditRepository, PredictionAuditRepository>();
        services.AddScoped<TrainingDatasetRepository>();

        // Domain Services
        services.AddScoped<ModelLifecycleManager>();

        // ML Infrastructure Services
        services.AddSingleton<GeometryFeatureExtractor>();
        services.AddSingleton<ITimeSeriesTransformer, TimeSeriesTransformer>();
        services.AddScoped<IDemandForecaster, DemandForecaster>();
        services.AddScoped<PrintTimePredictor>();
        services.AddScoped<PrintTimeTrainer>();

        // Application Services
        services.AddScoped<CacheService>();
        services.AddScoped<IPrintTimePredictionService, PrintTimePredictionService>();

        // Validators
        services.AddSingleton<PrintTimeRequestValidator>();
        services.AddSingleton<DemandForecastRequestValidator>();

        // Model Storage: Moved to Program.cs to use AddAuthenticatedServiceClient pattern
        // See builder.AddModelStorage() in Program.cs

        // Background Services (can be disabled via configuration for testing)
        var disableBackgroundServices = configuration.GetValue<bool>("BackgroundServices:Disabled", false);
        if (!disableBackgroundServices)
        {
            services.AddHostedService<ModelRetrainingBackgroundService>();
        }

        return services;
    }

    /// <summary>
    /// Ensures PostgreSQL connection string contains optimal pooling parameters.
    /// Npgsql uses connection pooling by default, but these parameters optimize it.
    /// </summary>
    private static string EnsureConnectionPooling(string connectionString)
    {
        var builder = new Npgsql.NpgsqlConnectionStringBuilder(connectionString);

        // Set connection pooling parameters if not already set
        if (!builder.ContainsKey("Pooling"))
        {
            builder.Pooling = true; // Enable connection pooling (default is true)
        }

        if (!builder.ContainsKey("Minimum Pool Size"))
        {
            builder.MinPoolSize = 5; // Minimum 5 connections in pool
        }

        if (!builder.ContainsKey("Maximum Pool Size"))
        {
            builder.MaxPoolSize = 100; // Maximum 100 connections in pool
        }

        if (!builder.ContainsKey("Connection Lifetime"))
        {
            builder.ConnectionLifetime = 300; // 5 minutes - recycle connections
        }

        if (!builder.ContainsKey("Connection Idle Lifetime"))
        {
            builder.ConnectionIdleLifetime = 60; // 1 minute - close idle connections
        }

        if (!builder.ContainsKey("Timeout"))
        {
            builder.Timeout = 30; // 30 second connection timeout
        }

        return builder.ToString();
    }
}
