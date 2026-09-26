@echo off
setlocal
cd /d "%~dp0"
echo Optional Sluaghbinder v0.32b - Darkness Falls Beta
echo Select a verified normal v0.32 installation. Earlier versions are refused.
echo This creates a new patched copy and never overwrites the folder you choose.
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Install-Sluaghbinder.ps1"
if errorlevel 1 (
  echo Installation failed. No existing installation was overwritten.
  pause
  exit /b 1
)
pause
