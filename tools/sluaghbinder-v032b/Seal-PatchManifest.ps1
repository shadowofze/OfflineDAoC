[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)][string]$BasePath,
    [string]$PayloadPath = (Join-Path $PSScriptRoot 'payload-v0.32b')
)
$ErrorActionPreference = 'Stop'

$packageRoot = [IO.Path]::GetFullPath($PSScriptRoot)
$base = [IO.Path]::GetFullPath($BasePath)
$payload = [IO.Path]::GetFullPath($PayloadPath)
function Get-ExtendedPath([string]$path) {
    $full = [IO.Path]::GetFullPath($path)
    return $(if ($full.StartsWith('\\?\')) { $full } else { '\\?\' + $full })
}

function Get-FileSHA256([string]$path) {
    # Windows PowerShell's Get-FileHash resolver can reject a valid file once
    # the optional package lives under a long Desktop/release folder.
    return (Get-FileHash -LiteralPath (Get-ExtendedPath $path) -Algorithm SHA256).Hash
}
if (!(Test-Path -LiteralPath $base -PathType Container) -or
    !(Test-Path -LiteralPath $payload -PathType Container)) {
    throw 'Provide a complete normal v0.32 base and staged v0.32b payload directory.'
}
if (Test-Path -LiteralPath (Join-Path $base 'runtime\account.txt')) {
    throw 'The release base has an account.txt. Use a fresh, clean public v0.32 download.'
}
if (Test-Path -LiteralPath (Join-Path $base '.sluaghbinder\patch-manifest.json')) {
    throw 'The release base already has the optional class.'
}
$python = Get-Command python.exe -ErrorAction Stop
& $python.Source (Join-Path $packageRoot 'check_public_base.py') --database (Join-Path $base 'runtime\data\opendaoc.sqlite3.db')
if ($LASTEXITCODE -ne 0) { throw 'The public v0.32 world check failed.' }

$baseFiles = @()
foreach ($relative in @('runtime\OfflineDAoC.dll','runtime\server\GameServer.dll')) {
    $path = Join-Path $base $relative
    if (!(Test-Path -LiteralPath $path -PathType Leaf)) { throw "Base file is missing: $relative" }
    $baseFiles += [ordered]@{
        Path=$relative; Bytes=(Get-Item -LiteralPath $path).Length
        SHA256=(Get-FileSHA256 $path)
    }
}
$baseLauncherBytes = [IO.File]::ReadAllBytes((Join-Path $base 'runtime\OfflineDAoC.dll'))
$baseLauncherText = [Text.Encoding]::ASCII.GetString($baseLauncherBytes) + [Text.Encoding]::Unicode.GetString($baseLauncherBytes)
if ($baseLauncherText -notmatch '(?<![0-9.])0\.32(?![0-9A-Za-z.])') {
    throw 'The base launcher does not contain the normal 0.32 label.'
}

$assets = Join-Path $packageRoot 'client-assets-v0.32b'
$clientAssets = @()
foreach ($name in @('Sluaghbinder_ZombieDefender.NIF','Sluaghbinder_Dullahan.NIF',
                   'sluagh_zombie_defender_body.dds','sluagh_dullahan_body.dds')) {
    $path = Join-Path $assets $name
    if (!(Test-Path -LiteralPath $path -PathType Leaf)) { throw "Client asset is missing: $name" }
    $clientAssets += [ordered]@{
        Name=$name; Bytes=(Get-Item -LiteralPath $path).Length
        SHA256=(Get-FileSHA256 $path)
    }
}
$overlay = Join-Path $packageRoot 'overlay.json'
if (!(Test-Path -LiteralPath $overlay -PathType Leaf)) { throw 'Class overlay is missing.' }
$classTables = (Get-Content -LiteralPath $overlay -Raw | ConvertFrom-Json).tables.PSObject.Properties.Name
$expectedTables = @('Specialization','SpellLine','LineXSpell','ClassXSpecialization','SpecXAbility',
                    'Style','Spell','NpcTemplate','NPCEquipment','Mob')
if (@($classTables).Count -ne $expectedTables.Count -or
    @($classTables | Where-Object { $expectedTables -notcontains $_ }).Count -ne 0) {
    throw 'Overlay contains unexpected tables. Never package account, save, or non-class world rows.'
}

$payloadRoot = $payload.TrimEnd([char[]]@('\','/')) + [IO.Path]::DirectorySeparatorChar
$files = @()
foreach ($file in @(Get-ChildItem -LiteralPath $payload -Recurse -File | Sort-Object FullName)) {
    $relative = $file.FullName.Substring($payloadRoot.Length)
    if ($relative -match '(^|[\\/])(account\.txt|opendaoc\.sqlite3\.db|logs?|progress-backups|deployment-backups)([\\/]|$)' -or
        $relative -match '\.(db|sqlite|sqlite3)([.-]|$)') {
        throw "Private database, account, or log payload is forbidden: $relative"
    }
    $files += [ordered]@{
        Path=$relative; Bytes=$file.Length
        SHA256=(Get-FileSHA256 $file.FullName)
        AllowCreate=(!(Test-Path -LiteralPath (Get-ExtendedPath (Join-Path $base $relative))))
    }
}
if (!$files.Count) { throw 'The optional payload has no files.' }
$optionalLauncher = Join-Path $payload 'runtime\OfflineDAoC.dll'
$optionalServer = Join-Path $payload 'runtime\server\GameServer.dll'
if (!(Test-Path -LiteralPath $optionalLauncher -PathType Leaf) -or
    !(Test-Path -LiteralPath $optionalServer -PathType Leaf)) {
    throw 'The optional payload must include the v0.32b launcher and server.'
}
$optionalBytes = [IO.File]::ReadAllBytes($optionalLauncher)
$optionalText = [Text.Encoding]::ASCII.GetString($optionalBytes) + [Text.Encoding]::Unicode.GetString($optionalBytes)
if ($optionalText -notmatch '(?<![0-9.])0\.32b(?![0-9A-Za-z.])') {
    throw 'The optional payload launcher does not contain the 0.32b label.'
}
$patcher = Join-Path $packageRoot 'patcher\OfflineDaoc.SluaghbinderPatch.exe'
if (!(Test-Path -LiteralPath $patcher -PathType Leaf)) {
    throw 'The v0.32b patch executable is missing.'
}
& $patcher --verify-v032-world 69 --database (Join-Path $base 'runtime\data\opendaoc.sqlite3.db')
if ($LASTEXITCODE -ne 0) { throw 'The staged patcher failed the read-only v0.32 world verification.' }

$manifest = [ordered]@{
    Version='0.32b'; BaseVersion='0.32'; Feature='Sluaghbinder'; Overlay='overlay.json'
    OverlaySHA256=(Get-FileSHA256 $overlay)
    PatcherSHA256=(Get-FileSHA256 $patcher)
    BaseFiles=$baseFiles; ClientAssets=$clientAssets; Files=$files
}
$manifestPath = Join-Path $packageRoot 'patch-manifest.json'
$manifest | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $manifestPath -Encoding UTF8
Write-Host "Sealed v0.32b patch manifest with $($files.Count) payload files against clean normal v0.32."
