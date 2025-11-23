# TradeData Studio Installation Guide

## Prerequisites

### System Requirements
- Windows 10 version 1903 or later (Windows 11 recommended)
- .NET 8.0 Runtime (included in self-contained builds)
- SQL Server 2017 or later (or SQL Server Express)
- 4 GB RAM minimum, 8 GB recommended
- 500 MB available disk space

### Database Setup
1. Ensure SQL Server is running and accessible
2. Create a database for TradeData Studio
3. Run any required table creation scripts
4. Note the connection string for configuration

## Installation Options

### Option 1: Self-Contained Build (Recommended)
1. Download the appropriate build:
   - `Windows-x64`: For 64-bit Windows systems
   - `Windows-x86`: For 32-bit Windows systems
2. Extract the ZIP file to your desired installation directory
3. No additional .NET runtime installation required

### Option 2: Portable Build
1. Download the `Portable` build
2. Ensure .NET 8.0 Runtime is installed on the target machine
3. Extract to desired directory

## Configuration

### Database Connection
1. Navigate to the `config` folder in your installation directory
2. Edit `database.json`:
```json
{
  "ConnectionString": "Server=your-server;Database=your-database;Integrated Security=true;",
  "CommandTimeout": 30,
  "RetryCount": 3,
  "RetryDelay": 1000
}
```

### Application Settings
Edit `appsettings.json` to customize application behavior:
```json
{
  "Logging": {
    "LogLevel": "Information",
    "FileRetentionDays": 30,
    "MaxFileSizeBytes": 10485760
  },
  "Application": {
    "Title": "TradeData Studio",
    "Version": "1.0.0"
  }
}
```

### Export Configuration
- Configure export tables in `export_tables.json`
- Configure stored procedures in `export_procedures.json`
- Configure import settings in `import_tables.json` and `import_procedures.json`

## Running the Application

### First Launch
1. Double-click `TradeDataStudio.Desktop.exe`
2. The application will create necessary directories if they don't exist
3. Check the `logs` folder for any startup issues

### Troubleshooting
- **Connection Issues**: Verify SQL Server is running and connection string is correct
- **Permission Errors**: Ensure the application has write access to logs and exports folders
- **Missing Dependencies**: For portable builds, ensure .NET 8.0 Runtime is installed

## Features

### Export Functions
- **Export to Excel**: Export query results to Excel format
- **Export to CSV**: Export data as comma-separated values
- **Stored Procedure Export**: Export using predefined stored procedures

### Data Management
- **Connection Testing**: Verify database connectivity
- **Log Viewing**: Access application logs for troubleshooting
- **Settings Management**: Configure application behavior

### User Interface
- **Modern Design**: Clean, intuitive interface built with AvaloniaUI
- **Keyboard Shortcuts**: 
  - Ctrl+E: Export to Excel
  - Ctrl+X: Export to CSV
  - Ctrl+S: Execute Stored Procedure
  - Ctrl+T: Test Connection
  - Ctrl+L: View Logs
  - Ctrl+O: Open Settings

## Support

### Log Files
Application logs are stored in the `logs` folder:
- `application.log`: General application events
- `error.log`: Error messages and stack traces
- `debug.log`: Detailed debugging information

### Common Issues
1. **Database Connection Failed**: Check connection string and SQL Server status
2. **Export Errors**: Verify write permissions to exports folder
3. **Performance Issues**: Consider using the OptimizedDatabaseService for large datasets

### Contact
For technical support or bug reports, please include:
- Application version
- Operating system details
- Error messages from log files
- Steps to reproduce the issue