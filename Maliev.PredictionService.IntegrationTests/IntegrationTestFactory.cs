using System.Security.Cryptography;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Maliev.PredictionService.Infrastructure.Persistence;
using Maliev.PredictionService.Domain.Enums;
using Microsoft.ML;

using Testcontainers.PostgreSql;

using Testcontainers.Redis;



namespace Maliev.PredictionService.IntegrationTests;



public class IntegrationTestFactory : WebApplicationFactory<Program>, IAsyncLifetime

{

    private readonly RSA _testRsa = RSA.Create(2048);

    private readonly PostgreSqlContainer _postgresContainer = new PostgreSqlBuilder("postgres:18-alpine")

        .Build();

    private readonly RedisContainer _redisContainer = new RedisBuilder("redis:7-alpine")

        .Build();



    public async Task InitializeAsync()

    {

        await _postgresContainer.StartAsync();

        await _redisContainer.StartAsync();



        // Seed an active model for tests

        using var scope = Services.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<PredictionDbContext>();

        db.Database.Migrate();



        if (!db.MLModels.Any(m => m.ModelType == ModelType.PrintTime && m.Status == ModelStatus.Active))

        {

            var tempDir = Path.Combine(Path.GetTempPath(), "predictionservice_integration_tests");

            Directory.CreateDirectory(tempDir);

            var modelPath = Path.Combine(tempDir, "print_time_v1.0.0.zip");



            // Create a dummy model file (minimal ML.NET model)



            var mlContext = new Microsoft.ML.MLContext(seed: 42);



            var data = new List<Maliev.PredictionService.Infrastructure.ML.Trainers.PrintTimeTrainer.PrintTimeInput>();



            for (int i = 1; i <= 100; i++)



            {



                var vol = i * 500;



                data.Add(new() { Volume = vol, PrintSpeed = 50, PrintTimeMinutes = vol / 5f });



                data.Add(new() { Volume = vol, PrintSpeed = 100, PrintTimeMinutes = vol / 10f });



            }



            var dataView = mlContext.Data.LoadFromEnumerable(data);



            var pipeline = mlContext.Transforms.Concatenate("Features", "Volume", "PrintSpeed")



                .Append(mlContext.Regression.Trainers.Sdca(labelColumnName: "PrintTimeMinutes"));



            var model = pipeline.Fit(dataView);



            mlContext.Model.Save(model, dataView.Schema, modelPath);



            db.MLModels.Add(new Domain.Entities.MLModel

            {

                Id = Guid.NewGuid(),

                ModelType = ModelType.PrintTime,

                ModelVersion = Domain.ValueObjects.ModelVersion.Parse("1.0.0"),

                Status = ModelStatus.Active,

                FilePath = modelPath,

                DeploymentDate = DateTime.UtcNow,

                CreatedAt = DateTime.UtcNow,

                PerformanceMetrics = new Domain.ValueObjects.PerformanceMetrics { RSquared = 0.9 }

            });

            db.SaveChanges();

        }

    }



    public new async Task DisposeAsync()

    {

        await _postgresContainer.DisposeAsync();

        await _redisContainer.DisposeAsync();

    }



    protected override void ConfigureWebHost(IWebHostBuilder builder)

    {

        var publicKeyPem = _testRsa.ExportRSAPublicKeyPem();

        var publicKeyBase64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(publicKeyPem));



        builder.UseSetting("Jwt:PublicKey", publicKeyBase64);

        builder.UseSetting("CORS:AllowedOrigins:0", "http://localhost:3000");

        builder.UseSetting("ConnectionStrings:PredictionDbContext", _postgresContainer.GetConnectionString());

        builder.UseSetting("ConnectionStrings:redis", _redisContainer.GetConnectionString());



        builder.ConfigureTestServices(services =>

        {

            // Remove existing DbContext registration

            services.RemoveAll<DbContextOptions<PredictionDbContext>>();

            services.RemoveAll<PredictionDbContext>();



            // Add DbContext with Testcontainers PostgreSQL

            services.AddDbContext<PredictionDbContext>(options =>

            {

                options.UseNpgsql(_postgresContainer.GetConnectionString());

                options.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));

            });



            // Ensure database is created and migrated

            using var scope = services.BuildServiceProvider().CreateScope();

            var db = scope.ServiceProvider.GetRequiredService<PredictionDbContext>();

            db.Database.Migrate();



            services.PostConfigureAll<JwtBearerOptions>(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = false,
                    ValidateIssuerSigningKey = false,
                    SignatureValidator = (token, parameters) => new Microsoft.IdentityModel.JsonWebTokens.JsonWebToken(token),
                    NameClaimType = "sub",
                    RoleClaimType = "role"
                };
            });
        });
    }

    public HttpClient CreateAuthenticatedClient(string userId = "test-user", string[]? permissions = null)
    {
        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var claims = new List<System.Security.Claims.Claim>
        {
            new System.Security.Claims.Claim("sub", userId),
            new System.Security.Claims.Claim("role", "admin")
        };

        if (permissions != null)
        {
            foreach (var p in permissions)
            {
                claims.Add(new System.Security.Claims.Claim("permission", p));
            }
        }

        var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken("test", "test", claims, expires: DateTime.UtcNow.AddHours(1));
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", handler.WriteToken(token));
        return client;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _testRsa.Dispose();
        }
        base.Dispose(disposing);
    }
}
