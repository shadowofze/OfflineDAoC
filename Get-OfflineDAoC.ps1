param(
    [string]$Destination = (Join-Path $PSScriptRoot 'playable'),
    [ValidatePattern('^\d+\.\d+(b)?$')]
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
if ($manifest.Version -ne $ReleaseVersion) { throw 'Unexpected release manifest version.' }

# v0.31b is an optional, copy-first expansion. It downloads the immutable
# v0.31 playable baseline, then applies the separately hash-verified
# Sluaghbinder patch into a new sibling folder. The old v0.3/v0.31 paths below
# remain unchanged, so anyone who does not want the class can keep using them.
if ($manifest.Mode -eq 'optional-patch') {
    if ($manifest.BaseVersion -ne '0.31' -or !$manifest.PatchName -or
        $manifest.PatchRootFolder -ne 'OfflineDAoC-Sluaghbinder-v0.31b-patch' -or
        $manifest.PatchSHA256 -notmatch '^[a-f0-9]{64}$') { throw 'Unexpected v0.31b optional patch manifest.' }
    $parent = Split-Path -Parent $target
    $leaf = Split-Path -Leaf $target
    $baseTarget = Join-Path $parent ($leaf + '-v0.31-base')
    if (Test-Path -LiteralPath $baseTarget) { throw "Base staging folder already exists: $baseTarget" }
    Write-Host 'Downloading the preserved v0.31 playable baseline first...'
    & $PSCommandPath -ReleaseVersion $manifest.BaseVersion -Destination $baseTarget
    if ($LASTEXITCODE -and $LASTEXITCODE -ne 0) { throw 'The v0.31 baseline download failed.' }

    $patchPath = Join-Path $cache $manifest.PatchName
    $patchValid = (Test-Path -LiteralPath $patchPath) -and
        (Get-Item -LiteralPath $patchPath).Length -eq [int64]$manifest.PatchBytes
    if ($patchValid) { $patchValid = (Get-FileHash -LiteralPath $patchPath -Algorithm SHA256).Hash -eq $manifest.PatchSHA256 }
    if (!$patchValid) {
        Write-Host "Downloading $($manifest.PatchName)..."
        Invoke-WebRequest -UseBasicParsing -Uri "$releaseBase/$($manifest.PatchName)" -OutFile $patchPath
        if ((Get-Item -LiteralPath $patchPath).Length -ne [int64]$manifest.PatchBytes -or
            (Get-FileHash -LiteralPath $patchPath -Algorithm SHA256).Hash -ne $manifest.PatchSHA256) {
            throw 'Sluaghbinder patch verification failed. The v0.31 baseline is intact; run again to retry.'
        }
    }
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $patchExtract = Join-Path $cache ('patch-' + [guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Path $patchExtract | Out-Null
    $zip = [IO.Compression.ZipFile]::OpenRead($patchPath)
    try {
        $prefix = $manifest.PatchRootFolder + '/'
        $extractPrefix = $patchExtract.TrimEnd('\') + '\'
        foreach ($entry in $zip.Entries) {
            $entryName = $entry.FullName.Replace('\','/')
            if (!$entryName.StartsWith($prefix,[StringComparison]::Ordinal) -or
                $entryName.Contains(':') -or $entryName.Contains('/../') -or $entryName.EndsWith('/..')) { throw 'Unexpected Sluaghbinder patch layout.' }
            $relative = $entryName.Substring($prefix.Length)
            if (!$relative) { continue }
            $resolved = [IO.Path]::GetFullPath((Join-Path $patchExtract $relative))
            if (!$resolved.StartsWith($extractPrefix,[StringComparison]::OrdinalIgnoreCase)) { throw 'Unsafe Sluaghbinder patch entry.' }
        }
        foreach ($entry in $zip.Entries) {
            $relative = $entry.FullName.Replace('\','/').Substring($prefix.Length)
            if (!$relative) { continue }
            $resolved = Join-Path $patchExtract $relative
            if ($entry.FullName.EndsWith('/')) { New-Item -ItemType Directory -Path $resolved -Force | Out-Null; continue }
            New-Item -ItemType Directory -Path (Split-Path -Parent $resolved) -Force | Out-Null
            [IO.Compression.ZipFileExtensions]::ExtractToFile($entry,$resolved,$false)
        }
    } finally { $zip.Dispose() }
    $installer = Join-Path $patchExtract 'Install-Sluaghbinder.ps1'
    if (!(Test-Path -LiteralPath $installer)) { throw 'Sluaghbinder patch installer is missing.' }
    & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $installer -BasePath $baseTarget -Destination $target
    if ($LASTEXITCODE -and $LASTEXITCODE -ne 0) { throw 'Sluaghbinder patch installation failed; the v0.31 baseline remains intact.' }
    Write-Host "Verified v0.31b optional Sluaghbinder expansion and installed it to $target"
    Write-Host "The unmodified v0.31 staging copy is at $baseTarget until you choose to remove it."
    exit 0
}

# v0.31 is intentionally a small, hash-verified update over the immutable v0.3
# playable seed. This keeps the large navmesh/world download in one place while
# preserving a clean, separate v0.3 install for anyone who wants that baseline.
if ($manifest.Mode -eq 'delta') {
    if ($manifest.BaseVersion -ne '0.3' -or !$manifest.PatchName -or
        $manifest.PatchRootFolder -ne 'OfflineDAoC-v0.31-update' -or
        $manifest.PatchSHA256 -notmatch '^[a-f0-9]{64}$') { throw 'Unexpected v0.31 update manifest.' }

    Write-Host 'Downloading the preserved v0.3 playable seed first...'
    & $PSCommandPath -ReleaseVersion $manifest.BaseVersion -Destination $target
    if ($LASTEXITCODE -and $LASTEXITCODE -ne 0) { throw 'The v0.3 seed download failed.' }

    $patchPath = Join-Path $cache $manifest.PatchName
    $patchValid = (Test-Path -LiteralPath $patchPath) -and
        (Get-Item -LiteralPath $patchPath).Length -eq [int64]$manifest.PatchBytes
    if ($patchValid) { $patchValid = (Get-FileHash -LiteralPath $patchPath -Algorithm SHA256).Hash -eq $manifest.PatchSHA256 }
    if (!$patchValid) {
        Write-Host "Downloading $($manifest.PatchName)..."
        Invoke-WebRequest -UseBasicParsing -Uri "$releaseBase/$($manifest.PatchName)" -OutFile $patchPath
        if ((Get-Item -LiteralPath $patchPath).Length -ne [int64]$manifest.PatchBytes -or
            (Get-FileHash -LiteralPath $patchPath -Algorithm SHA256).Hash -ne $manifest.PatchSHA256) {
            throw 'v0.31 update verification failed. The v0.3 seed is intact; run again to retry.'
        }
    }

    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $zip = [IO.Compression.ZipFile]::OpenRead($patchPath)
    try {
        $prefix = $manifest.PatchRootFolder + '/'
        $targetPrefix = $target.TrimEnd('\') + '\'
        foreach ($entry in $zip.Entries) {
            $entryName = $entry.FullName.Replace('\','/')
            if (!$entryName.StartsWith($prefix,[StringComparison]::Ordinal) -or
                $entryName.Contains(':')) { throw 'Unexpected v0.31 update layout.' }
            $relative = $entryName.Substring($prefix.Length)
            if (!$relative) { continue }
            $resolved = [IO.Path]::GetFullPath((Join-Path $target $relative))
            if (!$resolved.StartsWith($targetPrefix,[StringComparison]::OrdinalIgnoreCase)) { throw 'Unsafe v0.31 update entry.' }
        }
        foreach ($entry in $zip.Entries) {
            $entryName = $entry.FullName.Replace('\','/')
            $relative = $entryName.Substring($prefix.Length)
            if (!$relative) { continue }
            $resolved = Join-Path $target $relative
            if ($entryName.EndsWith('/')) { New-Item -ItemType Directory -Path $resolved -Force | Out-Null; continue }
            New-Item -ItemType Directory -Path (Split-Path -Parent $resolved) -Force | Out-Null
            [IO.Compression.ZipFileExtensions]::ExtractToFile($entry,$resolved,$true)
        }
    } finally { $zip.Dispose() }
    Write-Host "Verified v0.31 update and applied it to $target"
    Write-Host 'Read the included README.md and docs, then open START OFFLINE DAOC.cmd. Nothing was started automatically.'
    exit 0
}

if (!$manifest.Parts -or $manifest.RootFolder -ne $rootFolder) { throw 'Unexpected full-release manifest.' }
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
