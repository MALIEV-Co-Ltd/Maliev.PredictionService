using Maliev.PredictionService.Infrastructure.Caching;
using Maliev.PredictionService.Infrastructure.ML.FeatureEngineering;
using Maliev.PredictionService.Infrastructure.ML.Trainers;
using Microsoft.Extensions.Logging;
using Microsoft.ML;

namespace Maliev.PredictionService.Infrastructure.ML.Predictors;

/// <summary>
/// Performs print time predictions using trained ML.NET models.
/// Implements IPredictor interface for consistency across prediction types.
/// </summary>
public class PrintTimePredictor
{
    private readonly MLContext _mlContext;
    private readonly ILogger<PrintTimePredictor> _logger;
    private readonly GeometryFeatureExtractor _featureExtractor;
    private readonly ModelCacheService _modelCache;

    private const long MaxFileSizeBytes = 50 * 1024 * 1024; // 50 MB
    private static readonly HashSet<string> SupportedFormats = new(StringComparer.OrdinalIgnoreCase)
    {
        ".stl"
    };

    public PrintTimePredictor(
        ILogger<PrintTimePredictor> logger,
        GeometryFeatureExtractor featureExtractor,
        ModelCacheService modelCache)
    {
        _mlContext = new MLContext(seed: 42);
        _logger = logger;
        _featureExtractor = featureExtractor;
        _modelCache = modelCache ?? throw new ArgumentNullException(nameof(modelCache));
    }

    /// <summary>
    /// Prediction input parameters for print time estimation.
    /// </summary>
    public record PredictionInput
    {
        public required Stream GeometryFileStream { get; init; }
        public required string FileName { get; init; }
        public required float MaterialDensity { get; init; } // g/cm³
        public required float PrintSpeed { get; init; } // mm/s
        public required float NozzleTemperature { get; init; } // °C
        public required float BedTemperature { get; init; } // °C
        public required float InfillPercentage { get; init; } // 0-100
    }

    /// <summary>
    /// Prediction result with confidence interval and explanation.
    /// </summary>
    public record PredictionResult
    {
        public required float PredictedTimeMinutes { get; init; }
        public required float ConfidenceLower { get; init; } // 95% confidence interval lower bound
        public required float ConfidenceUpper { get; init; } // 95% confidence interval upper bound
        public required string Explanation { get; init; }
        public required GeometryFeatureExtractor.GeometryFeatures GeometryFeatures { get; init; }
    }

    /// <summary>
    /// Loads trained model from cache or file system.
    /// Uses ModelCacheService to avoid repeated disk I/O.
    /// </summary>
    /// <param name="modelFilePath">Path to the trained .zip model file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Loaded prediction engine.</returns>
    public async Task<PredictionEngine<PrintTimeTrainer.PrintTimeInput, PrintTimeTrainer.PrintTimePrediction>> LoadModelAsync(
        string modelFilePath,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Loading print time model from {FilePath}", modelFilePath);

        // Use cache to load model (will load from disk if not cached)
        var model = await _modelCache.GetOrLoadModelAsync(modelFilePath, cancellationToken);

        var predictionEngine = _mlContext.Model.CreatePredictionEngine<
            PrintTimeTrainer.PrintTimeInput,
            PrintTimeTrainer.PrintTimePrediction>(model);

        _logger.LogInformation("Model loaded successfully");

        return predictionEngine;
    }

