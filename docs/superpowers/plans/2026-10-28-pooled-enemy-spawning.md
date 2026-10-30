# Pooled Enemy Spawning Implementation Plan

> **For agentic workers:** Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox syntax for tracking.

**Goal:** GiungTestScene에서 분별 설정과 개발자가 지정한 개수의 스포너로 풀 몬스터를 생성한다.
**Architecture:** EnemySpawner는 한 작업의 n/l/m을 소유한다. EnemySpawnDirector는 분별 작업과 화면 밖 배치를 소유한다. Assembly-CSharp의 SceneEnemyPool이 기존 Managers.Pool과 EnemyMvp.Runtime 사이를 연결한다.
**Tech Stack:** Unity 6000.5.0f1, C#, Unity Test Framework.
**Spec:** docs/superpowers/specs/2026-10-28-pooled-enemy-spawning-design.md (사용자 승인 2026-10-28).

## Global Constraints

- m은 스포너 하나당 총량. 일반 몬스터 동시 130마리.
- 기존 공개 API·직렬화·GUID·사용자 변경 보존. 전역 매니저 추가 없음.
- 보스전은 진행 시간과 일반 스폰만 정지. Time.timeScale 변경 없음.
- 분별 수치·추가 종류는 Inspector 설정. 기존 씬의 1초/5마리를 테스트 초기값으로 보존.
- 현재 feature/poolmanager에서 작업한다. 사용자 Unity에 반영하기 위해 별도 checkout으로 전환하지 않는다.

## Review Focus

- 최초 대여 시 Awake 이전 초기화 및 사망 후 체력·충돌 복구.
- 130마리에서 미생성분 유지와 반납 후 재시도.
- 분 전환 시 이전 할당량 유지 및 한 분에 한 번만 작업 할당.
- 카메라 이동·화면비 변경 뒤 생성 위치와 완료 스포너의 몬스터 수명 분리.
- 보스 시작·종료와 씬 종료 시 풀 객체 누수 및 스포너 재시작 중복.

### Task 1: 풀 연결 및 몬스터 재사용

**Files:** Create IEnemySpawnPool.cs (NormalEnemy), SceneEnemyPool.cs (Game/Spawning); modify EnemyHealth.cs; add PooledEnemySpawningTests.cs in existing PlayMode test assembly.
**Interfaces:** IEnemySpawnPool.TrySpawn(GameObject, Vector3, Transform, Scene): bool; ReturnEnemy(GameObject): void. EnemyHealth.PrepareForSpawn(IEnemySpawnPool): void; Despawn(): void.
- [x] 사망 후 동일 객체 재대여, 체력·추적·충돌·속도 복구와 130 제한 테스트 작성.
- [x] 검증 복사본에서 새 테스트 실패 확인.
- [x] 기존 Managers.Pool.Pop/Push를 연결하고 씬 종료 시 이 소유자의 몬스터만 반납.
- [x] PlayMode 새 테스트 통과 확인.

### Task 2: 배치 스포너와 분별 진행

**Files:** Modify EnemySpawner.cs, BossEncounterController.cs; create EnemySpawnDirector.cs and EnemySpawnMinute.cs in NormalEnemy; extend tests.
**Interfaces:** EnemySpawner.Configure(pool, player, prefabs, interval, total, batch, camera, margin): void; Tick(float): void. Director uses a scaled clock, freezes during BossFight, and schedules idle slots once per minute.
- [x] n초 대기·l마리 배치·m 잔여·완료 비활성화·일시정지·분 경계·화면 밖 배치 테스트 작성 및 실패 확인.
- [x] EnemySpawner의 기존 직렬화 필드와 StopSpawning/RestartSpawning 보존하며 배치 구현.
- [x] 분별 작업 스케줄과 카메라 밖 재배치 구현; 보스전 일반 적 정리를 Despawn으로 연결.
- [x] 전체 PlayMode 실행하고 XML 결과 확인.

### Task 3: 씬 연결과 인수인계

**Files:** GiungTestScene.unity; 새 스크립트 meta; docs/AI/Handoff.md; docs/AI/EnemySpawning.md.
- [x] 기존 스포너를 템플릿으로 연결하고 풀 브리지·디렉터 컴포넌트만 최소 추가. 기존 값 외 정식 밸런스 수치는 넣지 않음.
- [x] 실제 씬 로드 후 스폰·추적·풀 반납·보스 연동 테스트 추가 및 실행.
- [x] diff·직렬화 참조 검토; 실행된 검증 결과와 사용자 확인 순서를 기록.

## Progress and rulings

- 승인한 작업을 이 세션에서 직접 실행한다. 추가 승인·브랜치 전환·커밋·푸시는 하지 않는다.
- 기존 사용자 변경: README.md, docs/AI/UnityProjectContext.md, AGENTS.md, docs/AI/Handoff.md. 보존한다.
- Unity 프로세스가 있어 별도 짧은 임시 검증 복사본을 사용한다. 전체 명령줄과 자격 정보는 기록하지 않는다.

## Final evidence

- 전체 Unity PlayMode 55/55 통과, 실패·생략 0. 기존 36개 + 신규 19개.
- 분 경계 첫 배치, 풀 소유자 비활성화, 이전 lease 추적 간섭, 새 분 화면 밖 재배치 회귀 테스트가 실제 실패한 뒤 수정해 통과했다.
- 독립 리뷰의 P2 2건을 테스트로 재현하고 해결했다. 남은 리뷰 지적 없음.
- 사용자 씬 변경(보스100초·기존 UI 제거 등)을 새 연결 제외 스냅샷 대조로 보존 확인했다.
- Android 빌드·실기기·시각 품질·성능 측정은 미검증이다.
- Ruling: 승인된 구현을 현재 사용자 프로젝트에 직접 반영한다. 별도 worktree 전환과 커밋·푸시는 하지 않는다.
