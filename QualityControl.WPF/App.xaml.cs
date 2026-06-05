using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using QualityControl.WPF.Messenger;
using QualityControl.WPF.Services;
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
                    .AddSingleton<IUserService, UserService>()
                    .AddSingleton<MainWindow>()
                    .BuildServiceProvider());

            Ioc.Default.GetRequiredService<MainWindow>().Show();
        }
    }

}
