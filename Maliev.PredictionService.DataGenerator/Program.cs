using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Maliev.PredictionService.Data.Contexts;
using Maliev.PredictionService.DataGenerator.Services;
using System;
using System.Threading.Tasks;

namespace Maliev.PredictionService.DataGenerator
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();

            using (var scope = host.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                try
                {
                    var dataGeneratorService = services.GetRequiredService<FdmDataGeneratorService>();
                    await dataGeneratorService.GenerateAndInsertFdmDataAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An error occurred during data generation: {ex.Message}");
                }
            }

            await host.RunAsync();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((hostingContext, configuration) =>
                {
                    configuration.Sources.Clear();
                    configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                    configuration.AddEnvironmentVariables();
                    if (args != null)
                    {
                        configuration.AddCommandLine(args);
                    }
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddDbContext<PredictionServiceContext>(options =>
                        options.UseSqlServer(hostContext.Configuration.GetConnectionString("PredictionServiceDbConnection")));

                    services.AddTransient<FdmDataGeneratorService>();
                });
    }
}