using Maliev.PredictionService.Application.DTOs.Responses;
using Maliev.PredictionService.Application.Interfaces;
using Maliev.PredictionService.Application.Validators;
using Maliev.PredictionService.Application.DTOs.ML;
using Maliev.PredictionService.Domain.Entities;
using Maliev.PredictionService.Domain.Enums;
using Maliev.PredictionService.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Maliev.PredictionService.Application.Commands.Predictions;

/// <summary>
/// Handles PredictDemandCommand with caching, time-series transformation, and audit logging.
/// Implements T122-T127: cache check (6h TTL), transformation, forecasting, anomaly detection, audit.
/// </summary>
public class PredictDemandCommandHandler : IRequestHandler<PredictDemandCommand, PredictionResponse>
{
    private readonly ILogger<PredictDemandCommandHandler> _logger;
    private readonly IModelRepository _modelRepository;
    private readonly IPredictionAuditRepository _auditRepository;
    private readonly ICacheKeyGenerator _cacheKeyGenerator;
    private readonly IDistributedCacheService _cacheService;
    private readonly IDemandForecaster _forecaster;
    private readonly ITimeSeriesTransformer _timeSeriesTransformer;
    private readonly DemandForecastRequestValidator _validator;

    private const int CacheTtlHours = 6; // 6-hour TTL for demand forecasts

