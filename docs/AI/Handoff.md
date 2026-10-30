# 작업 인수인계 — 새 Codex가 가장 먼저 읽을 문서

최종 정리: **2026-10-30, Asia/Seoul**. 날짜가 지난 뒤에는 아래 사실을 코드·Git·설정과 다시 대조한다.

## 최신 완료 — 플레이어 중앙 고정 카메라

- 사용자 요청: 기존 CameraPlayer의 Update 추적을 폐기하고 Cinemachine으로 교체. 후속 답변에서 **화면 중앙 고정**을 선택했다.
- **맵 경계 제한과 화면 흔들림은 추후 추가 구현 범위로 보존한다.** 현재 구현 범위에 포함하지 않는다. 경계 형상·흔들림 수치·발생 조건은 미정이다.
- 사용자 진행 승인 후 Cinemachine 3.1.7을 추가하고 MainTestScene에 Main Camera의 Brain 및 별도 `Player Follow Camera`(CinemachineCamera + Position Composer)를 구성했다. 두 갱신 방식은 LateUpdate, damping/dead zone/lookahead는 0이다. 직교 크기 10과 거리 10, 고정 회전을 유지한다. 구 CameraPlayer.cs/meta는 참조가 이 씬뿐임을 확인하고 제거했다.
- 설계: `docs/superpowers/specs/2026-10-30-player-camera-design.md`. 구현 계획: `docs/superpowers/plans/2026-10-30-player-camera.md`. 상세 설정·실패 목록·확인 순서는 [PlayerCamera.md](PlayerCamera.md).
- 작업 기준: main / 9b0d7f2. 기존 턴의 설계·Handoff 변경을 보존했다. GiungTestScene·프리팹·Project Settings는 변경하지 않았다. 패키지 lock에는 Cinemachine과 전이 의존 Splines 2.8.4 / Settings Manager 2.1.1만 추가됐다.
- 별도 짧은 검증 복사본에서 Unity C# 컴파일 오류 없음. 새 실제 Main 씬 PlayMode 테스트는 수정 전 Brain 누락으로 0/2, 수정 후 **2/2 통과**. 이동·회전·큰 위치 변경·일시정지/재개·씬 재진입의 화면 중심·깊이·직교 크기·회전을 확인했다. 물리 입력에 의한 실제 대시 조작의 시각 확인은 사용자 확인으로 남긴다.
- 이번 전체 PlayMode: **100 통과 / 16 실패 / 생략 0**. 실패 16개 모두 기존 baseline-suite.xml에도 있던 Giung Input Action Asset 참조 누락이며 카메라 테스트는 전체 실행에서도 통과했다. 전체 통과라고 보고하지 않는다. 영속 실패 목록은 PlayerCamera.md에 남겼다.
- 독립 리뷰 중요 지적 없음, git diff --check 통과. 원본/복사본 씬과 테스트 해시 일치. 임시 증거는 `.utmp/PlayerCamera/`의 red.xml, green.xml, suite.xml 및 로그이며 영속 근거는 테스트 코드와 문서다. 원본 에디터를 종료하거나 같은 프로젝트에 두 번째 에디터를 열지 않았다. 복사본 PlayFromTitle에만 batch guard를 사용했다.
- 직접 확인: 패키지 임포트가 끝나면 MainTestScene을 다시 열고 `Tools → Play From Title`을 확인한 뒤 Play. 이동·대시 중 플레이어 중앙 유지, 카메라 회전·확대율 유지, 정지/재개·씬 재진입 시 정상 추적을 확인한다. 오류 시 Console 첫 빨간 오류, Brain, Tracking Target, Position Composer를 확인한다. Android 빌드·실기기·사용자 화면 시각 검증은 미수행이다.

## 최신 수정 — 몬스터 재사용 후 Play 종료 시 Transform Assertion

- 사용자 보고: Play 종료 시 `Assertion failed on expression: 't.GetParent() == nullptr'`, Console 스택 없음.
- 현재 `main`, HEAD `4792821`. 시작 시 MainTestScene.unity, NanumGothic Combat SDF.asset 수정 및 swipe-both/UI 이미지 meta 미추적 상태였으며 보존했다.
- 프로젝트 `Logs/Editor.log`에서 같은 Assertion 3회 직후 `Temp/__Backupscenes/0.backup` 로드를 확인했다. 로그만으로 발생 오브젝트는 식별할 수 없다.
- 추가 사용자 확인: 몬스터 자동 재등장까지 진행한 뒤 Play 종료 시 발생. 이후 검증 대상은 **MainTestScene만**이며 GiungTestScene의 별도 설정은 수정하지 않는다.
- 원인 경로: 종료 중 SceneEnemyPool.OnDisable → ReturnAll → PooledObject.ReturnToPool → PoolManager.Release의 SetParent가 씬 루트 몬스터를 영속 풀로 이동한다. 별도 복사본에서 Managers를 먼저 생성한 뒤 사망·재대여 3회와 실제 Play 종료로 같은 Assertion을 재현했다. Managers를 나중에 생성한 순서에서는 발생하지 않았다.
- SceneEnemyPool.OnApplicationQuit에서 closing을 설정하고 소유 목록을 비워 종료 중 재부모화를 막았다. 평소 OnDisable/OnDestroy 반납과 사망·재사용 동작은 유지한다. 새 회귀 테스트는 수정 전 0/1 실패(종료 후 부모가 생김), 수정 후 통과했다.
- 별도 복사본의 실제 MainTestScene에서 정상 시간으로 5마리 생성 → 치명 피해로 반납 → 5마리 모두 같은 객체로 자동 재등장 → 실제 Play 종료를 확인했다. 수정 후 Assertion 없음. 직접 Main 씬에서 시작한 수정 전 비교 실행도 이번에는 Assertion이 없었으므로, Main에서 항상 재현됐다고 해석하지 않는다.
- 현재 코드·테스트·MainTestScene의 원본/검증 복사본 SHA-256 일치를 확인했다. 원본 씬·프리팹·Project Settings·입력 설정은 수정하지 않았다. 코드 리뷰에서 중요 지적 없음, git diff --check 통과.
- 최종 관련 PlayMode **4 통과 / 실패 0 / 생략 0**: 사망 후 체력·물리 복구 및 동일 객체 재사용, 소유자 파괴 시 반납, 평소 소유자 비활성화 반납, 종료 후 재부모화 방지. 이 실행은 다른 게임 씬을 열지 않았다. 결과는 `.utmp/ExitAssertion/focused.xml`이다.
- 전체 PlayMode 실행은 **95 통과 / 19 실패 / 생략 0**이며 전체 통과가 아니다. 수정 전 비교는 94 통과 / 20 실패로 새 회귀 테스트만 추가 실패했다. 기존 Giung 입력 참조 누락 16건과 검증 복사본 Joystick 프리팹 누락으로 Main 로드 실패 3건이 있었고, Joystick은 이후 복사본에 동기화해 Main 실재생을 확인했다. 사용자 범위 지정 후 다른 씬 테스트를 재실행하거나 설정을 고치지 않았다. 실패 테스트 이름·상세는 [PlayExitAssertion.md](PlayExitAssertion.md)에 기록한다.
- 임시 증거: `.utmp/ExitAssertion/`의 red.xml, baseline-early-managers.log, fixed-exit.log, main-before.log, main-after.log, final-current.xml, baseline-suite.xml. 영속 근거는 회귀 테스트와 이 기록이다. 검증 도구의 UnityEditor.Search.SearchDatabase 시작 시 IndexOutOfRangeException은 수정 전후 모두 발생한 별도 에디터 오류이며, 게임 Assertion과 구분한다.
- 직접 확인: MainTestScene 열기 → 컴파일 완료 → Play → 일반 몬스터를 처치해 자동 재등장 확인 → Play 종료. Console에 해당 Assertion이 없어야 한다. 여전히 발생하면 종료 직전 활성 일반 몬스터 수, 시작 경로(직접 Main/타이틀 경유), Console의 첫 오류를 확인한다. 사용자 에디터에서의 재확인과 Android 빌드·실기기는 미검증이다.

