
using System.Text.Json.Serialization;
using TypeGen.Core.TypeAnnotations;

namespace QualityControl.WPF.Models
{
    [ExportTsClass(OutputDir = "../quality-control-vue-dashboard/src/models")]
    public class WebMessageDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [JsonPropertyName("type")]
        public TypeEnum Type { get; set; }

        [JsonPropertyName("payload")]
        public object? Payload { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("correlationId")]
        [TsNull]
        public string? CorrelationId { get; set; }
    }

    [ExportTsEnum(OutputDir = "../quality-control-vue-dashboard/src/models")]
    public enum TypeEnum : byte
    {
        Request = 0,
        Response = 1,
        Stream = 2,
        Event = 3
    }

    [ExportTsClass(OutputDir = "../quality-control-vue-dashboard/src/models")]

    public class Constants
    {
        [TsStatic]
        public static string Dataset_Get = "datasets.get";
        [TsStatic]
        public static string Files_Get = "files.get";
        [TsStatic]
        public static string Files_Delete = "files.delete";
        [TsStatic]
        public static string Files_Upload = "files.upload";
        [TsStatic]
        public static string Files_Upload_Progress = "files.upload.progress";
        [TsStatic]
        public static int ImportFileMaxTimeoutSeconds = 180; // 3 minutes
    }
}
