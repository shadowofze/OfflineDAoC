$ErrorActionPreference = 'Stop'
$sourceRoot = Split-Path $PSScriptRoot -Parent
$runtime = 'C:\Users\thedo\Desktop\new class test\runtime\server'
function Assert-Stopped {
    if (Get-Process CoreServer,game,camelot,OfflineDaoc.Launcher -ErrorAction SilentlyContinue) {
        throw 'Close server, client and launcher before deploying.'
    }
}
Assert-Stopped
if ((Get-FileHash -LiteralPath (Join-Path $runtime 'GameServer.dll')).Hash -ne 'A19C1A2FD494C09FFA7744B17C9619DF012D8EDF83FACAA942FD6AF7CAE992AB') {
    throw 'Installed server changed since investigation. Refusing to overwrite.'
}
$results = Get-Content -LiteralPath (Join-Path $sourceRoot 'build\dragon-landing-tests.log') -Raw
if ($results -notmatch 'Failed:\s+0, Passed:\s+1434') { throw 'Full regression tests have not passed.' }
$backup = Join-Path $runtime ('rollback-dragon-landing-' + (Get-Date -Format 'yyyyMMdd-HHmmss'))
New-Item -ItemType Directory -Path $backup | Out-Null
$files = @()
foreach ($location in @('', 'lib')) {
    foreach ($name in @('GameServer.dll','GameServer.pdb')) {
        $target = Join-Path (Join-Path $runtime $location) $name
        $copy = Join-Path $backup (($location + '-' + $name).TrimStart('-'))
        Copy-Item -LiteralPath $target -Destination $copy
        $files += @{Target=$target; Backup=$copy; Source=(Join-Path $sourceRoot "Release\lib\$name"); OldHash=(Get-FileHash -LiteralPath $target).Hash}
    }
}
$files | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $backup 'manifest.json')
Assert-Stopped
try {
    foreach ($entry in $files) {
        Copy-Item -LiteralPath $entry.Source -Destination $entry.Target -Force
        if ((Get-FileHash -LiteralPath $entry.Source).Hash -ne (Get-FileHash -LiteralPath $entry.Target).Hash) { throw 'Deployment hash mismatch.' }
    }
} catch {
    foreach ($entry in $files) { Copy-Item -LiteralPath $entry.Backup -Destination $entry.Target -Force }
    throw
}
Write-Output "Dragon fixes installed and hash-verified. Rollback: $backup"
Write-Output 'Client, accounts, characters and settings were not modified. Dragon placement is corrected by their startup/respawn scripts.'
