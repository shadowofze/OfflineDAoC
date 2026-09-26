[CmdletBinding()]
param(
    [string]$BasePath,
    [string]$Destination
)

$ErrorActionPreference = 'Stop'
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

function Pick-Folder([string]$description) {
    Add-Type -AssemblyName System.Windows.Forms
    $dialog = New-Object System.Windows.Forms.FolderBrowserDialog
    $dialog.Description = $description
    $dialog.ShowNewFolderButton = $false
    if ($dialog.ShowDialog() -ne [System.Windows.Forms.DialogResult]::OK) {
        throw 'No Offline DAoC folder was selected.'
    }
    return $dialog.SelectedPath
}

function Quote-Identifier([string]$value) {
    return '"' + $value.Replace('"', '""') + '"'
}

function Get-Value($row, [string]$column) {
    $property = $row.PSObject.Properties[$column]
    if ($null -eq $property) { return $null }
    return $property.Value
}

function Resolve-PatchPath([string]$root, [string]$relative) {
    if ([string]::IsNullOrWhiteSpace($relative) -or [IO.Path]::IsPathRooted($relative) -or
        $relative.Contains(':')) { throw "Unsafe patch path: $relative" }
    $base = [IO.Path]::GetFullPath($root).TrimEnd([char[]]@('\','/'))
    $resolved = [IO.Path]::GetFullPath((Join-Path $base $relative))
    if (!$resolved.StartsWith($base + [IO.Path]::DirectorySeparatorChar,
            [StringComparison]::OrdinalIgnoreCase)) { throw "Patch path escapes its folder: $relative" }
    return $resolved
}

function Get-ExtendedPath([string]$path) {
    $full = [IO.Path]::GetFullPath($path)
    return $(if ($full.StartsWith('\\?\')) { $full } else { '\\?\' + $full })
}

function Get-FileSHA256([string]$path) {
    # Get-FileHash in Windows PowerShell 5.1 needs the extended path prefix for
    # deeply nested optional source files under a long Desktop folder.
    return (Get-FileHash -LiteralPath (Get-ExtendedPath $path) -Algorithm SHA256).Hash
}

function Invoke-Db($connection, $transaction, [string]$sql, [hashtable]$parameters = @{}) {
    $command = $connection.CreateCommand()
    $command.CommandText = $sql
    $command.Transaction = $transaction
    foreach ($key in $parameters.Keys) {
        $parameter = $command.Parameters.Add($key, [System.Data.DbType]::Object)
        $value = $parameters[$key]
        $parameter.Value = if ($null -eq $value) { [DBNull]::Value } else { $value }
    }
    return $command.ExecuteNonQuery()
}

function Invoke-Scalar($connection, $transaction, [string]$sql) {
    $command = $connection.CreateCommand()
    $command.CommandText = $sql
    $command.Transaction = $transaction
    return $command.ExecuteScalar()
}

function Get-TableColumns($connection, [string]$table) {
    $command = $connection.CreateCommand()
    $command.CommandText = 'PRAGMA table_info(' + (Quote-Identifier $table) + ')'
    $reader = $command.ExecuteReader()
    try {
        $columns = @()
        while ($reader.Read()) { $columns += [string]$reader['name'] }
        return $columns
    } finally { $reader.Dispose() }
}

function Assert-TableRows($connection, $overlay) {
    foreach ($tableProperty in $overlay.tables.PSObject.Properties) {
        $table = [string]$tableProperty.Name
        $columns = @(Get-TableColumns $connection $table)
        if (!$columns) { throw "The target database is missing required table '$table'." }
        foreach ($column in @($tableProperty.Value.columns)) {
            if ($columns -notcontains [string]$column) {
                throw "The target database table '$table' is missing column '$column'. Use a v0.32 install."
            }
        }
    }
}

function Delete-In($connection, $transaction, [string]$table, [string]$column, [object[]]$values) {
    if (!$values -or !$values.Count) { return }
    $names = @()
    $parameters = @{}
    for ($i = 0; $i -lt $values.Count; $i++) {
        $name = '@v' + $i
        $names += $name
        $parameters[$name] = $values[$i]
    }
    Invoke-Db $connection $transaction ('DELETE FROM ' + (Quote-Identifier $table) + ' WHERE ' + (Quote-Identifier $column) + ' IN (' + ($names -join ',') + ')') $parameters | Out-Null
}

function Apply-Overlay($connection, $overlay) {
    $transaction = $connection.BeginTransaction()
    try {
        Invoke-Db $connection $transaction 'PRAGMA foreign_keys=OFF' | Out-Null
        Delete-In $connection $transaction 'Specialization' 'KeyName' @('SluaghbinderCareer','Sluagh Host',"Abhartach's Rot",'Cairn Oath',"Dullahan's Bulwark","Abhartach's Bane",'Sluagh Covenant',"Sluaghbinder's Legacy",'Epic Spells')
        Delete-In $connection $transaction 'SpellLine' 'KeyName' @('Sluagh Host',"Abhartach's Rot",'Cairn Oath',"Dullahan's Bulwark","Abhartach's Bane",'Sluagh Covenant',"Sluaghbinder's Legacy",'Epic Spells')
        Delete-In $connection $transaction 'LineXSpell' 'LineName' @('Sluagh Host',"Abhartach's Rot",'Cairn Oath',"Dullahan's Bulwark","Abhartach's Bane",'Sluagh Covenant',"Sluaghbinder's Legacy",'Epic Spells')
        Delete-In $connection $transaction 'SpecXAbility' 'Spec' @('SluaghbinderCareer','Sluagh Host',"Abhartach's Rot",'Cairn Oath',"Dullahan's Bulwark","Abhartach's Bane",'Sluagh Covenant',"Sluaghbinder's Legacy",'Epic Spells')
        Invoke-Db $connection $transaction 'DELETE FROM "ClassXSpecialization" WHERE "ClassID"=63' | Out-Null
        Invoke-Db $connection $transaction 'DELETE FROM "Style" WHERE "ClassId"=63' | Out-Null
        Invoke-Db $connection $transaction 'DELETE FROM "Spell" WHERE "Spell_ID" LIKE ''Sluaghbinder_%'' OR "SpellID" BETWEEN 59000 AND 59084' | Out-Null
        Delete-In $connection $transaction 'NpcTemplate' 'TemplateId' @(60170001,60170002,60170003,60170004,60170005,60170006,60170007)
        if ($overlay.tables.PSObject.Properties['NPCEquipment']) {
            Delete-In $connection $transaction 'NPCEquipment' 'TemplateID' @(
                'sluagh_zombie_magician_staff',
                'sluagh_zombie_guardian_mace_shield',
                'sluagh_zombie_priest_mace_buckler',
                'sluagh_cairn_dullahan_flail_shield',
                'SluaghbinderMuirennBlack'
            )
        }
        Delete-In $connection $transaction 'Mob' 'Mob_ID' @('sluaghbinder_trainer_tir_na_nog','sluaghbinder_bound_wisp_tir_na_nog')

        foreach ($tableProperty in $overlay.tables.PSObject.Properties) {
            $table = [string]$tableProperty.Name
            $columns = @($tableProperty.Value.columns | ForEach-Object { [string]$_ })
            $quotedColumns = $columns | ForEach-Object { Quote-Identifier $_ }
            foreach ($row in @($tableProperty.Value.rows)) {
                $names = @()
                $parameters = @{}
                for ($i = 0; $i -lt $columns.Count; $i++) {
                    $name = '@p' + $i
                    $names += $name
                    $parameters[$name] = Get-Value $row $columns[$i]
                }
                Invoke-Db $connection $transaction ('INSERT OR REPLACE INTO ' + (Quote-Identifier $table) + ' (' + ($quotedColumns -join ',') + ') VALUES (' + ($names -join ',') + ')') $parameters | Out-Null
            }
        }
        $transaction.Commit()
    } catch {
        $transaction.Rollback()
        throw
    }
}

function Copy-Tree([string]$source, [string]$target) {
    $robocopyArgs = @($source, $target, '/E', '/COPY:DAT', '/DCOPY:DAT', '/R:2', '/W:1', '/NFL', '/NDL', '/NP', '/NJH', '/NJS')
    $realDirs = @((Join-Path $source 'runtime\logs'), (Join-Path $source 'runtime\progress-backups'), (Join-Path $source 'runtime\deployment-backups'))
    $existingDirs = @($realDirs | Where-Object { Test-Path -LiteralPath $_ })
    if ($existingDirs.Count) { $robocopyArgs += '/XD'; $robocopyArgs += $existingDirs }
    & robocopy.exe @robocopyArgs | Out-Null
    if ($LASTEXITCODE -gt 7) { throw "Copy failed with robocopy exit code $LASTEXITCODE." }
}

if (!$BasePath) { $BasePath = Pick-Folder 'Choose the existing normal Offline DAoC v0.32 folder to copy.' }
$base = [IO.Path]::GetFullPath($BasePath)
if (!(Test-Path -LiteralPath $base -PathType Container)) { throw "Base folder not found: $base" }
$baseMarker = Join-Path $base '.sluaghbinder\patch-manifest.json'
if (Test-Path -LiteralPath $baseMarker) { throw 'This folder already has a Sluaghbinder patch marker. Choose the normal v0.32 folder.' }
$baseDll = Join-Path $base 'runtime\OfflineDAoC.dll'
$baseDb = Join-Path $base 'runtime\data\opendaoc.sqlite3.db'
if (!(Test-Path -LiteralPath $baseDll) -or !(Test-Path -LiteralPath $baseDb)) { throw 'The selected folder is not a complete playable Offline DAoC installation.' }

$packageRoot = [IO.Path]::GetFullPath($PSScriptRoot)
$patchManifestPath = Join-Path $packageRoot 'patch-manifest.json'
$overlayPath = Join-Path $packageRoot 'overlay.json'
if (!(Test-Path -LiteralPath $patchManifestPath) -or
    !(Test-Path -LiteralPath $overlayPath)) {
    throw 'Patch package is incomplete. Download the Sluaghbinder v0.32b release asset, not the repository source ZIP.'
}
$patchManifest = Get-Content -LiteralPath $patchManifestPath -Raw | ConvertFrom-Json
$overlay = Get-Content -LiteralPath $overlayPath -Raw | ConvertFrom-Json
if ($patchManifest.Version -ne '0.32b' -or $patchManifest.BaseVersion -ne '0.32' -or
    $patchManifest.Feature -ne 'Sluaghbinder' -or $overlay.feature -ne 'Sluaghbinder' -or
    $overlay.format -ne 1 -or $overlay.classId -ne 63) { throw 'Unexpected Sluaghbinder v0.32b patch metadata.' }
if ([string]$patchManifest.OverlaySHA256 -notmatch '^[a-fA-F0-9]{64}$' -or
    (Get-FileSHA256 $overlayPath) -ne [string]$patchManifest.OverlaySHA256) {
    throw 'The v0.32b class overlay is missing or does not match the sealed manifest.'
}
$allowedTables = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
foreach ($table in @('Specialization','SpellLine','LineXSpell','ClassXSpecialization','SpecXAbility',
                   'Style','Spell','NpcTemplate','NPCEquipment','Mob')) { [void]$allowedTables.Add($table) }
$overlayTables = @($overlay.tables.PSObject.Properties.Name)
if ($overlayTables.Count -ne $allowedTables.Count -or
    @($overlayTables | Where-Object { !$allowedTables.Contains([string]$_) }).Count -ne 0) {
    throw 'The optional overlay contains unexpected world or progress tables.'
}

$assetDirectory = Join-Path $packageRoot 'client-assets-v0.32b'
if (!(Test-Path -LiteralPath $assetDirectory -PathType Container)) {
    throw 'Patch package is missing client-assets-v0.32b.'
}
$requiredAssets = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
foreach ($name in @('Sluaghbinder_ZombieDefender.NIF','Sluaghbinder_Dullahan.NIF',
                   'sluagh_zombie_defender_body.dds','sluagh_dullahan_body.dds')) { [void]$requiredAssets.Add($name) }
$assetFiles = @($patchManifest.ClientAssets)
if ($assetFiles.Count -ne $requiredAssets.Count) { throw 'The v0.32b client asset hashes are incomplete.' }
$seenAssets = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
foreach ($item in $assetFiles) {
    $name = [string]$item.Name
    if (!$requiredAssets.Contains($name) -or !$seenAssets.Add($name) -or
        [string]$item.SHA256 -notmatch '^[a-fA-F0-9]{64}$' -or [int64]$item.Bytes -le 0) {
        throw "Invalid v0.32b client asset manifest entry: $name"
    }
    $asset = Join-Path $assetDirectory $name
    if (!(Test-Path -LiteralPath $asset -PathType Leaf) -or
        (Get-Item -LiteralPath $asset).Length -ne [int64]$item.Bytes -or
        (Get-FileSHA256 $asset) -ne [string]$item.SHA256) {
        throw "The v0.32b client asset does not match the sealed manifest: $name"
    }
}

# The package is sealed only after normal v0.32 has been built. A placeholder
# hash, an older v0.31 installation, or a customized server fails here before
# the destination folder is created. Progress databases are deliberately not
# hashed, because a player's accounts and characters change during play.
$requiredBaseFiles = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
[void]$requiredBaseFiles.Add('runtime\OfflineDAoC.dll')
[void]$requiredBaseFiles.Add('runtime\server\GameServer.dll')
$baseFiles = @($patchManifest.BaseFiles)
if ($baseFiles.Count -ne $requiredBaseFiles.Count) { throw 'The patch manifest is missing required v0.32 base hashes.' }
$seenBaseFiles = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
foreach ($item in $baseFiles) {
    $relative = [string]$item.Path
    if (!$requiredBaseFiles.Contains($relative) -or !$seenBaseFiles.Add($relative) -or
        [string]$item.SHA256 -notmatch '^[a-fA-F0-9]{64}$' -or [int64]$item.Bytes -le 0) {
        throw "Invalid v0.32 base fingerprint: $relative"
    }
    $path = Resolve-PatchPath $base $relative
    if (!(Test-Path -LiteralPath $path -PathType Leaf) -or
        (Get-Item -LiteralPath $path).Length -ne [int64]$item.Bytes -or
        (Get-FileSHA256 $path) -ne [string]$item.SHA256) {
        throw "This is not the matching normal v0.32 base: $relative"
    }
}
$patcher = Join-Path $packageRoot 'patcher\OfflineDaoc.SluaghbinderPatch.exe'
if (!(Test-Path -LiteralPath $patcher -PathType Leaf) -or
    [string]$patchManifest.PatcherSHA256 -notmatch '^[a-fA-F0-9]{64}$' -or
    (Get-FileSHA256 $patcher) -ne [string]$patchManifest.PatcherSHA256) {
    throw 'The v0.32b patch executable is missing or does not match the sealed manifest.'
}
& $patcher --verify-v032-world 69 --database $baseDb
if ($LASTEXITCODE -ne 0) { throw 'The selected base does not contain the public v0.32 Darkness Falls world.' }
if (Get-Process -Name 'OfflineDAoC','CoreServer','connect' -ErrorAction SilentlyContinue) {
    throw 'Close the launcher, server, and game before making an optional copy.'
}

if (!$Destination) {
    $parent = Split-Path -Parent $base
    $leaf = Split-Path -Leaf $base
    $Destination = Join-Path $parent ($leaf + '-Sluaghbinder-v0.32b')
}
$destination = [IO.Path]::GetFullPath($Destination)
$baseParent = [IO.Path]::GetFullPath((Split-Path -Parent $base)).TrimEnd([char[]]@('\','/'))
$destinationParent = [IO.Path]::GetFullPath((Split-Path -Parent $destination)).TrimEnd([char[]]@('\','/'))
if (![string]::Equals($destinationParent, $baseParent, [StringComparison]::OrdinalIgnoreCase) -or
    [string]::Equals($destination, $base, [StringComparison]::OrdinalIgnoreCase)) {
    throw 'The Sluaghbinder destination must be a new sibling folder beside the normal v0.32 base.'
}
if (Test-Path -LiteralPath $destination) { throw "Destination already exists: $destination. Choose a new folder; no files were overwritten." }
New-Item -ItemType Directory -Path $destination | Out-Null

try {
    Write-Host 'Copying the selected installation to a new sibling folder...'
    Copy-Tree $base $destination
    $backupRoot = Join-Path $destination 'runtime\.sluaghbinder-backup'
    New-Item -ItemType Directory -Path $backupRoot -Force | Out-Null
    $targetDb = Join-Path $destination 'runtime\data\opendaoc.sqlite3.db'
    Copy-Item -LiteralPath $targetDb -Destination (Join-Path $backupRoot 'opendaoc.sqlite3.db') -Force

# Use the bundled .NET patch executable rather than PowerShell's legacy
# SQLite loader. Windows PowerShell 5.1 cannot load a netstandard2.1 provider;
# the small executable works on the same .NET runtime used by the launcher.
$clientApp = Join-Path $destination 'runtime\client-opendaoc\app'
$assetStage = Resolve-PatchPath $destination 'runtime\.sluaghbinder-client-stage'
if (Test-Path -LiteralPath $assetStage) { throw 'Client asset staging folder already exists.' }
$assetArgs = '--client-app "' + $clientApp + '" --asset-dir "' + $assetDirectory + '" --asset-output "' + $assetStage + '"'
$assetProcess = Start-Process -FilePath $patcher -ArgumentList $assetArgs -Wait -PassThru -NoNewWindow
if ($assetProcess.ExitCode -ne 0) { throw 'Sluaghbinder client asset preparation failed; the original installation was not modified.' }

$patcherArgs = '--database "' + $targetDb + '" --overlay "' + $overlayPath + '" --require-v032-world 69'
$patcherProcess = Start-Process -FilePath $patcher -ArgumentList $patcherArgs -Wait -PassThru -NoNewWindow
if ($patcherProcess.ExitCode -ne 0) { throw 'The Sluaghbinder database migration failed; the original installation was not modified.' }

$files = @($patchManifest.Files)
if (!$files.Count) { throw 'Patch manifest has no payload files.' }
$seenPaths = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
$payloadEntries = @()
foreach ($item in $files) {
    $relative = [string]$item.Path
    if (!$seenPaths.Add($relative)) { throw "Duplicate patch payload path: $relative" }
    if ($relative -match '(^|[\\/])(account\.txt|opendaoc\.sqlite3\.db|logs?)([\\/]|$)') {
        throw "Private progress or account data is forbidden in the patch payload: $relative"
    }
    $payload = Get-ExtendedPath (Resolve-PatchPath (Join-Path $packageRoot 'payload-v0.32b') $relative)
    $target = Get-ExtendedPath (Resolve-PatchPath $destination $relative)
    if (!(Test-Path -LiteralPath $payload -PathType Leaf)) { throw "Patch payload is missing: $relative" }
    if ((Get-FileSHA256 $payload) -ne $item.SHA256) { throw "Patch payload hash mismatch: $relative" }
    if ((Test-Path -LiteralPath $target) -and !(Test-Path -LiteralPath $target -PathType Leaf)) {
        throw "Patch target is not a file: $relative"
    }
    $existedBefore = Test-Path -LiteralPath $target -PathType Leaf
    if (!$existedBefore -and !([bool]$item.AllowCreate)) {
        throw "The selected base is missing patch target: $relative"
    }
    $payloadEntries += [pscustomobject]@{
        Path=$relative; Payload=$payload; Target=$target; ExistedBefore=$existedBefore; SHA256=[string]$item.SHA256
    }
}

# Build the two private meshes and three catalog/texture archives from this
# copied client's own verified base assets. This merges named rows and DDS
# entries instead of replacing an entire archive from somebody else's game.
$clientAssetPaths = @(
    'gamedata.mpk',
    'figures\skins\skin099.mpk',
    'figures\skins\skin106.mpk',
    'figures\Sluaghbinder_ZombieDefender.NIF',
    'figures\Sluaghbinder_Dullahan.NIF'
)
foreach ($assetRelative in $clientAssetPaths) {
    $relative = 'runtime\client-opendaoc\app\' + $assetRelative
    if (!$seenPaths.Add($relative)) { throw "Duplicate patch payload path: $relative" }
    $payload = Get-ExtendedPath (Resolve-PatchPath $assetStage $assetRelative)
    $target = Get-ExtendedPath (Resolve-PatchPath $destination $relative)
    if (!(Test-Path -LiteralPath $payload -PathType Leaf)) { throw "Prepared client asset is missing: $assetRelative" }
    if ((Test-Path -LiteralPath $target) -and !(Test-Path -LiteralPath $target -PathType Leaf)) {
        throw "Client asset target is not a file: $assetRelative"
    }
    $existedBefore = Test-Path -LiteralPath $target -PathType Leaf
    if (!$existedBefore -and !$assetRelative.EndsWith('.NIF', [StringComparison]::OrdinalIgnoreCase)) {
        throw "The selected base is missing a required client archive: $assetRelative"
    }
    $payloadEntries += [pscustomobject]@{
        Path=$relative; Payload=$payload; Target=$target; ExistedBefore=$existedBefore
        SHA256=(Get-FileSHA256 $payload)
    }
}

$replacements = @()
foreach ($entry in $payloadEntries) {
    $relative = [string]$entry.Path
    $target = [string]$entry.Target
    $backup = Get-ExtendedPath (Join-Path $backupRoot $relative)
    $before = $null
    if ($entry.ExistedBefore) {
        New-Item -ItemType Directory -Path (Split-Path -Parent $backup) -Force | Out-Null
        Copy-Item -LiteralPath $target -Destination $backup -Force
        $before = (Get-FileSHA256 $target)
    }
    New-Item -ItemType Directory -Path (Split-Path -Parent $target) -Force | Out-Null
    Copy-Item -LiteralPath $entry.Payload -Destination $target -Force
    $after = (Get-FileSHA256 $target)
    if ($after -ne $entry.SHA256) { throw "Installed payload hash mismatch: $relative" }
    $replacements += [pscustomobject]@{
        Path=$relative; ExistedBefore=[bool]$entry.ExistedBefore
        OriginalSHA256=$before; PatchedSHA256=$after
    }
}

$toolRoot = Join-Path $destination 'tools'
New-Item -ItemType Directory -Path $toolRoot -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $packageRoot 'Rollback-Sluaghbinder.ps1') -Destination (Join-Path $toolRoot 'Rollback-Sluaghbinder.ps1') -Force
$rollbackCmd = Join-Path $destination 'ROLLBACK SLAUGHBINDER PATCH.cmd'
Set-Content -LiteralPath $rollbackCmd -Encoding ASCII -Value @(
    '@echo off'
    'setlocal'
    'powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0tools\Rollback-Sluaghbinder.ps1"'
    'pause'
)
$markerRoot = Join-Path $destination '.sluaghbinder'
New-Item -ItemType Directory -Path $markerRoot -Force | Out-Null
$record = [ordered]@{
    Version='0.32b'; BaseVersion='0.32'; Feature='Sluaghbinder'; BasePath=$base; AppliedUtc=(Get-Date).ToUniversalTime().ToString('o')
    DatabaseBackup='runtime\.sluaghbinder-backup\opendaoc.sqlite3.db'; Files=$replacements
}
$record | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $markerRoot 'patch-manifest.json') -Encoding UTF8
if ($assetStage -eq (Resolve-PatchPath $destination 'runtime\.sluaghbinder-client-stage')) {
    Remove-Item -LiteralPath (Get-ExtendedPath $assetStage) -Recurse -Force
}
Write-Host "Sluaghbinder v0.32b installed into: $destination"
Write-Host 'The original installation was not modified. Start the new folder with START OFFLINE DAOC.cmd.'
Write-Host 'Use ROLLBACK SLAUGHBINDER PATCH.cmd in the new folder to restore its pre-patch files and database.'
} catch {
    # Only the destination created by this invocation can be discarded. Check
    # it again here so a failed patch can never recurse into the base, a
    # different parent, or a redirected junction/symlink.
    $failure = $_
    $createdCopy = Get-Item -LiteralPath $destination -Force -ErrorAction SilentlyContinue
    $safeParent = [string]::Equals(
        [IO.Path]::GetFullPath((Split-Path -Parent $destination)).TrimEnd([char[]]@('\','/')),
        $baseParent, [StringComparison]::OrdinalIgnoreCase)
    if ($createdCopy -and $createdCopy.PSIsContainer -and
        -not ($createdCopy.Attributes -band [IO.FileAttributes]::ReparsePoint) -and
        $safeParent -and -not [string]::Equals($destination, $base, [StringComparison]::OrdinalIgnoreCase)) {
        try {
            Remove-Item -LiteralPath (Get-ExtendedPath $destination) -Recurse -Force
            Write-Warning "The incomplete v0.32b copy was removed: $destination. The normal v0.32 base is unchanged."
        } catch {
            Write-Warning "Could not remove the incomplete v0.32b copy at $destination. Do not launch it; inspect or delete that folder manually. The normal v0.32 base is unchanged."
        }
    } else {
        Write-Warning "The incomplete destination could not be safely identified at $destination. Do not launch it; inspect it manually. The normal v0.32 base is unchanged."
    }
    throw $failure
}
