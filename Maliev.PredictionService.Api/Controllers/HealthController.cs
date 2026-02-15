using Microsoft.AspNetCore.Mvc;
using Maliev.PredictionService.Infrastructure.Persistence;
using StackExchange.Redis;

namespace Maliev.PredictionService.Api.Controllers;

/// <summary>
/// Health check endpoints for Kubernetes probes
/// </summary>
[ApiController]
[Route("predictionservice")]
public class HealthController : ControllerBase
{
    private readonly PredictionDbContext _dbContext;
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<HealthController> _logger;

    public HealthController(
        PredictionDbContext dbContext,
        IConnectionMultiplexer redis,
        ILogger<HealthController> logger)
    {
        _dbContext = dbContext;
        _redis = redis;
        _logger = logger;
    }

    /// <summary>
    /// Liveness probe - indicates if the service is alive
    /// Returns 200 if the service is running, regardless of dependency health
    /// </summary>
    [HttpGet("liveness")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Liveness()
    {
        return Ok(new { status = "alive", timestamp = DateTime.UtcNow });
    }

    /// <summary>
    /// Readiness probe - indicates if the service is ready to accept traffic
    /// Checks database and cache connectivity
    /// </summary>
    [HttpGet("readiness")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Readiness(CancellationToken cancellationToken)
    {
        var checks = new Dictionary<string, bool>();

        // Check database
        try
        {
            await _dbContext.Database.CanConnectAsync(cancellationToken);
            checks["database"] = true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Database readiness check failed");
            checks["database"] = false;
        }

        // Check Redis
        try
        {
            var db = _redis.GetDatabase();
            await db.PingAsync();
            checks["redis"] = true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis readiness check failed");
            checks["redis"] = false;
        }

        var isReady = checks.Values.All(v => v);

        if (isReady)
        {
            return Ok(new { status = "ready", checks, timestamp = DateTime.UtcNow });
        }

        return StatusCode(StatusCodes.Status503ServiceUnavailable,
            new { status = "not_ready", checks, timestamp = DateTime.UtcNow });
    }

    /// <summary>
    /// Health check - comprehensive health status
    /// </summary>
    [HttpGet("health")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Health(CancellationToken cancellationToken)
    {
        // Reuse readiness checks
        var readinessResult = await Readiness(cancellationToken);

        if (readinessResult is OkObjectResult okResult)
        {
            return Ok(okResult.Value);
        }

        return readinessResult;
    }
}
