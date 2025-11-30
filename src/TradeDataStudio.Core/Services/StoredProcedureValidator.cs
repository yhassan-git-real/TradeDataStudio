using Microsoft.Data.SqlClient;
using TradeDataStudio.Core.Interfaces;
using TradeDataStudio.Core.Models;

namespace TradeDataStudio.Core.Services
{
    /// <summary>
    /// Service for validating stored procedure existence in the database.
    /// Queries SQL Server metadata to verify if a stored procedure exists before operations.
    /// </summary>
    public class StoredProcedureValidator : IStoredProcedureValidator
    {
        private readonly IConfigurationService _configService;
        private readonly ILoggingService _loggingService;

        public StoredProcedureValidator(
            IConfigurationService configService,
            ILoggingService loggingService)
        {
            _configService = configService ?? throw new ArgumentNullException(nameof(configService));
            _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
        }

        /// <summary>
        /// Validates whether a stored procedure exists in the currently connected database.
        /// </summary>
        /// <param name="storedProcedureName">The name of the stored procedure to validate</param>
        /// <returns>
        /// A StoredProcedureValidationResult containing the validation status and any error messages.
        /// Returns true if the procedure exists, false otherwise.
        /// </returns>
        public async Task<StoredProcedureValidationResult> ValidateStoredProcedureAsync(
            string storedProcedureName)
        {
            try
            {
                // Validate input
                if (string.IsNullOrWhiteSpace(storedProcedureName))
                {
                    await _loggingService.LogErrorAsync(
                        "Stored procedure name cannot be null or empty.", null);
                    return new StoredProcedureValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = "Stored procedure name is required.",
                        ProcedureName = storedProcedureName
                    };
                }

                var dbConfig = await _configService.GetDatabaseConfigurationAsync();

                // Validate database connection is configured
                if (string.IsNullOrWhiteSpace(dbConfig.Server) || 
                    string.IsNullOrWhiteSpace(dbConfig.Database))
                {
                    await _loggingService.LogErrorAsync(
                        "Database connection not properly configured.", null);
                    return new StoredProcedureValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = "Database is not properly configured. Please check connection settings.",
                        ProcedureName = storedProcedureName
                    };
                }

                // Query INFORMATION_SCHEMA to check if procedure exists
                var procedureExists = await CheckProcedureExistsAsync(
                    dbConfig.ConnectionString,
                    storedProcedureName,
                    dbConfig.Database);

                if (procedureExists)
                {
                    await _loggingService.LogExecutionSeparatorAsync();
                    await _loggingService.LogMainAsync(
                        $"Stored procedure '{storedProcedureName}' validated successfully in database '{dbConfig.Database}'.");
                    return new StoredProcedureValidationResult
                    {
                        IsValid = true,
                        ProcedureName = storedProcedureName
                    };
                }
                else
                {
                    await _loggingService.LogErrorAsync(
                        $"Stored procedure '{storedProcedureName}' does not exist in database '{dbConfig.Database}'.", null);
                    return new StoredProcedureValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = "Stored procedure does not exist in the database. Please verify the stored procedure name and ensure it exists in the connected database.",
                        ProcedureName = storedProcedureName
                    };
                }
            }
            catch (SqlException sqlEx)
            {
                await _loggingService.LogErrorAsync(
                    $"SQL error while validating stored procedure '{storedProcedureName}': {sqlEx.Message}", sqlEx);
                return new StoredProcedureValidationResult
                {
                    IsValid = false,
                    ErrorMessage = $"Database error: {sqlEx.Message}",
                    ProcedureName = storedProcedureName
                };
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync(
                    $"Error validating stored procedure '{storedProcedureName}': {ex.Message}", ex);
                return new StoredProcedureValidationResult
                {
                    IsValid = false,
                    ErrorMessage = $"Validation error: {ex.Message}",
                    ProcedureName = storedProcedureName
                };
            }
        }

        /// <summary>
        /// Checks if a stored procedure exists by querying the INFORMATION_SCHEMA.ROUTINES view.
        /// </summary>
        private async Task<bool> CheckProcedureExistsAsync(
            string connectionString,
            string procedureName,
            string databaseName)
        {
            try
            {
                using var connection = new SqlConnection(connectionString);
                const string query = @"
                    SELECT COUNT(1)
                    FROM INFORMATION_SCHEMA.ROUTINES
                    WHERE ROUTINE_TYPE = 'PROCEDURE'
                    AND ROUTINE_NAME = @ProcedureName
                    AND ROUTINE_SCHEMA != 'sys'";

                using var command = new SqlCommand(query, connection)
                {
                    CommandTimeout = 10
                };

                command.Parameters.AddWithValue("@ProcedureName", procedureName);

                await connection.OpenAsync();
                var result = await command.ExecuteScalarAsync();
                var count = (int)(result ?? 0);

                return count > 0;
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync(
                    $"Failed to query INFORMATION_SCHEMA for procedure '{procedureName}': {ex.Message}", ex);
                throw;
            }
        }
    }
}
