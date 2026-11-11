param(
  [string]$CopyRoot = 'C:\sv\soul',
  [string]$LogFile = 'C:\sv\soul-batch-setup.log'
)
$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$unityExe = 'C:/Program Files/Unity/Hub/Editor/6000.5.0f1/Editor/Unity.exe'
if (!(Test-Path $unityExe)) { throw "Unity 없음: $unityExe" }

New-Item -ItemType Directory -Force -Path $CopyRoot | Out-Null
robocopy "$projectRoot\Assets\0.Scripts\Game\SoulPipeline" "$CopyRoot\Assets\0.Scripts\Game\SoulPipeline" /E /NFL /NDL /NJH /NJS /nc /ns /np | Out-Null
robocopy "$projectRoot\Assets\Editor\SoulPipeline" "$CopyRoot\Assets\Editor\SoulPipeline" /E /NFL /NDL /NJH /NJS /nc /ns /np | Out-Null
robocopy "$projectRoot\Assets\0.Scripts\Game\Prototype\MotionBoss" "$CopyRoot\Assets\0.Scripts\Game\Prototype\MotionBoss" MotionBossWeaponHitbox.cs /NFL /NDL /NJH /NJS /nc /ns /np | Out-Null

$args = @(
  '-batchmode', '-nographics',
  '-projectPath', $CopyRoot,
  '-executeMethod', 'SoulPipelineBuilder.OneClickSetupBatch',
  '-logFile', $LogFile
)
$p = Start-Process -FilePath $unityExe -ArgumentList $args -Wait -PassThru -WindowStyle Hidden
Write-Output "UNITY_EXIT:$($p.ExitCode)"
if ($p.ExitCode -ne 0) { throw "Batch One-Click 실패. 로그: $LogFile" }

$required = @(
  'Assets/1.Prefabs/SoulPipeline/SoulPlayerHumanoid.prefab',
  'Assets/1.Prefabs/SoulPipeline/SoulBossHumanoid.prefab',
  'Assets/Resources/SoulPipeline/SoulPlayerHumanoid.prefab',
  'Assets/Resources/SoulPipeline/SoulBossHumanoid.prefab',
  'Assets/9.Scenes/SoulArena3D.unity'
)
$missing = @()
foreach ($rel in $required) {
  if (!(Test-Path (Join-Path $CopyRoot $rel))) { $missing += $rel }
}
if ($missing.Count -gt 0) {
  $missing | ForEach-Object { Write-Output "MISSING $_" }
  throw 'Prefab/씬 생성 누락'
}
Write-Output "PASS Batch One-Click prefabs $($required.Count)/$($required.Count)"
