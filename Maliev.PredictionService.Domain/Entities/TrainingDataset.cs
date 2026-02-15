using Maliev.PredictionService.Domain.Enums;

namespace Maliev.PredictionService.Domain.Entities;

/// <summary>
/// Training Dataset entity - represents a dataset used for model training
/// Schema: training
/// </summary>
public class TrainingDataset
{
    /// <summary>
    /// Unique identifier for the dataset
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Model type this dataset is for
    /// </summary>
    public ModelType ModelType { get; set; }

    /// <summary>
    /// Total number of records in the dataset
    /// </summary>
    public int RecordCount { get; set; }

    /// <summary>
    /// Date range of the data (start date)
    /// </summary>
    public DateTime DateRangeStart { get; set; }

    /// <summary>
    /// Date range of the data (end date)
    /// </summary>
    public DateTime DateRangeEnd { get; set; }

    /// <summary>
    /// List of feature column names
    /// </summary>
    public required List<string> FeatureColumns { get; set; }

    /// <summary>
    /// Target column name (what we're predicting)
    /// </summary>
    public required string TargetColumn { get; set; }

    /// <summary>
    /// Data quality metrics (null percentage, outliers, etc.)
    /// Stored as JSONB in PostgreSQL
    /// </summary>
    public Dictionary<string, object>? DataQualityMetrics { get; set; }

    /// <summary>
    /// File path to the dataset (CSV, Parquet, etc.)
    /// </summary>
    public string? FilePath { get; set; }

    /// <summary>
    /// SHA-256 hash of the dataset for deduplication
    /// </summary>
    public string? DatasetHash { get; set; }

    /// <summary>
    /// Reference to the training job that used this dataset
    /// </summary>
    public Guid? TrainingJobId { get; set; }

    /// <summary>
    /// Navigation property to training job
    /// </summary>
    public TrainingJob? TrainingJob { get; set; }

    /// <summary>
    /// Audit fields
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? CreatedBy { get; set; }
}
