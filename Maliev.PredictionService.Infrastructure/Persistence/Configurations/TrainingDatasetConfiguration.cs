using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Maliev.PredictionService.Domain.Entities;
using System.Text.Json;

namespace Maliev.PredictionService.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for TrainingDataset entity
/// Schema: training
/// </summary>
public class TrainingDatasetConfiguration : IEntityTypeConfiguration<TrainingDataset>
{
    public void Configure(EntityTypeBuilder<TrainingDataset> builder)
    {
        builder.ToTable("training_datasets", schema: "training");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.ModelType)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(t => t.RecordCount)
            .IsRequired();

        builder.Property(t => t.DateRangeStart)
            .IsRequired();

        builder.Property(t => t.DateRangeEnd)
            .IsRequired();

        // FeatureColumns as JSONB (list of strings)
        builder.Property(t => t.FeatureColumns)
            .IsRequired()
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null)!);

        builder.Property(t => t.TargetColumn)
            .IsRequired()
            .HasMaxLength(200);

        // DataQualityMetrics as JSONB
        builder.Property(t => t.DataQualityMetrics)
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions?)null));

        builder.Property(t => t.FilePath)
            .HasMaxLength(500);

        builder.Property(t => t.DatasetHash)
            .HasMaxLength(64); // SHA-256 hash

        builder.Property(t => t.CreatedAt)
            .IsRequired();

        builder.Property(t => t.CreatedBy)
            .HasMaxLength(100);

        // Indexes
        builder.HasIndex(t => t.ModelType)
            .HasDatabaseName("ix_training_datasets_model_type");

        builder.HasIndex(t => t.DatasetHash)
            .HasDatabaseName("ix_training_datasets_hash");

        builder.HasIndex(t => t.CreatedAt)
            .HasDatabaseName("ix_training_datasets_created_at");

        // Relationship with TrainingJob
        builder.HasOne(t => t.TrainingJob)
            .WithOne(tj => tj.TrainingDataset)
            .HasForeignKey<TrainingDataset>(t => t.TrainingJobId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
