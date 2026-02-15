using Microsoft.EntityFrameworkCore;
using Maliev.PredictionService.Domain.Entities;
using Maliev.PredictionService.Domain.Enums;
using Maliev.PredictionService.Domain.Repositories;

namespace Maliev.PredictionService.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for ML Model persistence
/// </summary>
public class ModelRepository : IModelRepository
{
    private readonly PredictionDbContext _context;

    public ModelRepository(PredictionDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<MLModel?> GetActiveModelByTypeAsync(ModelType modelType, CancellationToken cancellationToken = default)
    {
        return await _context.MLModels
            .Where(m => m.ModelType == modelType && m.Status == ModelStatus.Active)
            .OrderByDescending(m => m.DeploymentDate)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<MLModel?> GetByIdAsync(Guid modelId, CancellationToken cancellationToken = default)
    {
        return await _context.MLModels
            .Include(m => m.TrainingJob)
            .FirstOrDefaultAsync(m => m.Id == modelId, cancellationToken);
    }

    public async Task<MLModel> SaveModelAsync(MLModel model, CancellationToken cancellationToken = default)
    {
        model.UpdatedAt = DateTime.UtcNow;

        if (_context.Entry(model).State == EntityState.Detached)
        {
            _context.MLModels.Add(model);
        }
        else
        {
            _context.MLModels.Update(model);
        }

        await _context.SaveChangesAsync(cancellationToken);
        return model;
    }

    public async Task<List<MLModel>> GetModelHistoryAsync(
        ModelType modelType,
        int take = 10,
        CancellationToken cancellationToken = default)
    {
        return await _context.MLModels
            .Where(m => m.ModelType == modelType)
            .OrderByDescending(m => m.CreatedAt)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<MLModel>> GetModelsByStatusAsync(
        ModelStatus status,
        CancellationToken cancellationToken = default)
    {
        return await _context.MLModels
            .Where(m => m.Status == status)
            .OrderByDescending(m => m.UpdatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<MLModel>> GetByTypeAsync(ModelType modelType, CancellationToken cancellationToken = default)
    {
        return await _context.MLModels
            .Where(m => m.ModelType == modelType)
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<MLModel> AddAsync(MLModel model, CancellationToken cancellationToken = default)
    {
        _context.MLModels.Add(model);
        await _context.SaveChangesAsync(cancellationToken);
        return model;
    }

    public async Task UpdateAsync(MLModel model, CancellationToken cancellationToken = default)
    {
        model.UpdatedAt = DateTime.UtcNow;
        _context.MLModels.Update(model);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var model = await _context.MLModels.FindAsync(new object[] { id }, cancellationToken);
        if (model != null)
        {
            model.Status = ModelStatus.Archived;
            model.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<List<MLModel>> GetStaleModelsAsync(DateTime trainedBeforeDate, CancellationToken cancellationToken = default)
    {
        return await _context.MLModels
            .Where(m => m.Status == ModelStatus.Active && m.TrainingDate < trainedBeforeDate)
            .OrderBy(m => m.TrainingDate) // Oldest first
            .ToListAsync(cancellationToken);
    }
}
