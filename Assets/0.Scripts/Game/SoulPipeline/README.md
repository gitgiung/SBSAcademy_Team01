# SoulPipeline 런타임

본선 병행 씬 `SoulArena3D`용 스크립트. MainScene Player/Enemy와 분리.

## 핵심
- `SoulArenaBootstrap` — 월드/폴백 생성, HUD, Esc/R
- `SoulBossApproachSlash` — Chase → Windup → Attack → Recovery
- `SoulWeaponHitbox3D` — 손 트리거 + Overlap 보조
- `SoulPipelineAvatar` — 이동/대시/패링/근접
- `SoulArenaWaveDirector` — 이완 잡몹 → 보스 해제
- `SoulArenaFallbackFactory` — Prefab 없을 때 폴백 몸

## Play
`Tools > Soul Pipeline > 0. One-Click Setup` 또는 로비 F6/도감.
상세: `docs/AI/SoulPipeline-TeamGuide.md`
