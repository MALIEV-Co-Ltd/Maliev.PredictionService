using Testcontainers.PostgreSql;

namespace Maliev.PredictionService.Tests.Integration.TestFixtures;

/// <summary>
/// xUnit fixture for PostgreSQL Testcontainer
/// </summary>
public class PostgreSqlFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container;

    public string ConnectionString => _container.GetConnectionString();

    public PostgreSqlFixture()
    {
        _container =
#pragma warning disable CS0618
        new PostgreSqlBuilder().WithImage("postgres:18-alpine")
            .WithDatabase("prediction_service_test")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .WithCleanUp(true)
            .Build();
#pragma warning restore CS0618
    }

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }
}
