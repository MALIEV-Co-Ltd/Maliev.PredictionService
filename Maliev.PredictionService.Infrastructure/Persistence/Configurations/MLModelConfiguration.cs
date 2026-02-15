using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Maliev.PredictionService.Domain.Entities;
using Maliev.PredictionService.Domain.ValueObjects;
using System.Text.Json;

namespace Maliev.PredictionService.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for MLModel entity
/// Schema: ml_models
/// </summary>
public class MLModelConfiguration : IEntityTypeConfiguration<MLModel>
{
    public void Configure(EntityTypeBuilder<MLModel> builder)
    {
        builder.ToTable("ml_models", schema: "ml_models");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.ModelType)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(m => m.Status)
            .IsRequired()
            .HasConversion<string>();

        // ModelVersion as complex property (value object inline)
        builder.ComplexProperty(m => m.ModelVersion, versionBuilder =>
        {
            versionBuilder.Property(v => v.Major).HasColumnName("version_major").IsRequired();
            versionBuilder.Property(v => v.Minor).HasColumnName("version_minor").IsRequired();
            versionBuilder.Property(v => v.Patch).HasColumnName("version_patch").IsRequired();
        });

        // PerformanceMetrics as JSONB column
        builder.Property(m => m.PerformanceMetrics)
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<PerformanceMetrics>(v, (JsonSerializerOptions?)null));

        builder.Property(m => m.FilePath)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(m => m.Algorithm)
            .HasMaxLength(100);

        // Metadata as JSONB
        builder.Property(m => m.Metadata)
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions?)null)!);

        builder.Property(m => m.CreatedAt)
            .IsRequired();

        builder.Property(m => m.UpdatedAt)
            .IsRequired();

        builder.Property(m => m.CreatedBy)
            .HasMaxLength(100);

        builder.Property(m => m.UpdatedBy)
            .HasMaxLength(100);

        // Indexes
        builder.HasIndex(m => new { m.ModelType, m.Status })
            .HasDatabaseName("ix_ml_models_type_status");

        builder.HasIndex(m => m.CreatedAt)
            .HasDatabaseName("ix_ml_models_created_at");

        // Relationship with TrainingJob
        builder.HasOne(m => m.TrainingJob)
            .WithOne(tj => tj.Model)
            .HasForeignKey<MLModel>(m => m.TrainingJobId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
