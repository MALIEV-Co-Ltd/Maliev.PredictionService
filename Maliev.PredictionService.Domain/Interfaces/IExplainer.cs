using Maliev.PredictionService.Domain.ValueObjects;

namespace Maliev.PredictionService.Domain.Interfaces;

/// <summary>
/// Interface for model explainability (feature importance, SHAP-like explanations)
/// </summary>
public interface IExplainer
{
    /// <summary>
    /// Explains a prediction by returning feature contributions
    /// </summary>
    /// <param name="inputFeatures">Input features used for the prediction</param>
    /// <param name="prediction">The prediction result</param>
    /// <param name="modelPath">Path to the model file</param>
    /// <param name="topN">Number of top features to return (default: 5)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of feature contributions, sorted by impact weight descending</returns>
    Task<List<FeatureContribution>> ExplainPredictionAsync(
        Dictionary<string, object> inputFeatures,
        object prediction,
        string modelPath,
        int topN = 5,
        CancellationToken cancellationToken = default);
}
