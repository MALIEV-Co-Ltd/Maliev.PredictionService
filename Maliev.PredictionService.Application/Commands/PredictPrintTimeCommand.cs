using Maliev.PredictionService.Application.DTOs.Requests;
using Maliev.PredictionService.Application.DTOs.Responses;
using MediatR;

namespace Maliev.PredictionService.Application.Commands;

/// <summary>
/// CQRS Command for print time prediction.
/// Encapsulates the request and returns a prediction response.
/// </summary>
public record PredictPrintTimeCommand : IRequest<PredictionResponse>
{
    public required PrintTimePredictionRequest Request { get; init; }

    /// <summary>
    /// User ID making the prediction request (from JWT claims).
    /// Used for audit logging and cache key generation.
    /// </summary>
    public required string UserId { get; init; }

    /// <summary>
    /// Correlation ID for distributed tracing.
    /// </summary>
    public required string CorrelationId { get; init; }
}
