$ErrorActionPreference = 'Stop'
$runtime = 'C:\Users\thedo\Desktop\Offline DAoC\runtime'
$sourceRoot = Split-Path $PSScriptRoot -Parent
$build = Join-Path (Split-Path $sourceRoot -Parent) 'tools\OfflineDaoc.Launcher\bin\Release\net10.0-windows'
function Assert-Stopped {
    if (Get-Process CoreServer,OfflineDAoC,game,camelot,connect -ErrorAction SilentlyContinue) { throw 'Close server, launcher and client first.' }
}
Assert-Stopped
if ((Get-FileHash -LiteralPath (Join-Path $build 'OfflineDAoC.deps.json')).Hash -ne
    (Get-FileHash -LiteralPath (Join-Path $runtime 'OfflineDAoC.deps.json')).Hash) { throw 'Dependencies differ.' }
$entries = foreach ($name in @('OfflineDAoC.dll','OfflineDAoC.pdb')) {
    $target = [IO.Path]::GetFullPath((Join-Path $runtime $name))
    if (!$target.StartsWith($runtime + '\', [StringComparison]::OrdinalIgnoreCase)) { throw 'Invalid target.' }
    [pscustomobject]@{Relative=$name; Target=$target; Source=(Join-Path $build $name); OldHash=(Get-FileHash -LiteralPath $target).Hash; NewHash=(Get-FileHash -LiteralPath (Join-Path $build $name)).Hash}
}
$preserved = foreach ($name in @('server\GameServer.dll','server\lib\GameServer.dll','account.txt','data\opendaoc.sqlite3.db','data\opendaoc.sqlite3.db-wal','server\rvr-world.json','server\bot-goals.json')) {
    $path = Join-Path $runtime $name
    if (Test-Path -LiteralPath $path) { [pscustomobject]@{Path=$path;Hash=(Get-FileHash -LiteralPath $path).Hash} }
}
$backup = Join-Path (Split-Path $runtime -Parent) ('deployment-backups\realm-events-launcher-' + (Get-Date -Format 'yyyyMMdd-HHmmss'))
New-Item -ItemType Directory -Path $backup | Out-Null
foreach ($entry in $entries) {
    Copy-Item -LiteralPath $entry.Target -Destination (Join-Path $backup $entry.Relative)
    if ((Get-FileHash -LiteralPath (Join-Path $backup $entry.Relative)).Hash -ne $entry.OldHash) { throw 'Backup mismatch.' }
}
@{Entries=$entries;Preserved=$preserved;Created=(Get-Date -Format o)} | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $backup 'manifest.json') -Encoding utf8
try {
    foreach ($entry in $entries) {
        Assert-Stopped
        if ((Get-FileHash -LiteralPath $entry.Target).Hash -ne $entry.OldHash) { throw 'Installed files changed.' }
        $staged = $entry.Target + '.events-layout-new'
        if (Test-Path -LiteralPath $staged) { throw 'Unexpected staging file.' }
        Copy-Item -LiteralPath $entry.Source -Destination $staged
        if ((Get-FileHash -LiteralPath $staged).Hash -ne $entry.NewHash) { throw 'Staged hash mismatch.' }
        [IO.File]::Move($staged, $entry.Target, $true)
        if ((Get-FileHash -LiteralPath $entry.Target).Hash -ne $entry.NewHash) { throw 'Installed hash mismatch.' }
    }
    foreach ($entry in $preserved) { if ((Get-FileHash -LiteralPath $entry.Path).Hash -ne $entry.Hash) { throw 'Protected data changed.' } }
} catch {
    Assert-Stopped
    foreach ($entry in $entries) {
        Copy-Item -LiteralPath (Join-Path $backup $entry.Relative) -Destination ($entry.Target + '.events-layout-restore')
        [IO.File]::Move($entry.Target + '.events-layout-restore', $entry.Target, $true)
    }
    throw
}
Write-Output "Launcher DLL/PDB installed and verified. Server binaries, accounts, database, snapshot and settings preserved. Backup: $backup"
