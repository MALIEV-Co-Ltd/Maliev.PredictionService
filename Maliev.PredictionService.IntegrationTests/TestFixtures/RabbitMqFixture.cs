using Testcontainers.RabbitMq;

namespace Maliev.PredictionService.IntegrationTests.TestFixtures;

/// <summary>
/// xUnit fixture for RabbitMQ Testcontainer
/// </summary>
public class RabbitMqFixture : IAsyncLifetime
{
    private readonly RabbitMqContainer _container;

    public string ConnectionString => _container.GetConnectionString();
    public string Hostname => _container.Hostname;
    public ushort Port => _container.GetMappedPublicPort(5672);

    public RabbitMqFixture()
    {
        _container = new RabbitMqBuilder("rabbitmq:3-management-alpine")
            .WithUsername("guest")
            .WithPassword("guest")
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
