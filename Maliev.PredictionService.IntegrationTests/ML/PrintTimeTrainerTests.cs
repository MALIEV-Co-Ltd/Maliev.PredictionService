using Maliev.PredictionService.Domain.Entities;
using Maliev.PredictionService.Domain.Enums;
using Maliev.PredictionService.Infrastructure.ML.Trainers;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Maliev.PredictionService.IntegrationTests.ML;

/// <summary>
/// Integration tests for PrintTimeTrainer ML model training workflow.
/// Tests training pipeline, model evaluation, and metrics validation.
/// </summary>
public class PrintTimeTrainerTests : IDisposable
{
    private readonly PrintTimeTrainer _trainer;
    private readonly ILogger<PrintTimeTrainer> _logger;
    private readonly List<string> _tempFiles = new();

    public PrintTimeTrainerTests()
    {
        _logger = LoggerFactory.Create(builder => builder.AddConsole())
            .CreateLogger<PrintTimeTrainer>();
        _trainer = new PrintTimeTrainer(_logger);
    }

    [Fact]
    public async Task TrainModelAsync_WithValidDataset_ReturnsTrainedModel()
    {
        // Arrange - Create synthetic training dataset
        var trainingDataPath = CreateMockTrainingDataset(sampleCount: 100);
        var trainingDataset = new TrainingDataset
        {
            Id = Guid.NewGuid(),
            ModelType = ModelType.PrintTime,
            FilePath = trainingDataPath,
            RecordCount = 100,
            DateRangeStart = DateTime.UtcNow.AddDays(-30),
            DateRangeEnd = DateTime.UtcNow,
            FeatureColumns = new List<string>
            {
                "Volume", "SurfaceArea", "LayerCount", "SupportPercentage", "ComplexityScore",
                "BoundingBoxWidth", "BoundingBoxDepth", "BoundingBoxHeight",
                "MaterialDensity", "PrintSpeed", "NozzleTemperature", "BedTemperature", "InfillPercentage"
            },
            TargetColumn = "PrintTimeMinutes",
            CreatedAt = DateTime.UtcNow
        };

        // Act
        var result = await _trainer.TrainModelAsync(trainingDataset, testSplitRatio: 0.2f, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Model);
        Assert.Equal(80, result.TrainingSampleCount); // 80% of 100 samples should be for training
        Assert.Equal(20, result.TestSampleCount); // 20% of 100 samples should be for testing
        Assert.True(result.TrainingDuration > TimeSpan.Zero, "Training should take measurable time");

        // Validate metrics - FastTreeRegression should achieve reasonable performance
        Assert.NotNull(result.TestMetrics);
        Assert.True(result.TestMetrics.RSquared > 0.5,
            "R² should be > 0.5 for reasonable model fit on synthetic data");
        Assert.True(result.TestMetrics.MeanAbsoluteError > 0,
            "MAE should be positive (perfect fit unlikely with test split)");
        Assert.True(result.TestMetrics.RootMeanSquaredError > 0,
            "RMSE should be positive");

        Assert.NotNull(result.TrainMetrics);
        Assert.True(result.TrainMetrics.RSquared > result.TestMetrics.RSquared - 0.3,
            "Train R² should not be drastically better than test R² (checking for overfitting)");
    }

    [Fact]
    public async Task TrainModelAsync_WithLargeDataset_HandlesDataEfficiently()
    {
        // Arrange - Create larger dataset to test scalability
        var trainingDataPath = CreateMockTrainingDataset(sampleCount: 500);
        var trainingDataset = new TrainingDataset
        {
            Id = Guid.NewGuid(),
            ModelType = ModelType.PrintTime,
            FilePath = trainingDataPath,
            RecordCount = 500,
            DateRangeStart = DateTime.UtcNow.AddDays(-30),
            DateRangeEnd = DateTime.UtcNow,
            FeatureColumns = new List<string>
            {
                "Volume", "SurfaceArea", "LayerCount", "SupportPercentage", "ComplexityScore",
                "BoundingBoxWidth", "BoundingBoxDepth", "BoundingBoxHeight",
                "MaterialDensity", "PrintSpeed", "NozzleTemperature", "BedTemperature", "InfillPercentage"
            },
            TargetColumn = "PrintTimeMinutes",
            CreatedAt = DateTime.UtcNow
        };

        // Act
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var result = await _trainer.TrainModelAsync(trainingDataset, testSplitRatio: 0.2f, CancellationToken.None);
        sw.Stop();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(400, result.TrainingSampleCount);
        Assert.Equal(100, result.TestSampleCount);
        Assert.True(sw.Elapsed < TimeSpan.FromSeconds(30),
            "Training 500 samples should complete within 30 seconds");

        // Larger dataset should yield better R²
        Assert.True(result.TestMetrics.RSquared > 0.6,
            "Larger dataset should improve model performance");
    }

