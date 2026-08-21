using Azure.Storage.Blobs;
using Azure.Storage.Queues.Models;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using UL.AuditPipeline.Worker.Models;

namespace UL.AuditPipeline.Worker;

public class InspectionProcessor
{
    private readonly ILogger<InspectionProcessor> _logger;
    private readonly BlobServiceClient _blobServiceClient;
    private readonly Container _cosmosContainer;

    public InspectionProcessor(ILogger<InspectionProcessor> logger, CosmosClient cosmosClient)
    {
        // Store the injected logger for logging purposes
        _logger = logger;

        // Store the injected CosmosClient for database operations
        _cosmosContainer = cosmosClient.GetContainer("AuditDB", "Inspections");

        // Initialize the BlobServiceClient to connect to Azure Blob Storage
        _blobServiceClient = new BlobServiceClient("UseDevelopmentStorage=true");
    }

    [Function(nameof(InspectionProcessor))]
    public async Task Run([QueueTrigger("inspection-queue", Connection = "AzureWebJobsStorage")] string fileName)
    {
        _logger.LogInformation($"[START] Processing queue message for file: {fileName}");

        try
        {
            // Connect to the Blob Storage container
            var containerClient = _blobServiceClient.GetBlobContainerClient("inspections");
            var blobClient = containerClient.GetBlobClient(fileName);

            // Download the blob content as a stream
            var response = await blobClient.DownloadStreamingAsync();

            // Ensure the response is not null and has content
            if (response.Value.Content == null)
            {
                _logger.LogError($"[ERROR] Failed to download content for file: {fileName}");
                return;
            }

            using var stream = response.Value.Content;
            using var reader = new StreamReader(stream);
            using var jsonReader = new JsonTextReader(reader);

            // Deserialize the JSON content into an InspectionRecord object
            var serializer = new JsonSerializer();
            var inspectionData = serializer.Deserialize<InspectionRecord>(jsonReader);

            if (inspectionData == null)
            {
                _logger.LogError($"[ERROR] Could not parse file {fileName} into InspectionRecord.");
                return;
            }

            inspectionData.Id = Guid.NewGuid().ToString();
            inspectionData.SourceFileName = fileName;
            inspectionData.ProcessedAtUtc = DateTime.UtcNow;

            var cosmosResponse = await _cosmosContainer.CreateItemAsync(
                    item: inspectionData,
                    partitionKey: new PartitionKey(inspectionData.InspectorId)
            );

            _logger.LogInformation($"[SUCCESS] Saved Document ID: {inspectionData.Id} to Cosmos DB. Request Charge: {cosmosResponse.RequestCharge} RUs");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"[FAILED] Error processing file {fileName}");
            throw;
        }
    }
}