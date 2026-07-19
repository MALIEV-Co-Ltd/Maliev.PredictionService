using Maliev.PredictionService.Application.DTOs.Responses;
using Maliev.PredictionService.Application.Services;
using Maliev.PredictionService.Application.Validators;
using Maliev.PredictionService.Domain.Entities;
using Maliev.PredictionService.Domain.Enums;
using Maliev.PredictionService.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Maliev.PredictionService.Application.Commands;

/// <summary>
/// Handles PredictPrintTimeCommand with caching, validation, and audit logging.
/// Flow: Validate → Check Cache → Load Model → Predict → Cache Result → Audit Log
/// </summary>
public class PredictPrintTimeCommandHandler : IRequestHandler<PredictPrintTimeCommand, PredictionResponse>
{
    private readonly IPrintTimePredictionService _predictionService;
    private readonly IModelRepository _modelRepository;
    private readonly IPredictionAuditRepository _auditRepository;
    private readonly PrintTimeRequestValidator _validator;
    private readonly ILogger<PredictPrintTimeCommandHandler> _logger;

    public PredictPrintTimeCommandHandler(
        IPrintTimePredictionService predictionService,
        IModelRepository modelRepository,
        IPredictionAuditRepository auditRepository,
        PrintTimeRequestValidator validator,
        ILogger<PredictPrintTimeCommandHandler> logger)
    {
        _predictionService = predictionService;
        _modelRepository = modelRepository;
        _auditRepository = auditRepository;
        _validator = validator;
        _logger = logger;
    }

    public async Task<PredictionResponse> Handle(PredictPrintTimeCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;
        var startTime = DateTime.UtcNow;

        _logger.LogInformation(
            "Processing print time prediction. CorrelationId: {CorrelationId}, File: {FileName}, User: {UserId}",
            command.CorrelationId,
            request.FileName,
            command.UserId);

        try
        {
            // Step 1: Validate request
            var validationResult = _validator.Validate(request);
            if (!validationResult.IsValid)
            {
                var errorMessage = string.Join("; ", validationResult.Errors);
                _logger.LogWarning(
                    "Validation failed for print time prediction. Errors: {Errors}",
                    errorMessage);

                throw new ArgumentException(errorMessage);
            }

            // Step 2: Get active model for print time predictions
            var activeModel = await _modelRepository.GetActiveModelByTypeAsync(
                ModelType.PrintTime,
                cancellationToken);

            if (activeModel == null)
            {
                _logger.LogError("No active print time model found");
                throw new InvalidOperationException("Print time prediction service is currently unavailable. No active model deployed.");
            }

            _logger.LogDebug(
                "Using active model: {ModelId}, Version: {ModelVersion}",
                activeModel.Id,
                activeModel.ModelVersion);

            // Step 3: Check cache (content-based key using geometry hash + parameters)
            var cacheKey = await _predictionService.GenerateCacheKeyAsync(request, activeModel.ModelVersion.ToString(), cancellationToken);
            var cachedResult = await _predictionService.GetCachedPredictionAsync(cacheKey, cancellationToken);

            if (cachedResult != null)
            {
                _logger.LogInformation("Cache hit for print time prediction. Key: {CacheKey}", cacheKey);
                return cachedResult with { CacheStatus = "hit" };
            }

            _logger.LogDebug("Cache miss. Computing fresh prediction.");

            // Step 4: Perform prediction via application service
            if (string.IsNullOrEmpty(activeModel.FilePath))
            {
                throw new InvalidOperationException($"Model {activeModel.Id} has no file path configured");
            }

            var predictionResponse = await _predictionService.PredictAsync(
                request,
                activeModel.FilePath,
                activeModel.ModelVersion.ToString(),
                cancellationToken);

            // Step 5: Cache the result
            await _predictionService.CachePredictionAsync(cacheKey, predictionResponse, cancellationToken);

            // Step 6: Audit log
            var duration = DateTime.UtcNow - startTime;
            await LogPredictionAuditAsync(
                command,
                activeModel,
                predictionResponse,
                duration,
                cancellationToken);

            _logger.LogInformation(
                "Print time prediction complete. Predicted: {Time:F2} min, Duration: {Duration:F2}s",
                predictionResponse.PredictedValue,
                duration.TotalSeconds);

            return predictionResponse;
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid request for print time prediction");
            throw;
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Print time prediction service error");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unexpected error during print time prediction. CorrelationId: {CorrelationId}",
                command.CorrelationId);
            throw;
        }
    }

    /// <summary>
    /// Logs prediction to audit table for compliance and analytics.
    /// </summary>
    private async Task LogPredictionAuditAsync(
        PredictPrintTimeCommand command,
        MLModel model,
        PredictionResponse response,
        TimeSpan duration,
        CancellationToken cancellationToken)
    {
        var auditLog = new PredictionAuditLog
        {
            Id = Guid.NewGuid(),
            RequestId = command.CorrelationId,
            ModelType = ModelType.PrintTime,
            ModelVersion = model.ModelVersion.ToString(),
            InputFeatures = new Dictionary<string, object>
            {
                { "file_name", command.Request.FileName },
                { "material_type", command.Request.MaterialType },
                { "material_density", command.Request.MaterialDensity },
                { "printer_type", command.Request.PrinterType },
                { "print_speed", command.Request.PrintSpeed },
                { "layer_height", command.Request.LayerHeight },
                { "nozzle_temp", command.Request.NozzleTemperature },
                { "bed_temp", command.Request.BedTemperature },
                { "infill", command.Request.InfillPercentage }
            },
            OutputPrediction = new Dictionary<string, object>
            {
                { "predicted_value", response.PredictedValue },
                { "unit", response.Unit },
                { "confidence_lower", response.ConfidenceLower },
                { "confidence_upper", response.ConfidenceUpper },
                { "explanation", response.Explanation }
            },
            CacheStatus = response.CacheStatus == "hit" ? PredictionStatus.CachedHit : PredictionStatus.Success,
            ResponseTimeMs = (int)duration.TotalMilliseconds,
            UserId = command.UserId,
            Timestamp = DateTime.UtcNow
        };

        await _auditRepository.AddAsync(auditLog, cancellationToken);
    }
}
