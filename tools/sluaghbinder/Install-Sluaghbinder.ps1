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
                throw "The target database table '$table' is missing column '$column'. Use a clean v0.3 or v0.31 install."
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
    $excluded = @(
        (Join-Path $source 'runtime\logs'),
        (Join-Path $source 'runtime\progress-backups'),
        (Join-Path $source 'runtime\deployment-backups'),
        (Join-Path $source 'runtime\server\rollback-*'),
        (Join-Path $source 'runtime\client-opendaoc\app\rollback-*')
    )
    $xd = @($excluded | Where-Object { $_ -notlike '*`**' })
    $robocopyArgs = @($source, $target, '/E', '/COPY:DAT', '/DCOPY:DAT', '/R:2', '/W:1', '/NFL', '/NDL', '/NP', '/NJH', '/NJS')
    $realDirs = @((Join-Path $source 'runtime\logs'), (Join-Path $source 'runtime\progress-backups'), (Join-Path $source 'runtime\deployment-backups'))
    $existingDirs = @($realDirs | Where-Object { Test-Path -LiteralPath $_ })
    if ($existingDirs.Count) { $robocopyArgs += '/XD'; $robocopyArgs += $existingDirs }
    & robocopy.exe @robocopyArgs | Out-Null
    if ($LASTEXITCODE -gt 7) { throw "Copy failed with robocopy exit code $LASTEXITCODE." }
}

if (!$BasePath) { $BasePath = Pick-Folder 'Choose the existing Offline DAoC v0.3 or v0.31 folder to copy.' }
$base = [IO.Path]::GetFullPath($BasePath)
if (!(Test-Path -LiteralPath $base -PathType Container)) { throw "Base folder not found: $base" }
$baseMarker = Join-Path $base '.sluaghbinder\patch-manifest.json'
if (Test-Path -LiteralPath $baseMarker) { throw 'This folder already has the Sluaghbinder patch marker. Choose the original v0.3/v0.31 folder.' }
$baseDll = Join-Path $base 'runtime\OfflineDAoC.dll'
$baseDb = Join-Path $base 'runtime\data\opendaoc.sqlite3.db'
if (!(Test-Path -LiteralPath $baseDll) -or !(Test-Path -LiteralPath $baseDb)) { throw 'The selected folder is not a complete playable Offline DAoC installation.' }
$labelBytes = [IO.File]::ReadAllBytes($baseDll)
$labelText = ([Text.Encoding]::ASCII.GetString($labelBytes) + [Text.Encoding]::Unicode.GetString($labelBytes))
if ($labelText -notmatch '0\.3') { throw 'This optional patch accepts only the public v0.3/v0.31 launcher. The private 0.4 build and unknown builds are refused.' }

$packageRoot = [IO.Path]::GetFullPath($PSScriptRoot)
$patchManifestPath = Join-Path $packageRoot 'patch-manifest.json'
$overlayPath = Join-Path $packageRoot 'overlay.json'
$worldPatchPath = Join-Path $packageRoot 'world-patches\shannon-beach-rat-camp.sql'
if (!(Test-Path -LiteralPath $patchManifestPath) -or
    !(Test-Path -LiteralPath $overlayPath) -or
    !(Test-Path -LiteralPath $worldPatchPath)) {
    throw 'Patch package is incomplete. Download the latest Sluaghbinder v0.31b release asset, not the repository source ZIP.'
}
$patchManifest = Get-Content -LiteralPath $patchManifestPath -Raw | ConvertFrom-Json
$overlay = Get-Content -LiteralPath $overlayPath -Raw | ConvertFrom-Json
if ($patchManifest.Version -ne '0.31b' -or $overlay.feature -ne 'Sluaghbinder') { throw 'Unexpected Sluaghbinder patch metadata.' }

if (!$Destination) {
    $parent = Split-Path -Parent $base
    $leaf = Split-Path -Leaf $base
    $Destination = Join-Path $parent ($leaf + '-Sluaghbinder-v0.31b')
}
$destination = [IO.Path]::GetFullPath($Destination)
if (Test-Path -LiteralPath $destination) { throw "Destination already exists: $destination. Choose a new folder; no files were overwritten." }
New-Item -ItemType Directory -Path $destination | Out-Null

try {
    Write-Host 'Copying the selected installation to a new sibling folder...'
    Copy-Tree $base $destination
    $backupRoot = Join-Path $destination 'runtime\.sluaghbinder-backup'
    New-Item -ItemType Directory -Path $backupRoot -Force | Out-Null
    $targetDb = Join-Path $destination 'runtime\data\opendaoc.sqlite3.db'
    Copy-Item -LiteralPath $targetDb -Destination (Join-Path $backupRoot 'opendaoc.sqlite3.db') -Force

} catch {
    Remove-Item -LiteralPath $destination -Recurse -Force -ErrorAction SilentlyContinue
    throw
}

