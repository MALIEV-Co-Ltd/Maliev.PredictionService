using Maliev.PredictionService.Application.DTOs.ML;
using Maliev.PredictionService.Infrastructure.ML.Predictors;
using Microsoft.ML;
using Microsoft.ML.Transforms.TimeSeries;
using Xunit;

namespace Maliev.PredictionService.IntegrationTests.ML;

/// <summary>
/// Training tests using the full Application DTOs schema.
/// Creates models compatible with DemandForecaster.
/// </summary>
public class FullSchemaTrainingTests
{
    [Fact]
    public void TrainSsaModel_WithFullSchema_CreatesCompatibleModel()
    {
        // Arrange
        var mlContext = new MLContext(seed: 42);
        var csvPath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "sample-demand-training-data-full.csv");

        // Load data using the helper
        var demandData = CsvDemandDataLoader.LoadFromCsv(csvPath);

        Assert.NotNull(demandData);
        Assert.Equal(180, demandData.Count);
        Assert.All(demandData, d => Assert.Equal("PROD-001", d.ProductId));

        // SSA only uses the Demand column, so convert to SsaDemandInput
        // This matches what DemandForecaster uses for prediction
        var ssaData = demandData.Select(d => new SsaDemandInput { Demand = d.Demand }).ToList();
        var dataView = mlContext.Data.LoadFromEnumerable(ssaData);

        // Act - Train SSA model with compatible parameters
        var trainSize = 150; // Use first 150 for training
        var horizon = 7;
        var windowSize = 10;
        var seriesLength = trainSize;

        // SSA uses only the Demand column from SsaDemandInput
        var pipeline = mlContext.Forecasting.ForecastBySsa(
            outputColumnName: "ForecastedDemand",
            inputColumnName: nameof(SsaDemandInput.Demand),
            windowSize: windowSize,
            seriesLength: seriesLength,
            trainSize: trainSize,
            horizon: horizon,
            confidenceLevel: 0.95f,
            confidenceLowerBoundColumn: "LowerBoundConfidence",
            confidenceUpperBoundColumn: "UpperBoundConfidence");

        var model = pipeline.Fit(dataView);

        // Assert
        Assert.NotNull(model);

        // Save the model for DemandForecaster tests
        var modelPath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "demand-forecast-sample-model.zip");
        mlContext.Model.Save(model, dataView.Schema, modelPath);

        Assert.True(File.Exists(modelPath));

        // Verify model can be loaded
        var loadedModel = mlContext.Model.Load(modelPath, out var schema);
        Assert.NotNull(loadedModel);
        Assert.NotNull(schema);
    }

    [Fact]
    public void LoadedModel_CanGeneratePredictions()
    {
        // Arrange - Always retrain to ensure correct schema
        TrainSsaModel_WithFullSchema_CreatesCompatibleModel();

        var mlContext = new MLContext(seed: 42);
        var csvPath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "sample-demand-training-data-full.csv");
        var modelPath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "demand-forecast-sample-model.zip");

        // Load model and data
        var model = mlContext.Model.Load(modelPath, out var schema);
        var demandData = CsvDemandDataLoader.LoadFromCsv(csvPath);

        // Act - Create prediction engine using SsaDemandInput/SsaForecastOutput (matches training)
        var forecastEngine = model.CreateTimeSeriesEngine<SsaDemandInput, SsaForecastOutput>(mlContext);
        var forecast = forecastEngine.Predict();

        // Assert - SSA outputs arrays
        Assert.NotNull(forecast);
        Assert.NotNull(forecast.ForecastedDemand);
        Assert.NotNull(forecast.LowerBoundConfidence);
        Assert.NotNull(forecast.UpperBoundConfidence);
        Assert.True(forecast.ForecastedDemand.Length > 0, "Should have forecast values");
        Assert.Equal(forecast.ForecastedDemand.Length, forecast.LowerBoundConfidence.Length);
        Assert.Equal(forecast.ForecastedDemand.Length, forecast.UpperBoundConfidence.Length);
    }
}
