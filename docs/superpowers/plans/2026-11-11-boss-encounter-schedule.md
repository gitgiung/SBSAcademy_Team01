# Boss encounter schedule Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [x]`) syntax for tracking.

**Goal:** 기존 보스전 흐름을 보존하며 3·6·9·12·15·18분에 지정한 프리팹을 등장시킨다.

**Architecture:** BossEncounterSchedule은 회차별 시각·프리팹·생성 오프셋을 저장한다. BossEncounterController는 Director의 단일 시계를 구독하고 현재 보스의 수명과 HP바를 관리한다. 일정 없는 기존 씬은 단일 보스 경로를 유지한다.

**Tech Stack:** Unity 6000.5.0f1, C#, ScriptableObject, uGUI, Unity Test Framework.

**Spec:** docs/superpowers/specs/2026-11-11-boss-encounter-schedule-design.md (사용자 승인 완료)

## Global Constraints

- Time.timeScale을 보스전 정지에 사용하지 않는다.
- 기존 직렬화 필드와 공개 API를 보존한다. 전역 매니저·asmdef 이동·패키지 변경은 없다.
- 미지정 회차는 경고 후 건너뛴다. 3분 야수만 연결하고 나머지는 비워 둔다.
- 최종 처치의 자동 승리·보상·히든 보스를 추가하지 않는다.
- 원본 씬을 전체 재저장하거나 원본과 같은 프로젝트의 두 번째 Unity Editor를 열지 않는다.

## Review Focus

- 활성 프리팹의 OnEnable이 대상·등장 연출·HP바 연결보다 먼저 실행되지 않도록 비활성 준비 후 활성화한다.
- 1080초 Director 종료 경계에서 마지막 회차도 실행한다.
- 비활성화 중 처치된 보스는 재활성화 때 처리하고 타이머 잠금이 남지 않게 한다.
- 누락/잘못된 프리팹 및 잘못된 시각은 진행 시계를 막지 않는다.
- 이전 보스가 재활성화되어 공용 HP바를 덮어쓰지 않게 연결을 해제한다.

---

### Task 1: 일정 데이터와 다회차 전환

**Files:** Create `Assets/0.Scripts/Game/Encounter/BossEncounterSchedule.cs`; modify `BossEncounterController.cs`; create `Assets/Tests/PlayMode/Encounter/BossEncounterScheduleTests.cs` and test asmdef/meta.

**Interfaces:** Schedule.Entry의 AppearTime, BossPrefab, SpawnOffset; Schedule.Entries; Controller의 기존 ElapsedTime, Phase, DisplayChanged 유지. 직렬화 schedule, bossTarget 추가. EnemyHealth.SetHealthSlider(Slider slider)로 현재 보스 HP 연결.

- [x] 경계 전/도달, 서로 다른 프리팹 6회, 시간 정지, 처치 후 재개, 1080초 등장, 비활성화/재활성화, 미지정/잘못된 설정, 기존 단일 경로의 실제 Unity 회귀 테스트를 먼저 작성한다.
- [x] 짧은 임시 검증 프로젝트에 실제 Combat/Enemy/Boss/Encounter 코드와 테스트를 복사하고 Unity PlayMode를 실행해 기능 부재 실패를 확인한다. 라이선스 등으로 실행 불가하면 실제 소스를 사용하는 .NET 하네스로 대체하고 한계를 기록한다.
- [x] 일정 검증, 다음 경계 예약, 비활성 생성 준비, 처치 구독 교체, 타이머/HP바 연결을 구현한다.
- [x] 같은 검증을 재실행해 통과를 확인한다. 실패 시 원인을 분리해 수정한다.

### Task 2: MainScene 연결

**Files:** Create `Assets/3.Datas/MainBossEncounterSchedule.asset` and meta; modify `Assets/9.Scenes/MainScene.unity` with schedule and target references only.

**Interfaces:** Task 1의 schedule Entry 필드/메타 GUID를 사용. 야수 `Assets/1.Prefabs/BeastZombie.prefab`, 루트 fileID 2240792168150154507, GUID f382559b7ff471e40bfa04d792b55312.

- [x] 6회 시각 180/360/540/720/900/1080, 첫 야수 오프셋 (0,9.26,0), 나머지 프리팹 null을 직렬화한다.
- [x] MainScene의 Boss Encounter에 일정과 기존 player Transform을 연결하고 다른 참조는 유지한다.
- [x] YAML 참조와 diff를 확인하고 Task 1 테스트 및 6개 런타임/Editor 어셈블리 컴파일을 실행한다.

### Task 3: 검토와 인수인계

**Files:** Update design/plan and `docs/AI/Handoff.md`; add `docs/AI/BossEncounterSchedule.md` 사용법.

- [x] 요청 범위, 이벤트 해제, 실패 경로, 기존 씬 호환성을 독립 검토한다.
- [x] 필요한 수정만 반영한 뒤 관련 검증과 git diff --check를 실행한다.
- [x] 실제 실행한 테스트 수·컴파일·미검증 항목 및 사용자의 씬/메뉴/정상 결과/오류 확인 절차를 기록한다.

## 실행 기록

- 시작 HEAD 28102a9, feature/boss-appear-6-times. 기존 문서 변경 2개를 보존한다. 사용자 지침의 임의 브랜치 전환 금지 및 이미 승인된 작업 진행 원칙에 따라 현재 전용 feature 브랜치에서 실행한다.
- 사용자가 구현을 승인했으므로 추가 실행 방식 승인 없이 이 세션에서 직접 구현한다. 커밋·원격 push는 하지 않는다.
- 사전 인터페이스 확인: 일정 소비자는 Controller, HP바 API 소비자는 Controller이며 기존 Enemy/Encounter 어셈블리 참조 방향을 유지한다.

- Task 1: complete — Unity 기능 부재 RED 1/17, 최초 GREEN 17/17, 확장 22/22. 실제 Unity 라이선스 IPC 환경을 해결한 후 분리 프로젝트에서 실행했다.
- Task 2: complete — MainScene 변경 2줄, 6회 일정 에셋과 meta 대조, 최종 6개 어셈블리 Roslyn 컴파일 성공/기존 경고3개.
- Task 3: complete — 독립 코드 리뷰의 P1 과거 경계 정지를 3개 테스트로 재현(22/25)하고 수정 후25/25통과. BossEntrance6/6, PlayerHitFeedback7/7. diff 공백검사 통과.
- Ruling: 비활성 상태에서 여러 회차가 지나면 재활성화와 처치/실패 스킵 뒤 이미 지난 회차를 즉시 순서대로 처리한다. Director가 과거 경계에서 이벤트를 보내지 못해 정지하는 것을 막는다. 400초와1080초 및 프리팹 소실을 실제 Unity로 검증했다.
- Final review: 독립 읽기 전용 검토 완료, 중요1건 수정. Minor 지적 없음. 검토자가 판단하지 않은 실제 화면/Android/카탈로그/번들 검증은 미완료로 인수인계에 명시했다.
- 최종 원본 MainScene 실제 플레이와 전체 프로젝트 Unity 테스트는 실행하지 않았다. 변경은 현재 feature 브랜치 작업 트리에 보존하며 커밋/통합하지 않았다.
