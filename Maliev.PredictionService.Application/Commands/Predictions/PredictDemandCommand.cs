using Maliev.PredictionService.Application.DTOs.Requests;
using Maliev.PredictionService.Application.DTOs.Responses;
using MediatR;

namespace Maliev.PredictionService.Application.Commands.Predictions;

/// <summary>
/// Command to predict demand forecast for a product over specified horizon.
/// Uses CQRS pattern with MediatR.
/// </summary>
public record PredictDemandCommand : IRequest<PredictionResponse>
{
    /// <summary>
    /// Demand forecast request payload.
    /// </summary>
    public required DemandForecastRequest Request { get; init; }

    /// <summary>
    /// Optional user/tenant identifier for multi-tenancy and audit logging.
    /// </summary>
    public string? UserId { get; init; }

    /// <summary>
    /// Optional correlation ID for distributed tracing.
    /// </summary>
    public string? CorrelationId { get; init; }
}
