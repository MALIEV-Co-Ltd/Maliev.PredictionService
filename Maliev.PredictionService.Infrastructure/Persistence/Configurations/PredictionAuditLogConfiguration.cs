using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Maliev.PredictionService.Domain.Entities;
using System.Text.Json;

namespace Maliev.PredictionService.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for PredictionAuditLog entity
/// Schema: audit (with monthly partitioning)
/// </summary>
public class PredictionAuditLogConfiguration : IEntityTypeConfiguration<PredictionAuditLog>
{
    public void Configure(EntityTypeBuilder<PredictionAuditLog> builder)
    {
        builder.ToTable("prediction_audit_logs", schema: "audit");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.RequestId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(p => p.ModelType)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(p => p.ModelVersion)
            .IsRequired()
            .HasMaxLength(20);

        // InputFeatures as JSONB
        builder.Property(p => p.InputFeatures)
            .IsRequired()
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions?)null)!);

        // OutputPrediction as JSONB
        builder.Property(p => p.OutputPrediction)
            .IsRequired()
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions?)null)!);

        builder.Property(p => p.CacheStatus)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(p => p.ResponseTimeMs)
            .IsRequired();

        builder.Property(p => p.UserId)
            .HasMaxLength(100);

        builder.Property(p => p.TenantId)
            .HasMaxLength(100);

        builder.Property(p => p.Timestamp)
            .IsRequired();

        // ActualOutcome as JSONB (nullable)
        builder.Property(p => p.ActualOutcome)
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions?)null));

        builder.Property(p => p.ErrorMessage)
            .HasMaxLength(1000);

        // Indexes for common queries
        builder.HasIndex(p => p.RequestId)
            .HasDatabaseName("ix_prediction_audit_logs_request_id");

        builder.HasIndex(p => new { p.ModelType, p.Timestamp })
            .HasDatabaseName("ix_prediction_audit_logs_type_timestamp");

        builder.HasIndex(p => new { p.UserId, p.Timestamp })
            .HasDatabaseName("ix_prediction_audit_logs_user_timestamp");

        builder.HasIndex(p => p.Timestamp)
            .HasDatabaseName("ix_prediction_audit_logs_timestamp");

        // Partial index for records with actual outcomes (for model performance tracking)
        builder.HasIndex(p => p.ActualOutcomeReceivedAt)
            .HasDatabaseName("ix_prediction_audit_logs_actual_outcome")
            .HasFilter("actual_outcome_received_at IS NOT NULL");
    }
}
