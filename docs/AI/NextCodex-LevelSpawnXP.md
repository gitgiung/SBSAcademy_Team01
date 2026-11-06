# 다음 Codex 인수인계: 18분 생존·경험치·레벨·스폰

작성일: 2026-11-04 (한국 시간)

## 2026-11-05 진행 갱신

**최신:** 사용자 재개 요청으로 단계3~5(처치 보석 풀/회수, 단일 진행 시간·공유30초 시도 예산·18분 중단, 생성 rawXP·회수 보정/carry·30초 지표)를 구현했다. 색상/기존 카드·보스 정책을 보존했다. 독립28/28, 격리 Unity51/51, 관련6어셈블리컴파일0, 장착4/4. 기존전투표8/17(9실패)은남아있다. `Handoff.md` 첫섹션과 `docs/superpowers/plans/2026-11-05-survival-integration.md`가현재상태다. 다음은원본씬수동확인과실제18분성장/FPS/GC측정이다. 아래미구현표현은과거기록이며완료단계를반복하지않는다.

추가 사용자 요청으로 ExperiencePickup의 경험치량별 tint/Inspector 색상표도 구현했다. RawExperience/SetExperienceAmount(float), 풀 초기화와 미리보기 메뉴를 사용한다. 현재 일반 보석은 생성 시각별2/3/4/6/7/9XP를 받으며 보스의 기존 프리팹1XP는 유지한다. 색은rawXP기준이고회수보정으로바뀌지않는다.

사용자 승인으로 **1단계 레벨·XP 계산과 2단계 카드 자동 연결** 및 스탯 우측 검정색 레벨 표시도 이미 구현했다. 당시 독립17/17·Unity15/15는과거기록이며현재검증은위최신항목을참조한다. 아래본문은11월4일조사시점기록이다.

## 현재 요청과 작업 범위

사용자는 `docs/VampireSurvivor_18min_LevelSpawnXP_Codex_Spec_v0.1.md`를 읽어 달라고 했고, 시간이 부족해 이번에는 **다음 Codex에게 넘길 문서를 작성**하도록 요청했다. 명세를 읽고 기존 연결 지점을 일부 조사했으며, 이 명세의 런타임 구현은 아직 시작하지 않았다. 이 문서는 구현 완료 보고가 아니다.

다음 세션에서 사용자가 이어서 진행하라고 하면 아래 순서로 시작한다. 이미 완료한 카드 디자인·추첨·획득·장착을 처음부터 다시 만들지 않는다.

## 먼저 읽을 자료와 실제 상태

1. 루트 `AGENTS.md`, `docs/AI/Handoff.md`.
2. 원본 명세: [VampireSurvivor_18min_LevelSpawnXP_Codex_Spec_v0.1.md](../VampireSurvivor_18min_LevelSpawnXP_Codex_Spec_v0.1.md).
3. `docs/AI/CoreInitialization.md`, `docs/AI/ResourceLoading.md`.
4. 기존 카드 계획: `docs/superpowers/plans/2026-11-04-level-up-panel.md`, `2026-11-04-weighted-level-up-skills.md`, `2026-11-04-level-up-acquisition.md`.

확인 당시 브랜치 `feature/Player-Growth-Design`, HEAD `521ef10` (`레벨 업 패널에서 스킬 선택, 획득`). 직전 관련 커밋은 `3c1e8e7`, `b55f733`이다. 다음 세션에서는 반드시 `git status --short`, `git branch --show-current`, `git log -5 --oneline`으로 다시 확인한다.

문서 작업 시작 시 미추적 파일은 다음 두 개였다. 사용자가 첨부한 파일이므로 보존하고, 자동 삭제·덮어쓰기하지 않는다.

- `docs/VampireSurvivor_18min_LevelSpawnXP_Codex_Spec_v0.1.md`
- 루트 `VampireSurvivor_18min_LevelSpawnXP_ImplementationSpec_v0.1.docx`

이번에는 docs의 Markdown 명세를 전부 읽었다. 루트 DOCX 원문을 별도로 비교하지는 않았다. 아래 요약보다 원본 Markdown의 상세 조건을 우선 읽고, 두 원본이 다르면 차이를 사용자에게 알린다. 커밋·PR은 요청받지 않았으며 수행하지 않았다.

