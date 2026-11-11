# Soul Pipeline 진행 로그 (단일 원본)

노션에는 이 내용을 **한 페이지**에만 올린다. 분산 금지.

## 브랜치
`feature/soul-pipeline-3d` — 검증·컴파일 수정·Humanoid Prefab 조립 반영(커밋 요청 전)

## 완료된 구현
- URP `UniversalRenderer` (index1), 기본 2D Renderer 유지
- 카메라: Main과 비슷하게 기울인 탑뷰(yaw 0 / pitch 55, ortho 10)
- `SoulArena3D` 씬 + Build Settings 등록
- 런타임: CameraRig, Avatar, BossApproachSlash, WeaponHitbox3D, Bootstrap, FallbackFactory, GroundGravity, DummyHealth, TrashMob
- KinemIQ CC0 휴머노이드 FBX (Idle/Walk/Slash 등) Humanoid Rig
- Prefab: `SoulPlayerHumanoid` / `SoulBossHumanoid` (+ Resources 복사), 보스 손 `hand_r`에 `SoulWeaponHitbox3D`
- 에디터: One-Click Setup, Batch One-Click(`SoulPipelineBuilder.OneClickSetupBatch`)
- 진입: 로비 도감탭/F6, Esc→로비, CombatUI `테스트/SoulArena3D 로드`
- 폴백 3D로 Prefab 없이도 Play 가능(지금은 Prefab 우선)
- 플레이어: 좌클릭/J 공격, Shift/Space 대시, E 패링
- 보스: Chase→Windup(다가오며)→Attack(손 히트)→Recovery
- 잡몹 이완·보충, 승패 HUD, 패링 리포스트, KillFeed, P/F1
- 컴파일: meta GUID 32자리 수정, `GetEntityId` 교체, FindAnyObjectByType
- 플레이어 표시: MainScene 캐릭터 스프라이트·애니메이터(`PlayerAnimator/Player`, 스케일 3) 빌보드 스킨 `SoulPlayerSpriteSkin` — 8방향·이동·대시·패링·공격 상태 연동. Bootstrap `useMainPlayerSprite`로 끄면 마네킹
- 잡몹 표시: MainScene 일반 좀비(`Enemy`) 스프라이트·`Zombie.controller`(스케일 3) `SoulTrashMobSpriteSkin` — 플레이어 방향 8방향, 추적 ZombieMove/정지 ZombieIdle, 피격 빨간 플래시- 2D 뼈대 리깅 시험판: `SoulPlayerBoneRig`(정면 대기 1장, 뼈대 11개) + F3 비교 `SoulBoneRigPreview`. 작은 동작은 가능, 큰 동작은 레이어 원본 필요- 보스 표시: MainScene BeastZombie 스프라이트·`Boss.controller`(스케일 4) `SoulBossSpriteSkin` — 추격/정지/8방향 휘두르기, 타격 프레임을 손 히트박스 무장 순간에 맞춤. 참조 에셋 `Resources/SoulPipeline/SoulArenaSpriteSet`
- F2 원근 확인 뷰(기본 직교 유지)

## Play
1. Play From Title 끄기
2. `Tools > Soul Pipeline > Play Setup` 또는 로비 F6/도감
3. WASD / Shift대시 / E / 좌클릭 / Esc / R / 휠 / F1 디버그
4. Prefab이 있으면 HUD에 Humanoid/Prefab, 없으면 폴백3D

## 검증(이번)
- `tools/SoulPipelineValidation/CheckFiles.ps1` 파일+GUID 통과
- 격리 복사본 `C:\sv\soul`에서 Batch One-Click: 손히트=`hand_r` fallback=False
- Play 프로브 7/7 + 애니 이벤트 오류 0: 접근슬래시(+메인·보스 스프라이트)·패링·히트1회·리커버리/사망·플레이어 스킨·F2 원근 뷰·보스 스킨(히트박스↔타격 프레임 0.01s)
- 화면 캡처: `RunPlayProbe.ps1 -Capture` → 적용 전/후 PNG(복사본 그래픽 렌더)
- 실기기·Mixamo 교체는 미실시

## 레거시
MainScene·BeastZombie 2D+바닥 VFX 유지. MotionBossPrototype=조기토이. pixelfx=비핵.

## 남은 일
1. 팀 Unity에서 Play로 손 모션·패링 가독성 확인
2. Mixamo/야수좀비 교체(같은 Prefab 계약)
3. BossEncounter 치환
4. 페이즈2 기믹(기획 후)

## 노션
https://app.notion.com/p/414283dd247981f49c61ee058a887059
