@echo off
echo TradeData Studio - PowerShell Setup Helper
echo ==========================================
echo.
echo This script will help setup PowerShell execution policy for TradeData Studio deployment.
echo.

:: Check if PowerShell is available
powershell -Command "Get-Host" >nul 2>&1
if errorlevel 1 (
    echo [ERROR] PowerShell is not available on this system.
    echo [INFO] Please use publish.bat instead.
    pause
    exit /b 1
)

echo [INFO] Checking current PowerShell execution policy...
for /f "tokens=*" %%a in ('powershell -Command "Get-ExecutionPolicy -Scope CurrentUser"') do set "CURRENT_POLICY=%%a"
echo [INFO] Current execution policy for current user: %CURRENT_POLICY%

if /i "%CURRENT_POLICY%"=="RemoteSigned" (
    echo [SUCCESS] Execution policy is already set correctly.
    echo [INFO] You can now run publish.ps1 directly.
) else if /i "%CURRENT_POLICY%"=="Unrestricted" (
    echo [SUCCESS] Execution policy allows script execution.
    echo [INFO] You can now run publish.ps1 directly.
) else (
    echo.
    echo [INFO] Current policy (%CURRENT_POLICY%) may prevent PowerShell scripts from running.
    echo [INFO] Attempting to set execution policy to RemoteSigned for current user...
    echo.
    
    powershell -Command "try { Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser -Force; Write-Host '[SUCCESS] Execution policy updated successfully' -ForegroundColor Green } catch { Write-Host '[ERROR] Failed to update execution policy:' $_.Exception.Message -ForegroundColor Red; exit 1 }"
    
    if errorlevel 1 (
        echo.
        echo [WARNING] Automatic policy update failed.
        echo [INFO] You may need to run PowerShell as Administrator and execute:
        echo [INFO] Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
        echo.
        echo [INFO] Alternatively, you can use publish.bat which doesn't require policy changes.
    ) else (
        echo.
        echo [SUCCESS] PowerShell execution policy has been updated.
        echo [INFO] You can now run publish.ps1 to build the application.
    )
)

echo.
echo Available deployment options:
echo  1. publish.ps1  - PowerShell script (requires execution policy setup)
echo  2. publish.bat  - Batch script (no policy requirements)
echo.
echo Press any key to exit...
pause >nul