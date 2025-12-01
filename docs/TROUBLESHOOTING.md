<div align="center">

```
╔═══════════════════════════════════════════════════════════════╗
║                                                               ║
║   ████████╗██████╗  ██████╗ ██╗   ██╗██████╗ ██╗             ║
║   ╚══██╔══╝██╔══██╗██╔═══██╗██║   ██║██╔══██╗██║             ║
║      ██║   ██████╔╝██║   ██║██║   ██║██████╔╝██║             ║
║      ██║   ██╔══██╗██║   ██║██║   ██║██╔══██╗██║             ║
║      ██║   ██║  ██║╚██████╔╝╚██████╔╝██████╔╝███████╗        ║
║      ╚═╝   ╚═╝  ╚═╝ ╚═════╝  ╚═════╝ ╚═════╝ ╚══════╝        ║
║                                                               ║
║   ███████╗██╗  ██╗ ██████╗  ██████╗ ████████╗██╗███╗   ██╗  ║
║   ██╔════╝██║  ██║██╔═══██╗██╔═══██╗╚══██╔══╝██║████╗  ██║  ║
║   ███████╗███████║██║   ██║██║   ██║   ██║   ██║██╔██╗ ██║  ║
║   ╚════██║██╔══██║██║   ██║██║   ██║   ██║   ██║██║╚██╗██║  ║
║   ███████║██║  ██║╚██████╔╝╚██████╔╝   ██║   ██║██║ ╚████║  ║
║   ╚══════╝╚═╝  ╚═╝ ╚═════╝  ╚═════╝    ╚═╝   ╚═╝╚═╝  ╚═══╝  ║
║                                                               ║
╚═══════════════════════════════════════════════════════════════╝
```

# Troubleshooting Guide

<p align="center">
  <img src="https://img.shields.io/badge/Version-1.0.0-00E5FF?style=for-the-badge" alt="Version"/>
  <img src="https://img.shields.io/badge/Support-Active-00FF87?style=for-the-badge" alt="Support"/>
  <img src="https://img.shields.io/badge/Updated-December_2025-02111B?style=for-the-badge" alt="Updated"/>
</p>

**Comprehensive troubleshooting guide for common issues and solutions**

</div>

---

## 📑 Table of Contents

