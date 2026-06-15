
using TypeGen.Core.TypeAnnotations;

namespace QualityControl.WPF.Models
{

    [ExportTsClass(OutputDir = "../quality-control-vue-dashboard/src/models")]
    public class ProjectRequestDto
    {
        public string Name { get; set; }
    }

    [ExportTsClass(OutputDir = "../quality-control-vue-dashboard/src/models")]
    public class ProjectResponseDto
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;
    }
}
