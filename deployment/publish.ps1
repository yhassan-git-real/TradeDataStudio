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
    Remove-Item $outputPath -Recurse -Force
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

# Display build summary
Write-Host "`nBuild Summary:" -ForegroundColor Cyan
Get-ChildItem $outputPath -Directory | ForEach-Object {
    $size = (Get-ChildItem $_.FullName -Recurse | Measure-Object -Property Length -Sum).Sum
    $sizeMB = [Math]::Round($size / 1MB, 2)
    Write-Host "  $($_.Name): $sizeMB MB" -ForegroundColor White
}