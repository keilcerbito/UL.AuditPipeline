namespace UL.AuditPipeline.Api.Services
{
    public interface IBlobStorageService
    {
        Task<string> UploadInspectionBlobAsync(string fileName, Stream fileStream);
        Task EnqueueInspectionMessageAsync(string queueName, string message);
    }
}
