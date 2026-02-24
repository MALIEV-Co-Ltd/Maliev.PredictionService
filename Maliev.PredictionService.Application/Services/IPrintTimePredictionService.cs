using Maliev.PredictionService.Application.DTOs.Requests;
using Maliev.PredictionService.Application.DTOs.Responses;

namespace Maliev.PredictionService.Application.Services;

/// <summary>
/// Application service interface for print time predictions.
/// Abstracts Infrastructure layer ML prediction logic.
/// </summary>
public interface IPrintTimePredictionService
{
    /// <summary>
    /// Performs print time prediction with model loading and inference.
    /// </summary>
    /// <param name="request">Prediction request with geometry and parameters.</param>
    /// <param name="modelFilePath">Path to the trained ML model file.</param>
    /// <param name="modelVersion">Model version string.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Prediction response with time estimate and metadata.</returns>
    Task<PredictionResponse> PredictAsync(
        PrintTimePredictionRequest request,
        string modelFilePath,
        string modelVersion,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets cached prediction if available.
    /// </summary>
    /// <param name="cacheKey">Cache key for the prediction.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Cached prediction response or null if not found.</returns>
    Task<PredictionResponse?> GetCachedPredictionAsync(
        string cacheKey,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Caches a prediction result.
    /// </summary>
    /// <param name="cacheKey">Cache key.</param>
    /// <param name="response">Prediction response to cache.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task CachePredictionAsync(
        string cacheKey,
        PredictionResponse response,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates content-based cache key for the prediction request.
    /// </summary>
    /// <param name="request">Prediction request.</param>
    /// <param name="modelVersion">Model version.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>SHA-256 based cache key.</returns>
    Task<string> GenerateCacheKeyAsync(
        PrintTimePredictionRequest request,
        string modelVersion,
        CancellationToken cancellationToken = default);
}
