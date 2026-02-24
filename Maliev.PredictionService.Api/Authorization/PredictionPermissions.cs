namespace Maliev.PredictionService.Api.Authorization;

/// <summary>
/// Defines all permissions for the Prediction Service.
/// Permissions follow the format: predictionservice.{resource}.{action}
/// </summary>
public static class PredictionPermissions
{
    // Prediction Operations
    /// <summary>Permission to request predictions (3D print time, demand forecast, price optimization).</summary>
    public const string PredictionsCreate = "predictionservice.predictions.create";
    /// <summary>Permission to view prediction results and history.</summary>
    public const string PredictionsRead = "predictionservice.predictions.read";
    /// <summary>Permission to export prediction data.</summary>
    public const string PredictionsExport = "predictionservice.predictions.export";

    // Model Registry Operations
    /// <summary>Permission to view ML model metadata, versions, and performance metrics.</summary>
    public const string ModelsRead = "predictionservice.models.read";
    /// <summary>Permission to update model metadata or lifecycle status.</summary>
    public const string ModelsUpdate = "predictionservice.models.update";
    /// <summary>Permission to deploy models to production.</summary>
    public const string ModelsDeploy = "predictionservice.models.deploy";
    /// <summary>Permission to deprecate or archive models.</summary>
    public const string ModelsArchive = "predictionservice.models.archive";

    // Training Operations
    /// <summary>Permission to trigger model training jobs.</summary>
    public const string TrainingCreate = "predictionservice.training.create";
    /// <summary>Permission to view training job status and results.</summary>
    public const string TrainingRead = "predictionservice.training.read";
    /// <summary>Permission to cancel running training jobs.</summary>
    public const string TrainingCancel = "predictionservice.training.cancel";

    // Dataset Operations
    /// <summary>Permission to upload training datasets.</summary>
    public const string DatasetsCreate = "predictionservice.datasets.create";
    /// <summary>Permission to view dataset metadata and statistics.</summary>
    public const string DatasetsRead = "predictionservice.datasets.read";
    /// <summary>Permission to delete training datasets.</summary>
    public const string DatasetsDelete = "predictionservice.datasets.delete";

    // Audit and Monitoring
    /// <summary>Permission to view prediction audit logs.</summary>
    public const string AuditRead = "predictionservice.audit.read";
    /// <summary>Permission to export audit logs.</summary>
    public const string AuditExport = "predictionservice.audit.export";

    /// <summary>
    /// Collection of all defined prediction service permissions with descriptions.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, string> AllWithDescriptions = new Dictionary<string, string>
    {
        { PredictionsCreate, "Request ML predictions (print time, demand, price)" },
        { PredictionsRead, "View prediction results and history" },
        { PredictionsExport, "Export prediction data" },
        { ModelsRead, "View ML model metadata and performance metrics" },
        { ModelsUpdate, "Update model metadata or lifecycle status" },
        { ModelsDeploy, "Deploy models to production" },
        { ModelsArchive, "Deprecate or archive models" },
        { TrainingCreate, "Trigger model training jobs" },
        { TrainingRead, "View training job status and results" },
        { TrainingCancel, "Cancel running training jobs" },
        { DatasetsCreate, "Upload training datasets" },
        { DatasetsRead, "View dataset metadata and statistics" },
        { DatasetsDelete, "Delete training datasets" },
        { AuditRead, "View prediction audit logs" },
        { AuditExport, "Export audit logs" }
    };

    /// <summary>
    /// All permissions defined for the Prediction Service.
    /// </summary>
    public static readonly string[] All = [.. AllWithDescriptions.Keys];
}
