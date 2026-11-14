$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$folder = Join-Path $root 'Assets/1.Prefabs/Normal Enemy/Temp_enemy'
$spawnFile = Join-Path $root 'Assets/1.Prefabs/Enemy Spawn System.prefab'
$spawn = [IO.File]::ReadAllText($spawnFile)
$typeNames = @('Enemy_movespeed2','Enemy_movespeed3','Enemy_movespeed4','ZombieHound')
$healthGuid = [regex]::Match([IO.File]::ReadAllText((Join-Path $root 'Assets/0.Scripts/Game/Enemy/NormalEnemy/EnemyHealth.cs.meta')), 'guid: (\w+)').Groups[1].Value
$followGuid = [regex]::Match([IO.File]::ReadAllText((Join-Path $root 'Assets/0.Scripts/Game/Enemy/NormalEnemy/EnemyFollow.cs.meta')), 'guid: (\w+)').Groups[1].Value
$count = 0
foreach ($type in $typeNames) {
    foreach ($minute in 3,6,9,12,15,18) {
        $path = Join-Path $folder ($type + '_' + $minute + 'min.prefab')
        if (!(Test-Path -LiteralPath $path)) { throw "Missing prefab: $type $minute" }
        $text = [IO.File]::ReadAllText($path)
        if (!$text.Contains('guid: ' + $healthGuid) -or !$text.Contains('guid: ' + $followGuid)) { throw "Missing enemy scripts: $path" }
        $count++
    }
}
Write-Output "PASS $count authored prefabs and required component references"
if ($spawn -notmatch '(?m)^  rosterMode: 1\r?$') { throw 'Spawn prefab does not enable StageMixing yet.' }
$mix = [regex]::Match($spawn, '(?s)  stageMixing:.*?(?=  waves:)').Value
if ($mix -notmatch 'transitionSeconds: 90') { throw 'Expected approved 90 second transition.' }
if ([regex]::Matches($mix, 'earlyWeight: 1\r?\n      lateWeight: 1').Count -ne 4) { throw 'Expected four equal early/late type weights.' }
Write-Output 'PASS mode, transition and initial archetype weights'
$index = 0
$references = [regex]::Matches($mix, '- \{fileID: (\d+), guid: (\w+), type: 3\}')
foreach ($type in $typeNames) {
    foreach ($minute in 3,6,9,12,15,18) {
        $path = Join-Path $folder ($type + '_' + $minute + 'min.prefab')
        $guid = [regex]::Match([IO.File]::ReadAllText($path + '.meta'), 'guid: (\w+)').Groups[1].Value
        $rootId = [regex]::Match([IO.File]::ReadAllText($path), '(?m)^--- !u!1 &(\d+)').Groups[1].Value
        if ($index -ge $references.Count -or $references[$index].Groups[1].Value -ne $rootId -or $references[$index].Groups[2].Value -ne $guid) { throw "Incorrect serialized slot: $type $minute" }
        $index++
    }
}
if ($references.Count -ne 24) { throw 'Expected exactly 24 references.' }
Write-Output 'PASS all 24 GUID/fileID references in stage order'
$spawnGuid = [regex]::Match([IO.File]::ReadAllText($spawnFile + '.meta'), 'guid: (\w+)').Groups[1].Value
$scene = [IO.File]::ReadAllText((Join-Path $root 'Assets/9.Scenes/MainScene.unity'))
if (!$scene.Contains('guid: ' + $spawnGuid) -or $scene -match 'propertyPath: (rosterMode|stageMixing)') { throw 'MainScene does not inherit the new mode/profile as expected.' }
Write-Output 'PASS MainScene inherits mixed mode without changing its existing overrides'
Push-Location $root
try {
    $baseline = (git show 'HEAD:Assets/1.Prefabs/Enemy Spawn System.prefab') -join "`n"
    $wavesPattern = '(?s)  waves:.*?(?=  settings:)'
    if ([regex]::Match($baseline,$wavesPattern).Value -ne [regex]::Match($spawn.Replace("`r`n","`n"),$wavesPattern).Value) { throw 'Legacy manual waves changed.' }
    Write-Output 'PASS existing manual waves preserved'
    $metaPaths = @('Assets/0.Scripts/Game/Enemy/NormalEnemy/EnemySpawnProgression.cs.meta','Assets/Editor/EnemySpawnDirectorEditor.cs.meta','Assets/Tests/EnemyStageMixing.meta') + @(Get-ChildItem 'Assets/Tests/EnemyStageMixing' -Filter '*.meta' -Recurse | ForEach-Object FullName)
    foreach ($meta in $metaPaths) {
        $metaPath = if ([IO.Path]::IsPathRooted($meta)) { $meta } else { Join-Path $root $meta }
        $guid = [regex]::Match([IO.File]::ReadAllText($metaPath), '(?m)^guid: ([a-f0-9]{32})\r?$').Groups[1].Value
        if ($guid.Length -ne 32) { throw "Invalid GUID: $meta" }
        $guidFiles = @(rg -l --glob '*.meta' "^guid: $guid\r?`$" Assets)
        if ($guidFiles.Count -ne 1) { throw "Duplicated GUID: $meta" }
    }
    Write-Output 'PASS new asset GUIDs are unique'
} finally { Pop-Location }
Write-Output 'RESULT 6/6 asset checks passed'
