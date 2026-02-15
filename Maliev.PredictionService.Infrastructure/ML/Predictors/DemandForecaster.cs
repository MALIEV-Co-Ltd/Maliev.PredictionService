using Maliev.PredictionService.Application.DTOs.ML;
using Maliev.PredictionService.Application.Interfaces;
using Maliev.PredictionService.Infrastructure.ML.FeatureEngineering;
using Microsoft.Extensions.Logging;
using Microsoft.ML;
using Microsoft.ML.Transforms.TimeSeries;

namespace Maliev.PredictionService.Infrastructure.ML.Predictors;

/// <summary>
/// Simple input class for SSA forecasting - only contains the Demand column.
/// SSA forecasting doesn't require input data for predictions.
/// Public to allow tests to use the same type for training.
/// </summary>
public class SsaDemandInput
{
    public float Demand { get; set; }
}

/// <summary>
/// Simple output class for SSA forecasting - only contains what ML.NET SSA outputs.
/// Maps to the full DemandForecast after prediction.
/// </summary>
public class SsaForecastOutput
{
    public float[]? ForecastedDemand { get; set; }
    public float[]? LowerBoundConfidence { get; set; }
    public float[]? UpperBoundConfidence { get; set; }
}

/// <summary>
/// Demand forecasting predictor using ML.NET SSA (Singular Spectrum Analysis).
/// Provides time-series demand predictions with anomaly detection.
/// </summary>
public class DemandForecaster : IDemandForecaster
{
    private readonly ILogger<DemandForecaster> _logger;
    private readonly MLContext _mlContext;
    private readonly TimeSeriesTransformer _featureTransformer;

    public DemandForecaster(ILogger<DemandForecaster> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _mlContext = new MLContext(seed: 42);
        _featureTransformer = new TimeSeriesTransformer();
    }

    /// <summary>
    /// Predict demand for the specified horizon with anomaly detection.
    /// Implements T125: Demand forecasting logic.
    /// </summary>
    public async Task<DemandPredictionResult> PredictAsync(DemandPredictionInput input, string modelPath, CancellationToken cancellationToken = default)
    {
        var model = await LoadModelAsync(modelPath, cancellationToken);
        return await PredictAsync(input, model, cancellationToken);
    }

    /// <summary>
    /// Load SSA model from disk.
    /// </summary>
    public async Task<ITransformer> LoadModelAsync(string modelPath, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(modelPath))
            throw new ArgumentException("Model path cannot be null or empty", nameof(modelPath));

        if (!File.Exists(modelPath))
            throw new FileNotFoundException($"Model file not found: {modelPath}");

        _logger.LogInformation("Loading demand forecast SSA model from {ModelPath}", modelPath);

        // Load model in background thread to avoid blocking
        return await Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var stream = File.OpenRead(modelPath);
            var model = _mlContext.Model.Load(stream, out var modelInputSchema);

            _logger.LogInformation("Successfully loaded demand forecast model. Input schema columns: {ColumnCount}",
                modelInputSchema.Count);

            return model;
        }, cancellationToken);
    }

    /// <summary>
    /// Predict demand for the specified horizon with anomaly detection.
    /// </summary>
    public async Task<DemandPredictionResult> PredictAsync(DemandPredictionInput input, ITransformer model, CancellationToken cancellationToken)
    {
        if (input == null)
            throw new ArgumentNullException(nameof(input));

        if (model == null)
            throw new ArgumentNullException(nameof(model));

        _logger.LogInformation("Generating demand forecast for product {ProductId}, horizon: {Horizon} days, granularity: {Granularity}",
            input.ProductId, input.Horizon, input.Granularity);

        return await Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Create forecasting engine from the loaded model
            // Use SsaDemandInput and SsaForecastOutput to match SSA model schema
            var forecastingEngine = model.CreateTimeSeriesEngine<SsaDemandInput, SsaForecastOutput>(_mlContext);

            // SSA outputs arrays for the entire horizon, not individual values
            var ssaOutput = forecastingEngine.Predict();

            if (ssaOutput.ForecastedDemand == null || ssaOutput.LowerBoundConfidence == null || ssaOutput.UpperBoundConfidence == null)
            {
                throw new InvalidOperationException("SSA model output arrays are null");
            }

            var forecasts = new List<DemandForecast>();
            var currentDate = input.BaselineDate.AddDays(1); // Start forecasting from next day

            // Generate forecasts for the specified horizon
            // Horizon represents the number of periods (days or weeks), not total days
            int step = input.Granularity == "weekly" ? 7 : 1;
            int periodsToForecast = input.Horizon;

            for (int period = 0; period < periodsToForecast; period++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                int dayIndex = period * step;
                if (dayIndex >= ssaOutput.ForecastedDemand.Length)
                    break; // Exceeded forecast array bounds

                float forecastedDemand = ssaOutput.ForecastedDemand[dayIndex];
                float lowerBound = ssaOutput.LowerBoundConfidence[dayIndex];
                float upperBound = ssaOutput.UpperBoundConfidence[dayIndex];

                // Detect anomalies: demand >40% deviation from expected
                float deviationPercent = Math.Abs((forecastedDemand - lowerBound) /
                                                   forecastedDemand * 100f);
                bool isAnomaly = deviationPercent > 40f;

                forecasts.Add(new DemandForecast
                {
                    ForecastedDemand = forecastedDemand,
                    LowerBoundConfidence = lowerBound,
                    UpperBoundConfidence = upperBound,
                    ForecastDate = currentDate.AddDays(dayIndex),
                    IsAnomaly = isAnomaly,
                    AnomalyScore = isAnomaly ? deviationPercent : null
                });
            }

            _logger.LogInformation("Generated {ForecastCount} forecasts. Anomalies detected: {AnomalyCount}",
                forecasts.Count, forecasts.Count(f => f.IsAnomaly));

            return new DemandPredictionResult
            {
                Forecasts = forecasts.AsReadOnly(),
                Horizon = input.Horizon,
                Granularity = input.Granularity,
                ForecastGeneratedAt = DateTime.UtcNow
            };
        }, cancellationToken);
    }
}
