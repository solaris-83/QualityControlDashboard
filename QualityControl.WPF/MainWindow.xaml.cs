using Microsoft.EntityFrameworkCore;
using Microsoft.Web.WebView2.Core;
using QualityControl.WPF.DB;
using QualityControl.WPF.Exceptions;
using QualityControl.WPF.Messenger;
using QualityControl.WPF.Models;
using QualityControl.WPF.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
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
            Environment.SetEnvironmentVariable("WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS", "--remote-debugging-port=9222");
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
#if DEBUG
             Browser.Source = new Uri(@"http://localhost:5173");
#elif RELEASE
            // Alternative: load from dist folder
            var distIndexPath = Path.GetFullPath(Path.Combine(
                AppContext.BaseDirectory, "..", "..", "..", "..",
                "quality-control-vue-dashboard", "dist", "index.html"));

            Browser.CoreWebView2.SetVirtualHostNameToFolderMapping(
               "app.local",
               Path.GetDirectoryName(distIndexPath),
               CoreWebView2HostResourceAccessKind.Allow);
            Browser.CoreWebView2.Navigate("https://app.local/index.html");
#endif
        }

        private void RegisterHandlers()
        {
            _messenger.RegisterHandler<DataSetRequestDto, StreamChunkDto<DataSetResponseDto>>(Constants.Dataset_Get, async request =>
            {
                var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                int chunkSize = 100; // Define the size of each chunk
                int chunkNumber = 0;
                var chunk = new List<DataSetResponseDto>();

                try
                {
                    await foreach (var item in _dataSetService.GetAsync(request, cancellationTokenSource.Token))
                    {
                        chunk.Add(item);

                        if (chunk.Count >= chunkSize)
                        {
                            var intermediateChunk = new StreamChunkDto<DataSetResponseDto>(Constants.Dataset_Get, chunkNumber, chunk, isLastChunk: false);
                            _messenger.Publish(TypeEnum.Stream, intermediateChunk, isError: false, name: Constants.Dataset_Get, correlationId: "");
                            chunkNumber++;
                            chunk.Clear();
                        }
                    }

                    // Send the final chunk with any remaining items
                    var finalChunk = new StreamChunkDto<DataSetResponseDto>(Constants.Dataset_Get, chunkNumber, chunk, isLastChunk: true);
                    return finalChunk;
                }
                catch (Exception ex)
                {
                    throw new DatabaseApplicationException(ex.Message);
                }
            });

            _messenger.RegisterHandler<List<int>, bool>(Constants.Files_Delete,
                async request =>
                {
                    await using var transaction = await _context.Database.BeginTransactionAsync();
                    try
                    {
                        await _context.DataSets.Where(ds => request.Contains(ds.File_Id)).ExecuteDeleteAsync();
                        await _context.Files.Where(f => request.Contains(f.Id)).ExecuteDeleteAsync();

                        await transaction.CommitAsync();
                        return true;
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        throw new DatabaseApplicationException(ex.Message);
                    }
                }
            );

            _messenger.RegisterHandler<FileRequestDto, List<FileResponseDto>>(Constants.Files_Get, // TODO gestire l'oggetto di ritorno anche lato TS?
                async request =>
                {
                    if (request == null)
                    {
                        return [];
                    }

                    try
                    {
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
                    }
                    catch (Exception ex)
                    {
                        throw new DatabaseApplicationException(ex.Message);
                    }
                });

            _messenger.RegisterHandler<FileRequestDto, List<ImportResultDto>>(
                Constants.Files_Upload, 
                async request =>
                {
                    List<ImportResultDto> importResults = [];
                    try
                    {
                        using var ct = new CancellationTokenSource(TimeSpan.FromSeconds(Constants.ImportFileMaxTimeoutSeconds)); // TODO gestire bene la cancellazione lato TS, altrimenti se il processo dura più di 30 secondi allora TS non riceve nulla e va in timeout
                        ct.Token.ThrowIfCancellationRequested();

                        Progress<ImportProgressDto> progress = new(i =>
                        {
                            _messenger.Publish(TypeEnum.Event, i, isError: false, name: Constants.Files_Upload_Progress);
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
                                continue;

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
                    }
                    catch (DatabaseApplicationException)
                    {
                        throw; // Rethrow database exceptions without wrapping
                    }
                    catch (OperationCanceledException)
                    {
                        throw new OperationCanceledApplicationException("The operation was canceled or timed out.");
                    }
                    catch (Exception ex)
                    {
                        throw new ApplicationException($"Unexpected error during file import: {ex.Message}");
                    }
                });

            _messenger.RegisterHandler<List<ProjectRequestDto>, List<ProjectResponseDto>>(Constants.Projects_Get,
                async request =>
                {
                    if (request == null)
                    {
                        return [];
                    }

                    try
                    {
                        var query = _context.Projects.AsQueryable();
                        if (request.Any(f => !string.IsNullOrEmpty(f.Name)))
                        {
                            foreach (var project in request)
                            {
                                if (!string.IsNullOrEmpty(project.Name))
                                {
                                    query = query.Where(p => p.Name.Contains(project.Name));
                                }
                            }
                        }
                        return await query.DistinctBy(p => p.Name).Select(p => new ProjectResponseDto
                        {
                            Name = p.Name
                        }).ToListAsync();
                    }
                    catch (Exception ex)
                    {
                        throw new DatabaseApplicationException(ex.Message);
                    }
                });
        }
    }
}