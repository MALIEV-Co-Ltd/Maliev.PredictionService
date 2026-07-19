using Microsoft.EntityFrameworkCore;
using Maliev.PredictionService.Domain.Entities;

namespace Maliev.PredictionService.Infrastructure.Persistence;

/// <summary>
/// EF Core DbContext for PredictionService with multi-schema support
/// Schemas: ml_models, training, predictions, audit
/// </summary>
public class PredictionDbContext : DbContext
{
    public PredictionDbContext(DbContextOptions<PredictionDbContext> options)
        : base(options)
    {
    }

    // Model Registry (ml_models schema)
    public DbSet<MLModel> MLModels => Set<MLModel>();

    // Training (training schema)
    public DbSet<TrainingDataset> TrainingDatasets => Set<TrainingDataset>();
    public DbSet<TrainingJob> TrainingJobs => Set<TrainingJob>();

    // Audit (audit schema)
    public DbSet<PredictionAuditLog> PredictionAuditLogs => Set<PredictionAuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all entity configurations from this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PredictionDbContext).Assembly);

        // Convert all table and column names to snake_case for PostgreSQL consistency
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            // Skip owned entities (they are usually part of a JSON document or complex type)
            if (entity.IsOwned())
            {
                continue;
            }

            // Set table name to snake_case
            var tableName = entity.GetTableName();
            if (!string.IsNullOrEmpty(tableName))
            {
                entity.SetTableName(tableName.ToSnakeCase());
            }

            foreach (var property in entity.GetProperties())
            {
                // Set column name to snake_case
                property.SetColumnName(property.GetColumnName().ToSnakeCase());
            }

            foreach (var key in entity.GetKeys())
            {
                key.SetName(key.GetName()?.ToSnakeCase());
            }

            foreach (var key in entity.GetForeignKeys())
            {
                key.SetConstraintName(key.GetConstraintName()?.ToSnakeCase());
            }

            foreach (var index in entity.GetIndexes())
            {
                index.SetDatabaseName(index.GetDatabaseName()?.ToSnakeCase());
            }
        }
    }
}

/// <summary>
/// String extension for converting PascalCase to snake_case
/// </summary>
internal static class StringExtensions
{
    public static string ToSnakeCase(this string? input)
    {
        if (string.IsNullOrEmpty(input))
            return input ?? string.Empty;

        var result = new System.Text.StringBuilder();
        result.Append(char.ToLowerInvariant(input[0]));

        for (int i = 1; i < input.Length; i++)
        {
            if (char.IsUpper(input[i]))
            {
                result.Append('_');
                result.Append(char.ToLowerInvariant(input[i]));
            }
            else
            {
                result.Append(input[i]);
            }
        }

        return result.ToString();
    }
}
