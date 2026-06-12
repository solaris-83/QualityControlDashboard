using QualityControl.WPF.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace QualityControl.WPF.Services
{
    public interface ICsvImportService
    {
        Task<ImportResultDto> ImportCsvAsync(string filePath, IProgress<ImportProgressDto>? progress = null, CancellationToken cancellationToken = default);
    }
}