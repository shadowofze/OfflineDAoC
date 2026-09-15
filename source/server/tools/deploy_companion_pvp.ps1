param([switch]$Apply)
$ErrorActionPreference = 'Stop'
$sourceRoot = Split-Path $PSScriptRoot -Parent
$runtime = 'C:\Users\thedo\Desktop\Offline DAoC\runtime'
function Assert-Stopped {
    if (Get-Process CoreServer,OfflineDAoC,game,'game.dll',camelot,connect -ErrorAction SilentlyContinue) {
        throw 'Close server, client and launcher before installation.'
    }
}
Assert-Stopped
[xml]$tests = Get-Content -LiteralPath (Join-Path $sourceRoot 'build\companion-pvp-tests\companion-pvp-all.trx') -Raw
if ([int]$tests.TestRun.ResultSummary.Counters.failed -ne 0 -or [int]$tests.TestRun.ResultSummary.Counters.passed -lt 1868) {
    throw 'Regression test results are incomplete.'
}
$expected = '20E8613E8F0920AE2EE610AE5272DA36EB49759D30585BBF4A7F8F8DB06C81E6'
foreach ($path in @('Release\lib\GameServer.dll','build\Tests\Release\lib\GameServer.dll')) {
    if ((Get-FileHash -LiteralPath (Join-Path $sourceRoot $path)).Hash -ne $expected) { throw 'Tested build changed.' }
}
foreach ($file in @('CoreBase.dll','CoreDatabase.dll')) {
    if ((Get-FileHash -LiteralPath (Join-Path $runtime "server\lib\$file")).Hash -ne
        (Get-FileHash -LiteralPath (Join-Path $sourceRoot "Release\lib\$file")).Hash) { throw "Dependency mismatch: $file" }
}
$entries = foreach ($relative in @('server\GameServer.dll','server\GameServer.pdb','server\lib\GameServer.dll','server\lib\GameServer.pdb')) {
    $target = [IO.Path]::GetFullPath((Join-Path $runtime $relative))
    if (!$target.StartsWith($runtime + '\', [StringComparison]::OrdinalIgnoreCase)) { throw 'Invalid target.' }
    $source = Join-Path $sourceRoot ('Release\lib\' + (Split-Path $relative -Leaf))
    $oldHash = (Get-FileHash -LiteralPath $target).Hash
    if ($relative.EndsWith('.dll') -and $oldHash -ne 'F6894FE32773A94DF12CEE71BD6BD2BF8E5759BC25946BC0CF68BF60846789E8') { throw 'Installed baseline changed.' }
    [pscustomobject]@{Relative=$relative; Target=$target; Source=$source; OldHash=$oldHash; NewHash=(Get-FileHash -LiteralPath $source).Hash}
}
if (!$Apply) { $entries | Format-Table Relative,OldHash,NewHash; return }
$backup = Join-Path (Split-Path $runtime -Parent) ('deployment-backups\companion-pvp-' + (Get-Date -Format 'yyyyMMdd-HHmmss'))
New-Item -ItemType Directory -Path $backup | Out-Null
$preserved = foreach ($relative in @('account.txt','data\opendaoc.sqlite3.db','data\opendaoc.sqlite3.db-wal','server\bot-goals.json','server\rvr-world.json','OfflineDAoC.dll')) {
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
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'rollback_companion_pvp.ps1') -Destination (Join-Path $backup 'ROLLBACK COMPANION PVP.ps1')
try {
    foreach ($entry in $entries) {
        Assert-Stopped
        if ((Get-FileHash -LiteralPath $entry.Target).Hash -ne $entry.OldHash) { throw 'Runtime changed during deployment.' }
        $temporary = $entry.Target + '.companion-pvp-new'
        if (Test-Path -LiteralPath $temporary) { throw 'Unexpected staging file.' }
        Copy-Item -LiteralPath $entry.Source -Destination $temporary
        if ((Get-FileHash -LiteralPath $temporary).Hash -ne $entry.NewHash) { throw 'Staging hash mismatch.' }
        [IO.File]::Move($temporary, $entry.Target, $true)
        if ((Get-FileHash -LiteralPath $entry.Target).Hash -ne $entry.NewHash) { throw 'Installed hash mismatch.' }
    }
    foreach ($item in $preserved) {
        if ((Get-FileHash -LiteralPath $item.Path).Hash -ne $item.Hash) { throw 'Preserved file unexpectedly changed.' }
    }
} catch {
    Assert-Stopped
    foreach ($entry in $entries) { Copy-Item -LiteralPath (Join-Path $backup $entry.Relative) -Destination $entry.Target -Force }
    throw
}
Write-Output "Installed four verified files. Preserved account, database, settings and launcher hashes. Backup: $backup"
