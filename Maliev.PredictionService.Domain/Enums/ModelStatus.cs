namespace Maliev.PredictionService.Domain.Enums;

/// <summary>
/// Lifecycle states for ML models
/// State transition: Draft → Testing → Active → Deprecated → Archived
/// </summary>
public enum ModelStatus
{
    /// <summary>
    /// Model has been trained but not yet validated
    /// </summary>
    Draft,

    /// <summary>
    /// Model is undergoing quality gate validation
    /// </summary>
    Testing,

    /// <summary>
    /// Model is deployed and serving production traffic
    /// </summary>
    Active,

    /// <summary>
    /// Model has been replaced by a newer version but still available for rollback
    /// </summary>
    Deprecated,

    /// <summary>
    /// Model is archived (90+ days after deprecation), preserved for audit
    /// </summary>
    Archived
}
