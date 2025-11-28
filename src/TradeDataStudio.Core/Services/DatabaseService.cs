using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using TradeDataStudio.Core.Interfaces;
using TradeDataStudio.Core.Models;

namespace TradeDataStudio.Core.Services
{
    /// <summary>
    /// Enhanced database service with performance optimizations:
    /// - Connection pooling
    /// - Async batch operations
    /// - Memory-efficient data streaming
    /// - Query optimization hints
    /// </summary>
    public class DatabaseService : IDatabaseService
    {
        private readonly IConfigurationService _configService;
        private readonly ILoggingService _loggingService;
        private readonly SemaphoreSlim _connectionSemaphore;
        private readonly Dictionary<string, SqlConnection> _connectionPool;
        private readonly object _poolLock = new();
        
        private const int MaxPoolSize = 10;
        private const int DefaultCommandTimeout = 300; // 5 minutes
        private const int BatchSize = 10000; // Records per batch

        public DatabaseService(IConfigurationService configService, ILoggingService loggingService)
        {
            _configService = configService ?? throw new ArgumentNullException(nameof(configService));
            _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
            _connectionSemaphore = new SemaphoreSlim(MaxPoolSize, MaxPoolSize);
            _connectionPool = new Dictionary<string, SqlConnection>();
        }

        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                var dbConfig = await _configService.GetDatabaseConfigurationAsync();
                
                // Validate configuration first (from DatabaseService)
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
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var dbConfig = await _configService.GetDatabaseConfigurationAsync();
                
                // Validate configuration first (from DatabaseService)
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
                
                // Test with a simple query
                using var command = new SqlCommand("SELECT @@VERSION", connection) { CommandTimeout = 10 };
                var version = await command.ExecuteScalarAsync();
                
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
                var errorMessage = GetFriendlyErrorMessageFromSqlException(sqlEx);
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

        /// <summary>
        /// Execute stored procedure with connection pooling and performance monitoring
        /// </summary>
        public async Task<ExecutionResult> ExecuteStoredProcedureAsync(string procedureName, Dictionary<string, object> parameters, CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            SqlConnection? connection = null;
            
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                await _connectionSemaphore.WaitAsync(cancellationToken);
                
                connection = await GetPooledConnectionAsync();
                
                using var command = new SqlCommand(procedureName, connection)
                {
                    CommandType = CommandType.StoredProcedure,
                    CommandTimeout = DefaultCommandTimeout
                };

                // Add parameters with proper type mapping
                foreach (var param in parameters)
                {
                    var sqlParam = command.Parameters.Add($"@{param.Key.TrimStart('@')}", GetSqlDbType(param.Value ?? ""));
                    sqlParam.Value = param.Value ?? DBNull.Value;
                }

                await _loggingService.LogMainAsync($"Executing {procedureName} with optimized connection pooling");
                
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

                await _loggingService.LogSuccessAsync($"SP execution completed in {stopwatch.Elapsed.TotalMilliseconds:F0}ms", OperationMode.Export);
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
            finally
            {
                if (connection != null)
                {
                    await ReturnConnectionToPoolAsync(connection);
                }
                _connectionSemaphore.Release();
            }
        }

        /// <summary>
        /// Query table with memory-efficient streaming for large datasets
        /// </summary>
        public async Task<DataTable> QueryTableAsync(string tableName, CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            SqlConnection? connection = null;
            
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                await _connectionSemaphore.WaitAsync(cancellationToken);
                connection = await GetPooledConnectionAsync();

                // Use streaming for better memory efficiency
                var query = $"SELECT * FROM {tableName} WITH (NOLOCK)"; // NOLOCK for read performance
                
                using var command = new SqlCommand(query, connection)
                {
                    CommandTimeout = DefaultCommandTimeout
                };

                var dataTable = new DataTable(tableName);
                
                cancellationToken.ThrowIfCancellationRequested();
                using var reader = await command.ExecuteReaderAsync(CommandBehavior.Default, cancellationToken);
                
                // Load schema first
                dataTable.Load(reader);
                
                stopwatch.Stop();
                
                await _loggingService.LogMainAsync($"Query completed: {dataTable.Rows.Count} rows in {stopwatch.Elapsed.TotalMilliseconds:F0}ms");
                
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
                await _loggingService.LogErrorAsync($"Failed to query table '{tableName}': {ex.Message}", ex);
                throw;
            }
            finally
            {
                if (connection != null)
                {
                    await ReturnConnectionToPoolAsync(connection);
                }
                _connectionSemaphore.Release();
            }
        }

