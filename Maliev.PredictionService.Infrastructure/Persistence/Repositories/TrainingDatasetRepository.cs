using Microsoft.EntityFrameworkCore;
using Maliev.PredictionService.Domain.Entities;
using Maliev.PredictionService.Domain.Enums;

namespace Maliev.PredictionService.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository for TrainingDataset CRUD operations
/// </summary>
public class TrainingDatasetRepository
{
    private readonly PredictionDbContext _context;

    public TrainingDatasetRepository(PredictionDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<TrainingDataset?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.TrainingDatasets
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task<TrainingDataset> CreateAsync(TrainingDataset dataset, CancellationToken cancellationToken = default)
    {
        _context.TrainingDatasets.Add(dataset);
        await _context.SaveChangesAsync(cancellationToken);
        return dataset;
    }

    public async Task<List<TrainingDataset>> GetByModelTypeAsync(
        ModelType modelType,
        int take = 10,
        CancellationToken cancellationToken = default)
    {
        return await _context.TrainingDatasets
            .Where(t => t.ModelType == modelType)
            .OrderByDescending(t => t.CreatedAt)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<TrainingDataset?> FindByHashAsync(
        string datasetHash,
        CancellationToken cancellationToken = default)
    {
        return await _context.TrainingDatasets
            .FirstOrDefaultAsync(t => t.DatasetHash == datasetHash, cancellationToken);
    }

    public async Task<int> GetTotalRecordCountAsync(
        ModelType modelType,
        CancellationToken cancellationToken = default)
    {
        return await _context.TrainingDatasets
            .Where(t => t.ModelType == modelType)
            .SumAsync(t => t.RecordCount, cancellationToken);
    }
}
