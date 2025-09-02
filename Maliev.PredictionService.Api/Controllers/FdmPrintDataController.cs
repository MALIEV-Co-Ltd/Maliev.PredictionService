using Maliev.PredictionService.Api.DTOs;
using Maliev.PredictionService.Api.Services;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using Asp.Versioning;

namespace Maliev.PredictionService.Api.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("v{version:apiVersion}/prediction-service/fdm-print-data")]
    public class FdmPrintDataController : ControllerBase
    {
        private readonly IPredictionServiceService _predictionServiceService;

        public FdmPrintDataController(IPredictionServiceService predictionServiceService)
        {
            _predictionServiceService = predictionServiceService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<FdmPrintDataDto>>> GetAllFdmPrintData()
        {
            var data = await _predictionServiceService.GetAllFdmPrintDataAsync();
            return Ok(data);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<FdmPrintDataDto>> GetFdmPrintDataById(int id)
        {
            var data = await _predictionServiceService.GetFdmPrintDataByIdAsync(id);
            if (data == null)
            {
                return NotFound();
            }
            return Ok(data);
        }

        [HttpPost]
        public async Task<ActionResult<FdmPrintDataDto>> CreateFdmPrintData([FromBody] CreateFdmPrintDataRequest request)
        {
            var createdData = await _predictionServiceService.CreateFdmPrintDataAsync(request);
            return CreatedAtAction(nameof(GetFdmPrintDataById), new { id = createdData.Id, version = HttpContext.GetRequestedApiVersion()?.ToString() }, createdData);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateFdmPrintData(int id, [FromBody] UpdateFdmPrintDataRequest request)
        {
            if (id != request.Id)
            {
                return BadRequest();
            }

            var result = await _predictionServiceService.UpdateFdmPrintDataAsync(request);
            if (!result)
            {
                return NotFound();
            }
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteFdmPrintData(int id)
        {
            var result = await _predictionServiceService.DeleteFdmPrintDataAsync(id);
            if (!result)
            {
                return NotFound();
            }
            return NoContent();
        }
    }
}
