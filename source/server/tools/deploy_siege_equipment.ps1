param([switch]$Apply)
$ErrorActionPreference = 'Stop'
$sourceRoot = Split-Path $PSScriptRoot -Parent
$runtime = 'C:\Users\thedo\Desktop\new class test\runtime'
function Assert-Stopped {
    if (Get-Process CoreServer,game,camelot,OfflineDAoC,connect -ErrorAction SilentlyContinue) {
        throw 'Close server, client and launcher first.'
    }
}
Assert-Stopped
[xml]$tests = Get-Content -LiteralPath (Join-Path $sourceRoot 'build\siege-test-results\siege-equipment-final.trx') -Raw
if ([int]$tests.TestRun.ResultSummary.Counters.failed -ne 0 -or [int]$tests.TestRun.ResultSummary.Counters.passed -lt 1810) { throw 'Final regression tests have not passed.' }
foreach ($dependency in @('CoreBase.dll','CoreDatabase.dll')) {
    if ((Get-FileHash -LiteralPath (Join-Path $runtime "server\lib\$dependency")).Hash -ne
        (Get-FileHash -LiteralPath (Join-Path $sourceRoot "Release\lib\$dependency")).Hash) { throw "Unexpected dependency change: $dependency" }
}
if ((Get-FileHash -LiteralPath (Join-Path $sourceRoot 'Release\lib\GameServer.dll')).Hash -ne
    (Get-FileHash -LiteralPath (Join-Path $sourceRoot 'build\Tests\Release\lib\GameServer.dll')).Hash) { throw 'Build differs from tested assembly.' }
$entries = foreach ($relative in @('server\GameServer.dll','server\GameServer.pdb','server\lib\GameServer.dll','server\lib\GameServer.pdb')) {
    $target = [IO.Path]::GetFullPath((Join-Path $runtime $relative))
    if (!$target.StartsWith($runtime + '\', [StringComparison]::OrdinalIgnoreCase)) { throw 'Invalid deployment target.' }
    $source = Join-Path $sourceRoot ('Release\lib\' + (Split-Path $relative -Leaf))
    [pscustomobject]@{Relative=$relative; Target=$target; Source=$source; OldHash=(Get-FileHash -LiteralPath $target).Hash; NewHash=(Get-FileHash -LiteralPath $source).Hash}
}
foreach ($entry in $entries | Where-Object { $_.Relative.EndsWith('.dll') }) {
    if ($entry.OldHash -ne 'BFE182012C6EE89811E58453F8BA03103D7E61F25236550F37FC6509D98A0A28') { throw 'Installed server differs from the inspected baseline.' }
}
if (!$Apply) { $entries | Format-Table Relative,OldHash,NewHash; return }
$backup = Join-Path (Split-Path $runtime -Parent) ('deployment-backups\siege-equipment-' + (Get-Date -Format 'yyyyMMdd-HHmmss'))
New-Item -ItemType Directory -Path $backup | Out-Null
$preserved = foreach ($relative in @('account.txt','data\opendaoc.sqlite3.db','data\opendaoc.sqlite3.db-wal','server\bot-goals.json','server\rvr-world.json','OfflineDAoC.dll')) {
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
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'rollback_siege_equipment.ps1') -Destination (Join-Path $backup 'ROLLBACK SIEGE EQUIPMENT.ps1')
try {
    foreach ($entry in $entries) {
        Assert-Stopped
        if ((Get-FileHash -LiteralPath $entry.Target).Hash -ne $entry.OldHash) { throw 'Runtime changed during deployment.' }
        $temporary = $entry.Target + '.siege-equipment-new'
        if (Test-Path -LiteralPath $temporary) { throw 'Unexpected staging file.' }
        Copy-Item -LiteralPath $entry.Source -Destination $temporary
        if ((Get-FileHash -LiteralPath $temporary).Hash -ne $entry.NewHash) { throw 'Staging verification failed.' }
        [IO.File]::Move($temporary, $entry.Target, $true)
        if ((Get-FileHash -LiteralPath $entry.Target).Hash -ne $entry.NewHash) { throw 'Installed verification failed.' }
    }
    foreach ($item in $preserved) {
        if ((Get-FileHash -LiteralPath $item.Path).Hash -ne $item.Hash) { throw 'Preserved data changed unexpectedly.' }
    }
} catch {
    Assert-Stopped
    foreach ($entry in $entries) { Copy-Item -LiteralPath (Join-Path $backup $entry.Relative) -Destination $entry.Target -Force }
    throw
}
Write-Output "Installed four verified server files. Accounts, database, launcher and settings unchanged. Backup: $backup"
