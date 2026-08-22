using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace UL.AuditPipeline.Core.Models
{
    public class InspectionRecord
    {
        [JsonProperty("id")]
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonProperty("inspectorId")]
        [JsonPropertyName("inspectorId")]
        public string InspectorId { get; set; } = string.Empty;

        [JsonProperty("location")]
        [JsonPropertyName("location")]
        public string Location { get; set; } = string.Empty;

        [JsonProperty("passFailStatus")]
        [JsonPropertyName("passFailStatus")]
        public string PassFailStatus { get; set; } = string.Empty;

        [JsonProperty("comments")]
        [JsonPropertyName("comments")]
        public string Comments { get; set; } = string.Empty;

        [JsonProperty("sourceFileName")]
        [JsonPropertyName("sourceFileName")]
        public string SourceFileName { get; set; } = string.Empty;

        [JsonProperty("processedAtUtc")]
        [JsonPropertyName("processedAtUtc")]
        public DateTime ProcessedAtUtc { get; set; }
    }
}
