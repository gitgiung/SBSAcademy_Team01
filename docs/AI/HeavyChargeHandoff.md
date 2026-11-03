# 강공격 차징 작업 재개 인수인계

작성일: 2026-11-03. 사용자가 컴퓨터를 끄기 위해 현재 작업을 중단하고 다음 Codex에 넘기도록 요청했다. 이 문서는 **미완성 작업 상태**이며 기능 완료 보고가 아니다.

## 확정된 요구와 범위

- 정식 SkillDatatable은 아직 준비되지 않았다. 데이터/획득·장착 연결보다 차징 기반을 먼저 만든다. 새 강공격 스킬 ID·피해량·임시 정식 테이블은 만들지 않는다.
- 차징 중 이동 차단, 현재 조이스틱/이동 키로 방향만 변경. 최대 단계에서도 홀드를 유지하고 버튼을 놓을 때 결과를 확정한다.
- MP는 시작 순간 한 번 차감. 방어/회피/피격 취소에 MP 반환 없음. 시작 도중 동기 MP 알림에서 비활성화/사망/정지되어 실제 시작하지 못한 비용만 복구한다.
- 사용자 승인 테스트 수치: MP10, 단계 진입 시점0/.4/.8초, 단계 누적 배율1.1/1.2/1.7. 저장할 StageBonuses는 증분.1/.1/.5다. Inspector에서 수정 가능해야 하며 최종 게임 밸런스 수치가 아니다.
- 차징 중 약공격과 자동 패시브 차단. 방어·회피가 실제로 시작하면 즉시 취소. 실패한 회피 입력은 차징을 유지한다.
- 피격은 기존 HP 처리를 유지. SuperArmor=false일 때 실제 HP 감소 시 취소, true면 피해를 받되 차징 유지. 사망·비활성화는 항상 취소. 피격 경직 시스템 자체는 아직 별도 구현하지 않았다.
- 팝업 일시정지 중 시간/표시를 유지한다. 정지 중 해제 또는 포커스 상실은 취소해 복귀 후 공격을 예약하지 않는다. 이는 구현상 보수적 선택으로 문서화한 동작이다.
- 이번 결과는 `HeavyChargeReleased` 이벤트와 실행 ID/단계/배율/방향/시간 스냅샷까지다. 실제 공격 효과/공격 애니메이션/사운드·햅틱/그로기는 후속이다.
- 모바일은 현재 씬에 존재하는 C_Attack 사용. PC Q는 임시 검증 입력(기존 Player/HeavyAttack이 있으면 복제하여 사용). 원본 InputAction 에셋은 수정하지 않는다. C_Attack 이름은 실제 씬에서 확인했고 별도 사용자 질문은 하지 않았다.

## 현재 Git 상태

확인 당시 브랜치 feat/PlayerSkill, HEAD3da8308 `테스트용 불꽃 탄환`. 이전 ccac76f 이후 사용자가 앞선 작업을 커밋했다. 현재 상태는 다음 세션에서 다시 확인한다. 이번 차징 작업의 커밋은 만들지 않았다.

수정 파일:

- Assets/0.Scripts/Game/Player/Player.cs
- Assets/0.Scripts/Mobile/PlayerController2D.cs

새 파일(미추적, 아직 meta 없음):

- Assets/0.Scripts/Game/Player/PlayerHeavyCharge.cs
- Assets/0.Scripts/Game/Player/PlayerHeavyAttackButton.cs
- Assets/0.Scripts/Game/Player/Player.HeavyCharge.cs
- Assets/Tests/PlayMode/PlayerHeavyChargeTests.cs
- Assets/Tests/PlayMode/PlayerHeavyChargeIntegrationTests.cs
- docs/superpowers/plans/2026-11-03-heavy-charge-foundation.md

이외 Handoff와 이 문서는 종료 정리에서 추가/수정했다. 새 파일이 미추적이어도 다음 세션의 정상 작업물이므로 삭제하지 않는다.

## 작성한 코드와 API

### PlayerHeavyCharge.cs

