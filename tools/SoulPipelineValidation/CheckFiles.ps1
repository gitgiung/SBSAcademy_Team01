$ErrorActionPreference = "Stop"
$root = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$required = @(
  "Assets/9.Scenes/SoulArena3D.unity",
  "Assets/0.Scripts/Game/SoulPipeline/SoulArenaBootstrap.cs",
  "Assets/0.Scripts/Game/SoulPipeline/SoulBossApproachSlash.cs",
  "Assets/0.Scripts/Game/SoulPipeline/SoulWeaponHitbox3D.cs",
  "Assets/0.Scripts/Game/SoulPipeline/SoulPipelineAvatar.cs",
  "Assets/0.Scripts/Game/SoulPipeline/SoulCameraRig.cs",
  "Assets/0.Scripts/Game/SoulPipeline/SoulArenaFallbackFactory.cs",
  "Assets/0.Scripts/Game/SoulPipeline/SoulArenaTrashMob.cs",
  "Assets/0.Scripts/Game/SoulPipeline/SoulArenaDummyHealth.cs",
  "Assets/0.Scripts/Game/SoulPipeline/SoulArenaWaveDirector.cs",
  "Assets/0.Scripts/Game/SoulPipeline/SoulArenaBossTelegraphHud.cs",
  "Assets/0.Scripts/Game/SoulPipeline/SoulBossWindupFlash.cs",
  "Assets/0.Scripts/Game/SoulPipeline/SoulPlayerDashTrail.cs",
  "Assets/0.Scripts/Game/SoulPipeline/SoulPlayerSpriteSkin.cs",
  "Assets/0.Scripts/Game/SoulPipeline/SoulSpriteBillboard.cs",
  "Assets/0.Scripts/Game/SoulPipeline/SoulBossSpriteSkin.cs",
  "Assets/0.Scripts/Game/SoulPipeline/SoulTrashMobSpriteSkin.cs",
  "Assets/0.Scripts/Game/SoulPipeline/SoulPlayerBoneRig.cs",
  "Assets/0.Scripts/Game/SoulPipeline/SoulBoneRigPreview.cs",
  "Assets/0.Scripts/Game/SoulPipeline/SoulPlayerBoneRigSkin.cs",
  "Assets/0.Scripts/Game/SoulPipeline/SoulSpriteAnimationEventSink.cs",
  "Assets/0.Scripts/Game/SoulPipeline/SoulArenaSpriteSet.cs",
  "Assets/Resources/SoulPipeline/SoulArenaSpriteSet.asset",
  "Assets/Resources/PlayerAnimator/Player.controller",
  "Assets/0.Scripts/Game/SoulPipeline/SoulWeaponArmedPulse.cs",
  "Assets/0.Scripts/Game/SoulPipeline/SoulGroundGravity.cs",
  "Assets/Editor/Settings/UniversalRenderer.asset",
  "docs/AI/SoulPipeline-Progress.md",
  "Assets/2.Resources/Art/SoulPipeline/Humanoid/KinemIQ_Mannequin.fbx",
  "Assets/2.Resources/Art/SoulPipeline/Humanoid/Sword_slash.fbx",
  "Assets/2.Resources/Art/SoulPipeline/Humanoid/Walk_forwards.fbx",
  "Assets/1.Prefabs/SoulPipeline/SoulPlayerHumanoid.prefab",
  "Assets/1.Prefabs/SoulPipeline/SoulBossHumanoid.prefab",
  "Assets/Resources/SoulPipeline/SoulPlayerHumanoid.prefab",
  "Assets/Resources/SoulPipeline/SoulBossHumanoid.prefab",
  "Assets/2.Resources/Art/SoulPipeline/Generated/SoulBoss.controller",
  "docs/AI/SoulPipeline-TeamGuide.md"
)
$missing = @()
foreach ($rel in $required) {
  $path = Join-Path $root $rel
  if (-not (Test-Path $path)) { $missing += $rel }
}
if ($missing.Count -gt 0) {
  Write-Output "FAIL missing:"
  $missing | ForEach-Object { Write-Output " - $_" }
  exit 1
}
Write-Output "PASS $($required.Count)/$($required.Count) Soul Pipeline files present"

$metaRoots = @(
  "Assets/0.Scripts/Game/SoulPipeline",
  "Assets/Editor/SoulPipeline",
  "Assets/0.Scripts/Framework/Scenes/SoulArena3DScene.cs.meta",
  "Assets/9.Scenes/SoulArena3D.unity.meta"
)
$badGuids = @()
foreach ($rel in $metaRoots) {
  $path = Join-Path $root $rel
  $files = if ((Get-Item $path).PSIsContainer) { Get-ChildItem $path -Filter *.meta -File } else { @(Get-Item $path) }
  foreach ($file in $files) {
    $match = Select-String -Path $file.FullName -Pattern '^guid: (\S+)' | Select-Object -First 1
    $guid = if ($match) { $match.Matches.Groups[1].Value } else { "" }
    if ($guid -notmatch '^[0-9a-fA-F]{32}$') {
      $badGuids += "$($file.FullName.Substring($root.Path.Length + 1)) len=$($guid.Length)"
    }
  }
}
if ($badGuids.Count -gt 0) {
  Write-Output "FAIL guid:"
  $badGuids | ForEach-Object { Write-Output " - $_" }
  exit 1
}
Write-Output "PASS $($metaRoots.Count) Soul Pipeline meta roots have 32-hex GUIDs"
exit 0
