# Soul Pipeline 진행 로그 (단일 원본)

노션에는 이 내용을 **한 페이지**에만 올린다. 분산 금지.

## 브랜치
`feature/soul-pipeline-3d`

## 완료된 구현
- URP `UniversalRenderer` (index1), 기본 2D Renderer 유지
- 카메라: 기울인 탑뷰(yaw 0 / pitch 55, ortho 10)
- `SoulArena3D` 씬 + Build Settings 등록
- 런타임: CameraRig, Avatar, BossApproachSlash, WeaponHitbox3D, Bootstrap, FallbackFactory, GroundGravity, DummyHealth, TrashMob
- KinemIQ CC0 휴머노이드 FBX + Prefab(`SoulPlayerHumanoid` / `SoulBossHumanoid`, 손 `hand_r` 히트박스)
- 진입: 로비 도감탭/F6, Esc→로비, CombatUI `테스트/SoulArena3D 로드`
- 플레이어/보스/잡몹 MainScene 스프라이트 스킨
- 본게임 정면 뼈대: `SoulPlayerBoneRigSkin` + Walk Inspector 튜닝
- 비스트 보스 Idle east/west 배 실루엣 정리(8 PNG)
- F2 원근 확인 뷰(기본 직교 유지)

## Play
1. Play From Title 끄기
2. `Tools > Soul Pipeline > Play Setup` 또는 로비 F6/도감
3. WASD / Shift대시 / E / 좌클릭 / Esc / R / 휠 / F1 디버그 / F5 뼈대 비교

## 검증(최근)
- CheckFiles 37/37 + GUID 통과
- `C:\sv\soul` Play 프로브 전체 PASS(스킨·뼈대·접근슬래시·패링 등) + 애니 이벤트 오류 0
- 걷기: 발 3.0px / 머리카락 2.2px / 엉덩이 1.4px / 몸통 늘어남 x3.79
- Idle 배: east/west 복부 앞선 2~5px 집어넣음(비교 `.utmp/belly_before_after.png`)
- 에디터 육안·기기·Android·Mixamo 교체 미실시

## 레거시
MainScene·BeastZombie 2D+바닥 VFX 유지. MotionBossPrototype=조기토이. pixelfx=비핵.

## 다음 할 일
1. 육안: 뼈대 걷기(F5), Idle 좌·우 배. 필요 시 대각 Idle도 정리
2. 뼈대 다방향(아트·손 소켓 좌표 확정 후)
3. Mixamo/야수좀비 Prefab 계약 교체
4. BossEncounter → Soul 보스 치환(합류 범위 합의 후)
5. 공격 판정↔지팡이 타이밍·페이즈2 기믹은 기획 확정 전 보류

## 노션
https://app.notion.com/p/414283dd247981f49c61ee058a887059
