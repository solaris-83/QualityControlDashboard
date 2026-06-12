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
using System.Xml.Linq;

namespace QualityControl.WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    { 
        private readonly IWebViewMessenger _messenger;
        private readonly ICsvImportService _csvImportService;
        private readonly AppDbContext _context;
        private readonly IDataSetService _dataSetService;

        public MainWindow(
            AppDbContext context,
            IWebViewMessenger messenger, 
            ICsvImportService csvImportService, 
            IDataSetService dataSetService)
        {
            _context = context;
            _messenger = messenger;
            _csvImportService = csvImportService;
            _dataSetService = dataSetService;

            InitializeComponent();
            Loaded += OnLoaded;
            
            Console.WriteLine(_context.Model.ToDebugString());
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            Environment.SetEnvironmentVariable(
    "WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS",
    "--remote-debugging-port=9222");
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

        // Fare classe statica fuori da MainWindow??
        private void RegisterHandlers()
        {
            //_messenger.RegisterHandler<UserRequest, UserDto>(
            //    "user.get", 
            //    async request =>
            //    {
            //        return await _userService.GetUserAsync(request!.Id);
            //    });

            //_messenger.RegisterHandler<UserDto, bool>(
            //    "user.save", 
            //    async user =>
            //    {
            //        await _userService.SaveUserAsync(user!);
            //        return true;
            //    });

            _messenger.RegisterHandler<DataSetRequestDto, StreamChunkDto<DataSetResponseDto>>(Constants.Dataset_Get, async request =>
            {
                var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                int chunkSize = 100; // Define the size of each chunk
                int chunkNumber = 0;
                var chunk = new List<DataSetResponseDto>();
                
                await foreach (var item in _dataSetService.GetAsync(request, cancellationTokenSource.Token))
                {
                    chunk.Add(item);
                    
                    if (chunk.Count >= chunkSize)
                    {
                        var intermediateChunk = new StreamChunkDto<DataSetResponseDto>(Constants.Dataset_Get, chunkNumber, chunk, isLastChunk: false);
                        _messenger.Publish(TypeEnum.Stream, intermediateChunk, Constants.Dataset_Get, correlationId: "");
                        chunkNumber++;
                        chunk.Clear();
                    }
                }
                
                // Send the final chunk with any remaining items
                var finalChunk = new StreamChunkDto<DataSetResponseDto>(Constants.Dataset_Get, chunkNumber, chunk, isLastChunk: true);
                return finalChunk;
            });

            _messenger.RegisterHandler<FileRequestDto, List<FileResponseDto>>(Constants.Files_Get,
                async request =>
                {
                    if (request == null)
                    {
                        return [];
                    }

                    var query = _context.Files.AsQueryable();

                    // Filter by year (always applied)
                    query = query.Where(f => f.Year == request.Year);

                    // Filter by week if specified
                    if (request.Week != 0)
                    {
                        query = query.Where(f => f.Week == request.Week);
                    }

                    // Filter by projects if specified
                    if (request.Projects != null && request.Projects.Count > 0)
                    {
                        query = query.Where(f => request.Projects.Contains(f.Project.Name));
                    }

                    return await query.Select(f => new FileResponseDto
                    {
                        Id = f.Id,
                        Week = f.Week,
                        Year = f.Year,
                        Name = f.Name,
                        StartImportedAt = f.StartImportAt,
                        EndImportedAt = f.EndImportAt, 
                        NumberOfRecords = f.NumberOfRecords,
                        ErrorMessage = f.ErrorMessage
                    }).ToListAsync();
            });

            _messenger.RegisterHandler<FileRequestDto, List<ImportResultDto>>(
                Constants.Files_Upload, 
                async request => // TODO Mettere nel handler un pattern che se va in errore la lambda expression allora notifica a TS
                {
                    List<ImportResultDto> importResults = [];
                    using var ct = new CancellationTokenSource(TimeSpan.FromSeconds(1800));
                    //ct.CancelAfter(TimeSpan.FromSeconds(30));
                    ct.Token.ThrowIfCancellationRequested(); // TODO set catch exception

                    Progress<ImportProgressDto> progress = new(i =>
                    {
                        _messenger.Publish(TypeEnum.Event, i, Constants.Files_Upload_Progress);
                    });

                    ImportResultDto importResult = default;
                    DirectoryInfo[] dirs = default;

                    if (request.Week == 0)
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
                        if (!Directory.Exists(dir.FullName))
                            continue; // TODO magari notificare

                        foreach (var file in dir.GetFiles().Where(f => request.Projects.Any(project => f.Name.Contains(project))))
                        {
                            if (!ct.IsCancellationRequested)
                            {
                                importResult = await _csvImportService.ImportCsvAsync(file.FullName, progress, ct.Token);
                                importResults.Add(importResult);
                            }
                        }
                    }
                    return importResults; // TODO non viene mappato errore c# verso il TS
                });
        }
    }
}