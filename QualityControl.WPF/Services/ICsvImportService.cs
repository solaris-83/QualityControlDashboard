using QualityControl.WPF.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace QualityControl.WPF.Services
{
    public interface ICsvImportService
    {
        Task<ImportResult> ImportCsvAsync(string filePath, IProgress<ImportProgress>? progress = null, CancellationToken cancellationToken = default);
    }
}