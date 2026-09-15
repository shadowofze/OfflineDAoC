$ErrorActionPreference = 'Stop'
$sourceRoot = Split-Path $PSScriptRoot -Parent
$runtime = 'C:\Users\thedo\Desktop\Offline DAoC\runtime\server'
function Assert-Stopped {
    if (Get-Process CoreServer,game,camelot,OfflineDaoc.Launcher -ErrorAction SilentlyContinue) {
        throw 'Close server, client and launcher before deploying.'
    }
}
Assert-Stopped
if ((Get-FileHash -LiteralPath (Join-Path $runtime 'GameServer.dll')).Hash -ne '645045E8577754B0FE4DFCEEE4016EDDA639B9BD3A36728F71547FF4C6DCF826') {
    throw 'Installed server changed since investigation; refusing to overwrite.'
}
$tests = Get-Content (Join-Path $sourceRoot 'build/dragon-full-tests.log') -Raw
$probe = Get-Content (Join-Path $sourceRoot 'build/dragon-geometry/probe.log') -Raw
if ($tests -notmatch 'Failed:\s+0, Passed:\s+1440' -or $probe -notmatch 'Test Run Successful') {
    throw 'Regression tests and staged mesh checks must pass first.'
}
$expected = @{
    'zone004.nav'='7e86c90b1c976915ece36e9734f64448999161ebe996eb575579ccbd7e00b466'
    'zone116.nav'='fde10fb11fdb70036670cb6905b630e18632b23ebfe0fed3b8b3310f822fb71a'
    'zone216.nav'='0cbcd27073a0ffc65486e9042b1ab3b198109d5652d99f8e8388c8b70b54e109'
}
foreach ($name in $expected.Keys) {
    if ((Get-FileHash -LiteralPath (Join-Path $runtime "navmesh/$name")).Hash -ne $expected[$name]) {
        throw "Installed mesh $name changed; refusing to overwrite."
    }
}
$backup = Join-Path $runtime ('rollback-dragon-combat-' + (Get-Date -Format 'yyyyMMdd-HHmmss'))
New-Item -ItemType Directory -Path $backup | Out-Null
$files = @()
foreach ($location in @('', 'lib')) {
    foreach ($name in @('GameServer.dll','GameServer.pdb')) {
        $files += @{Target=(Join-Path (Join-Path $runtime $location) $name); Backup=(Join-Path $backup (($location+'-'+$name).TrimStart('-'))); Source=(Join-Path $sourceRoot "Release/lib/$name")}
    }
}
foreach ($name in $expected.Keys) {
    $files += @{Target=(Join-Path $runtime "navmesh/$name"); Backup=(Join-Path $backup $name); Source=(Join-Path $sourceRoot "build/dragon-geometry/staged/navmesh/$name")}
}
foreach ($entry in $files) {
    $entry.OldHash = (Get-FileHash -LiteralPath $entry.Target).Hash
    $entry.NewHash = (Get-FileHash -LiteralPath $entry.Source).Hash
    Copy-Item -LiteralPath $entry.Target -Destination $entry.Backup
    if ((Get-FileHash -LiteralPath $entry.Backup).Hash -ne $entry.OldHash) { throw 'Backup verification failed.' }
}
$files | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $backup 'manifest.json')
Assert-Stopped
try {
    foreach ($entry in $files) {
        Copy-Item -LiteralPath $entry.Source -Destination $entry.Target -Force
        if ((Get-FileHash -LiteralPath $entry.Target).Hash -ne $entry.NewHash) { throw 'Deployment hash mismatch.' }
    }
} catch {
    foreach ($entry in $files) { Copy-Item -LiteralPath $entry.Backup -Destination $entry.Target -Force }
    throw
}
Write-Output "Installed and hash-verified 4 DLL/PDB copies and 3 local lair mesh repairs. Rollback: $backup"
Write-Output 'Client, accounts, characters, settings, loot and dragon damage/health were not modified.'
