using Microsoft.Data.SqlClient;
using System.Data;
using TradeDataStudio.Core.Interfaces;
using TradeDataStudio.Core.Models;

namespace TradeDataStudio.Core.Services
{
    public class DatabaseService : IDatabaseService
    {
        private readonly IConfigurationService _configService;
        private readonly ILoggingService _loggingService;
        
        public DatabaseService(IConfigurationService configService, ILoggingService loggingService)
        {
            _configService = configService ?? throw new ArgumentNullException(nameof(configService));
            _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
        }

        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                var dbConfig = await _configService.GetDatabaseConfigurationAsync();
                
                // Validate configuration first
                if (string.IsNullOrWhiteSpace(dbConfig.Server))
                {
                    await _loggingService.LogErrorAsync("Database server is not configured", null);
                    return false;
                }
                
                if (string.IsNullOrWhiteSpace(dbConfig.Database))
                {
                    await _loggingService.LogErrorAsync("Database name is not configured", null);
                    return false;
                }
                
                var connectionString = dbConfig.ConnectionString;
                await _loggingService.LogMainAsync($"Testing connection with: Server={dbConfig.Server}, Database={dbConfig.Database}, WindowsAuth={dbConfig.UseWindowsAuthentication}");
                
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();
                await _loggingService.LogMainAsync("Database connection test successful");
                return true;
            }
            catch (SqlException sqlEx)
            {
                await _loggingService.LogErrorAsync($"SQL connection failed: {sqlEx.Message} (Number: {sqlEx.Number})", sqlEx);
                return false;
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync($"Database connection test failed: {ex.Message}", ex);
                return false;
            }
        }

        public async Task<ConnectionTestResult> TestConnectionDetailedAsync()
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                var dbConfig = await _configService.GetDatabaseConfigurationAsync();
                
                // Validate configuration first
                if (string.IsNullOrWhiteSpace(dbConfig.Server))
                {
                    stopwatch.Stop();
                    return new ConnectionTestResult
                    {
                        Success = false,
                        Message = "Server name is required",
                        ErrorDetails = "Database server configuration is missing",
                        TestDuration = stopwatch.Elapsed
                    };
                }
                
                if (string.IsNullOrWhiteSpace(dbConfig.Database))
                {
                    stopwatch.Stop();
                    return new ConnectionTestResult
                    {
                        Success = false,
                        Message = "Database name is required",
                        ErrorDetails = "Database name configuration is missing",
                        TestDuration = stopwatch.Elapsed
                    };
                }
                
                var connectionString = dbConfig.ConnectionString;
                await _loggingService.LogMainAsync($"Testing connection with: Server={dbConfig.Server}, Database={dbConfig.Database}, WindowsAuth={dbConfig.UseWindowsAuthentication}");
                
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();
                stopwatch.Stop();
                
                await _loggingService.LogMainAsync("Database connection test successful");
                return new ConnectionTestResult
                {
                    Success = true,
                    Message = $"Successfully connected to {dbConfig.Server}\\{dbConfig.Database}",
                    TestDuration = stopwatch.Elapsed
                };
            }
            catch (SqlException sqlEx)
            {
                stopwatch.Stop();
                var errorMessage = GetFriendlyErrorMessage(sqlEx);
                await _loggingService.LogErrorAsync($"SQL connection failed: {sqlEx.Message} (Number: {sqlEx.Number})", sqlEx);
                return new ConnectionTestResult
                {
                    Success = false,
                    Message = errorMessage,
                    ErrorDetails = sqlEx.Message,
                    SqlErrorNumber = sqlEx.Number,
                    TestDuration = stopwatch.Elapsed
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                await _loggingService.LogErrorAsync($"Database connection test failed: {ex.Message}", ex);
                return new ConnectionTestResult
                {
                    Success = false,
                    Message = "Connection failed",
                    ErrorDetails = ex.Message,
                    TestDuration = stopwatch.Elapsed
                };
            }
        }

        private string GetFriendlyErrorMessage(SqlException sqlEx)
        {
            return sqlEx.Number switch
            {
                2 => "Server not found. Please check the server name and ensure it's accessible.",
                4060 => "Database not found. Please verify the database name is correct.",
                18456 => "Login failed. Please check your credentials or Windows authentication settings.",
                -2 => "Connection timeout. The server may be busy or unreachable.",
                53 => "Network path not found. Please check server name and network connectivity.",
                _ => $"Database connection error: {sqlEx.Message}"
            };
        }

        public async Task<ExecutionResult> ExecuteStoredProcedureAsync(string procedureName, Dictionary<string, object> parameters, CancellationToken cancellationToken = default)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                var dbConfig = await _configService.GetDatabaseConfigurationAsync();
                using var connection = new SqlConnection(dbConfig.ConnectionString);
                using var command = new SqlCommand(procedureName, connection)
                {
                    CommandType = CommandType.StoredProcedure,
                    CommandTimeout = 300 // 5 minutes timeout
                };

                // Add parameters
                Console.WriteLine($"Adding {parameters.Count} parameters to stored procedure '{procedureName}':");
                foreach (var param in parameters)
                {
                    var paramName = param.Key.StartsWith("@") ? param.Key : $"@{param.Key}";
                    Console.WriteLine($"  Parameter: {paramName} = {param.Value} (Type: {param.Value?.GetType().Name})");
                    command.Parameters.AddWithValue(paramName, param.Value ?? DBNull.Value);
                }

                await connection.OpenAsync(cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                
                var recordsAffected = await command.ExecuteNonQueryAsync(cancellationToken);
                stopwatch.Stop();

                var result = new ExecutionResult
                {
                    Success = true,
                    Message = $"Stored procedure '{procedureName}' executed successfully",
                    RecordsAffected = recordsAffected,
                    ExecutionTime = stopwatch.Elapsed
                };

                await _loggingService.LogSuccessAsync($"Stored procedure '{procedureName}' executed successfully. {recordsAffected} records affected.", OperationMode.Export);
                return result;
            }
            catch (OperationCanceledException)
            {
                stopwatch.Stop();
                await _loggingService.LogMainAsync($"Stored procedure '{procedureName}' cancelled by user after {stopwatch.Elapsed.TotalSeconds:F2}s");
                return new ExecutionResult
                {
                    Success = false,
                    Message = "Operation cancelled by user",
                    ExecutionTime = stopwatch.Elapsed,
                    Exception = new OperationCanceledException()
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                await _loggingService.LogErrorAsync($"Failed to execute stored procedure '{procedureName}': {ex.Message}", ex, OperationMode.Export);
                return new ExecutionResult
                {
                    Success = false,
                    Message = ex.Message,
                    ExecutionTime = stopwatch.Elapsed,
                    Exception = ex
                };
            }
        }

        public async Task<DataTable> QueryTableAsync(string tableName, CancellationToken cancellationToken = default)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                await _loggingService.LogMainAsync($"Querying table: {tableName}...");
                
                var dbConfig = await _configService.GetDatabaseConfigurationAsync();
                var query = $"SELECT * FROM {tableName}";
                using var connection = new SqlConnection(dbConfig.ConnectionString);
                using var command = new SqlCommand(query, connection)
                {
                    CommandTimeout = 600 // 10 minutes for large tables
                };

                var dataTable = new DataTable(tableName);
                await connection.OpenAsync(cancellationToken);
                
                cancellationToken.ThrowIfCancellationRequested();
                
                await _loggingService.LogMainAsync($"Connection opened, fetching data from {tableName}...");
                
                // Use SqlDataReader with CommandBehavior.SequentialAccess for better memory efficiency
                // This allows streaming of large data without loading everything into memory at once
                using var reader = await command.ExecuteReaderAsync(CommandBehavior.Default, cancellationToken);
                
                // Load schema first for faster DataTable initialization
                dataTable.Load(reader);
                
                stopwatch.Stop();
                await _loggingService.LogMainAsync($"Query completed: {dataTable.Rows.Count:N0} rows retrieved from {tableName} in {stopwatch.Elapsed.TotalSeconds:F2}s");
                
                return dataTable;
            }
            catch (OperationCanceledException)
            {
                stopwatch.Stop();
                await _loggingService.LogMainAsync($"Query for table {tableName} cancelled by user after {stopwatch.Elapsed.TotalSeconds:F2}s");
                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                await _loggingService.LogErrorAsync($"Failed to query table {tableName}: {ex.Message}", ex);
                throw;
            }
        }

        public async Task<List<string>> GetAvailableTablesAsync(OperationMode mode)
        {
            try
            {
                var tables = await _configService.GetTablesAsync(mode);
                return tables.Select(t => t.Name).ToList();
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync($"Failed to get available tables for mode {mode}: {ex.Message}", ex);
                throw;
            }
        }

        public async Task<int> GetTableRecordCountAsync(string tableName)
        {
            try
            {
                var dbConfig = await _configService.GetDatabaseConfigurationAsync();
                var query = $"SELECT COUNT(*) FROM {tableName}";
                using var connection = new SqlConnection(dbConfig.ConnectionString);
                using var command = new SqlCommand(query, connection);

                await connection.OpenAsync();
                var count = (int)(await command.ExecuteScalarAsync() ?? 0);

                await _loggingService.LogMainAsync($"Table '{tableName}' contains {count} records");
                return count;
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync($"Failed to get record count for table '{tableName}': {ex.Message}", ex);
                throw;
            }
        }

        public void Dispose()
        {
            // No unmanaged resources to dispose in this implementation
        }

        private static SqlDbType GetSqlDbType(object? value)
        {
            return value switch
            {
                int => SqlDbType.Int,
                long => SqlDbType.BigInt,
                short => SqlDbType.SmallInt,
                decimal => SqlDbType.Decimal,
                double => SqlDbType.Float,
                float => SqlDbType.Real,
                bool => SqlDbType.Bit,
                DateTime => SqlDbType.DateTime,
                byte[] => SqlDbType.VarBinary,
                _ => SqlDbType.VarChar
            };
        }
    }
}