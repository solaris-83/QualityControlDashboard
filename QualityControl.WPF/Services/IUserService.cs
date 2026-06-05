
using QualityControl.WPF.Models;

namespace QualityControl.WPF.Services
{
    public interface IUserService
    {
        Task<UserDto> GetUserAsync(int id);
        Task SaveUserAsync(UserDto user);
    }
}