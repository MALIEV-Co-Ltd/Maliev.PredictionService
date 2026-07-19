using Maliev.PredictionService.Domain.Entities;
using Maliev.PredictionService.Domain.Enums;
using Maliev.PredictionService.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using Microsoft.ML;
using Microsoft.ML.Data;

namespace Maliev.PredictionService.Infrastructure.ML.Trainers;

/// <summary>
/// Trains FastTreeRegression models for 3D print time prediction.
/// Implements ML.NET pipeline with feature engineering, normalization, and model evaluation.
/// </summary>
public class PrintTimeTrainer
{
    private readonly MLContext _mlContext;
    private readonly ILogger<PrintTimeTrainer> _logger;

    public PrintTimeTrainer(ILogger<PrintTimeTrainer> logger)
    {
        _mlContext = new MLContext(seed: 42); // Fixed seed for reproducibility
        _logger = logger;
    }

    /// <summary>
    /// Input data schema for print time prediction training.
    /// </summary>
    public class PrintTimeInput
    {
        [LoadColumn(0)] public float Volume { get; set; }
        [LoadColumn(1)] public float SurfaceArea { get; set; }
        [LoadColumn(2)] public float LayerCount { get; set; }
        [LoadColumn(3)] public float SupportPercentage { get; set; }
        [LoadColumn(4)] public float ComplexityScore { get; set; }
        [LoadColumn(5)] public float BoundingBoxWidth { get; set; }
        [LoadColumn(6)] public float BoundingBoxDepth { get; set; }
        [LoadColumn(7)] public float BoundingBoxHeight { get; set; }
        [LoadColumn(8)] public float MaterialDensity { get; set; } // g/cm³
        [LoadColumn(9)] public float PrintSpeed { get; set; } // mm/s
        [LoadColumn(10)] public float NozzleTemperature { get; set; } // °C
        [LoadColumn(11)] public float BedTemperature { get; set; } // °C
        [LoadColumn(12)] public float InfillPercentage { get; set; } // 0-100
        [LoadColumn(13)] public float PrintTimeMinutes { get; set; } // Target label
    }

    /// <summary>
    /// Output schema for print time predictions.
    /// </summary>
    public class PrintTimePrediction
    {
        [ColumnName("Score")]
        public float PrintTimeMinutes { get; set; }
    }

    /// <summary>
    /// Training result containing model, metrics, and metadata.
    /// </summary>
    public record TrainingResult
    {
        public required ITransformer Model { get; init; }
        public required RegressionMetrics TestMetrics { get; init; }
        public required RegressionMetrics TrainMetrics { get; init; }
        public required int TrainingSampleCount { get; init; }
        public required int TestSampleCount { get; init; }
        public required TimeSpan TrainingDuration { get; init; }
    }

    /// <summary>
    /// Trains a FastTreeRegression model on the provided training dataset.
    /// </summary>
    /// <param name="trainingDataset">Training dataset entity containing file path and metadata.</param>
    /// <param name="testSplitRatio">Ratio of data to use for testing (default 0.2 = 20%).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Training result with model and evaluation metrics.</returns>
    public async Task<TrainingResult> TrainModelAsync(
        TrainingDataset trainingDataset,
        float testSplitRatio = 0.2f,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Starting print time model training. Dataset: {DatasetId}, Rows: {RecordCount}",
            trainingDataset.Id,
            trainingDataset.RecordCount);

        var startTime = DateTime.UtcNow;

        // Load data from file
        var filePath = trainingDataset.FilePath ?? throw new ArgumentException("Dataset FilePath is null");
        var dataView = await LoadDataFromFileAsync(filePath, cancellationToken);

        // Split into train/test sets
        var trainTestSplit = _mlContext.Data.TrainTestSplit(dataView, testFraction: testSplitRatio, seed: 42);
        var trainData = trainTestSplit.TrainSet;
        var testData = trainTestSplit.TestSet;

        // GetRowRowCount might return null for text files. Use estimate if null.
        var trainCount = (int)(trainData.GetRowCount() ?? (long)Math.Round(trainingDataset.RecordCount * (1 - testSplitRatio)));
        var testCount = (int)(testData.GetRowCount() ?? (long)Math.Round(trainingDataset.RecordCount * testSplitRatio));

        _logger.LogInformation(
            "Data split complete. Train: {TrainCount}, Test: {TestCount}",
            trainCount,
            testCount);

        // Build training pipeline
        var pipeline = BuildTrainingPipeline();

        // Train the model
        _logger.LogInformation("Training FastTreeRegression model...");
        var model = pipeline.Fit(trainData);

        // Evaluate on both train and test sets
        var trainPredictions = model.Transform(trainData);
        var testPredictions = model.Transform(testData);

        var trainMetrics = _mlContext.Regression.Evaluate(trainPredictions, labelColumnName: "PrintTimeMinutes");
        var testMetrics = _mlContext.Regression.Evaluate(testPredictions, labelColumnName: "PrintTimeMinutes");

