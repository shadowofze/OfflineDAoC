@echo off
setlocal
cd /d "%~dp0"
if not exist "%~dp0Get-OfflineDAoC.ps1" (
  echo Download Get-OfflineDAoC.ps1 into this same folder first.
  pause
  exit /b 1
)
echo This downloads the preserved official shadowofze/OfflineDAoC v0.3 release.
echo It verifies download hashes and extracts into a NEW playable folder.
echo No game is started, and existing games or saves are not overwritten.
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Get-OfflineDAoC.ps1" -ReleaseVersion 0.3 -Destination "%~dp0playable-v0.3"
if errorlevel 1 (
  echo Download or extraction failed. Read the error above. Existing games were not replaced.
  pause
  exit /b 1
)
echo Open playable-v0.3, read READ ME FIRST.txt, and run START OFFLINE DAOC.cmd.
pause
