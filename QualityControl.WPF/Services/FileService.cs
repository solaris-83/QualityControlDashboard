using Microsoft.EntityFrameworkCore;
using QualityControl.WPF.DB;
using QualityControl.WPF.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QualityControl.WPF.Services
{
    //public class FileService(AppDbContext context) : IFileService
    //{
    //    private readonly AppDbContext _context = context;
       
    //    public async IAsyncEnumerable<FileResponseDto> GetAsync(CancellationToken cancellationToken)
    //    {
    //        await foreach (var row in _context.Files
    //            .AsAsyncEnumerable()
    //            .WithCancellation(cancellationToken))
    //        {
    //            yield return new FileResponseDto(row.Id, row.Week, row.Year, row.Name, (DateTime)row.StartImportAt, row.EndImportAt);
    //        }
    //    }
    //}
}
