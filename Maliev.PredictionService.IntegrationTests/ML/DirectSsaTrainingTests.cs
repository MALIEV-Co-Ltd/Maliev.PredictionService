using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Transforms.TimeSeries;
using Xunit;

namespace Maliev.PredictionService.IntegrationTests.ML;

/// <summary>
/// Direct SSA training tests to understand the correct parameter usage.
/// </summary>
public class DirectSsaTrainingTests
{
    public class DemandData
    {
        [LoadColumn(0)] public string? Date { get; set; }
        [LoadColumn(1)] public float Demand { get; set; }
    }

    public class DemandForecast
    {
        public float[] ForecastedDemand { get; set; } = Array.Empty<float>();
    }

    [Fact]
    public void DirectSsaTraining_WithCsvFile_Works()
    {
        // Arrange
        var mlContext = new MLContext(seed: 42);
        var csvPath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "sample-demand-training-data.csv");

        // Load CSV
        var dataView = mlContext.Data.LoadFromTextFile<DemandData>(
            csvPath,
            hasHeader: true,
            separatorChar: ',');

        // Note: GetRowCount() may return 0 if data hasn't been enumerated yet
        // Just proceed with training

        // Act - Try different parameter combinations
        // Based on ML.NET samples: use trainSize that's less than total rows
        var trainSize = 150; // Use first 150 for training
        var horizon = 7;
        var windowSize = 10;
        var seriesLength = trainSize; // Series length equals training size

        var pipeline = mlContext.Forecasting.ForecastBySsa(
            outputColumnName: nameof(DemandForecast.ForecastedDemand),
            inputColumnName: nameof(DemandData.Demand),
            windowSize: windowSize,
            seriesLength: seriesLength,
            trainSize: trainSize,
            horizon: horizon);

        var model = pipeline.Fit(dataView);

        // Assert
        Assert.NotNull(model);

        // Save the model for future use
        var modelPath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "demand-forecast-direct-model.zip");
        mlContext.Model.Save(model, dataView.Schema, modelPath);

        Assert.True(File.Exists(modelPath));
    }

    [Theory]
    [InlineData(30, 5, 30)]   // trainSize=30, windowSize=5, seriesLength=30
    [InlineData(50, 7, 50)]   // trainSize=50, windowSize=7, seriesLength=50
    [InlineData(100, 10, 100)] // trainSize=100, windowSize=10, seriesLength=100
    public void DirectSsaTraining_VariousParameters_Works(int trainSize, int windowSize, int seriesLength)
    {
        // Arrange
        var mlContext = new MLContext(seed: 42);
        var csvPath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "sample-demand-training-data.csv");

        var dataView = mlContext.Data.LoadFromTextFile<DemandData>(
            csvPath,
            hasHeader: true,
            separatorChar: ',');

        // Act
        var pipeline = mlContext.Forecasting.ForecastBySsa(
            outputColumnName: nameof(DemandForecast.ForecastedDemand),
            inputColumnName: nameof(DemandData.Demand),
            windowSize: windowSize,
            seriesLength: seriesLength,
            trainSize: trainSize,
            horizon: 7);

        var model = pipeline.Fit(dataView);

        // Assert
        Assert.NotNull(model);
    }
}
