using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;
using CsvHelper.TypeConversion;
using System.Globalization;

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
        public int? ElapsedTime { get; set; }

        [Name("App Version")]
        public int? BCAVersion { get; set; }

        [Name("App Name")]
        public string? AppName { get; set; }

        [Name("App Data Version")]
        public int WUVersion { get; set; }

        [Name("Error Code")]
        public int? ErrorCode { get; set; }

        [Name("Error Description Ex")]
        public string? AdditionalInfo { get; set; }

        [Name("Affected Controllers")]
        public string? AffectedControllers { get; set; }
    }


    public sealed class DataSetMap : ClassMap<DataSetCsvRecord>
    {
        public DataSetMap()
        {
            AutoMap(CultureInfo.InvariantCulture);
            
            // Configure nullable integer fields to treat empty strings as null
            Map(m => m.ErrorCode).TypeConverterOption.NullValues(string.Empty, " ", "  ");
            Map(m => m.WUVersion).TypeConverterOption.NullValues(string.Empty, " ", "  ");
            Map(m => m.ElapsedTime).TypeConverterOption.NullValues(string.Empty, " ", "  ");
            Map(m => m.BCAVersion).TypeConverterOption.NullValues(string.Empty, " ", "  ");
            
            // Configure nullable string fields
            Map(m => m.AppName).TypeConverterOption.NullValues(string.Empty, " ", "  ");
            Map(m => m.AdditionalInfo).TypeConverterOption.NullValues(string.Empty, " ", "  ");
            Map(m => m.AffectedControllers).TypeConverterOption.NullValues(string.Empty, " ", "  ");
        }
    }
}