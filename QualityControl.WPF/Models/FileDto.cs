using TypeGen.Core.TypeAnnotations;

namespace QualityControl.WPF.Models
{
    [ExportTsClass(OutputDir = "../quality-control-vue-dashboard/src/models")]
    public class FileDto
    {
        public int Week { get; set; }
        public int Year { get; set; }
        public string Name { get; set; }
        public DateTime? StartUploadedAt { get; set; }
        public DateTime? StopUploadedAt { get; set; }
    }
}
