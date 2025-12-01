using CsvHelper;
using CsvHelper.Configuration;
using OfficeOpenXml;
using System.Data;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Threading;
using TradeDataStudio.Core.Interfaces;
using TradeDataStudio.Core.Models;
using Microsoft.Data.SqlClient;

namespace TradeDataStudio.Core.Services
{
    public class ExportService : IExportService
    {
        private readonly IConfigurationService _configService;
        private readonly ILoggingService _loggingService;
        private int _batchSize = 50000;

        public ExportService(IConfigurationService configService, ILoggingService loggingService)
        {
            _configService = configService ?? throw new ArgumentNullException(nameof(configService));
            _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
            
            // Set EPPlus license context
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            
            // Load batch size from configuration
            _ = LoadConfigurationAsync();
        }
        
        private async Task LoadConfigurationAsync()
        {
            try
            {
                var appSettings = await _configService.GetApplicationSettingsAsync();
                _batchSize = appSettings.Performance.BatchSize;
                await _loggingService.LogMainAsync($"Export batch size configured: {_batchSize:N0} rows");
            }
            catch
            {
                _batchSize = 50000; // Default fallback
            }
        }

        public async Task<ExportResult> ExportToExcelAsync(
            string tableName, 
            string outputPath, 
            DataTable data, 
            OperationMode mode = OperationMode.Export,
            string startPeriod = "",
            string endPeriod = "",
            int tableSequence = 1,
            bool isDirectDownload = false,
            CancellationToken cancellationToken = default)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                var fileName = GenerateFileName(tableName, ExportFormat.Excel, mode, startPeriod, endPeriod, tableSequence, isDirectDownload);
                var fullPath = Path.Combine(outputPath, fileName);
                
                Directory.CreateDirectory(outputPath);
                
                int totalRows = data.Rows.Count;
                
                // EPPlus Excel limit is 1,048,576 rows (including header)
                if (totalRows > 1048575)
                {
                    throw new InvalidOperationException($"Excel format supports maximum 1,048,575 data rows. Current data has {totalRows:N0} rows. Please use CSV format or filter the data.");
                }
                
                // Log suggestion for large datasets
                if (totalRows > 1000000)
                {
                    await _loggingService.LogMainAsync($"⚠ Performance Notice: Exporting {totalRows:N0} rows to Excel. For better performance with large datasets, consider using CSV format.");
                }
                
                await _loggingService.LogMainAsync($"Starting Excel export for {tableName} with {totalRows:N0} rows...");

                // Run export in background task to prevent UI blocking
                await Task.Run(async () =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    
                    using var package = new ExcelPackage();
                    var worksheet = package.Workbook.Worksheets.Add(tableName);

                    // Use EPPlus LoadFromDataTable for bulk insertion (60-80% faster than cell-by-cell)
                    worksheet.Cells["A1"].LoadFromDataTable(data, true);
                    
                    cancellationToken.ThrowIfCancellationRequested();
                    
                    // Format header row with MS Sans Serif font
                    using (var headerRange = worksheet.Cells[1, 1, 1, data.Columns.Count])
                    {
                        headerRange.Style.Font.Bold = false;
                        headerRange.Style.Font.Name = "MS Sans Serif";
                        headerRange.Style.Font.Size = 10;
                    }
                    
                    cancellationToken.ThrowIfCancellationRequested();
                    
                    // Apply column-level formatting with MS Sans Serif font and preserve exact data values
                    for (int col = 0; col < data.Columns.Count; col++)
                    {
                        var dataType = data.Columns[col].DataType;
                        var excelCol = col + 1;
                        
                        // Apply formatting to entire column (excluding header)
                        if (totalRows > 0)
                        {
                            using (var columnRange = worksheet.Cells[2, excelCol, totalRows + 1, excelCol])
                            {
                                // Set MS Sans Serif font for all data cells
                                columnRange.Style.Font.Name = "MS Sans Serif";
                                
                                if (dataType == typeof(DateTime))
                                {
                                    columnRange.Style.Numberformat.Format = "yyyy-mm-dd";
                                }
                                else if (dataType == typeof(decimal) || dataType == typeof(double) || dataType == typeof(float))
                                {
                                    // Use General format to preserve exact values without rounding
                                    // This ensures whatever precision exists in source data is maintained
                                    columnRange.Style.Numberformat.Format = "General";
                                }
                                // Integer and other types use default formatting with MS Sans Serif font
                            }
                        }
                    }
                    
                    cancellationToken.ThrowIfCancellationRequested();
                    
                    // Save Excel file to disk
                    await package.SaveAsAsync(new FileInfo(fullPath));
                });

