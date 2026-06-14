
using TypeGen.Core.TypeAnnotations;

namespace QualityControl.WPF.Models
{
    [ExportTsClass(OutputDir = "../quality-control-vue-dashboard/src/models")]
    public class ErrorDto(string message, string? details = null, string? correlationId = null)
    {
        public ErrorDto(string message) : this (message, null, null)
        {
            
        }
        public string Message { get; set; } = message;
        public string? Details { get; set; } = details;
        public string? CorrelationId { get; set; } = correlationId;
    }
}
