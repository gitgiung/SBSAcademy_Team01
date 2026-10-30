# GiungTestScene 승패·재시작·타이틀 복귀

## 현재 경로와 추후 복원

현재 GiungTestScene의 GameManager에는 **Restart Scene = GiungTestScene**을 지정했다. 테스트 결과 화면의 Restart는 같은 테스트 씬을 새로 로드한다. 일반 GameManager의 기본값은 **LoadTestScene**이며 기존 타이틀 → 로딩 → MainTestScene 흐름은 변경하지 않았다.

**정식 게임 연결 시 변경할 곳:** GiungTestScene에서 GameManager 객체 → Inspector의 **Restart Scene을 LoadTestScene으로 변경**한다. 이후 경로는 **LoadTestScene → Managers.PrepareGame → MainTestScene**이다. 코드 기본값만 바꾸어도 이미 직렬화된 씬 값은 바뀌지 않으므로 Inspector 값도 반드시 변경한다. `Assets/0.Scripts/Manager/GameManager.cs`의 **TODO(MainSceneIntegration)** 주석에 같은 복원 절차를 표시했다.

테스트 재시작을 위해 Build Profiles의 기존 Title/Load/Main 뒤에 GiungTestScene을 추가했다. 기존 씬 순서와 시작 씬은 보존했다. Define.EScene에도 Giung을 끝에 추가해 기존 enum 값을 유지했다.

## 결과 연결

- 실제 Player.Died는 사망 시 한 번 발생하고 GameManager가 패배 신호로 받는다. 단순 비활성화를 사망으로 간주하지 않는다. 기존 Player Dead 테스트 버튼도 유지한다.
- 기존 Victory 테스트 버튼과 `HandleBossDefeated()`는 명시적 최종 승리 신호다. 일반 보스 한 마리 처치를 최종 승리로 자동 연결하지 않았다. 실제 보스 단계 진행자가 최종 승리를 결정하는 연결은 후속이다.
- 같은 프레임에 사망·승리 신호가 오면 기존처럼 패배가 우선이며 결과 확정 후 추가 신호는 무시한다.
- GameManager는 결과 상태와 timeScale 0을 먼저 확정하고 로컬 StateChanged 알림을 보낸다. CombatPopupController는 두 전투 팝업을 숨기고 정지 소유권을 정리하며 일시정지 버튼을 비활성화한다. 결과 화면의 첫 사용 가능한 버튼에 포커스를 둔다.
- Restart/To Title는 시간 배율을 1로 복원하고 기존 Managers.Scene 전환으로 풀·UI·사운드를 정리한다. Managers와 리소스 캐시는 유지한다. 목적지 검증 실패 시 이전 시간과 전환 입력 가드를 복원해 재시도할 수 있다.

## Unity에서 직접 확인

1. 컴파일을 기다리고 Console의 새 빨간 오류가 없는지 확인한다. `Tools → Play From Title`을 직접 씬 확인 동안 잠시 끈다.
2. `Assets/9.Scenes/GiungTestScene.unity` → Play. 기존 **Victory** 버튼을 누르면 승리 화면, **Player Dead** 테스트 버튼을 누르면 패배 화면이 보여야 한다. 각각 새 판에서 확인한다. 실제 사망은 Play 중 Player Target의 PlayerData 현재 체력을 낮추고 몬스터와 충돌해 확인할 수도 있다. runtime 체력 변경을 씬 설정 변경으로 저장하지 않는다.
3. 결과 화면에서 **Restart**: GiungTestScene이 새로 로드되고 결과·전투 팝업이 닫힌 상태, timeScale 1, 새 플레이어와 진행 시간으로 시작해야 한다.
4. 결과 화면에서 **To Title**: TitleTestScene으로 복귀하고 시간이 1로 복원돼야 한다. 타이틀 시작 터치 영역을 누르면 기존 LoadTestScene → MainTestScene으로 간다. 타이틀 시작 경로를 Giung으로 바꾸지는 않았다.
5. 팝업을 연 상태에서 결과가 발생하는 경우는 Test Runner의 **GameResultFlowTests → VictoryClosesBothPopupsAndFocusesResult → Run Selected**로 확인한다. 두 창이 닫히고 결과 버튼에 포커스가 생기며 전투 정지 0이 유지돼야 한다. 화면의 Victory 테스트 버튼은 모달 배경이 가리므로 클릭해서 우회하지 않는다.
6. `Window → General → Test Runner → PlayMode → GameResultFlowTests → Run Selected`: **8개**, `Run All`: **113개 통과**가 정상이다. 확인 후 Play From Title 설정을 복원한다.
7. 추후 정상 경로를 직접 확인하려면 Play 전 Restart Scene을 **LoadTestScene**으로 변경하고 결과 → Restart → MainTestScene 진입을 확인한다. 현재 테스트를 계속한다면 값을 GiungTestScene으로 돌린다. 이 복원 경로는 자동 테스트에서도 확인했다.

오류가 있으면 Console의 **첫 빨간 오류와 스택**, 실패한 테스트 이름·상세 결과를 확인한다. 재시작이 안 되면 GameManager의 Player·두 결과 Panel·Restart Scene 참조와 `File → Build Profiles → Scene List` 등록을 확인한다. 결과 창이 안 보이면 실제 사망 신호인지 테스트 버튼인지, 결과가 Playing에서 변경됐는지, 비활성 패널 참조가 올바른지 확인한다.

## 2026-10-30 검증

Unity 6000.5.0f1 컴파일 및 전체 PlayMode **113/113 통과**, 실패·생략 0, 종료 코드 0. 기존 105개 + 신규 8개다. 수정 전 6개 중 4개 실패를 재현했다. 실제 사망·1회 알림, 동시 판정, 결과 중 전투 팝업 정리와 포커스, Giung 재시작, 타이틀 정리, 실패 후 재시도와 정상 Load→Main 경로 복원을 검증했다.

사용자 Unity와 분리된 검증 복사본에서 실행했다. 새 결과 UI·보상·실제 보스 단계/최종 승리 규칙은 만들지 않았다. Android 빌드·실기기 및 사용자의 직접 시각 확인은 미검증이다.
