# Enemy Stage Mixing Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [x]`) syntax for tracking.

**Goal:** 24개 단계 프리팹을 자동 연결하고 30초마다 계산한 비율로 3분 경계 이후 서서히 교체한다.

**Architecture:** EnemySpawnProgression은 직렬화된 단계·종류 설정과 가중치 계산을 소유한다. EnemySpawnDirector는 기존 가중 스케줄러에 이를 연결한다. Editor Inspector는 자동 등록과 비율 미리보기를 제공한다.

**Tech Stack:** Unity 6000.5.0f1, C#, 기존 Enemy.Runtime, UnityEditor, .NET 검증 도구.

**Spec:** docs/superpowers/specs/2026-11-14-enemy-stage-mixing-design.md

## Global Constraints

- `_3min`=0–3분용, 3분 경계 이전에 다음 단계 등장 금지.
- 전환 90초, 종류별 초반/후반 기본 가중치 1. 모두 사용자 승인 초기값이며 조절 가능.
- 수동 waves·기존 공개 API·GUID·씬 Overrides·몬스터 수치 보존.
- 스폰 예산·130 활성 제한·18분 종료·XP·보스/전투 일시정지 계약 유지.
- 사용자 지침에 따라 브랜치를 전환하지 않고 현재 체크아웃에서 작업한다. 새 프로젝트를 원본 경로에서 두 번째 Unity로 열지 않는다.

## Review Focus

- 큰 Tick이 여러 웨이브를 건너도 시간별 구성은 올바르게 적용한다.
- 종류 가중치가 모두 0인 끝점은 설정 오류로 거절한다.
- 0초/180초 전환 및 마지막 구간에서도 미래 단계가 나오지 않는다.
- 자동 등록 실패는 기존 직렬화 설정을 변경하지 않는다.
- MainScene의 기존 waves Override가 단계 혼합 모드를 방해하지 않는다.

### Task 1: 계산과 Director 연결

**Files:** 새 EnemySpawnProgression.cs; 기존 EnemySpawnDirector.cs; tools/EnemyStageMixingValidation/{Program.cs,Boundary.cs,Validation.csproj,Run.ps1}; Assets/Tests/EnemyStageMixing/Editor/EnemyStageMixingTests.cs.

**Interfaces:** EnemySpawnProgression.IsValid, BuildRoster(double elapsed, double duration, out GameObject[] prefabs, out int[] weights); Director의 rosterMode(0=수동,1=혼합), stageMixing.

- [x] 시간 0/179.999/180/210/240/270/360/900/990/1080의 실제 선택 비율, 종류 초반/후반 보간, 0 가중치/누락/설정 오류 테스트를 먼저 작성하고 실패를 확인한다.
- [x] 직렬화 계산 클래스를 구현한다. 180초 단계, 0–180초 전환, 종류별 0–100 상대 가중치. 최소 정밀도 1/1000 가중치 단위로 기존 int 스케줄러에 전달한다.
- [x] Director에 수동/혼합 모드를 연결하고 waveSeconds 기준 경계 처리·가중 스케줄러를 재사용한다.
- [x] 실제 Director를 호출하는 검증에서 혼합·pause·cap/pool 실패·수동 회귀를 실행한다.

### Task 2: 편집·에셋 연결

**Files:** Assets/Editor/EnemySpawnDirectorEditor.cs 및 meta, Enemy Spawn System.prefab, 위 검증 도구.

**Interfaces:** EnemyStageRosterImporter.TryLoad(folder, out GameObject[][] prefabs, out string error); SerializedObject로 profile 연결, Undo 및 원자적 적용.

- [x] 실제 24개 이름/GUID·6단계 슬롯·필수 컴포넌트·중복/누락 거절을 검사한다.
- [x] 기본 Temp_enemy 폴더 자동 등록 버튼, 모드별 Inspector, 30초 웨이브 비율 미리보기를 추가한다. Play 중 편집은 막는다.
- [x] 스폰 시스템 프리팹의 Director에 혼합 모드와 24개 참조를 최소 추가한다. MainScene의 기존 수동 Override와 몬스터 프리팹은 보존한다.
- [x] Unity Roslyn으로 실제 엔진·Editor 참조를 사용해 컴파일한다. 필요한 새 소스를 응답 캐시에 보충하며 Library를 덮어쓰지 않는다.

### Task 3: 검토·인수인계

- [x] 관련 검증 전체 실행, 실제 실행 수/제한 기록, diff --check와 새 GUID 중복 검사.
- [x] 별도 검토에서 설계와 실제 동작·가중치 정밀도·씬 상속·Undo 경계를 확인한다.
- [x] Handoff에 완료 내용·현재 상태·다음 작업·실제 검증 수와 사용자 확인 순서를 갱신한다.

## 실행 기록

- 승인된 설계와 작업 승인을 근거로 추가 승인 대기 없이 같은 세션에서 직접 실행한다.
- 사용자 지침(임의 브랜치 전환 금지)을 우선하여 main 체크아웃을 유지한다. 자동 커밋/병합/푸시는 하지 않는다.
- 시작 상태: main / 2bbb71e, 이전 턴에서 작성한 설계와 Handoff 문서만 변경됨.
- Task 1 complete: .NET 검증 23/23. 최초22개 기능 부재 실패 + 수동 기준1개 통과 → 구현 후23개 통과.
- Task 2 complete: 프리팹 연결/에셋 검사6/6, 실제 Unity Roslyn 어셈블리6개 오류0/경고0. 씬과 몬스터 프리팹은 변경하지 않음.
- Task 3 complete: 격리 Unity EditMode21/21, PlayMode0. 별도 읽기 전용 리뷰에서 런타임 차단 지적 없음. 테스트 폴더 소유권 위험은 회귀 테스트 RED(20/21) → 고유 폴더/소유권 수정 → GREEN(21/21). 미검증은 실제 MainScene Play·밸런스·Android/기기.
- 테스트는 자산 임포트/Undo를 포함하므로 PlayMode 대신 Editor 전용 테스트 어셈블리를 사용했다. 실제 게임 스포너는 그대로 실행하고 풀은 기록용 경계를 사용한다.
- 초기 샌드박스 Unity 라이선스 실패는 환경 문제로 분리했다. 해당 격리 프로세스만 정리한 뒤 정상 라이선스 연결 환경에서 실제21개가 통과했다.
- 사용법/최종 검증/다음 작업은 docs/AI/EnemySpawning.md 및 docs/AI/Handoff.md에 기록했다. 생성물은 커밋 대상에 넣지 않았다.
