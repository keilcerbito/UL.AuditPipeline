using Microsoft.AspNetCore.Mvc;
using UL.AuditPipeline.Api.Services;

namespace UL.AuditPipeline.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InspectionsController : ControllerBase
    {
        private readonly IBlobStorageService _storageService;

        public InspectionsController(IBlobStorageService storageService)
        {
            _storageService = storageService;
        }

        [HttpPost]
        public async Task<IActionResult> UploadInspection(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { error = "No file uploaded." });
            }

            // Generate a unique tracking ID to prevent file name collisions
            var inspectionId = Guid.NewGuid().ToString();
            var fileName = $"{inspectionId}-{file.FileName}";

            // Upload the raw file to Blob Storage
            using var stream = file.OpenReadStream();
            await _storageService.UploadInspectionBlobAsync(fileName, stream);

            // Send a message to the Queue containing the file name
            await _storageService.EnqueueInspectionMessageAsync("inspection-queue", fileName);

            // Return 202 Accepted immediately
            return Accepted(new { trackingId = inspectionId, status = "Processing in background" });
        }
    }
}