    public PredictDemandCommandHandler(
        ILogger<PredictDemandCommandHandler> logger,
        IModelRepository modelRepository,
        IPredictionAuditRepository auditRepository,
        ICacheKeyGenerator cacheKeyGenerator,
        IDistributedCacheService cacheService,
        IDemandForecaster forecaster,
        ITimeSeriesTransformer timeSeriesTransformer,
        DemandForecastRequestValidator validator)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _modelRepository = modelRepository ?? throw new ArgumentNullException(nameof(modelRepository));
        _auditRepository = auditRepository ?? throw new ArgumentNullException(nameof(auditRepository));
        _cacheKeyGenerator = cacheKeyGenerator ?? throw new ArgumentNullException(nameof(cacheKeyGenerator));
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        _forecaster = forecaster ?? throw new ArgumentNullException(nameof(forecaster));
        _timeSeriesTransformer = timeSeriesTransformer ?? throw new ArgumentNullException(nameof(timeSeriesTransformer));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
    }

    public async Task<PredictionResponse> Handle(PredictDemandCommand command, CancellationToken cancellationToken)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            // Validate request
            _validator.ValidateAndThrow(command.Request);

            _logger.LogInformation(
                "Processing demand forecast request for ProductId: {ProductId}, Horizon: {Horizon}, Granularity: {Granularity}",
                command.Request.ProductId, command.Request.Horizon, command.Request.Granularity);

            // Get active model
            var activeModel = await _modelRepository.GetActiveModelByTypeAsync(ModelType.DemandForecast, cancellationToken);
            if (activeModel == null)
            {
                throw new InvalidOperationException("No active demand forecast model found. Please train a model first.");
            }

            // T123: Generate cache key (6-hour TTL)
            var cacheKey = GenerateCacheKey(command.Request, activeModel.ModelVersion.ToString());

            // Check cache
            var cachedResponse = await _cacheService.GetAsync<PredictionResponse>(cacheKey, cancellationToken);
            if (cachedResponse != null)
            {
                _logger.LogInformation("Cache hit for demand forecast. Key: {CacheKey}", cacheKey);
                return cachedResponse with { CacheStatus = "hit" };
            }

            // Cache miss - compute prediction
            _logger.LogInformation("Cache miss. Computing fresh demand forecast.");

            // T124: Load historical demand data
            var historicalData = await LoadHistoricalDemandDataAsync(
                command.Request.ProductId,
                command.Request.BaselineDate ?? DateTime.UtcNow,
                cancellationToken);

            // T124: Apply time-series transformation
            var transformedData = historicalData.Select(data =>
            {
                var features = _timeSeriesTransformer.Transform(data.Date, historicalData, data.IsPromotion);
                return new
                {
                    data.ProductId,
                    data.Date,
                    data.Demand,
                    Features = features
                };
            }).ToList();

            // T125: Load model and make forecast
            var predictionInput = new DemandPredictionInput
            {
                ProductId = command.Request.ProductId,
                Horizon = command.Request.Horizon,
                Granularity = command.Request.Granularity,
                BaselineDate = command.Request.BaselineDate ?? DateTime.UtcNow,
                HistoricalData = historicalData
            };

            var forecastResult = await _forecaster.PredictAsync(predictionInput, activeModel.FilePath, cancellationToken);

            // T126: Detect anomalies and generate alerts
            var anomalies = forecastResult.Forecasts.Where(f => f.IsAnomaly).ToList();
            if (anomalies.Any())
            {
                _logger.LogWarning(
                    "Anomaly detected in demand forecast for {ProductId}. {AnomalyCount} anomalous forecasts (>40% deviation)",
                    command.Request.ProductId, anomalies.Count);
            }

            // Build response
            var avgForecast = forecastResult.Forecasts.Average(f => f.ForecastedDemand);
            var avgLowerBound = forecastResult.Forecasts.Average(f => f.LowerBoundConfidence);
            var avgUpperBound = forecastResult.Forecasts.Average(f => f.UpperBoundConfidence);

            var response = new PredictionResponse
            {
                PredictedValue = avgForecast,
                Unit = "units",
                ConfidenceLower = avgLowerBound,
                ConfidenceUpper = avgUpperBound,
                Explanation = GenerateExplanation(command.Request, forecastResult, anomalies.Count),
                ModelVersion = activeModel.ModelVersion.ToString(),
                CacheStatus = "miss",
                Timestamp = DateTime.UtcNow,
                Metadata = new Dictionary<string, object>
                {
                    { "product_id", command.Request.ProductId },
                    { "horizon_days", command.Request.Horizon },
                    { "granularity", command.Request.Granularity },
                    { "forecast_count", forecastResult.Forecasts.Count },
                    { "anomaly_count", anomalies.Count },
                    { "baseline_date", (command.Request.BaselineDate ?? DateTime.UtcNow).ToString("yyyy-MM-dd") },
                    { "forecast_dates", forecastResult.Forecasts.Select(f => f.ForecastDate.ToString("yyyy-MM-dd")).ToList() }
                }
            };

            // T127: Store in cache (6-hour TTL)
            await _cacheService.SetAsync(cacheKey, response, TimeSpan.FromHours(CacheTtlHours), cancellationToken);

            // T127: Audit logging
            var duration = DateTime.UtcNow - startTime;
            await LogPredictionAuditAsync(command, activeModel, response, duration, cancellationToken);

            _logger.LogInformation(
                "Demand forecast completed for {ProductId}. Avg forecast: {Forecast:F2} units, Duration: {Duration:F2}s",
                command.Request.ProductId, avgForecast, duration.TotalSeconds);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing demand forecast for ProductId: {ProductId}", command.Request.ProductId);
            throw;
        }
    }

    /// <summary>
    /// Generate cache key for demand forecast request.
    /// </summary>
    private string GenerateCacheKey(Application.DTOs.Requests.DemandForecastRequest request, string modelVersion)
    {
        var inputParams = new Dictionary<string, object>
        {
            { "product_id", request.ProductId },
            { "horizon", request.Horizon },
            { "granularity", request.Granularity.ToLowerInvariant() },
            { "baseline_date", (request.BaselineDate ?? DateTime.UtcNow).ToString("yyyy-MM-dd") }
        };

        return _cacheKeyGenerator.GenerateCacheKey(ModelType.DemandForecast, inputParams, modelVersion);
    }

    /// <summary>
    /// Load historical demand data for the product.
    /// </summary>
    private async Task<IReadOnlyList<DemandInput>> LoadHistoricalDemandDataAsync(
        string productId,
        DateTime baselineDate,
        CancellationToken cancellationToken)
    {
        // Simulated historical data
        await Task.CompletedTask;

        var historicalData = new List<DemandInput>();
        var random = new Random(productId.GetHashCode());

        for (int i = 90; i > 0; i--)
        {
            var date = baselineDate.AddDays(-i);
            var baseDemand = 100f + (float)(random.NextDouble() * 50);
            var seasonality = (float)(20 * Math.Sin(2 * Math.PI * date.DayOfYear / 365));
            var isPromotion = date.Day % 15 == 0;
            var promotionBoost = isPromotion ? 30f : 0f;
            var noise = (float)(random.NextDouble() * 10 - 5);

            historicalData.Add(new DemandInput
            {
                ProductId = productId,
                Date = date,
                Demand = baseDemand + seasonality + promotionBoost + noise,
                IsPromotion = isPromotion,
                IsHoliday = false
            });
        }

        return historicalData.AsReadOnly();
    }

    /// <summary>
    /// Generate human-readable explanation for the forecast.
    /// </summary>
    private string GenerateExplanation(
        Application.DTOs.Requests.DemandForecastRequest request,
        DemandPredictionResult forecastResult,
        int anomalyCount)
    {
        var explanation = $"Demand forecast for product {request.ProductId} over {request.Horizon} days ({request.Granularity} granularity). ";
        explanation += $"Generated {forecastResult.Forecasts.Count} forecast points. ";

        if (anomalyCount > 0)
        {
            explanation += $"⚠ Detected {anomalyCount} anomalous forecasts with >40% deviation from expected values. ";
        }

        var avgForecast = forecastResult.Forecasts.Average(f => f.ForecastedDemand);
        var minForecast = forecastResult.Forecasts.Min(f => f.ForecastedDemand);
        var maxForecast = forecastResult.Forecasts.Max(f => f.ForecastedDemand);

        explanation += $"Average demand: {avgForecast:F1} units (range: {minForecast:F1} - {maxForecast:F1})";

        return explanation;
    }

    /// <summary>
    /// Log prediction to audit trail.
    /// </summary>
    private async Task LogPredictionAuditAsync(
        PredictDemandCommand command,
        MLModel model,
        PredictionResponse response,
        TimeSpan duration,
        CancellationToken cancellationToken)
    {
        var auditLog = new PredictionAuditLog
        {
            Id = Guid.NewGuid(),
            RequestId = command.CorrelationId ?? Guid.NewGuid().ToString(),
            ModelType = ModelType.DemandForecast,
            ModelVersion = model.ModelVersion.ToString(),
            InputFeatures = new Dictionary<string, object>
            {
                { "product_id", command.Request.ProductId },
                { "horizon", command.Request.Horizon },
                { "granularity", command.Request.Granularity },
                { "baseline_date", (command.Request.BaselineDate ?? DateTime.UtcNow).ToString("yyyy-MM-dd") }
            },
            OutputPrediction = new Dictionary<string, object>
            {
                { "predicted_value", response.PredictedValue },
                { "confidence_lower", response.ConfidenceLower },
                { "confidence_upper", response.ConfidenceUpper },
                { "anomaly_count", response.Metadata?["anomaly_count"] ?? 0 }
            },
            CacheStatus = response.CacheStatus == "hit" ? PredictionStatus.CachedHit : PredictionStatus.Success,
            ResponseTimeMs = (int)duration.TotalMilliseconds,
            Timestamp = DateTime.UtcNow,
            UserId = command.UserId
        };

        await _auditRepository.AddAsync(auditLog, cancellationToken);
    }
}
