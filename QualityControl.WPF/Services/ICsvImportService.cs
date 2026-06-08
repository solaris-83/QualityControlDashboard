using System;
using System.Threading;
using System.Threading.Tasks;

namespace QualityControl.WPF.Services
{
    public interface ICsvImportService
    {
        Task<ImportResult> ImportCsvAsync(string filePath, IProgress<ImportProgress>? progress = null, CancellationToken cancellationToken = default);
    }

    public class ImportResult
    {
        public bool Success { get; set; }
        public int TotalRecordsProcessed { get; set; }
        public int RecordsImported { get; set; }
        public int RecordsSkipped { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan Duration => EndTime - StartTime;
    }

    public class ImportProgress
    {
        public int CurrentRecord { get; set; }
        public int TotalRecords { get; set; }
        public int PercentComplete => TotalRecords > 0 ? (CurrentRecord * 100) / TotalRecords : 0;
        public string? CurrentStatus { get; set; }
    }
}