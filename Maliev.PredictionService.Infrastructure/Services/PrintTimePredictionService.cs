using Maliev.PredictionService.Application.DTOs.Requests;
using Maliev.PredictionService.Application.DTOs.Responses;
using Maliev.PredictionService.Application.Services;
using Maliev.PredictionService.Domain.Enums;
using Maliev.PredictionService.Infrastructure.Caching;
using Maliev.PredictionService.Infrastructure.ML.FeatureEngineering;
using Maliev.PredictionService.Infrastructure.ML.Predictors;
using Microsoft.Extensions.Logging;

namespace Maliev.PredictionService.Infrastructure.Services;

/// <summary>
/// Infrastructure implementation of print time prediction service.
/// Orchestrates ML prediction, caching, and feature extraction.
/// </summary>
public class PrintTimePredictionService : IPrintTimePredictionService
{
    private readonly PrintTimePredictor _predictor;
    private readonly GeometryFeatureExtractor _featureExtractor;
    private readonly CacheService _cacheService;
    private readonly CacheKeyGenerator _cacheKeyGenerator;
    private readonly ILogger<PrintTimePredictionService> _logger;

    public PrintTimePredictionService(
        PrintTimePredictor predictor,
        GeometryFeatureExtractor featureExtractor,
        CacheService cacheService,
        ILogger<PrintTimePredictionService> logger)
    {
        _predictor = predictor;
        _featureExtractor = featureExtractor;
        _cacheService = cacheService;
        _cacheKeyGenerator = new CacheKeyGenerator();
        _logger = logger;
    }

    public async Task<PredictionResponse> PredictAsync(
        PrintTimePredictionRequest request,
        string modelFilePath,
        string modelVersion,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Starting print time prediction. Model: {ModelVersion}, File: {FileName}", modelVersion, request.FileName);

        // Load model
        var predictionEngine = await _predictor.LoadModelAsync(modelFilePath, cancellationToken);

        // Prepare predictor input
        var predictorInput = new PrintTimePredictor.PredictionInput
        {
            GeometryFileStream = request.GeometryFileStream,
            FileName = request.FileName,
            MaterialDensity = request.MaterialDensity,
            PrintSpeed = request.PrintSpeed,
            NozzleTemperature = request.NozzleTemperature,
            BedTemperature = request.BedTemperature,
            InfillPercentage = request.InfillPercentage
        };

        // Perform prediction
        var predictionResult = await _predictor.PredictAsync(predictorInput, predictionEngine, cancellationToken);

        // Build response with metadata
        return new PredictionResponse
        {
            PredictedValue = predictionResult.PredictedTimeMinutes,
            Unit = "minutes",
            ConfidenceLower = predictionResult.ConfidenceLower,
            ConfidenceUpper = predictionResult.ConfidenceUpper,
            Explanation = predictionResult.Explanation,
            ModelVersion = modelVersion,
            CacheStatus = "miss",
            Timestamp = DateTime.UtcNow,
            Metadata = new Dictionary<string, object>
            {
                { "geometry_volume_mm3", predictionResult.GeometryFeatures.Volume },
                { "surface_area_mm2", predictionResult.GeometryFeatures.SurfaceArea },
                { "layer_count", predictionResult.GeometryFeatures.LayerCount },
                { "support_percentage", predictionResult.GeometryFeatures.SupportPercentage },
                { "complexity_score", predictionResult.GeometryFeatures.ComplexityScore },
                { "bounding_box_width_mm", predictionResult.GeometryFeatures.BoundingBoxWidth },
                { "bounding_box_depth_mm", predictionResult.GeometryFeatures.BoundingBoxDepth },
                { "bounding_box_height_mm", predictionResult.GeometryFeatures.BoundingBoxHeight }
            }
        };
    }

    public async Task<PredictionResponse?> GetCachedPredictionAsync(string cacheKey, CancellationToken cancellationToken = default)
    {
        return await _cacheService.GetByKeyAsync<PredictionResponse>(cacheKey, cancellationToken);
    }

    public async Task CachePredictionAsync(string cacheKey, PredictionResponse response, CancellationToken cancellationToken = default)
    {
        await _cacheService.SetByKeyAsync(cacheKey, response, ModelType.PrintTime, cancellationToken);
    }

    public async Task<string> GenerateCacheKeyAsync(
        PrintTimePredictionRequest request,
        string modelVersion,
        CancellationToken cancellationToken = default)
    {
        // Read geometry file bytes for hashing
        byte[] fileBytes;
        if (request.GeometryFileStream.CanSeek)
        {
            request.GeometryFileStream.Position = 0;
            fileBytes = new byte[request.GeometryFileStream.Length];
            await request.GeometryFileStream.ReadExactlyAsync(fileBytes, cancellationToken);
            request.GeometryFileStream.Position = 0; // Reset for later use
        }
        else
        {
            using var ms = new MemoryStream();
            await request.GeometryFileStream.CopyToAsync(ms, cancellationToken);
            fileBytes = ms.ToArray();
        }

        // Create input parameters dictionary for hashing
        var inputParams = new Dictionary<string, object>
        {
            { "geometry_hash", Convert.ToBase64String(System.Security.Cryptography.SHA256.HashData(fileBytes)) },
            { "material_density", request.MaterialDensity },
            { "print_speed", request.PrintSpeed },
            { "layer_height", request.LayerHeight },
            { "nozzle_temp", request.NozzleTemperature },
            { "bed_temp", request.BedTemperature },
            { "infill", request.InfillPercentage }
        };

        return _cacheKeyGenerator.GenerateCacheKey(ModelType.PrintTime, inputParams, modelVersion);
    }
}
