using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace Maliev.PredictionService.Api.Validation;

/// <summary>
/// Validates and sanitizes string inputs to prevent injection attacks.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = false)]
public partial class SanitizedStringAttribute : ValidationAttribute
{
    /// <summary>
    /// Maximum allowed length for the string.
    /// </summary>
    public int MaxLength { get; set; } = 200;

    /// <summary>
    /// Allowed pattern (regex). If specified, input must match this pattern.
    /// </summary>
    public string? AllowedPattern { get; set; }

    /// <summary>
    /// Whether to allow special characters (default: false for security).
    /// </summary>
    public bool AllowSpecialCharacters { get; set; } = false;

    [GeneratedRegex(@"[<>""'%;()&+]")]
    private static partial Regex DangerousCharactersRegex();

    [GeneratedRegex(@"^[a-zA-Z0-9\s\-_.]+$")]
    private static partial Regex SafeCharactersRegex();

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value == null)
        {
            return ValidationResult.Success; // Allow null, use [Required] for non-null
        }

        if (value is not string input)
        {
            return new ValidationResult("Value must be a string.");
        }

        // Check length
        if (input.Length > MaxLength)
        {
            return new ValidationResult($"String exceeds maximum length of {MaxLength} characters.");
        }

        // Check for dangerous characters if special characters are not allowed
        if (!AllowSpecialCharacters && DangerousCharactersRegex().IsMatch(input))
        {
            return new ValidationResult("String contains potentially dangerous characters: < > \" ' % ; ( ) & +");
        }

        // Validate against safe characters pattern if special characters are not allowed
        if (!AllowSpecialCharacters && !SafeCharactersRegex().IsMatch(input))
        {
            return new ValidationResult("String contains invalid characters. Only alphanumeric, spaces, hyphens, underscores, and periods are allowed.");
        }

        // Validate against custom pattern if specified
        if (!string.IsNullOrEmpty(AllowedPattern))
        {
            var regex = new Regex(AllowedPattern, RegexOptions.Compiled);
            if (!regex.IsMatch(input))
            {
                return new ValidationResult($"String does not match the required pattern.");
            }
        }

        return ValidationResult.Success;
    }
}
