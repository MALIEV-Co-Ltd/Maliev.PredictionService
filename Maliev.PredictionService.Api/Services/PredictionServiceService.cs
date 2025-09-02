using Maliev.PredictionService.Api.DTOs;
using Maliev.PredictionService.Data.Contexts;
using Maliev.PredictionService.Data.Entities;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Maliev.PredictionService.Api.Services
{
    public class PredictionServiceService : IPredictionServiceService
    {
        private readonly PredictionServiceContext _context;

        public PredictionServiceService(PredictionServiceContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<FdmPrintDataDto>> GetAllFdmPrintDataAsync()
        {
            return await _context.FdmPrintData
                .Select(f => new FdmPrintDataDto
                {
                    Id = f.Id,
                    Material = f.Material,
                    LayerHeight = f.LayerHeight,
                    InfillPercent = f.InfillPercent,
                    DimensionX = f.DimensionX,
                    DimensionY = f.DimensionY,
                    DimensionZ = f.DimensionZ,
                    OutboxVolume = f.OutboxVolume,
                    Volume = f.Volume,
                    EstimatedWeight = f.EstimatedWeight,
                    NumberOfLayers = f.NumberOfLayers,
                    PrintTimeMinutes = f.PrintTimeMinutes
                })
                .ToListAsync();
        }

        public async Task<FdmPrintDataDto?> GetFdmPrintDataByIdAsync(int id)
        {
            var fdmPrintData = await _context.FdmPrintData.FindAsync(id);
            if (fdmPrintData == null)
            {
                return null;
            }

            return new FdmPrintDataDto
            {
                Id = fdmPrintData.Id,
                Material = fdmPrintData.Material,
                LayerHeight = fdmPrintData.LayerHeight,
                InfillPercent = fdmPrintData.InfillPercent,
                DimensionX = fdmPrintData.DimensionX,
                DimensionY = fdmPrintData.DimensionY,
                DimensionZ = fdmPrintData.DimensionZ,
                OutboxVolume = fdmPrintData.OutboxVolume,
                Volume = fdmPrintData.Volume,
                EstimatedWeight = fdmPrintData.EstimatedWeight,
                NumberOfLayers = fdmPrintData.NumberOfLayers,
                PrintTimeMinutes = fdmPrintData.PrintTimeMinutes
            };
        }

        public async Task<FdmPrintDataDto> CreateFdmPrintDataAsync(CreateFdmPrintDataRequest request)
        {
            var fdmPrintData = new FdmPrintData
            {
                Material = request.Material,
                LayerHeight = request.LayerHeight,
                InfillPercent = request.InfillPercent,
                DimensionX = request.DimensionX,
                DimensionY = request.DimensionY,
                DimensionZ = request.DimensionZ,
                OutboxVolume = request.OutboxVolume,
                Volume = request.Volume,
                EstimatedWeight = request.EstimatedWeight,
                NumberOfLayers = request.NumberOfLayers,
                PrintTimeMinutes = request.PrintTimeMinutes
            };

            _context.FdmPrintData.Add(fdmPrintData);
            await _context.SaveChangesAsync();

            return new FdmPrintDataDto
            {
                Id = fdmPrintData.Id,
                Material = fdmPrintData.Material,
                LayerHeight = fdmPrintData.LayerHeight,
                InfillPercent = fdmPrintData.InfillPercent,
                DimensionX = fdmPrintData.DimensionX,
                DimensionY = fdmPrintData.DimensionY,
                DimensionZ = fdmPrintData.DimensionZ,
                OutboxVolume = fdmPrintData.OutboxVolume,
                Volume = fdmPrintData.Volume,
                EstimatedWeight = fdmPrintData.EstimatedWeight,
                NumberOfLayers = fdmPrintData.NumberOfLayers,
                PrintTimeMinutes = fdmPrintData.PrintTimeMinutes
            };
        }

        public async Task<bool> UpdateFdmPrintDataAsync(UpdateFdmPrintDataRequest request)
        {
            var fdmPrintData = await _context.FdmPrintData.FindAsync(request.Id);
            if (fdmPrintData == null)
            {
                return false;
            }

            fdmPrintData.Material = request.Material;
            fdmPrintData.LayerHeight = request.LayerHeight;
            fdmPrintData.InfillPercent = request.InfillPercent;
            fdmPrintData.DimensionX = request.DimensionX;
            fdmPrintData.DimensionY = request.DimensionY;
            fdmPrintData.DimensionZ = request.DimensionZ;
            fdmPrintData.OutboxVolume = request.OutboxVolume;
            fdmPrintData.Volume = request.Volume;
            fdmPrintData.EstimatedWeight = request.EstimatedWeight;
            fdmPrintData.NumberOfLayers = request.NumberOfLayers;
            fdmPrintData.PrintTimeMinutes = request.PrintTimeMinutes;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.FdmPrintData.Any(e => e.Id == request.Id))
                {
                    return false;
                }
                else
                {
                    throw;
                }
            }
            return true;
        }

        public async Task<bool> DeleteFdmPrintDataAsync(int id)
        {
            var fdmPrintData = await _context.FdmPrintData.FindAsync(id);
            if (fdmPrintData == null)
            {
                return false;
            }

            _context.FdmPrintData.Remove(fdmPrintData);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
