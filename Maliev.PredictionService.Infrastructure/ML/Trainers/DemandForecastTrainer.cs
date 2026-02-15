using Maliev.PredictionService.Domain.Entities;
using Maliev.PredictionService.Domain.Enums;
using Maliev.PredictionService.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Transforms.TimeSeries;

namespace Maliev.PredictionService.Infrastructure.ML.Trainers;

/// <summary>
/// Trains SSA (Singular Spectrum Analysis) models for sales demand forecasting.
/// Implements ML.NET time-series pipeline with trend/seasonality decomposition.
/// </summary>
public class DemandForecastTrainer
{
    private readonly MLContext _mlContext;
    private readonly ILogger<DemandForecastTrainer> _logger;

    public DemandForecastTrainer(ILogger<DemandForecastTrainer> logger)
    {
        _mlContext = new MLContext(seed: 42);
        _logger = logger;
    }

    /// <summary>
    /// Input data schema for demand forecasting training.
    /// Note: Date is string to support flexible parsing, SSA only uses Demand values.
    /// </summary>
    public class DemandInput
    {
        [LoadColumn(0)] public string? Date { get; set; }
        [LoadColumn(1)] public float Demand { get; set; }
    }

    /// <summary>
    /// Forecast output schema.
    /// </summary>
    public class DemandForecast
    {
        public float[] ForecastedDemand { get; set; } = Array.Empty<float>();
        public float[] LowerBoundConfidence { get; set; } = Array.Empty<float>();
        public float[] UpperBoundConfidence { get; set; } = Array.Empty<float>();
    }

    /// <summary>
    /// Training result containing model, metrics, and metadata.
    /// </summary>
    public record TrainingResult
    {
        public required ITransformer Model { get; init; }
        public required ForecastingMetrics Metrics { get; init; }
        public required int TrainingSampleCount { get; init; }
        public required int TestSampleCount { get; init; }
        public required TimeSpan TrainingDuration { get; init; }
        public DataViewSchema? Schema { get; init; } // Optional schema for model saving
    }

    /// <summary>
    /// Forecasting evaluation metrics.
    /// </summary>
    public record ForecastingMetrics
    {
        public required double MAPE { get; init; } // Mean Absolute Percentage Error
        public required double RMSE { get; init; } // Root Mean Squared Error
        public required double MAE { get; init; } // Mean Absolute Error
    }

    /// <summary>
    /// Trains an SSA forecasting model on historical sales data.
    /// </summary>
    /// <param name="trainingDataset">Training dataset entity with historical sales.</param>
    /// <param name="horizon">Forecast horizon in days (7, 30, or 90).</param>
    /// <param name="windowSize">SSA window size for trend extraction (default: horizon).</param>
    /// <param name="seriesLength">Historical data length to consider (default: 365 days).</param>
    /// <param name="testHoldoutDays">Days to holdout for testing (default: 20% of horizon).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Training result with model and metrics.</returns>
    public async Task<TrainingResult> TrainModelAsync(
        TrainingDataset trainingDataset,
        int horizon = 30,
        int? windowSize = null,
        int seriesLength = 365,
        int? testHoldoutDays = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Starting demand forecast model training. Dataset: {DatasetId}, Horizon: {Horizon} days",
            trainingDataset.Id,
            horizon);

        var startTime = DateTime.UtcNow;

        // Load historical sales data
        var filePath = trainingDataset.FilePath ?? throw new ArgumentException("Dataset FilePath is null");
        var dataView = await LoadDataFromFileAsync(filePath, cancellationToken);

        // Determine optimal parameters
        var actualWindowSize = windowSize ?? Math.Min(horizon, 10); // Default to smaller of horizon or 10
        var actualTestHoldout = testHoldoutDays ?? (int)(horizon * 0.2);

        // Split data for training and testing
        // Note: GetRowCount() may return 0 even when data exists, so we enumerate to get actual count
        var allData = _mlContext.Data.CreateEnumerable<DemandInput>(dataView, reuseRowObject: false).ToList();
        var rowCount = allData.Count;
        var trainCount = rowCount - actualTestHoldout;

        // SSA requirement: seriesLength must equal trainSize to avoid negative initialWindowSize
        var actualSeriesLength = trainCount;

        // Validate: trainSize must be > 2 * windowSize
        if (trainCount <= 2 * actualWindowSize)
        {
            actualWindowSize = Math.Max(1, trainCount / 3); // Adjust windowSize to be safe
        }

        _logger.LogInformation(
            "Data loaded. Total: {TotalRows}, Train: {TrainCount}, Test: {TestCount}, WindowSize: {WindowSize}, SeriesLength: {SeriesLength}",
            rowCount, trainCount, actualTestHoldout, actualWindowSize, actualSeriesLength);

