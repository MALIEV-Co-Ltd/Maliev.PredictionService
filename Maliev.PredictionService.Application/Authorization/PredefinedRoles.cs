namespace Maliev.PredictionService.Application.Authorization;

/// <summary>
/// Provides access to predefined roles for the Prediction Service.
/// </summary>
public static class PredictionPredefinedRoles
{
    public const string Admin = "roles.prediction.admin";
    public const string DataScientist = "roles.prediction.data-scientist";
    public const string Viewer = "roles.prediction.viewer";

    public static readonly IReadOnlyList<(string RoleId, string Description, string[] Permissions)> All = new List<(string, string, string[])>
    {
        (
            Admin,
            "Prediction Administrator with full access",
            new[]
            {
                PredictionPermissions.ModelCreate,
                PredictionPermissions.ModelRead,
                PredictionPermissions.ModelUpdate,
                PredictionPermissions.ModelDelete,
                PredictionPermissions.ModelDeploy,
                PredictionPermissions.DatasetCreate,
                PredictionPermissions.DatasetRead,
                PredictionPermissions.DatasetDelete,
                PredictionPermissions.JobCreate,
                PredictionPermissions.JobRead,
                PredictionPermissions.JobCancel,
                PredictionPermissions.ForecastRead,
                PredictionPermissions.ForecastExport,
                PredictionPermissions.Extract,
            }
        ),
        (
            DataScientist,
            "Data Scientist with model and dataset access",
            new[]
            {
                PredictionPermissions.ModelCreate,
                PredictionPermissions.ModelRead,
                PredictionPermissions.ModelUpdate,
                PredictionPermissions.ModelDeploy,
                PredictionPermissions.DatasetCreate,
                PredictionPermissions.DatasetRead,
                PredictionPermissions.JobCreate,
                PredictionPermissions.JobRead,
                PredictionPermissions.JobCancel,
                PredictionPermissions.ForecastRead,
                PredictionPermissions.ForecastExport,
                PredictionPermissions.Extract,
            }
        ),
        (
            Viewer,
            "Prediction Viewer with read-only access",
            new[]
            {
                PredictionPermissions.ModelRead,
                PredictionPermissions.DatasetRead,
                PredictionPermissions.JobRead,
                PredictionPermissions.ForecastRead,
            }
        ),
    };
}
