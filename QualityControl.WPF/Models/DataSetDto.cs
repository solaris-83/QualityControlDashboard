using TypeGen.Core.TypeAnnotations;

namespace QualityControl.WPF.Models
{
    [ExportTsClass(OutputDir = "../quality-control-vue-dashboard/src/models")]
    public class DataSetRequestDto
    {
        public int Week { get; set; }
        public int Year { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = int.MaxValue;
    }

    [ExportTsClass(OutputDir = "../quality-control-vue-dashboard/src/models")]
    public class DataSetResponseDto
    {
        public int Id { get; set; }
        public int Week { get; set; }
        public int Year { get; set; }
        public string License { get; set; } = "";
        public string Vin { get; set; } = "";
        public string Model { get; set; } = "";
        public string AppName { get; set; } = "";
        public string ResultType { get; set; } = "";
        [TsNull]
        public int? ErrorCode { get; set; }
        [TsNull]
        public int? ElapsedTime { get; set; }
        public string AffectedControllers { get; set; } = "";
    }
}
