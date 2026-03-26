namespace Maliev.PredictionService.Application.Authorization;

/// <summary>
/// Defines the permissions for the Prediction Service.
/// </summary>
public static class PredictionPermissions
{
    public const string ModelCreate = "prediction.models.create";
    public const string ModelRead = "prediction.models.read";
    public const string ModelUpdate = "prediction.models.update";
    public const string ModelDelete = "prediction.models.delete";
    public const string ModelDeploy = "prediction.models.deploy";

    public const string DatasetCreate = "prediction.datasets.create";
    public const string DatasetRead = "prediction.datasets.read";
    public const string DatasetDelete = "prediction.datasets.delete";

    public const string JobCreate = "prediction.jobs.create";
    public const string JobRead = "prediction.jobs.read";
    public const string JobCancel = "prediction.jobs.cancel";

    public const string ForecastRead = "prediction.forecasts.read";
    public const string ForecastExport = "prediction.forecasts.export";

    public const string Extract = "prediction.extractions.extract";

    public static readonly IReadOnlyDictionary<string, string> AllWithDescriptions = new Dictionary<string, string>
    {
        { ModelCreate, "Create prediction models" },
        { ModelRead, "Read prediction models" },
        { ModelUpdate, "Update prediction models" },
        { ModelDelete, "Delete prediction models" },
        { ModelDeploy, "Deploy prediction models" },
        { DatasetCreate, "Create prediction datasets" },
        { DatasetRead, "Read prediction datasets" },
        { DatasetDelete, "Delete prediction datasets" },
        { JobCreate, "Create prediction jobs" },
        { JobRead, "Read prediction jobs" },
        { JobCancel, "Cancel prediction jobs" },
        { ForecastRead, "Read forecasts" },
        { ForecastExport, "Export forecasts" },
        { Extract, "Extract prediction data" },
    };

    public static string[] All => AllWithDescriptions.Keys.ToArray();
}
