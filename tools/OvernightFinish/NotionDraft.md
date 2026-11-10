# Soul Pipeline 본선 착수 (2026-11-11)

## 요약
모바일 소울 공방 본선을 위해 **평탄 3D 맵 + 휴머노이드 본 애니 + 접근형 공격(손 히트박스)** 파이프라인을 프로젝트에 심음.
검증 씬은 `SoulArena3D`(병행). `MainScene`/야수좀비 2D는 레거시로 유지 후 단계 합류.

## 브랜치
- `feature/soul-pipeline-3d`
- (커밋 해시·푸시 결과는 08:00 마무리 시 갱신)

## Play 확인
1. Play From Title 끄기
2. `Tools > Soul Pipeline > 1 → 2 → 3` (또는 Open SoulArena3D)
3. Play: WASD / E 패링
4. 합격: 바닥 경고 없이 다가오며 슬래시 모션으로 읽고 반응

Prefab 조립 전이면 Bootstrap이 **3D 폴백 몸**으로도 Play 가능.

## 레거시
| 유지 | 방침 |
|------|------|
| MainScene, BeastZombie 2D+바닥 VFX | 회귀용 레거시 |
| MotionBossPrototype 스프라이트 토이 | 조기 검증용, 본선 아님 |
| pixelfx bake | 비핵 |
| 페이즈2 한 기믹 | 기획 미정 → 미구현 |
| 서버 | 스토어/패치용 추후 |

## 성능
정밀 판정=보스·플레이어, 잡몹 단순, pixelfx 양산 금지.

## 마감 전 남은 일
1. SoulArena Play 안정화(Humanoid Prefab)
2. Mixamo/야수좀비 아트 교체
3. 로비→SoulArena 진입 UX
4. BossEncounter 치환
5. 기믹 기획 후 구현

## 문서
- `docs/AI/SoulPipeline-TeamGuide.md`
- `docs/AI/Handoff.md` 최상단
- `docs/superpowers/specs/2026-11-11-motion-readable-boss-pipeline.md`
