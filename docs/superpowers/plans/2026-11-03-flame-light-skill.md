# Flame Bullet Light Skill Implementation Plan

> **For agentic workers:** Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 불꽃 탄환10001을 보유·장착하고 N_Attack/기존 Attack 입력으로 MP10·피해10의 빨간 직선탄과 약공격 애니메이션을 실행한다.

**Architecture:** 기존 Data.SkillData에 명시적 액티브 설정 여부·MP비용·기본 피해를 추가한다. 플레이어 소유 PlayerLightSkill이 장착 ID/설정을 검증한 뒤 기존 행동 게이트로 MP와 잠금을 처리한다. PlayerView와 LightAttackAnimationState가 Animator의 시작/진행/종료를 실행 ID에 연결하고 기존 PoolManager/Bullet을 재사용한다.

**Tech Stack:** Unity6000.5.0f1, 기존 Input System/uGUI/Animator/PoolManager, PlayMode tests.

## Global Constraints

- 사용자 승인: 불꽃 탄환10001, MP10, 피해10, 빨간 원, 기존 Bullet 재사용, 기존8방향 Attack 프레임. 모바일 버튼은 N_Attack. 사용자 요청으로 병합 전체 선행 재검증은 생략하지만 변경 대상 컴파일/회귀는 수행한다.
- 현재 feat/PlayerSkill, HEAD ccac76f. 브랜치 전환/원본 두 번째 Unity 금지. 씬과 Animator 원본 시작 해시 보존 후 필요한 범위만 수정한다.
- 기존 Animator를 삭제/재생성하지 않는다. 새8방향 LightAttack 클립·상태·종료 동작만 추가한다. 기본 탄환/패링/회피/이동 기존 상태 유지.
- 입력은 애니메이션 동안 이동/공격/방어/회피 모두 차단한다. 누적 예약 입력 없이 종료 후 새 입력만 받는다. 기본 자동 패시브 발사 규칙은 유지한다.
- 첫 스킬은 즉발 사양에 따라 애니메이션 시작 시 발사한다. 9프레임 Attack은 기존 플레이어 연출과 같은12fps를 초기 연출값으로 사용하고 편집 가능하게 둔다. 스킬 별도 쿨타임 없음.
- 비용/리소스/장착/애니메이션이 불완전하면 무소모·미발동. 메타데이터만 있는 다른 스킬을0비용으로 실행하지 않는다.
- MainTestScene에는 승인된10001 보유/약공격 장착과 N_Attack·새 투사체 참조만 최소 연결한다. 기존 MP설정(maxMP100)·스탯·씬 사용자 변경 유지. 실제 획득/다른 스킬·강공격·전략·그로기는 범위 밖이다.

## Review Focus

- MP10은 성공한 시전마다 한 번만 차감하고 미설정/부족/잠금/일시정지 입력은 무소모로 거절한다.
- 모바일 PointerDown과 PC Attack이 같은 프레임 들어와도 한 번만 발사한다. UI 위 일반 Mouse/Touch 액션은 중복 처리하지 않는다.
- 이전 애니메이션 종료 ID가 새 동작을 풀지 않는다. 사망/비활성화/Animator 중단은 잠금을 정리한다.
- 빨간 탄환은 방향 스냅샷으로 직진하고 기존 풀의 적중/수명/화면 이탈 반환 계약을 유지한다.
- 생성 작업은 기존 asset GUID를 보존하며 새 리소스만 추가한다.

## Task 1: 실행/입력/수명

**Files:** Data.Contents.cs, Player.cs, PlayerView.cs, PlayerActionGate.cs, Bullet.cs. New PlayerLightSkill.cs, LightAttackAnimationState.cs, PlayerLightAttackButton.cs 및 meta.

**Interfaces:** Player.TryUseLightSkill(), ActiveLightAttackId, NotifyLightAttackAnimationProgress(int,float), NotifyLightAttackAnimationFinished(int). Bullet.InitDirection(Vector2,float,float). Data.SkillData.ActiveConfigured/MPCost/BaseDamage. 기존 Init·TryBeginLightAttack·CompleteLightAttack 유지.

- [x] 무장착/불완전설정/MP부족 무소모 및 유효 시전 MP10·입력 잠금 테스트 먼저 실패 확인.
- [x] 방향과 피해를 시전 시작에 캡처하고 기존 풀에서 한 번 발사한다. 상태 종료/취소에 같은 실행 ID로 잠금을 정리한다.
- [x] 기존 Player/Attack을 선택적으로 복제하고 UI 포인터 입력과 중복을 방지한다. N_Attack은 PointerDown/Submit을 공통 API에 연결해 잠금 중 클릭을 예약하지 않는다.

## Task 2: 최소 리소스/씬 연결

**Files:** 새 Assets/Editor/PlayerLightAttackSetup.cs, Assets/Resources/PlayerAnimator/LightAttack_*.anim, 기존 Player.controller의 추가 상태, 새 Assets/Resources/SkillProjectiles/FlameBullet 리소스. MainTestScene 참조/첫 보유 스킬. 기존 PlayerAnimationSetup의 명시적 Generate 동작에도 새 상태 추가를 유지한다.

- [x] 검증 복사본 Editor API로 새 빨간 원 Sprite/기존 Bullet 복제 프리팹과 Attack 클립/상태를 생성한다. 원본 Bullet 프리팹은 수정하지 않는다.
- [x] 생성 Controller diff에서 기존 상태/Clip/GUID 유지 확인 후 원본으로 반영한다. 원본이 시작 해시와 다르면 덮어쓰지 않는다.
- [x] N_Attack Button1415531113과 새 투사체를 Player에 연결하고 빈 보유 첫 칸만10001 설정/약공격 장착한다.

## Task 3: 검증/인수인계

- [x] 실제 Animator 종료·MP1회 차감·N_Attack·부족/중복·정지·사망·중단·빨간 원/풀링 적중10 테스트.
- [x] 관련 MP/HUD/탄환/상태창 회귀 및 독립 리뷰. 사용자 요청에 따라 전체 병합/기기 빌드 검증은 별도다.
- [x] diff check, 소스/생성 리소스 검증 복사본 일치, Handoff 갱신 및 직접 확인 안내.
