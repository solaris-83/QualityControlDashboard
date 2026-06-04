using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QualityControl.WPF
{
    public class UserService : IUserService
    {
        public Task<UserDto> GetUserAsync(
            int id)
        {
            return Task.FromResult(
                new UserDto
                {
                    Id = id,
                    FirstName = "Marco",
                    LastName = "Rossi",
                    Email = "marco@company.com",
                    Age = 35
                });
        }

        public Task SaveUserAsync(
            UserDto user)
        {
            Console.WriteLine(
                $"Saved user {user.FirstName}");

            return Task.CompletedTask;
        }
    }
}