    /// <summary>
    /// Performs print time prediction with confidence interval calculation.
    /// </summary>
    /// <param name="input">Prediction input parameters.</param>
    /// <param name="predictionEngine">Loaded prediction engine.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Prediction result with confidence bounds.</returns>
    public async Task<PredictionResult> PredictAsync(
        PredictionInput input,
        PredictionEngine<PrintTimeTrainer.PrintTimeInput, PrintTimeTrainer.PrintTimePrediction> predictionEngine,
        CancellationToken cancellationToken = default)
    {
        // Validate input
        ValidateInput(input);

        // Extract geometric features
        _logger.LogDebug("Extracting geometric features from {FileName}", input.FileName);
        var geometryFeatures = await _featureExtractor.ExtractFeaturesAsync(
            input.GeometryFileStream,
            cancellationToken);

        // Prepare ML input
        var mlInput = new PrintTimeTrainer.PrintTimeInput
        {
            Volume = geometryFeatures.Volume,
            SurfaceArea = geometryFeatures.SurfaceArea,
            LayerCount = geometryFeatures.LayerCount,
            SupportPercentage = geometryFeatures.SupportPercentage,
            ComplexityScore = geometryFeatures.ComplexityScore,
            BoundingBoxWidth = geometryFeatures.BoundingBoxWidth,
            BoundingBoxDepth = geometryFeatures.BoundingBoxDepth,
            BoundingBoxHeight = geometryFeatures.BoundingBoxHeight,
            MaterialDensity = input.MaterialDensity,
            PrintSpeed = input.PrintSpeed,
            NozzleTemperature = input.NozzleTemperature,
            BedTemperature = input.BedTemperature,
            InfillPercentage = input.InfillPercentage
        };

        // Perform prediction
        var prediction = predictionEngine.Predict(mlInput);

        // Calculate confidence interval (approximate using model uncertainty)
        // For FastTree, use ±15% as confidence bounds (heuristic)
        var confidenceMargin = prediction.PrintTimeMinutes * 0.15f;
        var confidenceLower = Math.Max(0, prediction.PrintTimeMinutes - confidenceMargin);
        var confidenceUpper = prediction.PrintTimeMinutes + confidenceMargin;

        // Generate explanation
        var explanation = GenerateExplanation(geometryFeatures, input, prediction.PrintTimeMinutes);

        _logger.LogInformation(
            "Prediction complete. File: {FileName}, Predicted: {Time:F2} min ({ConfLow:F2}-{ConfHigh:F2})",
            input.FileName,
            prediction.PrintTimeMinutes,
            confidenceLower,
            confidenceUpper);

        return new PredictionResult
        {
            PredictedTimeMinutes = prediction.PrintTimeMinutes,
            ConfidenceLower = confidenceLower,
            ConfidenceUpper = confidenceUpper,
            Explanation = explanation,
            GeometryFeatures = geometryFeatures
        };
    }

    /// <summary>
    /// Validates prediction input parameters.
    /// </summary>
    private void ValidateInput(PredictionInput input)
    {
        // Validate file size
        if (input.GeometryFileStream.CanSeek && input.GeometryFileStream.Length > MaxFileSizeBytes)
        {
            throw new ArgumentException(
                $"File size ({input.GeometryFileStream.Length / 1024 / 1024:F2} MB) exceeds maximum allowed ({MaxFileSizeBytes / 1024 / 1024} MB)");
        }

        // Validate file format
        var extension = Path.GetExtension(input.FileName);
        if (!SupportedFormats.Contains(extension))
        {
            throw new ArgumentException(
                $"Unsupported file format: {extension}. Supported formats: {string.Join(", ", SupportedFormats)}");
        }

        // Validate parameter ranges
        if (input.MaterialDensity <= 0 || input.MaterialDensity > 20)
        {
            throw new ArgumentException($"Invalid material density: {input.MaterialDensity}. Expected: 0-20 g/cm³");
        }

        if (input.PrintSpeed <= 0 || input.PrintSpeed > 500)
        {
            throw new ArgumentException($"Invalid print speed: {input.PrintSpeed}. Expected: 0-500 mm/s");
        }

        if (input.NozzleTemperature < 150 || input.NozzleTemperature > 300)
        {
            throw new ArgumentException($"Invalid nozzle temperature: {input.NozzleTemperature}. Expected: 150-300 °C");
        }

        if (input.BedTemperature < 0 || input.BedTemperature > 150)
        {
            throw new ArgumentException($"Invalid bed temperature: {input.BedTemperature}. Expected: 0-150 °C");
        }

        if (input.InfillPercentage < 0 || input.InfillPercentage > 100)
        {
            throw new ArgumentException($"Invalid infill percentage: {input.InfillPercentage}. Expected: 0-100");
        }
    }

    /// <summary>
    /// Generates human-readable explanation of prediction factors.
    /// </summary>
    private static string GenerateExplanation(
        GeometryFeatureExtractor.GeometryFeatures geometry,
        PredictionInput input,
        float predictedTime)
    {
        var factors = new List<string>();

        // Geometry factors
        factors.Add($"Volume: {geometry.Volume:F2} mm³");
        factors.Add($"Surface Area: {geometry.SurfaceArea:F2} mm²");
        factors.Add($"Layer Count: {geometry.LayerCount}");

        if (geometry.SupportPercentage > 20)
        {
            factors.Add($"High support requirement ({geometry.SupportPercentage:F1}% of geometry)");
        }

        if (geometry.ComplexityScore > 70)
        {
            factors.Add($"Complex geometry (score: {geometry.ComplexityScore:F1}/100)");
        }

        // Material & printer factors
        factors.Add($"Print Speed: {input.PrintSpeed} mm/s");
        factors.Add($"Infill: {input.InfillPercentage}%");

        var hoursMinutes = TimeSpan.FromMinutes(predictedTime);
        var timeStr = hoursMinutes.TotalHours >= 1
            ? $"{hoursMinutes.Hours}h {hoursMinutes.Minutes}m"
            : $"{hoursMinutes.Minutes}m";

        return $"Estimated print time: {timeStr}. Key factors: {string.Join(", ", factors)}.";
    }
}
