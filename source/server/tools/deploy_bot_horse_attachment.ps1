param([switch]$Apply)
$ErrorActionPreference = 'Stop'
$sourceRoot = Split-Path $PSScriptRoot -Parent
$runtime = 'C:\Users\thedo\Desktop\Offline DAoC\runtime'
$backup = 'C:\Users\thedo\Desktop\Offline DAoC\deployment-backups\bot-horse-attachment-20260913'
function Assert-Stopped {
    if (Get-Process CoreServer,game,camelot,OfflineDAoC,connect -ErrorAction SilentlyContinue) {
        throw 'Close server, game and launcher before deployment.'
    }
}
Assert-Stopped
foreach ($dependency in @('CoreBase.dll','CoreDatabase.dll')) {
    if ((Get-FileHash -LiteralPath (Join-Path $runtime "server\lib\$dependency")).Hash -ne
        (Get-FileHash -LiteralPath (Join-Path $sourceRoot "Release\lib\$dependency")).Hash) {
        throw "Unexpected dependency difference: $dependency"
    }
}
$tested = Join-Path $sourceRoot 'build\Tests\Release\lib\GameServer.dll'
if ((Get-FileHash $tested).Hash -ne (Get-FileHash (Join-Path $sourceRoot 'Release\lib\GameServer.dll')).Hash) {
    throw 'Deployment assembly differs from tested assembly.'
}
[xml]$results = Get-Content -LiteralPath (Join-Path $backup 'tests\horse-full.trx') -Raw
if ($results.TestRun.ResultSummary.outcome -ne 'Completed' -or [int]$results.TestRun.ResultSummary.Counters.failed -ne 0) {
    throw 'Full regression suite did not pass.'
}
$entries = foreach ($relative in @('server\GameServer.dll','server\GameServer.pdb','server\lib\GameServer.dll','server\lib\GameServer.pdb')) {
    $target = [IO.Path]::GetFullPath((Join-Path $runtime $relative))
    if (!$target.StartsWith($runtime + '\', [StringComparison]::OrdinalIgnoreCase)) { throw 'Invalid target.' }
    $source = Join-Path $sourceRoot ('Release\lib\' + (Split-Path $relative -Leaf))
    if (Test-Path -LiteralPath ($target + '.horse-attachment-new')) { throw 'Unexpected staging file exists.' }
    [pscustomobject]@{Relative=$relative; Target=$target; Source=$source; OldHash=(Get-FileHash $target).Hash; NewHash=(Get-FileHash $source).Hash}
}
if (!$Apply) { $entries | Format-List; return }
if (Test-Path -LiteralPath (Join-Path $backup 'manifest.json')) { throw 'Deployment already recorded. Do not overwrite the backup.' }
$preserved = foreach ($relative in @('account.txt','data\opendaoc.sqlite3.db','data\opendaoc.sqlite3.db-wal','server\bot-goals.json','OfflineDAoC.dll','client-opendaoc\app\game.dll')) {
    $path = Join-Path $runtime $relative
    if (Test-Path -LiteralPath $path) { [pscustomobject]@{Path=$path;Hash=(Get-FileHash $path).Hash} }
}
foreach ($entry in $entries) {
    $copy = Join-Path $backup $entry.Relative
    New-Item -ItemType Directory -Path (Split-Path $copy) -Force | Out-Null
    Copy-Item -LiteralPath $entry.Target -Destination $copy
    if ((Get-FileHash $copy).Hash -ne $entry.OldHash) { throw 'Backup verification failed.' }
}
@{Entries=$entries;Preserved=$preserved;Created=(Get-Date -Format o)} | ConvertTo-Json -Depth 5 |
    Set-Content -LiteralPath (Join-Path $backup 'manifest.json') -Encoding utf8
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'rollback_bot_horse_attachment.ps1') -Destination (Join-Path $backup 'ROLLBACK BOT HORSE ATTACHMENT.ps1')
try {
    foreach ($entry in $entries) {
        Assert-Stopped
        if ((Get-FileHash $entry.Target).Hash -ne $entry.OldHash) { throw 'Runtime changed during deployment.' }
        $temporary = $entry.Target + '.horse-attachment-new'
        Copy-Item -LiteralPath $entry.Source -Destination $temporary
        if ((Get-FileHash $temporary).Hash -ne $entry.NewHash) { throw 'Staging verification failed.' }
        [IO.File]::Move($temporary, $entry.Target, $true)
        if ((Get-FileHash $entry.Target).Hash -ne $entry.NewHash) { throw 'Installed hash mismatch.' }
    }
    foreach ($item in $preserved) {
        if ((Get-FileHash $item.Path).Hash -ne $item.Hash) { throw "Unexpected data change: $($item.Path)" }
    }
} catch {
    Assert-Stopped
    foreach ($entry in $entries) {
        Copy-Item -LiteralPath (Join-Path $backup $entry.Relative) -Destination ($entry.Target + '.horse-attachment-new') -Force
        [IO.File]::Move($entry.Target + '.horse-attachment-new', $entry.Target, $true)
    }
    throw
}
Write-Output 'Installed 4 server assembly/symbol copies. Accounts, database, settings, launcher and client unchanged.'
Write-Output "Verified rollback backup: $backup"
