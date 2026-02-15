namespace Maliev.PredictionService.Application.DTOs.Requests;

/// <summary>
/// Request DTO for demand forecasting predictions.
/// Supports 7, 30, or 90-day horizons with daily or weekly granularity.
/// </summary>
public record DemandForecastRequest
{
    /// <summary>
    /// Product identifier for which to forecast demand.
    /// </summary>
    public required string ProductId { get; init; }

    /// <summary>
    /// Forecast horizon in days. Valid values: 7, 30, or 90.
    /// </summary>
    public required int Horizon { get; init; }

    /// <summary>
    /// Forecast granularity. Valid values: "daily" or "weekly".
    /// </summary>
    public required string Granularity { get; init; }

    /// <summary>
    /// Optional baseline date for forecast. Defaults to today if not provided.
    /// </summary>
    public DateTime? BaselineDate { get; init; }
}
