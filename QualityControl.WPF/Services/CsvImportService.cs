using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using QualityControl.WPF.DB;
using QualityControl.WPF.DB.Models;
using QualityControl.WPF.Exceptions;
using QualityControl.WPF.Models;
using System.Data;
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

        public async Task<ImportResultDto> ImportCsvAsync(string filePath, IProgress<ImportProgressDto>? progress = null, CancellationToken cancellationToken = default)
        {
            var result = new ImportResultDto { StartTime = DateTime.Now };
            File fileRecord = null;
            //  await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                // Validate file existence early
                if (!System.IO.File.Exists(filePath))
                {
                    result.Success = false;
                    result.ErrorMessage = $"File not found: {filePath}";
                    result.EndTime = DateTime.Now;
                    return result;
                }

                result.FullName = filePath;
                var fileName = Path.GetFileName(filePath);

                // Step 1: Create/validate file record (separate transaction)
                fileRecord = await CreateAndValidateFileRecordAsync(filePath, fileName, progress, cancellationToken);

                await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

                try
                {
                    // Step 2: Single-pass processing with streaming
                    var (recordCount, importedCount) = await ProcessCsvWithStreamingAsync(
                        filePath,
                        fileRecord,
                        fileName,
                        progress,
                        transaction,
                        cancellationToken);

                    // Step 3: Update file completion time (separate small transaction)
                //    await UpdateFileCompletionAsync(fileRecord.Id, recordCount, cancellationToken);

                    
                    result.Success = true;
                    result.TotalRecordsProcessed = recordCount;
                    result.RecordsImported = importedCount;
                    result.RecordsSkipped = 0;


                    await transaction.CommitAsync(cancellationToken);

                    progress?.Report(new ImportProgressDto
                    {
                        FileName = fileName,
                        CurrentRecord = importedCount,
                        TotalRecords = recordCount,
                        CurrentStatus = "Import completed successfully!"
                    });
                }
                catch (OperationCanceledException)
                {
                    result.RecordsImported = 0;
                    result.RecordsSkipped = 0;
                    result.TotalRecordsProcessed = 0;
                    result.Success = false;
                    result.ErrorMessage = "Import was cancelled by user.";
                }
                catch (AlreadyImportedFileApplicationException faie)
                {
                    result.RecordsImported = 0;
                    result.RecordsSkipped = 0;
                    result.TotalRecordsProcessed = 0;
                    result.Success = true;
                    result.ErrorMessage = faie.Message;
                }
                catch (Exception ex)
                {
                    result.RecordsImported = 0;
                    result.RecordsSkipped = 0;
                    result.TotalRecordsProcessed = 0;
                    result.Success = false;
                    result.ErrorMessage = ex.InnerException?.Message ?? ex.Message;
                }
                //finally
                //{
                //    result.EndTime = DateTime.Now;
                //}
            }
            //catch (OperationCanceledException)
            //{
            //    result.Success = false;
            //    result.ErrorMessage = "Import was cancelled by user.";
            //}
            //catch (FileAlreadyImportedException faie)
            //{
            //    result.RecordsImported = 0;
            //    result.RecordsSkipped = 0;
            //    result.TotalRecordsProcessed = 0;
            //    result.Success = true;
            //    result.ErrorMessage = faie.Message;
            //}
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.InnerException?.Message ?? ex.Message;
            }
            finally
            {
                result.EndTime = DateTime.Now;
                // Step 3: Update file completion time (separate small transaction)
                if (fileRecord != null)
                    await UpdateFileCompletionAsync(fileRecord.Id, result.RecordsImported, result.ErrorMessage, cancellationToken);
            }

            return result;
        }
        /// <summary>
        /// CORRECTED: Single-pass CSV processing with lookup-first strategy
        /// Ensures all foreign key references exist before inserting data records
        /// </summary>
        private async Task<(int recordCount, int importedCount)> ProcessCsvWithStreamingAsync(
            string filePath,
            File fileRecord,
            string fileName,
            IProgress<ImportProgressDto>? progress,
            IDbContextTransaction transaction,
            CancellationToken cancellationToken)
        {
            int recordCount = 0;
            int importedCount = 0;

            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = ";",
                HasHeaderRecord = true,
                MissingFieldFound = null,
                BadDataFound = null,
                TrimOptions = TrimOptions.Trim
            };

            progress?.Report(new ImportProgressDto
            {
                FileName = fileName,
                CurrentRecord = 0,
                TotalRecords = 0,
                CurrentStatus = "Analyzing CSV and collecting unique values..."
            });

            // PHASE 1: Collect ALL unique lookup values first
            var lookupCollector = new LookupCollector();

            using (var reader = new StreamReader(filePath, new FileStreamOptions
            {
                BufferSize = 81920,
                Mode = FileMode.Open,
                Access = FileAccess.Read,
                Share = FileShare.Read,
                Options = FileOptions.SequentialScan
            }))
            using (var csv = new CsvReader(reader, config))
            {
                csv.Context.RegisterClassMap<DataSetMap>();
                var records = csv.GetRecordsAsync<DataSetCsvRecord>(cancellationToken);

                await foreach (var record in records.WithCancellation(cancellationToken))
                {
                    recordCount++;

                    // Intern strings and collect unique values
                    var license = string.Intern(record.License);
                    var vin = string.Intern(record.VIN);
                    var model = string.Intern(record.Model);
                    var reportType = string.Intern(record.ReportType);
                    var category = string.Intern(record.Category);
                    var operationCode = string.Intern(record.OperationCode);
                    var resultType = string.Intern(record.ResultType);
                    var projectName = string.Intern(model.Split(".")[0]);

                    lookupCollector.Add(license, vin, projectName, model, reportType, category, operationCode, resultType);

                    // Report progress periodically
                    if (recordCount % 10000 == 0)
                    {
                        progress?.Report(new ImportProgressDto
                        {
                            FileName = fileName,
                            CurrentRecord = recordCount,
                            TotalRecords = 0,
                            CurrentStatus = $"Analyzed {recordCount:N0} records..."
                        });
                    }
                }
            }

            progress?.Report(new ImportProgressDto
            {
                FileName = fileName,
                CurrentRecord = recordCount,
                TotalRecords = recordCount,
                CurrentStatus = "Inserting lookup values..."
            });

            // PHASE 2: Bulk insert ALL lookup values at once
            var lookupCache = await BulkInsertAllLookupsAsync(lookupCollector, transaction, cancellationToken);

            // Clear collector to free memory
            lookupCollector.Clear();

            progress?.Report(new ImportProgressDto
            {
                FileName = fileName,
                CurrentRecord = 0,
                TotalRecords = recordCount,
                CurrentStatus = "Inserting data records..."
            });

            // PHASE 3: Second pass - Insert data records with guaranteed foreign keys
            var dataBatch = new List<DB.Models.DataSet>(BatchSize);

            using (var reader = new StreamReader(filePath, new FileStreamOptions
            {
                BufferSize = 81920,
                Mode = FileMode.Open,
                Access = FileAccess.Read,
                Share = FileShare.Read,
                Options = FileOptions.SequentialScan
            }))
            using (var csv = new CsvReader(reader, config))
            {
                var records = csv.GetRecordsAsync<DataSetCsvRecord>(cancellationToken);

                await foreach (var record in records.WithCancellation(cancellationToken))
                {
                    // Intern strings (they should already be in intern pool from first pass)
                    var license = string.Intern(record.License);
                    var vin = string.Intern(record.VIN);
                    var model = string.Intern(record.Model);
                    var reportType = string.Intern(record.ReportType);
                    var category = string.Intern(record.Category);
                    var operationCode = string.Intern(record.OperationCode);
                    var resultType = string.Intern(record.ResultType);
                    var projectName = string.Intern(model.Split(".")[0]);

                    // Map to DataSet - all keys are guaranteed to exist
                    var dataSet = MapToDataSetSafe(
                        license, vin, model, reportType, category, operationCode, resultType, projectName,
                        fileRecord, lookupCache, record);

                    dataBatch.Add(dataSet);

                    // Batch insert data records
                    if (dataBatch.Count >= BatchSize)
                    {
                        await InsertDataBatchAsync(dataBatch, transaction, cancellationToken);
                        importedCount += dataBatch.Count;
                        dataBatch.Clear();

                        // Report progress
                        progress?.Report(new ImportProgressDto
                        {
                            FileName = fileName,
                            CurrentRecord = importedCount,
                            TotalRecords = recordCount,
                            CurrentStatus = $"Inserted {importedCount:N0} / {recordCount:N0} records..."
                        });

                        // Hint GC for large batches
                        if (importedCount % (BatchSize * 10) == 0)
                        {
                            GC.Collect(1, GCCollectionMode.Optimized, false);
                        }
                    }
                }
            }

            // Final insert of remaining data
            if (dataBatch.Count > 0)
            {
                await InsertDataBatchAsync(dataBatch, transaction, cancellationToken);
                importedCount += dataBatch.Count;
            }

            return (recordCount, importedCount);
        }

        /// <summary>
        /// CORRECTED: Bulk insert all lookup values and return complete cache
        /// Ensures all foreign keys exist before data insertion
        /// </summary>
        private async Task<LookupCache> BulkInsertAllLookupsAsync(
            LookupCollector collector,
            IDbContextTransaction transaction,
            CancellationToken cancellationToken)
        {
           // await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                // Load existing lookup values
                var existingLicenses = await _context.Licenses
                    .AsNoTracking()
                    .Select(x => x.Name)
                    .ToHashSetAsync(cancellationToken);

                var existingVins = await _context.VINs
                    .AsNoTracking()
                    .Select(x => x.Code)
                    .ToHashSetAsync(cancellationToken);

                var existingProjects = await _context.Projects
                    .AsNoTracking()
                    .Select(x => x.Name)
                    .ToHashSetAsync(cancellationToken);

                var existingModels = await _context.Models
                    .AsNoTracking()
                    .Select(x => x.Name)
                    .ToHashSetAsync(cancellationToken);

                var existingReportTypes = await _context.ReportTypes
                    .AsNoTracking()
                    .Select(x => x.Name)
                    .ToHashSetAsync(cancellationToken);

                var existingCategories = await _context.Categories
                    .AsNoTracking()
                    .Select(x => x.Name)
                    .ToHashSetAsync(cancellationToken);

                var existingOperationCodes = await _context.OperationCodes
                    .AsNoTracking()
                    .Select(x => x.Name)
                    .ToHashSetAsync(cancellationToken);

                var existingResultTypes = await _context.ResultTypes
                    .AsNoTracking()
                    .Select(x => x.Name)
                    .ToHashSetAsync(cancellationToken);

                // Filter out only NEW values
                var newLicenses = collector.Licenses
                    .Where(x => !existingLicenses.Contains(x))
                    .Select(x => new License { Name = x })
                    .ToList();

                var newVins = collector.VINs
                    .Where(x => !existingVins.Contains(x))
                    .Select(x => new VIN { Code = x })
                    .ToList();

                var newProjects = collector.Projects
                    .Where(x => !existingProjects.Contains(x))
                    .Select(x => new Project { Name = x })
                    .ToList();

                var newModels = collector.Models
                    .Where(x => !existingModels.Contains(x))
                    .Select(x => new Model { Name = x })
                    .ToList();

                var newReportTypes = collector.ReportTypes
                    .Where(x => !existingReportTypes.Contains(x))
                    .Select(x => new ReportType { Name = x })
                    .ToList();

                var newCategories = collector.Categories
                    .Where(x => !existingCategories.Contains(x))
                    .Select(x => new Category { Name = x })
                    .ToList();

                var newOperationCodes = collector.OperationCodes
                    .Where(x => !existingOperationCodes.Contains(x))
                    .Select(x => new OperationCode { Name = x })
                    .ToList();

                var newResultTypes = collector.ResultTypes
                    .Where(x => !existingResultTypes.Contains(x))
                    .Select(x => new ResultType { Name = x })
                    .ToList();

                // Bulk insert new values - ORDER MATTERS for foreign key dependencies
                // Insert in order: Categories, Projects, then others
                if (newCategories.Count > 0)
                {
                    _context.Categories.AddRange(newCategories);
                  //  await _context.SaveChangesAsync(cancellationToken);
                }

                if (newProjects.Count > 0)
                {
                    _context.Projects.AddRange(newProjects);
                   // await _context.SaveChangesAsync(cancellationToken);
                }

                if (newLicenses.Count > 0)
                {
                    _context.Licenses.AddRange(newLicenses);
                  //  await _context.SaveChangesAsync(cancellationToken);
                }

                if (newVins.Count > 0)
                {
                    _context.VINs.AddRange(newVins);
                  //  await _context.SaveChangesAsync(cancellationToken);
                }

                if (newModels.Count > 0)
                {
                    _context.Models.AddRange(newModels);
                  //  await _context.SaveChangesAsync(cancellationToken);
                }

                if (newReportTypes.Count > 0)
                {
                    _context.ReportTypes.AddRange(newReportTypes);
                  //  await _context.SaveChangesAsync(cancellationToken);
                }

                if (newOperationCodes.Count > 0)
                {
                    _context.OperationCodes.AddRange(newOperationCodes);
                   // await _context.SaveChangesAsync(cancellationToken);
                }

                if (newResultTypes.Count > 0)
                {
                    _context.ResultTypes.AddRange(newResultTypes);
                  //  await _context.SaveChangesAsync(cancellationToken);
                }

                await _context.SaveChangesAsync(cancellationToken);
                //await transaction.CommitAsync(cancellationToken);

                // Clear collections to free memory
                existingLicenses.Clear();
                existingVins.Clear();
                existingProjects.Clear();
                existingModels.Clear();
                existingReportTypes.Clear();
                existingCategories.Clear();
                existingOperationCodes.Clear();
                existingResultTypes.Clear();
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }

            // Load complete cache with ALL values (existing + newly inserted)
            return await LoadLookupCacheAsync(cancellationToken);
        }

        /// <summary>
        /// CORRECTED: Safe mapping with key existence validation
        /// Throws descriptive error if any foreign key is missing
        /// </summary>
        private static DB.Models.DataSet MapToDataSetSafe(
            string license,
            string vin,
            string model,
            string reportType,
            string category,
            string operationCode,
            string resultType,
            string projectName,
            File fileRecord,
            LookupCache cache,
            DataSetCsvRecord record)
        {
            // Validate all keys exist before accessing
            if (!cache.Licenses.TryGetValue(license, out int licenseId))
                throw new InvalidOperationException($"License '{license}' not found in cache");

            if (!cache.VINs.TryGetValue(vin, out int vinId))
                throw new InvalidOperationException($"VIN '{vin}' not found in cache");

            if (!cache.Models.TryGetValue(model, out int modelId))
                throw new InvalidOperationException($"Model '{model}' not found in cache");

            if (!cache.ReportTypes.TryGetValue(reportType, out int reportTypeId))
                throw new InvalidOperationException($"ReportType '{reportType}' not found in cache");

            if (!cache.Categories.TryGetValue(category, out int categoryId))
                throw new InvalidOperationException($"Category '{category}' not found in cache");

            if (!cache.OperationCodes.TryGetValue(operationCode, out int operationCodeId))
                throw new InvalidOperationException($"OperationCode '{operationCode}' not found in cache");

            if (!cache.ResultTypes.TryGetValue(resultType, out int resultTypeId))
                throw new InvalidOperationException($"ResultType '{resultType}' not found in cache");

            if (!cache.Projects.TryGetValue(projectName, out int projectId))
                throw new InvalidOperationException($"Project '{projectName}' not found in cache");

            return new DB.Models.DataSet
            {
                License_Id = licenseId,
                VIN_Id = vinId,
                Model_Id = modelId,
                ReportType_Id = reportTypeId,
                Category_Id = categoryId,
                OperationCode_Id = operationCodeId,
                ResultType_Id = resultTypeId,
                Project_Id = projectId,
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

        /// <summary>
        /// OPTIMIZED: Create file record with separate transaction
        /// Prevents long-running transactions that lock database resources
        /// </summary>
        private async Task<File> CreateAndValidateFileRecordAsync(string filePath, string fileName, IProgress<ImportProgressDto>? progress, CancellationToken cancellationToken)
        {
           // await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                string hash = GetFileHash(filePath, SHA256.Create());
                short week = Convert.ToInt16(fileName.Substring(2, 2));
                int year = Convert.ToInt32(fileName.Substring(5, 4));

                // Check for exact duplicate
                var existingFile = await _context.Files
                    .AsNoTracking()
                    .FirstOrDefaultAsync(f => f.Name == fileName && f.Hash == hash, cancellationToken);

                if (existingFile != null)
                {
                    throw new AlreadyImportedFileApplicationException(
                        $"File '{fileName}' has already been imported successfully. " +
                        "To re-import, please delete the existing file record first.");
                }

                // Handle file with same name but different hash
                existingFile = await _context.Files.FirstOrDefaultAsync(f => f.Name == fileName && f.Hash != hash, cancellationToken);

                if (existingFile != null)
                {
                    progress?.Report(new ImportProgressDto
                    {
                        FileName = fileName,
                        CurrentRecord = 0,
                        TotalRecords = 0,
                        CurrentStatus = "Deleting rows of the previous import since a new file with the same name and different hash has been published...."
                    });
                    // OPTIMIZATION: Delete in separate batch to avoid loading all rows into memory
                    await _context.DataSets
                        .Where(d => d.File_Id == existingFile.Id)
                        .ExecuteDeleteAsync(cancellationToken);
                    await _context.SaveChangesAsync(cancellationToken);

                    _context.Files.Remove(existingFile);
                    await _context.SaveChangesAsync(cancellationToken);
                }

                // TODO allow to reimport an already imported with error file

                progress?.Report(new ImportProgressDto
                {
                    FileName = fileName,
                    CurrentRecord = 0,
                    TotalRecords = 0,
                    CurrentStatus = "Validating file..."
                });

                // Extract project name and link
                var projectName = fileName.Substring(10, fileName.Length - 14);
                var projects = await _context.Projects
                    .AsNoTracking()
                    .Where(p => p.Name == projectName)
                    .ToDictionaryAsync(x => x.Name, x => x.Id, cancellationToken);

                var fileRecord = new File
                {
                    Name = fileName,
                    StartImportAt = DateTime.Now,
                    Hash = hash,
                    Week = week,
                    Year = year,
                };

                if (projects.TryGetValue(projectName, out int projectId))
                {
                    fileRecord.Project_Id = projectId;
                }
                else
                {
                    fileRecord.Project = new Project { Name = projectName };
                }

                _context.Files.Add(fileRecord);

                return fileRecord;
            }
            catch
            {
                throw;
            }
            finally
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
        }

        /// <summary>
        /// OPTIMIZED: Batch insert with single transaction per batch
        /// Detaches entities after save to free memory
        /// </summary>
        private async Task InsertDataBatchAsync(List<DB.Models.DataSet> batch, IDbContextTransaction transaction, CancellationToken cancellationToken)
        {
          //  await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                _context.DataSets.AddRange(batch);
                await _context.SaveChangesAsync(cancellationToken);
               // await transaction.CommitAsync(cancellationToken);

                // CRITICAL: Detach entities to free memory
                //foreach (var entity in batch)
                //{
                //    _context.Entry(entity).State = EntityState.Detached;
                //}
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        ///// <summary>
        ///// OPTIMIZED: Flush collected lookups to database
        ///// Updates cache with newly inserted IDs for immediate use
        ///// </summary>
        //private async Task FlushLookupsAsync(
        //    LookupCollector collector,
        //    LookupCache cache,
        //    IDbContextTransaction transaction,
        //    CancellationToken cancellationToken)
        //{
        //    if (!collector.HasPendingItems()) return;

        //  //  await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        //    try
        //    {
        //        // Filter out already existing items
        //        var newLicenses = collector.Licenses.Where(x => !cache.Licenses.ContainsKey(x))
        //            .Select(x => new License { Name = x }).ToList();
        //        var newVins = collector.VINs.Where(x => !cache.VINs.ContainsKey(x))
        //            .Select(x => new VIN { Code = x }).ToList();
        //        var newProjects = collector.Projects.Where(x => !cache.Projects.ContainsKey(x))
        //            .Select(x => new Project { Name = x }).ToList();
        //        var newModels = collector.Models.Where(x => !cache.Models.ContainsKey(x))
        //            .Select(x => new Model { Name = x }).ToList();
        //        var newReportTypes = collector.ReportTypes.Where(x => !cache.ReportTypes.ContainsKey(x))
        //            .Select(x => new ReportType { Name = x }).ToList();
        //        var newCategories = collector.Categories.Where(x => !cache.Categories.ContainsKey(x))
        //            .Select(x => new Category { Name = x }).ToList();
        //        var newOperationCodes = collector.OperationCodes.Where(x => !cache.OperationCodes.ContainsKey(x))
        //            .Select(x => new OperationCode { Name = x }).ToList();
        //        var newResultTypes = collector.ResultTypes.Where(x => !cache.ResultTypes.ContainsKey(x))
        //            .Select(x => new ResultType { Name = x }).ToList();

        //        // Bulk insert
        //        if (newLicenses.Count > 0) _context.Licenses.AddRange(newLicenses);
        //        if (newVins.Count > 0) _context.VINs.AddRange(newVins);
        //        if (newProjects.Count > 0) _context.Projects.AddRange(newProjects);
        //        if (newModels.Count > 0) _context.Models.AddRange(newModels);
        //        if (newReportTypes.Count > 0) _context.ReportTypes.AddRange(newReportTypes);
        //        if (newCategories.Count > 0) _context.Categories.AddRange(newCategories);
        //        if (newOperationCodes.Count > 0) _context.OperationCodes.AddRange(newOperationCodes);
        //        if (newResultTypes.Count > 0) _context.ResultTypes.AddRange(newResultTypes);

        //        await _context.SaveChangesAsync(cancellationToken);
        //      //  await transaction.CommitAsync(cancellationToken);

        //        // Update cache with new IDs
        //        foreach (var item in newLicenses)
        //            cache.Licenses[item.Name] = item.Id;
        //        foreach (var item in newVins)
        //            cache.VINs[item.Code] = item.Id;
        //        foreach (var item in newProjects)
        //            cache.Projects[item.Name] = item.Id;
        //        foreach (var item in newModels)
        //            cache.Models[item.Name] = item.Id;
        //        foreach (var item in newReportTypes)
        //            cache.ReportTypes[item.Name] = item.Id;
        //        foreach (var item in newCategories)
        //            cache.Categories[item.Name] = item.Id;
        //        foreach (var item in newOperationCodes)
        //            cache.OperationCodes[item.Name] = item.Id;
        //        foreach (var item in newResultTypes)
        //            cache.ResultTypes[item.Name] = item.Id;

        //        // CRITICAL: Detach to free memory
        //        _context.ChangeTracker.Clear();
        //    }
        //    catch
        //    {
        //        await transaction.RollbackAsync(cancellationToken);
        //        throw;
        //    }
        //}

        /// <summary>
        /// Update file completion timestamp using ExecuteUpdate (no entity tracking)
        /// </summary>
        private async Task UpdateFileCompletionAsync(int fileId, int totalRecordsProcessed, string errorMessage, CancellationToken cancellationToken)
        {
          //  await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                await _context.Files
                    .Where(f => f.Id == fileId)
                    .ExecuteUpdateAsync(s => s.SetProperty(f => f.EndImportAt, DateTime.Now)
                                              .SetProperty(f => f.NumberOfRecords, totalRecordsProcessed)
                                              .SetProperty(f => f.ErrorMessage, errorMessage), cancellationToken);

                await _context.SaveChangesAsync(cancellationToken);
            }
            catch
            {
               // await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        /// <summary>
        /// Load lookup cache from database - called once at the start
        /// </summary>
        private async Task<LookupCache> LoadLookupCacheAsync(CancellationToken cancellationToken)
        {
            return new LookupCache
            {
                Licenses = await _context.Licenses.AsNoTracking()
                    .ToDictionaryAsync(x => x.Name, x => x.Id, cancellationToken),
                VINs = await _context.VINs.AsNoTracking()
                    .ToDictionaryAsync(x => x.Code, x => x.Id, cancellationToken),
                Projects = await _context.Projects.AsNoTracking()
                    .ToDictionaryAsync(x => x.Name, x => x.Id, cancellationToken),
                Models = await _context.Models.AsNoTracking()
                    .ToDictionaryAsync(x => x.Name, x => x.Id, cancellationToken),
                ReportTypes = await _context.ReportTypes.AsNoTracking()
                    .ToDictionaryAsync(x => x.Name, x => x.Id, cancellationToken),
                Categories = await _context.Categories.AsNoTracking()
                    .ToDictionaryAsync(x => x.Name, x => x.Id, cancellationToken),
                OperationCodes = await _context.OperationCodes.AsNoTracking()
                    .ToDictionaryAsync(x => x.Name, x => x.Id, cancellationToken),
                ResultTypes = await _context.ResultTypes.AsNoTracking()
                    .ToDictionaryAsync(x => x.Name, x => x.Id, cancellationToken)
            };
        }

        ///// <summary>
        ///// Map CSV record to DataSet entity using interned strings and cached lookup IDs
        ///// </summary>
        //private static DB.Models.DataSet MapToDataSet(
        //    string license,
        //    string vin,
        //    string model,
        //    string reportType,
        //    string category,
        //    string operationCode,
        //    string resultType,
        //    string projectName,
        //    File fileRecord,
        //    LookupCache cache,
        //    DataSetCsvRecord record)
        //{
        //    return new DB.Models.DataSet
        //    {
        //        License_Id = cache.Licenses[license],
        //        VIN_Id = cache.VINs[vin],
        //        Model_Id = cache.Models[model],
        //        ReportType_Id = cache.ReportTypes[reportType],
        //        Category_Id = cache.Categories[category],
        //        OperationCode_Id = cache.OperationCodes[operationCode],
        //        ResultType_Id = cache.ResultTypes[resultType],
        //        Project_Id = cache.Projects[projectName],
        //        File_Id = fileRecord.Id,
        //        UTC_DateTime = record.UTC_DateTime,
        //        ElapsedTime = record.ElapsedTime,
        //        BCAVersion = record.BCAVersion,
        //        AppName = record.AppName,
        //        WUVersion = record.WUVersion,
        //        ErrorCode = record.ErrorCode,
        //        AdditionalInfo = record.AdditionalInfo,
        //        AffectedControllers = record.AffectedControllers
        //    };
        //}

        /// <summary>
        /// Compute file hash for duplicate detection
        /// </summary>
        private static string GetFileHash(string filePath, HashAlgorithm algorithm)
        {
            using (algorithm)
            using (var stream = System.IO.File.OpenRead(filePath))
            {
                byte[] hashBytes = algorithm.ComputeHash(stream);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
            }
        }

        #region Helper Classes

        /// <summary>
        /// Cache for lookup table IDs to avoid repeated database queries
        /// </summary>
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

        /// <summary>
        /// Collector for unique lookup values during streaming
        /// Uses HashSet for O(1) duplicate detection
        /// </summary>
        private class LookupCollector
        {
            public HashSet<string> Licenses { get; } = new();
            public HashSet<string> VINs { get; } = new();
            public HashSet<string> Projects { get; } = new();
            public HashSet<string> Models { get; } = new();
            public HashSet<string> ReportTypes { get; } = new();
            public HashSet<string> Categories { get; } = new();
            public HashSet<string> OperationCodes { get; } = new();
            public HashSet<string> ResultTypes { get; } = new();

            public void Add(
                string license,
                string vin,
                string project,
                string model,
                string reportType,
                string category,
                string operationCode,
                string resultType)
            {
                Licenses.Add(license);
                VINs.Add(vin);
                Projects.Add(project);
                Models.Add(model);
                ReportTypes.Add(reportType);
                Categories.Add(category);
                OperationCodes.Add(operationCode);
                ResultTypes.Add(resultType);
            }

            public bool ShouldFlush(int threshold)
            {
                return Licenses.Count >= threshold ||
                       VINs.Count >= threshold ||
                       Projects.Count >= threshold ||
                       Models.Count >= threshold ||
                       ReportTypes.Count >= threshold ||
                       Categories.Count >= threshold ||
                       OperationCodes.Count >= threshold ||
                       ResultTypes.Count >= threshold;
            }

            public bool HasPendingItems()
            {
                return Licenses.Count > 0 ||
                       VINs.Count > 0 ||
                       Projects.Count > 0 ||
                       Models.Count > 0 ||
                       ReportTypes.Count > 0 ||
                       Categories.Count > 0 ||
                       OperationCodes.Count > 0 ||
                       ResultTypes.Count > 0;
            }

            public void Clear()
            {
                Licenses.Clear();
                VINs.Clear();
                Projects.Clear();
                Models.Clear();
                ReportTypes.Clear();
                Categories.Clear();
                OperationCodes.Clear();
                ResultTypes.Clear();
            }
        }

        #endregion
    }
}