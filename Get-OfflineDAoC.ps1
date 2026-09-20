param(
    [string]$Destination = (Join-Path $PSScriptRoot 'playable'),
    [ValidatePattern('^\d+\.\d+$')]
    [string]$ReleaseVersion = '0.31'
)
$ErrorActionPreference = 'Stop'
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
$releaseBase = "https://github.com/shadowofze/OfflineDAoC/releases/download/v$ReleaseVersion"
$rootFolder = "OfflineDAoC-v$ReleaseVersion"
$partPattern = '^' + [regex]::Escape($rootFolder) + '\.zip\.\d{3}$'
$target = [IO.Path]::GetFullPath($Destination)
if (Test-Path -LiteralPath $target) { throw 'Destination already exists. Choose a NEW folder; existing games and saves are never overwritten.' }
$cache = Join-Path $PSScriptRoot (Join-Path '.downloads' "v$ReleaseVersion")
New-Item -ItemType Directory -Path $cache -Force | Out-Null
$manifestPath = Join-Path $cache 'download-manifest.json'
Write-Host 'Downloading the release manifest...'
Invoke-WebRequest -UseBasicParsing -Uri "$releaseBase/download-manifest.json" -OutFile $manifestPath
$manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
if ($manifest.Version -ne $ReleaseVersion -or !$manifest.Parts -or $manifest.RootFolder -ne $rootFolder) { throw 'Unexpected release manifest.' }
$partPaths = @()
foreach ($part in $manifest.Parts) {
    if ($part.Name -notmatch $partPattern -or $part.SHA256 -notmatch '^[a-f0-9]{64}$') { throw 'Invalid part metadata.' }
    $path = Join-Path $cache $part.Name
    $valid = (Test-Path -LiteralPath $path) -and (Get-Item -LiteralPath $path).Length -eq $part.Bytes
    if ($valid) { $valid = (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -eq $part.SHA256 }
    if (!$valid) {
        Write-Host "Downloading $($part.Name)..."
        Invoke-WebRequest -UseBasicParsing -Uri "$releaseBase/$($part.Name)" -OutFile $path
        if ((Get-Item -LiteralPath $path).Length -ne $part.Bytes -or (Get-FileHash -LiteralPath $path).Hash -ne $part.SHA256) { throw 'Download verification failed. Run again to retry the incomplete part.' }
    }
    $partPaths += $path
}
$archive = Join-Path $cache "$rootFolder.zip"
$joined = [IO.File]::Create($archive)
try {
    foreach ($partPath in $partPaths) {
        $inputStream = [IO.File]::OpenRead($partPath)
        try { $inputStream.CopyTo($joined) } finally { $inputStream.Dispose() }
    }
} finally { $joined.Dispose() }
if ((Get-FileHash -LiteralPath $archive).Hash -ne $manifest.ArchiveSHA256) { throw 'Complete archive hash mismatch; nothing was installed.' }
Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip = [IO.Compression.ZipFile]::OpenRead($archive)
try {
    # Validate EVERY path before extracting anything. Archive cannot escape this new destination.
    $prefix = $manifest.RootFolder + '/'
    $targetPrefix = $target.TrimEnd('\') + '\'
    foreach ($entry in $zip.Entries) {
        if (!$entry.FullName.StartsWith($prefix,[StringComparison]::Ordinal) -or $entry.FullName.Contains(':')) { throw 'Unexpected archive layout.' }
        $relative = $entry.FullName.Substring($prefix.Length)
        if (!$relative) { continue }
        $resolved = [IO.Path]::GetFullPath((Join-Path $target $relative))
        if (!$resolved.StartsWith($targetPrefix,[StringComparison]::OrdinalIgnoreCase)) { throw 'Unsafe archive entry.' }
    }
    New-Item -ItemType Directory -Path $target | Out-Null
    foreach ($entry in $zip.Entries) {
        $relative = $entry.FullName.Substring($prefix.Length)
        if (!$relative) { continue }
        $resolved = Join-Path $target $relative
        if ($entry.FullName.EndsWith('/')) { New-Item -ItemType Directory -Path $resolved -Force | Out-Null; continue }
        New-Item -ItemType Directory -Path (Split-Path -Parent $resolved) -Force | Out-Null
        [IO.Compression.ZipFileExtensions]::ExtractToFile($entry,$resolved,$false)
    }
} finally { $zip.Dispose() }
Write-Host "Verified and extracted to $target"
Write-Host 'Read READ ME FIRST.txt, then open START OFFLINE DAOC.cmd. Nothing has been started automatically.'
Write-Host 'Downloads are retained in .downloads so interrupted downloads can be retried.'
