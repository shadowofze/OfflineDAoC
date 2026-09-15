@echo off
setlocal
pushd "%~dp0"
if not exist "runtime\pythonw.exe" (
  echo Missing bundled runtime. Keep the entire OFFLINE DAOC ASSET TOOL folder together.
  pause
  popd
  exit /b 1
)
start "" "runtime\pythonw.exe" -E -s "asset_tool_gui.py"
popd