## 최신 작업 — swipe-both 양손 할퀴기 다시 작성

- 사용자가 북쪽 외 프레임을 삭제한 뒤, 북쪽과 같은 양손 할퀴기 기준으로 7방향을 다시 넣었다. 북쪽 PNG는 교체하지 않았다.
- 남쪽은 북쪽과 같이 할퀴는 반쪽을 좌우로 맞췄다. 동쪽·북동쪽은 옆·대각 몸통을 유지한 채 반대 팔을 붙였고, 서쪽·남서쪽·북서쪽은 좌우 반전이다.
- 남동쪽·남서쪽 준비 프레임에는 원래 아래로 내린 손이 남을 수 있다. 전투 재생은 연결하지 않았다. Unity 재임포트 화면은 이번 턴에서 확인하지 않았다.

## 0. 최신 작업 — GiungTestScene 승패·임시 재시작·타이틀 복귀 연결

## 0. 최신 작업 — GiungTestScene 승패·임시 재시작·타이틀 복귀 연결

- 사용자 승인: 지금은 테스트용 GiungTestScene으로 재시작하되, 추후 MainTestScene 흐름이 필요하므로 복원할 곳에 주석을 남긴다.
- 시작 상태는 `feature/poolmanager`, HEAD `1d7b95e`(일시정지, 레벨 업 팝업), 작업 트리 깨끗함. 이전 팝업 작업은 커밋되어 있었다. 이번 작업에서 커밋·푸시·브랜치 전환은 하지 않았다.
- 기존 GameManager에 실제 Player.Died 구독과 로컬 StateChanged 알림을 추가했다. 사망은 1회 알림이며 같은 프레임의 사망·승리 신호는 기존대로 패배가 우선이다. 기존 테스트 버튼과 공개 HandleBossDefeated API는 보존했다.
- 결과 상태·timeScale 0 확정 후 CombatPopupController가 두 창과 정지 소유권을 정리하고 일시정지 버튼을 비활성화한다. 결과 화면 버튼으로 포커스를 옮긴다. 팝업 닫기로 전투가 재개되지 않는다.
- GameManager의 `restartScene` 기본값은 **LoadTestScene**이다. GiungTestScene의 GameManager만 **GiungTestScene(값 4)**으로 지정했다. `GameManager.cs`의 **TODO(MainSceneIntegration)** 주석에 추후 Inspector의 Restart Scene을 **LoadTestScene**으로 되돌려 **LoadTestScene → MainTestScene** 흐름을 쓰도록 명시했다. 코드 기본값만 바꾸면 씬의 직렬화 값은 변경되지 않는다.
- Define.EScene 끝에 Giung을 추가해 기존 enum 값을 보존했다. Scene List 끝에 Giung을 추가해 로드 가능하게 했으며 기존 Title/Load/Main 순서와 타이틀 시작 흐름은 그대로다.
- 씬 수정은 GameManager의 실제 Player 참조와 Restart Scene **2줄 추가**뿐이다. 기존 UI·프리팹·GUID·보스·스폰 수치는 보존했다. Project Settings 변경은 EditorBuildSettings의 Giung 항목 3줄 추가뿐이다.
- 전환은 기존 Managers.Scene 경로로 풀·UI·사운드를 정리하고 리소스·Managers를 유지한다. 목적지 검증 실패 시 시간과 전환 가드를 되돌려 설정 수정 후 재시도한다.
- 일반 보스 한 마리 사망을 최종 승리로 연결하지 않았다. 기존 Victory 테스트/명시적 최종 승리 신호를 사용한다. 실제 6회 보스 진행·최종 승리 판단·보상·레벨 시스템은 후속이다.

### 이번 검증 — 2026-10-30

- Unity 6000.5.0f1 컴파일 성공, 최종 **전체 PlayMode 113 통과 / 실패 0 / 생략 0**, 종료 코드 0. 기존 105개 + `GameResultFlowTests` 신규 8개다.
- 수정 전 6개 중 4개 실패(실제 사망 미연결·결과 중 팝업 잔류·임시 재시작 경로·설정 API)를 확인했다. 수정 후 111/111, 정상 Load→Main 복원 경로와 사망 1회 알림을 보강한 최종 113/113이 통과했다.
- 실제 씬에서 결과 포커스·동시 신호 우선순위·Giung 새 판·Managers 1개/리소스 유지·타이틀 복귀 시 UI/풀/사운드 정리·전환 실패 후 재시도·LoadTestScene→MainTestScene의 TestReady 진입을 확인했다.
- 독립 코드 리뷰에서 중요 결함 없음. `%TEMP%/sbs-ui` 별도 복사본에서 실행했고 사용자 에디터·원본 Play From Title 옵션은 바꾸지 않았다. 변경 코드·씬·Scene List·테스트 7개 경로의 원본/복사본 SHA-256 일치를 확인했다. `git diff --check` 통과, 기존 obsolete 경고는 남아 있다.
- 결과·로그는 `.utmp/GameResults/red.*`, `green.*`, **`final.xml` / `final.log`**다. 새 테스트 코드와 이 문서가 영속 근거이며 임시 로그를 Git에 추가하지 않는다.
- 실제 보스 최종 판정·운영 데이터·보상·Android 빌드·실기기·사용자 화면 직접 확인은 미검증이다.

### 직접 확인과 다음 작업

1. [GameResults.md](GameResults.md)에 따라 Play From Title을 잠시 끄고 GiungTestScene에서 Play한다. 기존 **Victory / Player Dead** 테스트 버튼으로 결과 화면을 확인한다.
2. **Restart**는 GiungTestScene의 새 판, **To Title**은 TitleTestScene 복귀가 정상이다. 재시작/복귀 후 timeScale 1, 결과·전투 팝업 닫힘을 확인한다. 실제 HP 0도 패배로 연결돼야 한다.
3. `Window → General → Test Runner → PlayMode → GameResultFlowTests → Run Selected`: **8개**, `Run All`: **113개 통과**가 정상이다. 테스트 후 Play From Title을 원래대로 돌린다.
4. 추후 Main 연결 시 **GiungTestScene → GameManager → Restart Scene을 LoadTestScene으로 변경**한다. TODO(MainSceneIntegration)가 이 임시 설정의 복원 지점이다. 타이틀 시작 경로는 이미 정상 Load→Main을 유지한다.
5. 문제 시 첫 빨간 오류·스택, 실패 테스트, GameManager의 Player/결과 패널/Restart Scene과 Build Profiles Scene List 등록을 확인한다.
6. 다음은 기존 진행·보스 구조와 기획을 대조한 실제 최종 승리 신호 연결이다. 일반 보스 처치를 곧 최종 승리로 바꾸지 않는다. 보상·성장 수치·레벨 시스템은 별도 승인 범위에서 진행한다.

