using Maliev.Aspire.ServiceDefaults.IAM;
using Maliev.PredictionService.Application.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Maliev.PredictionService.Application.Services;

/// <summary>
/// Registers Prediction Service permissions and roles with the centralized IAM service on startup.
/// </summary>
public class PredictionIAMRegistrationService(
    IConfiguration configuration,
    ILogger<PredictionIAMRegistrationService> logger) : IAMRegistrationService(configuration, logger, "prediction")
{
    /// <inheritdoc />
    protected override IEnumerable<PermissionRegistration> GetPermissions()
    {
        return PredictionPermissions.AllWithDescriptions.Select(p => new PermissionRegistration
        {
            PermissionId = p.Key,
            Description = p.Value
        });
    }

    /// <inheritdoc />
    protected override IEnumerable<RoleRegistration> GetPredefinedRoles()
    {
        return PredictionPredefinedRoles.All.Select(r => new RoleRegistration
        {
            RoleId = r.RoleId,
            Description = r.Description,
            PermissionIds = [.. r.Permissions],
            IsCustom = false
        });
    }
}
