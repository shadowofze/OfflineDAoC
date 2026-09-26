@echo off
setlocal
cd /d "%~dp0"
if not exist "%~dp0Get-OfflineDAoC.ps1" (
  echo Download Get-OfflineDAoC.ps1 into this same folder first.
  pause
  exit /b 1
)
echo Offline DAoC v0.32b - Darkness Falls Beta plus optional Hibernian Sluaghbinder.
echo The downloader verifies a v0.32 base, copies it, and patches only the NEW playable-v0.32b folder.
echo It never overwrites an existing game or starts the server automatically.
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Get-OfflineDAoC.ps1" -ReleaseVersion 0.32b -Destination "%~dp0playable-v0.32b"
if errorlevel 1 (
  echo Download or patch installation failed. Existing games were not replaced.
  pause
  exit /b 1
)
echo Open playable-v0.32b, read READ ME FIRST.txt, and run START OFFLINE DAOC.cmd.
pause
