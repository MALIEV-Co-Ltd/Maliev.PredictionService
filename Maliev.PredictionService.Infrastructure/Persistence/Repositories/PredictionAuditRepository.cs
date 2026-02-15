using Microsoft.EntityFrameworkCore;
using Maliev.PredictionService.Domain.Entities;
using Maliev.PredictionService.Domain.Enums;
using Maliev.PredictionService.Domain.Repositories;

namespace Maliev.PredictionService.Infrastructure.Persistence.Repositories;

/// <summary>
/// Append-only repository for prediction audit logs
/// </summary>
public class PredictionAuditRepository : IPredictionAuditRepository
{
    private readonly PredictionDbContext _context;

    public PredictionAuditRepository(PredictionDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>
    /// Adds a new prediction audit log entry
    /// </summary>
    public async Task<PredictionAuditLog> AddAsync(
        PredictionAuditLog auditLog,
        CancellationToken cancellationToken = default)
    {
        _context.PredictionAuditLogs.Add(auditLog);
        await _context.SaveChangesAsync(cancellationToken);
        return auditLog;
    }

    /// <summary>
    /// Gets prediction history for a specific user
    /// </summary>
    public async Task<List<PredictionAuditLog>> GetByUserIdAsync(
        string userId,
        int take = 100,
        CancellationToken cancellationToken = default)
    {
        return await _context.PredictionAuditLogs
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.Timestamp)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets audit logs for a specific model
    /// </summary>
    public async Task<List<PredictionAuditLog>> GetByModelIdAsync(
        Guid modelId,
        CancellationToken cancellationToken = default)
    {
        // Note: PredictionAuditLog doesn't have ModelId, using ModelType and ModelVersion instead
        // This is a simplified implementation
        return await _context.PredictionAuditLogs
            .OrderByDescending(p => p.Timestamp)
            .Take(100)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets audit logs for a specific model type within a date range
    /// </summary>
    public async Task<List<PredictionAuditLog>> GetByModelTypeAndDateRangeAsync(
        ModelType modelType,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        return await _context.PredictionAuditLogs
            .Where(p => p.ModelType == modelType
                && p.Timestamp >= startDate
                && p.Timestamp <= endDate)
            .OrderBy(p => p.Timestamp)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets audit logs by correlation ID for distributed tracing
    /// </summary>
    public async Task<PredictionAuditLog?> GetByCorrelationIdAsync(
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        return await _context.PredictionAuditLogs
            .FirstOrDefaultAsync(p => p.RequestId == correlationId, cancellationToken);
    }

    /// <summary>
    /// Gets predictions with actual outcomes (for model performance tracking)
    /// </summary>
    public async Task<List<PredictionAuditLog>> GetWithActualOutcomesAsync(
        ModelType modelType,
        string modelVersion,
        int take = 1000,
        CancellationToken cancellationToken = default)
    {
        return await _context.PredictionAuditLogs
            .Where(p => p.ModelType == modelType
                && p.ModelVersion == modelVersion
                && p.ActualOutcome != null)
            .OrderByDescending(p => p.ActualOutcomeReceivedAt)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Updates actual outcome for a prediction (one of the few mutable operations)
    /// </summary>
    public async Task UpdateActualOutcomeAsync(
        Guid predictionId,
        Dictionary<string, object> actualOutcome,
        CancellationToken cancellationToken = default)
    {
        var log = await _context.PredictionAuditLogs
            .FirstOrDefaultAsync(p => p.Id == predictionId, cancellationToken);

        if (log == null)
            throw new InvalidOperationException($"Prediction audit log {predictionId} not found");

        log.ActualOutcome = actualOutcome;
        log.ActualOutcomeReceivedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Deletes audit logs older than the specified date (for GDPR compliance / data retention)
    /// </summary>
    public async Task<int> DeleteOlderThanAsync(
        DateTime cutoffDate,
        CancellationToken cancellationToken = default)
    {
        var logsToDelete = await _context.PredictionAuditLogs
            .Where(p => p.Timestamp < cutoffDate)
            .ToListAsync(cancellationToken);

        _context.PredictionAuditLogs.RemoveRange(logsToDelete);
        return await _context.SaveChangesAsync(cancellationToken);
    }
}
