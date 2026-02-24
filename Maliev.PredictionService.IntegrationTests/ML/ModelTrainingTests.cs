using Maliev.PredictionService.Domain.Entities;
using Maliev.PredictionService.Domain.Enums;
using Maliev.PredictionService.Infrastructure.ML.Trainers;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.ML;
using Xunit;

namespace Maliev.PredictionService.IntegrationTests.ML;

/// <summary>
/// Collection definition to ensure ModelTrainingTests run sequentially (not in parallel).
/// This prevents file locking issues when accessing shared model files.
/// </summary>
[CollectionDefinition("ModelTraining", DisableParallelization = true)]
public class ModelTrainingCollection
{
}

/// <summary>
/// Integration tests for training ML.NET models and saving them for reuse.
/// This validates the training logic and produces models for other tests.
/// </summary>
[Collection("ModelTraining")]
public class ModelTrainingTests
{
    private readonly string _fixturesPath;
    private readonly string _sampleDataPath;
    private readonly string _trainedModelPath;

    public ModelTrainingTests()
    {
        // Get the fixtures directory path (files are copied to output directory)
        var baseDir = AppContext.BaseDirectory;
        _fixturesPath = Path.Combine(baseDir, "Fixtures");
        _sampleDataPath = Path.Combine(_fixturesPath, "sample-demand-training-data.csv");
        _trainedModelPath = Path.Combine(_fixturesPath, "demand-forecast-sample-model.zip");

        // Ensure fixtures directory exists
        Directory.CreateDirectory(_fixturesPath);
    }

    [Fact]
    public async Task TrainDemandForecastModel_WithSampleData_ProducesValidModel()
    {
        // Arrange - Create a TrainingDataset entity pointing to our sample CSV
        var trainingDataset = new TrainingDataset
        {
            Id = Guid.NewGuid(),
            ModelType = ModelType.DemandForecast,
            RecordCount = 180, // 180 days of data
            DateRangeStart = new DateTime(2025, 8, 1),
            DateRangeEnd = new DateTime(2026, 1, 27),
            FeatureColumns = new List<string> { "Date" },
            TargetColumn = "Demand",
            FilePath = _sampleDataPath,
            CreatedAt = DateTime.UtcNow
        };

        // Verify the sample data file exists
        Assert.True(File.Exists(_sampleDataPath), $"Sample data file not found: {_sampleDataPath}");

        var logger = new NullLogger<DemandForecastTrainer>();
        var trainer = new DemandForecastTrainer(logger);

        // Act - Train the model
        // Key insight: seriesLength should be >= trainSize
        // initialWindowSize = seriesLength - trainSize must be >= 0
        var result = await trainer.TrainModelAsync(
            trainingDataset,
            horizon: 7, // 7-day forecast
            windowSize: 10, // Window for SSA decomposition
            seriesLength: 180, // Use all available data as series buffer
            testHoldoutDays: 80, // Hold out 80 days, so trainSize = 100
            cancellationToken: default);

        // Assert - Verify training succeeded
        Assert.NotNull(result);
        Assert.NotNull(result.Model);
        Assert.NotNull(result.Metrics);

        // Verify metrics are reasonable (MAPE should be < 50% for decent model)
        Assert.True(result.Metrics.MAPE < 0.5, $"MAPE too high: {result.Metrics.MAPE:F2}");
        Assert.True(result.Metrics.RMSE > 0, "RMSE should be positive");
        Assert.True(result.Metrics.MAE > 0, "MAE should be positive");

        // Verify sample counts
        Assert.Equal(100, result.TrainingSampleCount); // 180 - 80 holdout
        Assert.Equal(80, result.TestSampleCount); // 80 days held out

        // Verify training duration is reasonable
        Assert.True(result.TrainingDuration.TotalSeconds > 0);
        Assert.True(result.TrainingDuration.TotalMinutes < 5, "Training should complete in under 5 minutes");

        // Act - Save the model to Fixtures directory for reuse
        var mlContext = new MLContext(seed: 42);
        mlContext.Model.Save(result.Model, result.Schema, _trainedModelPath);

        // Verify the model file was created
        Assert.True(File.Exists(_trainedModelPath), $"Trained model file not created: {_trainedModelPath}");

        var fileInfo = new FileInfo(_trainedModelPath);
        Assert.True(fileInfo.Length > 0, "Model file is empty");

        // Verify the model can be loaded back
        var loadedModel = mlContext.Model.Load(_trainedModelPath, out var modelInputSchema);
        Assert.NotNull(loadedModel);
        Assert.NotNull(modelInputSchema);
    }

    [Fact]
    public async Task TrainDemandForecastModel_With30DayHorizon_ProducesValidModel()
    {
        // Arrange - Create a TrainingDataset for 30-day forecasts
        var trainingDataset = new TrainingDataset
        {
            Id = Guid.NewGuid(),
            ModelType = ModelType.DemandForecast,
            RecordCount = 180,
            DateRangeStart = new DateTime(2025, 8, 1),
            DateRangeEnd = new DateTime(2026, 1, 27),
            FeatureColumns = new List<string> { "Date" },
            TargetColumn = "Demand",
            FilePath = _sampleDataPath,
            CreatedAt = DateTime.UtcNow
        };

        var logger = new NullLogger<DemandForecastTrainer>();
        var trainer = new DemandForecastTrainer(logger);

        // Act - Train for 30-day forecast
        var result = await trainer.TrainModelAsync(
            trainingDataset,
            horizon: 30, // 30-day forecast
            windowSize: 30, // 30-day window
            seriesLength: 150, // Use 150 days (180 - 30 holdout)
            testHoldoutDays: 30, // Hold out 30 days for testing
            cancellationToken: default);

        // Assert - Verify training succeeded
        Assert.NotNull(result);
        Assert.NotNull(result.Model);
        Assert.NotNull(result.Metrics);

        // For longer horizons, we expect slightly higher error
        Assert.True(result.Metrics.MAPE < 0.7, $"MAPE too high for 30-day horizon: {result.Metrics.MAPE:F2}");

        // Verify sample counts
        Assert.Equal(150, result.TrainingSampleCount); // 180 - 30 holdout
        Assert.Equal(30, result.TestSampleCount);

        // Save this model too (with different name)
        var model30DayPath = Path.Combine(_fixturesPath, "demand-forecast-30day-model.zip");
        var mlContext = new MLContext(seed: 42);
        mlContext.Model.Save(result.Model, null, model30DayPath);

        Assert.True(File.Exists(model30DayPath));
    }

    [Fact]
    public void VerifyTrainedModelExists_ForDemandForecasterTests()
    {
        // This test ensures the trained model exists for DemandForecasterTests to use
        // Run TrainDemandForecastModel_WithSampleData_ProducesValidModel first to generate it

        // Check if the model exists
        var modelExists = File.Exists(_trainedModelPath);

        if (!modelExists)
        {
            // Provide helpful error message
            throw new InvalidOperationException(
                $"Trained model not found at {_trainedModelPath}. " +
                "Run TrainDemandForecastModel_WithSampleData_ProducesValidModel test first to generate it.");
        }

        // Verify we can load it
        var mlContext = new MLContext(seed: 42);
        var model = mlContext.Model.Load(_trainedModelPath, out var schema);

        Assert.NotNull(model);
        Assert.NotNull(schema);
    }
}
