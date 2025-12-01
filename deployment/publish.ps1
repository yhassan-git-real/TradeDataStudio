# TradeData Studio Deployment Script
# This script builds and publishes the TradeData Studio application for deployment

Write-Host "TradeData Studio - Deployment Build Script" -ForegroundColor Green
Write-Host "=========================================" -ForegroundColor Green

# Check and handle PowerShell execution policy
$currentPolicy = Get-ExecutionPolicy -Scope CurrentUser
if ($currentPolicy -eq "Restricted" -or $currentPolicy -eq "Undefined") {
    Write-Host "[INFO] Current execution policy: $currentPolicy" -ForegroundColor Yellow
    Write-Host "[INFO] Attempting to set execution policy for current user..." -ForegroundColor Yellow
    try {
        Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser -Force
        Write-Host "[SUCCESS] Execution policy updated to RemoteSigned for current user" -ForegroundColor Green
    } catch {
        Write-Host "[WARNING] Could not update execution policy: $($_.Exception.Message)" -ForegroundColor Yellow
        Write-Host "[INFO] You may need to run: Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser" -ForegroundColor Yellow
    }
}

# Set error action preference
$ErrorActionPreference = "Stop"

# Load deployment configuration
$configPath = Join-Path $PSScriptRoot "deployment.json"
if (-not (Test-Path $configPath)) {
    Write-Host "[ERROR] Deployment configuration not found: $configPath" -ForegroundColor Red
    exit 1
}

try {
    $deploymentConfig = Get-Content $configPath | ConvertFrom-Json
    Write-Host "[SUCCESS] Loaded deployment configuration" -ForegroundColor Green
} catch {
    Write-Host "[ERROR] Failed to parse deployment configuration: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Define paths from configuration
$rootPath = Split-Path $PSScriptRoot -Parent
$projectPath = Join-Path $rootPath $deploymentConfig.paths.project
$outputPath = Join-Path $PSScriptRoot $deploymentConfig.paths.output

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
            Write-Host "[SUCCESS] Output directory cleaned successfully" -ForegroundColor Green
        }
        catch {
            $retryCount++
            Write-Host "[WARNING] Attempt $retryCount failed: $($_.Exception.Message)" -ForegroundColor Yellow
            
            if ($retryCount -lt $maxRetries) {
                Write-Host "Retrying in 3 seconds..." -ForegroundColor Yellow
                Start-Sleep -Seconds 3
            }
        }
    }
    
    if (-not $success) {
        Write-Host "[ERROR] Could not clean output directory after $maxRetries attempts." -ForegroundColor Red
        Write-Host "Please close any running applications and antivirus scanners, then try again." -ForegroundColor Red
        exit 1
    }
}
New-Item -ItemType Directory -Path $outputPath | Out-Null

# Build configurations from deployment config
$configurations = $deploymentConfig.build.configurations

foreach ($config in $configurations) {
    Write-Host "`nBuilding $($config.name) configuration..." -ForegroundColor Cyan
    
    $outputDir = Join-Path $outputPath $config.name
    
    # Base dotnet publish command
    $publishArgs = @(
        "publish"
        $projectPath
        "--configuration", "Release"
        "--output", $outputDir
        "--verbosity", "minimal"
    )
    
    # Add runtime-specific arguments
    if ($config.runtime) {
        $publishArgs += "--runtime", $config.runtime
        $publishArgs += "--self-contained", $config.selfContained.ToString().ToLower()
    }
    
    # Execute publish command
    try {
        & dotnet $publishArgs
        Write-Host "[SUCCESS] $($config.name) build completed successfully" -ForegroundColor Green
        
        # Copy configuration files (contents only, not the folder itself)
        $configSource = Join-Path $rootPath $deploymentConfig.paths.configSource
        $configDest = Join-Path $outputDir $deploymentConfig.paths.directories.config
        if (Test-Path $configSource) {
            New-Item -ItemType Directory -Path $configDest -Force | Out-Null
            Get-ChildItem -Path $configSource -File | ForEach-Object {
                Copy-Item $_.FullName -Destination $configDest -Force
            }
            Write-Host "[SUCCESS] Configuration files copied" -ForegroundColor Green
        }
        
        # Create logs directory
        $logsDir = Join-Path $outputDir $deploymentConfig.paths.directories.logs
        New-Item -ItemType Directory -Path $logsDir -Force | Out-Null
        
        # Create exports directory
        $exportsDir = Join-Path $outputDir $deploymentConfig.paths.directories.exports
        New-Item -ItemType Directory -Path $exportsDir -Force | Out-Null
        foreach ($subdir in $deploymentConfig.paths.directories.exports_subdirs) {
            New-Item -ItemType Directory -Path (Join-Path $exportsDir $subdir) -Force | Out-Null
        }
        
    } catch {
        Write-Host "[ERROR] Failed to build $($config.name): $($_.Exception.Message)" -ForegroundColor Red
        continue
    }
}

Write-Host "`nDeployment builds completed!" -ForegroundColor Green
Write-Host "Output location: $outputPath" -ForegroundColor Yellow

# Create desktop shortcut using configuration
Write-Host "`nCreating application shortcut..." -ForegroundColor Cyan

# Find the preferred configuration for shortcut
$preferredConfig = $configurations | Where-Object { $_.preferredForShortcut -eq $true } | Select-Object -First 1
if (-not $preferredConfig) {
    $preferredConfig = $configurations[0]  # fallback to first config
}

$shortcutPath = Join-Path $rootPath $deploymentConfig.deployment.shortcutName
$exePath = Join-Path $outputPath "$($preferredConfig.name)\$($deploymentConfig.executable.name)"

if (Test-Path $exePath) {
    try {
        $WshShell = New-Object -comObject WScript.Shell
        $Shortcut = $WshShell.CreateShortcut($shortcutPath)
        $Shortcut.TargetPath = $exePath
        $Shortcut.WorkingDirectory = Split-Path $exePath -Parent
        $Shortcut.Description = $deploymentConfig.executable.description
        $Shortcut.IconLocation = $exePath
        $Shortcut.Save()
        Write-Host "[SUCCESS] Shortcut created for $($preferredConfig.name): $($deploymentConfig.deployment.shortcutName)" -ForegroundColor Green
    } catch {
        Write-Host "[ERROR] Failed to create shortcut: $($_.Exception.Message)" -ForegroundColor Red
    }
} else {
    Write-Host "[ERROR] $($preferredConfig.name) executable not found for shortcut creation: $exePath" -ForegroundColor Red
}

# Display build summary
Write-Host "`nBuild Summary:" -ForegroundColor Cyan
Get-ChildItem $outputPath -Directory | ForEach-Object {
    $size = (Get-ChildItem $_.FullName -Recurse | Measure-Object -Property Length -Sum).Sum
    $sizeMB = [Math]::Round($size / 1MB, 2)
    Write-Host "  $($_.Name): $sizeMB MB" -ForegroundColor White
}