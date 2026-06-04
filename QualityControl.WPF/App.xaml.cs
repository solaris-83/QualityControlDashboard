using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

namespace QualityControl.WPF
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
           
            Ioc.Default.ConfigureServices(
                new ServiceCollection()
               //.AddSingleton<UserViewModel>()
               .AddSingleton<IWebViewMessenger, WebViewMessenger>()
               .AddSingleton<IUserService, UserService>()
               .BuildServiceProvider());

            var userService = Ioc.Default.GetService<IUserService>();
            var mainWindow = new MainWindow(userService);
            mainWindow.Show();
        }
    }

}
