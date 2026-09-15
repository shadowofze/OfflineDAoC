$ErrorActionPreference = 'Stop'
$runtime = 'C:\Users\thedo\Desktop\Offline DAoC\runtime'
function Assert-Stopped {
    if (Get-Process CoreServer,game,'game.dll',camelot,OfflineDAoC,connect -ErrorAction SilentlyContinue) {
        throw 'Close server, client and launcher before rollback.'
    }
}
Assert-Stopped
$manifest = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'manifest.json') -Raw | ConvertFrom-Json
foreach ($entry in $manifest.Entries) {
    $target = [IO.Path]::GetFullPath((Join-Path $runtime $entry.Relative))
    if ($target -ne $entry.Target -or !$target.StartsWith($runtime + '\', [StringComparison]::OrdinalIgnoreCase)) { throw 'Invalid rollback target.' }
    $current = (Get-FileHash -LiteralPath $target).Hash
    if ($current -ne $entry.NewHash -and $current -ne $entry.OldHash) { throw "Newer changes exist at $target; rollback stopped." }
    if ((Get-FileHash -LiteralPath (Join-Path $PSScriptRoot $entry.Relative)).Hash -ne $entry.OldHash) { throw 'Invalid backup.' }
}
foreach ($entry in $manifest.Entries) {
    Assert-Stopped
    $temporary = $entry.Target + '.ground-speed-rollback'
    if (Test-Path -LiteralPath $temporary) { throw 'Unexpected rollback staging file.' }
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot $entry.Relative) -Destination $temporary
    [IO.File]::Move($temporary, $entry.Target, $true)
    if ((Get-FileHash -LiteralPath $entry.Target).Hash -ne $entry.OldHash) { throw 'Restored hash mismatch.' }
}
Write-Output 'Pre-ground-speed-fix binaries restored. Accounts, inventories, coins, world database and settings were not rolled back.'