## 이전 작업 — GiungTestScene 전투 팝업과 정지 연결 완료

- 사용자는 실제 레벨 시스템 없이 향후 이벤트 연결 진입점과 수동 확인 기능을 먼저 만들고, 일시정지 팝업에는 **뒤로가기** 버튼을 추가하도록 승인했다. 작업 대상의 실제 파일명은 GiungTestScene이다.
- 시작 상태는 `feature/poolmanager`, HEAD `25671d6`(UIManager, 사운드 안정성, 씬 전환정리), 작업 트리 깨끗함이었다. 이전 UI·사운드·씬 작업은 이 커밋에 포함되어 있다. 이번 작업에서 커밋·푸시·브랜치 전환은 하지 않았다.
- `CombatUI.prefab`과 씬 로컬 CombatPopupController를 추가했다. 오른쪽 위 **일시정지** → 중앙 팝업의 **뒤로가기**로 전투를 정지·재개한다. 기존 UIManager의 단일 캐시·스택·표시 순서를 그대로 사용한다.
- 레벨업은 `ShowLevelUp()`, `CompleteLevelUp()`, `LevelUpContentRoot`를 준비했다. 화면에는 **레벨 업** 제목과 빈 콘텐츠 영역만 있으며 보상·선택지·레벨 시스템은 만들지 않았다. Editor의 컴포넌트 컨텍스트 메뉴로 수동 열기·완료를 확인한다.
- 겹친 팝업이 모두 닫혀야 저장한 timeScale로 복원한다. 중복 요청은 소유권을 늘리지 않는다. 외부 정지 0과 승패 상태를 보존하며 외부 비활성화·파괴·UI.Clear·씬 종료 시 팝업 소유 정지를 해제한다. 보스전 타이머 정지와 전투 정지는 분리했다.
- UIManager에는 기존 씬 인스턴스 등록 API를 추가했다. CacheAll은 ResourceManager에 실제 로드된 프리팹만 준비하므로 씬 팝업용 가짜 카탈로그를 만들지 않았다. 새 전역 매니저·싱글톤·이벤트 프레임워크는 없다.
- 실제 GiungTestScene은 MVP PlayerMeleeAttack이 아니라 기존 `Game/Player/Player.cs`를 사용한다. timeScale 0에서도 준비된 자동공격이 실행되는 문제를 재현해 Update에 정지 가드를 추가하고 패링·가드 루프도 정지 중 입력 상태를 넘기지 않도록 했다. 기존 공개 API와 직렬화 참조는 보존했다.
- 씬 변경은 새 UI 프리팹 인스턴스와 기존 GameManager 참조 **23줄 추가**뿐이다. 이를 제거해 작업 전 스냅샷과 줄바꿈 정규화 후 원문 일치를 확인했다. 기존 오브젝트·GUID·보스 등장·스폰 수치·Project Settings·패키지는 그대로다.
- 한글용 NanumGothic TTF 원본과 OFL 라이선스, 해당 원본을 참조하는 TMP dynamic SDF를 포함했다. Windows 시스템 글꼴에 의존하지 않는다.

### 이번 실행 검증 — 2026-10-30

- Unity 6000.5.0f1 컴파일 성공, 최종 **전체 PlayMode 105 통과 / 실패 0 / 생략 0**, 종료 코드 0. 기존 85개 + `CombatPopupTests` 13개 + `CombatPopupSceneTests` 7개다.
- 구현 전 관리/수명 테스트 11/11 실패 → 구현 후 기존 포함 96/96 통과를 확인했다. 초기 씬 테스트의 MVP 플레이어 가정을 실제 Player로 보정했다. 정지 중 준비된 자동공격이 attackTimer 0→0.5로 실행되는 문제와 뒤로가기 포커스 누락을 수정 전 실패로 재현하고 수정 후 전체 102/102가 통과했다.
- 별도 코드 리뷰에서도 발견한 `ShowPause → ShowLevelUp → ShowPause` 재전면 포커스 문제를 0/1 실패로 확인했다. Show 성공 시 포커스를 갱신한 후 이름 충돌·별칭 등록 회귀까지 포함한 최종 105/105가 통과했다.
- 실제 씬의 버튼·레이캐스트·키보드/패드 선택 포커스·이동/공격/스폰/진행 시간 정지·겹침·보스전 유지·씬 해제를 테스트했다. 테스트 asmdef에 UnityEngine.UI와 Unity.TextMeshPro 참조만 추가했으며 런타임 어셈블리는 이동하지 않았다.
- 별도 검증 복사본 `%TEMP%/sbs-ui`에서 실행했다. 사용자 Unity를 종료하거나 원본 프로젝트에 두 번째 에디터를 열지 않았다. 복사본 PlayFromTitle에만 batch guard를 유지했다.
- 변경 런타임·씬·프리팹·글꼴·테스트 10개 경로의 원본/복사본 SHA-256 일치를 확인했다. 기존 컴파일의 obsolete 경고는 남아 있다. `git diff --check` 통과.
- 새 UI 자산은 복사본 Unity에서 생성해 GUID/meta를 함께 옮겼다. 임시 생성기와 검증 로그는 `.utmp/CombatPopups/` 아래에 있으며 Git에 추가하지 않는다. 최종 결과는 **`final.xml` / `final.log`**다. 영속 근거는 테스트 코드·프리팹·이 기록이다.
- 배치 Game View 캡처는 시간 초과로 완료되지 않았다. 대신 전용 카메라로 **UI 프리팹**을 1920×1080 렌더해 한글·배치를 직접 확인했다(`pause.png`, `level-up.png`). 이를 실제 전체 게임 화면의 사용자 시각 확인으로 보고하지 않는다.
- Android 빌드·터치 실기기·노치 safe area·실제 성장 선택/효과/이벤트·여러 레벨업의 대기 큐는 미검증·미구현이다. 미래 레벨 시스템이 대기 선택 수와 보상 적용을 소유한다.

### 직접 확인과 다음 작업

1. [CombatPopups.md](CombatPopups.md)를 따라 `Tools → Play From Title`을 확인 중 잠시 끄고 GiungTestScene을 열어 Play한다.
2. 오른쪽 위 **일시정지** → **뒤로가기**로 멈춤·재개를 반복 확인한다. Play 중 **Combat UI → Combat Popup Controller** 컴포넌트 메뉴에서 **테스트 → 레벨업 팝업 열기 / 레벨업 선택 완료**를 확인한다.
3. 레벨업을 연 상태에서 메뉴의 **일시정지 팝업 열기** → 뒤로가기: 레벨업이 남으며 정지 유지 → 레벨업 선택 완료 후 재개가 정상이다. 보스전에서는 뒤로가기 후 전투만 재개되고 진행 타이머/일반 스폰의 보스전 정지는 유지돼야 한다.
4. `Window → General → Test Runner → PlayMode`: 신규 두 클래스 **Run Selected는 20개**, **Run All은 105개 통과**가 정상이다. Play From Title 설정을 원래대로 돌린다.
5. 오류 시 첫 Console 빨간 오류·스택, 실패 테스트 상세, Controller의 4개 직렬화 참조, 폰트/meta를 확인한다. 팝업이 Play 중 @UI_Root로 이동하는 것은 정상이다.
6. 후속은 기획과 기존 GameManager를 대조한 **승패·재시작·타이틀 복귀와 진행 연결**이다. 레벨/경험치 시스템은 이번 요청으로 확정하지 않았다. 미래 레벨업 이벤트 구독자는 제공한 API를 호출하도록 연결한다.

