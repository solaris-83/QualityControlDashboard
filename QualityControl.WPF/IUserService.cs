
namespace QualityControl.WPF
{
    public interface IUserService
    {
        Task<UserDto> GetUserAsync(int id);
        Task SaveUserAsync(UserDto user);
    }
}