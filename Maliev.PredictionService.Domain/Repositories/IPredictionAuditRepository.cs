using Maliev.PredictionService.Domain.Entities;
using Maliev.PredictionService.Domain.Enums;

namespace Maliev.PredictionService.Domain.Repositories;

/// <summary>
/// Repository interface for PredictionAuditLog entity operations.
/// </summary>
public interface IPredictionAuditRepository
{
    /// <summary>
    /// Adds a new prediction audit log entry.
    /// </summary>
    Task<PredictionAuditLog> AddAsync(PredictionAuditLog auditLog, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets audit logs for a specific model.
    /// </summary>
    Task<List<PredictionAuditLog>> GetByModelIdAsync(Guid modelId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets audit logs for a specific user.
    /// </summary>
    Task<List<PredictionAuditLog>> GetByUserIdAsync(string userId, int limit = 100, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets audit logs for a specific model type within a date range.
    /// </summary>
    Task<List<PredictionAuditLog>> GetByModelTypeAndDateRangeAsync(
        ModelType modelType,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets audit logs by correlation ID for distributed tracing.
    /// </summary>
    Task<PredictionAuditLog?> GetByCorrelationIdAsync(string correlationId, CancellationToken cancellationToken = default);
}