## 이전 작업 — 사운드 안정성과 씬 정리·전환 계약 완료

- 작업 시작 시 `feature/poolmanager`, HEAD `3d2490c`를 다시 확인했다. 이전 UIManager·UI_Popup·PooledEnemySpawningTests·Handoff 수정과 신규 UI 테스트·문서가 미커밋 상태였으며 모두 보존했다. 이번 작업에서도 커밋·푸시·브랜치 전환은 하지 않았다.
- SoundManager의 중복 클립 캐시를 제거했다. ResourceManager의 현재 캐시를 사용하며 누락 주소·null 클립은 Warning 후 기존 재생을 유지하고 재시도를 허용한다. BGM에도 pitch를 적용하고 잘못된 enum은 거절한다.
- 초기화 전 Sound.Clear는 안전하다. Clear는 소스를 멈추고 clip을 해제하며 pitch를 1로 되돌린다. 파괴된 소스·루트는 다음 사용 때 복구하고 원래 Managers 부모를 유지한다.
- SceneManagerEx의 동기·비동기 전환은 대상 검증 → Managers.Clear → Unity Single 로드를 공유한다. LoadScene의 MainTestScene 비동기 진입도 연결했다.
- 정리는 활성 씬의 BaseScene.Clear → Pool → UI → Sound 순서다. 재진입을 막고 씬 훅의 예외가 있어도 공통 정리 후 예외를 전달한다. 리소스·데이터·준비 상태는 유지한다. 씬·프리팹·기존 meta·Project Settings·패키지는 변경하지 않았다.
- 새 전역 매니저·오디오 설정·음량 저장·게임 일시정지·보상은 추가하지 않았다. 씬 전환 간 BGM 연속 재생, 동시 전환 큐·취소, 다중 씬별 정리 정책은 후속 요구가 있을 때 다룬다.

### 이번 실행 검증 — 2026-10-30

- Unity 6000.5.0f1 컴파일 성공, 최종 **전체 PlayMode 85 통과 / 실패 0 / 생략 0**, 종료 코드 0. 기존 72개 + 신규 `SoundSceneLifecycleTests` 13개다.
- 첫 회귀 10개 중 9개 실패로 누락 클립 재시도·BGM pitch·null·초기화 전 Clear·소스 파괴·잘못된 enum·씬 훅 누락·비동기 공통 API 부재를 재현했다. 수정 후 전체 82/82가 통과했다.
- 별도 리뷰에서도 확인한 루트 전체 파괴 후 복구 소유권 문제를 추가 테스트 0/1로 재현했다. 부모 Transform을 보존하도록 수정 후 83/83, 동기 전환과 씬 훅 예외·재진입 가드 해제 검증을 보강한 최종 실행에서 85/85를 확인했다.
- 테스트용 BaseScene 파생 `Assets/Tests/Fixtures/SceneCleanupProbe.cs`는 기존 Assembly-CSharp를 사용하며 UNITY_EDITOR로 제한한다. 런타임 asmdef 재편은 하지 않았다.
- 사용자 Unity를 종료하지 않고 이전 `%TEMP%/sbs-ui` 검증 복사본을 최신 Assets로 동기화했다. 복사본의 PlayFromTitle에만 batch guard를 유지했다. 수정 런타임 4개·새 테스트와 probe·GiungTestScene의 원본/복사본 SHA-256 일치를 확인했다.
- 결과·로그는 `.utmp/SoundScene/red.*`, `green.*`, `root-red.*`, `final.*`, **`final-expanded.xml` / `final-expanded.log`**다. 경로는 `.utmp/UIPopups/validation-path.txt`를 확인한다. 임시 생성물이므로 영속 근거는 테스트 코드와 이 문서다. `git diff --check` 통과. 기존 obsolete 경고는 남아 있다.
- 실제 음원 주소·카탈로그·번들, 청감·플랫폼별 오디오 지연, Android 빌드·실기기는 미검증이다. 합성 AudioClip과 실제 Unity AudioSource API로 상태·수명을 검증했다.

### 직접 확인과 다음 작업

1. [SoundAndScenes.md](SoundAndScenes.md)를 따라 컴파일 완료 후 `Tools → Play From Title`을 테스트 중 잠시 끈다.
2. `Window → General → Test Runner → PlayMode → SoundSceneLifecycleTests → Run Selected`: **13개 모두 녹색**이 정상이다. `Run All`은 **85개 통과**가 정상이다. 테스트 후 메뉴 설정을 복원한다.
3. TitleTestScene을 열고 Play → 시작 터치 영역(`StartTouchArea`)을 누른다. LoadTestScene → MainTestScene 진입과 `DontDestroyOnLoad/@Managers/@SoundRoot/Bgm·Effect` 한 세트를 확인한다.
4. 실제 음원 연결 후 전환 시 소스 정지·clip 비움, 재생 시 pitch·loop·청감을 확인한다. 문제가 있으면 실패 테스트 상세와 Console 첫 빨간 오류·스택, 클립 주소·로딩 완료 여부, Build Profiles Scene List 등록을 확인한다.
5. 다음은 **공통 매니저와 실제 게임 진행·일시정지·승패·보상 흐름 연결**이다. 기존 GameManager·Encounter·플레이어 구현과 기획 원문을 먼저 대조한다. 미정 보상·팝업 내용은 구현하지 않는다.

## 이전 작업 — UIManager 팝업 관리 완료

- 작업 시작 시 `feature/poolmanager`, HEAD `3d2490c`(몬스터, 몬스터 스포너 풀링), 작업 트리 깨끗함을 확인했다. 이 작업에서 커밋·푸시·브랜치 전환은 하지 않았다.
- 초기화·게임 준비·리소스·몬스터 풀링 런타임은 수정하지 않았다. 씬·프리팹·기존 meta·Project Settings·패키지도 변경하지 않았다.
- 같은 이름의 팝업을 다시 요청하면 같은 인스턴스를 재사용하고 열린 목록에 한 번만 유지하며 앞으로 올린다. Canvas 표시 순서는 열린 스택의 아래부터 **10, 11, 12…**로 다시 계산한다.
- CachePopupUI는 비활성 캐시 준비만 한다. 이미 열린 팝업과 CacheAllPopups의 기존 열린 상태를 보존한다. 최상단만 닫는 기존 계약은 유지했다.
- 외부 파괴·비활성화 항목을 열린 목록에서 제거하고, 파괴된 캐시는 재생성한다. CloseAll은 숨기고 캐시를 유지하며 Clear는 소유한 캐시 객체도 파괴한다.
- UI 생성 API 5곳과 CachePopupUI의 리소스 누락 시 null 역참조를 막았다. 잘못된 캐시 프리팹은 오류를 기록하고 폐기한다. 캐시 이름의 타입 불일치도 거절한다.
- 실제 게임 팝업 내용·버튼·전투 정지·보상은 추가하지 않았다. 현재 Framework에는 파생 게임 팝업이 없으므로 새 테스트는 기본 UI_Popup을 사용한다. 파생 팝업별 초기화와 화면 품질은 연결 후 확인해야 한다.

### 이번 실행 검증 — 2026-10-30

