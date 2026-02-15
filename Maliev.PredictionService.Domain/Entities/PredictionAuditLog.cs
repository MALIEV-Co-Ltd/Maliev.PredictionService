using Maliev.PredictionService.Domain.Enums;

namespace Maliev.PredictionService.Domain.Entities;

/// <summary>
/// Prediction Audit Log entity - immutable append-only audit trail
/// Schema: audit (monthly partitioned)
/// </summary>
public class PredictionAuditLog
{
    /// <summary>
    /// Unique identifier for this prediction request
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Correlation ID for distributed tracing
    /// </summary>
    public required string RequestId { get; set; }

    /// <summary>
    /// Model type used for this prediction
    /// </summary>
    public ModelType ModelType { get; set; }

    /// <summary>
    /// Model version used (e.g., "2.1.0")
    /// </summary>
    public required string ModelVersion { get; set; }

    /// <summary>
    /// Input features (stored as JSONB)
    /// </summary>
    public required Dictionary<string, object> InputFeatures { get; set; }

    /// <summary>
    /// Output prediction (stored as JSONB)
    /// </summary>
    public required Dictionary<string, object> OutputPrediction { get; set; }

    /// <summary>
    /// Whether this prediction was served from cache
    /// </summary>
    public PredictionStatus CacheStatus { get; set; }

    /// <summary>
    /// Response time in milliseconds
    /// </summary>
    public int ResponseTimeMs { get; set; }

    /// <summary>
    /// User ID who requested the prediction
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Tenant ID for multi-tenancy support
    /// </summary>
    public string? TenantId { get; set; }

    /// <summary>
    /// Timestamp when the prediction was made
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Actual outcome (if available later for model performance tracking)
    /// Null until ground truth is received
    /// </summary>
    public Dictionary<string, object>? ActualOutcome { get; set; }

    /// <summary>
    /// Date when actual outcome was received
    /// </summary>
    public DateTime? ActualOutcomeReceivedAt { get; set; }

    /// <summary>
    /// Error message if prediction failed
    /// </summary>
    public string? ErrorMessage { get; set; }
}
