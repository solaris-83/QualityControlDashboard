using TypeGen.Core.TypeAnnotations;

namespace QualityControl.WPF.Models
{
    [ExportTsClass(OutputDir = "../quality-control-vue-dashboard/src/models")]
    public class DataSetRequestDto
    {
        public int Week { get; set; }
        public int Year { get; set; }
    }

    [ExportTsClass(OutputDir = "../quality-control-vue-dashboard/src/models")]
    public class DataSetResponseDto
    {
        public int Id { get; set; }
        public int Week { get; set; }
        public int Year { get; set; }
        public string License { get; set; } = "";
        public string VIN { get; set; } = "";
        public string Model { get; set; } = "";
        public string AppName { get; set; } = "";
        public string ResultType { get; set; } = "";
        public string ErrorCode { get; set; } = "";
    }
}
