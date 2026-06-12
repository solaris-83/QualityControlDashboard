using TypeGen.Core.TypeAnnotations;

namespace QualityControl.WPF.Models
{
    [ExportTsClass(OutputDir = "../quality-control-vue-dashboard/src/models")]
    public class FileResponseDto
    {
        public FileResponseDto()
        {
            
        }
        public FileResponseDto(int id, int week, int year, string name, DateTime startUploadedAt, DateTime? stopUploadedAt, int numberOfRecords, string errorMessage = null)
        {
            Id = id;
            Week = week;
            Year = year;
            Name = name;
            StartImportedAt = startUploadedAt;
            EndImportedAt = stopUploadedAt;
            NumberOfRecords = numberOfRecords;
            ErrorMessage = errorMessage;
        }

        public int Id { get; set; }
        public int Week { get; set; }
        public int Year { get; set; }
        public string Name { get; set; }
        public DateTime StartImportedAt { get; set; }
        [TsNull]
        public DateTime? EndImportedAt { get; set; }
        public int NumberOfRecords { get; set; }
        [TsNull]
        public string? ErrorMessage { get; set; }
    }

    [ExportTsClass(OutputDir = "../quality-control-vue-dashboard/src/models")]
    public class FileRequestDto
    {
        public int Week { get; set; }
        public int Year { get; set; }
        public List<string> Projects { get; set; }
    }
}