1. [Connection Issues](#-connection-issues)
2. [Export Problems](#-export-problems)
3. [Import Failures](#-import-failures)
4. [Performance Issues](#-performance-issues)
5. [Application Errors](#-application-errors)
6. [Configuration Problems](#-configuration-problems)
7. [File System Issues](#-file-system-issues)
8. [Data Quality Issues](#-data-quality-issues)
9. [Diagnostic Tools](#-diagnostic-tools)
10. [Getting Additional Help](#-getting-additional-help)

---

## 🔌 Connection Issues

### Cannot Connect to Database

<div style="background: linear-gradient(135deg, #02111B 0%, #024059 100%); padding: 20px; border-radius: 10px; border-left: 4px solid #FF5555;">

**❌ Symptom:**
```
Error: A network-related or instance-specific error occurred
Unable to connect to SQL Server
Connection timeout expired
```

**🔍 Diagnosis Steps:**

1. **Verify SQL Server is Running**
   ```powershell
   # Check SQL Server service
   Get-Service -Name "MSSQL*" | Select-Object Name, Status
   
   # Expected output:
   # Name                Status
   # ----                ------
   # MSSQLSERVER         Running
   # or
   # MSSQL$INSTANCE      Running
   ```

2. **Test Network Connectivity**
   ```powershell
   # Test connection to server
   Test-NetConnection -ComputerName "SERVER_NAME" -Port 1433
   
   # Expected output:
   # TcpTestSucceeded : True
   ```

3. **Verify Configuration**
   ```powershell
   # Check database.json
   Get-Content config\database.json | ConvertFrom-Json
   ```

**✅ Solutions:**

<details>
<summary><b>Solution 1: Verify Server Name</b></summary>

```json
{
  "server": "CORRECT_SERVER\\INSTANCE_NAME",
  "database": "DATABASE_NAME",
  "useWindowsAuthentication": true
}
```

**Common Mistakes:**
- ❌ `localhost\SQLEXPRESS` (should be `.\\SQLEXPRESS`)
- ❌ `SERVER/INSTANCE` (should use backslash: `SERVER\\INSTANCE`)
- ❌ Missing instance name
- ❌ Using IP when server name required

**Find Your Server Name:**
```sql
-- Run in SSMS
SELECT @@SERVERNAME AS ServerName
```

</details>

<details>
<summary><b>Solution 2: Check SQL Server Configuration</b></summary>

**Enable TCP/IP Protocol:**
1. Open SQL Server Configuration Manager
2. Navigate to: SQL Server Network Configuration → Protocols for [Instance]
3. Enable TCP/IP
4. Restart SQL Server service

**Verify Port:**
```
TCP/IP Properties → IP Addresses → IPAll
TCP Port: 1433 (default)
```

**Restart Service:**
```powershell
Restart-Service -Name "MSSQL$INSTANCE_NAME"
```

</details>

<details>
<summary><b>Solution 3: Firewall Configuration</b></summary>

**Add Firewall Rule:**
```powershell
# Allow SQL Server port
New-NetFirewallRule -DisplayName "SQL Server" `
  -Direction Inbound `
  -Protocol TCP `
  -LocalPort 1433 `
  -Action Allow
```

**Windows Firewall with Advanced Security:**
1. Open Windows Firewall
2. Inbound Rules → New Rule
3. Port → TCP → Specific ports: 1433
4. Allow the connection
5. Apply to all profiles

</details>

<details>
<summary><b>Solution 4: Authentication Issues</b></summary>

**Windows Authentication:**
```json
{
  "server": "SERVER\\INSTANCE",
  "database": "DATABASE",
  "useWindowsAuthentication": true
}
```

**Verify Permissions:**
```sql
-- Run in SSMS as admin
-- Check if your Windows user has access
SELECT name, type_desc, create_date
FROM sys.server_principals
WHERE name = 'DOMAIN\Username'
```

**SQL Authentication:**
```json
{
  "server": "SERVER\\INSTANCE",
  "database": "DATABASE",
  "username": "sql_user",
  "password": "password",
  "useWindowsAuthentication": false
}
```

**Enable SQL Authentication:**
1. Open SSMS
2. Server Properties → Security
3. Select "SQL Server and Windows Authentication mode"
4. Restart SQL Server

</details>

<details>
<summary><b>Solution 5: Increase Connection Timeout</b></summary>

```json
{
  "server": "REMOTE_SERVER",
  "database": "DATABASE",
  "connectionTimeout": 60,
  "commandTimeout": 600
}
```

**When to Use:**
- Remote servers
- Slow networks
- VPN connections
- High server load

</details>

</div>

---

### Login Failed

<div style="background: #024059; padding: 20px; border-radius: 8px; border-left: 4px solid #FF5555;">

**❌ Symptom:**
```
Login failed for user 'username'
Cannot open database "DATABASE" requested by the login
```

**✅ Solutions:**

1. **Verify Credentials**
   ```json
   {
     "username": "correct_username",
     "password": "correct_password",
     "useWindowsAuthentication": false
   }
   ```

2. **Check Database Access**
   ```sql
   -- Verify user has access to database
   USE [DATABASE_NAME];
   SELECT USER_NAME() AS CurrentUser;
   
   -- Grant access if needed (as admin)
   USE [DATABASE_NAME];
   CREATE USER [username] FOR LOGIN [username];
   ALTER ROLE db_datareader ADD MEMBER [username];
   ALTER ROLE db_datawriter ADD MEMBER [username];
   ```

3. **Password Policy Issues**
   ```sql
   -- Check if password policy is blocking
   SELECT name, is_policy_checked, is_expiration_checked
   FROM sys.sql_logins
   WHERE name = 'username';
   
   -- Reset password if needed
   ALTER LOGIN [username] WITH PASSWORD = 'NewPassword123!';
   ```

</div>

---

### Certificate Validation Failed

<div style="background: #02111B; padding: 15px; border-radius: 8px; border: 1px solid #FF5555;">

**❌ Symptom:**
```
A connection was successfully established with the server, 
but then an error occurred during the login process. 
The certificate chain was issued by an authority that is not trusted.
```

**✅ Solution:**
```json
{
  "server": "SERVER\\INSTANCE",
  "database": "DATABASE",
  "trustServerCertificate": true,
  "encrypt": false
}
```

**For Production (Secure):**
1. Install proper SSL certificate on SQL Server
2. Set `trustServerCertificate: false`
3. Set `encrypt: true`

</div>

---

## 📤 Export Problems

### Export Hangs or Freezes

<div style="background: linear-gradient(to right, #02111B, #024059); padding: 20px; border-radius: 10px; border-left: 4px solid #FFA500;">

**⚠️ Symptom:**
- Export starts but never completes
- Application becomes unresponsive
- Progress bar stuck at certain percentage

**🔍 Diagnosis:**

```powershell
# Check application logs
Get-Content logs\application.log -Tail 50

# Look for:
# - Last successful operation
# - Error messages
# - Memory warnings
```

**✅ Solutions:**

<details>
<summary><b>Solution 1: Reduce Batch Size</b></summary>

**Edit `config/appsettings.json`:**
```json
{
  "performance": {
    "batchSize": 25000,
    "memoryThresholdMB": 256
  }
}
```

**Recommended Values by Dataset Size:**
- Small (< 100K rows): 50,000
- Medium (100K-1M rows): 25,000
- Large (> 1M rows): 10,000

</details>

<details>
<summary><b>Solution 2: Close Other Applications</b></summary>

**Free up system resources:**
```powershell
# Check memory usage
Get-Process | Sort-Object -Property WS -Descending | Select-Object -First 10

# Close unnecessary applications
# Keep only essential processes running
```

**Memory Guidelines:**
- 4GB RAM: Close all non-essential apps
- 8GB RAM: Keep browser/office closed
- 16GB+ RAM: Usually no issues

</details>

<details>
<summary><b>Solution 3: Increase Command Timeout</b></summary>

```json
{
  "performance": {
    "commandTimeout": 600
  }
}
```

**Timeout Guidelines:**
- Simple queries: 60 seconds
- Complex procedures: 300 seconds (5 min)
- Very large datasets: 600 seconds (10 min)
- Massive operations: 1800 seconds (30 min)

</details>

<details>
<summary><b>Solution 4: Check Database Query</b></summary>

**Monitor in SQL Server:**
```sql
-- Check currently running queries
SELECT 
    session_id,
    start_time,
    status,
    command,
    SUBSTRING(text, 1, 200) AS query_text,
    wait_type,
    wait_time
FROM sys.dm_exec_requests
CROSS APPLY sys.dm_exec_sql_text(sql_handle)
WHERE session_id > 50
ORDER BY start_time DESC;
```

**If query is slow:**
- Check for missing indexes
- Review execution plan
- Optimize stored procedure
- Update statistics

</details>

</div>

---

### Export File is Empty or Incomplete

<div style="background: #024059; padding: 20px; border-radius: 8px; border-left: 4px solid #FFA500;">

**⚠️ Symptom:**
- File created but 0 bytes or very small
- File has headers but no data
- Row count doesn't match expected

**🔍 Diagnosis:**

```powershell
# Check file size
Get-ChildItem exports\*.xlsx | Select-Object Name, Length, CreationTime

# Check logs for errors
Select-String -Path logs\error.log -Pattern "Export" -Context 2
```

**✅ Solutions:**

1. **Verify Data Source Has Records**
   ```sql
   -- Check if table/procedure returns data
   SELECT COUNT(*) FROM EXP_OTHERS_1;
   
   -- Or execute procedure manually
   EXEC Others_EXP_10Days @mon = 20241101, @mon1 = 20241130;
   ```

2. **Check Export Permissions**
   ```powershell
   # Verify write access to exports folder
   $testFile = "exports\test.txt"
   "Test" | Out-File $testFile
   Remove-Item $testFile
   ```

3. **Review Parameter Values**
   - Verify date ranges are correct
   - Check filter parameters
   - Ensure parameters return data

4. **Check Disk Space**
   ```powershell
   # Verify available disk space
   Get-PSDrive C | Select-Object Used, Free
   
   # Need at least:
   # - 100MB for small exports
   # - 1GB for medium exports
   # - 10GB+ for large exports
   ```

</div>

---

### Excel Export Errors

<div style="background: #02111B; padding: 20px; border-radius: 8px; border-left: 4px solid #FFA500;">

**⚠️ Common Excel Issues:**

<details>
<summary><b>Issue 1: Row Limit Exceeded</b></summary>

**Symptom:**
```
Error: Excel worksheet cannot contain more than 1,048,576 rows
```

**Solution:**
- Use CSV format for large datasets
- Application automatically splits into multiple sheets
- Verify multi-sheet export in settings

```json
{
  "performance": {
    "excelMaxRowsPerSheet": 1048576
  }
}
```

</details>

<details>
<summary><b>Issue 2: Memory Error During Excel Export</b></summary>

**Symptom:**
```
OutOfMemoryException during Excel generation
```

**Solutions:**
1. Reduce batch size:
   ```json
   {
     "performance": {
       "batchSize": 10000,
       "memoryThresholdMB": 256
     }
   }
   ```

2. Export to CSV instead of Excel
3. Split export into smaller date ranges
4. Close other applications

</details>

<details>
<summary><b>Issue 3: Excel File Corrupted</b></summary>

**Symptom:**
- Cannot open file in Excel
- "File is corrupted" error
- Repair prompt when opening

**Solutions:**
1. Check logs for errors during export
2. Ensure export completed successfully
3. Verify disk space wasn't exhausted
4. Check for special characters in data
5. Try exporting smaller dataset first

**Recovery:**
```powershell
# Delete corrupted file
Remove-Item "exports\corrupted_file.xlsx"

# Re-export with debug logging
# Edit appsettings.json:
# "logLevel": "Debug"

# Retry export and check logs
```

</details>

</div>

---

### CSV Export Issues

<div style="background: #024059; padding: 15px; border-radius: 8px; border-left: 4px solid #FFA500;">

**⚠️ Common CSV Problems:**

1. **Incorrect Delimiter**
   ```json
   {
     "export": {
       "csvDelimiter": ",",
       "csvQuoteChar": "\""
     }
   }
   ```

2. **Encoding Issues**
   - Special characters appear as �
   - Non-English characters corrupted
   
   **Solution:**
   ```json
   {
     "export": {
       "csvEncoding": "UTF-8",
       "csvIncludeBOM": true
     }
   }
   ```

3. **Data with Commas/Quotes**
   - Data contains delimiter character
   - Quotes not properly escaped
   
   **Solution:**
   - Application auto-quotes fields with delimiters
   - Double-quotes escape internal quotes
   - Use different delimiter (tab, pipe)

</div>

---

## 📥 Import Failures

### Import File Not Recognized

<div style="background: linear-gradient(135deg, #024059 0%, #02111B 100%); padding: 20px; border-radius: 10px; border-left: 4px solid #FF5555;">

**❌ Symptom:**
```
Error: Unable to read file format
File type not supported
Invalid file structure
```

**✅ Solutions:**

1. **Verify File Format**
   - Supported: .xlsx, .xls, .csv, .txt
   - Check file extension matches content
   - Ensure file not corrupted

2. **Check File Encoding**
   ```powershell
   # Check file encoding
   Get-Content -Path "imports\file.csv" -Encoding UTF8 -TotalCount 5
   
   # If garbled, try different encoding:
   Get-Content -Path "imports\file.csv" -Encoding UTF7 -TotalCount 5
   ```

3. **Validate CSV Structure**
   - First row should be headers
   - Consistent column count across rows
   - Proper delimiter usage

4. **Excel File Issues**
   - Save as .xlsx (not .xls)
   - Close file in Excel before import
   - Remove password protection
   - Ensure no hidden sheets with issues

</div>

---

### Data Type Mismatch

<div style="background: #02111B; padding: 20px; border-radius: 8px; border-left: 4px solid #FF5555;">

**❌ Symptom:**
```
Error: Cannot convert value to expected type
Invalid data type for column
Conversion failed when converting varchar to int
```

**✅ Solutions:**

1. **Review Column Mappings**
   ```json
   {
     "columnMappings": [
       {
         "sourceColumn": "Date",
         "targetColumn": "TransactionDate",
         "dataType": "date",
         "required": true
       }
     ]
   }
   ```

2. **Clean Source Data**
   - Remove extra spaces
   - Correct date formats
   - Ensure numeric fields contain only numbers
   - Check for null values in required fields

3. **Validation Before Import**
   ```powershell
   # Preview import file
   Import-Csv "imports\data.csv" | Select-Object -First 10
   
   # Check data types
   Import-Csv "imports\data.csv" | ForEach-Object {
       $_.Amount -is [double]
   }
   ```

</div>

---

## ⚡ Performance Issues

### Slow Export Performance

<div style="background: linear-gradient(to right, #02111B, #024059); padding: 20px; border-radius: 10px; border-left: 4px solid #FFA500;">

**⚠️ Symptom:**
- Export takes much longer than expected
- Progress very slow
- High CPU or memory usage

**🔍 Performance Metrics:**

| Dataset Size | Expected Duration | Acceptable Range |
|-------------|-------------------|------------------|
| 10K rows | < 5 seconds | 2-10 seconds |
| 100K rows | < 30 seconds | 10-60 seconds |
| 1M rows | < 5 minutes | 2-10 minutes |
| 10M rows | < 30 minutes | 10-60 minutes |

**✅ Optimization Solutions:**

<details>
<summary><b>Optimization 1: Tune Batch Size</b></summary>

**Start with baseline:**
```json
{
  "performance": {
    "batchSize": 50000
  }
}
```

**Test and adjust:**
1. Export 100K rows, note time
2. Increase batch size to 75K
3. Export again, compare time
4. If faster, increase to 100K
5. If slower, decrease to 25K
6. Find optimal balance

**Monitor:**
```powershell
# Watch memory during export
while ($true) {
    Get-Process "TradeDataStudio*" | 
      Select-Object Name, @{N='MemMB';E={$_.WS/1MB}}
    Start-Sleep -Seconds 5
}
```

</details>

<details>
<summary><b>Optimization 2: Database Query Performance</b></summary>

**Identify slow queries:**
```sql
-- Find expensive queries
SELECT TOP 10
    total_elapsed_time / execution_count AS avg_elapsed_time,
    execution_count,
    SUBSTRING(text, 1, 500) AS query_text
FROM sys.dm_exec_query_stats
CROSS APPLY sys.dm_exec_sql_text(sql_handle)
ORDER BY avg_elapsed_time DESC;
```

**Optimize stored procedures:**
- Add appropriate indexes
- Update statistics
- Avoid cursors
- Use SET NOCOUNT ON
- Minimize joins

**Add index example:**
```sql
-- Create index on frequently queried columns
CREATE NONCLUSTERED INDEX IX_ExportDate
ON EXP_OTHERS (ExportDate)
INCLUDE (CustomsCode, Amount);
```

</details>

<details>
<summary><b>Optimization 3: Network Performance</b></summary>

**For remote SQL Servers:**

1. **Test network speed:**
   ```powershell
   Test-NetConnection -ComputerName "SQL_SERVER" -Port 1433
   ```

2. **Enable compression (SQL Server 2016+):**
   ```sql
   ALTER DATABASE [DATABASE_NAME]
   SET QUERY_STORE = ON;
   
   -- Check if compression helps
   ```

3. **Export to local drive, then transfer:**
   ```json
   {
     "paths": {
       "exports": "C:\\Temp\\Exports\\"
     }
   }
   ```
   Then copy to network location after export.

</details>

<details>
<summary><b>Optimization 4: System Resources</b></summary>

**Check system performance:**
```powershell
# CPU usage
Get-Counter '\Processor(_Total)\% Processor Time'

# Memory availability
Get-Counter '\Memory\Available MBytes'

# Disk queue
Get-Counter '\PhysicalDisk(_Total)\Avg. Disk Queue Length'
```

**Improvements:**
- Close unnecessary applications
- Disable antivirus scanning on export folder (temporarily)
- Run during off-peak hours
- Increase application priority

```powershell
# Set high priority (run as admin)
$proc = Get-Process "TradeDataStudio*"
$proc.PriorityClass = "High"
```

</details>

</div>

---

### High Memory Usage

<div style="background: #024059; padding: 20px; border-radius: 8px; border-left: 4px solid #FFA500;">

**⚠️ Symptom:**
```
System running out of memory
Application consumes 2GB+ RAM
OutOfMemoryException
```

**✅ Solutions:**

1. **Reduce Batch Size**
   ```json
   {
     "performance": {
       "batchSize": 10000,
       "memoryThresholdMB": 256
     }
   }
   ```

2. **Lower Memory Threshold**
   - Triggers garbage collection sooner
   - Prevents memory buildup
   - Slightly slower but more stable

3. **Export in Smaller Chunks**
   - Split date ranges
   - Export tables individually
   - Run multiple smaller operations

4. **Monitor Memory**
   ```powershell
   # Track memory usage
   Get-Process "TradeDataStudio*" | 
     Select-Object Name, 
     @{N='MemMB';E={[math]::Round($_.WS/1MB,2)}}
   ```

</div>

---

## 🚨 Application Errors

### Application Won't Start

<div style="background: linear-gradient(135deg, #02111B 0%, #024059 100%); padding: 20px; border-radius: 10px; border-left: 4px solid #FF0000;">

**❌ Symptom:**
- Double-click executable, nothing happens
- Application crashes immediately
- Error message on startup

**✅ Solutions:**

<details>
<summary><b>Solution 1: Check .NET Runtime</b></summary>

**Verify .NET 8.0 is installed:**
```powershell
dotnet --list-runtimes

# Look for:
# Microsoft.NETCore.App 8.0.x
```

**Install if missing:**
```powershell
winget install Microsoft.DotNet.Runtime.8
```

Or download from: https://dotnet.microsoft.com/download/dotnet/8.0

</details>

<details>
<summary><b>Solution 2: Check Configuration Files</b></summary>

**Validate JSON syntax:**
```powershell
# Test each config file
Get-Content config\appsettings.json | ConvertFrom-Json
Get-Content config\database.json | ConvertFrom-Json

# If error, use online JSON validator or:
```

**Common JSON errors:**
- Missing comma
- Extra comma at end
- Unescaped backslashes
- Unclosed brackets/braces

**Example error:**
```json
{
  "server": "SERVER\\INSTANCE"  // ❌ Missing comma
  "database": "DB"
}

{
  "server": "SERVER\\INSTANCE",  // ✅ Correct
  "database": "DB"
}
```

</details>

<details>
<summary><b>Solution 3: Check File Permissions</b></summary>

**Verify permissions:**
```powershell
# Check if current user can write to directories
Test-Path -Path "logs" -PathType Container
Test-Path -Path "exports" -PathType Container

# Create if missing
New-Item -ItemType Directory -Path "logs" -Force
New-Item -ItemType Directory -Path "exports" -Force
```

**Run as Administrator (if needed):**
- Right-click executable
- Select "Run as administrator"

</details>

<details>
<summary><b>Solution 4: Check Windows Event Log</b></summary>

```powershell
# View application errors
Get-WinEvent -LogName Application -MaxEvents 20 | 
  Where-Object { $_.ProviderName -like "*TradeData*" -or 
                  $_.ProviderName -like "*.NET*" }
```

**Common errors:**
- Missing DLL files
- Access denied
- Configuration errors

</details>

</div>

---

### Unhandled Exceptions

<div style="background: #02111B; padding: 20px; border-radius: 8px; border-left: 4px solid #FF0000;">

**❌ Symptom:**
```
Unhandled exception occurred
NullReferenceException
IndexOutOfRangeException
Application must close
```

**✅ Solutions:**

1. **Enable Debug Logging**
   ```json
   {
     "application": {
       "logLevel": "Debug"
     }
   }
   ```

2. **Check Error Logs**
   ```powershell
   # View recent errors
   Get-Content logs\error.log -Tail 50
   
   # Search for stack traces
   Select-String -Path logs\error.log -Pattern "Exception"
   ```

3. **Reproduce with Logging**
   - Enable debug mode
   - Repeat steps that caused error
   - Review detailed logs
   - Report issue with logs

4. **Reset to Defaults**
   ```powershell
   # Backup current config
   Copy-Item config\* backup\

   # Restore default config
   # (from installation or documentation)
   ```

</div>

---

## ⚙️ Configuration Problems

### Invalid Configuration

<div style="background: #024059; padding: 20px; border-radius: 8px; border-left: 4px solid #FFA500;">

**⚠️ Symptom:**
```
Configuration error
Invalid JSON format
Missing required property
```

**✅ Solutions:**

1. **Validate JSON**
   ```powershell
   # Test JSON parsing
   try {
       Get-Content config\appsettings.json | ConvertFrom-Json
       Write-Host "✅ Valid JSON" -ForegroundColor Green
   } catch {
       Write-Host "❌ Invalid JSON: $($_.Exception.Message)" -ForegroundColor Red
   }
   ```

2. **Common JSON Mistakes**
   - Trailing commas
   - Unescaped backslashes in paths
   - Missing quotes around strings
   - Boolean values in quotes (should be bare)

3. **Use Default Template**
   - Check documentation for default config
   - Copy from fresh installation
   - Modify one setting at a time

</div>

---

### Missing Configuration Files

<div style="background: #02111B; padding: 15px; border-radius: 8px; border: 1px solid #FFA500;">

**⚠️ Symptom:**
```
Configuration file not found: config\database.json
Cannot find appsettings.json
```

**✅ Solution:**

```powershell
# Create missing config directory
New-Item -ItemType Directory -Path "config" -Force

# Copy from installation or create from templates
# See CONFIGURATION.md for default templates
```

**Minimum Required Files:**
- `config/appsettings.json`
- `config/database.json`

</div>

---

## 📁 File System Issues

### Permission Denied

<div style="background: linear-gradient(to right, #02111B, #024059); padding: 20px; border-radius: 10px; border-left: 4px solid #FF5555;">

**❌ Symptom:**
```
Access to path denied
UnauthorizedAccessException
Cannot write to exports folder
```

**✅ Solutions:**

1. **Check Folder Permissions**
   ```powershell
   # View current permissions
   Get-Acl -Path "exports" | Format-List
   
   # Grant full control to current user
   $acl = Get-Acl "exports"
   $rule = New-Object System.Security.AccessControl.FileSystemAccessRule(
       $env:USERNAME,
       "FullControl",
       "ContainerInherit,ObjectInherit",
       "None",
       "Allow"
   )
   $acl.SetAccessRule($rule)
   Set-Acl -Path "exports" -AclObject $acl
   ```

2. **Run as Administrator**
   - Close application
   - Right-click executable
   - Select "Run as administrator"

3. **Change Export Location**
   ```json
   {
     "paths": {
       "exports": "C:\\Users\\YourName\\Documents\\TradeDataExports\\"
     }
   }
   ```

</div>

---

### Disk Space Issues

<div style="background: #024059; padding: 20px; border-radius: 8px; border-left: 4px solid #FFA500;">

**⚠️ Symptom:**
```
There is not enough space on the disk
Disk full error
Export failed due to disk space
```

**✅ Solutions:**

1. **Check Available Space**
   ```powershell
   # Check disk space
   Get-PSDrive C | Select-Object Used, Free, 
     @{N='FreeGB';E={[math]::Round($_.Free/1GB,2)}}
   ```

2. **Clean Up Exports Folder**
   ```powershell
   # List large export files
   Get-ChildItem exports\*.xlsx | 
     Sort-Object Length -Descending | 
     Select-Object Name, @{N='SizeMB';E={[math]::Round($_.Length/1MB,2)}}
   
   # Archive old exports
   $archivePath = "F:\Archives\Exports_$(Get-Date -Format 'yyyyMMdd').zip"
   Compress-Archive -Path "exports\*" -DestinationPath $archivePath
   
   # Delete after verifying archive
   # Remove-Item exports\*.xlsx -Force
   ```

3. **Change Export Location**
   ```json
   {
     "paths": {
       "exports": "E:\\TradeDataExports\\"
     }
   }
   ```

4. **Clean Temp Files**
   ```powershell
   # Remove application temp files
   Remove-Item temp\* -Recurse -Force
   
   # Clean Windows temp
   Remove-Item $env:TEMP\* -Recurse -Force -ErrorAction SilentlyContinue
   ```

</div>

---

## 📊 Data Quality Issues

### Special Characters in Data

<div style="background: #02111B; padding: 20px; border-radius: 8px; border-left: 4px solid #FFA500;">

**⚠️ Symptom:**
- Special characters appear as �
- Unicode characters corrupted
- CSV file shows garbled text

**✅ Solutions:**

1. **Enable UTF-8 Encoding**
   ```json
   {
     "export": {
       "csvEncoding": "UTF-8",
       "csvIncludeBOM": true
     }
   }
   ```

2. **Excel Special Characters**
   - Excel handles special characters automatically
   - Use Excel format for international data

3. **Clean Data at Source**
   ```sql
   -- Remove problematic characters
   SELECT 
       REPLACE(REPLACE(ColumnName, CHAR(13), ''), CHAR(10), '') AS CleanedColumn
   FROM TableName
   ```

</div>

---

### Null Values

<div style="background: #024059; padding: 15px; border-radius: 8px; border-left: 4px solid #FFA500;">

**⚠️ Symptom:**
- Empty cells in export
- "NULL" text in CSV
- Import fails on required fields

**✅ Solutions:**

1. **Handle Nulls in Query**
   ```sql
   -- Replace nulls with defaults
   SELECT 
       ISNULL(ColumnName, 'N/A') AS ColumnName,
       ISNULL(NumericColumn, 0) AS NumericColumn
   FROM TableName
   ```

2. **Configure Import Validation**
   ```json
   {
     "columnMappings": [
       {
         "sourceColumn": "Amount",
         "targetColumn": "Amount",
         "required": true,
         "defaultValue": 0
       }
     ]
   }
   ```

</div>

---

## 🛠️ Diagnostic Tools

### Enabling Debug Mode

<div style="background: linear-gradient(135deg, #024059 0%, #02111B 100%); padding: 20px; border-radius: 10px; border-left: 4px solid #00E5FF;">

**Enable comprehensive logging:**

```json
{
  "application": {
    "logLevel": "Debug"
  }
}
```

**What Gets Logged:**
- All SQL queries executed
- Parameter values
- Connection strings (passwords redacted)
- Memory usage stats
- Execution timing
- Stack traces for errors

**View Debug Logs:**
```powershell
# Tail debug log in real-time
Get-Content logs\debug.log -Wait -Tail 20

# Search for specific operation
Select-String -Path logs\debug.log -Pattern "Export" -Context 2
```

</div>

---

### Log Analysis

<div style="background: #02111B; padding: 20px; border-radius: 8px;">

**Useful PowerShell Commands:**

```powershell
# Find errors in last hour
$since = (Get-Date).AddHours(-1)
Get-Content logs\error.log | 
  Where-Object { $_ -match $since.ToString("yyyy-MM-dd HH") }

# Count errors by type
Get-Content logs\error.log | 
  Select-String "Exception" | 
  Group-Object | 
  Sort-Object Count -Descending

# Export logs for support
$date = Get-Date -Format "yyyyMMdd"
Copy-Item logs\*.log "support_logs_$date\"
Compress-Archive "support_logs_$date\*" "support_logs_$date.zip"
```

</div>

---

### Performance Monitoring

<div style="background: #024059; padding: 20px; border-radius: 8px;">

**Monitor Application Performance:**

```powershell
# Real-time performance monitoring
while ($true) {
    $proc = Get-Process "TradeDataStudio*" -ErrorAction SilentlyContinue
    if ($proc) {
        [PSCustomObject]@{
            Time = Get-Date -Format "HH:mm:ss"
            CPUPercent = [math]::Round($proc.CPU, 2)
            MemoryMB = [math]::Round($proc.WS / 1MB, 2)
            Threads = $proc.Threads.Count
        } | Format-Table -AutoSize
    }
    Start-Sleep -Seconds 2
}
```

**Performance Baseline:**
- CPU: < 50% during export
- Memory: < 1GB for normal operations
- Threads: 10-50 typical

</div>

---

### SQL Server Diagnostics

<div style="background: #02111B; padding: 20px; border-radius: 8px;">

**Check SQL Server Performance:**

```sql
-- Active connections from application
SELECT 
    session_id,
    login_name,
    program_name,
    host_name,
    status,
    last_request_start_time
FROM sys.dm_exec_sessions
WHERE program_name LIKE '%TradeData%';

-- Current locks
SELECT 
    request_session_id,
    resource_type,
    resource_database_id,
    request_mode,
    request_status
FROM sys.dm_tran_locks
WHERE request_session_id IN (
    SELECT session_id 
    FROM sys.dm_exec_sessions 
    WHERE program_name LIKE '%TradeData%'
);

-- Query performance
SELECT 
    execution_count,
    total_elapsed_time / 1000000 AS total_seconds,
    total_elapsed_time / execution_count / 1000 AS avg_milliseconds,
    SUBSTRING(text, 1, 200) AS query_text
FROM sys.dm_exec_query_stats
CROSS APPLY sys.dm_exec_sql_text(sql_handle)
WHERE text LIKE '%EXP_OTHERS%'
ORDER BY total_elapsed_time DESC;
```

</div>

---

## 🆘 Getting Additional Help

### Before Requesting Support

<div style="background: linear-gradient(to right, #02111B, #024059); padding: 20px; border-radius: 10px; border-left: 4px solid #00E5FF;">

**📋 Gather This Information:**

1. **Application Details**
   - Version number
   - Installation type (self-contained/portable)
   - Operating system version

2. **Error Details**
   - Exact error message
   - Steps to reproduce
   - When error started occurring

3. **Log Files**
   ```powershell
   # Collect logs
   $date = Get-Date -Format "yyyyMMdd_HHmmss"
   $supportDir = "support_package_$date"
   
   New-Item -ItemType Directory -Path $supportDir
   Copy-Item logs\*.log $supportDir\
   Copy-Item config\*.json $supportDir\ 
   
   # Redact sensitive info
   $dbConfig = Get-Content "$supportDir\database.json" | ConvertFrom-Json
   $dbConfig.password = "***REDACTED***"
   $dbConfig | ConvertTo-Json | Set-Content "$supportDir\database.json"
   
   # Create archive
   Compress-Archive -Path $supportDir -DestinationPath "$supportDir.zip"
   ```

4. **System Information**
   ```powershell
   # System details
   Get-ComputerInfo | Select-Object `
       WindowsVersion, `
       OsArchitecture, `
       OsTotalVisibleMemorySize, `
       OsFreePhysicalMemory
   ```

</div>

---

### Support Channels

<div style="background: #024059; padding: 20px; border-radius: 8px; border-left: 4px solid #00FF87;">

**📧 Email Support**
- Address: support@tradedatastudio.com
- Include: Support package ZIP
- Response: Within 24 hours

**📖 Documentation**
- [User Guide](./USER_GUIDE.md)
- [Configuration Guide](./CONFIGURATION.md)
- [Installation Guide](../deployment/INSTALLATION.md)

**🐛 Issue Tracker**
- GitHub Issues: Report bugs
- Include error logs
- Describe reproduction steps

</div>

---

### Self-Service Resources

<div style="background: #02111B; padding: 15px; border-radius: 8px;">

**Quick Diagnostics Script:**

```powershell
# Run comprehensive diagnostics
$report = @"
=== TradeData Studio Diagnostics ===
Date: $(Get-Date)

Application Version: $(Get-Content VERSION.txt -ErrorAction SilentlyContinue)

System Info:
$(Get-ComputerInfo | Select-Object WindowsVersion, OsArchitecture | Out-String)

Disk Space:
$(Get-PSDrive C | Select-Object Name, Used, Free | Out-String)

Configuration Status:
"@

# Check config files
$configs = @("appsettings.json", "database.json")
foreach ($config in $configs) {
    $path = "config\$config"
    if (Test-Path $path) {
        try {
            Get-Content $path | ConvertFrom-Json | Out-Null
            $report += "`n✅ $config - Valid"
        } catch {
            $report += "`n❌ $config - Invalid: $($_.Exception.Message)"
        }
    } else {
        $report += "`n❌ $config - Missing"
    }
}

# Check directories
$dirs = @("exports", "imports", "logs", "config")
foreach ($dir in $dirs) {
    if (Test-Path $dir) {
        $report += "`n✅ Directory $dir exists"
    } else {
        $report += "`n❌ Directory $dir missing"
    }
}

$report += "`n`nRecent Errors:"
$report += Get-Content logs\error.log -Tail 10 -ErrorAction SilentlyContinue | Out-String

$report | Out-File "diagnostics_$(Get-Date -Format 'yyyyMMdd_HHmmss').txt"
Write-Host "Diagnostics saved to diagnostics_*.txt"
```

</div>

---

<div align="center">

## 🎯 Quick Troubleshooting Flowchart

```
                    ┌─────────────────┐
                    │  Issue Occurs   │
                    └────────┬────────┘
                             │
                    ┌────────▼────────┐
                    │ Check Error Log │
                    └────────┬────────┘
                             │
              ┌──────────────┼──────────────┐
              │              │              │
       ┌──────▼──────┐ ┌────▼─────┐ ┌─────▼──────┐
       │ Connection  │ │  Export  │ │    Other   │
       │   Error     │ │  Error   │ │   Error    │
       └──────┬──────┘ └────┬─────┘ └─────┬──────┘
              │             │              │
       ┌──────▼──────┐ ┌────▼─────┐ ┌─────▼──────┐
       │ Test DB     │ │ Check    │ │  Enable    │
       │ Connection  │ │ Disk     │ │  Debug     │
       │             │ │ Space    │ │  Logging   │
       └──────┬──────┘ └────┬─────┘ └─────┬──────┘
              │             │              │
       ┌──────▼──────┐ ┌────▼─────┐ ┌─────▼──────┐
       │ Fix Config  │ │ Clean    │ │  Review    │
       │ or Network  │ │ Exports  │ │  Logs      │
       └──────┬──────┘ └────┬─────┘ └─────┬──────┘
              │             │              │
              └──────────────┼──────────────┘
                             │
                    ┌────────▼────────┐
                    │   Issue Solved  │
                    │        or       │
                    │  Contact Support│
                    └─────────────────┘
```

---

**📚 Related Documentation:**
- [User Guide](./USER_GUIDE.md)
- [Configuration Guide](./CONFIGURATION.md)
- [Installation Guide](../deployment/INSTALLATION.md)

---

<p align="center">
  <img src="https://img.shields.io/badge/Need_Help%3F-We're_Here-00E5FF?style=for-the-badge" alt="Support"/>
</p>

<p align="center">
  <i>Most issues can be resolved by checking logs and adjusting configuration.</i><br>
  <i>If you're still stuck, we're here to help!</i>
</p>

</div>
