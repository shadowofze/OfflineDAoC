[CmdletBinding()]
param([string]$InstallPath)
$ErrorActionPreference = 'Stop'
if (!$InstallPath) { $InstallPath = Split-Path -Parent $PSScriptRoot }
$root = [IO.Path]::GetFullPath($InstallPath)
$marker = Join-Path $root '.sluaghbinder\patch-manifest.json'
if (!(Test-Path -LiteralPath $marker)) { throw 'No Sluaghbinder patch marker was found in this folder.' }
$record = Get-Content -LiteralPath $marker -Raw | ConvertFrom-Json
if ($record.Feature -ne 'Sluaghbinder' -or $record.Version -ne '0.31b') { throw 'This is not a recognized Sluaghbinder v0.31b patch.' }
if (Get-Process -Name 'OfflineDAoC','CoreServer','connect' -ErrorAction SilentlyContinue) { throw 'Close the launcher, server, and game before rolling back.' }
$backupRoot = Join-Path $root 'runtime\.sluaghbinder-backup'
foreach ($item in @($record.Files)) {
    $source = Join-Path $backupRoot ([string]$item.Path)
    $target = Join-Path $root ([string]$item.Path)
    if (!(Test-Path -LiteralPath $source)) { throw "Rollback backup is missing: $($item.Path)" }
    Copy-Item -LiteralPath $source -Destination $target -Force
}
$dbSource = Join-Path $root ([string]$record.DatabaseBackup)
$dbTarget = Join-Path $root 'runtime\data\opendaoc.sqlite3.db'
if (!(Test-Path -LiteralPath $dbSource)) { throw 'Rollback database backup is missing.' }
Copy-Item -LiteralPath $dbSource -Destination $dbTarget -Force
$done = Join-Path $root '.sluaghbinder\rollback-complete.json'
[ordered]@{ Feature='Sluaghbinder'; Version='0.31b'; RolledBackUtc=(Get-Date).ToUniversalTime().ToString('o') } | ConvertTo-Json | Set-Content -LiteralPath $done -Encoding UTF8
Write-Host 'Sluaghbinder files and static database rows were restored in this copy.'
Write-Host 'The original base installation was never modified. Backups remain under runtime\.sluaghbinder-backup.'
