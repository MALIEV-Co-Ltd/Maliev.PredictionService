using System.ComponentModel.DataAnnotations;

namespace Maliev.PredictionService.Api.Validation;

/// <summary>
/// Validates uploaded files for size, extension, and content type.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = false)]
public class ValidFileAttribute : ValidationAttribute
{
    /// <summary>
    /// Maximum file size in bytes (default: 50 MB).
    /// </summary>
    public long MaxFileSize { get; set; } = 50 * 1024 * 1024; // 50 MB

    /// <summary>
    /// Allowed file extensions (lowercase, with dot). Example: [".stl", ".obj"]
    /// </summary>
    public string[]? AllowedExtensions { get; set; }

    /// <summary>
    /// Allowed MIME content types. Example: ["application/octet-stream", "model/stl"]
    /// </summary>
    public string[]? AllowedContentTypes { get; set; }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is not IFormFile file)
        {
            return new ValidationResult("Invalid file upload.");
        }

        // Check file size
        if (file.Length > MaxFileSize)
        {
            var maxSizeMB = MaxFileSize / (1024.0 * 1024.0);
            return new ValidationResult($"File size exceeds maximum allowed size of {maxSizeMB:F1} MB.");
        }

        // Check if file is empty
        if (file.Length == 0)
        {
            return new ValidationResult("File is empty.");
        }

        // Check file extension
        if (AllowedExtensions != null && AllowedExtensions.Length > 0)
        {
            var extension = Path.GetExtension(file.FileName)?.ToLowerInvariant();
            if (string.IsNullOrEmpty(extension) || !AllowedExtensions.Contains(extension))
            {
                var allowedList = string.Join(", ", AllowedExtensions);
                return new ValidationResult($"File extension '{extension}' is not allowed. Allowed extensions: {allowedList}");
            }
        }

        // Check content type
        if (AllowedContentTypes != null && AllowedContentTypes.Length > 0)
        {
            if (string.IsNullOrEmpty(file.ContentType) || !AllowedContentTypes.Contains(file.ContentType.ToLowerInvariant()))
            {
                var allowedList = string.Join(", ", AllowedContentTypes);
                return new ValidationResult($"Content type '{file.ContentType}' is not allowed. Allowed types: {allowedList}");
            }
        }

        return ValidationResult.Success;
    }
}
