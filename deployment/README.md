# TradeData Studio - Deployment Scripts

This directory contains scripts to build and deploy the TradeData Studio application.

## Available Scripts

### 1. **publish.bat** (Recommended for most users)
- **Windows Batch file** - No special permissions required
- Works on all Windows systems without configuration
- Builds both Windows-x64 (self-contained) and Portable versions
- Creates desktop shortcut automatically

**Usage:**
```batch
cd deployment
publish.bat
```

### 2. **publish.ps1** (PowerShell version)
- **PowerShell script** - May require execution policy setup
- More advanced features and better error handling
- Reads configuration from deployment.json dynamically

**Usage:**
```powershell
cd deployment
.\publish.ps1
```

### 3. **setup-powershell.bat** (PowerShell Setup Helper)
- Helps configure PowerShell execution policy if needed
- Only run this if publish.ps1 fails with policy errors

**Usage:**
```batch
cd deployment
setup-powershell.bat
```

## Quick Start

1. **For most users**: Just run `publish.bat`
2. **If you prefer PowerShell**: Try `publish.ps1`, if it fails run `setup-powershell.bat` first

## Output

Both scripts create the same output structure:

```
deployment/output/
├── Windows-x64/          # Self-contained Windows executable
│   ├── TradeDataStudio.Desktop.exe
│   ├── config/           # Configuration files
│   ├── logs/             # Log directory
│   └── exports/          # Export directory
└── Portable/             # Requires .NET 8.0 Runtime
    ├── TradeDataStudio.Desktop.exe
    ├── config/           # Configuration files  
    ├── logs/             # Log directory
    └── exports/          # Export directory
```

## Desktop Shortcut

Both scripts automatically create a desktop shortcut (`TradeData Studio.lnk`) that points to the Portable version by default.

## Troubleshooting

### PowerShell Execution Policy Issues
If you get errors like "execution of scripts is disabled", you have three options:

1. **Use the batch file instead**: `publish.bat` (easiest)
2. **Run the setup helper**: `setup-powershell.bat`
3. **Manual setup**: Open PowerShell as Administrator and run:
   ```powershell
   Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
   ```

### Build Errors
- Ensure .NET 8.0 SDK is installed
- Check that all files are accessible (not locked by antivirus)
- Close any running instances of TradeData Studio

### File Access Issues
- Close TradeData Studio application if running
- Temporarily disable real-time antivirus scanning
- Run as Administrator if necessary

## Requirements

- Windows 10/11
- .NET 8.0 SDK (for building)
- .NET 8.0 Runtime (for running Portable version)

## Configuration

Build settings can be modified in `deployment.json`:
- Output directories
- Build configurations  
- Application metadata
- Shortcut preferences