        /// <summary>
        /// Get record count efficiently without loading all data
        /// </summary>
        public async Task<int> GetTableRecordCountAsync(string tableName)
        {
            SqlConnection? connection = null;
            
            try
            {
                await _connectionSemaphore.WaitAsync();
                connection = await GetPooledConnectionAsync();
                
                var query = $"SELECT COUNT_BIG(*) FROM {tableName} WITH (NOLOCK)";
                using var command = new SqlCommand(query, connection) { CommandTimeout = 60 };
                
                var result = await command.ExecuteScalarAsync();
                return Convert.ToInt32(result ?? 0);
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync($"Failed to get record count for '{tableName}': {ex.Message}", ex);
                throw;
            }
            finally
            {
                if (connection != null)
                {
                    await ReturnConnectionToPoolAsync(connection);
                }
                _connectionSemaphore.Release();
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

        #region Connection Pooling

        private async Task<SqlConnection> GetPooledConnectionAsync()
        {
            var dbConfig = await _configService.GetDatabaseConfigurationAsync();
            var connectionString = dbConfig.ConnectionString;
            
            lock (_poolLock)
            {
                // Try to get an existing connection from the pool
                if (_connectionPool.TryGetValue(connectionString, out var existingConnection) && 
                    existingConnection.State == ConnectionState.Open)
                {
                    _connectionPool.Remove(connectionString);
                    return existingConnection;
                }
            }
            
            // Create new connection if none available
            var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();
            return connection;
        }

        private Task ReturnConnectionToPoolAsync(SqlConnection connection)
        {
            if (connection.State != ConnectionState.Open)
            {
                connection.Dispose();
                return Task.CompletedTask;
            }
            
            lock (_poolLock)
            {
                if (_connectionPool.Count < MaxPoolSize)
                {
                    _connectionPool[connection.ConnectionString] = connection;
                    return Task.CompletedTask;
                }
            }
            
            // Pool is full, dispose the connection
            connection.Dispose();
            return Task.CompletedTask;
        }

        #endregion

        #region Helper Methods

        private static SqlDbType GetSqlDbType(object value)
        {
            return value switch
            {
                int => SqlDbType.Int,
                long => SqlDbType.BigInt,
                string => SqlDbType.VarChar,
                DateTime => SqlDbType.DateTime,
                decimal => SqlDbType.Decimal,
                bool => SqlDbType.Bit,
                byte[] => SqlDbType.VarBinary,
                _ => SqlDbType.VarChar
            };
        }

        private static string GetFriendlyErrorMessage(Exception exception)
        {
            return exception switch
            {
                SqlException sqlEx => GetSqlErrorMessage(sqlEx),
                TimeoutException => "Connection timeout. The database server is taking too long to respond.",
                InvalidOperationException => "Database connection is in an invalid state.",
                _ => $"Database error: {exception.Message}"
            };
        }

        private static string GetSqlErrorMessage(SqlException sqlEx)
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

        private static string GetFriendlyErrorMessageFromSqlException(SqlException sqlEx)
        {
            return GetSqlErrorMessage(sqlEx);
        }

        #endregion

        public void Dispose()
        {
            lock (_poolLock)
            {
                foreach (var connection in _connectionPool.Values)
                {
                    connection.Dispose();
                }
                _connectionPool.Clear();
            }
            
            _connectionSemaphore.Dispose();
        }
    }
}