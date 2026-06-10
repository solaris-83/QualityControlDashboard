using Microsoft.EntityFrameworkCore;
using QualityControl.WPF.DB;
using QualityControl.WPF.Models;

namespace QualityControl.WPF.Services
{
    public class DataSetService(AppDbContext context) : IDataSetService
    {
        private readonly AppDbContext _context = context;

        public async IAsyncEnumerable<DataSetResponseDto> GetAsync(DataSetRequestDto requestDto, CancellationToken cancellationToken)
        {
            await foreach (var row in _context.DataSets
                .Where(d => d.File.Week == requestDto.Week && d.File.Year == requestDto.Year)
                .Include(d => d.License)
                .Include(d => d.VIN)
                .Include(d => d.Model)
                .Include(d => d.ResultType)
                .Include(d => d.File)
                .Take(111)
                .AsAsyncEnumerable()
                .WithCancellation(cancellationToken))
            {
                yield return new DataSetResponseDto
                {
                    Id = row.Id,
                    Week = row.File.Week,
                    Year = row.File.Year,
                    License = row.License.Name,
                    VIN = row.VIN.Code,
                    Model = row.Model.Name,
                    AppName = row.AppName!,
                    ResultType = row.ResultType.Name,
                    ErrorCode = row.ErrorCode
                };
            }
        }
    }
}
