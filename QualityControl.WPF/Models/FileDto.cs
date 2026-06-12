using TypeGen.Core.TypeAnnotations;

namespace QualityControl.WPF.Models
{
    [ExportTsClass(OutputDir = "../quality-control-vue-dashboard/src/models")] // TODO mettere int per week and year e Type unions?
    public class FileResponseDto
    {
        public FileResponseDto()
        {
            
        }

        // TODO aggiungere numero record importati per file
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
