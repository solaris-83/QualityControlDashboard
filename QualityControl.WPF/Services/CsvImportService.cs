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
        private const int BatchSize = 1000;

        public async Task<ImportResult> ImportCsvAsync(string filePath, IProgress<ImportProgress>? progress = null, CancellationToken cancellationToken = default)
        {
            var result = new ImportResult
            {
                StartTime = DateTime.Now
            };

            try
            {
                // Use explicit transaction for complete rollback on failure
                await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
           
                if (!System.IO.File.Exists(filePath))
                {
                    result.Success = false;
                    result.ErrorMessage = $"File not found: {filePath}";
                    result.EndTime = DateTime.Now;
                    return result;
                }

                // Create file record within transaction
                var fileRecord = await CreateFileRecordAsync(filePath, cancellationToken);
               
                // Load lookup tables into memory for fast access
                var lookupCache = await LoadLookupCacheAsync(cancellationToken);

                // Configure CsvHelper
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

                var records = csv.GetRecords<DataSetCsvRecord>();
                var batch = new List<DataSet>(BatchSize);
                int recordCount = 0;
                int importedCount = 0;
                int skippedCount = 0;

                progress?.Report(new ImportProgress
                {
                    CurrentRecord = 0,
                    TotalRecords = records.Count(),
                    CurrentStatus = "Reading CSV file..."
                });

                foreach (var record in records)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    recordCount++;

                    try
                    {
                        var dataSet = await MapToDataSetAsync(record, fileRecord, lookupCache, cancellationToken);
                        batch.Add(dataSet);

                        if (batch.Count >= BatchSize)
                        {
                            await SaveBatchAsync(batch, cancellationToken);
                            importedCount += batch.Count;
                            batch.Clear();

                            progress?.Report(new ImportProgress
                            {
                                CurrentRecord = recordCount,
                                TotalRecords = records.Count(),
                                CurrentStatus = $"Imported {importedCount} records..."
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        // Critical: if any record fails, rollback entire transaction
                        throw new InvalidOperationException(
                            $"Failed to process record {recordCount}. Import aborted to maintain data integrity.", ex);
                    }
                }

                // Save remaining batch
                if (batch.Count > 0)
                {
                    await SaveBatchAsync(batch, cancellationToken);
                    importedCount += batch.Count;
                }

                // Update file record with completion time
                fileRecord.EndImportAt = DateTime.Now;
                await _context.SaveChangesAsync(cancellationToken);

                // Commit transaction only if everything succeeded
                await transaction.CommitAsync(cancellationToken);

                result.Success = true;
                result.TotalRecordsProcessed = recordCount;
                result.RecordsImported = importedCount;
                result.RecordsSkipped = skippedCount;

                progress?.Report(new ImportProgress
                {
                    CurrentRecord = recordCount,
                    TotalRecords = recordCount,
                    CurrentStatus = "Import completed successfully!"
                });
            }
            catch (OperationCanceledException)
            {
                // Transaction will auto-rollback on dispose
                result.Success = false;
                result.ErrorMessage = "Import was cancelled by user.";
            }
            catch(FileAlreadyImportedException faie)
            {
                result.RecordsImported = 0;
                result.RecordsSkipped = 0;
                result.TotalRecordsProcessed = 0;
                result.Success = true;
                result.ErrorMessage = faie.Message;
            }
            catch (Exception ex)
            {
                // Transaction will auto-rollback on dispose
                result.Success = false;
                result.ErrorMessage = ex.InnerException == null? ex.Message : ex.InnerException.Message;
            }
            finally
            {
                result.EndTime = DateTime.Now;
            }

            return result;
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
            // Check if file already has a successful import
            var existingFile = await _context.Files.FirstOrDefaultAsync(f => f.Name == fileName && f.Hash == hash /*&& f.EndImportAt != null*/, cancellationToken);

            if (existingFile != null)
            {
                throw new FileAlreadyImportedException($"File '{fileName}' has already been imported successfully. " +
                      "To re-import, please delete the existing file record first.");
            }
            
            // Create new file record
            var fileRecord = new File
            {
                Name = fileName,
                StartImportAt = DateTime.Now, 
                Hash = hash, 
                Week = week,
                Year = year, 
            };
            var projects = await _context.Projects.AsNoTracking().ToDictionaryAsync(x => x.Name, x => x.Id, cancellationToken);
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

            return fileRecord;
        }

        private async Task<LookupCache> LoadLookupCacheAsync(CancellationToken cancellationToken)
        {
            return new LookupCache
            {
                Licenses = await _context.Licenses.ToDictionaryAsync(x => x.Name, x => x.Id, cancellationToken),
                VINs = await _context.VINs.ToDictionaryAsync(x => x.Code, x => x.Id, cancellationToken),
                Projects = await _context.Projects.ToDictionaryAsync(x => x.Name, x => x.Id, cancellationToken),
                Models = await _context.Models.ToDictionaryAsync(x => x.Name, x => x.Id, cancellationToken),
                ReportTypes = await _context.ReportTypes.ToDictionaryAsync(x => x.Name, x => x.Id, cancellationToken),
                Categories = await _context.Categories.ToDictionaryAsync(x => x.Name, x => x.Id, cancellationToken),
                OperationCodes = await _context.OperationCodes.ToDictionaryAsync(x => x.Name, x => x.Id, cancellationToken),
                ResultTypes = await _context.ResultTypes.ToDictionaryAsync(x => x.Name, x => x.Id, cancellationToken)
            };
        }

        private async Task<DataSet> MapToDataSetAsync(
            DataSetCsvRecord record,
            File fileRecord,
            LookupCache cache,
            CancellationToken cancellationToken)
        {
            return new DataSet
            {
                License_Id = await GetOrCreateLookupIdAsync(record.License, cache.Licenses, 
                    () => new DB.Models.License { Name = record.License }, cancellationToken),
                VIN_Id = await GetOrCreateLookupIdAsync(record.VIN, cache.VINs, 
                    () => new VIN { Code = record.VIN }, cancellationToken),
                Model_Id = await GetOrCreateLookupIdAsync(record.Model, cache.Models, 
                    () => new Model { Name = record.Model }, cancellationToken),
                ReportType_Id = await GetOrCreateLookupIdAsync(record.ReportType, cache.ReportTypes, 
                    () => new ReportType { Name = record.ReportType }, cancellationToken),
                Category_Id = await GetOrCreateLookupIdAsync(record.Category, cache.Categories, 
                    () => new Category { Name = record.Category }, cancellationToken),
                OperationCode_Id = await GetOrCreateLookupIdAsync(record.OperationCode, cache.OperationCodes, 
                    () => new OperationCode { Name = record.OperationCode }, cancellationToken),
                ResultType_Id = await GetOrCreateLookupIdAsync(record.ResultType, cache.ResultTypes, 
                    () => new ResultType { Name = record.ResultType }, cancellationToken),
                Project_Id = await GetOrCreateLookupIdAsync(record.Model.Split(".")[0], cache.Projects, 
                    () => new Project { Name = record.Model.Split(".")[0] }, cancellationToken),
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

        private async Task<int> GetOrCreateLookupIdAsync<T>(
            string key,
            Dictionary<string, int> cache,
            Func<T> createEntity,
            CancellationToken cancellationToken) where T : class
        {
            if (cache.TryGetValue(key, out var id))
            {
                return id;
            }

            // Create new entity within the same transaction
            var entity = createEntity();
            _context.Set<T>().Add(entity);
            await _context.SaveChangesAsync(cancellationToken);

            // Get the ID using reflection
            id = (int)entity.GetType().GetProperty("Id")!.GetValue(entity)!;
            cache[key] = id;

            return id;
        }

        private TimeSpan? ParseElapsedTime(string? elapsedTime)
        {
            if (string.IsNullOrWhiteSpace(elapsedTime))
                return null;

            if (TimeSpan.TryParse(elapsedTime, out var result))
                return result;

            if (double.TryParse(elapsedTime, out var seconds))
                return TimeSpan.FromSeconds(seconds);

            return null;
        }

        private async Task SaveBatchAsync(List<DataSet> batch, CancellationToken cancellationToken)
        {
            _context.DataSets.AddRange(batch);
            await _context.SaveChangesAsync(cancellationToken);
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