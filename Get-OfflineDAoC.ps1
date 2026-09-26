param(
    [string]$Destination = (Join-Path $PSScriptRoot 'playable'),
    [ValidatePattern('^\d+\.\d+(b)?$')]
    [string]$ReleaseVersion = '0.32b'
)
$ErrorActionPreference = 'Stop'
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

function Get-ExtendedPath([string]$path) {
    $full = [IO.Path]::GetFullPath($path)
    if ($full.StartsWith('\\?\',[StringComparison]::Ordinal)) { return $full }
    if ($full.StartsWith('\\',[StringComparison]::Ordinal)) {
        return '\\?\UNC\' + $full.Substring(2)
    }
    return '\\?\' + $full
}

function Test-ExistsLong([string]$path) {
    $extended = Get-ExtendedPath $path
    return (Test-Path -LiteralPath $path) -or
        [IO.File]::Exists($extended) -or [IO.Directory]::Exists($extended)
}

function Move-FailedNewCopyAside([string]$path, [string]$parent, [string]$leaf) {
    # Only recover an exact, new child path that this downloader reserved. A
    # rename preserves the incomplete files for inspection and frees the name
    # for a retry without recursively deleting a player's data.
    $expected = [IO.Path]::GetFullPath((Join-Path $parent $leaf))
    $candidate = [IO.Path]::GetFullPath($path)
    if (![string]::Equals($candidate, $expected, [StringComparison]::OrdinalIgnoreCase) -or
        [string]::Equals($candidate, [IO.Path]::GetPathRoot($candidate), [StringComparison]::OrdinalIgnoreCase)) {
        Write-Warning "Refusing to move an unexpected failed download path: $candidate"
        return
    }
    $item = Get-Item -LiteralPath $candidate -Force -ErrorAction SilentlyContinue
    if (!$item) { return }
    if (!$item.PSIsContainer -or ($item.Attributes -band [IO.FileAttributes]::ReparsePoint)) {
        Write-Warning "The failed download path needs manual inspection: $candidate"
        return
    }
    $aside = Join-Path $parent ($leaf + '.failed-' + [guid]::NewGuid().ToString('N').Substring(0,8))
    try {
        Move-Item -LiteralPath $candidate -Destination $aside
        Write-Warning "Preserved an incomplete download at $aside. Rerun the helper to retry; the verified download cache is retained."
    } catch {
        Write-Warning "Could not move the incomplete folder at $candidate. Do not launch it; rename it manually before retrying."
    }
}

$releaseBase = "https://github.com/shadowofze/OfflineDAoC/releases/download/v$ReleaseVersion"
$rootFolder = "OfflineDAoC-v$ReleaseVersion"
$partPattern = '^' + [regex]::Escape($rootFolder) + '\.zip\.\d{3}$'
$target = [IO.Path]::GetFullPath($Destination)
if (Test-ExistsLong $target) { throw 'Destination already exists. Choose a NEW folder; existing games and saves are never overwritten.' }
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
    $expectedBase = switch ($ReleaseVersion) {
        '0.31b' { '0.31' }
        '0.32b' { '0.32' }
        default { throw 'Unexpected optional release version.' }
    }
    $expectedPatchRoot = "OfflineDAoC-Sluaghbinder-v$ReleaseVersion-patch"
    if ($manifest.BaseVersion -ne $expectedBase -or !$manifest.PatchName -or
        $manifest.PatchRootFolder -ne $expectedPatchRoot -or
        $manifest.PatchSHA256 -notmatch '^[a-f0-9]{64}$') { throw "Unexpected v$ReleaseVersion optional patch manifest." }
    $parent = Split-Path -Parent $target
    $leaf = Split-Path -Leaf $target
    $baseTarget = Join-Path $parent ($leaf + "-v$expectedBase-base")
    if (Test-ExistsLong $baseTarget) { throw "Base staging folder already exists: $baseTarget" }
    $legacyBaseTarget = if ($ReleaseVersion -eq '0.32b') { $baseTarget + '-v0.31-base' } else { $null }
    if ($legacyBaseTarget -and (Test-ExistsLong $legacyBaseTarget)) {
        throw "Legacy base staging folder already exists: $legacyBaseTarget"
    }
    try {
    Write-Host "Downloading the preserved v$expectedBase playable baseline first..."
    & $PSCommandPath -ReleaseVersion $manifest.BaseVersion -Destination $baseTarget
    if ($LASTEXITCODE -and $LASTEXITCODE -ne 0) { throw "The v$expectedBase baseline download failed." }

    $patchPath = Join-Path $cache $manifest.PatchName
    $patchValid = (Test-Path -LiteralPath $patchPath) -and
        (Get-Item -LiteralPath $patchPath).Length -eq [int64]$manifest.PatchBytes
    if ($patchValid) { $patchValid = (Get-FileHash -LiteralPath $patchPath -Algorithm SHA256).Hash -eq $manifest.PatchSHA256 }
    if (!$patchValid) {
        Write-Host "Downloading $($manifest.PatchName)..."
        Invoke-WebRequest -UseBasicParsing -Uri "$releaseBase/$($manifest.PatchName)" -OutFile $patchPath
        if ((Get-Item -LiteralPath $patchPath).Length -ne [int64]$manifest.PatchBytes -or
            (Get-FileHash -LiteralPath $patchPath -Algorithm SHA256).Hash -ne $manifest.PatchSHA256) {
            throw "Sluaghbinder patch verification failed. The v$expectedBase baseline is intact; run again to retry."
        }
    }
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    # A release helper can live in a very deep Downloads folder. Extracting
    # below .downloads would push nested source paths past Windows PowerShell
    # 5.1's path limit. Use a short, unique temporary folder instead.
    $patchExtractParent = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
    $patchExtractLeaf = 'odcp-' + [guid]::NewGuid().ToString('N').Substring(0,12)
    $patchExtract = [IO.Path]::GetFullPath((Join-Path $patchExtractParent $patchExtractLeaf))
    if (Test-Path -LiteralPath $patchExtract) { throw "Patch extraction folder already exists: $patchExtract" }
    New-Item -ItemType Directory -Path $patchExtract | Out-Null
    try {
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
            if ($resolved.Length -ge 240) {
                throw 'The Windows temporary folder is too deep for this patch. Set TEMP to a shorter writable folder and retry.'
            }
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
    if ($LASTEXITCODE -and $LASTEXITCODE -ne 0) { throw "Sluaghbinder patch installation failed; the v$expectedBase baseline remains intact." }
    Write-Host "Verified v$ReleaseVersion optional Sluaghbinder expansion and installed it to $target"
    Write-Host "The unmodified v$expectedBase staging copy is at $baseTarget for rollback."
    } finally {
        # The ZIP is retained in .downloads. Only this exact generated temp
        # child is discarded, never an existing game or a redirected folder.
        $expectedExtract = [IO.Path]::GetFullPath((Join-Path $patchExtractParent $patchExtractLeaf))
        $extractItem = Get-Item -LiteralPath $patchExtract -Force -ErrorAction SilentlyContinue
        if ($extractItem -and
            [string]::Equals($patchExtract, $expectedExtract, [StringComparison]::OrdinalIgnoreCase) -and
            $extractItem.PSIsContainer -and
            -not ($extractItem.Attributes -band [IO.FileAttributes]::ReparsePoint)) {
            try { Remove-Item -LiteralPath $patchExtract -Recurse -Force }
            catch { Write-Warning "Temporary patch files could not be removed from $patchExtract." }
        }
    }
    exit 0
    } catch {
        $failure = $_
        if ($ReleaseVersion -eq '0.32b') {
            Move-FailedNewCopyAside $target $parent $leaf
            Move-FailedNewCopyAside $baseTarget $parent ($leaf + '-v0.32-base')
            Move-FailedNewCopyAside $legacyBaseTarget $parent ($leaf + '-v0.32-base-v0.31-base')
        }
        throw $failure
    }
}

# v0.32 is a copy-first update of the preserved v0.31 playable baseline. The
# old folder is kept alongside the new one for a straightforward rollback. A
# clean world snapshot is used only in the NEW copy, never in an existing save.
if ($manifest.Mode -eq 'delta' -and $ReleaseVersion -eq '0.32') {
    $expectedPatchRoot = 'OfflineDAoC-v0.32-darkness-falls-beta-update'
    if ($manifest.BaseVersion -ne '0.31' -or !$manifest.PatchName -or
        $manifest.PatchRootFolder -ne $expectedPatchRoot -or
        $manifest.PatchSHA256 -notmatch '^[a-f0-9]{64}$') {
        throw 'Unexpected v0.32 update manifest.'
    }
    $parent = Split-Path -Parent $target
    $leaf = Split-Path -Leaf $target
    $baseTarget = Join-Path $parent ($leaf + '-v0.31-base')
    if (Test-ExistsLong $baseTarget) { throw "Base staging folder already exists: $baseTarget" }
    try {
    Write-Host 'Downloading a fresh v0.31 playable baseline for rollback...'
    & $PSCommandPath -ReleaseVersion '0.31' -Destination $baseTarget
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
            throw 'v0.32 update verification failed. The v0.31 baseline remains intact; run again to retry.'
        }
    }

    [IO.Directory]::CreateDirectory((Get-ExtendedPath $target)) | Out-Null
    & robocopy.exe $baseTarget $target /E /COPY:DAT /DCOPY:DAT /R:2 /W:1 /NFL /NDL /NP /NJH /NJS | Out-Null
    if ($LASTEXITCODE -gt 7) { throw 'Copying the v0.31 base into the new v0.32 folder failed.' }
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $zip = [IO.Compression.ZipFile]::OpenRead($patchPath)
    try {
        $prefix = $expectedPatchRoot + '/'
        $targetPrefix = $target.TrimEnd('\') + '\'
        $seen = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
        foreach ($entry in $zip.Entries) {
            $entryName = $entry.FullName.Replace('\','/')
            if (!$entryName.StartsWith($prefix,[StringComparison]::Ordinal) -or
                $entryName.Contains(':') -or $entryName.Contains('/../') -or
                $entryName.EndsWith('/..')) { throw 'Unexpected v0.32 update layout.' }
            $relative = $entryName.Substring($prefix.Length)
            if (!$relative) { continue }
            if (!$seen.Add($relative)) { throw "Duplicate v0.32 update entry: $relative" }
            $resolved = [IO.Path]::GetFullPath((Join-Path $target $relative))
            if (!$resolved.StartsWith($targetPrefix,[StringComparison]::OrdinalIgnoreCase)) {
                throw 'Unsafe v0.32 update entry.'
            }
        }
        foreach ($entry in $zip.Entries) {
            $relative = $entry.FullName.Replace('\','/').Substring($prefix.Length)
            if (!$relative) { continue }
            $resolved = Join-Path $target $relative
            if ($entry.FullName.EndsWith('/')) {
                [IO.Directory]::CreateDirectory((Get-ExtendedPath $resolved)) | Out-Null
                continue
            }
            [IO.Directory]::CreateDirectory((Get-ExtendedPath (Split-Path -Parent $resolved))) | Out-Null
            [IO.Compression.ZipFileExtensions]::ExtractToFile($entry,(Get-ExtendedPath $resolved),$true)
        }
    } finally { $zip.Dispose() }
    foreach ($required in @('runtime\OfflineDAoC.exe','runtime\server\GameServer.dll',
            'runtime\server\navmesh\zone249.nav','runtime\data\opendaoc.sqlite3.db')) {
        if (!(Test-Path -LiteralPath (Join-Path $target $required) -PathType Leaf)) {
            throw "The v0.32 copy is missing $required. The v0.31 base remains intact."
        }
    }
    Write-Host "Verified v0.32 Darkness Falls Beta in $target"
    Write-Host "The untouched v0.31 baseline is at $baseTarget for rollback. Nothing was started automatically."
    exit 0
    } catch {
        $failure = $_
        Move-FailedNewCopyAside $target $parent $leaf
        Move-FailedNewCopyAside $baseTarget $parent ($leaf + '-v0.31-base')
        throw $failure
    }
}

# v0.31 is intentionally a small, hash-verified update over the immutable v0.3
# playable seed. This legacy download path remains unchanged.
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
            if ($entryName.EndsWith('/')) { [IO.Directory]::CreateDirectory((Get-ExtendedPath $resolved)) | Out-Null; continue }
            [IO.Directory]::CreateDirectory((Get-ExtendedPath (Split-Path -Parent $resolved))) | Out-Null
            [IO.Compression.ZipFileExtensions]::ExtractToFile($entry,(Get-ExtendedPath $resolved),$true)
        }
    } finally { $zip.Dispose() }
    $worldPatcher = Join-Path $target 'runtime\world-patches\OfflineDaoc.WorldPatch.exe'
    $beachRatSql = Join-Path $target 'tools\world-patches\shannon-beach-rat-camp.sql'
    $worldDatabase = Join-Path $target 'runtime\data\opendaoc.sqlite3.db'
    if (!(Test-Path -LiteralPath $worldPatcher -PathType Leaf) -or
        !(Test-Path -LiteralPath $beachRatSql -PathType Leaf)) {
        throw 'The v0.31 update is missing its Shannon Estuary world patch. Use the latest release manifest and update asset.'
    }
    $worldPatchArgs = '--database "' + $worldDatabase + '" --sql "' + $beachRatSql + '"'
    $worldPatchProcess = Start-Process -FilePath $worldPatcher -ArgumentList $worldPatchArgs -Wait -PassThru -NoNewWindow
    if ($worldPatchProcess.ExitCode -ne 0) {
        throw 'The Shannon Estuary world patch failed in the new copy. The original installation was not changed.'
    }
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
    [IO.Directory]::CreateDirectory((Get-ExtendedPath $target)) | Out-Null
    foreach ($entry in $zip.Entries) {
        $relative = $entry.FullName.Substring($prefix.Length)
        if (!$relative) { continue }
        $resolved = Join-Path $target $relative
        if ($entry.FullName.EndsWith('/')) { [IO.Directory]::CreateDirectory((Get-ExtendedPath $resolved)) | Out-Null; continue }
        [IO.Directory]::CreateDirectory((Get-ExtendedPath (Split-Path -Parent $resolved))) | Out-Null
        [IO.Compression.ZipFileExtensions]::ExtractToFile($entry,(Get-ExtendedPath $resolved),$false)
    }
} finally { $zip.Dispose() }
Write-Host "Verified and extracted to $target"
Write-Host 'Read READ ME FIRST.txt, then open START OFFLINE DAOC.cmd. Nothing has been started automatically.'
Write-Host 'Downloads are retained in .downloads so interrupted downloads can be retried.'
