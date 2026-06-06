using System.ComponentModel.DataAnnotations;

namespace QualityControl.WPF.DB.Models
{
    public class Project
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        // Navigation property
        public ICollection<File> Files { get; set; } = new List<File>();
        public ICollection<DataSet> DataSets { get; set; } = new List<DataSet>();
    }
}
