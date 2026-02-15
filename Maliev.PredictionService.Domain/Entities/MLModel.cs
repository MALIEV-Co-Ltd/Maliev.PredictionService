using Maliev.PredictionService.Domain.Enums;
using Maliev.PredictionService.Domain.ValueObjects;

namespace Maliev.PredictionService.Domain.Entities;

/// <summary>
/// ML Model entity - represents a trained model in the model registry
/// Schema: ml_models
/// </summary>
public class MLModel
{
    /// <summary>
    /// Unique identifier for the model
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Type of prediction this model performs
    /// </summary>
    public ModelType ModelType { get; set; }

    /// <summary>
    /// Semantic version of the model
    /// </summary>
    public required ModelVersion ModelVersion { get; set; }

    /// <summary>
    /// Current lifecycle status
    /// </summary>
    public ModelStatus Status { get; set; }

    /// <summary>
    /// Performance metrics from validation
    /// </summary>
    public PerformanceMetrics? PerformanceMetrics { get; set; }

    /// <summary>
    /// Date and time when the model was trained
    /// </summary>
    public DateTime TrainingDate { get; set; }

    /// <summary>
    /// Date and time when the model was deployed to Active status
    /// Null if never deployed
    /// </summary>
    public DateTime? DeploymentDate { get; set; }

    /// <summary>
    /// File path to the serialized model binary (ML.NET ZIP or ONNX)
    /// </summary>
    public required string FilePath { get; set; }

    /// <summary>
    /// Reference to the training job that created this model
    /// </summary>
    public Guid? TrainingJobId { get; set; }

    /// <summary>
    /// Navigation property to training job
    /// </summary>
    public TrainingJob? TrainingJob { get; set; }

    /// <summary>
    /// Algorithm used for training (e.g., "FastTreeRegression", "SSA")
    /// </summary>
    public string? Algorithm { get; set; }

    /// <summary>
    /// Metadata and hyperparameters (stored as JSONB in PostgreSQL)
    /// </summary>
    public Dictionary<string, object>? Metadata { get; set; }

    /// <summary>
    /// Audit fields
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
}
