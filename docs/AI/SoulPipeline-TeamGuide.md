# Soul Pipeline 팀 안내 (마감용)

목표: **본선 파이프라인**이 프로젝트에 박혀, 팀이 Mixamo/야수좀비 아트·패턴을 이어서 넣을 수 있는 상태.  
토이(MotionBossPrototype 스프라이트)는 참고용 레거시로 둔다.

## 한 줄 요약

`SoulArena3D` = 평탄 3D 맵 + URP 3D Renderer + 휴머노이드 본 애니 + **다가오며 슬래시(손 히트박스)**.  
`MainScene` = 당분간 기존 2D/도트·뱀서·성장 UI **유지(레거시 플레이 가능)**. 합류는 아래 단계.

## Prefab 계약 (휴머노이드=폴백 동일)
- 플레이어: `SoulPipelineAvatar` + CC/Gravity/Bounds · Animator `Idle`/`Walk`
- 보스: `SoulBossApproachSlash` + DummyHealth + WindupFlash · 손 `SoulWeaponHitbox3D`(+ArmedPulse) · `Idle`/`Walk`/`Slash`
- 카메라: `SoulCameraRig` + `SoulCameraShake`, URP renderer #1

## Play 확인 (전원)

1. Unity 임포트 끝날 때까지 대기. Console 오류 없는지 확인.
2. `Tools > Play From Title` **끄기**.
3. 진입 방법 (아무거나):
   - `Tools > Soul Pipeline > Play Setup` → Play
   - 타이틀→로비 후 **도감 탭** 또는 **F6**
   - MainScene Play 중 CombatUI ⋮ → `테스트/SoulArena3D 로드`
4. Prefab 없으면 HUD에 `폴백3D`. 본 애니는 메뉴 1→2→3.
5. 조작: WASD · Shift/Space 대시 · E 패링 · 좌클릭/J 공격 · 휠 줌 · Esc 로비 · R 재시작 · P 일시정지 · F1 디버그  
6. 흐름: 잡몹 이완(~22초/정리) → 보스 Chase→**Windup(다가오며 올림)**→Attack→Recovery  
7. 학습 루프: 윈드업에 E 패링 → 보스 리커버리 → 클릭 추가타. HUD 안내는 보조.  
8. **합격**: 바닥 경고 없이 윈드업+접근 모션으로 읽고 패링.

이상 시: Build Settings의 SoulArena3D, UniversalRP #1, Console 첫 오류.  
노션 기록(한곳): https://app.notion.com/p/414283dd247981f49c61ee058a887059

## 레거시 방침

| 대상 | 방침 |
|------|------|
| MainScene / BeastZombie 2D+바닥 VFX | **레거시 유지** (플레이·회귀용). 개편은 SoulArena → 이후 Main 합류 |
| MotionBossPrototype (스프라이트 임시몸) | 조기 검증용. 본선 아님 |
| pixelfx 스킬/카드 bake | **비핵**. 이 브랜치 미커밋은 본선과 무관 |
| 잡몹 도트/슬라임 | 단순 유지. 인간형만 본 애니 우선 |
| 페이즈2 한 기믹 | 기획 미정 → 구현 금지 |
| 서버/스토어 패치 | 추후 |

## Main 합류 경로 (마감 전 목표 순서)

1. SoulArena3D에서 파이프라인 안정화 (현재 단계)
2. 사용자 야수좀비/Mixamo 최종 아트를 같은 Prefab 계약(`Animator` 상태 Idle/Walk/Slash + `SoulWeaponHitbox3D` on hand)으로 교체
3. MainScene에 **병행 진입** 옵션(로비/디버그 메뉴에서 SoulArena 로드) 추가
4. 기존 BossEncounter를 Soul 보스로 치환 (바닥 텔레그래프 비활성)
5. 뱀서 3분 구간은 기존 스폰·성장을 SoulArena 카메라 규격에 맞게 이식

지금 MainScene을 직접 덮지 않는 이유: 팀 공유·회귀·미커밋 pixelfx와 충돌을 막기 위함.

## 성능·용량 한도 (기획)

- 정밀 부위 콜라이더·무거운 모션: **보스·플레이어**
- 잡몹·다수 적·스킬 VFX: 가볍게 / 공용
- 스킬마다 pixelfx PNG bake 양산: 금지
- 서버: 스토어·패치용 추후. 전투는 로컬
- 임시 휴머노이드(KinemIQ CC0)는 파이프라인용. 최종 아트로 교체 예정

## 기본으로 둔 미정 결정 (번복 가능)

- **첫 휴머노이드**: KinemIQ 마네킹 + Sword_slash (CC0). 팀 Mixamo/야수좀비 오면 교체.
- **씬**: `SoulArena3D` 병행 본선. Main 직접 덮어쓰기 안 함.

다른 선택이 필요하면 Handoff만 고치고 파이프라인 코드는 유지한다.