## 사용자가 이미 결정한 동작

- 한국어로 설명하고 자원 명칭은 MP로 통일한다.
- 현재 카드 추첨/실행 대상은 **약공격만**이다. 강공격은 나중에 추가하되 강공격 버튼에만 연결한다.
- 카드 선택 시 약공격 보유 목록에 등록하고 다음 추첨에서 보유 스킬을 제외한다. 자동 장착하지 않는다. 일시정지 창에서 직접 장착/해제한다.
- 후보가 1~2개면 있는 만큼 표시한다. 후보 0개는 제목 **`카드 없음`**, 내용 **`카드가 소진되었습니다.`**, 확인으로 종료한다. 실제 수치 대체 보상은 아직 없다.
- 보유 공격 슬롯은 기본 6칸, 최대 8칸이다. **가득 차면 가득 참 안내 후 확인으로 종료**한다. 스킬 교체를 임의로 추가하지 않는다.
- 보유에서 제거된 스킬은 다시 후보가 될 수 있다. 영구 획득 이력은 없다.
- 18분 명세의 밸런스는 **1차 플레이테스트용 임시 설정값**이다. 최종 확정값으로 설명하지 않는다.

## 원본 명세의 핵심 계약

- 게임 진행 시간 1,080초. 일시정지·레벨업 선택 중에는 진행하지 않는다. 18분에는 일반 스폰만 중단하며 새 승패/보스 전환을 설계하지 않는다.
- 초기 레벨 1, 최대 100. 레벨 L의 요구 XP는 `(108000 + 19800*L + 675*L*L + 5000) / 10000` 정수 나눗셈이다. L<1 또는 L>=100이면 0. 초과 XP를 차감/이월하고 대량 획득 시 중간 레벨을 빠뜨리지 않는다.
- 30초 × 36구간, index 0~35. 구간 목표 시도 수 `50 + 10*index`. 130마리 제한이나 풀 실패는 성공 수와 분리한다. 막힌 시도를 나중에 보충하는 무한 큐를 만들지 않는다. 프레임 지연 뒤 폭발 생성도 제한한다.
- 동시 활성 **일반 몬스터** 최대 130. 비활성 풀 보관 한도와 다르다. 보스/특수 소환에는 일반 상한을 임의로 적용하지 않는다.
- 생성 시각 기준 3분 단위 처치 rawXP는 `2/3/4/6/7/9`. 생성 때 고정해 경계 이후 처치해도 유지한다. 실제 처치 때만 한 번 드롭하고, 거리 반환/씬 정리는 처치로 간주하지 않는다.
- XP 보석은 rawXP를 보관하고 실제 회수 때만 XP에 반영한다. 기존 Managers.Pool을 이용한다. 중복 죽음·Trigger·반환을 막고 풀 재대여 시 획득 상태를 초기화한다.
- 보정 체크포인트 3/6/9/12/15분의 목표 레벨은 `19/35/51/70/87`. 차이>=3이면 ON, <=1이면 OFF, 차이2면 유지. 켜진 동안 획득 XP ×1.25, 최대 레벨에서 종료한다.
- 보정은 **회수 시점**에 적용한다. 이미 떨어진 보석의 rawXP는 바꾸지 않는다. 소수 잔여를 보존한다(2 XP 두 번 ×1.25 = 합계5). 보정 해제 때도 잔여를 버리지 않는다.
- 처치율85%·회수율90%는 시뮬레이션 가정이다. 실제 적을 15% 제거하거나 보석을 10% 소멸시키는 기능을 구현하지 않는다.
- 설정과 런타임 상태를 분리한다. 새 전역 매니저/풀/이벤트 프레임워크를 만들지 않는다. 몬스터 HP/DPS·보스 설계·100레벨 이후 보상·추가 XP 아이템·저장 확장·UI 재디자인은 범위 밖이다.

명세 검산은 PowerShell 정수 공식/합산으로 실행했다: 요구 XP 총합 **33,033**, 스폰 목표 총합 **8,100**, 시뮬레이션 획득 XP **38,762.55** 일치. 게임이 실제로 해당 성장을 달성했다는 검증은 아니다.

