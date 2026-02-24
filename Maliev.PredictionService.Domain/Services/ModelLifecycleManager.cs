using Maliev.PredictionService.Domain.Entities;
using Maliev.PredictionService.Domain.Enums;
using Maliev.PredictionService.Domain.ValueObjects;

namespace Maliev.PredictionService.Domain.Services;

/// <summary>
/// Domain service for managing ML model lifecycle state transitions and quality gates
/// State flow: Draft → Testing → Active → Deprecated → Archived
/// </summary>
public class ModelLifecycleManager
{
    private const int MinDatasetSizePrintTime = 10000;
    private const int MinDatasetSizePrice = 5000;
    private const int MinDatasetSizeChurn = 2000;
    private const int MinDatasetSizeDefault = 1000;
    private const double MinAccuracyImprovementPercent = 2.0;
    private const int MaxVersionsToPreserve = 5;

    /// <summary>
    /// Validates whether a model can transition to Testing status
    /// </summary>
    public ModelTransitionResult CanTransitionToTesting(MLModel model, TrainingDataset dataset)
    {
        var minDatasetSize = GetMinimumDatasetSize(model.ModelType);

        if (dataset.RecordCount < minDatasetSize)
        {
            return ModelTransitionResult.Failure(
                $"Dataset size ({dataset.RecordCount}) is below minimum threshold ({minDatasetSize}) for {model.ModelType}");
        }

        if (model.Status != ModelStatus.Draft)
        {
            return ModelTransitionResult.Failure(
                $"Model must be in Draft status to transition to Testing. Current status: {model.Status}");
        }

        return ModelTransitionResult.Success("Model meets requirements for Testing status");
    }

    /// <summary>
    /// Validates quality gates for promoting a model from Testing to Active
    /// </summary>
    public ModelTransitionResult CanTransitionToActive(
        MLModel newModel,
        MLModel? currentActiveModel,
        Dictionary<string, object> validationResults)
    {
        if (newModel.Status != ModelStatus.Testing)
        {
            return ModelTransitionResult.Failure(
                $"Model must be in Testing status to transition to Active. Current status: {newModel.Status}");
        }

        if (newModel.PerformanceMetrics == null)
        {
            return ModelTransitionResult.Failure("Model must have performance metrics to be promoted to Active");
        }

        // Quality Gate 1: Data quality check
        if (HasCriticalDataQualityWarnings(validationResults))
        {
            return ModelTransitionResult.Failure("Model has critical data quality warnings");
        }

        // Quality Gate 2: Accuracy improvement (if there's an existing active model)
        if (currentActiveModel?.PerformanceMetrics != null)
        {
            var improvementPercent = CalculateAccuracyImprovement(newModel, currentActiveModel);
            if (improvementPercent < MinAccuracyImprovementPercent)
            {
                return ModelTransitionResult.Failure(
                    $"Model accuracy improvement ({improvementPercent:F2}%) is below minimum threshold ({MinAccuracyImprovementPercent}%)");
            }
        }

        return ModelTransitionResult.Success(
            $"Model passed all quality gates and can be promoted to Active");
    }

    /// <summary>
    /// Transitions a model to Active status and deprecates the previous active model
    /// </summary>
    public void PromoteToActive(MLModel newModel, MLModel? previousActiveModel)
    {
        if (previousActiveModel != null)
        {
            previousActiveModel.Status = ModelStatus.Deprecated;
            previousActiveModel.UpdatedAt = DateTime.UtcNow;
        }

        newModel.Status = ModelStatus.Active;
        newModel.DeploymentDate = DateTime.UtcNow;
        newModel.UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Rolls back to a previous model version
    /// </summary>
    public ModelTransitionResult RollbackToVersion(
        MLModel targetModel,
        MLModel currentActiveModel,
        string reason)
    {
        if (targetModel.Status != ModelStatus.Deprecated)
        {
            return ModelTransitionResult.Failure(
                "Can only rollback to models in Deprecated status");
        }

        // Demote current active model to deprecated
        currentActiveModel.Status = ModelStatus.Deprecated;
        currentActiveModel.UpdatedAt = DateTime.UtcNow;

        // Promote target model back to active
        targetModel.Status = ModelStatus.Active;
        targetModel.DeploymentDate = DateTime.UtcNow;
        targetModel.UpdatedAt = DateTime.UtcNow;

        if (targetModel.Metadata == null)
            targetModel.Metadata = new Dictionary<string, object>();

        targetModel.Metadata["rollback_reason"] = reason;
        targetModel.Metadata["rollback_timestamp"] = DateTime.UtcNow;
        targetModel.Metadata["rollback_from_version"] = currentActiveModel.ModelVersion.ToString();

        return ModelTransitionResult.Success(
            $"Successfully rolled back to version {targetModel.ModelVersion}");
    }

    /// <summary>
    /// Determines which deprecated models can be archived based on age
    /// </summary>
    public List<MLModel> GetModelsEligibleForArchival(
        List<MLModel> deprecatedModels,
        int deprecationDaysThreshold = 90)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-deprecationDaysThreshold);