- `[Serializable] PlayerHeavyChargeSettings`: Configured(default false), MPCost, StageTimes[], StageBonuses[], SuperArmor. 기본 배열은 비어 있으므로 씬에 명시적으로 테스트 설정을 넣어야 한다.
- `readonly struct HeavyChargeRelease`: ChargeId, Stage, Elapsed, Multiplier, Direction 읽기 전용 속성.
- internal PlayerHeavyCharge(PlayerData, Func<bool> contextValid): Player별 실행 상태. TryBegin(settings, direction), Tick(delta, direction), TryRelease(id, out result), Cancel().
- 시작 전 설정 검증/배열 복제. IsStarting 예약으로 MP 알림 재진입 거절. 비용 지불 뒤 실행 ID/생명주기/정지 재검사. 실제 시작 실패만 환불. 차징 중 Cancel은 환불하지 않음.
- 최대3단계/누적 가산, 최종 시점에서 시간 고정, 중립 입력이면 마지막 방향 유지. 오래된 ID/중복 해제 거절. 엔진 자체는 정지 중 TryRelease를 거절하고 보존하며, Player wrapper가 정지 중 입력 해제를 취소로 처리한다.

### Player.HeavyCharge.cs / Player.cs

- Player를 `sealed partial`로 바꾸고 차징 전용 필드/API를 별도 파일에 분리했다. 기존 주 MonoBehaviour의 GUID와 필드 참조는 보존한다.
- 직렬화 필드 `heavyAttackButton`, `heavyChargeSettings`. 공개 읽기 속성 IsHeavyCharging, HeavyChargeId/Stage/Multiplier/Progress, CanMove, LastHeavyCharge. 이벤트 HeavyChargeReleased.
- 공개 TryBeginHeavyCharge(), TryReleaseHeavyCharge(int), CancelHeavyCharge(int). private InitializeHeavyCharge/EnableHeavyInput/TickHeavyCharge/ResetHeavyCharge/RefreshHeavyIndicator.
- Awake/OnEnable/OnDisable/DisposeInputActions에 연결했다. 약공격 중 거절, 차징 중 약공격 거절. CanAcceptGameplayInput은 차징 중에도 방어·회피를 허용하고 IsStarting 및 시작 예약 중에는 차단한다.
- PC 이동은 CanMove=false면 Tick을 건너뛴다. TryDash 성공 후 ResetHeavyCharge, TryParry 성공 경로에서 ResetHeavyCharge. 실제 피해 후 SuperArmor/죽음에 따라 취소.
- Update의 TickHeavyCharge 호출은 현재 이동·방어/회피 입력 처리보다 앞이다. 같은 프레임의 Q 해제+방어 입력 우선순위와 패시브 재개 시점은 통합 테스트/리뷰 시 확인할 것.
- Q를 누를 때 시작 ID를 보관하고 해당 소스의 해제에만 반응한다. 정지 중 해제를 놓치면 복귀 때 홀드 상태로 검사해 취소한다. OnApplicationFocus(false)/OnApplicationPause(true)에서 정리.
- **현재 PlayerView.SetHeavyChargeIndicator(...) 호출만 있고 정의가 없어 컴파일이 막히는 미완성 상태.** 실제 변경 후 컴파일을 아직 실행하지 않았으며 정적 코드 확인으로 발견한 미구현 지점이다.

### PlayerHeavyAttackButton.cs

- PointerDown/Up/Cancel 처리, pointerId와 차징 실행 ID를 보관한다. 다른 손가락/과거 차징의 늦은 해제는 새 차징을 해제하지 않는다. 버튼 비활성화 시 자신이 시작한 ID만 취소.

### PlayerController2D.cs

- raw joystick 기반 AimDirection 추가. 기존 FixedUpdate의 이동 차단 기준을 CanAcceptGameplayInput에서 CanMove로 바꿨다.
- StopMovement는 기존 joystickDirection을0으로 만들지만 AimDirection은 실제 조이스틱을 읽으므로 이동 정지 중에도 방향 조절 가능하게 의도했다. 실제 모바일 입력 검증은 아직 없다.

## 바로 이어서 구현할 것

