<div align="center">

```
╔═══════════════════════════════════════════════════════════════╗
║                                                               ║
║   ████████╗██████╗  █████╗ ██████╗ ███████╗                   ║
║   ╚══██╔══╝██╔══██╗██╔══██╗██╔══██╗██╔════╝                   ║
║      ██║   ██████╔╝███████║██║  ██║█████╗                     ║
║      ██║   ██╔══██╗██╔══██║██║  ██║██╔══╝                     ║
║      ██║   ██║  ██║██║  ██║██████╔╝███████╗                   ║
║      ╚═╝   ╚═╝  ╚═╝╚═╝  ╚═╝╚═════╝ ╚══════╝                   ║
║                                                               ║
║            ██╗   ██╗███████╗███████╗██████╗                   ║
║            ██║   ██║██╔════╝██╔════╝██╔══██╗                  ║
║            ██║   ██║███████╗█████╗  ██████╔╝                  ║
║            ██║   ██║╚════██║██╔══╝  ██╔══██╗                  ║
║            ╚██████╔╝███████║███████╗██║  ██║                  ║
║             ╚═════╝ ╚══════╝╚══════╝╚═╝  ╚═╝                  ║   
║                                                               ║
║             ██████╗ ██╗   ██╗██╗██████╗ ███████╗              ║
║            ██╔════╝ ██║   ██║██║██╔══██╗██╔════╝              ║
║            ██║  ███╗██║   ██║██║██║  ██║█████╗                ║
║            ██║   ██║██║   ██║██║██║  ██║██╔══╝                ║
║            ╚██████╔╝╚██████╔╝██║██████╔╝███████╗              ║
║             ╚═════╝  ╚═════╝ ╚═╝╚═════╝ ╚══════╝              ║
║                                                               ║
╚═══════════════════════════════════════════════════════════════╝
```

# User Guide

<p align="center">
  <img src="https://img.shields.io/badge/Version-1.0.0-00E5FF?style=for-the-badge" alt="Version"/>
  <img src="https://img.shields.io/badge/Updated-December_2025-02111B?style=for-the-badge" alt="Updated"/>
  <img src="https://img.shields.io/badge/Level-Beginner_to_Advanced-00FF87?style=for-the-badge" alt="Level"/>
</p>

</div>

---

## 📑 Table of Contents

