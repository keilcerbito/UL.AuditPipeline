using Azure.Storage.Blobs;
using Azure.Storage.Queues;

namespace UL.AuditPipeline.Api.Services
{
    public class BlobStorageService : IBlobStorageService
    {
        private readonly string _connectionString;

        public BlobStorageService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("AzureStorage")
                ?? throw new InvalidOperationException("AzureStorage connection string is missing.");
        }

        public async Task<string> UploadInspectionBlobAsync(string fileName, Stream fileStream)
        {
            var blobServiceClient = new BlobServiceClient(_connectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient("inspections");

            await containerClient.CreateIfNotExistsAsync();

            var blobClient = containerClient.GetBlobClient(fileName);
            await blobClient.UploadAsync(fileStream, overwrite: true);

            return blobClient.Uri.ToString();
        }

        public async Task EnqueueInspectionMessageAsync(string queueName, string message)
        {
            var queueClient = new QueueClient(_connectionString, queueName);

            await queueClient.CreateIfNotExistsAsync();

            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(message);
            var base64Message = Convert.ToBase64String(plainTextBytes);

            await queueClient.SendMessageAsync(base64Message);
        }
    }
}
