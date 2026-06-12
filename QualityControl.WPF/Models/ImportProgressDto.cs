using TypeGen.Core.TypeAnnotations;

namespace QualityControl.WPF.Models
{
    [ExportTsClass(OutputDir = "../quality-control-vue-dashboard/src/models")]
    public class ImportProgressDto
    {
        public required string FileName { get; set; }
        public int CurrentRecord { get; set; }
        public int TotalRecords { get; set; }
        public int PercentComplete => TotalRecords > 0 ? (CurrentRecord * 100) / TotalRecords : 0;
        [TsNull]
        public string? CurrentStatus { get; set; }
    }
}
