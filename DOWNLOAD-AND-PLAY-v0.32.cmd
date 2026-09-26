@echo off
setlocal
cd /d "%~dp0"
if not exist "%~dp0Get-OfflineDAoC.ps1" (
  echo Download Get-OfflineDAoC.ps1 into this same folder first.
  pause
  exit /b 1
)
echo Offline DAoC v0.32 - Darkness Falls Beta, without Sluaghbinder.
echo The downloader verifies each release, keeps a v0.31 rollback copy, and creates a NEW playable-v0.32 folder.
echo It never overwrites an existing game or starts the server automatically.
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Get-OfflineDAoC.ps1" -ReleaseVersion 0.32 -Destination "%~dp0playable-v0.32"
if errorlevel 1 (
  echo Download or extraction failed. Existing games were not replaced.
  pause
  exit /b 1
)
echo Open playable-v0.32, read READ ME FIRST.txt, and run START OFFLINE DAOC.cmd.
pause
