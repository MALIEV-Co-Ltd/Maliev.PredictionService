using Maliev.PredictionService.Application.DTOs.Requests;

namespace Maliev.PredictionService.Application.Validators;

/// <summary>
/// Manual validator for PrintTimePredictionRequest.
/// Validates file constraints, parameter ranges, and business rules.
/// </summary>
public class PrintTimeRequestValidator
{
    private const long MaxFileSizeBytes = 50 * 1024 * 1024; // 50 MB
    private static readonly HashSet<string> SupportedFormats = new(StringComparer.OrdinalIgnoreCase)
    {
        ".stl"
    };

    private static readonly HashSet<string> SupportedMaterials = new(StringComparer.OrdinalIgnoreCase)
    {
        "PLA", "ABS", "PETG", "TPU", "Nylon", "HIPS", "ASA", "PC"
    };

    /// <summary>
    /// Validation result.
    /// </summary>
    public record ValidationResult
    {
        public required bool IsValid { get; init; }
        public required List<string> Errors { get; init; }

        public static ValidationResult Success() => new()
        {
            IsValid = true,
            Errors = new List<string>()
        };

        public static ValidationResult Failure(List<string> errors) => new()
        {
            IsValid = false,
            Errors = errors
        };
    }

    /// <summary>
    /// Validates print time prediction request.
    /// </summary>
    public ValidationResult Validate(PrintTimePredictionRequest request)
    {
        var errors = new List<string>();

        // File validations
        ValidateFile(request, errors);

        // Material validations
        ValidateMaterial(request, errors);

        // Printer parameter validations
        ValidatePrinterParameters(request, errors);

        return errors.Count == 0
            ? ValidationResult.Success()
            : ValidationResult.Failure(errors);
    }

    private static void ValidateFile(PrintTimePredictionRequest request, List<string> errors)
    {
        if (request.GeometryFileStream == null)
        {
            errors.Add("Geometry file is required");
            return;
        }

        if (string.IsNullOrWhiteSpace(request.FileName))
        {
            errors.Add("File name is required");
        }
        else
        {
            var extension = Path.GetExtension(request.FileName);
            if (!SupportedFormats.Contains(extension))
            {
                errors.Add($"Unsupported file format: {extension}. Supported formats: {string.Join(", ", SupportedFormats)}");
            }
        }

        if (request.GeometryFileStream.CanSeek && request.GeometryFileStream.Length > MaxFileSizeBytes)
        {
            var sizeInMB = request.GeometryFileStream.Length / 1024.0 / 1024.0;
            errors.Add($"File size ({sizeInMB:F2} MB) exceeds maximum allowed ({MaxFileSizeBytes / 1024 / 1024} MB)");
        }
    }

    private static void ValidateMaterial(PrintTimePredictionRequest request, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(request.MaterialType))
        {
            errors.Add("Material type is required");
        }
        else if (!SupportedMaterials.Contains(request.MaterialType))
        {
            errors.Add($"Unsupported material: {request.MaterialType}. Supported materials: {string.Join(", ", SupportedMaterials)}");
        }

        if (request.MaterialDensity <= 0 || request.MaterialDensity > 20)
        {
            errors.Add($"Material density must be between 0 and 20 g/cm³. Received: {request.MaterialDensity}");
        }
    }

    private static void ValidatePrinterParameters(PrintTimePredictionRequest request, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(request.PrinterType))
        {
            errors.Add("Printer type is required");
        }

        if (request.PrintSpeed <= 0 || request.PrintSpeed > 500)
        {
            errors.Add($"Print speed must be between 0 and 500 mm/s. Received: {request.PrintSpeed}");
        }

        if (request.LayerHeight <= 0 || request.LayerHeight > 1.0)
        {
            errors.Add($"Layer height must be between 0 and 1.0 mm. Received: {request.LayerHeight}");
        }

        if (request.NozzleTemperature < 150 || request.NozzleTemperature > 300)
        {
            errors.Add($"Nozzle temperature must be between 150 and 300 °C. Received: {request.NozzleTemperature}");
        }

        if (request.BedTemperature < 0 || request.BedTemperature > 150)
        {
            errors.Add($"Bed temperature must be between 0 and 150 °C. Received: {request.BedTemperature}");
        }

        if (request.InfillPercentage < 0 || request.InfillPercentage > 100)
        {
            errors.Add($"Infill percentage must be between 0 and 100. Received: {request.InfillPercentage}");
        }
    }
}
