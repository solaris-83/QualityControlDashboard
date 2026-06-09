using Microsoft.EntityFrameworkCore;
using QualityControl.WPF.DB;
using QualityControl.WPF.Messenger;
using QualityControl.WPF.Models;
using QualityControl.WPF.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;

namespace QualityControl.WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    { 
        private readonly IWebViewMessenger _messenger;
        private readonly IUserService _userService;
        private readonly ICsvImportService _csvImportService;
        private readonly AppDbContext _context;

        public MainWindow(
            IUserService userService, 
            AppDbContext context,
            IWebViewMessenger messenger, 
            ICsvImportService csvImportService)
        {
            _context = context;
            _userService = userService;
            _messenger = messenger;
            _csvImportService = csvImportService;
            
            InitializeComponent();
            Loaded += OnLoaded;
            
            Console.WriteLine(_context.Model.ToDebugString());
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            await Browser.EnsureCoreWebView2Async();

            // Initialize the messenger with the WebView2 control
            _messenger.Initialize(Browser);

            // Register the WebMessageReceived event
            Browser.CoreWebView2.WebMessageReceived += async (_, args) =>
            {
                await _messenger.ReceiveMessageAsync(args.WebMessageAsJson);
            };

            // Register message handlers
            RegisterHandlers();

            // Navigate to the web application
            Browser.Source = new Uri(@"http://localhost:5173");
            
            // Alternative: load from dist folder
            // var distIndexPath = Path.GetFullPath(Path.Combine(
            //     AppContext.BaseDirectory, "..", "..", "..", "..", 
            //     "quality-control-vue-dashboard", "dist", "index.html"));
            // Browser.Source = new Uri(distIndexPath);
        }

        private void RegisterHandlers()
        {
            _messenger.RegisterHandler<UserRequest, UserDto>(
                "user.get", 
                async request =>
                {
                    return await _userService.GetUserAsync(request!.Id);
                });

            _messenger.RegisterHandler<UserDto, bool>(
                "user.save", 
                async user =>
                {
                    await _userService.SaveUserAsync(user!);
                    return true;
                });

            _messenger.RegisterHandler<object, List<FileDto>>(
                "files.get",
                async user =>
                {
                    return await _context.Files.Select(f => new FileDto
                    {
                        Week = f.Week,
                        Year = f.Year,
                        Name = f.Name,
                        StartUploadedAt = f.StartImportAt,
                        StopUploadedAt = f.EndImportAt
                    }).ToListAsync();
                });

            _messenger.RegisterHandler<FileRequestDto, List<ImportResult>>(
                "files.upload", 
                async request =>
                {
                    List<ImportResult> importResults = [];
                    using var ct = new CancellationTokenSource(TimeSpan.FromSeconds(1800));
                    //ct.CancelAfter(TimeSpan.FromSeconds(30));
                    ct.Token.ThrowIfCancellationRequested(); // TODO set catch exception

                    Progress<ImportProgress> progress = new Progress<ImportProgress>(i =>
                    {
                        _messenger.Publish(true, i);
                    });
                    ImportResult importResult = null;
                    DirectoryInfo[] dirs = null;
                    if (request.Week == -1)
                    {
                        DirectoryInfo directoryInfo = new DirectoryInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @$"QualityControl\{request.Year}"));
                        dirs = directoryInfo.GetDirectories();
                    }
                    else
                    {
                        DirectoryInfo directoryInfo = new DirectoryInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @$"QualityControl\{request.Year}\wk{request.Week}"));
                        dirs = new[] { directoryInfo };
                    }

                    foreach (var dir in dirs)
                    {
                        foreach (var file in dir.GetFiles().Where(f => request.Projects.Any(project => f.Name.Contains(project))))
                        {
                            if (!ct.IsCancellationRequested)
                            {
                                importResult = await _csvImportService.ImportCsvAsync(file.FullName, progress, ct.Token);
                                importResults.Add(importResult);
                            }
                        }
                    }
                    return importResults;
                });
        }
    }
}