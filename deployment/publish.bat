@echo off
setlocal enabledelayedexpansion

:: TradeData Studio Deployment Script (Batch Version)
:: This script builds and publishes the TradeData Studio application for deployment

echo.
echo TradeData Studio - Deployment Build Script (Batch Version)
echo ==========================================================
echo.

:: Set variables
set "SCRIPT_DIR=%~dp0"
set "CONFIG_PATH=%SCRIPT_DIR%deployment.json"
set "ROOT_PATH=%SCRIPT_DIR%.."
set "OUTPUT_PATH=%SCRIPT_DIR%output"

:: Check if deployment.json exists
if not exist "%CONFIG_PATH%" (
    echo [ERROR] Deployment configuration not found: %CONFIG_PATH%
    pause
    exit /b 1
)

:: Parse JSON configuration (simplified approach)
:: Extract project path from JSON
for /f "tokens=2 delims=:" %%a in ('findstr /c:"project" "%CONFIG_PATH%"') do (
    set "PROJECT_LINE=%%a"
    set "PROJECT_LINE=!PROJECT_LINE:"=!"
    set "PROJECT_LINE=!PROJECT_LINE:,=!"
    set "PROJECT_LINE=!PROJECT_LINE: =!"
    set "PROJECT_PATH=!PROJECT_LINE:\=\!"
)

:: Remove leading/trailing spaces and quotes
for /f "tokens=* delims= " %%a in ("!PROJECT_PATH!") do set "PROJECT_PATH=%%a"
set "PROJECT_PATH=!PROJECT_PATH:"=!"
set "FULL_PROJECT_PATH=%ROOT_PATH%\!PROJECT_PATH!"

echo [INFO] Project path: !FULL_PROJECT_PATH!
echo [INFO] Output path: %OUTPUT_PATH%
echo.

:: Clean existing output directory
if exist "%OUTPUT_PATH%" (
    echo [INFO] Cleaning existing output directory...
    
    :: Try to kill any running TradeDataStudio processes
    taskkill /f /im TradeDataStudio.Desktop.exe >nul 2>&1
    timeout /t 2 /nobreak >nul
    
    :: Remove output directory with retry logic
    set "RETRY_COUNT=0"
    set "MAX_RETRIES=3"
    set "SUCCESS=0"
    
    :RETRY_LOOP
    if !RETRY_COUNT! lss !MAX_RETRIES! (
        rmdir /s /q "%OUTPUT_PATH%" >nul 2>&1
        if not exist "%OUTPUT_PATH%" (
            set "SUCCESS=1"
            echo [SUCCESS] Output directory cleaned successfully
            goto :CONTINUE_BUILD
        ) else (
            set /a "RETRY_COUNT+=1"
            echo [WARNING] Attempt !RETRY_COUNT! failed to clean directory
            if !RETRY_COUNT! lss !MAX_RETRIES! (
                echo [INFO] Retrying in 3 seconds...
                timeout /t 3 /nobreak >nul
                goto :RETRY_LOOP
            )
        )
    )
    
    if !SUCCESS! equ 0 (
        echo [ERROR] Could not clean output directory after !MAX_RETRIES! attempts.
        echo Please close any running applications and try again.
        pause
        exit /b 1
    )
)

:CONTINUE_BUILD
:: Create output directory
mkdir "%OUTPUT_PATH%" 2>nul

:: Build Windows-x64 configuration
echo.
echo [INFO] Building Windows-x64 configuration...
set "OUTPUT_DIR=%OUTPUT_PATH%\Windows-x64"
dotnet publish "!FULL_PROJECT_PATH!" --configuration Release --runtime win-x64 --self-contained true --output "%OUTPUT_DIR%" --verbosity minimal
if errorlevel 1 (
    echo [ERROR] Failed to build Windows-x64 configuration
    goto :BUILD_CONTINUE
) else (
    echo [SUCCESS] Windows-x64 build completed successfully
)

:: Copy configuration files for Windows-x64
set "CONFIG_SOURCE=%ROOT_PATH%\config"
set "CONFIG_DEST=%OUTPUT_DIR%\config"
if exist "%CONFIG_SOURCE%" (
    mkdir "%CONFIG_DEST%" 2>nul
    copy "%CONFIG_SOURCE%\*.json" "%CONFIG_DEST%\" >nul
    echo [SUCCESS] Configuration files copied to Windows-x64
)

:: Create directories for Windows-x64
mkdir "%OUTPUT_DIR%\logs" 2>nul
mkdir "%OUTPUT_DIR%\exports" 2>nul
mkdir "%OUTPUT_DIR%\exports\export" 2>nul
mkdir "%OUTPUT_DIR%\exports\import" 2>nul

:BUILD_CONTINUE

:: Build Portable configuration
echo.
echo [INFO] Building Portable configuration...
set "OUTPUT_DIR=%OUTPUT_PATH%\Portable"
dotnet publish "!FULL_PROJECT_PATH!" --configuration Release --output "%OUTPUT_DIR%" --verbosity minimal
if errorlevel 1 (
    echo [ERROR] Failed to build Portable configuration
    goto :CREATE_SHORTCUT
) else (
    echo [SUCCESS] Portable build completed successfully
)

:: Copy configuration files for Portable
set "CONFIG_SOURCE=%ROOT_PATH%\config"
set "CONFIG_DEST=%OUTPUT_DIR%\config"
if exist "%CONFIG_SOURCE%" (
    mkdir "%CONFIG_DEST%" 2>nul
    copy "%CONFIG_SOURCE%\*.json" "%CONFIG_DEST%\" >nul
    echo [SUCCESS] Configuration files copied to Portable
)

:: Create directories for Portable
mkdir "%OUTPUT_DIR%\logs" 2>nul
mkdir "%OUTPUT_DIR%\exports" 2>nul
mkdir "%OUTPUT_DIR%\exports\export" 2>nul
mkdir "%OUTPUT_DIR%\exports\import" 2>nul

:CREATE_SHORTCUT
:: Create desktop shortcut (using Portable version as preferred)
echo.
echo [INFO] Creating application shortcut...
set "SHORTCUT_PATH=%ROOT_PATH%\TradeData Studio.lnk"
set "EXE_PATH=%OUTPUT_PATH%\Portable\TradeDataStudio.Desktop.exe"

if exist "%EXE_PATH%" (
    :: Use PowerShell to create shortcut if available
    powershell -Command "try { $WshShell = New-Object -comObject WScript.Shell; $Shortcut = $WshShell.CreateShortcut('%SHORTCUT_PATH%'); $Shortcut.TargetPath = '%EXE_PATH%'; $Shortcut.WorkingDirectory = Split-Path '%EXE_PATH%' -Parent; $Shortcut.Description = 'TradeData Studio - Database Management Tool'; $Shortcut.IconLocation = '%EXE_PATH%'; $Shortcut.Save(); Write-Host '[SUCCESS] Shortcut created successfully' } catch { Write-Host '[ERROR] Failed to create shortcut' }" 2>nul
    if errorlevel 1 (
        echo [WARNING] Could not create shortcut using PowerShell
    )
) else (
    echo [ERROR] Portable executable not found for shortcut creation: %EXE_PATH%
)

:: Display build summary
echo.
echo [INFO] Build Summary:
echo ==================
if exist "%OUTPUT_PATH%\Windows-x64" (
    echo   Windows-x64: Available
)
if exist "%OUTPUT_PATH%\Portable" (
    echo   Portable: Available
)

echo.
echo [SUCCESS] Deployment builds completed!
echo [INFO] Output location: %OUTPUT_PATH%
echo.
echo Press any key to exit...
pause >nul