<div align="center">

```
╔═══════════════════════════════════════════════════════════════╗
║                                                               ║
║              ██████╗ ██████╗ ███╗   ██╗███████╗               ║
║             ██╔════╝██╔═══██╗████╗  ██║██╔════╝               ║
║             ██║     ██║   ██║██╔██╗ ██║█████╗                 ║
║             ██║     ██║   ██║██║╚██╗██║██╔══╝                 ║
║             ╚██████╗╚██████╔╝██║ ╚████║██║                    ║
║              ╚═════╝ ╚═════╝ ╚═╝  ╚═══╝╚═╝                    ║
║                                                               ║
║             ███████╗██╗ ██████╗ ██╗   ██╗██████╗              ║
║             ██╔════╝██║██╔════╝ ██║   ██║██╔══██╗             ║
║             █████╗  ██║██║  ███╗██║   ██║██████╔╝             ║
║             ██╔══╝  ██║██║   ██║██║   ██║██╔══██╗             ║
║             ██║     ██║╚██████╔╝╚██████╔╝██║  ██║             ║
║             ╚═╝     ╚═╝ ╚═════╝  ╚═════╝ ╚═╝  ╚═╝             ║
║                                                               ║
║              █████╗ ████████╗██╗ ██████╗ ███╗   ██╗           ║
║             ██╔══██╗╚══██╔══╝██║██╔═══██╗████╗  ██║           ║ 
║             ███████║   ██║   ██║██║   ██║██╔██╗ ██║           ║
║             ██╔══██║   ██║   ██║██║   ██║██║╚██╗██║           ║
║             ██║  ██║   ██║   ██║╚██████╔╝██║ ╚████║           ║
║             ╚═╝  ╚═╝   ╚═╝   ╚═╝ ╚═════╝ ╚═╝  ╚═══╝           ║
║                                                               ║
╚═══════════════════════════════════════════════════════════════╝
```

# Configuration Guide

<p align="center">
  <img src="https://img.shields.io/badge/Version-1.0.0-00E5FF?style=for-the-badge" alt="Version"/>
  <img src="https://img.shields.io/badge/Config_Format-JSON-02111B?style=for-the-badge" alt="Format"/>
  <img src="https://img.shields.io/badge/Updated-December_2025-00FF87?style=for-the-badge" alt="Updated"/>
</p>

**Complete reference for configuring TradeData Studio**

</div>

---

## 📑 Table of Contents

