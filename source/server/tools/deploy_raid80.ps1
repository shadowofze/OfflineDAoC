$ErrorActionPreference = 'Stop'
$sourceRoot = Split-Path $PSScriptRoot -Parent
$runtime = 'C:\Users\thedo\Desktop\Offline DAoC\runtime'
$client = Join-Path $runtime 'client-opendaoc\app'
$server = Join-Path $runtime 'server'
$stage = Join-Path $sourceRoot 'build\native-raid80'
function Assert-Stopped {
    $running = Get-CimInstance Win32_Process | Where-Object {
        $_.Name -in @('CoreServer.exe','game.dll','game.exe','camelot.exe','OfflineDaoc.Launcher.exe') -or
        ($_.ExecutablePath -and $_.ExecutablePath.StartsWith($runtime, [StringComparison]::OrdinalIgnoreCase))
    }
    if ($running) { throw 'Close server, client and launcher before deployment.' }
}
Assert-Stopped
if ((Get-FileHash -LiteralPath (Join-Path $client 'game.dll')).Hash -ne '852770F16534BA37E5F41892BE9FE53F2B15714B91791EF1644CC5BF996A4D16') {
    throw 'Installed client differs from the verified 40-person client; do not overwrite.'
}
$manifest = Get-Content -LiteralPath (Join-Path $stage 'manifest.json') -Raw | ConvertFrom-Json
if ((Get-FileHash -LiteralPath (Join-Path $stage 'game.dll')).Hash -ne $manifest.sha256) { throw 'Staged client hash mismatch.' }
$entries = @(
    @{Source=(Join-Path $stage 'game.dll'); Target=(Join-Path $client 'game.dll')},
    @{Source=(Join-Path $stage 'uimain.xml'); Target=(Join-Path $client 'ui\uimain.xml')}
)
foreach ($skin in @('atlantis','isles')) {
    $entries += @{Source=(Join-Path $stage 'custom10_window.xml'); Target=(Join-Path $client "ui\$skin\custom10_window.xml")}
}
foreach ($location in @('', 'lib')) {
    foreach ($name in @('GameServer.dll','GameServer.pdb')) {
        $entries += @{Source=(Join-Path $sourceRoot "Release\lib\$name"); Target=(Join-Path (Join-Path $server $location) $name)}
    }
}
$backup = Join-Path $runtime ('rollback-raid80-' + (Get-Date -Format 'yyyyMMdd-HHmmss'))
New-Item -ItemType Directory -Path $backup | Out-Null
for ($i=0; $i -lt $entries.Count; $i++) {
    $entry=$entries[$i]
    $entry.NewHash=(Get-FileHash -LiteralPath $entry.Source).Hash
    $entry.Existed=Test-Path -LiteralPath $entry.Target
    $entry.Backup=Join-Path $backup ($i.ToString('D2')+'-'+(Split-Path $entry.Target -Leaf))
    if ($entry.Existed) {
        $entry.OldHash=(Get-FileHash -LiteralPath $entry.Target).Hash
        Copy-Item -LiteralPath $entry.Target -Destination $entry.Backup
        if ((Get-FileHash -LiteralPath $entry.Backup).Hash -ne $entry.OldHash) { throw 'Backup hash mismatch.' }
    }
}
Assert-Stopped
try {
    foreach ($entry in $entries) {
        if ($entry.Existed -and (Get-FileHash -LiteralPath $entry.Target).Hash -ne $entry.OldHash) { throw 'Target changed during deployment.' }
        Copy-Item -LiteralPath $entry.Source -Destination $entry.Target -Force
        if ((Get-FileHash -LiteralPath $entry.Target).Hash -ne $entry.NewHash) { throw 'Installed hash mismatch.' }
    }
} catch {
    foreach ($entry in $entries) {
        if ($entry.Existed) { Copy-Item -LiteralPath $entry.Backup -Destination $entry.Target -Force }
        elseif ((Test-Path -LiteralPath $entry.Target) -and (Get-FileHash -LiteralPath $entry.Target).Hash -eq $entry.NewHash) {
            Remove-Item -LiteralPath $entry.Target
        }
    }
    throw
}
Write-Output "Installed $($entries.Count) verified files. Original 40-person XML left untouched. Rollback copies: $backup"
