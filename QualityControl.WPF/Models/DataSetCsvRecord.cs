using CsvHelper.Configuration.Attributes;
using CsvHelper.TypeConversion;

namespace QualityControl.WPF.Models
{
    public class DataSetCsvRecord
    {
        [Name("License Id")]
        public string License { get; set; } = string.Empty;

        [Name("License Domain")]
        public string LicenseDomain { get; set; } = string.Empty;

        [Name("VIN")]
        public string VIN { get; set; } = string.Empty;

        //[Name("Project")]
        //public string Project { get; set; } = string.Empty;

        [Name("ModelID")]
        public string Model { get; set; } = string.Empty;

        [Name("Report Type")]
        public string ReportType { get; set; } = string.Empty;

        [Name("Category")]
        public string Category { get; set; } = string.Empty;

        [Name("Operation Code")]
        public string OperationCode { get; set; } = string.Empty;

        [Name("Result")]
        public string ResultType { get; set; } = string.Empty;

        [Name("UTC date/time")]
        public DateTime UTC_DateTime { get; set; }

        [Name("Elapsed Time")]
        [Optional]
        public string? ElapsedTime { get; set; }

        [Name("App Version")]
        [Optional]
        public string? BCAVersion { get; set; }

        [Name("App Name")]
        [Optional]
        public string? AppName { get; set; }

        [Name("App Data Version")]
        public string WUVersion { get; set; }

        [Name("Error Code")]
        [Optional]
        public string? ErrorCode { get; set; }

        [Name("Error Description Ex")]
        [Optional]
        public string? AdditionalInfo { get; set; }

        [Name("Affected Controllers")]
        [Optional]
        public string? AffectedControllers { get; set; }
    }
}