1. PlayerView에 `SetHeavyChargeIndicator(bool visible, Vector2 direction, float progress, int stage)` 구현. 테스트는 활성화된 LineRenderer를 기대한다. 기존 PlayerView가 owner Transform을 현재 필드로 저장하지 않으므로 필요한 참조를 보존한다. 길이/색상으로 단계·진행을 보여주는 임시 조준 화살표를 계획했다. 실제 피해 범위로 오해시키지 않도록 문서화한다. 생성한 자식·Material을 Reset/Dispose 시 숨김/정리한다. Animator 전체 재생성은 하지 않는다.
2. MainTestScene Player에 heavyAttackButton=C_Attack의 Button(fileID452539071), heavyChargeSettings.Configured=1/MPCost10/StageTimes[0,.4,.8]/StageBonuses[.1,.1,.5]/SuperArmor=false 추가. 실제 확인한 C_Attack GameObject452539069. 전체 씬 저장 없이 최소 패치하고 GUID/기존 참조 보존.
3. 새 **런타임3개+테스트2개, 총5개**의 meta를 Unity에서 준비한다. 새 GUID 재사용 금지. 기존 Player.cs.meta는 변경하지 않는다.
4. 임시 검증 복사본에 최신 변경 소스/테스트/씬을 반영해 컴파일 후 단위·통합 테스트 실행. 첫 구현에는 아직 통과 결과가 없다.
5. 통합 테스트에 자동 패시브 중단/복귀, 실제 모바일 이동 정지·raw 조준, 사망 및 입력 해제/정지 엣지 케이스를 보강한다. 현재 작성한 통합 테스트는 모든 요구를 다 덮지 않는다.
6. 관련 회귀 실행 후 독립 리뷰(해당 스킬의 reviewer subagent 사용 가능), 발견 문제 수정/재검증. 실제 원본 씬/Android를 검증 완료라고 하지 않는다.
7. 강공격 직접 확인 문서와 Handoff를 실제 결과로 업데이트. 정식 스킬 테이블 연결은 여전히 후속.

## 테스트·환경 증거

- Unity6000.5.0f1. 원본과 같은 프로젝트의 두 번째 에디터는 열지 않는다. 현재 종료 정리 때 실행 중 Unity 프로세스는 보이지 않았다. 컴퓨터 종료/재시작 명령은 실행하지 않았다.
- 사용했던 별도 검증 프로젝트: C:/Users/User/AppData/Local/Temp/sb-bullet-814e7b. 경로 기록 .utmp/FlameSkill/validation-path.txt. 재부팅 후 임시 폴더가 없어졌으면 원본에 두 번째 에디터를 여는 대신 검증 복사본을 다시 준비한다.
- Unity exe: C:/Program Files/Unity/Hub/Editor/6000.5.0f1/Editor/Unity.exe. 기존 실행 방법은 Hidden Start-Process, batchmode/nographics/runTests/PlayMode, testFilter. runTests에는 quit를 같이 넣지 않는다. 접근 정책상 실행 시 도구 승격이 필요했으며 원본 에디터는 건드리지 않았다.
- temp 프로젝트는 필요한 최소 패키지/리소스 복사본이며 PlayFromTitle에만 batch guard가 있다. 원본 전체 프로젝트/기기 검증과 구분한다. 원본 PlayFromTitle의 사용자 옵션은 바꾸지 않는다.
- **실제 실행한 차징 테스트는 구현 전 단위12개, 통과0/실패12.** 차징 타입이 없다는 SetUp assertion으로 의도한 부재를 확인했다. 그 뒤 엔진/Player 연결을 작성했지만 아직 temp로 반영·재실행하지 않았다. 새 통합 테스트는 미실행.
- 그때 결과는 temp 프로젝트 heavy-red.xml/heavy-red.log. 종료 정리에서 .utmp/HeavyCharge에도 복사하며 이 문서에 핵심 결과를 영속 기록한다. 생성물은 Git에 추가하지 않는다.
- .utmp/HeavyCharge에는 Player.before.cs/View.before.cs/Mobile.before.cs/Scene.before.unity가 있다. 자동 덮어쓰기/되돌림용이 아니라 이번 변경과 이전 사용자 작업을 구분하기 위한 자료다.
- 앞선 기능의89/89, 보스 HP55/55는 과거 별도 검증 기록이며 차징 변경의 결과로 재사용하지 않는다.

## 다음 사용자 직접 확인 절차 (구현·자동 검증 완료 후에만)

Tools > Play From Title 끄기 → MainTestScene Play → C_Attack 또는 Q 홀드. MP10이 한 번 줄고 이동 없이 방향 조절,0/.4/.8초 단계 진행과1.1/1.2/1.7배 결과를 확인한다. 해제 시 표시가 사라지고 LastHeavyCharge/이벤트 결과가 한 번 갱신되어야 한다. 실제 강공격 피해가 없는 것은 이번 기반 범위다. 차징 중 패시브 중단, 방어/회피 취소·무환불, 피격/사망/정지/복귀를 확인한다. PC 이동 테스트는 PlayerController2D를 비활성화한다.

지금은 위 절차가 동작할 완료 상태가 아니다. 오류 시 우선 미구현 View 메서드와 Console 컴파일 오류를 해결하고, C_Attack/Button·Configured/배열 설정·입력 소스·포커스 취소를 확인한다. 사용자에게 같은 합의 사항을 다시 묻지 않는다.