- Unity 6000.5.0f1 C# 컴파일 성공, 최종 **전체 PlayMode 72 통과 / 실패 0 / 생략 0**, 종료 코드 0. 기존 55개 + 신규 `UIPopupTests.cs` 17개다.
- 수정 전 UI 14개 중 13개 실패로 중복 Push·재사용 Canvas 순서·캐시 스택 등록·null 예외·파괴 객체 정리 문제를 재현했다. 리뷰 후 캐시 3개를 보강해 수정 전 코드에서 추가 실패도 확인했다.
- 첫 수정 후 전체 68/69에서 기존 GiungScene 통합 테스트가 실패했다. UI 수정 전 코드로 전체 대조 실행해 같은 실패를 확인했다. 현재 씬은 **스포너 5개, 간격 5초, 스포너당 12마리, 배치 1마리**인데 테스트가 과거 1초 간격을 가정했다. 씬과 런타임은 보존하고 테스트의 두 Tick만 현재 씬 간격을 읽도록 수정했다. 최종 실행에서 보스 정지·재개·씬 해제까지 통과했다.
- 별도 코드 리뷰에서 중요 결함은 발견되지 않았다. 이미 열린 동일 타입의 캐시 요청, 잘못된 컴포넌트 프리팹, CacheAll의 열린 상태 보존을 테스트에 추가했다. 실제 파생 타입 충돌 시나리오는 미검증이다.
- 사용자 에디터를 종료하지 않고 `%TEMP%/sbs-ui` 검증 복사본에서 실행했다. 복사본 PlayFromTitle에만 batch guard를 넣었다. 원본의 메뉴 설정과 편집기 스크립트는 그대로다.
- 수정 런타임 2개·테스트 2개 및 GiungTestScene의 원본/검증 복사본 SHA-256 일치를 확인했다. `git diff --check` 통과. 기존 API 사용의 obsolete 경고는 남아 있으며 이번 작업에서 일괄 정리하지 않았다.
- XML·로그: `.utmp/UIPopups/red.xml`, `baseline.xml`, `green.xml`, `final.xml`과 대응 로그. 위치 파일은 같은 폴더 `validation-path.txt`다. 모두 임시 생성물이다. 영속 근거는 테스트 코드와 이 문서다.
- 실제 사용자의 화면 확인, 파생 게임 팝업·입력·해상도별 시각 검증, 실제 카탈로그·번들, Android 빌드·기기는 미검증이다.

### 다음 작업과 직접 확인

1. [UIPopups.md](UIPopups.md)의 순서로 `Tools → Play From Title`을 테스트 중 잠시 끄고, `Window → General → Test Runner → PlayMode → UIPopupTests → Run Selected`를 실행한다. **17개 모두 녹색**이어야 한다.
2. 반복 열기는 열린 수 1, A→B→A는 A가 가장 위, 한 번 닫으면 B가 남는지 해당 테스트 결과로 확인한다. 캐시 준비는 열린 수를 늘리지 않고 누락 프리팹은 예외 없이 실패해야 한다.
3. `Run All`은 현재 **72개 통과**가 정상이다. TitleTestScene에서 Play 후 LoadTestScene을 거쳐 MainTestScene에 진입하는 기존 흐름도 확인한다. 테스트 후 Play From Title 설정을 원래대로 돌린다.
4. 오류가 나면 실패한 테스트 이름·상세 결과와 Console 첫 빨간 오류·스택을 확인한다. 실제 팝업 연결 후에는 요청 주소의 리소스 로딩, `@UI_Root`의 활성 객체, Canvas sortingOrder와 닫기 대상을 함께 확인한다.
5. 다음 우선순위는 **사운드 사용 안정성과 씬 정리·전환 계약**이다. 게임 일시정지·보상 연결이나 미정 기획을 이번 팝업 수정에 섞지 않았다.

## 이전 작업 — GiungTestScene 몬스터 풀 스폰 완료 (2026-10-28 기록)

사용자의 새 요청으로 아래 기존 매니저 3단계(UI 팝업)보다 **GiungTestScene 몬스터 풀 스폰**을 먼저 구현했다. 사용자는 m이 **스포너 하나당 총량**임을 확인했고 분 전환 시 이전 할당량을 끝까지 완료하는 설계를 승인했다.

- `Enemy Spawn System` 씬 객체의 EnemySpawnDirector에서 Spawner Count와 Minutes를 설정한다. 분별 몬스터 프리팹 목록, n초 간격, 스포너당 m마리, 배치 l마리를 지정한다.
- 작업 시작 때 현재 카메라 경계 밖에 스포너를 배치하고, 다음 배치 때 화면 안에 들어왔으면 재배치한다. 완료 시 스포너만 비활성화하며 이미 생성한 몬스터는 추적한다.
- 각 스포너는 한 분에 최대 한 작업을 시작한다. 미완료 작업은 기존 종류·간격·총량을 끝까지 유지하고 완료 후 현재 분 설정을 적용한다. 지난 분을 몰아서 실행하지 않으며 마지막 설정은 이후 매분 재사용한다.
- `SceneEnemyPool`은 새 풀을 만들지 않고 기존 `Managers.Pool`에 연결한다. 이 시스템의 프리팹 종류 전체에 일반 몬스터 동시 130마리 제한을 적용한다. 제한 때문에 못 만든 수는 생성량에 포함하지 않는다.
- 사망한 몬스터를 풀로 반납하고 체력·사망 상태·물리·충돌·추적·피격 색상을 초기화해 재사용한다. 보스 등 풀 미사용 객체의 기존 사망 처리는 유지한다.
- 기존 EnemyMvp.Runtime과 Framework를 대규모 이동하지 않았다. 작은 IEnemySpawnPool 계약과 Assembly-CSharp의 SceneEnemyPool로 연결했다. 새 전역 매니저·싱글톤은 없다.
- BossEncounterController의 Spawn Director 연결이 있는 씬은 일반 생성과 분별 시간을 정지·재개하고 일반 몬스터를 반납한다. 연결 없는 씬의 기존 스포너 목록 API는 유지한다. Time.timeScale을 보스 출현 시 변경하지 않는다.
- 초기 연결값은 기존 테스트의 스포너 1개, n=1, m=5, l=1, EnemyMvp 1종이다. 정식 분별 수치와 추가 종류를 임의로 만들지 않았다.

### 이번 작업의 사용자 변경 보존

- 작업 시작 시 README.md, docs/AI/UnityProjectContext.md 수정과 AGENTS.md, Handoff.md 미추적 상태를 확인했다. 관련 없는 변경은 보존했다.
- 작업 도중 사용자 GiungTestScene 변경(보스 등장 **100초**, 일부 기존 UI 제거 등)을 확인했다. 이후 최신 씬을 기준으로 연결만 추가했으며 10초로 되돌리지 않았다.
- 추가한 스폰 객체와 연결을 제외한 씬 내용을 작업 중 보관한 사용자 최신 스냅샷과 줄바꿈 정규화 후 대조해 동일함을 확인했다. 씬·프리팹·Project Settings 일괄 재저장 없음.
- 브랜치 `feature/poolmanager`, 시작 HEAD `7f31651`. 이 작업에서 커밋·푸시·브랜치 전환은 하지 않았다. 현재값은 다음 세션에서 다시 확인한다.

### 이번 실행 검증 — 2026-10-28

