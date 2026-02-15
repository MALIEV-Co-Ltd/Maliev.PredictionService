using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Transforms.TimeSeries;
using Xunit;

namespace Maliev.PredictionService.IntegrationTests.ML;

/// <summary>
/// Minimal test to verify ML.NET SSA works with our data.
/// </summary>
public class MinimalSsaTests
{
    public class DemandData
    {
        public float Demand { get; set; }
    }

    public class DemandPrediction
    {
        public float[] ForecastedDemand { get; set; } = Array.Empty<float>();
    }

    [Fact]
    public void MinimalSsaTraining_Works()
    {
        // Arrange - Create simple in-memory data
        var mlContext = new MLContext(seed: 42);

        var data = new List<DemandData>();
        for (int i = 0; i < 100; i++)
        {
            data.Add(new DemandData { Demand = 100f + i });
        }

        var dataView = mlContext.Data.LoadFromEnumerable(data);

        // Act - Train SSA model with simple parameters
        var pipeline = mlContext.Forecasting.ForecastBySsa(
            outputColumnName: nameof(DemandPrediction.ForecastedDemand),
            inputColumnName: nameof(DemandData.Demand),
            windowSize: 7,
            seriesLength: 90,
            trainSize: 90,
            horizon: 7);

        var model = pipeline.Fit(dataView);

        // Create prediction engine
        var forecastEngine = model.CreateTimeSeriesEngine<DemandData, DemandPrediction>(mlContext);
        var forecast = forecastEngine.Predict();

        // Assert
        Assert.NotNull(forecast);
        Assert.NotNull(forecast.ForecastedDemand);
        Assert.Equal(7, forecast.ForecastedDemand.Length);
        Assert.True(forecast.ForecastedDemand[0] > 0);
    }
}
