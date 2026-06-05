using QualityControl.WPF.Models;

namespace QualityControl.WPF.Services
{
    public class UserService : IUserService
    {
        public Task<UserDto> GetUserAsync(int id)
        {
            return Task.FromResult(new UserDto
                {
                    Id = id,
                    FirstName = "Marco",
                    LastName = "Rossi",
                    Email = "marco@company.com",
                    Age = 35
                });
        }

        public Task SaveUserAsync(UserDto user)
        {
            Console.WriteLine(
                $"Saved user {user.FirstName}");

            return Task.CompletedTask;
        }
    }
}