        var duration = DateTime.UtcNow - startTime;

        _logger.LogInformation(
            "Training complete. Test R²: {RSquared:F4}, MAE: {Mae:F2} min, RMSE: {Rmse:F2} min, Duration: {Duration:F2}s",
            testMetrics.RSquared,
            testMetrics.MeanAbsoluteError,
            testMetrics.RootMeanSquaredError,
            duration.TotalSeconds);

        return new TrainingResult
        {
            Model = model,
            TestMetrics = testMetrics,
            TrainMetrics = trainMetrics,
            TrainingSampleCount = trainCount,
            TestSampleCount = testCount,
            TrainingDuration = duration
        };
    }

    /// <summary>
    /// Loads training data from CSV file into IDataView.
    /// </summary>
    private Task<IDataView> LoadDataFromFileAsync(string filePath, CancellationToken cancellationToken)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Training dataset file not found: {filePath}");
        }

        _logger.LogDebug("Loading training data from {FilePath}", filePath);

        // Load from CSV with header
        var dataView = _mlContext.Data.LoadFromTextFile<PrintTimeInput>(
            filePath,
            hasHeader: true,
            separatorChar: ',');

        return Task.FromResult(dataView);
    }

    /// <summary>
    /// Builds ML.NET training pipeline with feature engineering and FastTree regressor.
    /// Pipeline stages:
    /// 1. Feature concatenation - combine all numeric features into single vector
    /// 2. Normalization - scale features to mean=0, stddev=1
    /// 3. FastTree regression - gradient boosting decision trees
    /// </summary>
    private IEstimator<ITransformer> BuildTrainingPipeline()
    {
        var featureColumns = new[]
        {
            nameof(PrintTimeInput.Volume),
            nameof(PrintTimeInput.SurfaceArea),
            nameof(PrintTimeInput.LayerCount),
            nameof(PrintTimeInput.SupportPercentage),
            nameof(PrintTimeInput.ComplexityScore),
            nameof(PrintTimeInput.BoundingBoxWidth),
            nameof(PrintTimeInput.BoundingBoxDepth),
            nameof(PrintTimeInput.BoundingBoxHeight),
            nameof(PrintTimeInput.MaterialDensity),
            nameof(PrintTimeInput.PrintSpeed),
            nameof(PrintTimeInput.NozzleTemperature),
            nameof(PrintTimeInput.BedTemperature),
            nameof(PrintTimeInput.InfillPercentage)
        };

        var pipeline = _mlContext.Transforms.Concatenate("Features", featureColumns)
            .Append(_mlContext.Transforms.NormalizeMinMax("Features"))
            .Append(_mlContext.Regression.Trainers.FastTree(
                labelColumnName: nameof(PrintTimeInput.PrintTimeMinutes),
                featureColumnName: "Features",
                numberOfLeaves: 50,
                numberOfTrees: 200,
                minimumExampleCountPerLeaf: 10,
                learningRate: 0.1));

        return pipeline;
    }

    /// <summary>
    /// Saves trained model to file system with versioned naming.
    /// </summary>
    /// <param name="model">Trained ML.NET model.</param>
    /// <param name="modelVersion">Semantic version for this model.</param>
    /// <param name="outputDirectory">Directory to save model file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Full path to saved model file.</returns>
    public async Task<string> SaveModelAsync(
        ITransformer model,
        ModelVersion modelVersion,
        string outputDirectory,
        CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
            _logger.LogInformation("Created model output directory: {Directory}", outputDirectory);
        }

        var fileName = $"print-time_v{modelVersion}.zip";
        var filePath = Path.Combine(outputDirectory, fileName);

        _logger.LogInformation("Saving model to {FilePath}", filePath);

        // Create a dummy schema for model saving (ML.NET requires IDataView schema)
        var emptyData = _mlContext.Data.LoadFromEnumerable(new List<PrintTimeInput>());

        await Task.Run(() =>
        {
            _mlContext.Model.Save(model, emptyData.Schema, filePath);
        }, cancellationToken);

        _logger.LogInformation("Model saved successfully: {FilePath}", filePath);

        return filePath;
    }

    /// <summary>
    /// Evaluates model performance and converts metrics to domain value object.
    /// </summary>
    public static PerformanceMetrics ConvertToPerformanceMetrics(RegressionMetrics metrics)
    {
        return new PerformanceMetrics
        {
            RSquared = metrics.RSquared,
            MAE = metrics.MeanAbsoluteError,
            RMSE = metrics.RootMeanSquaredError,
            MAPE = null, // Not calculated for regression
            Precision = null, // Not applicable for regression
            Recall = null, // Not applicable for regression
            F1Score = null, // Not applicable for regression
            AUC = null // Not applicable for regression
        };
    }
}
