param([switch]$Apply)
$ErrorActionPreference = 'Stop'
$sourceRoot = Split-Path $PSScriptRoot -Parent
$runtime = 'C:\Users\thedo\Desktop\new class test\runtime'
function Assert-Stopped {
    if (Get-Process CoreServer,game,camelot,OfflineDAoC,connect -ErrorAction SilentlyContinue) {
        throw 'Close the server, client and launcher first.'
    }
}
Assert-Stopped
[xml]$tests = Get-Content -LiteralPath 'C:\Users\thedo\Desktop\new class test\reports\realm-repairs-tests\realm-repairs-server.trx' -Raw
if ([int]$tests.TestRun.ResultSummary.Counters.failed -ne 0 -or [int]$tests.TestRun.ResultSummary.Counters.passed -lt 1676) { throw 'Repair tests have not passed.' }
foreach ($dependency in @('CoreBase.dll','CoreDatabase.dll')) {
    if ((Get-FileHash -LiteralPath (Join-Path $runtime "server\lib\$dependency")).Hash -ne
        (Get-FileHash -LiteralPath (Join-Path $sourceRoot "Release\lib\$dependency")).Hash) { throw "Dependency changed: $dependency" }
}
if ((Get-FileHash -LiteralPath (Join-Path $sourceRoot 'Release\lib\GameServer.dll')).Hash -ne
    (Get-FileHash -LiteralPath (Join-Path $sourceRoot 'build\Tests\Release\lib\GameServer.dll')).Hash) { throw 'Build differs from tested assembly.' }
$entries = foreach ($relative in @('server\GameServer.dll','server\GameServer.pdb','server\lib\GameServer.dll','server\lib\GameServer.pdb')) {
    $target = [IO.Path]::GetFullPath((Join-Path $runtime $relative))
    if (!$target.StartsWith($runtime + '\', [StringComparison]::OrdinalIgnoreCase)) { throw 'Invalid target.' }
    $source = Join-Path $sourceRoot ('Release\lib\' + (Split-Path $relative -Leaf))
    [pscustomobject]@{Relative=$relative; Target=$target; Source=$source; OldHash=(Get-FileHash -LiteralPath $target).Hash; NewHash=(Get-FileHash -LiteralPath $source).Hash}
}
if (!$Apply) { $entries | Select-Object Relative,OldHash,NewHash; return }
$backup = Join-Path (Split-Path $runtime -Parent) ('deployment-backups\realm-repairs-' + (Get-Date -Format 'yyyyMMdd-HHmmss'))
New-Item -ItemType Directory -Path $backup | Out-Null
$preserved = foreach ($relative in @('account.txt','data\opendaoc.sqlite3.db','data\opendaoc.sqlite3.db-wal','server\bot-goals.json','OfflineDAoC.dll')) {
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
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'rollback_realm_event_records.ps1') -Destination (Join-Path $backup 'ROLLBACK REALM REPAIRS.ps1')
try {
    foreach ($entry in $entries) {
        Assert-Stopped
        if ((Get-FileHash -LiteralPath $entry.Target).Hash -ne $entry.OldHash) { throw 'Runtime changed during deployment.' }
        $temporary = $entry.Target + '.realm-repairs-new'
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
    foreach ($entry in $entries) {
        Copy-Item -LiteralPath (Join-Path $backup $entry.Relative) -Destination $entry.Target -Force
    }
    throw
}
Write-Output "Installed and verified four files. Accounts, database, launcher and settings unchanged. Backup: $backup"