1. [Getting Started](#-getting-started)
2. [User Interface Overview](#-user-interface-overview)
3. [Export Operations](#-export-operations)
4. [Import Operations](#-import-operations)
5. [Database Connection](#-database-connection)
6. [Stored Procedures](#-stored-procedures)
7. [Activity Logs](#-activity-logs)
8. [Settings & Configuration](#️-settings--configuration)
9. [Best Practices](#-best-practices)
10. [Tips & Tricks](#-tips--tricks)

---

## 🚀 Getting Started

### First Launch

When you launch TradeData Studio for the first time:

<div style="background: linear-gradient(135deg, #02111B 0%, #024059 100%); padding: 20px; border-radius: 10px; border-left: 4px solid #00E5FF;">

**Step 1: Verify Installation**
```
✓ Application starts successfully
✓ Main window displays
✓ Default mode is "Export"
✓ Config files are loaded
```

**Step 2: Configure Database Connection**
- Navigate to Settings (Ctrl+O)
- Enter your SQL Server details
- Test connection (Ctrl+T)
- Save settings

**Step 3: Verify Directories**
```
TradeDataStudio/
├── config/   ✓ Configuration files present
├── exports/  ✓ Ready for export files
├── imports/  ✓ Ready for import files
└── logs/     ✓ Logging initialized
```

</div>

### Quick Start Checklist

- [ ] Application installed and launched
- [ ] Database connection configured
- [ ] Connection tested successfully
- [ ] Export/Import directories accessible
- [ ] Sample export completed
- [ ] Activity log reviewed

---

## 🖥️ User Interface Overview

### Main Window Layout

```
┌─────────────────────────────────────────────────────────────────┐
│  TradeData Studio                                    [_] [□] [X] │
├─────────────────────────────────────────────────────────────────┤
│  ┌──────────────────────────────────────────────────────────┐  │
│  │  Mode Selector:  ○ Export    ○ Import                    │  │
│  └──────────────────────────────────────────────────────────┘  │
├─────────────────────────────────────────────────────────────────┤
│  ┌────────────────────────┐  ┌────────────────────────────┐   │
│  │   Operation Panel      │  │   Configuration Panel      │   │
│  │                        │  │                            │   │
│  │  • Table Export        │  │  Parameters:               │   │
│  │  • Procedure Export    │  │  ┌──────────────────────┐ │   │
│  │  • Quick Export        │  │  │ @mon: 20241101       │ │   │
│  │                        │  │  │ @mon1: 20241130      │ │   │
│  │  [Export to Excel]     │  │  └──────────────────────┘ │   │
│  │  [Export to CSV]       │  │                            │   │
│  │  [Export to TXT]       │  │  Format: [Excel ▼]        │   │
│  └────────────────────────┘  └────────────────────────────┘   │
├─────────────────────────────────────────────────────────────────┤
│  ┌──────────────────────────────────────────────────────────┐  │
│  │  Activity Log                                            │  │
│  │  ──────────────────────────────────────────────────────  │  │
│  │  [12:34:56] Export started for table EXP_OTHERS_1        │  │
│  │  [12:35:12] Processing 125,000 rows...                   │  │
│  │  [12:35:45] Export completed: export_20241201.xlsx       │  │
│  │  [12:35:46] Execution time: 49 seconds                   │  │
│  └──────────────────────────────────────────────────────────┘  │
├─────────────────────────────────────────────────────────────────┤
│  Status: Ready  |  Database: Connected  |  v1.0.0              │
└─────────────────────────────────────────────────────────────────┘
```

### Key UI Elements

<table>
<tr>
<th width="25%">Element</th>
<th width="75%">Description</th>
</tr>
<tr>
<td><b>🎯 Mode Selector</b></td>
<td>Switch between Export and Import modes. Changes available operations dynamically.</td>
</tr>
<tr>
<td><b>📋 Operation Panel</b></td>
<td>Lists available operations based on current mode (tables, procedures, quick actions).</td>
</tr>
<tr>
<td><b>⚙️ Configuration Panel</b></td>
<td>Input parameters, select output format, configure operation-specific settings.</td>
</tr>
<tr>
<td><b>📊 Activity Log</b></td>
<td>Real-time display of operations, progress, and results. Shows timestamps and details.</td>
</tr>
<tr>
<td><b>📈 Progress Bar</b></td>
<td>Visual indicator of current operation progress (appears during long operations).</td>
</tr>
<tr>
<td><b>🔔 Status Bar</b></td>
<td>Shows connection status, current mode, version, and quick stats.</td>
</tr>
</table>

---

## 📤 Export Operations

### Table Export

Export data directly from database tables with full control over format and output.

#### **Basic Table Export**

<div style="background: #02111B; padding: 20px; border-radius: 8px;">

**Step-by-Step Process:**

1. **Select Export Mode**
   - Ensure "Export" is selected in mode selector
   
2. **Choose Table**
   - Browse available tables in operation panel
   - Example tables:
     - `EXP_OTHERS_1` - Export Others (Group 1)
     - `EXP_OTHERS_2` - Export Others (Group 2)
     - `EXP_OTHERS_V_1` - Vishal variant (Group 1)
     - `EXP_OTHERS_V_2` - Vishal variant (Group 2)

3. **Select Output Format**
   ```
   ┌─────────────────────────┐
   │ Format:  [Excel ▼]      │
   │                         │
   │ Options:                │
   │ • Excel (.xlsx)         │
   │ • CSV (.csv)            │
   │ • TXT (.txt)            │
   └─────────────────────────┘
   ```

4. **Configure Options**
   - **Include Headers**: ✓ (Recommended)
   - **Date Format**: YYYY-MM-DD or custom
   - **Delimiter** (CSV/TXT): Comma, Tab, Pipe

5. **Execute Export**
   - Click "Export" button or press `Ctrl+E`
   - Monitor progress in activity log
   - Wait for completion notification

6. **Access Results**
   - Files saved to: `exports/`
   - Filename format: `tablename_YYYYMMDD_HHMMSS.ext`
   - Example: `EXP_OTHERS_1_20241201_123456.xlsx`

</div>

#### **Advanced Table Export Options**

<details>
<summary><b>📊 Excel Export Features</b></summary>

**Excel-Specific Options:**
- Auto-fit columns for readability
- Freeze header row for easy scrolling
- Apply basic formatting (borders, headers)
- Data type preservation (numbers, dates, text)
- Multiple sheets for large datasets (1M+ rows)

**Excel Limitations:**
- Maximum 1,048,576 rows per sheet
- Large datasets automatically split across sheets
- Sheet naming: `Data_1`, `Data_2`, etc.

**Best For:**
- Data analysis and visualization
- Sharing with business users
- Reports requiring formatting
- Datasets under 1 million rows

</details>

<details>
<summary><b>📄 CSV Export Features</b></summary>

**CSV-Specific Options:**
- Custom delimiter selection
- Quote character configuration
- UTF-8 encoding with BOM support
- Line ending style (Windows/Unix)

**CSV Advantages:**
- Universal compatibility
- Smaller file size
- Unlimited row capacity
- Fast processing speed

**Best For:**
- Data imports to other systems
- Very large datasets
- Automated processing
- Cross-platform compatibility

</details>

<details>
<summary><b>📝 TXT Export Features</b></summary>

**TXT-Specific Options:**
- Custom field delimiter
- Custom record separator
- Fixed-width format option
- Custom header format

**Best For:**
- Legacy system integration
- Custom format requirements
- Plain text processing
- Mainframe imports

</details>

---

### Stored Procedure Export

Execute predefined stored procedures with parameter validation and multiple output handling.

#### **Executing Stored Procedures**

<div style="background: linear-gradient(to right, #02111B, #024059); padding: 20px; border-radius: 10px;">

**Available Procedures:**

1. **Others_EXP_10Days** - Standard Export Processing
   - Parameters:
     - `@mon` (int): Start date (YYYYMMDD format)
     - `@mon1` (int): End date (YYYYMMDD format)
   - Output Tables: `EXP_OTHERS`, `EXP_OTHERS_1`, `EXP_OTHERS_2`
   - Use Case: Standard monthly exports with anonymization

2. **Others_EXP_10Days_Kushal** - Kushal Export Processing
   - Parameters:
     - `@mon` (int): Start date
     - `@mon1` (int): End date
   - Output Tables: Same as standard
   - Use Case: Kushal-specific anonymization logic

3. **Others_EXP_10Days_Vishal** - Vishal Export Processing
   - Parameters:
     - `@mon` (int): Start date
     - `@mon1` (int): End date
   - Output Tables: `EXP_OTHERS_V_1`, `EXP_OTHERS_V_2`
   - Use Case: Vishal-specific data consolidation

</div>

#### **Step-by-Step Procedure Execution**

```
Step 1: Select Procedure
┌────────────────────────────────────────┐
│ Stored Procedures                      │
│ ● Others_EXP_10Days                   │
│ ○ Others_EXP_10Days_Kushal            │
│ ○ Others_EXP_10Days_Vishal            │
└────────────────────────────────────────┘

Step 2: Enter Parameters
┌────────────────────────────────────────┐
│ Parameters                             │
│ ┌────────────────────────────────────┐ │
│ │ @mon:  [20241101            ]      │ │
│ │        Start date (YYYYMMDD)       │ │
│ │                                    │ │
│ │ @mon1: [20241130            ]      │ │
│ │        End date (YYYYMMDD)         │ │
│ └────────────────────────────────────┘ │
│                                        │
│ [Validate Parameters]                  │
└────────────────────────────────────────┘

Step 3: Select Export Tables
┌────────────────────────────────────────┐
│ Output Tables                          │
│ ☑ EXP_OTHERS                          │
│ ☑ EXP_OTHERS_1                        │
│ ☑ EXP_OTHERS_2                        │
│                                        │
│ Select All | Deselect All             │
└────────────────────────────────────────┘

Step 4: Configure Export
┌────────────────────────────────────────┐
│ Export Settings                        │
│ Format: [Excel     ▼]                 │
│ Include Headers: ☑                    │
│ Timestamp Files: ☑                    │
└────────────────────────────────────────┘

Step 5: Execute
┌────────────────────────────────────────┐
│ [Execute Procedure]  [Ctrl+S]         │
└────────────────────────────────────────┘
```

#### **Parameter Validation**

TradeData Studio automatically validates parameters before execution:

| Validation | Description | Example |
|-----------|-------------|---------|
| **Type Checking** | Ensures correct data type | int, varchar, date |
| **Required Fields** | Verifies all required params | @mon, @mon1 must be provided |
| **Date Format** | Validates date formats | YYYYMMDD: 20241101 |
| **Range Validation** | Checks logical ranges | Start date < End date |
| **SQL Injection** | Prevents malicious input | Sanitizes all inputs |

**Validation Feedback:**
```
✓ Parameter @mon validated: 20241101
✓ Parameter @mon1 validated: 20241130
✓ Date range valid: 30 days
✓ All required parameters provided
✓ Ready to execute
```

---

### Quick Export

Shortcut buttons for frequently used export operations.

<div style="background: #024059; padding: 15px; border-radius: 8px; border-left: 4px solid #00E5FF;">

**Quick Actions:**

| Button | Shortcut | Action |
|--------|----------|--------|
| 📗 **Export to Excel** | `Ctrl+E` | Export selected table/procedure to Excel format |
| 📄 **Export to CSV** | `Ctrl+X` | Export selected table/procedure to CSV format |
| 📝 **Export to TXT** | `Ctrl+Shift+X` | Export selected table/procedure to TXT format |

**Usage:**
1. Select your data source (table or procedure result)
2. Press the corresponding quick action button
3. Export executes with default settings
4. File saved to `exports/` folder

**When to Use Quick Export:**
- Frequent exports of the same data
- Time-sensitive operations
- Standard formatting requirements
- Familiar with default settings

</div>

---

## 📥 Import Operations

### Table Import

Import data from files directly into database tables.

#### **Supported Import Formats**

<table>
<tr>
<th>Format</th>
<th>Extensions</th>
<th>Features</th>
</tr>
<tr>
<td><b>Excel</b></td>
<td>.xlsx, .xls</td>
<td>Multiple sheets, formatted data, automatic type detection</td>
</tr>
<tr>
<td><b>CSV</b></td>
<td>.csv</td>
<td>Various delimiters, quote handling, header detection</td>
</tr>
<tr>
<td><b>TXT</b></td>
<td>.txt</td>
<td>Custom delimiters, fixed-width, flexible formatting</td>
</tr>
</table>

#### **Import Process**

<div style="background: #02111B; padding: 20px; border-radius: 8px;">

**Step 1: Select Import Mode**
```
Switch to Import mode using mode selector
```

**Step 2: Choose Target Table**
```
Select destination table from available import tables
```

**Step 3: Select Source File**
```
┌────────────────────────────────────────┐
│ Import File Selection                  │
│                                        │
│ File: [Browse...                    ] │
│                                        │
│ Recent Files:                          │
│ • import_data_20241130.xlsx           │
│ • monthly_data.csv                    │
│ • custom_export.txt                   │
└────────────────────────────────────────┘
```

**Step 4: Configure Import Settings**
```
┌────────────────────────────────────────┐
│ Import Configuration                   │
│                                        │
│ ☑ First row contains headers          │
│ ☑ Validate data types                 │
│ ☑ Skip invalid rows                   │
│ ☐ Truncate table before import        │
│                                        │
│ Error Handling:                        │
│ ○ Stop on first error                 │
│ ● Continue and log errors             │
│ ○ Skip errors silently                │
└────────────────────────────────────────┘
```

**Step 5: Preview Data**
```
┌────────────────────────────────────────┐
│ Data Preview (First 10 rows)          │
│ ────────────────────────────────────── │
│ Col1      | Col2      | Col3    | ...  │
│ ────────────────────────────────────── │
│ Value1    | Value2    | Value3  | ...  │
│ Value4    | Value5    | Value6  | ...  │
│ ...                                    │
└────────────────────────────────────────┘
```

**Step 6: Execute Import**
```
[Import Data]  or  [Ctrl+I]
```

**Step 7: Review Results**
```
Import Summary:
✓ Total Rows: 125,000
✓ Imported Successfully: 124,987
⚠ Skipped (errors): 13
✓ Execution Time: 2m 15s
```

</div>

---

## 🔌 Database Connection

### Connection Configuration

#### **Windows Authentication** (Recommended)

<div style="background: linear-gradient(135deg, #024059 0%, #02111B 100%); padding: 15px; border-radius: 8px;">

**Configuration: `config/database.json`**

```json
{
  "server": "YOUR_SERVER\\INSTANCE",
  "database": "YOUR_DATABASE",
  "username": "",
  "password": "",
  "useWindowsAuthentication": true,
  "connectionTimeout": 30,
  "trustServerCertificate": true
}
```

**Advantages:**
- ✓ No password storage required
- ✓ Uses Windows security
- ✓ Single sign-on capability
- ✓ Inherits AD permissions

**Requirements:**
- Windows domain account
- SQL Server configured for Windows Auth
- Appropriate database permissions

</div>

#### **SQL Authentication**

<div style="background: #02111B; padding: 15px; border-radius: 8px; border: 1px solid #00E5FF;">

**Configuration: `config/database.json`**

```json
{
  "server": "YOUR_SERVER\\INSTANCE",
  "database": "YOUR_DATABASE",
  "username": "sql_username",
  "password": "sql_password",
  "useWindowsAuthentication": false,
  "connectionTimeout": 30,
  "trustServerCertificate": true
}
```

**⚠️ Security Considerations:**
- Password stored in configuration file
- Use strong passwords
- Restrict file permissions
- Consider encryption for production
- Use dedicated SQL account with minimum required permissions

</div>

### Testing Connection

**Method 1: Using Keyboard Shortcut**
```
Press Ctrl+T
```

**Method 2: Using Menu**
```
File → Test Connection
```

**Method 3: Using Settings Panel**
```
Settings → Database → Test Connection Button
```

**Connection Test Results:**

<div style="background: #024059; padding: 15px; border-radius: 8px;">

**✅ Success:**
```
Connection Test Results
────────────────────────
Status: Connected
Server: MATRIX\MATRIX
Database: RAW_PROCESS
Response Time: 125ms
SQL Server Version: 15.0.4345.5
Authentication: Windows
```

**❌ Failure:**
```
Connection Test Results
────────────────────────
Status: Failed
Error: Login failed for user
Details: Cannot open database
Suggestion: Check credentials and permissions
```

</div>

### Connection Troubleshooting

<details>
<summary><b>🔍 Common Connection Issues</b></summary>

**Issue 1: "Server not found"**
- Verify server name and instance
- Check network connectivity
- Ensure SQL Server is running
- Verify firewall settings

**Issue 2: "Login failed"**
- Verify username/password
- Check authentication mode
- Ensure user has database access
- Review SQL Server error logs

**Issue 3: "Timeout expired"**
- Increase `connectionTimeout` value
- Check network latency
- Verify server load
- Review long-running queries

**Issue 4: "Certificate validation failed"**
- Set `trustServerCertificate: true`
- Install proper certificates
- Update SQL Server configuration
- Check TLS settings

</details>

---

## 🔧 Stored Procedures

### Understanding Stored Procedures

Stored procedures in TradeData Studio are predefined database operations that:
- Process data according to business rules
- Apply transformations and anonymization
- Generate multiple output tables
- Ensure data consistency
- Improve performance

### Parameter Types

<table>
<tr>
<th>Type</th>
<th>Format</th>
<th>Example</th>
<th>Validation</th>
</tr>
<tr>
<td><b>int</b></td>
<td>Integer number</td>
<td>20241101</td>
<td>Numeric, range checks</td>
</tr>
<tr>
<td><b>varchar</b></td>
<td>Text string</td>
<td>"INDEL4"</td>
<td>Length, pattern matching</td>
</tr>
<tr>
<td><b>date</b></td>
<td>YYYY-MM-DD</td>
<td>2024-11-01</td>
<td>Date format, range</td>
</tr>
<tr>
<td><b>datetime</b></td>
<td>YYYY-MM-DD HH:MM:SS</td>
<td>2024-11-01 12:00:00</td>
<td>DateTime format</td>
</tr>
</table>

### Date Format Guidelines

**Standard Format: YYYYMMDD**

<div style="background: #02111B; padding: 15px; border-radius: 8px;">

**Examples:**
```
✓ Correct:
  20241101 - November 1, 2024
  20241231 - December 31, 2024
  20250101 - January 1, 2025

✗ Incorrect:
  2024-11-01  (Contains dashes)
  01112024    (Wrong format)
  11012024    (Month/Day/Year)
  241101      (2-digit year)
```

**Tips:**
- Always use 4-digit year
- Month: 01-12 (leading zero)
- Day: 01-31 (leading zero)
- No separators (dashes, slashes)

</div>

### Procedure Output Handling

When executing procedures that generate multiple tables:

1. **Automatic Detection**
   - Application detects all output tables
   - Lists available tables for export

2. **Selective Export**
   - Choose which tables to export
   - Select different formats per table
   - Export all at once or individually

3. **Result Organization**
   ```
   exports/
   ├── Others_EXP_10Days_20241201_123456/
   │   ├── EXP_OTHERS.xlsx
   │   ├── EXP_OTHERS_1.xlsx
   │   └── EXP_OTHERS_2.xlsx
   ```

---

## 📊 Activity Logs

### Log Panel

The Activity Log panel provides real-time feedback on operations:

<div style="background: linear-gradient(to right, #02111B, #024059); padding: 20px; border-radius: 10px;">

**Sample Log Output:**
```
[12:34:56] INFO  | Application started in Export mode
[12:35:01] INFO  | Database connection established
[12:35:15] INFO  | Procedure selected: Others_EXP_10Days
[12:35:20] INFO  | Parameters validated: @mon=20241101, @mon1=20241130
[12:35:25] INFO  | Executing stored procedure...
[12:35:26] INFO  | Procedure execution completed (1.2s)
[12:35:27] INFO  | Detected 3 output tables
[12:35:30] INFO  | Exporting EXP_OTHERS to Excel...
[12:36:15] INFO  | ├─ Processing: 125,000 rows
[12:37:02] INFO  | ├─ File created: EXP_OTHERS_20241201_123530.xlsx
[12:37:03] INFO  | └─ Export completed (47s)
[12:37:05] INFO  | All operations completed successfully
```

</div>

### Log Levels

<table>
<tr>
<th>Level</th>
<th>Color</th>
<th>Description</th>
<th>Examples</th>
</tr>
<tr>
<td><b>INFO</b></td>
<td>🟢 Green</td>
<td>Normal operations</td>
<td>Started, Completed, Connected</td>
</tr>
<tr>
<td><b>WARNING</b></td>
<td>🟡 Yellow</td>
<td>Potential issues</td>
<td>Slow query, Large dataset, Deprecated feature</td>
</tr>
<tr>
<td><b>ERROR</b></td>
<td>🔴 Red</td>
<td>Operation failures</td>
<td>Connection failed, Invalid parameter, Access denied</td>
</tr>
<tr>
<td><b>DEBUG</b></td>
<td>🔵 Blue</td>
<td>Detailed diagnostics</td>
<td>SQL queries, Parameter values, Internal state</td>
</tr>
</table>

### Log Files

Persistent logs are saved to the `logs/` directory:

**File Structure:**
```
logs/
├── application.log          # All log levels (rolling)
├── error.log               # Errors only
├── debug.log               # Debug information
└── archived/               # Rotated log files
    ├── application_20241130.log
    └── application_20241129.log
```

**Log Rotation:**
- Files rotate daily at midnight
- Maximum file size: 10 MB
- Retention period: 30 days
- Compressed archives for older logs

**Viewing Log Files:**

```powershell
# View recent logs
Get-Content logs/application.log -Tail 50

# Search for errors
Select-String -Path logs/error.log -Pattern "Export"

# View logs with timestamps
Get-Content logs/application.log | Where-Object { $_ -match "2024-12-01" }
```

---

## ⚙️ Settings & Configuration

### Application Settings

Located in `config/appsettings.json`:

<div style="background: #024059; padding: 20px; border-radius: 8px; border-left: 4px solid #00E5FF;">

**General Settings:**
```json
{
  "application": {
    "name": "TradeData Studio",
    "version": "1.0.0",
    "defaultMode": "Export",
    "logLevel": "Information",
    "exportFormats": ["Excel", "CSV", "TXT"]
  }
}
```

| Setting | Options | Description |
|---------|---------|-------------|
| `defaultMode` | Export, Import | Starting mode on launch |
| `logLevel` | Debug, Info, Warning, Error | Minimum log level |
| `exportFormats` | Array of formats | Available export options |

**Performance Settings:**
```json
{
  "performance": {
    "batchSize": 50000,
    "excelMaxRowsPerSheet": 1048576,
    "enableAsyncExport": true,
    "memoryThresholdMB": 512
  }
}
```

| Setting | Default | Range | Impact |
|---------|---------|-------|--------|
| `batchSize` | 50,000 | 1K-100K | Processing speed vs memory |
| `excelMaxRowsPerSheet` | 1,048,576 | Fixed | Excel limitation |
| `enableAsyncExport` | true | true/false | Non-blocking operations |
| `memoryThresholdMB` | 512 | 256-2048 | Memory usage limit |

</div>

### Performance Tuning

<details>
<summary><b>🚀 Optimizing for Speed</b></summary>

**For Fast Exports (Small to Medium Datasets):**
```json
{
  "batchSize": 100000,
  "enableAsyncExport": true,
  "memoryThresholdMB": 1024
}
```

**Benefits:**
- Larger batches = fewer I/O operations
- More memory = less disk swapping
- Async = responsive UI

**Best for:**
- Datasets under 1 million rows
- Systems with 8GB+ RAM
- SSD storage

</details>

<details>
<summary><b>💾 Optimizing for Memory</b></summary>

**For Memory-Constrained Systems:**
```json
{
  "batchSize": 25000,
  "enableAsyncExport": true,
  "memoryThresholdMB": 256
}
```

**Benefits:**
- Smaller batches = less memory per operation
- Lower threshold = triggers cleanup sooner
- Prevents out-of-memory errors

**Best for:**
- Systems with 4GB RAM
- Multiple concurrent applications
- Very large datasets (10M+ rows)

</details>

### Path Configuration

<div style="background: #02111B; padding: 15px; border-radius: 8px;">

**Default Paths:**
```json
{
  "paths": {
    "exports": "./exports/",
    "imports": "./imports/",
    "logs": "./logs/",
    "config": "./config/"
  }
}
```

**Custom Paths:**
```json
{
  "paths": {
    "exports": "D:/Data/TradeStudio/Exports/",
    "imports": "D:/Data/TradeStudio/Imports/",
    "logs": "C:/Logs/TradeStudio/",
    "config": "./config/"
  }
}
```

**Notes:**
- Use forward slashes (/) or escaped backslashes (\\\\)
- Absolute paths recommended for network locations
- Ensure write permissions on all paths
- UNC paths supported: `//server/share/folder/`

</div>

---

## 💡 Best Practices

### Export Best Practices

<div style="background: linear-gradient(135deg, #02111B 0%, #024059 100%); padding: 20px; border-radius: 10px; border-left: 4px solid #00E5FF;">

**1. Choose the Right Format**
- **Excel**: Analysis, presentations, formatted reports
- **CSV**: Data exchange, imports, large datasets
- **TXT**: Legacy systems, custom formats

**2. Optimize Export Size**
- Filter data at the database level
- Export only required columns
- Use date ranges to limit data
- Consider splitting very large exports

**3. Naming Conventions**
- Include date/time in filenames
- Use descriptive prefixes
- Maintain consistent format
- Example: `Monthly_Export_202412_v1.xlsx`

**4. Scheduling**
- Run large exports during off-peak hours
- Schedule resource-intensive procedures
- Monitor database load
- Use batch operations for multiple exports

**5. Data Validation**
- Preview data before full export
- Validate parameter values
- Check row counts post-export
- Review logs for warnings

**6. Performance**
- Adjust batch size based on dataset
- Monitor memory usage
- Use appropriate export format
- Close unnecessary applications

</div>

### Import Best Practices

<div style="background: #024059; padding: 20px; border-radius: 8px; border-left: 4px solid #00FF87;">

**1. Data Preparation**
- Validate source data format
- Remove duplicate headers
- Check for special characters
- Ensure encoding compatibility (UTF-8)

**2. Error Handling**
- Always enable error logging
- Review skipped rows
- Fix errors before re-import
- Keep backup of source files

**3. Performance**
- Import during low-traffic periods
- Use bulk insert when possible
- Disable indexes temporarily (large imports)
- Re-enable and rebuild indexes after import

**4. Validation**
- Verify row counts (source vs imported)
- Sample check imported data
- Run data quality checks
- Compare totals/aggregates

</div>

### Security Best Practices

<div style="background: #02111B; padding: 15px; border-radius: 8px; border: 1px solid #00E5FF;">

**1. Connection Security**
- ✅ Use Windows Authentication when possible
- ✅ Encrypt connection strings
- ✅ Restrict database user permissions
- ✅ Use dedicated service accounts

**2. File Security**
- ✅ Protect configuration files
- ✅ Secure export directories
- ✅ Review file permissions
- ✅ Delete temporary files

**3. Credential Management**
- ❌ Never commit passwords to source control
- ❌ Don't share configuration files
- ✅ Use environment variables
- ✅ Rotate credentials regularly

**4. Audit & Compliance**
- ✅ Enable comprehensive logging
- ✅ Review logs regularly
- ✅ Monitor unusual activities
- ✅ Maintain audit trail

</div>

---

## 🎓 Tips & Tricks

### Productivity Shortcuts

<div style="background: linear-gradient(to right, #02111B, #024059); padding: 20px; border-radius: 10px;">

**🔥 Power User Tips:**

1. **Batch Operations**
   ```
   Export multiple tables in one go:
   1. Select procedure with multiple outputs
   2. Check all desired output tables
   3. Execute once - all tables exported
   ```

2. **Template Parameters**
   ```
   Save frequently used parameter sets:
   - Create templates for common date ranges
   - Save custom filter combinations
   - Quick-load from templates
   ```

3. **Log Filtering**
   ```
   Ctrl+F in log panel:
   - Search for specific operations
   - Filter by log level
   - Find error messages quickly
   ```

4. **Quick Navigation**
   ```
   Tab: Move between fields
   Shift+Tab: Reverse navigation
   Enter: Execute default action
   Esc: Cancel operation
   ```

5. **Configuration Profiles**
   ```
   Maintain multiple database.json files:
   - database.dev.json
   - database.test.json
   - database.prod.json
   Swap as needed
   ```

</div>

### Time-Saving Workflows

<div style="background: #024059; padding: 15px; border-radius: 8px;">

**Monthly Export Routine:**
```
1. Start of Month:
   ├─ Execute EXP_10Days procedure
   ├─ Parameters: Previous month dates
   ├─ Export all tables to Excel
   └─ Archive to network share

2. Mid-Month Review:
   ├─ Run procedure for current month-to-date
   ├─ Quick export to CSV for analysis
   └─ Compare with previous month

3. Month-End Close:
   ├─ Final export with complete month data
   ├─ Generate reports in Excel format
   ├─ Backup configuration files
   └─ Review logs for any issues
```

</div>

### Advanced Techniques

<details>
<summary><b>🔬 Custom Export Scripts</b></summary>

Automate TradeData Studio using PowerShell:

```powershell
# Example: Automated Monthly Export
$startDate = Get-Date -Day 1 -Format "yyyyMMdd"
$endDate = Get-Date -Format "yyyyMMdd"

# Update configuration
$config = Get-Content "config\database.json" | ConvertFrom-Json
# Modify as needed
$config | ConvertTo-Json | Set-Content "config\database.json"

# Launch application (with automation API if available)
# Export, wait for completion, verify outputs
```

</details>

<details>
<summary><b>📊 Data Validation Scripts</b></summary>

Post-export validation:

```powershell
# Verify export file exists and has data
$exportFile = "exports\EXP_OTHERS_*.xlsx"
$file = Get-Item $exportFile -ErrorAction SilentlyContinue

if ($file -and $file.Length -gt 1KB) {
    Write-Host "✓ Export successful: $($file.Name)"
    Write-Host "  Size: $([math]::Round($file.Length/1MB, 2)) MB"
} else {
    Write-Host "✗ Export failed or empty"
}
```

</details>

### Troubleshooting Quick Reference

| Symptom | Likely Cause | Quick Fix |
|---------|--------------|-----------|
| Export hangs | Large dataset, insufficient memory | Reduce batch size, close other apps |
| Connection timeout | Network/server issue | Increase timeout, check connectivity |
| Invalid parameters | Wrong format | Verify date format (YYYYMMDD) |
| Permission denied | Insufficient rights | Check SQL permissions, file access |
| Slow export | Network/disk I/O | Export to local drive, run during off-peak |
| Out of memory | Dataset too large | Reduce batch size, increase memory threshold |

---

<div align="center">

## 🎯 Quick Reference Card

```
╔══════════════════════════════════════════════════════════════╗
║                    TRADEDATASTUDIO                           ║
║                    QUICK REFERENCE                           ║
╠══════════════════════════════════════════════════════════════╣
║                                                              ║
║  KEYBOARD SHORTCUTS              FILE LOCATIONS              ║
║  ─────────────────────           ──────────────              ║
║  Ctrl+E  Export to Excel         exports/  Exported files    ║
║  Ctrl+X  Export to CSV           imports/  Import sources    ║
║  Ctrl+S  Execute Procedure       logs/     Activity logs     ║ 
║  Ctrl+T  Test Connection         config/   Settings          ║
║  Ctrl+L  View Logs               temp/     Temporary files   ║
║  Ctrl+O  Open Settings           DATE FORMAT                 ║
║  F5      Refresh                 ───────────                 ║
║  Esc     Cancel                  YYYYMMDD (e.g., 20241201)   ║
║                                                              ║
║  SUPPORT                         VERSION                     ║
║  ────────                        ────────                    ║
║  Docs: ./docs/                   1.0.0                       ║
║  Logs: ./logs/error.log                                      ║
║                                                              ║
╚══════════════════════════════════════════════════════════════╝
```

---

**📚 For More Information:**
- [Configuration Guide](./CONFIGURATION.md)
- [Troubleshooting Guide](./TROUBLESHOOTING.md)
- [Installation Guide](../deployment/INSTALLATION.md)

---

<p align="center">
  <img src="https://img.shields.io/badge/Happy-Exporting-00E5FF?style=for-the-badge" alt="Happy Exporting"/>
</p>

</div>
