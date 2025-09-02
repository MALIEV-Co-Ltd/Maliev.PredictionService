using Maliev.PredictionService.Data.Contexts;
using Maliev.PredictionService.Data.Entities;
using System;
using System.Threading.Tasks;

namespace Maliev.PredictionService.DataGenerator.Services
{
    public class FdmDataGeneratorService
    {
        private readonly PredictionServiceContext _context;

        public FdmDataGeneratorService(PredictionServiceContext context)
        {
            _context = context;
        }

        public async Task GenerateAndInsertFdmDataAsync()
        {
            Console.WriteLine("Generating and inserting FDM Print Data...");

            var random = new Random();

            for (int i = 0; i < 10; i++) // Generate 10 records for demonstration
            {
                var data = new FdmPrintData
                {
                    Material = "PLA" + random.Next(1, 3),
                    LayerHeight = (float)Math.Round(random.NextDouble() * (0.3 - 0.1) + 0.1, 2),
                    InfillPercent = (float)random.Next(10, 100),
                    DimensionX = (float)random.Next(10, 200),
                    DimensionY = (float)random.Next(10, 200),
                    DimensionZ = (float)random.Next(10, 200),
                    OutboxVolume = (float)random.Next(100, 5000),
                    Volume = (float)random.Next(50, 4000),
                    EstimatedWeight = (float)Math.Round(random.NextDouble() * (100 - 10) + 10, 2),
                    NumberOfLayers = random.Next(50, 1000),
                    PrintTimeMinutes = (float)random.Next(30, 1000)
                };

                _context.FdmPrintData.Add(data);
            }

            await _context.SaveChangesAsync();
            Console.WriteLine("FDM Print Data generation complete.");
        }
    }
}
