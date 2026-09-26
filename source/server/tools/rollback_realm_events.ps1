$ErrorActionPreference = 'Stop'
if (Get-Process CoreServer,game,camelot,OfflineDAoC,connect -ErrorAction SilentlyContinue) {
    throw 'Close the server, client and launcher before rollback.'
}
$runtime = 'C:\Users\thedo\Desktop\new class test\runtime'
$manifest = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'manifest.json') -Raw | ConvertFrom-Json
foreach ($entry in $manifest.Entries) {
    $target = [IO.Path]::GetFullPath((Join-Path $runtime $entry.Relative))
    if ($target -ne $entry.Target -or !$target.StartsWith($runtime + '\', [StringComparison]::OrdinalIgnoreCase)) {
        throw 'Invalid rollback target.'
    }
    $current = (Get-FileHash -LiteralPath $target).Hash
    if ($current -ne $entry.NewHash -and $current -ne $entry.OldHash) {
        throw "A newer change exists at $target; rollback stopped to preserve it."
    }
    if ((Get-FileHash -LiteralPath (Join-Path $PSScriptRoot $entry.Relative)).Hash -ne $entry.OldHash) {
        throw 'Backup verification failed.'
    }
}
foreach ($entry in $manifest.Entries) {
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot $entry.Relative) -Destination ($entry.Target + '.realm-events-rollback')
    [IO.File]::Move($entry.Target + '.realm-events-rollback', $entry.Target, $true)
    if ((Get-FileHash -LiteralPath $entry.Target).Hash -ne $entry.OldHash) { throw 'Restored file hash mismatch.' }
}
Write-Output 'Realm Events binaries and Glacier navigation rolled back. Accounts, settings and progress were not changed.'
