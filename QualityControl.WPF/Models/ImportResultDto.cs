using TypeGen.Core.TypeAnnotations;

namespace QualityControl.WPF.Models
{
    [ExportTsClass(OutputDir = "../quality-control-vue-dashboard/src/models")]
    public class ImportResultDto
    {
        public bool Success { get; set; }
        public int TotalRecordsProcessed { get; set; }
        public int RecordsImported { get; set; }
        public int RecordsSkipped { get; set; }
        [TsNull]
        public string? ErrorMessage { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan Duration => EndTime - StartTime;
        public string FullName { get; set; }
    }
}