# Use the bundled .NET patch executable rather than PowerShell's legacy
# SQLite loader. Windows PowerShell 5.1 cannot load a netstandard2.1 provider;
# the small executable works on the same .NET runtime used by the launcher.
$patcher = Join-Path $packageRoot 'patcher\OfflineDaoc.SluaghbinderPatch.exe'
if (!(Test-Path -LiteralPath $patcher)) { throw 'Patch package is missing patcher\OfflineDaoc.SluaghbinderPatch.exe.' }
$assetDirectory = Join-Path $packageRoot 'client-assets-v0.31b'
if (!(Test-Path -LiteralPath $assetDirectory -PathType Container)) {
    throw 'Patch package is missing client-assets-v0.31b.'
}
$clientApp = Join-Path $destination 'runtime\client-opendaoc\app'
$assetStage = Resolve-PatchPath $destination 'runtime\.sluaghbinder-client-stage'
if (Test-Path -LiteralPath $assetStage) { throw 'Client asset staging folder already exists.' }
$assetArgs = '--client-app "' + $clientApp + '" --asset-dir "' + $assetDirectory + '" --asset-output "' + $assetStage + '"'
$assetProcess = Start-Process -FilePath $patcher -ArgumentList $assetArgs -Wait -PassThru -NoNewWindow
if ($assetProcess.ExitCode -ne 0) { throw 'Sluaghbinder client asset preparation failed; the original installation was not modified.' }

$patcherArgs = '--database "' + $targetDb + '" --overlay "' + $overlayPath + '" --world-patch "' + $worldPatchPath + '"'
$patcherProcess = Start-Process -FilePath $patcher -ArgumentList $patcherArgs -Wait -PassThru -NoNewWindow
if ($patcherProcess.ExitCode -ne 0) { throw 'The Sluaghbinder database migration failed; the original installation was not modified.' }

$files = @($patchManifest.Files)
if (!$files.Count) { throw 'Patch manifest has no payload files.' }
$seenPaths = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
$payloadEntries = @()
foreach ($item in $files) {
    $relative = [string]$item.Path
    if (!$seenPaths.Add($relative)) { throw "Duplicate patch payload path: $relative" }
    $payload = Resolve-PatchPath (Join-Path $packageRoot 'payload-v0.31b') $relative
    $target = Resolve-PatchPath $destination $relative
    if (!(Test-Path -LiteralPath $payload -PathType Leaf)) { throw "Patch payload is missing: $relative" }
    if ((Get-FileHash -LiteralPath $payload -Algorithm SHA256).Hash -ne $item.SHA256) { throw "Patch payload hash mismatch: $relative" }
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
    $payload = Resolve-PatchPath $assetStage $assetRelative
    $target = Resolve-PatchPath $destination $relative
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
        SHA256=(Get-FileHash -LiteralPath $payload -Algorithm SHA256).Hash
    }
}

$replacements = @()
foreach ($entry in $payloadEntries) {
    $relative = [string]$entry.Path
    $target = [string]$entry.Target
    $backup = Join-Path $backupRoot $relative
    $before = $null
    if ($entry.ExistedBefore) {
        New-Item -ItemType Directory -Path (Split-Path -Parent $backup) -Force | Out-Null
        Copy-Item -LiteralPath $target -Destination $backup -Force
        $before = (Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash
    }
    New-Item -ItemType Directory -Path (Split-Path -Parent $target) -Force | Out-Null
    Copy-Item -LiteralPath $entry.Payload -Destination $target -Force
    $after = (Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash
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
    Version='0.31b'; Feature='Sluaghbinder'; BasePath=$base; AppliedUtc=(Get-Date).ToUniversalTime().ToString('o')
    DatabaseBackup='runtime\.sluaghbinder-backup\opendaoc.sqlite3.db'; Files=$replacements
}
$record | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $markerRoot 'patch-manifest.json') -Encoding UTF8
if ($assetStage -eq (Resolve-PatchPath $destination 'runtime\.sluaghbinder-client-stage')) {
    Remove-Item -LiteralPath $assetStage -Recurse -Force
}
Write-Host "Sluaghbinder v0.31b installed into: $destination"
Write-Host 'The original installation was not modified. Start the new folder with START OFFLINE DAOC.cmd.'
Write-Host 'Use ROLLBACK SLAUGHBINDER PATCH.cmd in the new folder to restore its pre-patch files and database.'
