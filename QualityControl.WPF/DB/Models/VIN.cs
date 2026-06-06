using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace QualityControl.WPF.DB.Models
{
    public class VIN
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Code { get; set; } = string.Empty;

        // Navigation property
        public ICollection<DataSet> DataSets { get; set; } = new List<DataSet>();
    }
}