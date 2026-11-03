# Player MP and Action Control Implementation Plan

> **For agentic workers:** Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 플레이어 MP 연산 및 약공격 입력 잠금을 구현하고 PC/조이스틱 경로에서 검증한다.

**Architecture:** PlayerData가 MP를 소유하고 플레이어 소유 PlayerActionGate가 약공격 실행 식별자와 잠금을 관리한다. Player는 입력·회피·패링·자연 회복을 연결하고 PlayerController2D도 동일 잠금을 확인한다. 실제 스킬 버튼·약공격 애니메이션/타격은 다음 단계에서 연결한다.

**Tech Stack:** Unity 6000.5.0f1, 기존 Input System, NUnit PlayMode.

**Spec:** docs/superpowers/specs/2026-11-03-player-skills-design.md 및 사용자 두 번째 단계 진행 승인.

## Global Constraints

- 기존 Bullet 풀링과 사용자 씬 변경 보존. 씬·프리팹·애니메이션·InputAction 에셋·패키지를 재저장하지 않는다.
- MP 최대치·자연회복·회피비용·패링회복은 미정이므로 Inspector 설정 가능하게 두되 초기값 0으로 기능을 임의 활성화하지 않는다. 테스트에서만 예시값을 주입한다.
- 약공격 잠금 중 이동·공격·전략·방어·회피를 거절하며 입력 예약은 하지 않는다. 자동 패시브 발사는 기존 동작을 유지한다.
- 가드 비용 부과 방식, 강공격 환불·그로기·실제 스킬 효과는 이번 범위 밖이다.
- 이전 임시 검증 프로젝트를 재사용하고 새 테스트를 기존 코드에서 먼저 실패시킨다. 원본 Unity를 두 번째로 열지 않는다.

## Review Focus

- 음수/NaN/Infinity 비용은 MP를 늘리거나 오염시키지 않는다.
- MP 부족 및 입력 잠금/회피 쿨타임 거절은 MP를 차감하지 않는다.
- 오래된 애니메이션 종료 식별자가 다음 공격 잠금을 풀지 않는다.
- 조이스틱의 남아 있는 Rigidbody 속도는 잠금 시작 즉시 0이 되고 해제 후 현재 입력으로 복구된다.
- 비활성화·사망은 잠금/진행 코루틴을 정리한다. 일시정지는 MP 회복을 멈춘다.

## Task 1: MP 모델과 행동 게이트

**Files:** Modify Assets/0.Scripts/Game/Player/Data/PlayerData.cs. Create Assets/0.Scripts/Game/Player/PlayerActionGate.cs 및 meta. Test Assets/Tests/PlayMode/PlayerResourceActionTests.cs 및 meta.

**Interfaces:** PlayerData.CurrentMP, InitMP(), bool TrySpendMP(float amount), float RestoreMP(float amount), void TickMPRecovery(float deltaTime), MPChanged(float current,float maximum), MPSpendRejected(float amount). PlayerActionGate(PlayerData data), bool IsInputLocked, bool TryBeginLightAttack(float mpCost,out int actionId), bool CompleteLightAttack(int actionId), void Reset().

- [x] 먼저 MP 경계값·부족·초과회복·자연회복 및 실행 식별자 테스트 작성, 기존 코드에서 실패 확인.
- [x] 기존 HP 및 직렬화 필드를 유지하며 MP 상태/연산/알림 구현. MP 초기화는 Awake에서 한 번 수행한다.
- [x] 행동 게이트는 비용 검사와 입력 잠금을 묶고 동일 실행 ID 종료만 허용한다.

## Task 2: 기존 플레이어·모바일·방어 연결

**Files:** Modify Assets/0.Scripts/Game/Player/Player.cs, PlayerMovement.cs, PlayerDefense.cs, Assets/0.Scripts/Mobile/PlayerController2D.cs. Test asmdef에 Unity.InputSystem 참조 추가.

**Interfaces:** Player.IsInputLocked, CanAcceptGameplayInput, TryBeginLightAttack(float,out int), CompleteLightAttack(int), TryDash(Vector2), TryParry(). PlayerMovement.CanBeginDash(Vector2). PlayerController2D.StopMovement(). PlayerDefense.ParrySucceeded 이벤트는 성공 한 번당 1회.

- [x] PC 이동/회피/패링과 조이스틱 관성, 일시정지·사망·비활성화 테스트를 먼저 작성한다.
- [x] 입력 처리보다 먼저 공통 잠금을 검사한다. 회피 가능 여부 검사 후 MP 차감하고 회피를 시작한다. 자연회복과 패링 보상을 MP 모델로 연결한다.
- [x] 모바일 FixedUpdate도 공통 입력 허용 상태를 확인한다. 잠금 시작 즉시 속도를 정리하고 잠금 종료 후 현재 조이스틱 입력을 읽는다.
- [x] 패링/가드 모션 중 자동 발사를 억제하도록 상태를 제공하고 방어 입력을 발사보다 먼저 처리한다. 기존 회피·방어 API와 동작을 가능한 한 유지한다.
- [x] 실제 약공격 모션/버튼은 구현하지 않는다. 후속 스킬 실행부는 시작 성공 시 받은 actionId를 애니메이션 실행에 묶고 종료 때 같은 ID를 반환해야 한다.

## Task 3: 검증과 전달

- [x] 새 테스트 및 이전 탄환/Core/적 풀링 회귀 실행. 검증 복사본과 최종 소스 해시 일치 확인.
- [x] 독립 코드 리뷰, git diff --check, 사용자 씬 SHA-256 보존 확인.
- [x] Handoff에 실제 결과·미연결 범위·직접 확인 순서를 기록한다. 커밋·브랜치 전환은 하지 않는다.

## 직접 확인

Unity → Window > General > Test Runner → PlayMode → PlayerResourceActionTests → Run Selected. MainTestScene의 PlayerData MP 테스트 설정을 Play 중 지정한 뒤 컴포넌트 메뉴 MP 초기화로 시작값을 적용한다. 약공격 버튼/애니메이션은 다음 단계이므로 이번 단계에서 버튼을 눌러 잠금이 시작된다고 안내하지 않는다. Console 오류 및 테스트 실패 이름, MP 설정과 조이스틱 Player 참조를 확인한다.
