using Maliev.PredictionService.Infrastructure.ML.Trainers;
using Microsoft.ML;
using Microsoft.ML.Data;
using Xunit;

namespace Maliev.PredictionService.IntegrationTests.ML;

/// <summary>
/// Tests to verify CSV loading works correctly for model training.
/// </summary>
public class CsvLoadingTests
{
    [Fact]
    public void LoadCsvFile_SampleDemandData_LoadsCorrectly()
    {
        // Arrange
        var baseDir = AppContext.BaseDirectory;
        var csvPath = Path.Combine(baseDir, "Fixtures", "sample-demand-training-data.csv");

        var mlContext = new MLContext(seed: 42);

        // Act
        var dataView = mlContext.Data.LoadFromTextFile<DemandForecastTrainer.DemandInput>(
            csvPath,
            hasHeader: true,
            separatorChar: ',');

        // Assert file exists first
        Assert.True(File.Exists(csvPath), $"CSV file not found: {csvPath}");

        // Enumerate data - GetRowCount() may return 0 even when data exists
        Exception? enumerationError = null;
        List<DemandForecastTrainer.DemandInput>? data = null;
        try
        {
            data = mlContext.Data.CreateEnumerable<DemandForecastTrainer.DemandInput>(dataView, reuseRowObject: false).ToList();
        }
        catch (Exception ex)
        {
            enumerationError = ex;
        }

        // Assert with more details
        if (enumerationError != null)
        {
            throw new InvalidOperationException($"Failed to enumerate data: {enumerationError.Message}", enumerationError);
        }

        Assert.NotNull(data);
        Assert.Equal(180, data.Count); // 180 data rows (181 lines - 1 header)

        // Verify first and last rows
        var firstRow = data[0];
        Assert.Equal("2025-08-01", firstRow.Date);
        Assert.Equal(100.5f, firstRow.Demand, precision: 2);

        var lastRow = data[179];
        Assert.Equal("2026-01-27", lastRow.Date);
        Assert.Equal(228.7f, lastRow.Demand, precision: 2);
    }
}