        return deprecatedModels
            .Where(m => m.Status == ModelStatus.Deprecated && m.UpdatedAt < cutoffDate)
            .OrderBy(m => m.UpdatedAt)
            .Skip(MaxVersionsToPreserve) // Always preserve the last N versions
            .ToList();
    }

    private int GetMinimumDatasetSize(ModelType modelType)
    {
        return modelType switch
        {
            ModelType.PrintTime => MinDatasetSizePrintTime,
            ModelType.PriceOptimization => MinDatasetSizePrice,
            ModelType.ChurnPrediction => MinDatasetSizeChurn,
            _ => MinDatasetSizeDefault
        };
    }

    private bool HasCriticalDataQualityWarnings(Dictionary<string, object> validationResults)
    {
        if (!validationResults.TryGetValue("data_quality_warnings", out var warningsObj))
            return false;

        if (warningsObj is List<string> warnings)
        {
            return warnings.Any(w => w.Contains("CRITICAL", StringComparison.OrdinalIgnoreCase));
        }

        return false;
    }

    private double CalculateAccuracyImprovement(MLModel newModel, MLModel currentModel)
    {
        // Use the primary metric for each model type
        var newMetric = GetPrimaryMetric(newModel);
        var currentMetric = GetPrimaryMetric(currentModel);

        if (newMetric == null || currentMetric == null)
            return 0;

        // For metrics where higher is better (R², Precision, Recall)
        if (IsHigherBetter(newModel.ModelType))
        {
            return ((newMetric.Value - currentMetric.Value) / currentMetric.Value) * 100;
        }
        // For metrics where lower is better (MAE, RMSE, MAPE)
        else
        {
            return ((currentMetric.Value - newMetric.Value) / currentMetric.Value) * 100;
        }
    }

    private double? GetPrimaryMetric(MLModel model)
    {
        if (model.PerformanceMetrics == null)
            return null;

        return model.ModelType switch
        {
            ModelType.PrintTime => model.PerformanceMetrics.RSquared,
            ModelType.DemandForecast => model.PerformanceMetrics.MAPE,
            ModelType.MaterialDemand => model.PerformanceMetrics.MAPE,
            ModelType.PriceOptimization => model.PerformanceMetrics.RSquared,
            ModelType.ChurnPrediction => model.PerformanceMetrics.Precision,
            ModelType.BottleneckDetection => model.PerformanceMetrics.RSquared,
            _ => null
        };
    }

    private bool IsHigherBetter(ModelType modelType)
    {
        return modelType switch
        {
            ModelType.PrintTime => true,        // R² higher is better
            ModelType.DemandForecast => false,  // MAPE lower is better
            ModelType.MaterialDemand => false,  // MAPE lower is better
            ModelType.PriceOptimization => true, // R² higher is better
            ModelType.ChurnPrediction => true,  // Precision higher is better
            ModelType.BottleneckDetection => true, // R² higher is better
            _ => true
        };
    }
}

/// <summary>
/// Result of a model lifecycle transition attempt
/// </summary>
public class ModelTransitionResult
{
    public bool IsSuccess { get; init; }
    public required string Message { get; init; }

    public static ModelTransitionResult Success(string message) =>
        new() { IsSuccess = true, Message = message };

    public static ModelTransitionResult Failure(string message) =>
        new() { IsSuccess = false, Message = message };
}
