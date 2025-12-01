# TradeData Studio Deployment Script
# This script builds and publishes the TradeData Studio application for deployment

Write-Host "TradeData Studio - Deployment Build Script" -ForegroundColor Green
Write-Host "=========================================" -ForegroundColor Green

# Set error action preference
$ErrorActionPreference = "Stop"

# Define paths
$rootPath = Split-Path $PSScriptRoot -Parent
$projectPath = Join-Path $rootPath "src\TradeDataStudio.Desktop\TradeDataStudio.Desktop.csproj"
$outputPath = Join-Path $PSScriptRoot "output"

# Create output directory
if (Test-Path $outputPath) {
    Write-Host "Cleaning existing output directory..." -ForegroundColor Yellow
    
    # Try to remove with retry logic for locked files
    $retryCount = 0
    $maxRetries = 3
    $success = $false
    
    while (-not $success -and $retryCount -lt $maxRetries) {
        try {
            # First try to kill any running instances of the application
            Get-Process | Where-Object { $_.ProcessName -like "*TradeDataStudio*" } | Stop-Process -Force -ErrorAction SilentlyContinue
            
            # Wait a moment for processes to clean up
            Start-Sleep -Seconds 2
            
            # Remove the directory
            Remove-Item $outputPath -Recurse -Force -ErrorAction Stop
            $success = $true
            Write-Host "✓ Output directory cleaned successfully" -ForegroundColor Green
        }
        catch {
            $retryCount++
            Write-Host "⚠ Attempt $retryCount failed: $($_.Exception.Message)" -ForegroundColor Yellow
            
            if ($retryCount -lt $maxRetries) {
                Write-Host "Retrying in 3 seconds..." -ForegroundColor Yellow
                Start-Sleep -Seconds 3
            }
        }
    }
    
    if (-not $success) {
        Write-Host "✗ Could not clean output directory after $maxRetries attempts." -ForegroundColor Red
        Write-Host "Please close any running applications and antivirus scanners, then try again." -ForegroundColor Red
        exit 1
    }
}
New-Item -ItemType Directory -Path $outputPath | Out-Null

# Build configurations
$configurations = @(
    @{
        Name = "Windows-x64"
        Runtime = "win-x64"
        SelfContained = $true
    },
    @{
        Name = "Windows-x86"
        Runtime = "win-x86"
        SelfContained = $true
    },
    @{
        Name = "Portable"
        Runtime = $null
        SelfContained = $false
    }
)

foreach ($config in $configurations) {
    Write-Host "`nBuilding $($config.Name) configuration..." -ForegroundColor Cyan
    
    $outputDir = Join-Path $outputPath $config.Name
    
    # Base dotnet publish command
    $publishArgs = @(
        "publish"
        $projectPath
        "--configuration", "Release"
        "--output", $outputDir
        "--verbosity", "minimal"
    )
    
    # Add runtime-specific arguments
    if ($config.Runtime) {
        $publishArgs += "--runtime", $config.Runtime
        $publishArgs += "--self-contained", $config.SelfContained.ToString().ToLower()
    }
    
    # Execute publish command
    try {
        & dotnet $publishArgs
        Write-Host "✓ $($config.Name) build completed successfully" -ForegroundColor Green
        
        # Copy configuration files (contents only, not the folder itself)
        $configSource = Join-Path $rootPath "config"
        $configDest = Join-Path $outputDir "config"
        if (Test-Path $configSource) {
            New-Item -ItemType Directory -Path $configDest -Force | Out-Null
            Get-ChildItem -Path $configSource -File | ForEach-Object {
                Copy-Item $_.FullName -Destination $configDest -Force
            }
            Write-Host "✓ Configuration files copied" -ForegroundColor Green
        }
        
        # Create logs directory
        $logsDir = Join-Path $outputDir "logs"
        New-Item -ItemType Directory -Path $logsDir -Force | Out-Null
        
        # Create exports directory
        $exportsDir = Join-Path $outputDir "exports"
        New-Item -ItemType Directory -Path $exportsDir -Force | Out-Null
        New-Item -ItemType Directory -Path (Join-Path $exportsDir "export") -Force | Out-Null
        New-Item -ItemType Directory -Path (Join-Path $exportsDir "import") -Force | Out-Null
        
    } catch {
        Write-Host "✗ Failed to build $($config.Name): $($_.Exception.Message)" -ForegroundColor Red
        continue
    }
}

Write-Host "`nDeployment builds completed!" -ForegroundColor Green
Write-Host "Output location: $outputPath" -ForegroundColor Yellow

# Create desktop shortcut in root directory
Write-Host "`nCreating application shortcut..." -ForegroundColor Cyan
$shortcutPath = Join-Path $rootPath "TradeData Studio.lnk"
$exePath = Join-Path $outputPath "Windows-x64\TradeDataStudio.Desktop.exe"

if (Test-Path $exePath) {
    try {
        $WshShell = New-Object -comObject WScript.Shell
        $Shortcut = $WshShell.CreateShortcut($shortcutPath)
        $Shortcut.TargetPath = $exePath
        $Shortcut.WorkingDirectory = Split-Path $exePath -Parent
        $Shortcut.Description = "TradeData Studio - Database Management Tool"
        $Shortcut.IconLocation = $exePath
        $Shortcut.Save()
        Write-Host "✓ Shortcut created: TradeData Studio.lnk" -ForegroundColor Green
    } catch {
        Write-Host "✗ Failed to create shortcut: $($_.Exception.Message)" -ForegroundColor Red
    }
} else {
    Write-Host "✗ Windows-x64 executable not found for shortcut creation" -ForegroundColor Red
}

# Display build summary
Write-Host "`nBuild Summary:" -ForegroundColor Cyan
Get-ChildItem $outputPath -Directory | ForEach-Object {
    $size = (Get-ChildItem $_.FullName -Recurse | Measure-Object -Property Length -Sum).Sum
    $sizeMB = [Math]::Round($size / 1MB, 2)
    Write-Host "  $($_.Name): $sizeMB MB" -ForegroundColor White
}