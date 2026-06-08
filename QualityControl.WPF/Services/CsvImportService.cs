using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.EntityFrameworkCore;
using QualityControl.WPF.DB;
using QualityControl.WPF.DB.Models;
using QualityControl.WPF.Exceptions;
using QualityControl.WPF.Models;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using File = QualityControl.WPF.DB.Models.File;

namespace QualityControl.WPF.Services
{
    public class CsvImportService(AppDbContext context) : ICsvImportService
    {
        private readonly AppDbContext _context = context;
        private const int BatchSize = 5000;

        public async Task<ImportResult> ImportCsvAsync(string filePath, IProgress<ImportProgress>? progress = null, CancellationToken cancellationToken = default)
        {
            var result = new ImportResult
            {
                StartTime = DateTime.Now
            };

            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                if (!System.IO.File.Exists(filePath))
                {
                    result.Success = false;
                    result.ErrorMessage = $"File not found: {filePath}";
                    result.EndTime = DateTime.Now;
                    return result;
                }

                result.FullName = filePath;

                progress?.Report(new ImportProgress
                {
                    CurrentRecord = 0,
                    TotalRecords = 0,
                    CurrentStatus = "Reading CSV file..."
                });

                // Create file record
                var fileRecord = await CreateFileRecordAsync(filePath, cancellationToken);

                // Use streaming enumerable for records
                var allRecords = ReadAllCsvRecordsAsync(filePath, cancellationToken);

                progress?.Report(new ImportProgress
                {
                    CurrentRecord = 0,
                    TotalRecords = 0,
                    CurrentStatus = "Preparing lookup tables..."
                });

                // Bulk insert/update all lookup tables FIRST (streaming)
                await BulkUpsertLookupTablesAsync(allRecords, cancellationToken);

                var projects = await _context.Projects.AsNoTracking().ToDictionaryAsync(x => x.Name, x => x.Id, cancellationToken);
                var projectName = fileRecord.Name.Substring(10, fileRecord.Name.Length - 14);
                if (projects.TryGetValue(projectName, out int value))
                {
                    fileRecord.Project_Id = value;
                }
                else
                {
                    fileRecord.Project = new Project { Name = projectName };
                }

                _context.Files.Add(fileRecord);
                await _context.SaveChangesAsync(cancellationToken);

                progress?.Report(new ImportProgress
                {
                    CurrentRecord = 0,
                    TotalRecords = 0,
                    CurrentStatus = "Loading lookup cache..."
                });

                // Load lookup cache after bulk insert
                var lookupCache = await LoadLookupCacheAsync(cancellationToken);

                progress?.Report(new ImportProgress
                {
                    CurrentRecord = 0,
                    TotalRecords = 0,
                    CurrentStatus = "Processing and inserting records..."
                });

                // Process records in streaming fashion
                var batch = new List<DataSet>(BatchSize);
                int recordCount = 0;
                int importedCount = 0;

                await foreach (var record in ReadAllCsvRecordsAsync(filePath, cancellationToken))
                {
                    recordCount++;

                    var dataSet = MapToDataSet(record, fileRecord, lookupCache);
                    batch.Add(dataSet);

                    if (batch.Count >= BatchSize)
                    {
                        _context.DataSets.AddRange(batch);
                        await _context.SaveChangesAsync(cancellationToken);
                        importedCount += batch.Count;
                        batch.Clear();

                        progress?.Report(new ImportProgress
                        {
                            CurrentRecord = importedCount,
                            TotalRecords = 0,
                            CurrentStatus = $"Inserted {importedCount} records..."
                        });
                    }
                }

                // Save remaining batch
                if (batch.Count > 0)
                {
                    _context.DataSets.AddRange(batch);
                    await _context.SaveChangesAsync(cancellationToken);
                    importedCount += batch.Count;
                }

                // Update file record with completion time
                fileRecord.EndImportAt = DateTime.Now;
                await _context.SaveChangesAsync(cancellationToken);

                // Commit transaction
                await transaction.CommitAsync(cancellationToken);

                result.Success = true;
                result.TotalRecordsProcessed = recordCount;
                result.RecordsImported = importedCount;
                result.RecordsSkipped = 0;

                progress?.Report(new ImportProgress
                {
                    CurrentRecord = importedCount,
                    TotalRecords = recordCount,
                    CurrentStatus = "Import completed successfully!"
                });
            }
            catch (OperationCanceledException)
            {
                result.Success = false;
                result.ErrorMessage = "Import was cancelled by user.";
                await transaction.RollbackAsync(cancellationToken);
            }
            catch (FileAlreadyImportedException faie)
            {
                result.RecordsImported = 0;
                result.RecordsSkipped = 0;
                result.TotalRecordsProcessed = 0;
                result.Success = true;
                result.ErrorMessage = faie.Message;
                await transaction.RollbackAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.InnerException?.Message ?? ex.Message;
                await transaction.RollbackAsync(cancellationToken);
            }
            finally
            {
                result.EndTime = DateTime.Now;
            }

