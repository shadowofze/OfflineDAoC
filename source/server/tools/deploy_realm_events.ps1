param([switch]$Apply)
$ErrorActionPreference = 'Stop'
$sourceRoot = Split-Path $PSScriptRoot -Parent
$runtime = 'C:\Users\thedo\Desktop\new class test\runtime'
function Assert-Stopped {
    if (Get-Process CoreServer,game,camelot,OfflineDAoC,connect -ErrorAction SilentlyContinue) {
        throw 'Close the server, client, connect program and launcher first. Nothing will be installed while they run.'
    }
}
Assert-Stopped
$launcher = Join-Path (Split-Path $sourceRoot -Parent) 'tools\OfflineDaoc.Launcher\bin\Release\net10.0-windows'
$map = @(
    @('server\GameServer.dll', (Join-Path $sourceRoot 'Release\lib\GameServer.dll')),
    @('server\GameServer.pdb', (Join-Path $sourceRoot 'Release\lib\GameServer.pdb')),
    @('server\lib\GameServer.dll', (Join-Path $sourceRoot 'Release\lib\GameServer.dll')),
    @('server\lib\GameServer.pdb', (Join-Path $sourceRoot 'Release\lib\GameServer.pdb')),
    @('OfflineDAoC.dll', (Join-Path $launcher 'OfflineDAoC.dll')),
    @('OfflineDAoC.pdb', (Join-Path $launcher 'OfflineDAoC.pdb')),
    @('server\navmesh\zone160.nav', (Join-Path $sourceRoot 'build\glacier-climb-clearance128-20260913\zones\zone160.nav'))
)
foreach ($dependency in @('CoreBase.dll','CoreDatabase.dll')) {
    if ((Get-FileHash -LiteralPath (Join-Path $runtime "server\lib\$dependency")).Hash -ne
        (Get-FileHash -LiteralPath (Join-Path $sourceRoot "Release\lib\$dependency")).Hash) {
        throw "Server dependency changed: $dependency. Review before deployment."
    }
}
if ((Get-FileHash -LiteralPath (Join-Path $runtime 'OfflineDAoC.deps.json')).Hash -ne
    (Get-FileHash -LiteralPath (Join-Path $launcher 'OfflineDAoC.deps.json')).Hash) {
    throw 'Launcher dependencies differ; do not install a partial build.'
}
$entries = foreach ($pair in $map) {
    $target = [IO.Path]::GetFullPath((Join-Path $runtime $pair[0]))
    if (!$target.StartsWith($runtime + '\', [StringComparison]::OrdinalIgnoreCase)) { throw 'Target escaped runtime.' }
    if (Test-Path -LiteralPath ($target + '.realm-events-new')) { throw "Unexpected staging file at $target" }
    [pscustomobject]@{Relative=$pair[0]; Target=$target; Source=$pair[1];
        OldHash=(Get-FileHash -LiteralPath $target).Hash; NewHash=(Get-FileHash -LiteralPath $pair[1]).Hash}
}
$nav = $entries | Where-Object Relative -eq 'server\navmesh\zone160.nav'
if ($nav.OldHash -ne 'CCB756A5B3561B0A778B27C78D841C4A9893955C3F26578F1BA5B9822AA23881' -or
    $nav.NewHash -ne 'BB3D26D76216F0BEE4D3EB5505538748CB6455FCFDE908D71182544D1D0D971C') {
    throw 'Installed or tested Glacier mesh changed; review instead of overwriting.'
}
if (!$Apply) { $entries | Select-Object Relative,OldHash,NewHash; return }
$backup = Join-Path (Split-Path $runtime -Parent) ('deployment-backups\realm-events-' + (Get-Date -Format 'yyyyMMdd-HHmmss'))
New-Item -ItemType Directory -Path $backup | Out-Null
$preserved = foreach ($relative in @('account.txt','data\opendaoc.sqlite3.db','data\opendaoc.sqlite3.db-wal')) {
    $path = Join-Path $runtime $relative
    if (Test-Path -LiteralPath $path) { [pscustomobject]@{Path=$path; Hash=(Get-FileHash -LiteralPath $path).Hash} }
}
foreach ($entry in $entries) {
    $copy = Join-Path $backup $entry.Relative
    New-Item -ItemType Directory -Path (Split-Path $copy -Parent) -Force | Out-Null
    Copy-Item -LiteralPath $entry.Target -Destination $copy
    if ((Get-FileHash -LiteralPath $copy).Hash -ne $entry.OldHash) { throw 'Backup hash mismatch.' }
}
@{Entries=$entries; Preserved=$preserved; Created=(Get-Date -Format o)} | ConvertTo-Json -Depth 5 |
    Set-Content -LiteralPath (Join-Path $backup 'manifest.json') -Encoding utf8
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'rollback_realm_events.ps1') -Destination (Join-Path $backup 'ROLLBACK REALM EVENTS.ps1')
Assert-Stopped
try {
    foreach ($entry in $entries) {
        Assert-Stopped
        if ((Get-FileHash -LiteralPath $entry.Target).Hash -ne $entry.OldHash) { throw 'Runtime changed while installing.' }
        $temporary = $entry.Target + '.realm-events-new'
        Copy-Item -LiteralPath $entry.Source -Destination $temporary
        if ((Get-FileHash -LiteralPath $temporary).Hash -ne $entry.NewHash) { throw 'Staged file hash mismatch.' }
        # Replace only the named file, breaking any old hardlink instead of editing its other aliases.
        [IO.File]::Move($temporary, $entry.Target, $true)
        if ((Get-FileHash -LiteralPath $entry.Target).Hash -ne $entry.NewHash) { throw 'Installed file hash mismatch.' }
    }
    foreach ($item in $preserved) {
        if ((Get-FileHash -LiteralPath $item.Path).Hash -ne $item.Hash) { throw 'Preserved account/database changed unexpectedly.' }
    }
} catch {
    Assert-Stopped
    foreach ($entry in $entries) {
        Copy-Item -LiteralPath (Join-Path $backup $entry.Relative) -Destination ($entry.Target + '.realm-events-new') -Force
        [IO.File]::Move($entry.Target + '.realm-events-new', $entry.Target, $true)
    }
    throw
}
Write-Output "Installed and hash-verified $($entries.Count) files. Database, account and WAL unchanged. Rollback: $backup"
