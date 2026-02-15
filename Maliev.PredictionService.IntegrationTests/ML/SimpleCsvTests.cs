using Microsoft.ML;
using Microsoft.ML.Data;
using Xunit;

namespace Maliev.PredictionService.IntegrationTests.ML;

/// <summary>
/// Simplified CSV loading tests using string date format.
/// </summary>
public class SimpleCsvTests
{
    public class SimpleDemandData
    {
        [LoadColumn(0)] public string? Date { get; set; }
        [LoadColumn(1)] public float Demand { get; set; }
    }

    [Fact]
    public void LoadCsv_WithStringDate_LoadsCorrectly()
    {
        // Arrange
        var baseDir = AppContext.BaseDirectory;
        var csvPath = Path.Combine(baseDir, "Fixtures", "sample-demand-training-data.csv");

        Assert.True(File.Exists(csvPath), $"CSV not found: {csvPath}");

        var mlContext = new MLContext(seed: 42);

        // Act - Load with string date
        var dataView = mlContext.Data.LoadFromTextFile<SimpleDemandData>(
            csvPath,
            hasHeader: true,
            separatorChar: ',');

        var data = mlContext.Data.CreateEnumerable<SimpleDemandData>(dataView, reuseRowObject: false).ToList();

        // Assert
        Assert.NotNull(data);
        Assert.True(data.Count > 0, $"Expected rows, got {data.Count}");
        Assert.Equal(180, data.Count);

        // Verify first row
        var first = data[0];
        Assert.Equal("2025-08-01", first.Date);
        Assert.Equal(100.5f, first.Demand, precision: 2);

        // Verify last row
        var last = data[179];
        Assert.Equal("2026-01-27", last.Date);
        Assert.Equal(228.7f, last.Demand, precision: 2);
    }
}
