using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace QualityControl.WPF
{
    public partial class UserViewModel : ObservableObject
    {
        [ObservableProperty]
        private int _id;

        [ObservableProperty]
        private string _firstName = "";

        [ObservableProperty]
        private string _lastName = "";

        [ObservableProperty]
        private string _email = "";

        [ObservableProperty]
        private int _age;

        

        public void Load(UserDto dto)
        {
            Id = dto.Id;
            FirstName = dto.FirstName;
            LastName = dto.LastName;
            Email = dto.Email;
            Age = dto.Age;
        }
    }
}
