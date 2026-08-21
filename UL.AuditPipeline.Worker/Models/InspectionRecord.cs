using Newtonsoft.Json;

namespace UL.AuditPipeline.Worker.Models
{
    public class InspectionRecord
    {
        [JsonProperty("id")]
        public string Id { get; set; } = string.Empty;


        [JsonProperty("inspectorId")]
        public required string InspectorId { get; set; }


        [JsonProperty("location")]
        public required string Location { get; set; }


        [JsonProperty("passFailStatus")]
        public required string PassFailStatus { get; set; }


        [JsonProperty("comments")]
        public string? Comments { get; set; }


        [JsonProperty("sourceFileName")]
        public string SourceFileName { get; set; } = string.Empty;


        [JsonProperty("processedAtUtc")]
        public DateTime ProcessedAtUtc { get; set; }
    }
}
