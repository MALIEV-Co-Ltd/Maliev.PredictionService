namespace Maliev.PredictionService.Application.Interfaces;

/// <summary>
/// Interface for time-series feature engineering
/// </summary>
public interface ITimeSeriesTransformer
{
    /// <summary>
    /// Transforms raw time-series data into feature vectors
    /// </summary>
    Dictionary<string, float> Transform(DateTime date, IEnumerable<dynamic> historicalData, bool isPromotion);
}
