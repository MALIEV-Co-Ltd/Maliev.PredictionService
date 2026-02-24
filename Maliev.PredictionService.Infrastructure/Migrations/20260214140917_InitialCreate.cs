using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Maliev.PredictionService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "ml_models");

            migrationBuilder.EnsureSchema(
                name: "audit");

            migrationBuilder.EnsureSchema(
                name: "training");

            migrationBuilder.CreateTable(
                name: "prediction_audit_logs",
                schema: "audit",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    request_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    model_type = table.Column<string>(type: "text", nullable: false),
                    model_version = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    input_features = table.Column<string>(type: "jsonb", nullable: false),
                    output_prediction = table.Column<string>(type: "jsonb", nullable: false),
                    cache_status = table.Column<string>(type: "text", nullable: false),
                    response_time_ms = table.Column<int>(type: "integer", nullable: false),
                    user_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    actual_outcome = table.Column<string>(type: "jsonb", nullable: true),
                    actual_outcome_received_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    error_message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_prediction_audit_logs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "training_jobs",
                schema: "training",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    model_type = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    start_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    end_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    validation_results = table.Column<string>(type: "jsonb", nullable: true),
                    training_dataset_id = table.Column<Guid>(type: "uuid", nullable: true),
                    model_id = table.Column<Guid>(type: "uuid", nullable: true),
                    error_message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    triggered_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    triggered_by_user_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    hyperparameters = table.Column<string>(type: "jsonb", nullable: true),
                    logs = table.Column<string>(type: "text", nullable: true),
                    performance_metrics = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_training_jobs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "ml_models",
                schema: "ml_models",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    model_type = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    training_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    deployment_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    file_path = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    training_job_id = table.Column<Guid>(type: "uuid", nullable: true),
                    algorithm = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    metadata = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    updated_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    version_major = table.Column<int>(type: "integer", nullable: false),
                    version_minor = table.Column<int>(type: "integer", nullable: false),
                    version_patch = table.Column<int>(type: "integer", nullable: false),
                    performance_metrics = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ml_models", x => x.id);
                    table.ForeignKey(
                        name: "fk_ml_models_training_jobs_training_job_id",
                        column: x => x.training_job_id,
                        principalSchema: "training",
                        principalTable: "training_jobs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "training_datasets",
                schema: "training",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    model_type = table.Column<string>(type: "text", nullable: false),
                    record_count = table.Column<int>(type: "integer", nullable: false),
                    date_range_start = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    date_range_end = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    feature_columns = table.Column<string>(type: "jsonb", nullable: false),
                    target_column = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    data_quality_metrics = table.Column<string>(type: "jsonb", nullable: true),
                    file_path = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    dataset_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    training_job_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_training_datasets", x => x.id);
                    table.ForeignKey(
                        name: "fk_training_datasets_training_jobs_training_job_id",
                        column: x => x.training_job_id,
                        principalSchema: "training",
                        principalTable: "training_jobs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "ix_ml_models_created_at",
                schema: "ml_models",
                table: "ml_models",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_ml_models_training_job_id",
                schema: "ml_models",
                table: "ml_models",
                column: "training_job_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ml_models_type_status",
                schema: "ml_models",
                table: "ml_models",
                columns: new[] { "model_type", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_prediction_audit_logs_actual_outcome",
                schema: "audit",
                table: "prediction_audit_logs",
                column: "actual_outcome_received_at",
                filter: "actual_outcome_received_at IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_prediction_audit_logs_request_id",
                schema: "audit",
                table: "prediction_audit_logs",
                column: "request_id");

            migrationBuilder.CreateIndex(
                name: "ix_prediction_audit_logs_timestamp",
                schema: "audit",
                table: "prediction_audit_logs",
                column: "timestamp");

            migrationBuilder.CreateIndex(
                name: "ix_prediction_audit_logs_type_timestamp",
                schema: "audit",
                table: "prediction_audit_logs",
                columns: new[] { "model_type", "timestamp" });

            migrationBuilder.CreateIndex(
                name: "ix_prediction_audit_logs_user_timestamp",
                schema: "audit",
                table: "prediction_audit_logs",
                columns: new[] { "user_id", "timestamp" });

            migrationBuilder.CreateIndex(
                name: "ix_training_datasets_created_at",
                schema: "training",
                table: "training_datasets",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_training_datasets_hash",
                schema: "training",
                table: "training_datasets",
                column: "dataset_hash");

            migrationBuilder.CreateIndex(
                name: "ix_training_datasets_model_type",
                schema: "training",
                table: "training_datasets",
                column: "model_type");

            migrationBuilder.CreateIndex(
                name: "ix_training_datasets_training_job_id",
                schema: "training",
                table: "training_datasets",
                column: "training_job_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_training_jobs_model_type",
                schema: "training",
                table: "training_jobs",
                column: "model_type");

            migrationBuilder.CreateIndex(
                name: "ix_training_jobs_start_time",
                schema: "training",
                table: "training_jobs",
                column: "start_time");

            migrationBuilder.CreateIndex(
                name: "ix_training_jobs_status_start",
                schema: "training",
                table: "training_jobs",
                columns: new[] { "status", "start_time" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ml_models",
                schema: "ml_models");

            migrationBuilder.DropTable(
                name: "prediction_audit_logs",
                schema: "audit");

            migrationBuilder.DropTable(
                name: "training_datasets",
                schema: "training");

            migrationBuilder.DropTable(
                name: "training_jobs",
                schema: "training");
        }
    }
}
