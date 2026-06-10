using QualityControl.WPF.Models;

namespace QualityControl.WPF.Services
{
    public interface IDataSetService
    {
        IAsyncEnumerable<DataSetResponseDto> GetAsync(DataSetRequestDto requestDto, CancellationToken cancellationToken);
    }
}