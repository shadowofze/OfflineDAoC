@echo off
setlocal
cd /d "%~dp0"
if not exist "%~dp0Get-OfflineDAoC.ps1" (
  echo Download Get-OfflineDAoC.ps1 into this same folder first.
  pause
  exit /b 1
)
echo This downloads Offline DAoC v0.32 - Darkness Falls Beta, without Sluaghbinder.
echo It verifies download hashes and extracts into a NEW playable folder.
echo No game is started, and existing games or saves are not overwritten.
echo PowerShell's script policy is set only for this process, not for Windows.
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Get-OfflineDAoC.ps1" -ReleaseVersion 0.32
if errorlevel 1 (
  echo Download or extraction failed. Read the error above. Existing games were not replaced.
  pause
  exit /b 1
)
echo Open the playable folder, read READ ME FIRST.txt, and run START OFFLINE DAOC.cmd.
pause
