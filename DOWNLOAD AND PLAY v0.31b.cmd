@echo off
setlocal
cd /d "%~dp0"
if not exist "%~dp0Get-OfflineDAoC.ps1" (
  echo Download Get-OfflineDAoC.ps1 into this same folder first.
  pause
  exit /b 1
)
echo This downloads the preserved v0.31 playable release and the optional Sluaghbinder v0.31b expansion.
echo It verifies hashes, creates a new patched folder, and never overwrites an existing game or save.
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Get-OfflineDAoC.ps1" -ReleaseVersion 0.31b -Destination "%~dp0playable-v0.31b"
if errorlevel 1 (
  echo Download or extraction failed. Read the error above. Existing games were not replaced.
  pause
  exit /b 1
)
echo Open playable-v0.31b, read READ ME FIRST.txt, and run START OFFLINE DAOC.cmd.
pause
