using System.ComponentModel.DataAnnotations;

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
    [Required(ErrorMessage = "ProductId is required.")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "ProductId must be between 1 and 100 characters.")]
    [RegularExpression(@"^[a-zA-Z0-9\-_]+$", ErrorMessage = "ProductId can only contain alphanumeric characters, hyphens, and underscores.")]
    public required string ProductId { get; init; }

    /// <summary>
    /// Forecast horizon in days. Valid values: 7, 30, or 90.
    /// </summary>
    [Required(ErrorMessage = "Horizon is required.")]
    [Range(1, 365, ErrorMessage = "Horizon must be between 1 and 365 days.")]
    public required int Horizon { get; init; }

    /// <summary>
    /// Forecast granularity. Valid values: "daily" or "weekly".
    /// </summary>
    [Required(ErrorMessage = "Granularity is required.")]
    [RegularExpression(@"^(daily|weekly)$", ErrorMessage = "Granularity must be either 'daily' or 'weekly'.")]
    public required string Granularity { get; init; }

    /// <summary>
    /// Optional baseline date for forecast. Defaults to today if not provided.
    /// </summary>
    public DateTime? BaselineDate { get; init; }
}