            return result;
        }

        private async IAsyncEnumerable<DataSetCsvRecord> ReadAllCsvRecordsAsync(string filePath, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = ";",
                HasHeaderRecord = true,
                MissingFieldFound = null,
                BadDataFound = null,
                TrimOptions = TrimOptions.Trim
            };

            using var reader = new StreamReader(filePath);
            using var csv = new CsvReader(reader, config);

            await foreach (var record in csv.GetRecordsAsync<DataSetCsvRecord>(cancellationToken))
            {
                yield return record;
            }
        }

        private async Task BulkUpsertLookupTablesAsync(IAsyncEnumerable<DataSetCsvRecord> records, CancellationToken cancellationToken)
        {
            // Collect unique values while streaming
            var uniqueLicenses = new HashSet<string>();
            var uniqueVins = new HashSet<string>();
            var uniqueProjects = new HashSet<string>();
            var uniqueModels = new HashSet<string>();
            var uniqueReportTypes = new HashSet<string>();
            var uniqueCategories = new HashSet<string>();
            var uniqueOperationCodes = new HashSet<string>();
            var uniqueResultTypes = new HashSet<string>();

            await foreach (var record in records.WithCancellation(cancellationToken))
            {
                uniqueLicenses.Add(record.License);
                uniqueVins.Add(record.VIN);
                uniqueProjects.Add(record.Model.Split(".")[0]);
                uniqueModels.Add(record.Model);
                uniqueReportTypes.Add(record.ReportType);
                uniqueCategories.Add(record.Category);
                uniqueOperationCodes.Add(record.OperationCode);
                uniqueResultTypes.Add(record.ResultType);
            }

            // Load existing values
            var existingLicenses = await _context.Licenses.Select(x => x.Name).ToHashSetAsync(cancellationToken);
            var existingVins = await _context.VINs.Select(x => x.Code).ToHashSetAsync(cancellationToken);
            var existingProjects = await _context.Projects.Select(x => x.Name).ToHashSetAsync(cancellationToken);
            var existingModels = await _context.Models.Select(x => x.Name).ToHashSetAsync(cancellationToken);
            var existingReportTypes = await _context.ReportTypes.Select(x => x.Name).ToHashSetAsync(cancellationToken);
            var existingCategories = await _context.Categories.Select(x => x.Name).ToHashSetAsync(cancellationToken);
            var existingOperationCodes = await _context.OperationCodes.Select(x => x.Name).ToHashSetAsync(cancellationToken);
            var existingResultTypes = await _context.ResultTypes.Select(x => x.Name).ToHashSetAsync(cancellationToken);

            // Insert only new values
            var newLicenses = uniqueLicenses.Where(x => !existingLicenses.Contains(x)).Select(x => new License { Name = x }).ToList();
            var newVins = uniqueVins.Where(x => !existingVins.Contains(x)).Select(x => new VIN { Code = x }).ToList();
            var newProjects = uniqueProjects.Where(x => !existingProjects.Contains(x)).Select(x => new Project { Name = x }).ToList();
            var newModels = uniqueModels.Where(x => !existingModels.Contains(x)).Select(x => new Model { Name = x }).ToList();
            var newReportTypes = uniqueReportTypes.Where(x => !existingReportTypes.Contains(x)).Select(x => new ReportType { Name = x }).ToList();
            var newCategories = uniqueCategories.Where(x => !existingCategories.Contains(x)).Select(x => new Category { Name = x }).ToList();
            var newOperationCodes = uniqueOperationCodes.Where(x => !existingOperationCodes.Contains(x)).Select(x => new OperationCode { Name = x }).ToList();
            var newResultTypes = uniqueResultTypes.Where(x => !existingResultTypes.Contains(x)).Select(x => new ResultType { Name = x }).ToList();

            // Bulk insert new lookup values
            if (newCategories.Count > 0) _context.Categories.AddRange(newCategories);
            if (newLicenses.Count > 0) _context.Licenses.AddRange(newLicenses);
            if (newVins.Count > 0) _context.VINs.AddRange(newVins);
            if (newProjects.Count > 0) _context.Projects.AddRange(newProjects);
            if (newModels.Count > 0) _context.Models.AddRange(newModels);
            if (newReportTypes.Count > 0) _context.ReportTypes.AddRange(newReportTypes);
            if (newOperationCodes.Count > 0) _context.OperationCodes.AddRange(newOperationCodes);
            if (newResultTypes.Count > 0) _context.ResultTypes.AddRange(newResultTypes);
           
            await _context.SaveChangesAsync(cancellationToken);
        }

        private static string GetFileHash(string filePath, HashAlgorithm algorithm)
        {
            using (algorithm)
            using (var stream = System.IO.File.OpenRead(filePath))
            {
                byte[] hashBytes = algorithm.ComputeHash(stream);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
            }
        }

        private async Task<File> CreateFileRecordAsync(string filePath, CancellationToken cancellationToken)
        {
            var fileName = Path.GetFileName(filePath);
            string hash = GetFileHash(filePath, SHA256.Create());
            short week = Convert.ToInt16(fileName.Substring(2, 2));
            int year = Convert.ToInt32(fileName.Substring(5, 4));
            string projectName = fileName.Substring(10, fileName.Length - 14);

            var existingFile = await _context.Files
                .FirstOrDefaultAsync(f => f.Name == fileName && f.Hash == hash, cancellationToken);

            if (existingFile != null)
            {
                throw new FileAlreadyImportedException(
                    $"File '{fileName}' has already been imported successfully. " +
                    "To re-import, please delete the existing file record first.");
            }

            var fileRecord = new File
            {
                Name = fileName,
                StartImportAt = DateTime.Now,
                Hash = hash,
                Week = week,
                Year = year,
            };

            //var projects = await _context.Projects.AsNoTracking()
            //    .ToDictionaryAsync(x => x.Name, x => x.Id, cancellationToken);

            //if (projects.TryGetValue(projectName, out int value))
            //{
            //    fileRecord.Project_Id = value;
            //}
            //else
            //{
            //    fileRecord.Project = new Project { Name = projectName };
            //}

            return fileRecord;
        }

        private async Task<LookupCache> LoadLookupCacheAsync(CancellationToken cancellationToken)
        {
            return new LookupCache
            {
                Licenses = await _context.Licenses.AsNoTracking().ToDictionaryAsync(x => x.Name, x => x.Id, cancellationToken),
                VINs = await _context.VINs.AsNoTracking().ToDictionaryAsync(x => x.Code, x => x.Id, cancellationToken),
                Projects = await _context.Projects.AsNoTracking().ToDictionaryAsync(x => x.Name, x => x.Id, cancellationToken),
                Models = await _context.Models.AsNoTracking().ToDictionaryAsync(x => x.Name, x => x.Id, cancellationToken),
                ReportTypes = await _context.ReportTypes.AsNoTracking().ToDictionaryAsync(x => x.Name, x => x.Id, cancellationToken),
                Categories = await _context.Categories.AsNoTracking().ToDictionaryAsync(x => x.Name, x => x.Id, cancellationToken),
                OperationCodes = await _context.OperationCodes.AsNoTracking().ToDictionaryAsync(x => x.Name, x => x.Id, cancellationToken),
                ResultTypes = await _context.ResultTypes.AsNoTracking().ToDictionaryAsync(x => x.Name, x => x.Id, cancellationToken)
            };
        }

        private DataSet MapToDataSet(DataSetCsvRecord record, File fileRecord, LookupCache cache)
        {
            return new DataSet
            {
                License_Id = cache.Licenses[record.License],
                VIN_Id = cache.VINs[record.VIN],
                Model_Id = cache.Models[record.Model],
                ReportType_Id = cache.ReportTypes[record.ReportType],
                Category_Id = cache.Categories[record.Category],
                OperationCode_Id = cache.OperationCodes[record.OperationCode],
                ResultType_Id = cache.ResultTypes[record.ResultType],
                Project_Id = cache.Projects[record.Model.Split(".")[0]],
                File_Id = fileRecord.Id,
                UTC_DateTime = record.UTC_DateTime,
                ElapsedTime = record.ElapsedTime,
                BCAVersion = record.BCAVersion,
                AppName = record.AppName,
                WUVersion = record.WUVersion,
                ErrorCode = record.ErrorCode,
                AdditionalInfo = record.AdditionalInfo,
                AffectedControllers = record.AffectedControllers
            };
        }

        private class LookupCache
        {
            public Dictionary<string, int> Licenses { get; set; } = new();
            public Dictionary<string, int> VINs { get; set; } = new();
            public Dictionary<string, int> Projects { get; set; } = new();
            public Dictionary<string, int> Models { get; set; } = new();
            public Dictionary<string, int> ReportTypes { get; set; } = new();
            public Dictionary<string, int> Categories { get; set; } = new();
            public Dictionary<string, int> OperationCodes { get; set; } = new();
            public Dictionary<string, int> ResultTypes { get; set; } = new();
        }
    }
}