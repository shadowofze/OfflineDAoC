[CmdletBinding()]
param([string]$InstallPath)
$ErrorActionPreference = 'Stop'

function Resolve-PatchPath([string]$folder, [string]$relative) {
    if ([string]::IsNullOrWhiteSpace($relative) -or [IO.Path]::IsPathRooted($relative) -or
        $relative.Contains(':')) { throw "Unsafe patch path: $relative" }
    $base = [IO.Path]::GetFullPath($folder).TrimEnd([char[]]@('\','/'))
    $resolved = [IO.Path]::GetFullPath((Join-Path $base $relative))
    if (!$resolved.StartsWith($base + [IO.Path]::DirectorySeparatorChar,
            [StringComparison]::OrdinalIgnoreCase)) { throw "Patch path escapes its folder: $relative" }
    return $resolved
}

if (!$InstallPath) { $InstallPath = Split-Path -Parent $PSScriptRoot }
$root = [IO.Path]::GetFullPath($InstallPath)
$marker = Join-Path $root '.sluaghbinder\patch-manifest.json'
if (!(Test-Path -LiteralPath $marker)) { throw 'No Sluaghbinder patch marker was found in this folder.' }
$record = Get-Content -LiteralPath $marker -Raw | ConvertFrom-Json
if ($record.Feature -ne 'Sluaghbinder' -or $record.Version -ne '0.31b') { throw 'This is not a recognized Sluaghbinder v0.31b patch.' }
if (Get-Process -Name 'OfflineDAoC','CoreServer','connect' -ErrorAction SilentlyContinue) { throw 'Close the launcher, server, and game before rolling back.' }
$backupRoot = Join-Path $root 'runtime\.sluaghbinder-backup'
$dbSource = Resolve-PatchPath $root ([string]$record.DatabaseBackup)
if (!(Test-Path -LiteralPath $dbSource -PathType Leaf)) { throw 'Rollback database backup is missing.' }
$entries = @()
$seenPaths = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
foreach ($item in @($record.Files)) {
    $relative = [string]$item.Path
    if (!$seenPaths.Add($relative)) { throw "Duplicate rollback path: $relative" }
    $target = Resolve-PatchPath $root $relative
    $source = $null
    # Old v0.31b receipts had no ExistedBefore flag: every old payload file
    # replaced a baseline file and must be restored from its backup.
    $existedBefore = if ($item.PSObject.Properties['ExistedBefore']) { [bool]$item.ExistedBefore } else { $true }
    if ($existedBefore) {
        $source = Resolve-PatchPath $backupRoot $relative
        if (!(Test-Path -LiteralPath $source -PathType Leaf)) { throw "Rollback backup is missing: $relative" }
        if ($item.OriginalSHA256 -and
            (Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash -ne $item.OriginalSHA256) {
            throw "Rollback backup hash mismatch: $relative"
        }
    } elseif (Test-Path -LiteralPath $target) {
        if (!(Test-Path -LiteralPath $target -PathType Leaf)) { throw "Added patch target is not a file: $relative" }
        if ((Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash -ne $item.PatchedSHA256) {
            throw "Added patch file changed since installation; refusing to remove it: $relative"
        }
    }
    $entries += [pscustomobject]@{ Path=$relative; Source=$source; Target=$target; ExistedBefore=$existedBefore }
}
$dbTarget = Join-Path $root 'runtime\data\opendaoc.sqlite3.db'
foreach ($entry in $entries) {
    if ($entry.ExistedBefore) {
        Copy-Item -LiteralPath $entry.Source -Destination $entry.Target -Force
    } elseif (Test-Path -LiteralPath $entry.Target -PathType Leaf) {
        Remove-Item -LiteralPath $entry.Target -Force
    }
}
Copy-Item -LiteralPath $dbSource -Destination $dbTarget -Force
$done = Join-Path $root '.sluaghbinder\rollback-complete.json'
[ordered]@{ Feature='Sluaghbinder'; Version='0.31b'; RolledBackUtc=(Get-Date).ToUniversalTime().ToString('o') } | ConvertTo-Json | Set-Content -LiteralPath $done -Encoding UTF8
Write-Host 'Sluaghbinder files and static database rows were restored in this copy.'
Write-Host 'The original base installation was never modified. Backups remain under runtime\.sluaghbinder-backup.'
