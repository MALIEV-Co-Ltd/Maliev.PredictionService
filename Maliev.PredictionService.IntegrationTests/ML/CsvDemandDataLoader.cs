using Maliev.PredictionService.Application.DTOs.ML;
using Microsoft.ML;
using Microsoft.ML.Data;
using System.Globalization;

namespace Maliev.PredictionService.IntegrationTests.ML;

/// <summary>
/// Helper class to load demand data from CSV and convert to Application DTOs format.
/// Handles the DateTime conversion from CSV string format.
/// </summary>
public static class CsvDemandDataLoader
{
    /// <summary>
    /// Intermediate class for loading CSV data (all fields as parseable types).
    /// </summary>
    private class CsvDemandRow
    {
        [LoadColumn(0)] public string ProductId { get; set; } = string.Empty;
        [LoadColumn(1)] public string Date { get; set; } = string.Empty;
        [LoadColumn(2)] public float Demand { get; set; }
        [LoadColumn(3)] public bool IsPromotion { get; set; }
        [LoadColumn(4)] public bool IsHoliday { get; set; }
    }

    /// <summary>
    /// ML.NET-compatible class for SSA training (only needs Demand column).
    /// </summary>
    public class SsaDemandData
    {
        public float Demand { get; set; }
    }

    /// <summary>
    /// Loads CSV file and converts to Application DTOs DemandInput format.
    /// </summary>
    public static List<DemandInput> LoadFromCsv(string csvPath)
    {
        if (!File.Exists(csvPath))
        {
            throw new FileNotFoundException($"CSV file not found: {csvPath}");
        }

        var mlContext = new MLContext(seed: 42);

        // Load CSV with string Date column
        var dataView = mlContext.Data.LoadFromTextFile<CsvDemandRow>(
            csvPath,
            hasHeader: true,
            separatorChar: ',');

        // Convert to list and transform to DemandInput
        var csvRows = mlContext.Data.CreateEnumerable<CsvDemandRow>(dataView, reuseRowObject: false).ToList();

        var demandInputs = csvRows.Select(row => new DemandInput
        {
            ProductId = row.ProductId,
            Date = DateTime.ParseExact(row.Date, "yyyy-MM-dd", CultureInfo.InvariantCulture),
            Demand = row.Demand,
            IsPromotion = row.IsPromotion,
            IsHoliday = row.IsHoliday
        }).ToList();

        return demandInputs;
    }

    /// <summary>
    /// Creates an ML.NET IDataView suitable for SSA training (only Demand column).
    /// </summary>
    public static IDataView CreateSsaDataView(MLContext mlContext, List<DemandInput> demandData)
    {
        // SSA only needs the Demand values, not the other fields
        var ssaData = demandData.Select(d => new SsaDemandData { Demand = d.Demand }).ToList();

        return mlContext.Data.LoadFromEnumerable(ssaData);
    }
}