        // Build SSA forecasting pipeline
        var pipeline = _mlContext.Forecasting.ForecastBySsa(
            outputColumnName: nameof(DemandForecast.ForecastedDemand),
            inputColumnName: nameof(DemandInput.Demand),
            windowSize: actualWindowSize,
            seriesLength: actualSeriesLength,
            trainSize: trainCount,
            horizon: horizon,
            confidenceLevel: 0.95f, // 95% confidence interval
            confidenceLowerBoundColumn: nameof(DemandForecast.LowerBoundConfidence),
            confidenceUpperBoundColumn: nameof(DemandForecast.UpperBoundConfidence));

        // Train the model
        _logger.LogInformation("Training SSA model with window size {WindowSize}, series length {SeriesLength}...",
            actualWindowSize, seriesLength);

        var model = pipeline.Fit(dataView);

        // Evaluate on holdout test set
        var forecastEngine = model.CreateTimeSeriesEngine<DemandInput, DemandForecast>(_mlContext);
        var testMetrics = EvaluateForecast(dataView, trainCount, actualTestHoldout, forecastEngine);

        var duration = DateTime.UtcNow - startTime;

        _logger.LogInformation(
            "Training complete. MAPE: {MAPE:F2}%, RMSE: {RMSE:F2}, MAE: {MAE:F2}, Duration: {Duration:F2}s",
            testMetrics.MAPE * 100,
            testMetrics.RMSE,
            testMetrics.MAE,
            duration.TotalSeconds);

        return new TrainingResult
        {
            Model = model,
            Metrics = testMetrics,
            TrainingSampleCount = trainCount,
            TestSampleCount = actualTestHoldout,
            TrainingDuration = duration,
            Schema = dataView.Schema
        };
    }

    /// <summary>
    /// Loads time-series data from CSV file.
    /// Expected format: Date,Demand
    /// </summary>
    private Task<IDataView> LoadDataFromFileAsync(string filePath, CancellationToken cancellationToken)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Training dataset file not found: {filePath}");
        }

        _logger.LogDebug("Loading time-series data from {FilePath}", filePath);

        var dataView = _mlContext.Data.LoadFromTextFile<DemandInput>(
            filePath,
            hasHeader: true,
            separatorChar: ',');

        return Task.FromResult(dataView);
    }

    /// <summary>
    /// Evaluates forecast accuracy using MAPE, RMSE, and MAE.
    /// </summary>
    private ForecastingMetrics EvaluateForecast(
        IDataView fullData,
        int trainSize,
        int testSize,
        TimeSeriesPredictionEngine<DemandInput, DemandForecast> forecastEngine)
    {
        // Get actual test values
        var allData = _mlContext.Data.CreateEnumerable<DemandInput>(fullData, reuseRowObject: false).ToList();
        var testActual = allData.Skip(trainSize).Take(testSize).Select(d => d.Demand).ToArray();

        // Get forecasted values
        var forecast = forecastEngine.Predict();
        var testForecast = forecast.ForecastedDemand.Take(testSize).ToArray();

        // Calculate metrics
        double sumAbsError = 0;
        double sumSquaredError = 0;
        double sumPercentageError = 0;
        int validCount = 0;

        for (int i = 0; i < Math.Min(testActual.Length, testForecast.Length); i++)
        {
            var actual = testActual[i];
            var predicted = testForecast[i];

            if (actual > 0) // Avoid division by zero for MAPE
            {
                var absError = Math.Abs(actual - predicted);
                var sqError = Math.Pow(actual - predicted, 2);
                var pctError = Math.Abs((actual - predicted) / actual);

                sumAbsError += absError;
                sumSquaredError += sqError;
                sumPercentageError += pctError;
                validCount++;
            }
        }

        if (validCount == 0)
            throw new InvalidOperationException("No valid test samples for evaluation");

        return new ForecastingMetrics
        {
            MAPE = sumPercentageError / validCount, // Mean Absolute Percentage Error (0-1 range, multiply by 100 for %)
            RMSE = Math.Sqrt(sumSquaredError / validCount), // Root Mean Squared Error
            MAE = sumAbsError / validCount // Mean Absolute Error
        };
    }

    /// <summary>
    /// Calculates confidence bands (80%, 95%) for forecasts.
    /// SSA model already provides 95% confidence, this method converts to different levels.
    /// </summary>
    public (float[] Lower80, float[] Upper80, float[] Lower95, float[] Upper95) CalculateConfidenceBands(
        DemandForecast forecast,
        double standardError)
    {
        // 80% confidence: ±1.28 standard errors
        // 95% confidence: ±1.96 standard errors (already provided by SSA)

        var horizon = forecast.ForecastedDemand.Length;
        var lower80 = new float[horizon];
        var upper80 = new float[horizon];

        for (int i = 0; i < horizon; i++)
        {
            var predicted = forecast.ForecastedDemand[i];
            var margin80 = (float)(1.28 * standardError);

            lower80[i] = Math.Max(0, predicted - margin80); // Demand cannot be negative
            upper80[i] = predicted + margin80;
        }

        return (lower80, upper80, forecast.LowerBoundConfidence, forecast.UpperBoundConfidence);
    }
}
