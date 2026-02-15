using Microsoft.EntityFrameworkCore;
using Maliev.PredictionService.Infrastructure.Persistence;
using Maliev.PredictionService.Infrastructure.Persistence.Repositories;
using Maliev.PredictionService.Infrastructure.Caching;
using Maliev.PredictionService.Infrastructure.ML.FeatureEngineering;
using Maliev.PredictionService.Infrastructure.ML.Predictors;
using Maliev.PredictionService.Infrastructure.ML.Trainers;
using Maliev.PredictionService.Infrastructure.Services;
using Maliev.PredictionService.Domain.Interfaces;
using Maliev.PredictionService.Domain.Repositories;
using Maliev.PredictionService.Domain.Services;
using Maliev.PredictionService.Application.Interfaces;
using Maliev.PredictionService.Application.Services;
using Maliev.PredictionService.Application.Validators;
using MassTransit;
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

        // EF Core DbContext with PostgreSQL
        services.AddDbContext<PredictionDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("PredictionDatabase")
                ?? throw new InvalidOperationException("PredictionDatabase connection string not found");

            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsAssembly(typeof(PredictionDbContext).Assembly.FullName);
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorCodesToAdd: null);
            });

            // Enable sensitive data logging in development
            if (configuration.GetValue<bool>("Logging:EnableSensitiveDataLogging"))
            {
                options.EnableSensitiveDataLogging();
            }
        });

        // Redis Cache
        var redisConnectionString = configuration.GetConnectionString("Redis")
            ?? throw new InvalidOperationException("Redis connection string not found");

        services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var configurationOptions = ConfigurationOptions.Parse(redisConnectionString);
            configurationOptions.AbortOnConnectFail = false;
            configurationOptions.ConnectRetry = 3;
            configurationOptions.ReconnectRetryPolicy = new ExponentialRetry(5000);

            return ConnectionMultiplexer.Connect(configurationOptions);
        });

        services.AddSingleton<RedisCacheService>();
        services.AddSingleton<CacheKeyGenerator>();
        services.AddSingleton<ICacheKeyGenerator>(sp => sp.GetRequiredService<CacheKeyGenerator>());
        services.AddSingleton<IDistributedCacheService>(sp => sp.GetRequiredService<RedisCacheService>());

        // MassTransit with RabbitMQ
        services.AddMassTransit(x =>
        {
            // Register consumers from Infrastructure assembly
            x.AddConsumers(typeof(Maliev.PredictionService.Infrastructure.AssemblyReference).Assembly);

            x.UsingRabbitMq((context, cfg) =>
            {
                var rabbitMqHost = configuration.GetValue<string>("RabbitMQ:Host") ?? "localhost";
                var rabbitMqUser = configuration.GetValue<string>("RabbitMQ:Username") ?? "guest";
                var rabbitMqPassword = configuration.GetValue<string>("RabbitMQ:Password") ?? "guest";

                cfg.Host(rabbitMqHost, "/", h =>
                {
                    h.Username(rabbitMqUser);
                    h.Password(rabbitMqPassword);
                });

                cfg.ConfigureEndpoints(context);
            });
        });

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

        return services;
    }
}
