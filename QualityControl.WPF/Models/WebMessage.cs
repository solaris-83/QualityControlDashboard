
using System.Text.Json.Serialization;
using TypeGen.Core.TypeAnnotations;

namespace QualityControl.WPF.Models
{
    [ExportTsClass(OutputDir = "../quality-control-vue-dashboard/src/models")]
    public class WebMessage
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }
            = Guid.NewGuid().ToString();

        [JsonPropertyName("type")]
        public TypeEnum Type { get; set; }

        [JsonPropertyName("payload")]
        public object? Payload { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("correlationId")]
        public string? CorrelationId { get; set; }
    }

    public enum TypeEnum : byte
    {
        Request = 0,
        Response = 1,
        Stream = 2,
        Event = 3
    }
}
