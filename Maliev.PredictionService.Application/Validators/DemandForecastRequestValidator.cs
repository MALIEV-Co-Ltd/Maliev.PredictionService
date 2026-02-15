using Maliev.PredictionService.Application.DTOs.Requests;

namespace Maliev.PredictionService.Application.Validators;

/// <summary>
/// Custom validator for DemandForecastRequest.
/// Validates horizon (7, 30, or 90 days) and granularity ("daily" or "weekly").
/// NO FluentValidation - manual validation following project rules.
/// </summary>
public class DemandForecastRequestValidator
{
    private static readonly HashSet<int> ValidHorizons = new() { 7, 30, 90 };
    private static readonly HashSet<string> ValidGranularities = new(StringComparer.OrdinalIgnoreCase) { "daily", "weekly" };

    /// <summary>
    /// Validates the DemandForecastRequest.
    /// </summary>
    /// <returns>List of validation error messages. Empty list if valid.</returns>
    public List<string> Validate(DemandForecastRequest request)
    {
        var errors = new List<string>();

        if (request == null)
        {
            errors.Add("Request cannot be null");
            return errors;
        }

        // Validate ProductId
        if (string.IsNullOrWhiteSpace(request.ProductId))
        {
            errors.Add("ProductId is required and cannot be empty");
        }
        else if (request.ProductId.Length > 100)
        {
            errors.Add("ProductId must not exceed 100 characters");
        }

        // Validate Horizon
        if (!ValidHorizons.Contains(request.Horizon))
        {
            errors.Add($"Horizon must be one of: {string.Join(", ", ValidHorizons)} days. Got: {request.Horizon}");
        }

        // Validate Granularity
        if (string.IsNullOrWhiteSpace(request.Granularity))
        {
            errors.Add("Granularity is required and cannot be empty");
        }
        else if (!ValidGranularities.Contains(request.Granularity))
        {
            errors.Add($"Granularity must be 'daily' or 'weekly'. Got: {request.Granularity}");
        }

        // Validate BaselineDate
        if (request.BaselineDate.HasValue)
        {
            if (request.BaselineDate.Value > DateTime.UtcNow)
            {
                errors.Add("BaselineDate cannot be in the future");
            }

            // Baseline date should not be more than 2 years in the past
            if (request.BaselineDate.Value < DateTime.UtcNow.AddYears(-2))
            {
                errors.Add("BaselineDate cannot be more than 2 years in the past");
            }
        }

        // Validate granularity/horizon compatibility
        if (request.Granularity?.Equals("weekly", StringComparison.OrdinalIgnoreCase) == true && request.Horizon == 7)
        {
            errors.Add("Weekly granularity requires a horizon of at least 30 days");
        }

        return errors;
    }

    /// <summary>
    /// Validates the request and throws an exception if validation fails.
    /// </summary>
    public void ValidateAndThrow(DemandForecastRequest request)
    {
        var errors = Validate(request);
        if (errors.Any())
        {
            throw new ArgumentException($"Validation failed: {string.Join("; ", errors)}");
        }
    }
}
