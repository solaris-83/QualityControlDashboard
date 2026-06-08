
using System.Text.Json.Serialization;

namespace QualityControl.WPF.Models
{
    public class WebMessage
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }
            = Guid.NewGuid().ToString();

        [JsonPropertyName("type")]
        public string Type { get; set; } = "";

        [JsonPropertyName("payload")]
        public object? Payload { get; set; }

        [JsonPropertyName("isResponse")]
        public bool IsResponse { get; set; }

        [JsonPropertyName("correlationId")]
        public string? CorrelationId { get; set; }
    }
}
