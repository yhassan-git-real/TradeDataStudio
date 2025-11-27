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
            int tableSequence = 1)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                var fileName = GenerateFileName(tableName, ExportFormat.Excel, mode, startPeriod, endPeriod, tableSequence);
                var fullPath = Path.Combine(outputPath, fileName);
                
                Directory.CreateDirectory(outputPath);
                await _loggingService.LogMainAsync($"Starting Excel export for {tableName} with {data.Rows.Count:N0} rows...");

                // Run export in background task to prevent UI blocking
                await Task.Run(async () =>
                {
                    using var package = new ExcelPackage();
                    var worksheet = package.Workbook.Worksheets.Add(tableName);

                    // Write headers with data type information
                    for (int col = 0; col < data.Columns.Count; col++)
                    {
                        worksheet.Cells[1, col + 1].Value = data.Columns[col].ColumnName;
                        worksheet.Cells[1, col + 1].Style.Font.Bold = true;
                        worksheet.Cells[1, col + 1].Style.Font.Size = 11;
                    }

                    int totalRows = data.Rows.Count;
                    int totalBatches = (int)Math.Ceiling((double)totalRows / _batchSize);
                    
                    // EPPlus Excel limit is 1,048,576 rows (including header)
                    if (totalRows > 1048575)
                    {
                        throw new InvalidOperationException($"Excel format supports maximum 1,048,575 data rows. Current data has {totalRows:N0} rows. Please use CSV format or filter the data.");
                    }
                    
                    // Process data in batches
                    for (int startRow = 0; startRow < totalRows; startRow += _batchSize)
                    {
                        int endRow = Math.Min(startRow + _batchSize, totalRows);
                        int currentBatch = (startRow / _batchSize) + 1;
                        
                        await _loggingService.LogMainAsync($"Processing batch {currentBatch}/{totalBatches} ({startRow + 1:N0}-{endRow:N0} of {totalRows:N0})");
                        
                        // Write batch data with proper type handling
                        for (int row = startRow; row < endRow; row++)
                        {
                            for (int col = 0; col < data.Columns.Count; col++)
                            {
                                var cellValue = data.Rows[row][col];
                                var excelRow = row + 2; // +2 because Excel is 1-indexed and row 1 is header
                                var excelCol = col + 1;
                                
                                if (cellValue == DBNull.Value || cellValue == null)
                                {
                                    worksheet.Cells[excelRow, excelCol].Value = null;
                                }
                                else
                                {
                                    // Handle different data types correctly
                                    var dataType = data.Columns[col].DataType;
                                    
                                    if (dataType == typeof(DateTime))
                                    {
                                        worksheet.Cells[excelRow, excelCol].Value = (DateTime)cellValue;
                                        worksheet.Cells[excelRow, excelCol].Style.Numberformat.Format = "yyyy-mm-dd hh:mm:ss";
                                    }
                                    else if (dataType == typeof(int) || dataType == typeof(long) || 
                                             dataType == typeof(short) || dataType == typeof(byte))
                                    {
                                        worksheet.Cells[excelRow, excelCol].Value = Convert.ToInt64(cellValue);
                                    }
                                    else if (dataType == typeof(decimal) || dataType == typeof(double) || 
                                             dataType == typeof(float))
                                    {
                                        worksheet.Cells[excelRow, excelCol].Value = Convert.ToDouble(cellValue);
                                        worksheet.Cells[excelRow, excelCol].Style.Numberformat.Format = "#,##0.00";
                                    }
                                    else if (dataType == typeof(bool))
                                    {
                                        worksheet.Cells[excelRow, excelCol].Value = (bool)cellValue;
                                    }
                                    else
                                    {
                                        // String and other types
                                        worksheet.Cells[excelRow, excelCol].Value = cellValue.ToString();
                                    }
                                }
                            }
                        }
                        
                        // Allow other tasks to run
                        await Task.Yield();
                    }

                    await _loggingService.LogMainAsync($"Writing Excel file to disk: {fileName}");
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

                await _loggingService.LogSuccessAsync($"Excel export completed: {fileName} ({data.Rows.Count:N0} rows, {fileInfo.Length / 1024.0 / 1024.0:F2} MB, {stopwatch.Elapsed.TotalSeconds:F2}s)", OperationMode.Export);
                return result;
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
            int tableSequence = 1)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                var fileName = GenerateFileName(tableName, ExportFormat.CSV, mode, startPeriod, endPeriod, tableSequence);
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

                // Write data rows
                foreach (DataRow row in data.Rows)
                {
                    for (int i = 0; i < data.Columns.Count; i++)
                    {
                        var value = row[i] == DBNull.Value ? string.Empty : row[i]?.ToString() ?? string.Empty;
                        csv.WriteField(value);
                    }
                    await csv.NextRecordAsync();
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

                await _loggingService.LogSuccessAsync($"CSV export completed: {fileName}", OperationMode.Export);
                return result;
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
            int tableSequence = 1)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                var fileName = GenerateFileName(tableName, ExportFormat.TXT, mode, startPeriod, endPeriod, tableSequence);
                var fullPath = Path.Combine(outputPath, fileName);
                
                Directory.CreateDirectory(outputPath);

                var sb = new StringBuilder();
                
                // Add headers
                var headers = data.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToList();
                sb.AppendLine(string.Join("\t", headers));
                
                // Add data rows
                foreach (DataRow row in data.Rows)
                {
                    var values = new List<string>();
                    for (int i = 0; i < data.Columns.Count; i++)
                    {
                        var value = row[i] == DBNull.Value ? string.Empty : row[i]?.ToString() ?? string.Empty;
                        values.Add(value);
                    }
                    sb.AppendLine(string.Join("\t", values));
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

                await _loggingService.LogSuccessAsync($"Text export completed: {fileName}", OperationMode.Export);
                return result;
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
            Func<string, Task<bool>>? zeroRecordPromptFunc = null)
        {
            var results = new List<ExportResult>();
            int tableIndex = 0;
            
            try
            {
                foreach (var tableName in tableNames)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    tableIndex++;
                    try
                    {
                        await _loggingService.LogMainAsync($"[{tableIndex}/{tableNames.Count}] Starting export for table: {tableName}");
                        
                        DataTable? data = null;
                        
                        // Query table data if database service is provided - run in background
                        if (databaseService != null)
                        {
                            await _loggingService.LogMainAsync($"Querying data from table: {tableName}...");
                            
                            // Run data retrieval in background task
                            data = await Task.Run(async () => await databaseService.QueryTableAsync(tableName));
                            
                            await _loggingService.LogMainAsync($"Retrieved {data.Rows.Count:N0} rows from {tableName}");
                            
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
                            ExportFormat.Excel => await ExportToExcelAsync(tableName, outputDirectory, data, mode, startPeriod, endPeriod, tableIndex),
                            ExportFormat.CSV => await ExportToCsvAsync(tableName, outputDirectory, data, mode, startPeriod, endPeriod, tableIndex),
                            ExportFormat.TXT => await ExportToTextAsync(tableName, outputDirectory, data, mode, startPeriod, endPeriod, tableIndex),
                            _ => new ExportResult { Success = false, Message = $"Unsupported format: {format}" }
                        };
                        
                        results.Add(result);
                        
                        // Aggressive memory cleanup after each table export
                        if (data != null)
                        {
                            data.Clear();
                            data.Dispose();
                        }
                        
                        // Force garbage collection between tables
                        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true);
                        GC.WaitForPendingFinalizers();
                        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true);
                        
                        await _loggingService.LogMainAsync($"[{tableIndex}/{tableNames.Count}] Completed export for {tableName}, memory cleaned");
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
            }
            finally
            {
                // Ensure any database connections are properly released
                // Force final cleanup
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
            
            return results;
        }

        public string GenerateFileName(string tableName, ExportFormat format, OperationMode mode, string startPeriod = "", string endPeriod = "", int tableSequence = 1)
        {
            var modePrefix = mode == OperationMode.Export ? "EX" : "IM";
            var extension = format switch
            {
                ExportFormat.Excel => "xlsx",
                ExportFormat.CSV => "csv",
                ExportFormat.TXT => "txt",
                ExportFormat.JSON => "json",
                ExportFormat.XML => "xml",
                _ => "dat"
            };
            
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