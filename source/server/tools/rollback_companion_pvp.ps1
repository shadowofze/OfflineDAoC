$ErrorActionPreference = 'Stop'
$runtime = 'C:\Users\thedo\Desktop\Offline DAoC\runtime'
function Assert-Stopped {
    if (Get-Process CoreServer,OfflineDAoC,game,'game.dll',camelot,connect -ErrorAction SilentlyContinue) {
        throw 'Close server, client and launcher first.'
    }
}
Assert-Stopped
$manifest = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'manifest.json') -Raw | ConvertFrom-Json
foreach ($entry in $manifest.Entries) {
    $target = [IO.Path]::GetFullPath((Join-Path $runtime $entry.Relative))
    if ($target -ne $entry.Target -or !$target.StartsWith($runtime + '\', [StringComparison]::OrdinalIgnoreCase)) { throw 'Invalid rollback target.' }
    $current = (Get-FileHash -LiteralPath $target).Hash
    if ($current -ne $entry.NewHash -and $current -ne $entry.OldHash) { throw 'Newer changes exist; rollback stopped.' }
    if ((Get-FileHash -LiteralPath (Join-Path $PSScriptRoot $entry.Relative)).Hash -ne $entry.OldHash) { throw 'Backup hash mismatch.' }
}
foreach ($entry in $manifest.Entries) {
    Assert-Stopped
    $temporary = $entry.Target + '.companion-pvp-rollback'
    if (Test-Path -LiteralPath $temporary) { throw 'Unexpected rollback staging file.' }
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot $entry.Relative) -Destination $temporary
    [IO.File]::Move($temporary, $entry.Target, $true)
    if ((Get-FileHash -LiteralPath $entry.Target).Hash -ne $entry.OldHash) { throw 'Restored hash mismatch.' }
}
Write-Output 'Previous server binaries restored. Account progress and world data were not rolled back.'
