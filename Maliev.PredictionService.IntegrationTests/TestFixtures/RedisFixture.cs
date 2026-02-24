using Testcontainers.Redis;

namespace Maliev.PredictionService.IntegrationTests.TestFixtures;

/// <summary>
/// xUnit fixture for Redis Testcontainer
/// </summary>
public class RedisFixture : IAsyncLifetime
{
    private readonly RedisContainer _container;

    public string ConnectionString => _container.GetConnectionString();

    public RedisFixture()
    {
        _container = new RedisBuilder("redis:7-alpine")
            .WithCleanUp(true)
            .Build();
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