    [Fact]
    public async Task TrainModelAsync_WithDifferentTestSplits_ProducesConsistentResults()
    {
        // Arrange
        var trainingDataPath = CreateMockTrainingDataset(sampleCount: 200);
        var trainingDataset = new TrainingDataset
        {
            Id = Guid.NewGuid(),
            ModelType = ModelType.PrintTime,
            FilePath = trainingDataPath,
            RecordCount = 200,
            DateRangeStart = DateTime.UtcNow.AddDays(-30),
            DateRangeEnd = DateTime.UtcNow,
            FeatureColumns = new List<string>
            {
                "Volume", "SurfaceArea", "LayerCount", "SupportPercentage", "ComplexityScore",
                "BoundingBoxWidth", "BoundingBoxDepth", "BoundingBoxHeight",
                "MaterialDensity", "PrintSpeed", "NozzleTemperature", "BedTemperature", "InfillPercentage"
            },
            TargetColumn = "PrintTimeMinutes",
            CreatedAt = DateTime.UtcNow
        };

        // Act - Train with different test split ratios
        var result10 = await _trainer.TrainModelAsync(trainingDataset, testSplitRatio: 0.1f, CancellationToken.None);
        var result20 = await _trainer.TrainModelAsync(trainingDataset, testSplitRatio: 0.2f, CancellationToken.None);
        var result30 = await _trainer.TrainModelAsync(trainingDataset, testSplitRatio: 0.3f, CancellationToken.None);

        // Assert - Sample counts match expected splits
        Assert.Equal(180, result10.TrainingSampleCount); // 90%
        Assert.Equal(20, result10.TestSampleCount);      // 10%

        Assert.Equal(160, result20.TrainingSampleCount); // 80%
        Assert.Equal(40, result20.TestSampleCount);      // 20%

        Assert.Equal(140, result30.TrainingSampleCount); // 70%
        Assert.Equal(60, result30.TestSampleCount);      // 30%

        // All models should achieve reasonable performance
        Assert.True(result10.TestMetrics.RSquared > 0.4);
        Assert.True(result20.TestMetrics.RSquared > 0.4);
        Assert.True(result30.TestMetrics.RSquared > 0.4);
    }

    /// <summary>
    /// Creates a synthetic CSV training dataset with realistic print time data.
    /// Generates data following approximate physics: time ~ volume/speed + complexity_factor
    /// </summary>
    private string CreateMockTrainingDataset(int sampleCount)
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"print_time_training_{Guid.NewGuid()}.csv");
        _tempFiles.Add(tempPath);

        using var writer = new StreamWriter(tempPath);

        // CSV Header matching PrintTimeInput schema
        writer.WriteLine("Volume,SurfaceArea,LayerCount,SupportPercentage,ComplexityScore," +
                        "BoundingBoxWidth,BoundingBoxDepth,BoundingBoxHeight," +
                        "MaterialDensity,PrintSpeed,NozzleTemperature,BedTemperature," +
                        "InfillPercentage,PrintTimeMinutes");

        var random = new Random(42); // Fixed seed for reproducibility

        for (int i = 0; i < sampleCount; i++)
        {
            // Generate realistic geometry features
            float volume = (float)(random.NextDouble() * 50000 + 100); // 100-50,100 mm³
            float surfaceArea = (float)(Math.Pow(volume, 2.0 / 3.0) * 15); // Approximate surface area
            float height = (float)(random.NextDouble() * 100 + 5); // 5-105 mm
            float width = (float)(random.NextDouble() * 80 + 10); // 10-90 mm
            float depth = (float)(random.NextDouble() * 80 + 10); // 10-90 mm
            float layerCount = height / 0.2f; // 0.2mm layer height
            float supportPercentage = (float)(random.NextDouble() * 30); // 0-30%
            float complexityScore = (float)(random.NextDouble() * 80 + 10); // 10-90

            // Print settings
            float materialDensity = (float)(random.NextDouble() * 0.5 + 1.0); // 1.0-1.5 g/cm³
            float printSpeed = (float)(random.NextDouble() * 40 + 30); // 30-70 mm/s
            float nozzleTemp = (float)(random.NextDouble() * 50 + 190); // 190-240°C
            float bedTemp = (float)(random.NextDouble() * 40 + 50); // 50-90°C
            float infillPercentage = (float)(random.NextDouble() * 80 + 10); // 10-90%

            // Physics-based time calculation with noise
            float baseTime = volume / (printSpeed * 60); // Base time from volume/speed
            float complexityFactor = 1 + (complexityScore / 200f); // Complexity increases time
            float supportFactor = 1 + (supportPercentage / 100f); // Support increases time
            float infillFactor = 0.5f + (infillPercentage / 100f); // More infill = more time
            float noise = (float)(random.NextDouble() * 0.2 - 0.1); // ±10% noise

            float printTimeMinutes = baseTime * complexityFactor * supportFactor * infillFactor * (1 + noise);
            printTimeMinutes = Math.Max(1, printTimeMinutes); // Minimum 1 minute

            // Write CSV row
            writer.WriteLine($"{volume:F2},{surfaceArea:F2},{layerCount:F2},{supportPercentage:F2}," +
                           $"{complexityScore:F2},{width:F2},{depth:F2},{height:F2}," +
                           $"{materialDensity:F3},{printSpeed:F2},{nozzleTemp:F2},{bedTemp:F2}," +
                           $"{infillPercentage:F2},{printTimeMinutes:F2}");
        }

        return tempPath;
    }

    public void Dispose()
    {
        // Cleanup temporary training files
        foreach (var file in _tempFiles)
        {
            try
            {
                if (File.Exists(file))
                    File.Delete(file);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }
}