                stopwatch.Stop();

                var fileInfo = new FileInfo(fullPath);
                var result = new ExportResult
                {
                    Success = true,
                    FilePath = fullPath,
                    FileName = fileName,
                    FileSize = fileInfo.Length,
                    RecordsExported = data.Rows.Count,
                    Format = ExportFormat.Excel,
                    ElapsedTime = stopwatch.Elapsed,
                    Message = $"Successfully exported {data.Rows.Count:N0} records to Excel in {stopwatch.Elapsed.TotalSeconds:F2}s"
                };

                return result;
            }
            catch (OperationCanceledException)
            {
                stopwatch.Stop();
                await _loggingService.LogMainAsync($"Excel export for {tableName} cancelled by user after {stopwatch.Elapsed.TotalSeconds:F2}s");
                return new ExportResult
                {
                    Success = false,
                    Message = "Export cancelled by user",
                    Format = ExportFormat.Excel,
                    Exception = new OperationCanceledException()
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                await _loggingService.LogErrorAsync($"Failed to export to Excel: {ex.Message}", ex, OperationMode.Export);
                return new ExportResult
                {
                    Success = false,
                    Message = ex.Message,
                    Format = ExportFormat.Excel,
                    Exception = ex
                };
            }
        }

        public async Task<ExportResult> ExportToCsvAsync(
            string tableName, 
            string outputPath, 
            DataTable data,
            OperationMode mode = OperationMode.Export,
            string startPeriod = "",
            string endPeriod = "",
            int tableSequence = 1,
            bool isDirectDownload = false,
            CancellationToken cancellationToken = default)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                var fileName = GenerateFileName(tableName, ExportFormat.CSV, mode, startPeriod, endPeriod, tableSequence, isDirectDownload);
                var fullPath = Path.Combine(outputPath, fileName);
                
                Directory.CreateDirectory(outputPath);

                var config = new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    HasHeaderRecord = true,
                    Delimiter = ",",
                    Quote = '"',
                    Escape = '"',
                    Encoding = Encoding.UTF8
                };

                await using var writer = new StreamWriter(fullPath, false, Encoding.UTF8);
                await using var csv = new CsvWriter(writer, config);

                // Write headers
                foreach (DataColumn column in data.Columns)
                {
                    csv.WriteField(column.ColumnName);
                }
                await csv.NextRecordAsync();

                // Write data rows with cancellation checks every 10000 rows
                int rowCount = 0;
                foreach (DataRow row in data.Rows)
                {
                    // Check cancellation periodically to avoid excessive overhead
                    if (rowCount % 10000 == 0)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                    }

                    for (int i = 0; i < data.Columns.Count; i++)
                    {
                        var value = row[i] == DBNull.Value ? string.Empty : row[i]?.ToString() ?? string.Empty;
                        csv.WriteField(value);
                    }
                    await csv.NextRecordAsync();
                    rowCount++;
                }

                await csv.FlushAsync();
                stopwatch.Stop();

                var fileInfo = new FileInfo(fullPath);
                var result = new ExportResult
                {
                    Success = true,
                    FilePath = fullPath,
                    FileName = fileName,
                    FileSize = fileInfo.Length,
                    RecordsExported = data.Rows.Count,
                    Format = ExportFormat.CSV,
                    ElapsedTime = stopwatch.Elapsed,
                    Message = $"Successfully exported {data.Rows.Count} records to CSV"
                };

                return result;
            }
            catch (OperationCanceledException)
            {
                stopwatch.Stop();
                await _loggingService.LogMainAsync($"CSV export for {tableName} cancelled by user after {stopwatch.Elapsed.TotalSeconds:F2}s");
                return new ExportResult
                {
                    Success = false,
                    Message = "Export cancelled by user",
                    Format = ExportFormat.CSV,
                    Exception = new OperationCanceledException()
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                await _loggingService.LogErrorAsync($"Failed to export to CSV: {ex.Message}", ex, OperationMode.Export);
                return new ExportResult
                {
                    Success = false,
                    Message = ex.Message,
                    Format = ExportFormat.CSV,
                    Exception = ex
                };
            }
        }

        public async Task<ExportResult> ExportToTextAsync(
            string tableName, 
            string outputPath, 
            DataTable data,
            OperationMode mode = OperationMode.Export,
            string startPeriod = "",
            string endPeriod = "",
            int tableSequence = 1,
            bool isDirectDownload = false,
            CancellationToken cancellationToken = default)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                var fileName = GenerateFileName(tableName, ExportFormat.TXT, mode, startPeriod, endPeriod, tableSequence, isDirectDownload);
                var fullPath = Path.Combine(outputPath, fileName);
                
                Directory.CreateDirectory(outputPath);

                var sb = new StringBuilder();
                
                // Add headers
                var headers = data.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToList();
                sb.AppendLine(string.Join("\t", headers));
                
                // Add data rows with cancellation checks every 10000 rows
                int rowCount = 0;
                foreach (DataRow row in data.Rows)
                {
                    // Check cancellation periodically to avoid excessive overhead
                    if (rowCount % 10000 == 0)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                    }

                    var values = new List<string>();
                    for (int i = 0; i < data.Columns.Count; i++)
                    {
                        var value = row[i] == DBNull.Value ? string.Empty : row[i]?.ToString() ?? string.Empty;
                        values.Add(value);
                    }
                    sb.AppendLine(string.Join("\t", values));
                    rowCount++;
                }

                await File.WriteAllTextAsync(fullPath, sb.ToString(), Encoding.UTF8);
                stopwatch.Stop();

                var fileInfo = new FileInfo(fullPath);
                var result = new ExportResult
                {
                    Success = true,
                    FilePath = fullPath,
                    FileName = fileName,
                    FileSize = fileInfo.Length,
                    RecordsExported = data.Rows.Count,
                    Format = ExportFormat.TXT,
                    ElapsedTime = stopwatch.Elapsed,
                    Message = $"Successfully exported {data.Rows.Count} records to Text"
                };

                return result;
            }
            catch (OperationCanceledException)
            {
                stopwatch.Stop();
                await _loggingService.LogMainAsync($"Text export for {tableName} cancelled by user after {stopwatch.Elapsed.TotalSeconds:F2}s");
                return new ExportResult
                {
                    Success = false,
                    Message = "Export cancelled by user",
                    Format = ExportFormat.TXT,
                    Exception = new OperationCanceledException()
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                await _loggingService.LogErrorAsync($"Failed to export to Text: {ex.Message}", ex, OperationMode.Export);
                return new ExportResult
                {
                    Success = false,
                    Message = ex.Message,
                    Format = ExportFormat.TXT,
                    Exception = ex
                };
            }
        }

        public async Task<List<ExportResult>> ExportAllTablesAsync(
            List<string> tableNames, 
            ExportFormat format, 
            string outputDirectory, 
            IDatabaseService? databaseService = null,
            string startPeriod = "",
            string endPeriod = "",
            OperationMode mode = OperationMode.Export,
            CancellationToken cancellationToken = default,
            Func<string, Task<bool>>? zeroRecordPromptFunc = null,
            bool isDirectDownload = false)
        {
            // Generate correlation ID for this export batch operation
            var correlationId = CorrelationIdGenerator.GenerateNewCorrelationId("EXPORT");
            
            var results = new List<ExportResult>();
            int tableIndex = 0;
            
            try
            {
                // Log phase header with visual separators
                await _loggingService.LogPhaseAsync("WORKFLOW EXECUTION");
                await _loggingService.LogSummaryAsync("Operation Mode", mode.ToString());
                await _loggingService.LogSummaryAsync("Export Format", format.ToString());
                await _loggingService.LogSummaryAsync("Tables to Export", tableNames.Count.ToString());
                
                // Process tables sequentially to avoid excessive memory usage
                // For production: could implement semaphore-controlled parallel processing
                foreach (var tableName in tableNames)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    tableIndex++;
                    try
                    {
                        // Subphase header
                        if (tableIndex == 1)
                        {
                            await _loggingService.LogEmptyLineAsync();
                            await _loggingService.LogSubphaseAsync("DATA EXPORT");
                        }
                        
                        await _loggingService.LogBatchOperationAsync(tableIndex, tableNames.Count, tableName);
                        
                        DataTable? data = null;
                        var tableStartTime = DateTime.UtcNow;
                        
                        // Query table data if database service is provided
                        if (databaseService != null)
                        {
                            var queryStartTime = DateTime.UtcNow;
                            
                            // Query data - DatabaseService handles streaming internally
                            data = await databaseService.QueryTableAsync(tableName, cancellationToken);
                            
                            var queryDuration = LogMessageTemplates.FormatDuration(DateTime.UtcNow - queryStartTime);
                            await _loggingService.LogHierarchyItemAsync(
                                $"Query: {queryDuration} → {LogMessageTemplates.FormatNumber(data.Rows.Count)} records loaded", 
                                isLast: false);
                            
                            // Check for zero records and prompt user if needed
                            if (data.Rows.Count == 0 && zeroRecordPromptFunc != null)
                            {
                                await _loggingService.LogMainAsync($"Table {tableName} has zero records. Prompting user...");
                                bool generateReport = await zeroRecordPromptFunc(tableName);
                                
                                if (!generateReport)
                                {
                                    // User chose to skip
                                    await _loggingService.LogMainAsync($"Skipping table {tableName} (user choice)");
                                    results.Add(new ExportResult
                                    {
                                        Success = true,
                                        Message = "Skipped (zero records)",
                                        Format = format,
                                        RecordsExported = 0,
                                        FileName = $"{tableName}_skipped"
                                    });
                                    continue; // Skip to next table
                                }
                                // If generateReport is true, continue with export below
                            }
                        }
                        else
                        {
                            // Create empty data table as fallback
                            data = new DataTable(tableName);
                            await _loggingService.LogMainAsync($"No database service provided for table {tableName}, creating empty export");
                        }
                        
                        var result = format switch
                        {
                            ExportFormat.Excel => await ExportToExcelAsync(tableName, outputDirectory, data, mode, startPeriod, endPeriod, tableIndex, isDirectDownload, cancellationToken),
                            ExportFormat.CSV => await ExportToCsvAsync(tableName, outputDirectory, data, mode, startPeriod, endPeriod, tableIndex, isDirectDownload, cancellationToken),
                            ExportFormat.TXT => await ExportToTextAsync(tableName, outputDirectory, data, mode, startPeriod, endPeriod, tableIndex, isDirectDownload, cancellationToken),
                            _ => new ExportResult { Success = false, Message = $"Unsupported format: {format}" }
                        };
                        
                        results.Add(result);
                        
                        // Log export result
                        if (result.Success)
                        {
                            var exportDuration = LogMessageTemplates.FormatDuration(DateTime.UtcNow - tableStartTime);
                            var fileSize = LogMessageTemplates.FormatFileSize(result.FileSize);
                            await _loggingService.LogHierarchyItemAsync(
                                $"Export: {exportDuration} → {result.FileName} ({fileSize})",
                                isLast: tableIndex == tableNames.Count);
                        }
                        else
                        {
                            await _loggingService.LogMainAsync($"Export failed for {tableName}: {result.Message}");
                        }
                        
                        // Clean up DataTable resources
                        if (data != null)
                        {
                            data.Clear();
                            data.Dispose();
                        }
                        
                        // Allow natural garbage collection (removing forced GC for 5-10% performance improvement)
                    }
                    catch (Exception ex)
                    {
                        await _loggingService.LogErrorAsync($"Failed to export table {tableName}: {ex.Message}", ex);
                        results.Add(new ExportResult
                        {
                            Success = false,
                            Message = ex.Message,
                            Format = format,
                            Exception = ex
                        });
                    }
                }
                
                // Log completion with summary
                await _loggingService.LogEmptyLineAsync();
                await _loggingService.LogCompletionAsync("WORKFLOW COMPLETE");
                await _loggingService.LogMainAsync(LogFormattingHelpers.MinorSeparator);
                
                var successCount = results.Count(r => r.Success);
                var totalRecords = results.Sum(r => r.RecordsExported);
                var totalSize = LogMessageTemplates.FormatFileSize(results.Sum(r => r.FileSize));
                
                await _loggingService.LogSummaryAsync("✓ Results", $"{successCount}/{tableNames.Count} tables exported");
                await _loggingService.LogSummaryAsync("✓ Total Records", $"{LogMessageTemplates.FormatNumber(totalRecords)} ({totalSize})");
                await _loggingService.LogSummaryAsync("✓ Output Directory", outputDirectory);
                await _loggingService.LogMainAsync(LogFormattingHelpers.MajorSeparator);
            }
            catch (OperationCanceledException)
            {
                // Propagate cancellation exception so caller can handle it
                CorrelationIdGenerator.CompleteCorrelationId(correlationId, false);
                await _loggingService.LogMainAsync($"Export operation cancelled by user");
                throw;
            }
            finally
            {
                // Mark correlation ID as completed
                CorrelationIdGenerator.CompleteCorrelationId(correlationId, results.All(r => r.Success));
            }
            
            return results;
        }

        public string GenerateFileName(string tableName, ExportFormat format, OperationMode mode, string startPeriod = "", string endPeriod = "", int tableSequence = 1, bool isDirectDownload = false)
        {
            var extension = format switch
            {
                ExportFormat.Excel => "xlsx",
                ExportFormat.CSV => "csv",
                ExportFormat.TXT => "txt",
                ExportFormat.JSON => "json",
                ExportFormat.XML => "xml",
                _ => "dat"
            };
            
            // Direct downloads use tablename_timestamp format
            if (isDirectDownload)
            {
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                return $"{tableName}_{timestamp}.{extension}";
            }
            
            // Batch operations use the existing format
            var modePrefix = mode == OperationMode.Export ? "EX" : "IM";
            
            // Generate period string from parameters (e.g., JAN25_01-20)
            string periodStr = "";
            if (!string.IsNullOrEmpty(startPeriod) && !string.IsNullOrEmpty(endPeriod))
            {
                periodStr = GeneratePeriodString(startPeriod, endPeriod);
            }
            else
            {
                // Fallback to timestamp if no parameters
                periodStr = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            }
            
            // Format: EX_JAN25_01-20_1.xlsx (or IM_JAN25_01-20_2.xlsx)
            return $"{modePrefix}_{periodStr}_{tableSequence}.{extension}";
        }

        private string GeneratePeriodString(string startPeriod, string endPeriod)
        {
            try
            {
                // Parse YYYYMMDD format (e.g., 20250120 -> JAN25_01-20)
                if (startPeriod.Length == 8 && endPeriod.Length == 8)
                {
                    var startYear = startPeriod.Substring(2, 2); // 25
                    var startMonth = startPeriod.Substring(4, 2); // 01
                    var startDay = startPeriod.Substring(6, 2); // 20
                    
                    var endDay = endPeriod.Substring(6, 2); // 10
                    
                    var monthName = int.Parse(startMonth) switch
                    {
                        1 => "JAN",
                        2 => "FEB",
                        3 => "MAR",
                        4 => "APR",
                        5 => "MAY",
                        6 => "JUN",
                        7 => "JUL",
                        8 => "AUG",
                        9 => "SEP",
                        10 => "OCT",
                        11 => "NOV",
                        12 => "DEC",
                        _ => "UNK"
                    };
                    
                    return $"{monthName}{startYear}_{startDay}-{endDay}";
                }
            }
            catch
            {
                // Fallback to timestamp on parse error
            }
            
            return DateTime.Now.ToString("yyyyMMdd_HHmmss");
        }

        public void Dispose()
        {
            // No unmanaged resources to dispose in this implementation
        }
    }
}