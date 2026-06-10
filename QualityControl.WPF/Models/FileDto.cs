using TypeGen.Core.TypeAnnotations;

namespace QualityControl.WPF.Models
{
    [ExportTsClass(OutputDir = "../quality-control-vue-dashboard/src/models")]
    public class FileResponseDto
    {
        public FileResponseDto()
        {
            
        }
        public FileResponseDto(int id, int week, int year, string name, DateTime startUploadedAt, DateTime? stopUploadedAt)
        {
            Id = id;
            Week = week;
            Year = year;
            Name = name;
            StartImportedAt = startUploadedAt;
            EndImportedAt = stopUploadedAt;
        }

        public int Id { get; set; }
        public int Week { get; set; }
        public int Year { get; set; }
        public string Name { get; set; }
        public DateTime StartImportedAt { get; set; }
        public DateTime? EndImportedAt { get; set; }
    }
}
