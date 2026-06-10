
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QualityControl.WPF.DB.Models
{
    public class File
    {
        public int Id { get; set; }

        [Required]
        public int Week { get; set; }

        [Required]
        public int Year { get; set; }

        [Required]
        [MaxLength(64)]
        public string Hash { get; set; }

        [Required]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;

        public DateTime StartImportAt { get; set; }

        public DateTime? EndImportAt { get; set; }

        // Foreign Key to Project
        [Required]
        public int Project_Id { get; set; }

        // Navigation properties
        [ForeignKey(nameof(Project_Id))]
        public Project Project { get; set; } = null!;

        public ICollection<DataSet> DataSets { get; set; } = new List<DataSet>();
    }
}