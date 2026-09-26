$ErrorActionPreference = 'Stop'
$runtime = 'C:\Users\thedo\Desktop\new class test\runtime'
function Assert-Stopped {
    if (Get-Process CoreServer,game,camelot,OfflineDAoC,connect -ErrorAction SilentlyContinue) {
        throw 'Close server, game and launcher before rollback.'
    }
}
Assert-Stopped
$manifest = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'manifest.json') -Raw | ConvertFrom-Json
foreach ($entry in $manifest.Entries) {
    $target = [IO.Path]::GetFullPath((Join-Path $runtime $entry.Relative))
    if ($target -ne $entry.Target -or !$target.StartsWith($runtime + '\', [StringComparison]::OrdinalIgnoreCase)) { throw 'Invalid target.' }
    $hash = (Get-FileHash $target).Hash
    if ($hash -ne $entry.OldHash -and $hash -ne $entry.NewHash) { throw 'Newer deployment found; rollback stopped.' }
    if ((Get-FileHash (Join-Path $PSScriptRoot $entry.Relative)).Hash -ne $entry.OldHash) { throw 'Invalid backup.' }
}
foreach ($entry in $manifest.Entries) {
    Assert-Stopped
    $temporary = $entry.Target + '.horse-attachment-rollback'
    if (Test-Path -LiteralPath $temporary) { throw 'Unexpected rollback staging file.' }
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot $entry.Relative) -Destination $temporary
    [IO.File]::Move($temporary, $entry.Target, $true)
    if ((Get-FileHash $entry.Target).Hash -ne $entry.OldHash) { throw 'Restored hash mismatch.' }
}
Write-Output 'Restored pre-fix server binaries. Player/bot progress, settings and client unchanged.'
