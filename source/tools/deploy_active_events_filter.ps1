param([switch]$Apply)
$ErrorActionPreference = 'Stop'
$runtime = 'C:\Users\thedo\Desktop\Offline DAoC\runtime'
$built = Join-Path $PSScriptRoot 'OfflineDaoc.Launcher\bin\Release\net10.0-windows'
function Assert-Closed {
    if (Get-Process OfflineDAoC -ErrorAction SilentlyContinue) { throw 'Close the launcher before replacing it.' }
}
Assert-Closed
[xml]$tests = Get-Content -LiteralPath 'C:\Users\thedo\Desktop\Offline DAoC\reports\active-events-filter-tests\active-events-filter.trx' -Raw
if ($tests.TestRun.ResultSummary.outcome -ne 'Completed' -or [int]$tests.TestRun.ResultSummary.Counters.failed -ne 0 -or
    [int]$tests.TestRun.ResultSummary.Counters.passed -lt 85) { throw 'Launcher tests did not pass.' }
foreach ($dependency in @('OfflineDAoC.deps.json','OfflineDAoC.runtimeconfig.json','OfflineDAoC.exe')) {
    if ((Get-FileHash -LiteralPath (Join-Path $runtime $dependency)).Hash -ne
        (Get-FileHash -LiteralPath (Join-Path $built $dependency)).Hash) { throw "Launcher dependency changed: $dependency" }
}
if ((Get-FileHash -LiteralPath (Join-Path $built 'OfflineDAoC.dll')).Hash -ne
    (Get-FileHash -LiteralPath (Join-Path $PSScriptRoot 'OfflineDaoc.Launcher.Tests\bin\Release\net10.0-windows\OfflineDAoC.dll')).Hash) { throw 'Launcher differs from tested assembly.' }
$entries = foreach ($name in @('OfflineDAoC.dll','OfflineDAoC.pdb')) {
    $target=Join-Path $runtime $name
    if (Test-Path -LiteralPath ($target+'.active-events-new')) { throw 'Unexpected staged file.' }
    [pscustomobject]@{Name=$name;Target=$target;Source=(Join-Path $built $name);OldHash=(Get-FileHash -LiteralPath $target).Hash;NewHash=(Get-FileHash -LiteralPath (Join-Path $built $name)).Hash}
}
if (!$Apply) { $entries; return }
$backup=Join-Path (Split-Path $runtime -Parent) ('deployment-backups\active-events-filter-'+(Get-Date -Format 'yyyyMMdd-HHmmss'))
New-Item -ItemType Directory -Path $backup | Out-Null
$preserved=foreach ($relative in @('account.txt','data\opendaoc.sqlite3.db','server\bot-goals.json','server\GameServer.dll','server\lib\GameServer.dll')) {
    $path=Join-Path $runtime $relative
    if (Test-Path -LiteralPath $path) { [pscustomobject]@{Path=$path;Hash=(Get-FileHash -LiteralPath $path).Hash} }
}
foreach ($entry in $entries) {
    Copy-Item -LiteralPath $entry.Target -Destination (Join-Path $backup $entry.Name)
    if ((Get-FileHash -LiteralPath (Join-Path $backup $entry.Name)).Hash -ne $entry.OldHash) { throw 'Backup hash mismatch.' }
}
@{Entries=$entries;Preserved=$preserved} | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $backup 'manifest.json') -Encoding utf8
try {
    foreach ($entry in $entries) {
        Assert-Closed
        if ((Get-FileHash -LiteralPath $entry.Target).Hash -ne $entry.OldHash) { throw 'Launcher changed during deployment.' }
        Copy-Item -LiteralPath $entry.Source -Destination ($entry.Target+'.active-events-new')
        if ((Get-FileHash -LiteralPath ($entry.Target+'.active-events-new')).Hash -ne $entry.NewHash) { throw 'Staged hash mismatch.' }
        [IO.File]::Move($entry.Target+'.active-events-new',$entry.Target,$true)
        if ((Get-FileHash -LiteralPath $entry.Target).Hash -ne $entry.NewHash) { throw 'Installed hash mismatch.' }
    }
    foreach ($item in $preserved) {
        if ((Get-FileHash -LiteralPath $item.Path).Hash -ne $item.Hash) { throw 'Preserved file changed.' }
    }
} catch {
    Assert-Closed
    foreach ($entry in $entries) { Copy-Item -LiteralPath (Join-Path $backup $entry.Name) -Destination $entry.Target -Force }
    throw
}
Write-Output "Installed and verified two launcher files. Server, account, progress and settings unchanged. Backup: $backup"