- Unity 6000.5.0f1 컴파일 성공, **전체 PlayMode 55 통과 / 실패 0 / 생략 0**. 기존 Framework 36개 + `PooledEnemySpawningTests.cs` 새 19개다.
- 풀 기능 없음 3/3 실패 → 풀 구현 후 3개 통과 및 미구현 스포너 8개 실패 → 스포너 구현 후 13개 통과 및 씬 연결 전 통합 1개 실패를 확인했다.
- 전체 실행 50/51에서 새 분 첫 배치가 이전 프레임 시간을 받는 오류를 재현하고, 기존 작업 Tick 후 새 작업을 할당하도록 수정했다.
- 독립 리뷰의 오래된 대여 소유자 추적 간섭, 새 분에서 먼 옛 위치 유지 문제를 각각 테스트 실패로 재현한 뒤 수정했다. 풀 소유자 비활성화 시 미반납도 재현 후 수정했다.
- 실제 GiungTestScene을 PlayMode에서 로드해 풀 생성·보스전 일반 몬스터 반납·시간/스폰 정지·보스 사망 후 재개·씬 언로드 후 대여 수 0을 검증했다. 재대여 몬스터가 새 목표로 이동하는 물리 테스트도 통과했다.
- 사용자 에디터를 종료하거나 같은 프로젝트에 두 번째 Unity를 열지 않았다. 기존 짧은 검증 복사본에 이번 코드·meta·씬을 동기화해 실행했다. 복사본 PlayFromTitle에만 batch guard를 유지했다. 원본 옵션은 변경하지 않았다.
- 결과 XML은 `.utmp/EnemySpawning/final.xml`, 검증 경로는 같은 폴더 `validation-path.txt`에 있다. 임시 생성물이므로 다음 검증의 유일한 근거로 삼지 않는다. 영속 근거는 테스트 코드와 이 기록이다.
- 사용자 화면에서의 시각 확인, Android 빌드·실기기·성능 측정은 수행하지 않았다.

### 다음 작업과 직접 확인

1. [EnemySpawning.md](EnemySpawning.md)의 순서로 GiungTestScene → Enemy Spawn System → Enemy Spawn Director를 확인한다. Spawner Count와 Minutes의 n/m/l/프리팹을 설정하고 실행한다.
2. 기본값은 1초마다 한 마리씩 5마리 생성 후 스포너 비활성화다. 60초에 다음 작업, 현재 씬 설정상 100초에 보스가 등장한다. 보스 처치 후 시간과 미완료 스폰이 재개돼야 한다.
3. 추가 종류·실제 분별 수치는 개발자가 등록한다. 종류가 여러 개면 현재는 목록 순환이며 임의의 가중 확률은 없다.
4. 문제가 있으면 Console 첫 빨간 오류와 상세 내용, Director 참조·Minutes·Spawner Count, 진행 시간과 보스전 여부를 확인한다. `Tools → Play From Title`이 켜져 있으면 테스트 중 잠시 끈다.
5. 기능 추가 지시가 없다면 기존 매니저 후속 우선순위는 아래 UI 팝업 관리다. 이번 작업에서 UIManager 팝업 수정은 하지 않았다.

설계: `docs/superpowers/specs/2026-10-28-pooled-enemy-spawning-design.md`. 구현 계획·검증 진행: `docs/superpowers/plans/2026-10-28-pooled-enemy-spawning.md`.

이하 1~9절은 **앞서 완료한 공통 매니저 작업의 기록**이다. 테스트 수 36개와 씬 설정은 그 작업 당시 정보다.

## 1. 현재 작업과 재개 위치

사용자 목표는 **게임 전역 매니저와 Framework Core의 오류를 고치고 좀비소울에 사용할 수 있게 정리하는 것**이다. 매니저 코드 분석 후 우선순위대로 진행 중이다.

| 순서 | 작업 | 상태 |
|---|---|---|
| 0 | Managers와 Core 전체 분석 | 완료 |
| 1 | 초기화·로딩 흐름, Core와 게임 데이터 준비 상태 분리 | 완료, 당시 PlayMode 21/21 |
| 2 | 리소스 로딩 중복·동시 요청·실패·해제 처리 통합 | 완료, 누적 PlayMode 36/36 |
| 3 | **UI 팝업 중복 열기·재사용·표시 순서·생성 실패 처리** | **완료, 2026-10-30 전체 PlayMode 72/72** |
| 4 | 사운드의 남은 사용 안정성, 씬 정리·전환 계약 보완 | 완료, 2026-10-30 전체 PlayMode 85/85 |
| 5 | 공통 매니저와 실제 게임 진행·보상·일시정지 연결 | 팝업·정지·승패·재시작/복귀 완료. 실제 최종 판정·보상·성장은 후속 |

1·2단계 정리 당시에는 3단계를 시작하지 않았다. 이후 2026-10-30에 3단계를 완료했다. 최신 상태는 0절을 기준으로 한다.

### Git 기준점

- 정리 직전 브랜치: `feature/poolmanager`.
- 정리 직전 HEAD: `7f31651595083b7549c52b010ae9b3859b416ef7` — `초기화, 로딩, 리소스 로딩 수정`.
- 직전 관련 커밋: `688e854` — `매니저 개선`.
- 이 문서 작업 시작 시 `git status --short`는 비어 있었다. 초기화·리소스 수정과 테스트는 이미 위 HEAD에 포함되어 있다.
- 이전 대화의 “미커밋 상태” 안내는 당시 상황이다. **지금도 미커밋이라고 단정하지 않는다.** 인수인계 문서 자체의 커밋·원격 반영 여부는 복귀할 때 따로 확인한다.
- 당시 사용자 수정이었던 `GiungTestScene.unity`와 meta도 HEAD에 포함되어 있다. 공통 매니저 수정의 부산물로 오인해 되돌리지 않는다.

## 2. 합의한 게임 기획과 사용자 선호

- 가칭 **좀비소울**: 안드로이드용 2D 탑다운 보스 러쉬 액션 로그라이트.
- 일반 구간은 자동 추적 탄환과 이동, 보스전은 가드·패링·회피·약공격·차징 강공격 중심이다.
- 자원 이름은 **MP**로 통일하기로 확정했다. 기획서 일부 ST는 구표기다.
- 일반 전투 진행 타이머 18분, 3분마다 보스가 나와 총 6회 전투한다. 보스전에는 진행 타이머만 멈추고 전투는 계속된다.
- 성장 선택 팝업 등 전투 전체 정지와 보스전의 타이머 정지는 별개의 규칙이다.
- 일반 몬스터 동시 제한은 총 130마리다. 풀 대기 보관 한도나 예제의 60마리 제한과 혼동하지 않는다.
- DNA 경험치, 선택형 성장, 유물·증강·융합이 있다. 세부 수치와 아직 없는 테이블을 가짜 데이터로 채우지 않는다.
- 필드 전체 N×M 크기, 히든 보스 진입 조건, 퀘스트 조건·보상, 일부 팝업은 미정이다. 사용자는 미정인 대로 남기라고 했다.
- **완료 보고에는 사용자가 무엇을 어떻게 확인할지 반드시 적는다.** “테스트 통과”만 보고하고 끝내지 않는다.

### 기획 원본

- 루트 `SBSAcademy_Team01_기획서.docx` — 2026-10-28 수정까지 읽었다.
- 루트 `Zomso_Skill_기획서.docx`, `Zomso_Parrying_기획서.docx`, `Zomso_Build_기획서.docx` — 관련 작업 때 세부 원문을 읽는다. 이 문서화에서 세부 기획서까지 읽었다고 주장하지 않는다.
- `SkillDatatable.xlsx`, `Strengthentable.xlsx`가 있다. 소스 표가 있다는 것과 런타임 JSON·Addressables 연결 완료는 다르다.

