param([switch]$Apply)
$ErrorActionPreference = 'Stop'
$sourceRoot = Split-Path $PSScriptRoot -Parent
$runtime = 'C:\Users\thedo\Desktop\Offline DAoC\runtime'
function Assert-Stopped {
    if (Get-Process CoreServer,game,camelot,OfflineDAoC,connect -ErrorAction SilentlyContinue) {
        throw 'Close the server, client and launcher first.'
    }
}
Assert-Stopped
$launcher = Join-Path (Split-Path $sourceRoot -Parent) 'tools\OfflineDaoc.Launcher\bin\Release\net10.0-windows'
$map = @(
    @('server\GameServer.dll', (Join-Path $sourceRoot 'Release\lib\GameServer.dll')),
    @('server\GameServer.pdb', (Join-Path $sourceRoot 'Release\lib\GameServer.pdb')),
    @('server\lib\GameServer.dll', (Join-Path $sourceRoot 'Release\lib\GameServer.dll')),
    @('server\lib\GameServer.pdb', (Join-Path $sourceRoot 'Release\lib\GameServer.pdb'))
)
foreach ($dependency in @('CoreBase.dll','CoreDatabase.dll')) {
    if ((Get-FileHash -LiteralPath (Join-Path $runtime "server\lib\$dependency")).Hash -ne
        (Get-FileHash -LiteralPath (Join-Path $sourceRoot "Release\lib\$dependency")).Hash) { throw "Dependency changed: $dependency" }
}
if ((Get-FileHash -LiteralPath (Join-Path $runtime 'OfflineDAoC.deps.json')).Hash -ne
    (Get-FileHash -LiteralPath (Join-Path $launcher 'OfflineDAoC.deps.json')).Hash) { throw 'Launcher dependencies changed.' }
if ((Get-FileHash -LiteralPath (Join-Path $sourceRoot 'Release\lib\GameServer.dll')).Hash -ne
    (Get-FileHash -LiteralPath (Join-Path $sourceRoot 'build\Tests\Release\lib\GameServer.dll')).Hash) { throw 'Server build differs from tested assembly.' }
$entries = foreach ($pair in $map) {
    $target = [IO.Path]::GetFullPath((Join-Path $runtime $pair[0]))
    if (!$target.StartsWith($runtime + '\', [StringComparison]::OrdinalIgnoreCase)) { throw 'Target escaped runtime.' }
    if (Test-Path -LiteralPath ($target + '.frontier-threat-new')) { throw "Unexpected staging file: $target" }
    [pscustomobject]@{Relative=$pair[0]; Target=$target; Source=$pair[1]; OldHash=(Get-FileHash -LiteralPath $target).Hash; NewHash=(Get-FileHash -LiteralPath $pair[1]).Hash}
}
if (!$Apply) { $entries | Select-Object Relative,OldHash,NewHash; return }
$backup = Join-Path (Split-Path $runtime -Parent) ('deployment-backups\frontier-threat-' + (Get-Date -Format 'yyyyMMdd-HHmmss'))
New-Item -ItemType Directory -Path $backup | Out-Null
$preserved = foreach ($relative in @('account.txt','data\opendaoc.sqlite3.db','data\opendaoc.sqlite3.db-wal','server\bot-goals.json','OfflineDAoC.dll','OfflineDAoC.pdb')) {
    $path = Join-Path $runtime $relative
    if (Test-Path -LiteralPath $path) { [pscustomobject]@{Path=$path; Hash=(Get-FileHash -LiteralPath $path).Hash} }
}
foreach ($entry in $entries) {
    $copy = Join-Path $backup $entry.Relative
    New-Item -ItemType Directory -Path (Split-Path $copy -Parent) -Force | Out-Null
    Copy-Item -LiteralPath $entry.Target -Destination $copy
    if ((Get-FileHash -LiteralPath $copy).Hash -ne $entry.OldHash) { throw 'Backup verification failed.' }
}
@{Entries=$entries; Preserved=$preserved; Created=(Get-Date -Format o)} | ConvertTo-Json -Depth 5 |
    Set-Content -LiteralPath (Join-Path $backup 'manifest.json') -Encoding utf8
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'rollback_frontier_threat.ps1') -Destination (Join-Path $backup 'ROLLBACK FRONTIER THREAT.ps1')
try {
    foreach ($entry in $entries) {
        Assert-Stopped
        if ((Get-FileHash -LiteralPath $entry.Target).Hash -ne $entry.OldHash) { throw 'Runtime changed while installing.' }
        $temporary = $entry.Target + '.frontier-threat-new'
        Copy-Item -LiteralPath $entry.Source -Destination $temporary
        if ((Get-FileHash -LiteralPath $temporary).Hash -ne $entry.NewHash) { throw 'Staged file verification failed.' }
        [IO.File]::Move($temporary, $entry.Target, $true)
        if ((Get-FileHash -LiteralPath $entry.Target).Hash -ne $entry.NewHash) { throw 'Installed verification failed.' }
    }
    foreach ($item in $preserved) {
        if ((Get-FileHash -LiteralPath $item.Path).Hash -ne $item.Hash) { throw 'Preserved data changed unexpectedly.' }
    }
} catch {
    Assert-Stopped
    foreach ($entry in $entries) {
        Copy-Item -LiteralPath (Join-Path $backup $entry.Relative) -Destination ($entry.Target + '.frontier-threat-new') -Force
        [IO.File]::Move($entry.Target + '.frontier-threat-new', $entry.Target, $true)
    }
    throw
}
Write-Output "Installed and verified $($entries.Count) files. Accounts, database and settings unchanged. Backup: $backup"
