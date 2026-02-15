using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Maliev.PredictionService.Infrastructure.Persistence;

/// <summary>
/// Design-time factory for PredictionDbContext.
/// Enables EF Core migrations to create the DbContext without full application services.
/// </summary>
public class PredictionDbContextFactory : IDesignTimeDbContextFactory<PredictionDbContext>
{
    public PredictionDbContext CreateDbContext(string[] args)
    {
        // Use a default connection string for design-time operations (migrations)
        // This will be overridden at runtime by the actual configuration
        var optionsBuilder = new DbContextOptionsBuilder<PredictionDbContext>();

        optionsBuilder.UseNpgsql(
            "Host=localhost;Database=prediction_service_dev;Username=postgres;Password=postgres",
            npgsqlOptions => npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "public"));

        return new PredictionDbContext(optionsBuilder.Options);
    }
}
