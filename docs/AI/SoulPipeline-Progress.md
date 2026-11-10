# Soul Pipeline 진행 로그 (단일 원본)

노션에는 이 내용을 **한 페이지**에만 올린다. 분산 금지.

## 브랜치
`feature/soul-pipeline-3d` (커밋 08:00 예약)

## 완료된 구현
- URP `UniversalRenderer` (index1), 기본 2D Renderer 유지
- `SoulArena3D` 씬 + Build Settings 등록
- 런타임: CameraRig, Avatar, BossApproachSlash, WeaponHitbox3D, Bootstrap, FallbackFactory, GroundGravity, DummyHealth, TrashMob
- KinemIQ CC0 휴머노이드 FBX (Idle/Walk/Slash 등)
- 에디터: One-Click Setup, Prefab→Resources 복사
- 진입: 로비 도감탭/F6, Esc→로비, CombatUI `테스트/SoulArena3D 로드`
- 폴백 3D로 Prefab 없이 Play 가능
- 플레이어: 좌클릭/J 공격, Shift/Space 대시, E 패링
- 보스: 다가오며 손 히트 / 사망 시 AI 정지
- 잡몹 3: 단순 접근 접촉(정밀 판정 아님)
- 피격 스케일 피드백, 맵 경계 클램프, R 재시작, 휠 줌
- 승패 HUD (보스 처치 / 플레이어 사망)
- WaveDirector: ~25초/잡몹 정리 후 보스 + 이완 중 잡몹 보충(최대 8)
- 보스 패턴: Chase → Windup(다가오며 올림) → Attack(히트) → Recovery(뒤로 물러남)
- 윈드업 시 카메라가 보스를 살짝 따라가 모션 가독성 보조
- 보스 사망 시 AI·히트박스 즉시 정지
- 폴백 팔: 윈드업 올림 → 내려찍기, 플레이어 Pulse 스윙
- 패링/피격/승패 KillFeed, P 일시정지, F1(윈드업·armed·trash)
- 윈드업 HUD 보조(`↑ 휘두르기!`) — 바닥 VFX 아님
- 카메라 쉐이크, 잡몹 6, 보스 몸 틴트(윈드업 노랑/공격 빨강)
- 근접 타격 최대 3대상, 피격 넉백, 패링/피격 틴트
- Esc/R 시 timeScale 복구, Validate/파일 체크 19항
- 대시 잔상(`SoulPlayerDashTrail`), 보스 윈드업 0.48s / 리커버리 0.7s / 개시거리 2.85
- 보스 윈드업 진입 시 측면 커밋(직선 추격과 구분), 피격 화이트 틴트
- 손 `SoulWeaponArmedPulse`(armed 스케일), 폴백 팔 Yaw 스윙
- 잡몹: 측면 접근·분리·보스 공격 중 감속
- 보스: 리커버리 후 `postRecoveryGap`으로 즉시 재윈드업 방지
- 윈드업 중 패링 창 +0.06, 대시→패링 캔슬, 리커버리 타격 18
- 패링 성공 시 `StunFromParry`→리커버리 + 리포스트 힌트
- HUD: 윈드업/반격 창 안내
- Prefab 빌더: Bounds/WindupFlash/CameraShake + boss Bind, TeamGuide 계약 절
- KillFeed 페이드, 잡몹 Separation 0.2s 캐시, SoulArena3DScene.Clear timeScale=1
- HUD DASH/PARRY 표시
- 입력 힌트 15초, 3분 work-continue 웨이크로 턴 사이 재개
- 노션 기록은 작업 DB 해당 페이지 **한곳만**

## Play
1. Play From Title 끄기
2. `Tools > Soul Pipeline > 0. One-Click Setup` 또는 Play Setup
3. WASD / Shift대시 / E / 좌클릭 / Esc / R / 휠 / F1 디버그

## 레거시
MainScene·BeastZombie 2D+바닥 VFX 유지. MotionBossPrototype=조기토이. pixelfx=비핵.

## 남은 일
1. Humanoid Prefab Play 안정화(팀 Unity에서 메뉴 0 실행)
2. Mixamo/야수좀비 교체
3. BossEncounter 치환
4. 페이즈2 기믹(기획 후)

## 노션
https://app.notion.com/p/414283dd247981f49c61ee058a887059