## 현재 코드에서 확인한 연결 지점

| 경로 | 현재 역할 / 필요한 확장 |
| --- | --- |
| `Assets/0.Scripts/Game/Player/Data/PlayerData.cs` | `CurrentEXP`, `maxEXP`, `EXPChanged`, `SetEXP`, `AddEXP`가 있다. 현재는 EXP 값 누적/표시만 하며 레벨·요구량 차감·다중 레벨업은 없다. 기존 API/직렬화 참조를 보존한다. |
| `Assets/0.Scripts/Game/Player/PlayerView.cs` | EXPChanged를 구독해 EXP Slider를 갱신한다. 새 HUD를 중복 제작하지 않는다. |
| `Assets/0.Scripts/Game/Player/ExperiencePickup.cs` | 기존 보석. Trigger에서 `PlayerData.AddEXP(experienceAmount)` 후 Destroy. 끌어당김/획득 범위가 있다. rawXP 주입·보정·풀 반환·재사용 초기화가 필요하다. |
| `Assets/0.Scripts/Game/Enemy/NormalEnemy/EnemyHealth.cs` | `Die`에서 보석 생성→Died→finally Despawn. 보석은 현재 Instantiate. 실제 죽음과 Despawn을 구분하고 생성 시 XP 스냅샷을 연결해야 한다. |
| `Assets/0.Scripts/Game/Enemy/NormalEnemy/IExperiencePickup.cs` | `Initialize(Transform target)` 인터페이스. EnemyMvp.Runtime 쪽 경계이므로 XP 인자를 추가할 때 기존 구현/어셈블리 참조를 먼저 검색하고 호환을 보존한다. |
| `Assets/0.Scripts/Game/Spawning/SceneEnemyPool.cs` | 기존 Managers.Pool을 사용하며 130 상한과 lease 기반 반환을 구현했다. ActiveEnemyCount 조회 때 활성 Dictionary 정리 순회가 있다. 이벤트 기반 정산을 보완하되 풀과 카운터를 중복 만들지 않는다. |
| `Assets/0.Scripts/Game/Enemy/NormalEnemy/EnemySpawnDirector.cs` | `ElapsedTime`, 분별 EnemySpawnMinute[], spawnerCount. 현재 60초 설정을 마지막 값으로 계속 유지한다. 30초/36구간·목표 시도 수·18분 중단으로 확장 필요. |
| `Assets/0.Scripts/Game/Enemy/NormalEnemy/EnemySpawner.cs` | 기존 화면 밖 배치 스폰. 지금은 생성 성공 수를 total과 비교하고 프레임당 한 배치, 간격 누산을 0으로 초기화한다. 명세의 ‘시도 예산’과 구간 경계를 그대로 충족한다고 가정하지 않는다. |
| `Assets/0.Scripts/Game/Encounter/BossEncounterController.cs` | 별도 ElapsedTime이 있고 보스전 중 진행 시간/일반 스폰 정지, 보스 사망 후 재개한다. 보스 진입 시 일반 적 정리도 한다. 이 반환으로 XP가 드롭되면 안 된다. |
| `Assets/0.Scripts/Game/Encounter/EncounterTimerView.cs` | 보스 진행 담당 시간 표시. 단일 시계 통합 시 표시 참조도 함께 검토한다. |
| `Assets/0.Scripts/Manager/GameManager.cs` | Playing/Victory/Defeat와 결과 화면/씬 전환 소유. 18분 도달을 임의로 승리 처리하지 않는다. 실제 경로는 Framework/Managers가 아니다. |
| `Assets/0.Scripts/Game/UI/CombatPopupController.cs` | `ShowLevelUp`이 현재 보유 제외 약공격을 추첨하고 선택→등록→`CompleteLevelUp`한다. 소진/가득 참 안내도 완료 경로를 사용한다. 팝업 수명에 따른 timeScale 정지/복원을 소유한다. |
| `Assets/0.Scripts/Game/UI/LevelUpSelectionPanel.cs` | 3장 카드·상세 설명·선택 한 번 가드·ShowMessage 확인 처리. 완료 이벤트를 성장 진행과 연결할 때 기존 가드를 보존한다. |
| `Assets/0.Scripts/Game/UI/LevelUpSkillDraft.cs`, `LevelUpSkillReward.cs` | 등급 가중 추첨/소유 제외, 보유 메타데이터 등록. 미정 패시브 수치를 생성하지 않는다. |
| `Assets/0.Scripts/Game/Player/PlayerLoadout.cs` | 보유·장착·빈 슬롯·중복 거절과 Changed 알림. 실제 약공격은 장착 ID의 전투 표 정의로 시전한다. |
| `Assets/0.Scripts/Framework/Data/CombatSkillTable.cs` 및 `Framework/Editor/CombatSkillTableExporter.cs` | 기존 SkillDatatable.xlsx → JSON. Skill_Rarity 소수 가중치/색 포함. EXP 설정을 기존 스킬 시트의 임의 칸에 넣지 않는다. |

