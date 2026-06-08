
using TypeGen.Core.TypeAnnotations;

namespace QualityControl.WPF.Models
{
    [ExportTsClass(OutputDir = "../quality-control-vue-dashboard/src/models")]
    public class FileRequestDto
    {
        public int Week { get; set; }
        public int Year { get; set; }
        public List<string> Projects { get; set; }
    }
}
