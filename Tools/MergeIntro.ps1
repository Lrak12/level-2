param([switch]$Apply)
$ErrorActionPreference = 'Stop'
$sourceRoot = 'E:\Unity\Unity Hub\Intro\Assets'
$projectRoot = 'E:\Unity\Unity Hub\level 2'
$targetRoot = Join-Path $projectRoot 'Assets'
$importRoot = Join-Path $targetRoot 'Intro'
function Index-Guids($root) {
    $index = @{}
    Get-ChildItem -LiteralPath $root -Recurse -File -Filter '*.meta' | ForEach-Object {
        $match = [regex]::Match([IO.File]::ReadAllText($_.FullName), '(?m)^guid: ([a-f0-9]{32})')
        if ($match.Success) { $index[$match.Groups[1].Value] = $_.FullName.Substring(0, $_.FullName.Length - 5) }
    }
    return $index
}
$sourceIndex = Index-Guids $sourceRoot
$targetIndex = Index-Guids $targetRoot
$queue = [Collections.Generic.Queue[string]]::new()
$queue.Enqueue('99c9720ab356a0642a771bea13969a05')
$seen = @{}; $remap = @{}; $imports = @(); $shared = @()
$textExtensions = @('.unity', '.asset', '.prefab', '.mat', '.controller', '.anim', '.inputactions', '.cs', '.shader', '.shadergraph', '.cginc', '.hlsl')
while ($queue.Count) {
    $guid = $queue.Dequeue()
    if ($seen.ContainsKey($guid)) { continue }
    $seen[$guid] = $true
    if (!$sourceIndex.ContainsKey($guid)) { continue }
    $source = $sourceIndex[$guid]
    if (!(Test-Path -LiteralPath $source -PathType Leaf)) { continue }
    $relative = $source.Substring($sourceRoot.Length + 1)
    if ($targetIndex.ContainsKey($guid)) {
        $same = (Get-FileHash -LiteralPath $source).Hash -eq (Get-FileHash -LiteralPath $targetIndex[$guid]).Hash
        if ($same) { $shared += $targetIndex[$guid]; continue }
        $remap[$guid] = [guid]::NewGuid().ToString('N')
    }
    $destination = Join-Path $importRoot $relative
    if ($relative -eq 'Scenes\SampleScene.unity') { $destination = Join-Path $importRoot 'Scenes\Intro.unity' }
    if (Test-Path -LiteralPath $destination) { throw "Refusing to overwrite existing import: $destination" }
    $imports += [PSCustomObject]@{Source=$source; Destination=$destination; Guid=$guid; Hash=(Get-FileHash -LiteralPath $source).Hash}
    $texts = @([IO.File]::ReadAllText("$source.meta"))
    if ([IO.Path]::GetExtension($source) -in $textExtensions) { $texts += [IO.File]::ReadAllText($source) }
    foreach ($text in $texts) {
        foreach ($match in [regex]::Matches($text, 'guid: ([a-f0-9]{32})')) { $queue.Enqueue($match.Groups[1].Value) }
    }
}
$report = [PSCustomObject]@{Imports=$imports; Reused=$shared; RemappedGuids=$remap}
if (!$Apply) { $report | ConvertTo-Json -Depth 5; exit }
if (Test-Path -LiteralPath $importRoot) { throw 'Intro import folder already exists. Review it before reimporting.' }
$backup = Join-Path $projectRoot ('IntroMerge_Backup\' + (Get-Date -Format 'yyyyMMdd-HHmmss'))
New-Item -ItemType Directory -Path $backup -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $projectRoot 'ProjectSettings\EditorBuildSettings.asset') -Destination $backup
$baseline = Get-ChildItem -LiteralPath $targetRoot -Recurse -File | Where-Object { $_.Extension -eq '.unity' } | ForEach-Object { [PSCustomObject]@{Path=$_.FullName;Hash=(Get-FileHash -LiteralPath $_.FullName).Hash} }
$baseline | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $backup 'SceneHashes.json') -Encoding UTF8
$utf8 = [Text.UTF8Encoding]::new($false)
foreach ($item in $imports) {
    New-Item -ItemType Directory -Path (Split-Path $item.Destination) -Force | Out-Null
    Copy-Item -LiteralPath $item.Source -Destination $item.Destination
    Copy-Item -LiteralPath ($item.Source + '.meta') -Destination ($item.Destination + '.meta')
    $files = @($item.Destination + '.meta')
    if ([IO.Path]::GetExtension($item.Destination) -in $textExtensions) { $files += $item.Destination }
    foreach ($file in $files) {
        $content = [IO.File]::ReadAllText($file)
        foreach ($oldGuid in $remap.Keys) { $content = $content.Replace($oldGuid, $remap[$oldGuid]) }
        [IO.File]::WriteAllText($file, $content, $utf8)
    }
}
$report | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $backup 'ImportManifest.json') -Encoding UTF8
Write-Output "Imported $($imports.Count) assets, reused $($shared.Count) shared assets, remapped $($remap.Count) conflicting GUID. Backup: $backup"