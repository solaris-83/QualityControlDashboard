using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using QualityControl.WPF.DB;
using QualityControl.WPF.Messenger;
using QualityControl.WPF.Services;
using System.IO;
using System.Windows;

namespace QualityControl.WPF
{
    public partial class App : Application
    {
        private ServiceProvider? _serviceProvider;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);
            _serviceProvider = serviceCollection.BuildServiceProvider();

            // Ensure database is created and migrations are applied
            EnsureDatabaseCreated();

            var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }

        // Marked as static since it doesn't rely on instance state and is only called once during startup
        private static void ConfigureServices(IServiceCollection services)
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var dbPath = Path.Combine(appDataPath, "QualityControl", "qualitycontrol.db");

            // Ensure directory exists
            Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
            // Database
            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlite($"Data Source={dbPath}"));

            // Services
            services.AddTransient<IDataSetService, DataSetService>();
            services.AddTransient<ICsvImportService, CsvImportService>();
            
            // Messenger - Scoped so each MainWindow instance gets its own messenger
            services.AddScoped<IWebViewMessenger, WebViewMessenger>();

            // Windows
            services.AddTransient<MainWindow>();
        }

        private void EnsureDatabaseCreated()
        {
            try
            {
                using var scope = _serviceProvider!.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                // Apply any pending migrations (creates DB if it doesn't exist)
                context.Database.Migrate();

                // Alternative: Just create DB without migrations
                // context.Database.EnsureCreated();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to initialize database: {ex.Message}",
                    "Database Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                Shutdown();
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _serviceProvider?.Dispose();
            base.OnExit(e);
        }
    }
}
