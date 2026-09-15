@echo off
setlocal
pushd "%~dp0"
"runtime\python.exe" -E -s "asset_tool.py" %*
set "asset_tool_exit=%errorlevel%"
popd
exit /b %asset_tool_exit%
