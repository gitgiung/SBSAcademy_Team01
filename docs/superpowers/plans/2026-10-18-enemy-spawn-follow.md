# Enemy Spawn and Follow Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [x]`) syntax for tracking.

**Goal:** 장애물 없는 탑다운 공간에서 지정한 수의 적을 생성하고 플레이어를 직선 추적하게 한다.

**Architecture:** EnemySpawner가 생성 설정과 누적 생성 수를 소유한다. EnemyFollow는 명시적으로 전달된 Transform을 향해 Kinematic Rigidbody2D를 이동한다. 별도 테스트 씬과 프리팹으로 기능을 확인한다.

**Tech Stack:** Unity 6000.5.0f1, C#, Physics2D, Unity Test Framework 1.7.0.

**Spec:** `docs/superpowers/specs/2026-10-18-enemy-spawn-follow-design.md`

## Global Constraints

- 탑다운, 장애물 없는 공간에서 적 생성 및 플레이어 추적만 구현한다.
- 보스, 공격, 체력, 피격, 사망, 길찾기, 오브젝트 풀링은 이번 단계에 포함하지 않는다.
- 기존 팀원 테스트 씬은 수정하지 않는다.
- 플레이어 입력·이동 시스템은 이번 범위에 추가하지 않는다.
- 패키지와 Build Settings는 변경하지 않는다.
- 필수 참조가 잘못됐으면 경고 후 중단하고 반복 예외를 발생시키지 않는다.

## Review Focus

- 생성 지점 목록에 null이 섞이면 해당 슬롯에 스포너 위치를 사용한다.
- 비활성 프리팹에서 생성해도 대상 지정 후 활성화하여 추적할 수 있어야 한다.
- 대상 파괴 또는 비활성화 후 적이 이동을 계속하지 않아야 한다.
- 스포너 재활성화가 생성 한도를 초기화하지 않아야 한다.
- 이동 속도·정지 거리·생성 수가 0이어도 안전하며 간격은 양수로 제한한다.

### Task 1: 플레이어 추적 컴포넌트

**Files:**
- Create: `Assets/0.Scripts/Enemy/NormalEnemy/EnemyFollow.cs` 및 `.meta`.
- Create: 같은 폴더의 `EnemyMvp.Runtime.asmdef` 및 `.meta`.
- Test: `Assets/Tests/PlayMode/EnemyFollowTests.cs`, 테스트 asmdef 및 메타 파일.

**Interfaces:**
- Consumes: UnityEngine.Transform, Rigidbody2D.
- Produces: `EnemyFollow.SetTarget(Transform target): void`.
- Inspector: private serialized `moveSpeed` (기본 2), `stoppingDistance` (기본 0.5).

- [x] 추적, 대상 이동 후 방향 갱신, 정지 거리 유지, 대상 비활성화·파괴 시 정지, 속도 0의 PlayMode 테스트를 먼저 작성한다.
  - 적을 (0,0), 대상을 (3,0)에 놓고 물리 프레임 후 적의 x가 증가하는지 검증한다.
  - 대상을 반대 방향으로 이동하고 적의 이동 방향이 바뀌는지 검증한다.
  - 빠른 이동에서도 대상까지 거리가 정지 거리보다 작아지지 않는지 검증한다.
  - 대상을 비활성화하거나 파괴하고 물리 프레임 후 적 위치가 유지되는지 검증한다.
- [x] 실행 가능한 Unity 테스트 경로를 확인하고 구현 전 실패를 확인한다. 실행이 불가능하면 이유와 미실행 상태를 기록한다.
- [x] RequireComponent로 Rigidbody2D를 보장한다. Awake에서 캐시하고 Kinematic, 중력 0, 회전 고정을 설정한다. FixedUpdate에서 거리 제한을 적용한 MovePosition을 호출한다. 매 프레임 검색하지 않는다.
- [x] 관련 PlayMode 테스트를 실행하고 결과를 확인한다. 테스트를 위한 공개 API는 추가하지 않는다.

### Task 2: 적 생성 컴포넌트

**Files:**
- Create: `Assets/0.Scripts/Enemy/NormalEnemy/EnemySpawner.cs` 및 `.meta`.
- Test: `Assets/Tests/PlayMode/EnemySpawnerTests.cs` 및 `.meta`.

**Interfaces:**
- Consumes: Task 1의 `EnemyFollow.SetTarget(Transform target): void`.
- Produces: Inspector 구성의 EnemySpawner. 외부 명령 API는 불필요하다.
- Inspector: private serialized `enemyPrefab` (GameObject), `player` (Transform), `spawnPoints` (Transform[]), `spawnInterval` (기본 1초), `totalSpawnCount` (기본 5).

- [x] 즉시 첫 생성, 간격 및 총 수 제한, 지점 순환, 빈 목록·null 지점의 위치 대체, 비활성 프리팹, 누락 참조 경고, 비활성화·재활성화 시 한도 유지 테스트를 작성한다. 테스트 구성은 SerializedObject로 설정하고 실제 생성된 오브젝트 수·위치·이동을 검증한다.
- [x] 구현 전 해당 테스트의 실패를 확인한다. 실행 불가 시 미실행으로 기록한다.
- [x] OnEnable/OnDisable에서 생성 루틴을 시작·정리한다. 생성 전 프리팹 루트의 EnemyFollow와 플레이어 참조를 검증한다. 누적 수를 유지하고 코루틴의 중복 실행을 방지한다. 비활성 프리팹의 복제본은 대상 지정 후 활성화한다.
- [x] 생성 테스트와 Task 1 테스트를 함께 실행하여 결과를 확인한다.

### Task 3: 실행 가능한 예제와 최종 검증

**Files:**
- Create: `Assets/1.Prefabs/EnemyMvp.prefab` 및 `.meta`.
- Create: `Assets/9.Scenes/EnemyMvpTestScene.unity` 및 `.meta`.
- Create: `docs/EnemyMvp.md`.
- 필요 시 예제 전용 작은 스프라이트 에셋과 메타 파일을 추가한다.

**Interfaces:**
- Consumes: Task 1과 2의 컴포넌트 및 Inspector 설정.
- Produces: 카메라, 플레이어 표식, 생성 지점 2개, 스포너와 적 프리팹이 연결된 독립 테스트 씬.

- [x] 카메라에서 보이는 영역 안에 플레이어와 생성 지점을 배치한다. 생성된 적이 식별되도록 단순한 스프라이트를 사용한다. 물리·스크립트·프리팹 GUID 참조를 검사한다.
- [x] 사용 문서에 씬 열기, Play, Scene 뷰에서 플레이어 표식 이동, 생성 수·간격·속도·정지 거리 조정 절차를 작성한다.
- [x] Unity 컴파일 및 두 테스트 묶음을 실행한다. 가능한 경우 Play Mode에서 씬 연결과 추적을 확인한다. Editor가 사용할 수 없으면 가능한 컴파일 대안 및 정적 검사를 수행하고 Play Mode 미검증을 명시한다.
- [x] `git diff --check`와 변경 파일 검토를 수행한다. 보스·피격·사망 코드 및 기존 씬 수정이 없는지 확인한다.
- [x] 구현 내용, 검증 결과, 남은 수동 확인 사항을 사용자에게 보고한다. 커밋·PR·새 브랜치는 요청이 있을 때 진행한다.

## Execution choice

권장: 이 세션에서 직접 순서대로 구현. 추적 컴포넌트를 스포너와 예제 씬이 연속해서 사용하므로 작업을 따로 위임할 이점이 작다. 사용자 검토 및 실행 방식 선택 후 구현을 시작한다.

