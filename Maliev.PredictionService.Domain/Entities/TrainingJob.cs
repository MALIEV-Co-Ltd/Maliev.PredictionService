using Maliev.PredictionService.Domain.Enums;
using Maliev.PredictionService.Domain.ValueObjects;

namespace Maliev.PredictionService.Domain.Entities;

/// <summary>
/// Training Job entity - represents a model training execution
/// Schema: training
/// </summary>
public class TrainingJob
{
    /// <summary>
    /// Unique identifier for the training job
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Model type being trained
    /// </summary>
    public ModelType ModelType { get; set; }

    /// <summary>
    /// Current status of the training job
    /// </summary>
    public TrainingJobStatus Status { get; set; }

    /// <summary>
    /// Start timestamp
    /// </summary>
    public DateTime StartTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// End timestamp (null if still running)
    /// </summary>
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// Duration in seconds (calculated from StartTime and EndTime)
    /// </summary>
    public int? DurationSeconds => EndTime.HasValue
        ? (int)(EndTime.Value - StartTime).TotalSeconds
        : null;

    /// <summary>
    /// Performance metrics from validation
    /// </summary>
    public PerformanceMetrics? PerformanceMetrics { get; set; }

    /// <summary>
    /// Validation results and quality gate checks (stored as JSONB)
    /// </summary>
    public Dictionary<string, object>? ValidationResults { get; set; }

    /// <summary>
    /// Reference to the dataset used for training
    /// </summary>
    public Guid? TrainingDatasetId { get; set; }

    /// <summary>
    /// Navigation property to training dataset
    /// </summary>
    public TrainingDataset? TrainingDataset { get; set; }

    /// <summary>
    /// Reference to the model created by this job (if successful)
    /// </summary>
    public Guid? ModelId { get; set; }

    /// <summary>
    /// Navigation property to created model
    /// </summary>
    public MLModel? Model { get; set; }

    /// <summary>
    /// Error message if training failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Triggered by (manual, scheduled, auto-retrain)
    /// </summary>
    public required string TriggeredBy { get; set; }

    /// <summary>
    /// User ID who triggered the training (if manual)
    /// </summary>
    public string? TriggeredByUserId { get; set; }

    /// <summary>
    /// Hyperparameters used for training (stored as JSONB)
    /// </summary>
    public Dictionary<string, object>? Hyperparameters { get; set; }

    /// <summary>
    /// Training logs (optional, stored as text)
    /// </summary>
    public string? Logs { get; set; }
}

/// <summary>
/// Status of a training job
/// </summary>
public enum TrainingJobStatus
{
    /// <summary>
    /// Job is queued but not started yet
    /// </summary>
    Queued,

    /// <summary>
    /// Job is currently running
    /// </summary>
    Running,

    /// <summary>
    /// Job completed successfully and model is ready
    /// </summary>
    Completed,

    /// <summary>
    /// Job failed due to error
    /// </summary>
    Failed,

    /// <summary>
    /// Job was cancelled by user
    /// </summary>
    Cancelled
}
