namespace Maliev.PredictionService.Domain.ValueObjects;

/// <summary>
/// Semantic versioning for ML models
/// Immutable value object
/// Format: Major.Minor.Patch
/// </summary>
public record ModelVersion
{
    /// <summary>
    /// Major version (breaking changes, algorithm changes)
    /// </summary>
    public required int Major { get; init; }

    /// <summary>
    /// Minor version (feature additions, non-breaking improvements)
    /// </summary>
    public required int Minor { get; init; }

    /// <summary>
    /// Patch version (bug fixes, minor adjustments)
    /// </summary>
    public required int Patch { get; init; }

    /// <summary>
    /// Returns the version as a string (e.g., "1.2.3")
    /// </summary>
    public override string ToString() => $"{Major}.{Minor}.{Patch}";

    /// <summary>
    /// Parses a version string into a ModelVersion object
    /// </summary>
    public static ModelVersion Parse(string versionString)
    {
        var parts = versionString.Split('.');
        if (parts.Length != 3)
            throw new ArgumentException($"Invalid version format: {versionString}. Expected format: Major.Minor.Patch");

        return new ModelVersion
        {
            Major = int.Parse(parts[0]),
            Minor = int.Parse(parts[1]),
            Patch = int.Parse(parts[2])
        };
    }

    /// <summary>
    /// Creates the next major version (increments Major, resets Minor and Patch)
    /// </summary>
    public ModelVersion NextMajor() => this with { Major = Major + 1, Minor = 0, Patch = 0 };

    /// <summary>
    /// Creates the next minor version (increments Minor, resets Patch)
    /// </summary>
    public ModelVersion NextMinor() => this with { Minor = Minor + 1, Patch = 0 };

    /// <summary>
    /// Creates the next patch version (increments Patch)
    /// </summary>
    public ModelVersion NextPatch() => this with { Patch = Patch + 1 };
}
