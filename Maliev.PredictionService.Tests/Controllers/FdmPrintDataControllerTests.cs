using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Maliev.PredictionService.Api.Controllers;
using Maliev.PredictionService.Api.Services;
using Maliev.PredictionService.Api.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Asp.Versioning.ApiExplorer;
using Asp.Versioning;

namespace Maliev.PredictionService.Tests.Controllers
{
    public class FdmPrintDataControllerTests
    {
        private readonly Mock<IPredictionServiceService> _mockService;
        private readonly FdmPrintDataController _controller;

        public FdmPrintDataControllerTests()
        {
            _mockService = new Mock<IPredictionServiceService>();
            _controller = new FdmPrintDataController(_mockService.Object);

            // Mock HttpContext for API Versioning
            var httpContext = new DefaultHttpContext();
            var apiVersioningFeature = new Mock<IApiVersioningFeature>();
            apiVersioningFeature.Setup(f => f.RequestedApiVersion).Returns(new ApiVersion(1, 0));
            httpContext.Features.Set(apiVersioningFeature.Object);
            _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        }

        [Fact]
        public async Task GetAllFdmPrintData_ReturnsOkResult_WithListOfData()
        {
            // Arrange
            var data = new List<FdmPrintDataDto> { new FdmPrintDataDto { Id = 1, Material = "PLA" } };
            _mockService.Setup(s => s.GetAllFdmPrintDataAsync()).ReturnsAsync(data);

            // Act
            var result = await _controller.GetAllFdmPrintData();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnValue = Assert.IsType<List<FdmPrintDataDto>>(okResult.Value);
            Assert.Single(returnValue);
        }

        [Fact]
        public async Task GetFdmPrintDataById_ReturnsOkResult_WithData()
        {
            // Arrange
            var data = new FdmPrintDataDto { Id = 1, Material = "PLA" };
            _mockService.Setup(s => s.GetFdmPrintDataByIdAsync(1)).ReturnsAsync(data);

            // Act
            var result = await _controller.GetFdmPrintDataById(1);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnValue = Assert.IsType<FdmPrintDataDto>(okResult.Value);
            Assert.Equal(1, returnValue.Id);
        }

        [Fact]
        public async Task GetFdmPrintDataById_ReturnsNotFoundResult_WhenDataDoesNotExist()
        {
            // Arrange
            _mockService.Setup(s => s.GetFdmPrintDataByIdAsync(It.IsAny<int>())).ReturnsAsync((FdmPrintDataDto?)null);

            // Act
            var result = await _controller.GetFdmPrintDataById(1);

            // Assert
            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task CreateFdmPrintData_ReturnsCreatedAtActionResult()
        {
            // Arrange
            var request = new CreateFdmPrintDataRequest { Material = "PLA", LayerHeight = 0.2f, InfillPercent = 20f, DimensionX = 10, DimensionY = 10, DimensionZ = 10, OutboxVolume = 100, Volume = 50, EstimatedWeight = 10, NumberOfLayers = 100, PrintTimeMinutes = 60 };
            var createdData = new FdmPrintDataDto { Id = 1, Material = "PLA" };
            _mockService.Setup(s => s.CreateFdmPrintDataAsync(It.IsAny<CreateFdmPrintDataRequest>())).ReturnsAsync(createdData);

            // Act
            var result = await _controller.CreateFdmPrintData(request);

            // Assert
            var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            Assert.Equal(nameof(_controller.GetFdmPrintDataById), createdAtActionResult.ActionName);
            Assert.Equal(createdData.Id, ((FdmPrintDataDto)createdAtActionResult.Value).Id);
        }

        [Fact]
        public async Task UpdateFdmPrintData_ReturnsNoContentResult()
        {
            // Arrange
            var request = new UpdateFdmPrintDataRequest
            {
                Id = 1,
                Material = "Updated PLA",
                LayerHeight = 0.2f,
                InfillPercent = 20f,
                DimensionX = 10f,
                DimensionY = 10f,
                DimensionZ = 10f,
                OutboxVolume = 100f,
                Volume = 50f,
                EstimatedWeight = 10f,
                NumberOfLayers = 100f,
                PrintTimeMinutes = 60f
            };
            _mockService.Setup(s => s.UpdateFdmPrintDataAsync(It.IsAny<UpdateFdmPrintDataRequest>())).ReturnsAsync(true);

            // Act
            var result = await _controller.UpdateFdmPrintData(1, request);

            // Assert
            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task UpdateFdmPrintData_ReturnsBadRequest_WhenIdMismatch()
        {
            // Arrange
            var request = new UpdateFdmPrintDataRequest
            {
                Id = 1,
                Material = "Updated PLA", // This is a non-null literal string
                LayerHeight = 0.2f,
                InfillPercent = 20f,
                DimensionX = 10f,
                DimensionY = 10f,
                DimensionZ = 10f,
                OutboxVolume = 100f,
                Volume = 50f,
                EstimatedWeight = 10f,
                NumberOfLayers = 100f,
                PrintTimeMinutes = 60f
            };

            // Act
            var result = await _controller.UpdateFdmPrintData(2, request);

            // Assert
            Assert.IsType<BadRequestResult>(result);
        }

        [Fact]
        public async Task UpdateFdmPrintData_ReturnsNotFound_WhenDataDoesNotExist()
        {
            // Arrange
            var request = new UpdateFdmPrintDataRequest
            {
                Id = 1,
                Material = "Updated PLA",
                LayerHeight = 0.2f,
                InfillPercent = 20f,
                DimensionX = 10f,
                DimensionY = 10f,
                DimensionZ = 10f,
                OutboxVolume = 100f,
                Volume = 50f,
                EstimatedWeight = 10f,
                NumberOfLayers = 100f,
                PrintTimeMinutes = 60f
            };
            _mockService.Setup(s => s.UpdateFdmPrintDataAsync(It.IsAny<UpdateFdmPrintDataRequest>())).ReturnsAsync(false);

            // Act
            var result = await _controller.UpdateFdmPrintData(1, request);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task DeleteFdmPrintData_ReturnsNoContentResult()
        {
            // Arrange
            _mockService.Setup(s => s.DeleteFdmPrintDataAsync(It.IsAny<int>())).ReturnsAsync(true);

            // Act
            var result = await _controller.DeleteFdmPrintData(1);

            // Assert
            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task DeleteFdmPrintData_ReturnsNotFoundResult_WhenDataDoesNotExist()
        {
            // Arrange
            _mockService.Setup(s => s.DeleteFdmPrintDataAsync(It.IsAny<int>())).ReturnsAsync(false);

            // Act
            var result = await _controller.DeleteFdmPrintData(1);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }
    }
}
