using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Maliev.PredictionService.Domain.Entities;
using Maliev.PredictionService.Domain.ValueObjects;
using System.Text.Json;

namespace Maliev.PredictionService.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for TrainingJob entity
/// Schema: training
/// </summary>
public class TrainingJobConfiguration : IEntityTypeConfiguration<TrainingJob>
{
    public void Configure(EntityTypeBuilder<TrainingJob> builder)
    {
        builder.ToTable("training_jobs", schema: "training");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.ModelType)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(t => t.Status)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(t => t.StartTime)
            .IsRequired();

        builder.Property(t => t.EndTime);

        // DurationSeconds is computed property, no database column needed
        builder.Ignore(t => t.DurationSeconds);

        // PerformanceMetrics as JSONB column
        builder.OwnsOne(t => t.PerformanceMetrics, metricsBuilder =>
        {
            metricsBuilder.ToJson();
        });

        // ValidationResults as JSONB
        builder.Property(t => t.ValidationResults)
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions?)null));

        builder.Property(t => t.ErrorMessage)
            .HasMaxLength(2000);

        builder.Property(t => t.TriggeredBy)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(t => t.TriggeredByUserId)
            .HasMaxLength(100);

        // Hyperparameters as JSONB
        builder.Property(t => t.Hyperparameters)
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions?)null));

        builder.Property(t => t.Logs)
            .HasColumnType("text");

        // Indexes
        builder.HasIndex(t => new { t.Status, t.StartTime })
            .HasDatabaseName("ix_training_jobs_status_start");

        builder.HasIndex(t => t.ModelType)
            .HasDatabaseName("ix_training_jobs_model_type");

        builder.HasIndex(t => t.StartTime)
            .HasDatabaseName("ix_training_jobs_start_time");
    }
}