## 3. 확인된 환경과 구조

| 항목 | 2026-10-28 확인값 |
|---|---|
| Unity | 6000.5.0f1, revision 88b47c5e7076 |
| 렌더링 | URP 17.5.0, 2D 프로젝트 |
| 입력 | Input System 1.19.0, `activeInputHandler: 2` (Both) |
| 리소스 | Addressables 2.9.1 |
| JSON / 테스트 | Newtonsoft.Json 3.2.2 / Unity Test Framework 1.7.0 |
| 빌드 씬 | TitleTestScene → LoadTestScene → MainTestScene |

프로젝트 루트는 매번 `git rev-parse --show-toplevel`로 확인한다. 과거 문서의 `C:/GitHub/...`나 당시 사용자 프로필 경로를 고정값으로 재사용하지 않는다.

주요 코드:

- `Assets/0.Scripts/Framework/Managers/Managers.cs`: 영속 서비스 접근점. `@Managers` 하나와 자식 `@Pools`, `@SoundRoot`를 소유한다.
- `Assets/0.Scripts/Framework/Managers/Core/`: DataManager, ResourceManager, PoolManager, PooledObject, IPoolable, SceneManagerEx, SoundManager, UIManager.
- `Assets/0.Scripts/Framework/UI/`: UI_Base, Popup/UI_Popup, Scene/UI_Scene.
- `Assets/0.Scripts/Framework/Scenes/`: BaseScene, TitleScene, LoadScene.
- `Assets/0.Scripts/Manager/GameManager.cs`: 별도 승패·결과 화면 담당. Managers와 이름이 비슷하지만 책임이 다르다.
- `Assets/0.Scripts/Game/Encounter/`: 전투 진행 타이머와 보스 전환 관련 기존 코드.

Framework는 기존 Assembly-CSharp에 있다. 테스트는 별도 `Framework.PlayModeTests.asmdef`에서 reflection으로 실제 런타임 API를 호출한다. 이 접근 때문에 테스트가 있다는 이유만으로 런타임 어셈블리를 재편하지 않는다.

`docs/AI/UnityProjectContext.md`에는 10월 18일 역사 기록도 있다. 옛 테스트 수 26/33/41, 이전 입력 설정, 이전 Build Settings 설명을 현재값으로 사용하지 않는다.

## 4. 완료된 구현의 계약

### 1단계: 초기화와 게임 준비

- `Managers.Initialized`: 풀·사운드 등 Core 준비만 뜻하며 외부에서 변경할 수 없다.
- `Managers.PrepareGame(string preloadLabel = "", bool requireSkillData = false)`: Core → 요청 리소스 → SkillData 검증 순서로 준비한다.
- 상태는 `NotStarted`, `Preparing`, `TestReady`, `Ready`, `Failed`다. `GameDataReady`는 유효한 Ready일 때만 true다.
- 사용자가 **Core 준비와 게임 데이터 준비를 분리하고 빈 테스트 씬 진입은 허용**하는 방식을 선택했다.
- LoadScene 기본값은 빈 preloadLabel, requireSkillData=false다. 현재 테스트 시작은 TestReady이며 게임 데이터가 완성됐다는 뜻이 아니다.
- 명시적으로 요청한 라벨이 없거나 로딩·데이터 검증에 실패하면 Failed로 끝내고 MainTestScene 진입을 막는다.
- SkillData의 빈 목록·null 항목·중복 ID·JSON 오류는 실패다. 검증 완료된 사전만 게시한다.
- 비동기 준비는 영속 Managers가 소유한다. 기다리던 로딩 객체가 사라져도 공유 준비가 고립되지 않는다.
- 같은 설정의 동시 준비 요청은 같은 작업을 기다리고, 다른 설정의 동시 요청은 거절한다.
- 리소스 Clear 또는 실패한 데이터 재초기화는 이전 Ready를 무효화한다. 다시 준비하기 전에는 NotStarted다.
- Sound.Init은 기존 사운드 루트도 재연결하고 AudioSource를 준비한다. 사운드 루트는 Managers와 함께 파괴된다.
- Managers 파괴 시 풀과 리소스 핸들을 정리한다.

상세: [CoreInitialization.md](CoreInitialization.md), `docs/superpowers/specs/2026-10-28-core-initialization-design.md`, 대응 plans 문서.

### 2단계: 리소스 로딩

- PreloadAsync와 LoadAllAsync는 같은 배치 로딩 처리 경로를 사용한다.
- 기존 `LoadAllAsync<T>(label, progress)`를 유지하고 `(label, progress, completed)` overload를 추가했다.
- 진행 수는 성공 수가 아닌 **처리된 수**다. 전체 성공 여부는 `completed(success, error)` 또는 PreloadAsync 결과로 판단한다.
- 빈 라벨·조회 결과는 진행 `(null, 0, 0)`을 알리고 실패로 종료한다. 기존 완료 콜백 없는 API의 실패는 Console에 기록한다.
- 동일 주소·공급자·파일·타입·의존 리소스 요청은 진행 중 로딩을 공유한다. 호출자별 알림은 유지한다.
- 실패는 캐시하지 않아 재시도할 수 있다. 캐시 사용 시에도 각 요청의 알림을 보낸다.
- 주소·타입·의존 리소스 충돌은 명시적으로 실패한다. Sprite 우선 선택은 Texture와 같은 원본일 때만 적용한다.
- 배치별 조회 핸들과 공유 에셋 핸들의 취득·이전·해제 소유권을 구분한다. 이전 HashSet의 참조 취득 횟수 손실 문제를 제거했다.
- Clear는 기존 상태를 먼저 분리하고 실패로 종료한다. 늦은 완료가 캐시를 다시 채우지 못하며, 정리 콜백이 시작한 새 요청은 유지한다.
- 한 호출자의 콜백 예외는 다른 호출자의 알림을 막지 않는다.

상세: [ResourceLoading.md](ResourceLoading.md), `docs/superpowers/specs/2026-10-28-resource-loading-design.md`, 대응 plans 문서.

## 5. UI 팝업 관리 — 수정 전 조사 기록

먼저 읽을 파일:

1. `Assets/0.Scripts/Framework/Managers/Core/UIManager.cs`
2. `Assets/0.Scripts/Framework/UI/Popup/UI_Popup.cs`
3. `Assets/0.Scripts/Framework/UI/UI_Base.cs`
4. `Assets/0.Scripts/Framework/UI/Scene/UI_Scene.cs`
5. `Assets/0.Scripts/Framework/Utils/InitBase.cs`

확인된 문제와 재현 후보:

- `ShowPopupUI`가 이미 열린 인스턴스를 스택에 다시 Push한다. 같은 팝업을 두 번 열고 한 번 닫으면 화면은 사라지지만 스택에 남을 수 있다.
- 팝업 Canvas 순서는 최초 Init에서만 설정된다. 캐시된 팝업을 다시 열 때 표시 순서와 스택 최상단이 어긋날 수 있다.
- Close할 때 `_order--`하지만 재사용 Show에서는 대응되는 순서 증가가 없다.
- CachePopupUI가 캐시 준비와 열린 팝업 스택 등록을 섞고 있다.
- UI 생성 경로에서 Resource.Instantiate가 null을 반환했을 때 즉시 역참조하는 곳이 있다.
- 캐시된 Unity 객체가 외부에서 파괴된 경우의 재생성·스택 정합성도 확인해야 한다.

