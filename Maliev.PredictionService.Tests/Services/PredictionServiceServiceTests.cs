using Xunit;
using Moq;
using Microsoft.EntityFrameworkCore;
using Maliev.PredictionService.Data.Contexts;
using Maliev.PredictionService.Data.Entities;
using Maliev.PredictionService.Api.Services;
using Maliev.PredictionService.Api.DTOs;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Maliev.PredictionService.Tests.Services
{
    public class PredictionServiceServiceTests
    {
        private PredictionServiceContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<PredictionServiceContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new PredictionServiceContext(options);
        }

        [Fact]
        public async Task GetAllFdmPrintDataAsync_ReturnsAllData()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            context.FdmPrintData.AddRange(new List<FdmPrintData>
            {
                new FdmPrintData { Id = 1, Material = "PLA", LayerHeight = 0.2f, InfillPercent = 20f, DimensionX = 10, DimensionY = 10, DimensionZ = 10, OutboxVolume = 100, Volume = 50, EstimatedWeight = 10, NumberOfLayers = 100, PrintTimeMinutes = 60 },
                new FdmPrintData { Id = 2, Material = "ABS", LayerHeight = 0.3f, InfillPercent = 30f, DimensionX = 20, DimensionY = 20, DimensionZ = 20, OutboxVolume = 200, Volume = 100, EstimatedWeight = 20, NumberOfLayers = 200, PrintTimeMinutes = 120 }
            });
            await context.SaveChangesAsync();

            var service = new PredictionServiceService(context);

            // Act
            var result = await service.GetAllFdmPrintDataAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
        }

        [Fact]
        public async Task GetFdmPrintDataByIdAsync_ReturnsCorrectData()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            context.FdmPrintData.Add(new FdmPrintData { Id = 1, Material = "PLA", LayerHeight = 0.2f, InfillPercent = 20f, DimensionX = 10, DimensionY = 10, DimensionZ = 10, OutboxVolume = 100, Volume = 50, EstimatedWeight = 10, NumberOfLayers = 100, PrintTimeMinutes = 60 });
            await context.SaveChangesAsync();

            var service = new PredictionServiceService(context);

            // Act
            var result = await service.GetFdmPrintDataByIdAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.Id);
            Assert.Equal("PLA", result.Material);
        }

        [Fact]
        public async Task GetFdmPrintDataByIdAsync_ReturnsNull_WhenNotFound()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var service = new PredictionServiceService(context);

            // Act
            var result = await service.GetFdmPrintDataByIdAsync(99);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task CreateFdmPrintDataAsync_AddsNewData()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var service = new PredictionServiceService(context);
            var request = new CreateFdmPrintDataRequest
            {
                Material = "PETG", LayerHeight = 0.25f, InfillPercent = 25f, DimensionX = 15, DimensionY = 15, DimensionZ = 15, OutboxVolume = 150, Volume = 75, EstimatedWeight = 15, NumberOfLayers = 150, PrintTimeMinutes = 90
            };

            // Act
            var result = await service.CreateFdmPrintDataAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("PETG", result.Material);
            Assert.Equal(1, context.FdmPrintData.Count());
        }

        [Fact]
        public async Task UpdateFdmPrintDataAsync_UpdatesExistingData()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            context.FdmPrintData.Add(new FdmPrintData { Id = 1, Material = "PLA", LayerHeight = 0.2f, InfillPercent = 20f, DimensionX = 10, DimensionY = 10, DimensionZ = 10, OutboxVolume = 100, Volume = 50, EstimatedWeight = 10, NumberOfLayers = 100, PrintTimeMinutes = 60 });
            await context.SaveChangesAsync();

            var service = new PredictionServiceService(context);
            var request = new UpdateFdmPrintDataRequest
            {
                Id = 1, Material = "Updated PLA", LayerHeight = 0.2f, InfillPercent = 20f, DimensionX = 10, DimensionY = 10, DimensionZ = 10, OutboxVolume = 100, Volume = 50, EstimatedWeight = 10, NumberOfLayers = 100, PrintTimeMinutes = 60
            };

            // Act
            var result = await service.UpdateFdmPrintDataAsync(request);

            // Assert
            Assert.True(result);
            var updatedData = await context.FdmPrintData.FindAsync(1);
            Assert.Equal("Updated PLA", updatedData!.Material);
        }

        [Fact]
        public async Task UpdateFdmPrintDataAsync_ReturnsFalse_WhenNotFound()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var service = new PredictionServiceService(context);
            var request = new UpdateFdmPrintDataRequest
            {
                Id = 99, Material = "NonExistent", LayerHeight = 0.2f, InfillPercent = 20f, DimensionX = 10, DimensionY = 10, DimensionZ = 10, OutboxVolume = 100, Volume = 50, EstimatedWeight = 10, NumberOfLayers = 100, PrintTimeMinutes = 60
            };

            // Act
            var result = await service.UpdateFdmPrintDataAsync(request);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task DeleteFdmPrintDataAsync_RemovesData()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            context.FdmPrintData.Add(new FdmPrintData { Id = 1, Material = "PLA", LayerHeight = 0.2f, InfillPercent = 20f, DimensionX = 10, DimensionY = 10, DimensionZ = 10, OutboxVolume = 100, Volume = 50, EstimatedWeight = 10, NumberOfLayers = 100, PrintTimeMinutes = 60 });
            await context.SaveChangesAsync();

            var service = new PredictionServiceService(context);

            // Act
            var result = await service.DeleteFdmPrintDataAsync(1);

            // Assert
            Assert.True(result);
            Assert.Equal(0, context.FdmPrintData.Count());
        }

        [Fact]
        public async Task DeleteFdmPrintDataAsync_ReturnsFalse_WhenNotFound()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var service = new PredictionServiceService(context);

            // Act
            var result = await service.DeleteFdmPrintDataAsync(99);

            // Assert
            Assert.False(result);
        }
    }
}
