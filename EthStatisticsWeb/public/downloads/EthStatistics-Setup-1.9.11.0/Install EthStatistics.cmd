@echo off
setlocal
title EthStatistics Setup

cd /d "%~dp0"

echo.
echo ==============================================
echo          EthStatistics Setup
echo ==============================================
echo.
echo Starting installer...
echo.

powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Install-EthStatistics.ps1"

if errorlevel 1 (
    echo.
    echo ==============================================
    echo Installation encountered an error.
    echo ==============================================
    echo.
    pause
    exit /b 1
)

exit /b 0