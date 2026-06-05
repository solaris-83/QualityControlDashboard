using QualityControl.WPF.Messenger;
using QualityControl.WPF.Models;
using QualityControl.WPF.Services;
using System.Windows;

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
                    await _messenger.ReceiveMessageAsync(args.WebMessageAsJson);
                };

            RegisterHandlers();

            Browser.Source = new Uri(@"http://localhost:5173");
           // var distIndexPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"..", "..", "..", "..", "quality-control-vue-dashboard", "dist", "index.html"));

           // Browser.Source = new Uri(distIndexPath);
        }

        private void RegisterHandlers()
        {
            _messenger!
                .RegisterHandler<UserRequest, UserDto>("user.get", async request =>
                            {
                                return await _userService.GetUserAsync(request!.Id);
                            });

            _messenger!
                .RegisterHandler<UserDto, bool>("user.save",async user =>
                    {
                        await _userService.SaveUserAsync(user!);

                        return true;
                    });
        }
    }
}