using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using System.Net.NetworkInformation;
using UL.AuditPipeline.Api.Services;
using UL.AuditPipeline.Core.Models;
using UL.AuditPipeline.Shared.Models;

namespace UL.AuditPipeline.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InspectionsController : ControllerBase
    {
        private readonly IBlobStorageService _storageService;
        private readonly CosmosClient _cosmosClient;

        public InspectionsController(IBlobStorageService storageService, CosmosClient cosmosClient)
        {
            _storageService = storageService;
            _cosmosClient = cosmosClient;
        }

        /// <summary>
        /// Uploads an inspection file to Blob Storage and enqueues a message for background processing.
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Retrieves all inspection records from Cosmos DB, ordered by processedAtUtc in descending order.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> GetInspections([FromQuery] string? status)
        {
            var container = _cosmosClient.GetContainer("AuditDB", "Inspections");

            //var query = new QueryDefinition("SELECT * FROM c ORDER BY c.processedAtUtc DESC");
            //using var iterator = container.GetItemQueryIterator<InspectionRecord>(query);
            //var results = new List<InspectionRecord>();

            // Use LINQ to query the container for InspectionRecord items
            IQueryable<InspectionRecord> queryable = container.GetItemLinqQueryable<InspectionRecord>();

            // Apply filtering based on the PassFailStatus if provided
            if (!string.IsNullOrEmpty(status))
            {
                queryable = queryable.Where(record => record.PassFailStatus == status);
            }

            // Order the results by ProcessedAtUtc in descending order
            queryable = queryable.OrderByDescending(record => record.ProcessedAtUtc);

            // Use a FeedIterator to retrieve the results in pages
            using FeedIterator<InspectionRecord> iterator = queryable.ToFeedIterator();
            var results = new List<InspectionRecord>();

            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                results.AddRange(response);
            }

            return Ok(results);
        }

        /// <summary>
        /// Retrieves a specific inspection record by its ID and partition key (inspectorId) from Cosmos DB.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="inspectorId"></param>
        /// <returns></returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetInspectionById(string id, [FromQuery] string inspectorId)
        {
            if (string.IsNullOrEmpty(inspectorId))
            {
                return BadRequest(new { error = "The 'inspectorId' query parameter is required as the partition key." });
            }

            try
            {
                var container = _cosmosClient.GetContainer("AuditDB", "Inspections");

                // A point read requires both the Document ID and the Partition Key value
                ItemResponse<InspectionRecord> response = await container.ReadItemAsync<InspectionRecord>(
                    id: id,
                    partitionKey: new PartitionKey(inspectorId)
                );

                return Ok(response.Resource);
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return NotFound(new { error = "Inspection record not found." });
            }
        }

        /// <summary>
        /// Updates an existing inspection record in Cosmos DB. The record is identified by its ID and partition key (inspectorId). 
        /// Only the PassFailStatus and Comments fields can be updated.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="inspectorId"></param>
        /// <param name="updateDto"></param>
        /// <returns></returns>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateInspection(string id, [FromQuery] string inspectorId, [FromBody] UpdateInspectionDto updateDto)
        {
            if (string.IsNullOrEmpty(inspectorId))
            {
                return BadRequest(new { error = "The 'inspectorId' query parameter is required as the partition key." });
            }

            var container = _cosmosClient.GetContainer("AuditDB", "Inspections");

            try
            {
                // Read the existing item from Cosmos DB
                ItemResponse<InspectionRecord> existingItem = await container.ReadItemAsync<InspectionRecord>(
                    id: id,
                    partitionKey: new PartitionKey(inspectorId)
                );

                var itemToUpdate = existingItem.Resource;
                itemToUpdate.PassFailStatus = updateDto.PassFailStatus;
                itemToUpdate.Comments = updateDto.Comments;

                // Replace the item in Cosmos DB
                ItemResponse<InspectionRecord> response = await container.ReplaceItemAsync(
                    item: itemToUpdate,
                    id: id,
                    partitionKey: new PartitionKey(inspectorId)
                );

                return Ok(response.Resource);
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return NotFound(new { error = "Inspection record not found." });
            }
        }

        /// <summary>
        /// Deletes an inspection record from Cosmos DB. The record is identified by its ID and partition key (inspectorId).
        /// </summary>
        /// <param name="id"></param>
        /// <param name="inspectorId"></param>
        /// <returns></returns>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteInspection(string id, [FromQuery] string inspectorId)
        {
            if (string.IsNullOrEmpty(inspectorId))
            {
                return BadRequest(new { error = "The 'inspectorId' query parameter is required as the partition key." });
            }

            var container = _cosmosClient.GetContainer("AuditDB", "Inspections");

            try
            {
                // Delete the item from Cosmos DB using the provided ID and partition key
                await container.DeleteItemAsync<InspectionRecord>(
                    id: id,
                    partitionKey: new PartitionKey(inspectorId)
                );

                return NoContent(); // HTTP 204
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return NotFound(new { error = "Inspection record not found." });
            }
        }
    }
}
