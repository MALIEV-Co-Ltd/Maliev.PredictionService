namespace Maliev.PredictionService.Application.DTOs.Requests;

/// <summary>
/// Request DTO for 3D print time prediction.
/// </summary>
public record PrintTimePredictionRequest
{
    /// <summary>
    /// 3D geometry file (STL binary format).
    /// Uploaded as multipart/form-data.
    /// </summary>
    public required Stream GeometryFileStream { get; init; }

    /// <summary>
    /// Original file name (e.g., "part-v2.stl").
    /// </summary>
    public required string FileName { get; init; }

    /// <summary>
    /// Material type for the print job.
    /// Example values: "PLA", "ABS", "PETG", "TPU", "Nylon".
    /// </summary>
    public required string MaterialType { get; init; }

    /// <summary>
    /// Material density in g/cm³.
    /// PLA: ~1.25, ABS: ~1.05, PETG: ~1.27, Nylon: ~1.14
    /// </summary>
    public required float MaterialDensity { get; init; }

    /// <summary>
    /// 3D printer model identifier.
    /// Example values: "Prusa i3 MK3S+", "Ultimaker S5", "Formlabs Form 3".
    /// </summary>
    public required string PrinterType { get; init; }

    /// <summary>
    /// Print speed in mm/s.
    /// Typical range: 30-80 mm/s (slower for quality, faster for drafts).
    /// </summary>
    public required float PrintSpeed { get; init; }

    /// <summary>
    /// Layer height in mm.
    /// Common values: 0.1, 0.15, 0.2, 0.3
    /// Lower = higher quality but longer print time.
    /// </summary>
    public required float LayerHeight { get; init; }

    /// <summary>
    /// Nozzle temperature in °C.
    /// PLA: 190-220°C, ABS: 220-250°C, PETG: 220-250°C
    /// </summary>
    public required float NozzleTemperature { get; init; }

    /// <summary>
    /// Heated bed temperature in °C.
    /// PLA: 50-60°C, ABS: 90-110°C, PETG: 70-85°C
    /// </summary>
    public required float BedTemperature { get; init; }

    /// <summary>
    /// Infill percentage (0-100).
    /// 0% = hollow, 100% = solid.
    /// Typical range: 10-30% for most parts.
    /// </summary>
    public required float InfillPercentage { get; init; }
}
