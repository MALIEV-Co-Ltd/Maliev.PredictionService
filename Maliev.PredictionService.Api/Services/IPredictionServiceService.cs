using Maliev.PredictionService.Api.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Maliev.PredictionService.Api.Services
{
    public interface IPredictionServiceService
    {
        Task<IEnumerable<FdmPrintDataDto>> GetAllFdmPrintDataAsync();
        Task<FdmPrintDataDto?> GetFdmPrintDataByIdAsync(int id);
        Task<FdmPrintDataDto> CreateFdmPrintDataAsync(CreateFdmPrintDataRequest request);
        Task<bool> UpdateFdmPrintDataAsync(UpdateFdmPrintDataRequest request);
        Task<bool> DeleteFdmPrintDataAsync(int id);
    }
}
