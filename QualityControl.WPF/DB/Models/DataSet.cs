using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QualityControl.WPF.DB.Models
{
    public class DataSet
    {
        public int Id { get; set; }

        // Foreign Keys
        public int License_Id { get; set; }
        public int File_Id { get; set; }
        public int VIN_Id { get; set; }
        public int Project_Id { get; set; }
        public int Model_Id { get; set; }
        public int ReportType_Id { get; set; }
        public int Category_Id { get; set; }
        public int OperationCode_Id { get; set; }
        public int ResultType_Id { get; set; }

        // Navigation properties
        [ForeignKey(nameof(License_Id))]
        public License License { get; set; } = null!;

        [ForeignKey(nameof(File_Id))]
        public File File { get; set; } = null!;

        [ForeignKey(nameof(VIN_Id))]
        public VIN VIN { get; set; } = null!;

        [ForeignKey(nameof(Model_Id))]
        public Model Model { get; set; } = null!;

        [ForeignKey(nameof(Project_Id))]
        public Project Project { get; set; } = null!;

        [ForeignKey(nameof(ReportType_Id))]
        public ReportType ReportType { get; set; } = null!;

        [ForeignKey(nameof(Category_Id))]
        public Category Category { get; set; } = null!;

        [ForeignKey(nameof(OperationCode_Id))]
        public OperationCode OperationCode { get; set; } = null!;

        [ForeignKey(nameof(ResultType_Id))]
        public ResultType ResultType { get; set; } = null!;

        // Data properties
        public DateTime UTC_DateTime { get; set; }

        //[MaxLength(4)]
        public int? ElapsedTime { get; set; }

       // [MaxLength(100)]
        public int? BCAVersion { get; set; }

        [MaxLength(100)]
        public string? AppName { get; set; }

       // [MaxLength(100)]
        public int WUVersion { get; set; }

       // [MaxLength(50)]
        public int? ErrorCode { get; set; }

        [MaxLength(500)]
        public string? AdditionalInfo { get; set; }

        [MaxLength(500)]
        public string? AffectedControllers { get; set; }
    }
}