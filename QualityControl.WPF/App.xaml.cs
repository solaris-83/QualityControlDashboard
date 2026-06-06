using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using QualityControl.WPF.DB;
using QualityControl.WPF.Messenger;
using QualityControl.WPF.Services;
using System.IO;
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
            var folder = Environment.SpecialFolder.LocalApplicationData;
            var path = Path.Combine(Environment.GetFolderPath(folder), "QualityControl");
            Directory.CreateDirectory(path);
            var connectionString = Path.Join(path, "qualitycontrol.db");

            Ioc.Default.ConfigureServices(
                new ServiceCollection()
                    .AddSingleton<IUserService, UserService>()
                    //.AddSingleton<IWebViewMessenger>(p => p.GetService<MainWindow>()!.Browser != null ? new WebViewMessenger(p.GetService<MainWindow>()!.Browser) : throw new InvalidOperationException("MainWindow or Browser is not available"))
                    .AddDbContext<AppDbContext>(options =>
                        options.UseSqlite(connectionString))
                    .AddSingleton<MainWindow>()
                    .BuildServiceProvider());

            Ioc.Default.GetRequiredService<MainWindow>().Show();
        }
    }

}
