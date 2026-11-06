$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$source = Get-Content -LiteralPath (Join-Path $projectRoot 'Assets/0.Scripts/Game/Player/Test/DamageNumberTest.cs') -Raw
$match = [regex]::Match($source, 'canvas\.sortingOrder\s*=\s*(-?\d+)\s*;')
if (-not $match.Success) { throw '데미지 숫자 Canvas 정렬값을 찾을 수 없습니다.' }
$damageOrder = [int]$match.Groups[1].Value
$checked = 0
foreach ($scene in @('MainTestScene', 'GiungTestScene')) {
    $yaml = Get-Content -LiteralPath (Join-Path $projectRoot "Assets/9.Scenes/$scene.unity") -Raw
    foreach ($block in [regex]::Matches($yaml, '(?ms)^--- !u!223 .*?(?=^--- |\z)')) {
        if ($block.Value -notmatch 'm_RenderMode: 0') { continue }
        $order = [regex]::Match($block.Value, 'm_SortingOrder: (-?\d+)')
        if (-not $order.Success) { throw "$scene Canvas 정렬값 누락" }
        if ($damageOrder -ge [int]$order.Groups[1].Value) {
            throw "$scene UI보다 숫자가 앞에 표시될 수 있습니다: 숫자=$damageOrder UI=$($order.Groups[1].Value)"
        }
        $checked++
    }
}
if ($checked -eq 0) { throw '검증할 Overlay Canvas가 없습니다.' }
Write-Output "PASS: 데미지 숫자 Order=$damageOrder, 두 테스트 씬 Overlay Canvas $checked 개보다 뒤에 표시"
