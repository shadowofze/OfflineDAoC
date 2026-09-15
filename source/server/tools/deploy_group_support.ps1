$ErrorActionPreference = 'Stop'
$sourceRoot = Split-Path $PSScriptRoot -Parent
$runtime = 'C:\Users\thedo\Desktop\Offline DAoC\runtime\server'
function Assert-Stopped {
    if (Get-Process CoreServer,game,camelot,OfflineDaoc.Launcher -ErrorAction SilentlyContinue) {
        throw 'Close server, client and launcher before deploying.'
    }
}
Assert-Stopped
$backup = Join-Path $runtime ('rollback-group-support-' + (Get-Date -Format 'yyyyMMdd-HHmmss'))
New-Item -ItemType Directory -Path $backup | Out-Null
$entries = @()
foreach ($location in @('', 'lib')) {
    foreach ($name in @('GameServer.dll','GameServer.pdb')) {
        $entry = @{Target=(Join-Path (Join-Path $runtime $location) $name); Backup=(Join-Path $backup (($location+'-'+$name).TrimStart('-'))); Source=(Join-Path $sourceRoot "Release/lib/$name")}
        $entry.OldHash = (Get-FileHash -LiteralPath $entry.Target).Hash
        $entry.NewHash = (Get-FileHash -LiteralPath $entry.Source).Hash
        Copy-Item -LiteralPath $entry.Target -Destination $entry.Backup
        if ((Get-FileHash -LiteralPath $entry.Backup).Hash -ne $entry.OldHash) { throw 'Backup verification failed.' }
        $entries += $entry
    }
}
Assert-Stopped
try {
    foreach ($entry in $entries) {
        if ((Get-FileHash -LiteralPath $entry.Target).Hash -ne $entry.OldHash) { throw 'Installed file changed during deployment.' }
        Copy-Item -LiteralPath $entry.Source -Destination $entry.Target -Force
        if ((Get-FileHash -LiteralPath $entry.Target).Hash -ne $entry.NewHash) { throw 'Deployment hash mismatch.' }
    }
} catch {
    foreach ($entry in $entries) { Copy-Item -LiteralPath $entry.Backup -Destination $entry.Target -Force }
    throw
}
Write-Output "Installed verified group-support DLL/PDB. Rollback: $backup"
