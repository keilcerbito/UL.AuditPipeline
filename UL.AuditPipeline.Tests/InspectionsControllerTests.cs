using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Moq;
using UL.AuditPipeline.Api.Controllers;
using UL.AuditPipeline.Api.Services;

namespace UL.AuditPipeline.Tests
{
    public class InspectionsControllerTests
    {
        [Fact]
        public async Task UploadInspection_NoFile_ReturnsBadRequest()
        {
            // Arrange: Mock the dependencies so we don't need real Azure connections
            var mockStorageService = new Mock<IBlobStorageService>();
            var mockCosmosClient = new Mock<CosmosClient>();

            var controller = new InspectionsController(mockStorageService.Object, mockCosmosClient.Object);

            // Act: Call the UploadInspection method with a null file
            var result = await controller.UploadInspection(null!);

            // Assert: Verify the API protects itself and returns a 400 Bad Request
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(badRequestResult.Value);
        }

        [Fact]
        public async Task UploadInspection_ValidFile_ReturnsAcceptedAndCallsServices()
        {
            // Arrange: Mock the dependencies
            var mockStorageService = new Mock<IBlobStorageService>();
            var mockCosmosClient = new Mock<CosmosClient>();

            // Arrange: Create a fake uploaded file (IFormFile)
            var mockFile = new Mock<IFormFile>();
            var content = "Fake JSON Content";
            var fileName = "test-report.json";
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(content);
            writer.Flush();
            stream.Position = 0;

            mockFile.Setup(f => f.OpenReadStream()).Returns(stream);
            mockFile.Setup(f => f.FileName).Returns(fileName);
            mockFile.Setup(f => f.Length).Returns(stream.Length);

            var controller = new InspectionsController(mockStorageService.Object, mockCosmosClient.Object);

            // Act: Call the controller with our fake file
            var result = await controller.UploadInspection(mockFile.Object);

            // Assert 1: The API returned a 202 Accepted
            var acceptedResult = Assert.IsType<AcceptedResult>(result);
            Assert.NotNull(acceptedResult.Value);

            // Assert 2: Verify the storage service was actually called EXACTLY once
            mockStorageService.Verify(
                s => s.UploadInspectionBlobAsync(It.IsAny<string>(), It.IsAny<Stream>()),
                Times.Once);

            mockStorageService.Verify(
                s => s.EnqueueInspectionMessageAsync("inspection-queue", It.IsAny<string>()),
                Times.Once);
        }
    }
}