### 구현 전 특히 확인할 연결 문제

1. **단일 진행 시간:** 지금 보스 진행과 스폰 담당이 별도 시계를 갖고 있다. 실제 씬 연결과 정지 의미를 읽고 하나의 시간 권위를 정한다. 보스전에는 진행 타이머가 정지하지만 전투 전체를 timeScale=0으로 멈추지 않는다. 보스 규칙 변경이 필요하다면 사용자에게 구체적으로 확인한다.
2. **다중 레벨업과 카드 완료:** 지금 `CompleteLevelUp()`은 창을 닫을 뿐 다음 대기 레벨업을 알리는 계약이 없다. 대량 XP로 여러 레벨을 올랐을 때 선택을 누락하지 않도록 플레이어/진행 담당이 대기 선택을 관리하는 작은 연결이 필요하다. 소진/가득 참의 ‘확인’도 한 번의 완료로 처리한다. 새 이벤트 버스나 팝업 스택을 만들지 않는다.
3. **어셈블리 경계:** EnemyMvp.Runtime·Boss.Runtime·Encounter.Runtime이 있다. Player/UI를 테스트 목적으로 새 asmdef로 옮기거나 순환 참조를 만들지 않는다.
4. **설정 vs 상태:** 레벨/잔여 XP/소수 carry/활성 수는 런타임 상태다. ScriptableObject 설정 에셋에 저장하지 않는다. 기존 Inspector의 EXP 설정과 호환/초기화 정책을 먼저 확인한다.

## 권장 구현 순서

각 단계마다 실제 코드 확인→수치/회귀 검증→최소 구현→컴파일→인수인계 갱신으로 진행한다. 아래는 실행 순서 제안이며 아직 구현하지 않았다.

1. **레벨·XP 계산 기반:** 조정 가능한 설정과 순수 계산/상태 로직으로 정수 요구량·레벨상한·초과분·다중 레벨업을 검증한다. 기존 PlayerData.AddEXP/SetEXP·EXPChanged·Slider와 연결한다.
2. **레벨업 카드 자동 연결:** 레벨업 발생과 기존 ShowLevelUp/CompleteLevelUp을 연결한다. 대량 XP 시 선택을 순서대로 처리하고, 마지막 선택 전 전투가 재개되거나 입력이 새지 않는지 검증한다. 사망/씬 종료 정리도 확인한다.
3. **처치→보석→회수:** 기존 EnemyHealth/ExperiencePickup을 풀 경로로 바꾸고 생성 시 rawXP 스냅샷과 회수 한 번 처리를 연결한다. 기존 보석 이동/획득 범위는 보존한다.
4. **시간·스폰 통합:** 실제 보스 정지 정책을 보존하면서 단일 진행 시간을 연결한다. 기존 스포너를 30초 구간 시도 예산/18분 중단/130 상한/실패 집계에 맞게 확장한다.
5. **성장 보정·관측:** 체크포인트 히스테리시스, 25% 보정과 소수 carry, 30초 지표를 추가한다. 설정으로 조정 가능하게 하고 Release 로그 기본 비활성 방식을 따른다.
6. **통합 플레이테스트:** 130 상한 빈도→처치율/회수율→체크포인트 레벨→100레벨 시각→FPS/GC 순으로 확인한다. 실제 결과 전 16~17분 달성을 보장하지 않는다.

