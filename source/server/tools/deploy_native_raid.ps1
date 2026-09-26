param([switch]$Apply)
$ErrorActionPreference = 'Stop'
$sourceRoot = Split-Path $PSScriptRoot -Parent
$runtimeRoot = 'C:\Users\thedo\Desktop\new class test\runtime'
$clientRoot = Join-Path $runtimeRoot 'client-opendaoc\app'
$serverRoot = Join-Path $runtimeRoot 'server'
$probeBackup = Join-Path $clientRoot 'rollback-native-raid-probe'
$stage = Join-Path $sourceRoot 'build\native-raid'
function Assert-Stopped {
    $running = Get-CimInstance Win32_Process | Where-Object {
        $_.Name -in @('CoreServer.exe','game.dll','game.exe','camelot.exe','OfflineDaoc.Launcher.exe') -or
        ($_.ExecutablePath -and $_.ExecutablePath.StartsWith($runtimeRoot, [StringComparison]::OrdinalIgnoreCase))
    }
    if ($running) { throw 'Close the game, server and launcher before deploying the raid update.' }
}
Assert-Stopped
if ((Get-FileHash -LiteralPath (Join-Path $clientRoot 'game.dll')).Hash -ne '1F91D6874A1512383DBC1BB9ED7EA62955A372BD8AF021F3708E81A93AC47856') {
    throw 'Client changed since the layout probe; refusing to overwrite it.'
}
if ((Get-FileHash -LiteralPath (Join-Path $serverRoot 'GameServer.dll')).Hash -ne '76C513B30D2C2B11C44AD90BFEB5D103D2416A854A1054ACA2FD2FA7C3900AD5') {
    throw 'Server changed since the raid work began; rebuild/review before deployment.'
}
$manifest = Get-Content -LiteralPath (Join-Path $stage 'manifest.json') -Raw | ConvertFrom-Json
if ((Get-FileHash -LiteralPath (Join-Path $stage 'game.dll')).Hash -ne $manifest.sha256) { throw 'Staged client hash mismatch.' }
$files = @(
    @{ Source = (Join-Path $stage 'game.dll'); Target = (Join-Path $clientRoot 'game.dll') }
)
foreach ($skin in @('atlantis','isles')) {
    $files += @{ Source = (Join-Path $stage 'custom9_window.xml'); Target = (Join-Path $clientRoot "ui\$skin\custom9_window.xml") }
    $commands = Join-Path $probeBackup "$skin-command_window.xml"
    if (Test-Path -LiteralPath $commands) {
        $files += @{ Source = $commands; Target = (Join-Path $clientRoot "ui\$skin\command_window.xml") }
    }
}
foreach ($name in @('GameServer.dll','GameServer.pdb')) {
    foreach ($directory in @($serverRoot, (Join-Path $serverRoot 'lib'))) {
        $files += @{ Source = (Join-Path $sourceRoot "Release\lib\$name"); Target = (Join-Path $directory $name) }
    }
}
foreach ($entry in $files) {
    if (!(Test-Path -LiteralPath $entry.Source) -or !(Test-Path -LiteralPath $entry.Target)) { throw "Missing deployment input/target: $($entry.Target)" }
}
if (!$Apply) { Write-Output "Validated $($files.Count) files. Dry run only; use -Apply to deploy."; exit }
$backup = Join-Path $runtimeRoot ('rollback-native-raid-' + (Get-Date -Format 'yyyyMMdd-HHmmss'))
New-Item -ItemType Directory -Path $backup | Out-Null
$index = 0
foreach ($entry in $files) {
    $entry.Backup = Join-Path $backup ($index.ToString('D2') + '-' + (Split-Path $entry.Target -Leaf))
    $entry.OldHash = (Get-FileHash -LiteralPath $entry.Target).Hash
    $entry.NewHash = (Get-FileHash -LiteralPath $entry.Source).Hash
    Copy-Item -LiteralPath $entry.Target -Destination $entry.Backup
    $index++
}
$files | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $backup 'manifest.json')
Assert-Stopped
try {
    foreach ($entry in $files) {
        Copy-Item -LiteralPath $entry.Source -Destination $entry.Target -Force
        if ((Get-FileHash -LiteralPath $entry.Target).Hash -ne $entry.NewHash) { throw "Verification failed: $($entry.Target)" }
    }
} catch {
    foreach ($entry in $files) { Copy-Item -LiteralPath $entry.Backup -Destination $entry.Target -Force }
    throw
}
Write-Output "Installed and verified $($files.Count) files. Rollback copies: $backup"
Write-Output 'No account/settings/database files were changed by deployment. Server and client remain stopped.'
