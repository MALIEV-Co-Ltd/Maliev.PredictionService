using Asp.Versioning;
using Maliev.Aspire.ServiceDefaults.Authorization;
using Maliev.PredictionService.Api.Authorization;
using Maliev.PredictionService.Api.Validation;
using Maliev.PredictionService.Application.Commands;
using Maliev.PredictionService.Application.Commands.Predictions;
using Maliev.PredictionService.Application.DTOs.Requests;
using Maliev.PredictionService.Application.DTOs.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace Maliev.PredictionService.Api.Controllers;

/// <summary>
/// Controller for ML prediction operations (print time, demand forecast, price optimization).
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("predictionservice/v{version:apiVersion}/predictions")]
[Authorize]
public class PredictionsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<PredictionsController> _logger;

    public PredictionsController(IMediator mediator, ILogger<PredictionsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Predict 3D print time for a given geometry file.
    /// </summary>
    /// <remarks>
    /// Uploads a 3D geometry file (STL binary format) with print parameters and returns
    /// an estimated manufacturing time with confidence intervals.
    ///
    /// **Sample Request:**
    /// ```
    /// POST /predictionservice/v1/predictions/print-time
    /// Content-Type: multipart/form-data
    ///
    /// geometryFile: [binary STL file]
    /// materialType: "PLA"
    /// materialDensity: 1.25
    /// printerType: "Prusa i3 MK3S+"
    /// printSpeed: 60
    /// layerHeight: 0.2
    /// nozzleTemperature: 210
    /// bedTemperature: 60
    /// infillPercentage: 20
    /// ```
    ///
    /// **Sample Response:**
    /// ```json
    /// {
    ///   "predictedValue": 135.5,
    ///   "unit": "minutes",
    ///   "confidenceLower": 115.2,
    ///   "confidenceUpper": 155.8,
    ///   "explanation": "Estimated print time: 2h 15m. Key factors: Volume: 12500 mm³, Layer Count: 450, Complex geometry (score: 75/100).",
    ///   "modelVersion": "1.0.0",
    ///   "cacheStatus": "miss",
    ///   "timestamp": "2026-02-14T10:30:00Z",
    ///   "metadata": {
    ///     "geometry_volume_mm3": 12500,
    ///     "surface_area_mm2": 8500,
    ///     "layer_count": 450,
    ///     "support_percentage": 15.5,
    ///     "complexity_score": 75
    ///   }
    /// }
    /// ```
    /// </remarks>
    /// <param name="geometryFile">3D geometry file in STL binary format (max 50MB).</param>
    /// <param name="materialType">Material type (PLA, ABS, PETG, TPU, Nylon, HIPS, ASA, PC).</param>
    /// <param name="materialDensity">Material density in g/cm³ (e.g., PLA: 1.25, ABS: 1.05).</param>
    /// <param name="printerType">3D printer model identifier (e.g., "Prusa i3 MK3S+").</param>
    /// <param name="printSpeed">Print speed in mm/s (typical: 30-80).</param>
    /// <param name="layerHeight">Layer height in mm (common: 0.1, 0.15, 0.2, 0.3).</param>
    /// <param name="nozzleTemperature">Nozzle temperature in °C (PLA: 190-220, ABS: 220-250).</param>
    /// <param name="bedTemperature">Heated bed temperature in °C (PLA: 50-60, ABS: 90-110).</param>
    /// <param name="infillPercentage">Infill percentage 0-100 (typical: 10-30).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Prediction response with estimated print time and metadata.</returns>
    /// <response code="200">Returns the prediction result with time estimate.</response>
    /// <response code="400">Invalid request (file too large, unsupported format, invalid parameters).</response>
    /// <response code="401">Unauthorized - authentication required.</response>
    /// <response code="403">Forbidden - insufficient permissions.</response>
    /// <response code="500">Internal server error during prediction.</response>
    /// <response code="503">Service unavailable - no active model deployed.</response>
    [HttpPost("print-time")]
    [RequirePermission(PredictionPermissions.PredictionsCreate)]
    [EnableRateLimiting("predictions")]
    [Consumes("multipart/form-data")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(PredictionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> PredictPrintTime(
        [FromForm, Required, ValidFile(MaxFileSize = 50 * 1024 * 1024, AllowedExtensions = new[] { ".stl" }, AllowedContentTypes = new[] { "application/octet-stream", "application/sla", "model/stl", "model/x.stl-binary" })] IFormFile geometryFile,
        [FromForm, Required, SanitizedString(MaxLength = 50)] string materialType,
        [FromForm, Required, Range(0.1, 20.0)] float materialDensity,
        [FromForm, Required, SanitizedString(MaxLength = 100)] string printerType,
        [FromForm, Required, Range(1, 500)] float printSpeed,
        [FromForm, Required, Range(0.05, 1.0)] float layerHeight,
        [FromForm, Required, Range(150, 300)] float nozzleTemperature,
        [FromForm, Required, Range(0, 150)] float bedTemperature,
        [FromForm, Required, Range(0, 100)] float infillPercentage,
        CancellationToken cancellationToken = default)
    {
        var correlationId = HttpContext.TraceIdentifier;
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "anonymous";

        _logger.LogInformation(
            "Print time prediction request received. File: {FileName}, User: {UserId}, CorrelationId: {CorrelationId}",
            geometryFile.FileName,
            userId,
            correlationId);

        try
        {
            // Create request DTO from form data
            await using var fileStream = geometryFile.OpenReadStream();

            var request = new PrintTimePredictionRequest
            {
                GeometryFileStream = fileStream,
                FileName = geometryFile.FileName,
                MaterialType = materialType,
                MaterialDensity = materialDensity,
                PrinterType = printerType,
                PrintSpeed = printSpeed,
                LayerHeight = layerHeight,
                NozzleTemperature = nozzleTemperature,
                BedTemperature = bedTemperature,
                InfillPercentage = infillPercentage
            };

            // Dispatch command via MediatR
            var command = new PredictPrintTimeCommand
            {
                Request = request,
                UserId = userId,
                CorrelationId = correlationId
            };

            var response = await _mediator.Send(command, cancellationToken);

            _logger.LogInformation(
                "Print time prediction successful. Predicted: {Time:F2} min, Cache: {CacheStatus}, CorrelationId: {CorrelationId}",
                response.PredictedValue,
                response.CacheStatus,
                correlationId);

            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation error in print time prediction. CorrelationId: {CorrelationId}", correlationId);
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Validation Error",
                Detail = ex.Message,
                Instance = HttpContext.Request.Path
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Service error in print time prediction. CorrelationId: {CorrelationId}", correlationId);
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new ProblemDetails
            {
                Status = StatusCodes.Status503ServiceUnavailable,
                Title = "Service Unavailable",
                Detail = ex.Message,
                Instance = HttpContext.Request.Path
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in print time prediction. CorrelationId: {CorrelationId}", correlationId);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Internal Server Error",
                Detail = "An unexpected error occurred while processing the prediction request.",
                Instance = HttpContext.Request.Path
            });
        }
    }

    /// <summary>
    /// Predict demand forecast for a product over specified horizon.
    /// </summary>
    /// <remarks>
    /// Generates time-series demand forecasts with anomaly detection for product planning.
    ///
    /// **Sample Request:**
    /// ```json
    /// POST /predictionservice/v1/predictions/demand-forecast
    /// Content-Type: application/json
    ///
    /// {
    ///   "productId": "PROD-12345",
    ///   "horizon": 30,
    ///   "granularity": "daily",
    ///   "baselineDate": "2026-02-14"
    /// }
    /// ```
    ///
    /// **Sample Response:**
    /// ```json
    /// {
    ///   "predictedValue": 850.5,
    ///   "unit": "units",
    ///   "confidenceLower": 720.3,
    ///   "confidenceUpper": 980.7,
    ///   "explanation": "Demand forecast for product PROD-12345 over 30 days (daily granularity). Generated 30 forecast points. Average demand: 850.5 units (range: 720.3 - 980.7)",
    ///   "modelVersion": "1.0.0",
    ///   "cacheStatus": "miss",
    ///   "timestamp": "2026-02-14T10:30:00Z",
    ///   "metadata": {
    ///     "product_id": "PROD-12345",
    ///     "horizon_days": 30,
    ///     "granularity": "daily",
    ///     "forecast_count": 30,
    ///     "anomaly_count": 2
    ///   }
    /// }
    /// ```
    /// </remarks>
    /// <param name="request">Demand forecast request with product ID, horizon, and granularity.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Prediction response with forecasted demand and confidence intervals.</returns>
    /// <response code="200">Returns the demand forecast with time-series predictions.</response>
    /// <response code="400">Invalid request (invalid horizon, unsupported granularity, missing product ID).</response>
    /// <response code="401">Unauthorized - authentication required.</response>
    /// <response code="403">Forbidden - insufficient permissions.</response>
    /// <response code="500">Internal server error during forecasting.</response>
    /// <response code="503">Service unavailable - no active model deployed.</response>
    [HttpPost("demand-forecast")]
    [RequirePermission(PredictionPermissions.PredictionsCreate)]
    [EnableRateLimiting("predictions")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(PredictionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> PredictDemandForecast(
        [FromBody, Required] DemandForecastRequest request,
        CancellationToken cancellationToken = default)
    {
        var correlationId = HttpContext.TraceIdentifier;
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "anonymous";

        _logger.LogInformation(
            "Demand forecast request received. ProductId: {ProductId}, Horizon: {Horizon}, User: {UserId}, CorrelationId: {CorrelationId}",
            request.ProductId,
            request.Horizon,
            userId,
            correlationId);

        try
        {
            // Dispatch command via MediatR
            var command = new PredictDemandCommand
            {
                Request = request,
                UserId = userId,
                CorrelationId = correlationId
            };

            var response = await _mediator.Send(command, cancellationToken);

            _logger.LogInformation(
                "Demand forecast successful. Avg forecast: {Forecast:F2} units, Horizon: {Horizon} days, Cache: {CacheStatus}, CorrelationId: {CorrelationId}",
                response.PredictedValue,
                request.Horizon,
                response.CacheStatus,
                correlationId);

            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation error in demand forecast. ProductId: {ProductId}, CorrelationId: {CorrelationId}",
                request.ProductId, correlationId);
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Validation Error",
                Detail = ex.Message,
                Instance = HttpContext.Request.Path
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Service error in demand forecast. ProductId: {ProductId}, CorrelationId: {CorrelationId}",
                request.ProductId, correlationId);
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new ProblemDetails
            {
                Status = StatusCodes.Status503ServiceUnavailable,
                Title = "Service Unavailable",
                Detail = ex.Message,
                Instance = HttpContext.Request.Path
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in demand forecast. ProductId: {ProductId}, CorrelationId: {CorrelationId}",
                request.ProductId, correlationId);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Internal Server Error",
                Detail = "An unexpected error occurred while processing the forecast request.",
                Instance = HttpContext.Request.Path
            });
        }
    }
}
