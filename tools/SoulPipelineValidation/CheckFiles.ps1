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
  "Assets/0.Scripts/Game/SoulPipeline/SoulWeaponArmedPulse.cs",
  "Assets/0.Scripts/Game/SoulPipeline/SoulGroundGravity.cs",
  "Assets/Editor/Settings/UniversalRenderer.asset",
  "docs/AI/SoulPipeline-Progress.md",
  "Assets/2.Resources/Art/SoulPipeline/Humanoid/KinemIQ_Mannequin.fbx",
  "Assets/2.Resources/Art/SoulPipeline/Humanoid/Sword_slash.fbx",
  "Assets/2.Resources/Art/SoulPipeline/Humanoid/Walk_forwards.fbx",
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
exit 0
