$ErrorActionPreference = 'Stop'
$runtime = 'C:\Users\thedo\Desktop\new class test\runtime'
function Assert-Stopped {
    if (Get-Process CoreServer,game,camelot,OfflineDAoC,connect -ErrorAction SilentlyContinue) { throw 'Close server, client and launcher before rollback.' }
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
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot $entry.Relative) -Destination ($entry.Target + '.bot-goals-rollback')
    [IO.File]::Move($entry.Target + '.bot-goals-rollback', $entry.Target, $true)
    if ((Get-FileHash -LiteralPath $entry.Target).Hash -ne $entry.OldHash) { throw 'Restored hash mismatch.' }
}
Write-Output 'Bot Goals Setting binaries rolled back. Accounts and progress unchanged. Any saved bot-goals.json is preserved but ignored by the older server.'