위 내용은 수정 전 분석 기록이다. 2026-10-30에 실제 API 호출로 재현하고 수정했다. 재요청 시 앞으로 올리기를 관리 동작으로 적용했으며 세부 계약과 검증은 0절과 [UIPopups.md](UIPopups.md)를 참조한다. 팝업의 게임 내용·보상 규칙은 만들지 않았다.

수정 후 확인할 결과: 중복 스택 없음, 마지막에 연 팝업이 실제 최상단, 캐시 재사용 후에도 순서 일치, CloseAll/Clear 후 열린 수 0, 리소스 생성 실패 시 잘못된 캐시·스택 항목 없음. 이전 36개 테스트도 유지한다.

## 6. 이후 남은 일과 미검증 범위

- SceneManagerEx·LoadScene의 공통 정리와 사운드 사용 안정성은 0절에서 완료했다. Unity SceneManager를 직접 부르는 외부 경로는 공통 정리를 자동 적용받지 않는다. 다중 씬별 정리와 동시 전환 큐·취소는 미구현이다.
- 음량 저장·오디오 일시정지·BGM 연속 재생 같은 추가 정책은 확정하지 않았다.
- 실제 6회 보스·보상·게임 일시정지·승패 시스템과 공통 매니저 연결은 후속 작업이다.
- Addressables 기본 그룹은 정리 시점에 `m_SerializeEntries: []`다. 실제 SkillData 연결 완료가 아니다.
- 리소스 독립 리뷰의 추가 검증 제안: 별칭 주소의 참조 수, Clear 직후 같은 주소 재요청, 완료 콜백 예외, 조회 핸들별 참조 수 직접 계측.
- 실제 운영 카탈로그·번들, 전체 게임 데이터, Android 빌드·실기기, 성능·시각 품질은 미검증이다.

## 7. 테스트 기록과 재실행 주의

1·2단계 당시 실행 기록은 **2026-10-28 Unity 컴파일 성공 / PlayMode 36 통과, 실패 0, 생략 0**이다. 아래는 당시 기록이며 최신 실행은 0절의 72개 결과를 참조한다.

| 파일 | 테스트 수 |
|---|---:|
| CoreInitializationTests.cs | 4 |
| GamePreparationTests.cs | 16 |
| LoadSceneStartupTests.cs | 1 |
| ResourceLoadingTests.cs | 15 |

모두 `Assets/Tests/PlayMode/` 아래에 있다. 새 수정 후에는 다시 실행하고 실제 수와 결과를 보고한다.

### 환경 초기화 후 필요한 것

- 소스·기획·AGENTS·인수인계는 Git에 보존한다. `.utmp`, OS 임시 폴더, Library, 이전 채팅이나 스킬 설치 상태는 사라질 수 있다.
- 테스트 XML·로그가 없으면 재실행한다. 기록이 남아 있어도 새 수정의 통과 증거로 쓰지 않는다.
- 기존 `.utmp/ResourceLoading/validation-path.txt`나 그 안의 임시 경로가 내일도 존재한다고 가정하지 않는다.
- 설치된 Unity 경로도 재확인한다. 당시 실행 파일은 `C:/Program Files/Unity/Hub/Editor/6000.5.0f1/Editor/Unity.exe`였다.

### 사용자가 열어 둔 Unity가 있을 때

- 같은 프로젝트 경로에 배치 Unity를 겹쳐 열지 않는다. 사용자 에디터를 임의 종료하지 않는다.
- 별도 복사본이 필요하면 **짧은 임시 경로**를 사용한다. 저장소 아래 긴 복사본 경로는 패키지의 .uss/.uxml 경로 제한 오류를 실제로 일으켰다.
- `Assets`, `Packages`, `ProjectSettings`의 현재 상태를 복사한다. 캐시 활용 여부는 환경에 맞추되 테스트 대상 코드가 원본과 동일한지 확인한다.
- `Tools → Play From Title`이 켜져 있으면 테스트 씬 대신 타이틀이 열려 결과가 나오지 않을 수 있다. 원본 설정을 임의 변경하지 않는다.
- 이전 자동 검증에서는 **복사본의** `Framework/Editor/PlayFromTitle.cs` 정적 생성자에 `if (Application.isBatchMode) return;`을 추가해 우회했다. 원본 코드는 수정하지 않았다. 다음 복사본에서도 필요 여부를 확인한다.
- Windows 샌드박스에서 Unity 라이선스 IPC 연결이 거부된 적이 있다. 이 경우 환경 제한으로 분류하고 정식 권한 절차를 사용한다. 라이선스 문제를 소스 오류로 처리하지 않는다.
- Unity Test Framework의 LogAssert는 RegexOptions를 보존하지 않는 구현이 있다. 정규식은 옵션 없이 맞는 패턴이나 인라인 옵션을 쓰고, 기대 로그 불일치를 실제 런타임 결함과 구분한다.

배치 실행 예시(프로젝트·설치 경로·출력 경로는 확인 후 대입):

```powershell
& $unityExe -batchmode -nographics -projectPath $validationProject `
    -runTests -testPlatform PlayMode -testResults $resultsXml -logFile $editorLog
```

종료 코드뿐 아니라 XML의 통과/실패/생략 수와 컴파일 오류를 확인한다. 배치 앱이 비동기로 실행되는 경우 런처 반환만 보고 통과로 처리하지 않는다. 검증 후 원본 씬·설정이 불필요하게 바뀌지 않았는지도 확인한다.

## 8. 사용자에게 안내할 직접 확인 절차

1·2단계의 확인 방법(최신 UI 확인 절차와 테스트 수는 0절):

1. Unity에서 컴파일 완료를 기다리고 Console의 새 빨간 오류가 없는지 확인한다.
2. `Tools → Play From Title`이 켜져 있다면 사용자가 테스트 중 잠시 끄고, 종료 후 원래대로 돌린다.
3. `Window → General → Test Runner → PlayMode → Run All`을 실행한다. 2026-10-30 최신 기준 113개가 모두 통과하면 정상이다.
4. 타이틀에서 시작해 로딩을 거쳐 MainTestScene으로 진입하는지 확인한다.
5. 실행 중 Hierarchy의 DontDestroyOnLoad 아래 @Managers가 하나이고 자식 @Pools, @SoundRoot 아래 Bgm·Effect가 있는지 확인한다.
6. 재시작·타이틀 복귀 후에도 매니저가 중복 생성되지 않는지 확인한다.
7. 문제가 있으면 Console의 **첫 번째 빨간 오류와 상세 내용**을 요청한다. 새 UI 작업 완료 후에는 해당 팝업을 여닫는 구체적인 확인 절차를 추가한다.

현재는 빈 데이터로 테스트 시작을 허용하므로 정상 시작 확인을 위해 requireSkillData를 켤 필요는 없다. 사용자가 직접 정상 동작을 확인했다고 아직 보고하지 않은 항목을 “사용자 검증 완료”로 기록하지 않는다.

## 9. 다음 세션 시작 문구

> AGENTS.md와 docs/AI/Handoff.md를 먼저 읽고 현재 Git 상태를 확인해. 완료된 초기화·리소스 작업은 보존하고, 다음 순서인 UIManager 팝업 중복 열기와 표시 순서부터 이어서 진행해. 완료 후 내가 직접 확인할 방법도 설명해.

이 문구는 사용자가 실제로 이어서 진행을 요청할 때 사용할 예시다. 인수인계 문서를 읽는 것만으로 관련 없는 새 작업을 시작하지 않는다.