1. [Configuration Overview](#-configuration-overview)
2. [Application Settings](#-application-settings)
3. [Database Configuration](#-database-configuration)
4. [Export Table Definitions](#-export-table-definitions)
5. [Export Procedure Definitions](#-export-procedure-definitions)
6. [Import Configurations](#-import-configurations)
7. [Performance Tuning](#-performance-tuning)
8. [Path Configuration](#-path-configuration)
9. [Logging Configuration](#-logging-configuration)
10. [Advanced Configuration](#-advanced-configuration)

---

## 📋 Configuration Overview

### Configuration File Structure

<div style="background: linear-gradient(135deg, #02111B 0%, #024059 100%); padding: 20px; border-radius: 10px; border-left: 4px solid #00E5FF;">

All configuration files are located in the `config/` directory:

```
config/
├── appsettings.json          # Application behavior and performance
├── database.json             # Database connection settings
├── export_tables.json        # Table export definitions
├── export_procedures.json    # Stored procedure definitions
├── import_tables.json        # Import table configurations
└── import_procedures.json    # Import procedure definitions
```

**File Format:** All files use JSON format
**Encoding:** UTF-8 without BOM
**Validation:** Automatic on application startup

</div>

### Configuration Hierarchy

```
┌─────────────────────────────────────────────────┐
│           Application Settings                  │
│           (appsettings.json)                   │
│   ┌─────────────────────────────────────┐      │
│   │ • Default mode                      │      │
│   │ • Log levels                        │      │
│   │ • Performance parameters            │      │
│   │ • Path configurations               │      │
│   └─────────────────────────────────────┘      │
└─────────────────────────────────────────────────┘
                     ▼
┌─────────────────────────────────────────────────┐
│         Database Configuration                  │
│         (database.json)                        │
│   ┌─────────────────────────────────────┐      │
│   │ • Connection string                 │      │
│   │ • Authentication mode               │      │
│   │ • Timeout settings                  │      │
│   └─────────────────────────────────────┘      │
└─────────────────────────────────────────────────┘
                     ▼
┌─────────────────────────────────────────────────┐
│    Operation Definitions                        │
│    (tables/procedures)                         │
│   ┌─────────────────────────────────────┐      │
│   │ • Export definitions                │      │
│   │ • Import definitions                │      │
│   │ • Parameter specifications          │      │
│   └─────────────────────────────────────┘      │
└─────────────────────────────────────────────────┘
```

---

## ⚙️ Application Settings

### File: `appsettings.json`

<div style="background: #02111B; padding: 20px; border-radius: 8px;">

**Complete Configuration Template:**

```json
{
  "application": {
    "name": "TradeData Studio",
    "version": "1.0.0",
    "defaultMode": "Export",
    "logLevel": "Information",
    "exportFormats": ["Excel", "CSV", "TXT"],
    "theme": "Dark",
    "language": "en-US"
  },
  "paths": {
    "exports": "./exports/",
    "imports": "./imports/",
    "logs": "./logs/",
    "config": "./config/",
    "temp": "./temp/"
  },
  "performance": {
    "batchSize": 50000,
    "excelMaxRowsPerSheet": 1048576,
    "enableAsyncExport": true,
    "memoryThresholdMB": 512,
    "maxConcurrentExports": 3,
    "commandTimeout": 300
  },
  "features": {
    "enableImport": true,
    "enableExport": true,
    "enableStoredProcedures": true,
    "enableDirectTableAccess": true,
    "enableAutoRefresh": true,
    "refreshIntervalSeconds": 60
  },
  "export": {
    "defaultFormat": "Excel",
    "includeHeaders": true,
    "timestampFilenames": true,
    "filenameFormat": "{tablename}_{timestamp}.{ext}",
    "csvDelimiter": ",",
    "csvQuoteChar": "\"",
    "excelAutoFit": true,
    "excelFreezeHeaders": true
  }
}
```

</div>

### Application Section

<table>
<tr>
<th>Property</th>
<th>Type</th>
<th>Default</th>
<th>Description</th>
</tr>
<tr>
<td><code>name</code></td>
<td>string</td>
<td>"TradeData Studio"</td>
<td>Application display name</td>
</tr>
<tr>
<td><code>version</code></td>
<td>string</td>
<td>"1.0.0"</td>
<td>Version number (read-only)</td>
</tr>
<tr>
<td><code>defaultMode</code></td>
<td>enum</td>
<td>"Export"</td>
<td>Starting mode: Export, Import</td>
</tr>
<tr>
<td><code>logLevel</code></td>
<td>enum</td>
<td>"Information"</td>
<td>Debug, Information, Warning, Error</td>
</tr>
<tr>
<td><code>exportFormats</code></td>
<td>array</td>
<td>["Excel", "CSV", "TXT"]</td>
<td>Available export formats</td>
</tr>
<tr>
<td><code>theme</code></td>
<td>enum</td>
<td>"Dark"</td>
<td>UI theme: Dark, Light</td>
</tr>
<tr>
<td><code>language</code></td>
<td>string</td>
<td>"en-US"</td>
<td>Application language/culture</td>
</tr>
</table>

**Example Configurations:**

<details>
<summary><b>Development Environment</b></summary>

```json
{
  "application": {
    "name": "TradeData Studio [DEV]",
    "version": "1.0.0",
    "defaultMode": "Export",
    "logLevel": "Debug",
    "exportFormats": ["Excel", "CSV", "TXT"],
    "theme": "Dark",
    "language": "en-US"
  }
}
```

**Features:**
- Debug logging enabled
- Development mode indicators
- All formats available

</details>

<details>
<summary><b>Production Environment</b></summary>

```json
{
  "application": {
    "name": "TradeData Studio",
    "version": "1.0.0",
    "defaultMode": "Export",
    "logLevel": "Information",
    "exportFormats": ["Excel"],
    "theme": "Light",
    "language": "en-US"
  }
}
```

**Features:**
- Information-level logging
- Excel only (restricted)
- Production-ready settings

</details>

### Performance Section

<div style="background: #024059; padding: 20px; border-radius: 8px; border-left: 4px solid #00E5FF;">

**Performance Parameters Explained:**

| Property | Range | Impact | Recommendation |
|----------|-------|--------|----------------|
| `batchSize` | 1K-100K | Memory vs Speed | 50K for most cases |
| `excelMaxRowsPerSheet` | Fixed | Excel limitation | 1,048,576 (Excel max) |
| `enableAsyncExport` | true/false | UI responsiveness | true (recommended) |
| `memoryThresholdMB` | 256-2048 | Memory management | 512 MB baseline |
| `maxConcurrentExports` | 1-10 | Parallel processing | 3 for balance |
| `commandTimeout` | 30-3600 | Query timeout (seconds) | 300 (5 minutes) |

</div>

**Performance Profiles:**

<div style="background: #02111B; padding: 15px; border-radius: 8px;">

**🐇 Speed-Optimized (High-End System)**
```json
{
  "performance": {
    "batchSize": 100000,
    "memoryThresholdMB": 1024,
    "enableAsyncExport": true,
    "maxConcurrentExports": 5,
    "commandTimeout": 600
  }
}
```
- Best for: 16GB+ RAM, SSD, modern CPU
- Handles: Large datasets quickly
- Trade-off: Higher memory usage

**🐢 Memory-Optimized (Limited Resources)**
```json
{
  "performance": {
    "batchSize": 25000,
    "memoryThresholdMB": 256,
    "enableAsyncExport": true,
    "maxConcurrentExports": 1,
    "commandTimeout": 300
  }
}
```
- Best for: 4-8GB RAM, HDD, older systems
- Handles: Conserves memory
- Trade-off: Slower processing

**⚖️ Balanced (Default)**
```json
{
  "performance": {
    "batchSize": 50000,
    "memoryThresholdMB": 512,
    "enableAsyncExport": true,
    "maxConcurrentExports": 3,
    "commandTimeout": 300
  }
}
```
- Best for: Most scenarios
- Handles: Good speed with reasonable memory
- Trade-off: Balanced approach

</div>

### Export Section

<div style="background: linear-gradient(to right, #02111B, #024059); padding: 20px; border-radius: 10px;">

**Export Configuration Options:**

```json
{
  "export": {
    "defaultFormat": "Excel",
    "includeHeaders": true,
    "timestampFilenames": true,
    "filenameFormat": "{tablename}_{timestamp}.{ext}",
    "csvDelimiter": ",",
    "csvQuoteChar": "\"",
    "csvEncoding": "UTF-8",
    "csvIncludeBOM": true,
    "excelAutoFit": true,
    "excelFreezeHeaders": true,
    "excelFormatHeaders": true,
    "txtDelimiter": "\t",
    "txtLineEnding": "CRLF"
  }
}
```

**Filename Format Tokens:**
- `{tablename}` - Table or procedure name
- `{timestamp}` - Current timestamp (yyyyMMdd_HHmmss)
- `{date}` - Current date (yyyyMMdd)
- `{time}` - Current time (HHmmss)
- `{ext}` - File extension
- `{mode}` - Export/Import mode

**Examples:**
```
"{tablename}_{date}.{ext}"
  → EXP_OTHERS_20241201.xlsx

"{mode}_{tablename}_{timestamp}.{ext}"
  → Export_EXP_OTHERS_20241201_143022.xlsx

"{tablename}_backup_{timestamp}.{ext}"
  → EXP_OTHERS_backup_20241201_143022.xlsx
```

</div>

---

## 🗄️ Database Configuration

### File: `database.json`

<div style="background: #024059; padding: 20px; border-radius: 8px; border-left: 4px solid #00E5FF;">

**Complete Database Configuration:**

```json
{
  "server": "SERVER\\INSTANCE",
  "database": "DATABASE_NAME",
  "username": "",
  "password": "",
  "useWindowsAuthentication": true,
  "connectionTimeout": 30,
  "commandTimeout": 300,
  "trustServerCertificate": true,
  "encrypt": false,
  "multipleActiveResultSets": true,
  "applicationName": "TradeDataStudio",
  "pooling": true,
  "minPoolSize": 0,
  "maxPoolSize": 100,
  "connectionLifetime": 0
}
```

</div>

### Connection Parameters

<table>
<tr>
<th>Parameter</th>
<th>Type</th>
<th>Description</th>
<th>Example</th>
</tr>
<tr>
<td><code>server</code></td>
<td>string</td>
<td>SQL Server instance</td>
<td>"MATRIX\\MATRIX"</td>
</tr>
<tr>
<td><code>database</code></td>
<td>string</td>
<td>Database name</td>
<td>"RAW_PROCESS"</td>
</tr>
<tr>
<td><code>username</code></td>
<td>string</td>
<td>SQL login (if not Windows Auth)</td>
<td>"sql_user"</td>
</tr>
<tr>
<td><code>password</code></td>
<td>string</td>
<td>SQL password (if not Windows Auth)</td>
<td>"SecureP@ss123"</td>
</tr>
<tr>
<td><code>useWindowsAuthentication</code></td>
<td>boolean</td>
<td>Use Windows credentials</td>
<td>true</td>
</tr>
<tr>
<td><code>connectionTimeout</code></td>
<td>int</td>
<td>Connection timeout (seconds)</td>
<td>30</td>
</tr>
<tr>
<td><code>commandTimeout</code></td>
<td>int</td>
<td>Command timeout (seconds)</td>
<td>300</td>
</tr>
<tr>
<td><code>trustServerCertificate</code></td>
<td>boolean</td>
<td>Accept self-signed certificates</td>
<td>true</td>
</tr>
<tr>
<td><code>encrypt</code></td>
<td>boolean</td>
<td>Encrypt connection</td>
<td>false</td>
</tr>
<tr>
<td><code>multipleActiveResultSets</code></td>
<td>boolean</td>
<td>Enable MARS</td>
<td>true</td>
</tr>
</table>

### Connection Scenarios

<details>
<summary><b>🔐 Windows Authentication (Recommended)</b></summary>

```json
{
  "server": "PROD_SERVER\\SQLEXPRESS",
  "database": "TradeData",
  "username": "",
  "password": "",
  "useWindowsAuthentication": true,
  "connectionTimeout": 30,
  "trustServerCertificate": true
}
```

**Advantages:**
- ✅ No credentials in config file
- ✅ Uses Windows security
- ✅ Single sign-on
- ✅ Integrated with Active Directory

**Requirements:**
- Windows domain account
- SQL Server configured for Windows Auth
- Proper permissions granted

</details>

<details>
<summary><b>🔑 SQL Authentication</b></summary>

```json
{
  "server": "REMOTE_SERVER,1433",
  "database": "TradeData",
  "username": "trade_app_user",
  "password": "S3cur3P@ssw0rd!",
  "useWindowsAuthentication": false,
  "connectionTimeout": 30,
  "trustServerCertificate": true,
  "encrypt": true
}
```

**⚠️ Security Notes:**
- Password stored in plain text
- Use strong passwords
- Restrict file permissions
- Consider environment variables
- Enable encryption in production

**Improved Security:**
```powershell
# Set environment variable for password
$env:SQL_PASSWORD = "S3cur3P@ssw0rd!"

# Reference in application (requires code modification)
# or use encrypted configuration
```

</details>

<details>
<summary><b>🌐 Remote Server with Port</b></summary>

```json
{
  "server": "192.168.1.100,1433",
  "database": "TradeData",
  "username": "remote_user",
  "password": "RemoteP@ss123",
  "useWindowsAuthentication": false,
  "connectionTimeout": 60,
  "commandTimeout": 600,
  "trustServerCertificate": true,
  "encrypt": true
}
```

**Configuration Notes:**
- Specify port after comma
- Increase timeouts for network latency
- Enable encryption for remote connections
- Verify firewall rules

</details>

<details>
<summary><b>🔒 Encrypted Connection</b></summary>

```json
{
  "server": "SECURE_SERVER\\INSTANCE",
  "database": "SecureDB",
  "username": "",
  "password": "",
  "useWindowsAuthentication": true,
  "connectionTimeout": 30,
  "trustServerCertificate": false,
  "encrypt": true,
  "multipleActiveResultSets": true
}
```

**Requirements:**
- Valid SSL certificate on SQL Server
- Certificate trusted by client
- SQL Server configured for encryption
- Modern TLS protocol support

</details>

### Connection String Examples

The application builds connection strings from these settings:

<div style="background: #02111B; padding: 15px; border-radius: 8px;">

**Windows Authentication:**
```
Server=MATRIX\MATRIX;
Database=RAW_PROCESS;
Integrated Security=True;
TrustServerCertificate=True;
Connection Timeout=30;
Application Name=TradeDataStudio
```

**SQL Authentication:**
```
Server=SERVER\INSTANCE;
Database=DATABASE_NAME;
User Id=username;
Password=password;
TrustServerCertificate=True;
Connection Timeout=30;
Application Name=TradeDataStudio
```

</div>

### Connection Pooling

<div style="background: #024059; padding: 20px; border-radius: 8px;">

**Connection Pool Configuration:**

```json
{
  "pooling": true,
  "minPoolSize": 0,
  "maxPoolSize": 100,
  "connectionLifetime": 0
}
```

| Parameter | Description | Recommended |
|-----------|-------------|-------------|
| `pooling` | Enable connection pooling | true |
| `minPoolSize` | Minimum connections | 0 (on-demand) |
| `maxPoolSize` | Maximum connections | 100 |
| `connectionLifetime` | Max connection age (seconds) | 0 (unlimited) |

**When to Adjust:**
- High-traffic scenarios: Increase `minPoolSize` to 5-10
- Connection limits reached: Reduce `maxPoolSize`
- Connection leaks: Set `connectionLifetime` to 600-1800

</div>

---

## 📊 Export Table Definitions

### File: `export_tables.json`

<div style="background: linear-gradient(135deg, #02111B 0%, #024059 100%); padding: 20px; border-radius: 10px; border-left: 4px solid #00E5FF;">

**Table Definition Structure:**

```json
{
  "tables": [
    {
      "name": "TABLE_NAME",
      "displayName": "Friendly Display Name",
      "description": "Detailed description of table purpose",
      "schema": "dbo",
      "enabled": true,
      "defaultFormat": "Excel",
      "includeInQuickExport": true,
      "customQuery": "",
      "tags": ["export", "monthly", "trade"]
    }
  ]
}
```

</div>

### Complete Example

```json
{
  "tables": [
    {
      "name": "EXP_OTHERS_1",
      "displayName": "Export Others (Group 1)",
      "description": "Export data for customs: INDEL4, INDPC4, INNSA1, INBOM4",
      "schema": "dbo",
      "enabled": true,
      "defaultFormat": "Excel",
      "includeInQuickExport": true,
      "customQuery": "",
      "tags": ["export", "group1", "customs"]
    },
    {
      "name": "EXP_OTHERS_2",
      "displayName": "Export Others (Group 2)",
      "description": "Export data for remaining customs offices",
      "schema": "dbo",
      "enabled": true,
      "defaultFormat": "CSV",
      "includeInQuickExport": true,
      "customQuery": "",
      "tags": ["export", "group2", "customs"]
    },
    {
      "name": "EXP_OTHERS_V_1",
      "displayName": "Export Others Vishal (Group 1)",
      "description": "Vishal variant for customs: INDEL4, INDPC4, INNSA1, INBOM4",
      "schema": "dbo",
      "enabled": true,
      "defaultFormat": "Excel",
      "includeInQuickExport": false,
      "customQuery": "",
      "tags": ["export", "vishal", "group1"]
    },
    {
      "name": "EXP_OTHERS_V_2",
      "displayName": "Export Others Vishal (Group 2)",
      "description": "Vishal variant for remaining customs offices",
      "schema": "dbo",
      "enabled": true,
      "defaultFormat": "Excel",
      "includeInQuickExport": false,
      "customQuery": "",
      "tags": ["export", "vishal", "group2"]
    }
  ]
}
```

### Table Definition Properties

<table>
<tr>
<th>Property</th>
<th>Type</th>
<th>Required</th>
<th>Description</th>
</tr>
<tr>
<td><code>name</code></td>
<td>string</td>
<td>✅</td>
<td>Actual database table name</td>
</tr>
<tr>
<td><code>displayName</code></td>
<td>string</td>
<td>✅</td>
<td>User-friendly name shown in UI</td>
</tr>
<tr>
<td><code>description</code></td>
<td>string</td>
<td>❌</td>
<td>Detailed description (tooltip)</td>
</tr>
<tr>
<td><code>schema</code></td>
<td>string</td>
<td>❌</td>
<td>Database schema (default: "dbo")</td>
</tr>
<tr>
<td><code>enabled</code></td>
<td>boolean</td>
<td>❌</td>
<td>Show in UI (default: true)</td>
</tr>
<tr>
<td><code>defaultFormat</code></td>
<td>enum</td>
<td>❌</td>
<td>Excel, CSV, TXT (default: Excel)</td>
</tr>
<tr>
<td><code>includeInQuickExport</code></td>
<td>boolean</td>
<td>❌</td>
<td>Show in quick export list</td>
</tr>
<tr>
<td><code>customQuery</code></td>
<td>string</td>
<td>❌</td>
<td>Custom SQL query (overrides table)</td>
</tr>
<tr>
<td><code>tags</code></td>
<td>array</td>
<td>❌</td>
<td>Categorization tags</td>
</tr>
</table>

### Using Custom Queries

<div style="background: #02111B; padding: 20px; border-radius: 8px;">

**Custom Query Example:**

```json
{
  "name": "CUSTOM_EXPORT",
  "displayName": "Filtered Export",
  "description": "Last 30 days of data only",
  "customQuery": "SELECT * FROM EXP_OTHERS WHERE ExportDate >= DATEADD(DAY, -30, GETDATE())",
  "defaultFormat": "Excel"
}
```

**Benefits:**
- Apply filters before export
- Join multiple tables
- Calculated columns
- Data transformations

**⚠️ Cautions:**
- Ensure query is optimized
- Validate syntax carefully
- Test with small datasets first
- Consider query timeouts

</div>

---

## 🔧 Export Procedure Definitions

### File: `export_procedures.json`

<div style="background: #024059; padding: 20px; border-radius: 8px; border-left: 4px solid #00E5FF;">

**Procedure Definition Structure:**

```json
{
  "procedures": [
    {
      "name": "PROCEDURE_NAME",
      "displayName": "Friendly Display Name",
      "description": "Detailed description",
      "schema": "dbo",
      "enabled": true,
      "parameters": [
        {
          "name": "@parameter",
          "type": "int",
          "required": true,
          "description": "Parameter description",
          "defaultValue": null,
          "validation": {
            "minValue": 0,
            "maxValue": 99999999,
            "pattern": null
          }
        }
      ],
      "outputTables": ["TABLE1", "TABLE2"],
      "estimatedDuration": "2-5 minutes",
      "tags": ["monthly", "export"]
    }
  ]
}
```

</div>

### Complete Example

```json
{
  "procedures": [
    {
      "name": "Others_EXP_10Days",
      "displayName": "Standard Export Processing",
      "description": "Processes export data with standard filtering and anonymization",
      "schema": "dbo",
      "enabled": true,
      "parameters": [
        {
          "name": "@mon",
          "type": "int",
          "required": true,
          "description": "Start date in YYYYMMDD format (e.g., 20241101)",
          "defaultValue": null,
          "validation": {
            "minValue": 20000101,
            "maxValue": 21001231,
            "pattern": "^\\d{8}$"
          }
        },
        {
          "name": "@mon1",
          "type": "int",
          "required": true,
          "description": "End date in YYYYMMDD format (e.g., 20241130)",
          "defaultValue": null,
          "validation": {
            "minValue": 20000101,
            "maxValue": 21001231,
            "pattern": "^\\d{8}$"
          }
        }
      ],
      "outputTables": ["EXP_OTHERS", "EXP_OTHERS_1", "EXP_OTHERS_2"],
      "estimatedDuration": "2-5 minutes",
      "tags": ["standard", "monthly", "export"]
    },
    {
      "name": "Others_EXP_10Days_Kushal",
      "displayName": "Kushal Export Processing",
      "description": "Export processing with Kushal-specific anonymization logic",
      "schema": "dbo",
      "enabled": true,
      "parameters": [
        {
          "name": "@mon",
          "type": "int",
          "required": true,
          "description": "Start date in YYYYMMDD format",
          "defaultValue": null,
          "validation": {
            "minValue": 20000101,
            "maxValue": 21001231,
            "pattern": "^\\d{8}$"
          }
        },
        {
          "name": "@mon1",
          "type": "int",
          "required": true,
          "description": "End date in YYYYMMDD format",
          "defaultValue": null,
          "validation": {
            "minValue": 20000101,
            "maxValue": 21001231,
            "pattern": "^\\d{8}$"
          }
        }
      ],
      "outputTables": ["EXP_OTHERS", "EXP_OTHERS_1", "EXP_OTHERS_2"],
      "estimatedDuration": "2-5 minutes",
      "tags": ["kushal", "monthly", "export"]
    },
    {
      "name": "Others_EXP_10Days_Vishal",
      "displayName": "Vishal Export Processing",
      "description": "Export processing with Vishal-specific data consolidation",
      "schema": "dbo",
      "enabled": true,
      "parameters": [
        {
          "name": "@mon",
          "type": "int",
          "required": true,
          "description": "Start date in YYYYMMDD format",
          "defaultValue": null,
          "validation": {
            "minValue": 20000101,
            "maxValue": 21001231,
            "pattern": "^\\d{8}$"
          }
        },
        {
          "name": "@mon1",
          "type": "int",
          "required": true,
          "description": "End date in YYYYMMDD format",
          "defaultValue": null,
          "validation": {
            "minValue": 20000101,
            "maxValue": 21001231,
            "pattern": "^\\d{8}$"
          }
        }
      ],
      "outputTables": ["EXP_OTHERS_V_1", "EXP_OTHERS_V_2"],
      "estimatedDuration": "3-6 minutes",
      "tags": ["vishal", "monthly", "export"]
    }
  ]
}
```

### Parameter Configuration

<div style="background: linear-gradient(to right, #02111B, #024059); padding: 20px; border-radius: 10px;">

**Parameter Types and Validation:**

| Type | SQL Type | Validation Options | Example |
|------|----------|-------------------|---------|
| `int` | INT | minValue, maxValue, pattern | 20241101 |
| `bigint` | BIGINT | minValue, maxValue | 123456789 |
| `varchar` | VARCHAR/NVARCHAR | maxLength, pattern | "INDEL4" |
| `decimal` | DECIMAL/NUMERIC | minValue, maxValue, precision | 123.45 |
| `date` | DATE | minDate, maxDate, format | 2024-11-01 |
| `datetime` | DATETIME | minDate, maxDate, format | 2024-11-01 12:00:00 |
| `bit` | BIT | - | true/false |

</div>

**Validation Examples:**

```json
{
  "parameters": [
    {
      "name": "@startDate",
      "type": "int",
      "validation": {
        "minValue": 20200101,
        "maxValue": 21001231,
        "pattern": "^\\d{8}$"
      }
    },
    {
      "name": "@customsCode",
      "type": "varchar",
      "validation": {
        "maxLength": 10,
        "pattern": "^IN[A-Z]{3}\\d$"
      }
    },
    {
      "name": "@threshold",
      "type": "decimal",
      "validation": {
        "minValue": 0,
        "maxValue": 999999.99,
        "precision": 2
      }
    }
  ]
}
```

---

## 📥 Import Configurations

### Files: `import_tables.json` & `import_procedures.json`

<div style="background: #02111B; padding: 20px; border-radius: 8px;">

**Import Table Configuration:**

```json
{
  "tables": [
    {
      "name": "IMP_TRADE_DATA",
      "displayName": "Import Trade Data",
      "description": "Import trade transaction data",
      "schema": "dbo",
      "enabled": true,
      "supportedFormats": ["Excel", "CSV"],
      "columnMappings": [
        {
          "sourceColumn": "Transaction Date",
          "targetColumn": "TransactionDate",
          "dataType": "date",
          "required": true,
          "defaultValue": null
        },
        {
          "sourceColumn": "Amount",
          "targetColumn": "Amount",
          "dataType": "decimal",
          "required": true,
          "defaultValue": 0
        }
      ],
      "validationRules": [
        {
          "column": "Amount",
          "rule": "greaterThan",
          "value": 0
        }
      ]
    }
  ]
}
```

**Import Procedure Configuration:**

```json
{
  "procedures": [
    {
      "name": "ImportTradeData",
      "displayName": "Import Trade Data Procedure",
      "description": "Bulk import with validation",
      "parameters": [
        {
          "name": "@filePath",
          "type": "varchar",
          "required": true,
          "description": "Full path to import file"
        },
        {
          "name": "@validateOnly",
          "type": "bit",
          "required": false,
          "defaultValue": false,
          "description": "Validate without importing"
        }
      ]
    }
  ]
}
```

</div>

---

## ⚡ Performance Tuning

### Performance Tuning Guide

<div style="background: #024059; padding: 20px; border-radius: 8px; border-left: 4px solid #00E5FF;">

**System-Specific Recommendations:**

**💻 System: 4GB RAM, HDD**
```json
{
  "performance": {
    "batchSize": 10000,
    "memoryThresholdMB": 256,
    "enableAsyncExport": true,
    "maxConcurrentExports": 1,
    "commandTimeout": 600
  }
}
```

**💻 System: 8GB RAM, SSD**
```json
{
  "performance": {
    "batchSize": 50000,
    "memoryThresholdMB": 512,
    "enableAsyncExport": true,
    "maxConcurrentExports": 3,
    "commandTimeout": 300
  }
}
```

**💻 System: 16GB+ RAM, NVMe SSD**
```json
{
  "performance": {
    "batchSize": 100000,
    "memoryThresholdMB": 1024,
    "enableAsyncExport": true,
    "maxConcurrentExports": 5,
    "commandTimeout": 300
  }
}
```

</div>

### Tuning Methodology

1. **Baseline Testing**
   - Start with default settings
   - Measure export time
   - Monitor memory usage

2. **Incremental Adjustment**
   - Change one parameter at a time
   - Test with representative data
   - Document results

3. **Optimization Targets**
   - Export speed (seconds per 100K rows)
   - Memory usage (peak MB)
   - UI responsiveness (lag in ms)

---

## 📁 Path Configuration

### Custom Path Settings

<div style="background: linear-gradient(135deg, #024059 0%, #02111B 100%); padding: 15px; border-radius: 8px;">

**Network Share Example:**
```json
{
  "paths": {
    "exports": "\\\\FileServer\\Shares\\TradeData\\Exports\\",
    "imports": "\\\\FileServer\\Shares\\TradeData\\Imports\\",
    "logs": "C:\\ProgramData\\TradeDataStudio\\Logs\\",
    "config": "./config/"
  }
}
```

**External Drive Example:**
```json
{
  "paths": {
    "exports": "E:\\TradeDataExports\\",
    "imports": "E:\\TradeDataImports\\",
    "logs": "./logs/",
    "config": "./config/"
  }
}
```

**⚠️ Path Requirements:**
- Use forward slashes (/) or escaped backslashes (\\\\)
- Ensure write permissions
- UNC paths supported
- Paths created automatically if missing

</div>

---

## 📝 Logging Configuration

### Log Level Details

<div style="background: #02111B; padding: 20px; border-radius: 8px;">

| Level | Verbosity | Use Case | Performance Impact |
|-------|-----------|----------|-------------------|
| **Error** | Minimal | Production (errors only) | Negligible |
| **Warning** | Low | Production (stable) | Very Low |
| **Information** | Medium | Production (recommended) | Low |
| **Debug** | High | Development/Troubleshooting | Medium |
| **Trace** | Maximum | Detailed diagnostics | High |

**Configuration:**
```json
{
  "application": {
    "logLevel": "Information"
  }
}
```

</div>

---

<div align="center">

## 🎯 Configuration Quick Reference

```
╔══════════════════════════════════════════════════════════╗
║                 CONFIGURATION FILES                      ║
╠══════════════════════════════════════════════════════════╣
║                                                          ║
║  appsettings.json        Application behavior           ║
║  database.json           Connection settings            ║
║  export_tables.json      Table definitions              ║
║  export_procedures.json  Procedure definitions          ║
║  import_tables.json      Import configurations          ║
║  import_procedures.json  Import procedure configs       ║
║                                                          ║
╚══════════════════════════════════════════════════════════╝
```

---

**📚 Related Documentation:**
- [User Guide](./USER_GUIDE.md)
- [Troubleshooting Guide](./TROUBLESHOOTING.md)
- [Installation Guide](../deployment/INSTALLATION.md)

---

<p align="center">
  <img src="https://img.shields.io/badge/Configuration-Complete-00E5FF?style=for-the-badge" alt="Configuration Complete"/>
</p>

</div>