## 검증 기준과 환경 주의사항

- 핵심 수치: 요구 XP 합33,033, 구간0/35 목표50/400, 목표 총8,100, 29.999/30초 경계, 179.999/180초 XP 경계, 1,080초 일반 스폰 중단.
- 상태: 대량 XP의 모든 레벨 통지, Lv100 상한, 카드 연속 완료, 소진/가득 참 확인, 사망/씬 전환 중 정리, 정지 중 진행 시간 불변.
- 풀: 처치만 드롭, 거리 반환/씬 정리 무드롭, 중복 사망/Trigger/반환 무효, 재대여 초기화, 최고 활성 수130 이하.
- 보정: 차이3 ON/1 OFF/2 유지, 2 XP 두 번×1.25=5, 해제 후 carry 보존, 보석 rawXP 불변.
- 기존 독립 검증 실행 명령: `& tools/CombatSkillTableValidation/Run.ps1`, `& tools/PlayerLoadoutValidation/Run.ps1`.
- **과거 결과:** 직전 구현 세션에서 위 검증은 17/17 및 4/4, Unity 설치본 Roslyn 런타임/에디터 컴파일 종료0이었다. 이번 문서 작업에서는 재실행하지 않았다. 다음 변경의 검증 결과로 재사용하지 않는다.
- Unity 버전 파일은 `6000.5.0f1`. Assets/Tests는 사용자가 삭제를 요청해 제거된 상태다. 테스트만을 위해 무조건 복원하거나 런타임 어셈블리를 재구성하지 않는다. Assets 밖 독립 수치 검증 패턴을 우선 참고하되 Unity 수명/물리/UI 검증을 대신한다고 주장하지 않는다.
- 사용자가 열어 둔 같은 프로젝트를 두 번째 Unity로 열지 않는다. 열린 에디터 또는 필요 시 짧은 임시 경로 검증 복사본을 사용한다. 긴 경로 및 Play From Title의 테스트 실행 간섭은 Handoff 참조.
- Roslyn 대체 검증 시 출력은 격리하고 Library DLL을 덮어쓰지 않는다. `.utmp`와 응답 파일은 생성물이므로 다음 세션에서 존재를 가정하지 않는다. 실행 명령/실제 결과를 영속 문서에 요약한다.
- 씬·프리팹·GUID·사용자 변경을 보존한다. 필요한 최소 씬 연결만 수정하며 자동 재저장하지 않는다. 전체 Unity 실행 명령줄/자격증명을 출력하지 않는다.

## 사용자가 직접 확인할 순서

현재 완료된 기능 확인은 MainTestScene 또는 GiungTestScene → Play From Title 끄기 → Play → CombatUI 선택 → CombatPopupController 메뉴 `테스트/레벨업 팝업 열기` → 카드 선택 → 일시정지 보유 등록 → 클릭 장착 → N_Attack/Enter 발동 순이다. `테스트/카드 소진 안내`로 안내/확인 복귀를 확인할 수 있다. 등록을 반복해 기본6칸을 채우면 가득 참 안내가 떠야 한다. 실제 클릭/정지복원은 아직 미검증이다.

새 성장 구현 후에는 실제 적 처치→보석 회수→EXP 변화→레벨업 카드 자동 열기→선택→전투 재개를 확인하고, 대량 XP로 연속 레벨업도 확인한다. 오류 시 Console 첫 오류 전체, PlayerData/EXP Slider/Status Player 참조, 보석 프리팹 연결, 진행 시계/스폰 설정을 확인한다. 실제 카탈로그·Android 빌드·기기는 실행한 경우에만 검증 완료로 보고한다.

## 다음 채팅에 붙여 넣을 시작 문구

> docs/AI/NextCodex-LevelSpawnXP.md와 docs/AI/Handoff.md, docs/VampireSurvivor_18min_LevelSpawnXP_Codex_Spec_v0.1.md를 읽고 이어서 진행해줘. 현재 코드/작업 트리를 확인한 뒤 레벨·경험치 계산과 기존 레벨업 카드 자동 연결부터 단계별로 구현해줘. 기존 카드 선택·획득·장착을 다시 만들지 말고 사용자 변경을 보존해줘.
