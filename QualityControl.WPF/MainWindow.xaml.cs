using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

namespace QualityControl.WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    { 
        private IWebViewMessenger _messenger;
        private readonly IUserService _userService;
        public MainWindow(IUserService userService)
        {
            _userService = userService;
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            await Browser.EnsureCoreWebView2Async();

            _messenger = new WebViewMessenger(Browser);

            Browser.CoreWebView2.WebMessageReceived +=
                async (_, args) =>
                {
                    await _messenger
                        .ReceiveMessageAsync(
                            args.WebMessageAsJson);
                };

            RegisterHandlers();

            Browser.Source = new Uri(@"http://localhost:5173");
            var distIndexPath = Path.GetFullPath(Path.Combine(
                AppContext.BaseDirectory,
                "..", "..", "..", "..",   // from QualityControl/bin/Debug/net9.0-windows
                "quality-control-app", "dist", "index.html"));

           // Browser.Source = new Uri(distIndexPath);
        }

        private void RegisterHandlers()
        {
            _messenger!
                .RegisterHandler<
                    UserRequest,
                    UserDto>(
                "user.get",
                async request =>
                {
                    return await _userService
                        .GetUserAsync(
                            request!.Id);
                });

            _messenger!
                .RegisterHandler<
                    UserDto,
                    bool>(
                "user.save",
                async user =>
                {
                    await _userService
                        .SaveUserAsync(
                            user!);

                    return true;
                });
        }
    }
}