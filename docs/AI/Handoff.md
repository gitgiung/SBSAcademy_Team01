# 작업 인수인계

## 2026-11-03 보유/장착 모델·일시정지 상태창·음량 옵션

- 사용자 참고 이미지의 상태창 요청을 구현했다. 첫 이미지는 배치/색상, 두 번째는 영역 설명으로 사용했다. 기존 옵션 창이 없음을 확인한 뒤 사용자에게 음량 옵션 포함 승인을 받았다. `main`, HEAD `f3fd42c` 유지, 커밋/브랜치 전환 없음.
- PlayerData.Loadout과 PlayerLoadout 추가: 공격8칸 중 기본6·해금2, 전략5칸, 유물/증강 각60개, 보유 스킬만 약/강/전략 버튼에 하나씩 장착, 제거 시 장착 참조 정리. 기존 Data.SkillData를 재사용하며 새 전역 인벤토리 매니저를 만들지 않았다. 실제 스킬/유물을 자동 지급하지 않는다.
- 기존 CombatPopupController.ShowPause가 같은 씬 Player를 찾아 pause ContentRoot에 PlayerStatusPanel을 한 번 구성한다. 실제 스탯/보유 데이터 이벤트로 갱신하고 닫기/소스 교체 시 구독 정리. 남색 패널·크림 스탯·색상별 제목·8/5 슬롯·가로 목록·톱니 옵션·우하단 뒤로가기 배치를 참고 이미지에 맞췄다. 모델이 없는 스탯은 —로 표시한다.
- 기존 CloseButton/팝업 정지 소유권/스택을 유지했다. 옵션은 동일 팝업 내부 전환이며 옵션 뒤로가기는 상태창으로만 돌아온다. 옵션 진입 시 전체 음량 Slider, 복귀 시 기존 뒤로가기 버튼으로 EventSystem 선택을 옮겨 키보드/패드 탐색을 유지한다.
- SoundManager에 전체(AudioListener)·BGM·효과음 채널 음량 API를 추가했다. 기존 AudioSource와 씬 재초기화에 설정을 반영한다. 앱 재시작 영구 저장은 추가하지 않았다.
- 씬/프리팹/기존 meta/패키지/설정/엑셀 변경 없음. MainTestScene과 CombatUI.prefab SHA-256이 시작값과 동일함을 확인했다. 상태창은 런타임 생성이므로 비실행 원본 프리팹에는 기존 기본 패널이 보일 수 있다.
- 실제 테이블을 이번에 발견했다: SkillDatatable.xlsx(약11/강5/전략10), Strengthentable.xlsx. 이름·일부 등급/속성은 있으나 비용·계수·애니메이션·프리팹 대부분 비어 있다. 보유/장착/조회만 구현했으며 기본 Bullet을 보유 스킬 전체의 패시브로 바꾸거나 액티브 버튼/약공격 타격을 연결하지 않았다. Fusion10011 중복 정의와 원본 Cooldawn 컬럼 매핑에 향후 주의한다. 이전 ‘xlsx 미발견’ 기록은 과거 상태다.
- TDD: 새 테스트7개 모두 기존 코드에서 실패 확인. 첫 구현에서 새 UI helper meta GUID 길이 입력 오류로 파일이 임포트되지 않아 컴파일 실패했고 새 meta만 올바른32자리로 수정했다. 리뷰에서 옵션 선택 포커스 문제를 찾고 새 Submit/Move 테스트 실패를 확인한 뒤 수정. 실제 CombatUI 프리팹 통합 테스트도 추가했다. 독립 재리뷰에서 추가 수정 필요 결함 없음.
- 최종 검증: Unity6000.5.0f1 임시 검증본에서 **111/111 통과**, 실패/생략0. 기존60 + PlayerStatusTests9 + CombatPopupTests13 + UIPopupTests17 + 씬 비의존 사운드11 + 그래픽 렌더1. C# 컴파일·git diff --check·최종 소스/테스트 해시 일치 확인.
- 검증 제한: 초기에 사운드 씬 전환2개까지 확장한 실행은112개 중110통과/2실패. 최소 복사본의 MainTestScene 입력/스폰 참조 의존성이 누락되어 Player/EnemySpawnDirector 설정 오류가 발생했다. 원본 씬을 고치거나 실패를 숨기지 않고 이2개를 최종 비씬 회귀 집합에서 분리했다. 전체 원본 MainTestScene 실행·실제 터치/기기/Android 검증은 하지 않았다.
- 그래픽 QA: 그래픽 사용 검증 Unity의 오프스크린 카메라에서 빈 상태·보유 테스트 데이터·목록 끝 스크롤·음량 화면 PNG를 렌더해 직접 확인했다. 테스트 데이터는 검증 픽스처에만 존재한다. 첫 캡처에서 잔존 검증 씬 배경이 분홍색으로 나와 미리보기 카메라를 UI 레이어/명시적 배경으로 분리했다. 첫 목록 폭과 스탯 줄바꿈을 보완 후 재렌더했다. 콘텐츠/뷰포트 폭 비1.5, 스크롤바 크기2/3 및 마지막 항목 표시를 확인했다.
- 영속 테스트: Assets/Tests/PlayMode/PlayerStatusTests.cs(9개). 계획 docs/superpowers/plans/2026-11-03-pause-status-loadout.md. 직접 확인/API 설명 docs/AI/PlayerStatus.md. 결과 `.utmp/PlayerStatus/red.xml`, `review-red.xml`, `final.xml`, `final.log`, 미리보기 status-empty/populated/scroll-end/options.png. 검증 프로젝트 위치 validation-path.txt. 임시 렌더 헬퍼는 같은 폴더 StatusPreviewTests.cs이며 원본 런타임/테스트 에셋에는 넣지 않았다.
- 직접 확인: Unity 컴파일 완료 → MainTestScene → Play(필요하면 Tools > Play From Title 끄기) → 오른쪽 위 기존 일시정지 → 스탯/공격6빈칸+잠금2/전략5/유물·증강 가로 목록 확인 → 톱니 → 전체/BGM/효과음 조절 → 옵션 뒤로가기에서 전투 계속 정지 → 상태창 뒤로가기에서 재개 확인. 반복 열기도 확인한다.
- 보유 표시 확인: Play 중 PlayerData.Loadout.Attack Skills 첫 항목을 실제 표의 DataId10001/Name불꽃 탄환으로 설정 후 다시 열면 이름이 보인다. 자동 획득/발사 확인이 아니라 임시 표시 확인이며 저장하지 않는다. 정상 획득 코드는 Loadout.TryAddAttackSkill 등 API를 호출해야 Changed가 즉시 갱신한다. 원본 아이콘 키가 비었거나 사전 로딩되지 않았으면 이름 표시로 대체한다.
- 오류 시 Console 첫 오류, CombatUI Controller의 Pause Popup ContentRoot/CloseButton/Status Player와 PlayerData.Loadout을 확인한다. 데이터 미보유와 잠금은 오류가 아니다. 한글 폰트는 기존 Assets/UI/Fonts의 NanumGothic Combat SDF를 사용한다.
- 다음 작업: 3단계 남은 실제 스킬 실행/약공격 버튼·애니메이션·타격 연결. 첫 스킬의 MP 비용·계수·애니메이션/프리팹 등 미정 값을 확정해야 하며 임의로0비용/효과를 채우지 않는다. 실제 획득/성장/유물·증강 해금 효과와 MP 부족 점멸도 후속 범위다.

## 2026-11-03 HP 입력 버그 수정 및 MP·EXP HUD 연결

- 사용자 요청: Mobile UI/PlayerHUD의 HP·MP·EXP를 PlayerData → PlayerView 흐름으로 통일하고 이동 방향에 HP 표시가 변하는 버그 수정. 시작 브랜치 `main`, HEAD `f3fd42c`; 기존 코드·문서·씬 사용자 변경 보존, 커밋/브랜치 전환 없음.
- 원인 재현: MainTestScene의 Player_HP/MP/EXP Slider가 Interactable=true, Navigation=Automatic이었다. 기존 PlayerView에 연결한 HP Slider 선택 후 좌측 Move 이벤트를 보내면 모델 HP는 50인데 표시가 0.5→0.4로 감소함을 새 테스트로 확인했다. 선택 여부에 따라 재현되는 UI 탐색 입력 문제이며 체력/이동 수식 오류가 아니다.
- 수정: PlayerView에서 세 Slider를 transition=None, navigation=None, interactable=false로 표시 전용 설정하고 모델 값을 SetValueWithoutNotify로 반영한다. 기존 레이아웃·색상·채움 방향 및 사용자 지정 Slider 범위는 유지한다. 최대치 0·비정상 수치는 빈 표시로 처리한다.
- MP는 기존 PlayerData.MPChanged를 구독, EXP는 PlayerData에 maxEXP/CurrentEXP 및 EXPChanged·InitEXP·SetEXP(current,maximum)를 추가해 같은 흐름으로 표시한다. 초과 EXP는 모델에 유지하고 바만 100%로 제한한다. EXP 요구량 기본 0이며 획득/드랍·레벨업·보상 규칙은 추가하지 않았다.
- Player의 기존 healthSlider와 PlayerView 생성자 API를 유지했다. 선택 MP/EXP 참조와 BindResourceSliders를 추가하고 재활성화 때 RefreshResources로 갱신한다. PlayerView.Dispose는 세 데이터 이벤트를 모두 해제한다. 참조가 없는 기존 씬도 안전하게 동작한다.
- MainTestScene의 Player 컴포넌트에 mpSlider=1395659084, expSlider=1801941379 참조 두 줄만 추가했다. 기존 healthSlider=496842392 및 PlayerHUD 자식 Slider fileID를 확인했다. 시작 씬 원본에 정확히 두 줄을 삽입한 결과와 종료 파일이 일치하며 기존 Grid→Ground 사용자 변경도 유지된다. GiungTestScene·프리팹·기존 meta·설정·패키지·InputAction 에셋 수정 없음.
- 검증: 새 HUD 테스트 12/12가 기존 코드에서 실패한 뒤 수정. Unity 6000.5.0f1 임시 검증 프로젝트에서 **60/60 통과**, 실패/생략 0(HUD 12 + MP·행동 19 + 탄환 6 + Core 4 + 씬 비의존 적 풀링 19). C# 컴파일 및 git diff --check 통과. 독립 코드 리뷰에서 수정 필요 결함 없음. 소스/씬 해시가 검증 복사본과 일치한다.
- 범위: 실제 MainTestScene 실행 화면·터치·Android는 이번에도 검증하지 않았다. 씬 참조는 파일ID 정적 검사, UI 동작은 실제 uGUI 컴포넌트 기반 PlayMode 테스트로 검증했다. Player 재활성화 표시 갱신은 코드로 확인했으며 별도 씬 통합 테스트는 없다. EXP 성장 시스템 및 MP 부족 점멸은 후속 범위다.
- 테스트 원본: Assets/Tests/PlayMode/PlayerHudTests.cs. 결과 `.utmp/PlayerHUD/red.xml`, `final.xml`, `final.log`, 검증 위치 validation-path.txt. 계획 docs/superpowers/plans/2026-11-03-player-resource-hud.md.
- 직접 확인 1: Unity 컴파일 완료 → MainTestScene → Play(자동 타이틀 시작이면 Tools > Play From Title 옵션 확인) → PC 검사에서는 PlayerController2D 끄기 → 적 피해를 받지 않는 상태에서 Mobile UI/PlayerHUD/Player_HP 클릭 후 좌우 이동. HP 값과 채움이 그대로이고, 실제 피격 때만 줄어드는 것이 정상이다. HP/MP/EXP는 입력용 Slider가 아니므로 Play 중 Interactable 해제·Navigation=None이 정상이다.
- 직접 확인 2: Play 중 PlayerData에 테스트용 Max MP=10, Dash MP Cost=3, MP Recovery Per Second=0 지정 → 컴포넌트 메뉴 MP > 현재 설정으로 초기화 → MP바가 가득 참 → WASD 이동 중 Left Shift 회피 → MP=7일 때 70% 표시 확인. EXP는 아직 획득 경로가 없으므로 SetEXP(25,100)→25%, SetEXP(125,100)→100% 표시를 자동 테스트로 확인했다. 레벨업/경험치 획득이 이미 연결됐다고 설명하지 않는다.
- 직접 확인 3: Tools > Play From Title 비활성화 → Window > General > Test Runner → PlayMode → PlayerHudTests → Run Selected. HP 선택/좌우 입력, MP 소모/회복, EXP 표시/잘못된 값, 이벤트 해제 테스트 12개 통과 확인.
- 오류 시 Console, Player의 Health/MP/EXP Slider 참조(각 PlayerHUD 자식의 Slider인지), PlayerData의 최대치와 초기화 여부를 확인한다. Inspector에서 필드를 직접 바꾼 것은 데이터 API 이벤트 호출과 다르므로 MP 초기화 메뉴나 SetEXP API로 확인한다. Play 중 테스트값은 저장하지 않는다.
- 다음 우선순위: 원래 3단계 스킬 보유·버튼 장착 및 약공격 애니메이션/타격 연결. EXP 성장 규칙·실제 테이블 미확정은 유지.

## 2026-11-03 2단계 MP 및 행동 제어 기반 완료

- 사용자가 1단계 탄환 풀링 직접 확인 후 두 번째 단계 진행 승인. `main`, HEAD `f3fd42c` 유지, 커밋/브랜치 전환 없음. 이전 탄환 변경과 MainTestScene 사용자 변경을 보존했으며 씬 SHA-256 일치 확인.
- PlayerData에 현재/최대 MP, 유효 비용 차감, 상한 회복, 자연 회복, MPChanged/MPSpendRejected 알림을 추가했다. maxMP·mpRecoveryPerSecond·dashMPCost·parryMPRecovery는 Inspector 설정값이고 미정이므로 기본 0. 실제 게임 수치를 임의 확정하지 않았다. `MP/현재 설정으로 초기화` 컴포넌트 메뉴로 Play 중 설정을 적용할 수 있다.
- 플레이어 소유 PlayerActionGate와 Player.TryBeginLightAttack(cost,out actionId)/CompleteLightAttack(actionId) 계약 추가. 잠금 중 공통 CanAcceptGameplayInput=false로 이동/다른 스킬 시작/방어/회피를 거절한다. 이전 실행 ID 종료는 새 잠금을 풀지 않는다. 사망/비활성화 때 잠금과 진행 코루틴을 정리한다. 다음 단계 스킬 실행부가 시작 성공 후 ID를 애니메이션 실행에 연결해야 한다.
- PC 이동 및 PlayerController2D의 Rigidbody 이동에 같은 잠금을 적용하고 잠금 시작 즉시 모바일 속도를 정리한다. 회피는 방향·쿨타임·진행 상태 확인 후 MP를 차감한다. MP 동기 알림 중 재진입을 예약 상태로 차단하며, 비활성화로 실제 시작하지 못한 회피는 비용을 복구한다. 이는 강공격 차징 취소 환불 정책과 별개다.
- 자연 회복은 전투 시간으로 진행하며 일시정지 중 멈춘다. 패링 성공 알림을 연결해 접촉이 여러 번 와도 한 번만 설정량 회복한다. 패링/가드 모션 중 자동 발사를 중단하며, 약공격 잠금 중 기존 자동 패시브 발사는 유지한다.
- 범위 제한: 실제 약공격 버튼·애니메이션·타격·장착은 3단계. MP HUD 게이지/부족 점멸은 아직 연결하지 않았으며 데이터 이벤트만 제공한다. 가드 MP 부과 방식·강공격 환불·그로기·미정 수치도 이번에 확정하지 않았다. 씬·프리팹·InputAction 에셋·애니메이션·패키지 변경 없음. 테스트 asmdef에 Unity.InputSystem 참조만 추가했다.
- 검증: 기존 코드에서 새 17개 테스트 실패 확인. 첫 구현 16/17 통과 후 PC 키보드 테스트의 배치 포커스 문제를 분리했다. 테스트가 복제한 InputSettings에만 IgnoreFocus/게임뷰 입력을 적용하고 원래 설정을 복구하도록 보완했다. 리뷰에서 찾은 MP 알림 재진입·비활성화 취소를 추가 2개 테스트로 먼저 실패시킨 뒤 수정했다. 최종 Unity 6000.5.0f1 검증 복사본에서 **48/48 통과**, 실패/생략 0(MP·행동 19 + 탄환 6 + Core 4 + 씬 비의존 적 풀링 19). 독립 재리뷰에서 중요한 잔여 결함 없음. C# 컴파일 및 git diff --check 통과.
- 검증 환경은 이전 BulletPooling 임시 소스 프로젝트 재사용. 원본과 같은 개별 패키지 캐시지만 필요한 패키지만 참조하는 최소 구성이다. 실제 원본 씬·그래픽·전체 패키지 조합·Android 빌드/기기 검증은 별도이며 이전 기록을 현재 결과로 재사용하지 않았다.
- 재현 테스트: Assets/Tests/PlayMode/PlayerResourceActionTests.cs. 결과 `.utmp/PlayerMP/red.xml`, `review-red.xml`, `final.xml`, `final.log`; 검증 위치 validation-path.txt. 문서 계획은 docs/superpowers/plans/2026-11-03-player-mp-actions.md.
- 직접 확인 1: Unity 컴파일 완료 → Tools > Play From Title 비활성화 → Window > General > Test Runner → PlayMode → PlayerResourceActionTests → Run Selected. MP 부족 무소모, 애니메이션 종료 ID, PC/모바일 잠금·복구, 사망/비활성화, 회피 재진입, 중복 패링 회복 테스트 19개가 통과하는 것이 정상이다.
- 직접 확인 2: MainTestScene → Play(자동 Title 시작이면 Tools > Play From Title 옵션 확인) → Player 선택 → PC 검증 시 PlayerController2D 비활성화(사용자 확인 방법 반영) → PlayerData에 테스트용 Max MP=10, Dash MP Cost=3, MP Recovery Per Second=0 지정 → 컴포넌트 메뉴 MP > 현재 설정으로 초기화 → WASD 이동 중 Left Shift 회피. Current MP가 10→7→4→1로 줄고 부족한 다음 시도는 추가 회피나 MP 차감 없이 거절되는 것이 정상이다. 회피 쿨타임이 있으므로 각 시도 사이 기존 설정만큼 기다린다. 회복은 MP Recovery Per Second=2 등 명시적 테스트값으로 별도 확인하며 이 값은 확정 기획값이 아니다. Play 중 테스트 설정은 저장하지 않는다.
- 약공격 버튼은 아직 없으므로 실제 버튼으로 입력 잠금을 확인했다고 보고하지 않는다. 잠금은 이번 단계 자동 테스트로 확인했고 다음 단계에서 실제 애니메이션/버튼으로 검증해야 한다. MP 게이지가 아직 변하지 않는 것은 HUD 미연결 상태다. 오류 시 Console·실패 테스트명, PlayerData MP 설정, Player inputActions, PC/조이스틱 제어 모드와 Rigidbody 속도를 확인한다.
- 다음 우선순위: 3단계 스킬 보유/버튼 장착 모델과 약공격 실행·애니메이션 연결. 실제 SkillDatatable.xlsx 미발견 및 개별 스킬 계수 미정은 유지한다.

## 2026-11-03 플레이어 탄환 풀링 구현 완료

- 브랜치 `main`, HEAD `f3fd42c` 유지. 커밋·브랜치 전환 없음. 시작 시 MainTestScene 및 이전 설계/Handoff 변경을 보존했고, 종료 시 MainTestScene SHA-256이 시작값과 같음을 확인했다.
- PlayerCombat.FireAt을 `Managers.Pool.Pop`으로 연결하고 활성화 전 기존 `Bullet.Init(Transform, float, float)` 호출. Bullet은 IPoolable을 구현하며 적중·수명 종료·화면 이탈에 풀로 반환한다. 비풀링 Init 호출은 Destroy 호환 경로를 유지한다.
- 대여/반환 때 피해·속도·방향·남은 수명·적중/초기화 상태와 Rigidbody 속도를 정리한다. 예약 Destroy 대신 수명 카운트다운을 사용한다. 피해 콜백 중 재대여되면 대여 버전으로 새 발사를 보호한다.
- 발사 순간 방향만 저장하고 타깃 참조를 유지하지 않아 표적 이동·소멸·풀 재사용으로 비행 중 방향이나 수명을 바꾸지 않는다. 카메라 부재 시에도 수명으로 회수한다. 화면 기반 타깃 선정·MP·스킬·입력 잠금은 이번 범위에서 변경하지 않았다.
- 씬·프리팹·기존 meta·Project Settings·패키지 변경 없음. 기존 Bullet.Init API와 DeleteTime 직렬화 필드명 유지. 새 PlayMode 테스트/meta 및 구현 계획 문서 추가.
- 검증: Unity 6000.5.0f1 검증 복사본에서 기존 코드의 새 테스트 6/6 실패 확인 후 수정. 첫 수정 검증 9/10에서 비활성 객체 SendMessage 테스트 호출 문제를 수정했다. 최종 탄환 6 + CoreInitialization 4 + 씬 의존 테스트를 제외한 PooledEnemySpawning 19 = **29/29 통과**, 실패/생략 0. C# 컴파일 성공, git diff --check 통과. 독립 정적 리뷰에서 수정 필요 결함 없음.
- 검증 환경: 원본 Unity를 다시 열지 않고 짧은 Temp 경로에 코드·테스트·Joystick Pack·설정 복사본 사용. 라이선스 초기화 지연 후 검증 프로세스만 권한 재실행했다. 복사본에만 PlayFromTitle 배치 guard와 필요한 기존 로컬 캐시 패키지 참조를 적용했다. 원본 패키지 구성 그대로의 전체 프로젝트/씬/그래픽 검증은 아님.
- 결과: `.utmp/BulletPooling/red.xml`, `final.xml`, `final.log`; 검증 위치는 같은 폴더 validation-path.txt. 임시 결과는 참고이며 재현 가능한 테스트 원본은 Assets/Tests/PlayMode/PlayerBulletPoolingTests.cs에 있다.
- 남은 검증: 실제 MainTestScene 물리 충돌·화면 표시, Android 빌드·기기·성능. Rigidbody 속도 초기화 및 피해 콜백 내부 즉시 재대여는 정적 검토했지만 별도 직접 테스트로 계측하지 않았다.
- 직접 확인: Unity 컴파일 완료 → Console 오류 확인 → MainTestScene → Play(자동 Title 시작이면 Tools > Play From Title 옵션 확인) → 적이 범위 안에 있을 때 발사 → Hierarchy `@Managers/@Pools/@BulletPool` 아래 탄환 선택 → 적중/화면 이탈 시 같은 인스턴스가 비활성화되고 다음 발사에 재활성화되는지 확인. 반복 재사용 후에도 적 체력이 줄고 탄환이 회수되는 것이 정상이다. 동시 비행 수가 늘면 필요한 새 인스턴스가 추가되는 것은 정상이다.
- 오류 시 Console 메시지, Player.bulletPrefab, Bullet·PooledObject 컴포넌트, 카메라 MainCamera 태그를 확인한다. PooledObject는 첫 풀 생성 시 자동 부착되므로 프리팹 수동 수정은 불필요하다.
- 다음 우선순위: 승인된 스킬 설계의 MP·행동 제어 구현 범위를 구체화한다. 화면 내 대상 선정 등 탄환 기획 정합성은 별도 작업이며 이번 풀링 완료와 혼동하지 않는다.

## 2026-11-03 스킬 후속 합의 및 설계 초안

- 최신 결정은 `docs/superpowers/specs/2026-11-03-player-skills-design.md`에 정리했다. 아래 초기 검토의 유도탄 설명과 이동 약공격 제안은 이 합의로 대체된다.
- 탄환은 발사 때 화면 내 가까운 일반/엘리트/보스를 다시 선택하되 발사 순간 위치로 직진하며 비행 중 유도·재타겟하지 않는다.
- 약공격 애니메이션 중 이동·공격·전략·방어·회피 입력을 모두 차단하고 종료 후 해제한다.
- 차징 배율은 누적 증가, 시간·배율은 조정 가능한 테스트값, MP는 차징 시작 때 차감. 전략 쿨타임 6초 기준. 보유 스킬은 패시브/액티브를 가지며 슬롯 8개 중 6개 기본 활성, 2개 해금 가능. 버튼당 스킬 하나 장착. 보스 그로기 미정.
- 다음 작업: 설계 초안 기준으로 탄환 정합성·풀링부터 구체적 구현 계획을 작성한다. 자동 패시브의 약공격 중 발사 여부, 장착 제약·개별 효과·취소 환불 등은 확정으로 취급하지 않는다.
- 런타임 코드·씬·프리팹 변경 없음. 사용자 MainTestScene과 Office 잠금 파일 보존. Unity 실행·컴파일·테스트 0개. 문서 변경에 git diff --check 수행.

## 2026-11-03 스킬 기획 구현 방향 검토

- `Zomso_Skill_기획서.docx`와 루트 종합 기획서를 읽고 Player/PlayerCombat/Bullet/PlayerDefense/PlayerData 및 기존 SkillData·리소스·풀·팝업 코드를 비교함. 구현은 요청되지 않아 런타임 코드와 에셋은 변경하지 않음.
- 제안: 기존 Player 구성에 플레이어 소유 스킬 실행·우선순위 중재를 추가하고, MP는 PlayerData, 스테이지별 전략 횟수 초기화 신호는 게임 진행, 로딩과 풀링은 기존 Core를 사용. 데이터 정의와 실행 중 상태를 분리.
- 차이: 현재 Bullet은 범위 내 직선탄·Instantiate/Destroy이며, 화면 내 유도탄·차징/방어 중 자동 발사 중단과 다름. PlayerData는 HP 중심이고 MP 실행 규칙이 없음. 기존 SkillData는 예제 스키마이며 기획 컬럼을 충분히 표현하지 못함. 추적 파일 검색에서 SkillDatatable.xlsx는 발견하지 못함.
- 구현 전 결정할 내용: 비행 중 탄환 재타겟 여부/일반 적과 보스의 타깃 범위, 차징 3단계 임계시간과 배율 누적 방식, MP 차감 시점 및 취소별 환불, 전략 쿨타임 본문 6초와 QA 6~10초 차이, 다중 보유 스킬과 버튼 연결, 보스 그로기 임계값. MP 명칭 통일하며 문서 잔존 구표기는 새 코드에 반영하지 않음.
- 권장 순서: MP·상태 우선순위 → 자동 탄환 정합성·풀링 → 약공격 → 강공격 → 전략 효과·스테이지 초기화 → 데이터 확장·성장 연결·연출. 검증 순서는 MP 부족/이동 약공격/차징 캔슬/전략 제한/일시정지 및 재시작/풀 재대여.
- 이번 검토의 Unity 실행·컴파일·테스트는 0개. 사용자 MainTestScene 변경 보존. 다음은 브리핑의 미정 사항을 확정한 뒤 설계·구현 범위를 정하는 것.

## 2026-11-03 플레이어 Bullet 풀링 확인

- 현재 상태: `main`, HEAD `f3fd42c`. 시작 시 사용자 변경 `Assets/9.Scenes/MainTestScene.unity`가 있었으며 수정하지 않음.
- 확인 결과: 플레이어 Bullet은 풀링되지 않음. `PlayerCombat.FireAt`이 `Object.Instantiate`로 생성하고, `Bullet`은 충돌·화면 이탈·타깃 소멸·수명 종료에 `Destroy`를 사용함. 공통 `PoolManager`와 풀링 예제는 있지만 실제 플레이어 발사 경로에 연결되지 않음.
- MainTestScene의 bulletPrefab GUID는 `Assets/1.Prefabs/Bullet.prefab`와 일치하고 해당 프리팹은 실제 `Bullet` 스크립트를 참조함.
- 검증: 소스 및 씬/프리팹 참조 정적 확인. Unity 실행·컴파일·테스트 0개. 런타임 인스턴스 재사용은 계측하지 않음. 코드·씬·프리팹 변경 없음.
- 직접 확인: MainTestScene에서 Play → 적이 공격 범위에 있는 동안 발사 확인 → Hierarchy에서 Bullet(Clone)이 생성 후 사라지는지 확인. 발사가 없으면 Player의 bulletPrefab, 적 Enemy 태그·Collider2D·공격 범위 및 Console 오류를 확인.
- 다음 작업: 풀링 적용은 아직 요청되지 않았음. 적용 시 생성/반환 경로 전환과 함께 isHit 등 재대여 상태 초기화 및 예약 Destroy 제거를 검토해야 함. 기존 실제 데이터·Android 검증 제한은 유지.

## 2026-11-03 스프라이트 필터 설정

- 시작 상태: `main`, HEAD `b3b0de6`, 작업 트리 깨끗함. 기존 Handoff.md는 없었음.
- 완료: `Assets/2.Resources` 하위 TextureImporter의 Sprite(`textureType: 8`) 1,471개를 확인하고 Bilinear였던 908개의 `.meta`를 Point(`filterMode: 0`)로 변경. 기존 Point 563개 유지.
- 검증: 전체 Sprite 1,471개에서 Point가 아닌 항목 0개. Git diff에서 필터 값 외 변경 0줄. `git diff --check` 통과. GUID와 다른 임포트 설정은 유지.
- 실행한 Unity 테스트: 0개. 설정 파일 검사만 수행했으며 Unity 재임포트, 화면 표시, 빌드와 기기는 검증하지 않음.
- 다음 확인: Unity로 돌아가 자동 임포트 완료 후 Project → Assets/2.Resources에서 스프라이트 선택 → Inspector → Filter Mode가 `Point (no filter)`인지 확인. 이전 값이 보이면 해당 에셋 우클릭 → Reimport 후 Console의 임포트 오류 확인.
- 이번 작업에서 씬, 프리팹, 코드, Project Settings, 패키지는 변경하지 않음.

## 2026-11-03 보스 남쪽 Swipe 잘림 수정

- 시작 상태: `BossId`, HEAD `964ff51`. 기존 변경인 UniversalRenderPipelineGlobalSettings.asset, EditorBuildSettings.asset, ShaderGraphSettings.asset는 보존.
- 재현: Beast_Zombie_Boss/Idle/animations/swipe/south의 4~7.png에서 원본의 불투명 픽셀이 Sprite Rect 밖에 있었음. 각각 526/384/224/125픽셀이 영역 밖으로 잘림. Boss_Swipe_South.anim은 해당 Sprite ID를 직접 참조함.
- 완료: 해당 `.png.meta` 4개의 Sprite Rect만 상하좌우 대칭 확장하고 1픽셀 여유 확보. Center 정렬과 원본 좌표상 중심, GUID, spriteID, internalID를 보존. 코드·씬·프리팹·애니메이션 클립·원본 PNG·Collider는 변경하지 않음.
- 새 Rect(x,y,w,h): 4=(29,26,107,110), 5=(28,25,106,110), 6=(28,23,105,114), 7=(29,22,103,116).
- 검증: 원본 PNG 알파와 Rect 비교에서 수정 전 4개 모두 잘림 재현, 수정 후 영역 밖 불투명 픽셀 0. 중심 좌표 동일, Rect 외 메타 내용 동일 확인. git diff --check 통과. Unity 컴파일/테스트 실행 0개이며 실제 재임포트·화면 재생은 미확인.
- 사용자 확인: Unity 자동 임포트 후 Project → Assets/2.Resources/Art/Beast_Zombie_Boss/Idle/animations/swipe/south → 4~7.png → Sprite Editor에서 손끝과 이펙트 전체가 사각형 안에 들어오는지 확인. 보스 선택 → Animation 창 → Boss_Swipe_South의 4~7프레임을 이동해 잘림 없이 중심이 유지되는지 확인. 이전 영역이면 PNG 4개만 Reimport하고 Console 임포트 오류 확인.
- 다음 작업: 실제 Unity 재생 확인. 다른 방향·프레임은 이번 요청 범위 밖으로 유지.

## 2026-11-03 캐릭터 Sprite Rect 추가 검사

- 캐릭터/보스 원본 Sprite PNG 1,410개 검사(배경 CityPeaceful, Items, Tiles 제외), 읽기 오류 0. 영역 밖 알파 픽셀 검사에서 Beast_Zombie_Boss에 추가 57개 발견. 앞서 수정한 south 4~7은 잘림 0 유지.
- 애니메이션 클립이 참조하는 문제 이미지 33개: swipe/north 0~7, north-west 0·3~7, south 0~3, south-east 0·2~7, south-west 0~7.
- 현재 클립이 직접 참조하지 않는 문제 이미지 24개: swipe/east 0~7, west 0~7, north-east 4~7, swipe-both/east 0·2, west 2·4. 대체 FrameSources를 쓰는 경우 원본 결함과 실제 재생 결함을 구분해야 함.
- 다른 캐릭터는 이번 Rect 검사에서 영역 밖 픽셀 없음. 별도로 PNG 캔버스 가장자리에 그림이 닿는 후보 65개(Beast 보스 6, Magical_Girl_Zombie 1, Pretty_Magical_Girl 43, Zombie 7, Zombie_Construction 8)가 있으나 원본 잘림 여부는 미확정. Tight 메시·압축·실제 화면 가림은 이 검사 범위 밖.
- 이번 요청은 추가 확인이므로 에셋 추가 수정 없음. 다음 작업은 사용자 수정 요청 시 위 목록의 Rect 확장. Unity 테스트 실행 0개, 실제 화면 재생 미확인.
- 직접 확인: 보스 선택 → Animation 창 → Boss_Swipe_North / NorthWest / South / SouthEast / SouthWest에서 해당 프레임 확인. Sprite Editor에서 손·이펙트가 Rect 밖이면 같은 문제. 불일치 시 실제 연결 Sprite와 Console 임포트 오류 확인.

## 2026-11-03 추가 발견 57개 Sprite Rect 수정 완료

- 사용자 요청에 따라 위 추가 검사에서 발견한 Beast_Zombie_Boss 이미지 57개의 `.png.meta`를 수정. 현재 클립 참조 33개와 미참조 원본 24개 모두 포함. 앞서 수정한 south 4~7은 그대로 유지하여 총 수정 메타 61개.
- 주 Sprite Rect를 상하좌우 대칭 확장하여 원본의 모든 알파 픽셀과 최소 1픽셀 여유 포함. 중심 좌표·Center 정렬·GUID·spriteID·internalID·기타 메타 설정 보존. 여러 Sprite가 나뉜 원본 12개는 가장 큰 본체 Sprite만 확장하고 나머지 작은 이펙트 Sprite의 Rect/ID는 유지.
- 검증: 57개 수정 전 잘림 재현 보고서 기반으로 수정 후 캐릭터/보스 PNG 1,410개 재검사 → Sprite Rect 밖 알파 픽셀을 가진 이미지 0개, 읽기 오류 0개. 57개 모두 중심 동일·Rect 이외 메타 내용 동일 확인. 코드·씬·프리팹·클립·Project Settings 등 231개 파일의 작업 전후 해시 동일. git diff --check 통과.
- Unity 컴파일/테스트 실행 0개. 실제 에디터 재임포트·애니메이션 재생은 미확인. 캔버스 가장자리 접촉 후보 65개는 원본 이미지 자체의 별도 조사 대상으로 유지하며 이번 수정으로 해결됐다고 판단하지 않음.
- 사용자 확인: Unity 자동 임포트 완료 → 보스 선택 → Window/Animation/Animation → Boss_Swipe의 각 방향 재생. 손끝·공격 이펙트가 잘리지 않고 보스 중심이 유지되는 것이 정상. 변화가 없으면 해당 PNG만 Reimport하고 Sprite Editor의 Rect 및 Console 임포트 오류 확인.
- 다음 작업: 실제 재생에서 수정 결과 확인. 이전 절의 '추가 수정 없음'은 당시 검사 시점 기록이며 이번 절에서 수정 완료함.

## 2026-11-03 남동쪽 걷기 첫 프레임 참조 수정

- 사용자 스크린샷은 swipe가 아닌 move/south-east/0.png의 Sprite 목록이었음. 0_0은 작은 조각(10x8), 0_1은 본체(83x94)인데 BossWalk_SouthEast.anim 첫 프레임이 0_0을 참조하는 문제 확인.
- 수정: 해당 클립의 첫 Sprite 키와 pptrCurveMapping만 본체 0_1(fileID 8745284039982918138)로 변경. GUID, 나머지 프레임·타이밍·이벤트·코드·씬·프리팹·원본 meta 유지.
- 검증: 본체 Rect에 원본의 전체 알파 픽셀이 포함됨 확인. 키와 바인딩의 본체 참조 2곳 일치, git diff --check 통과. Unity 테스트 0개, 실제 재생 미확인.
- 이전 1,410개 검사는 한 PNG의 모든 Sprite Rect 합집합과 원본을 비교한 것이므로, 작은 조각을 애니메이션이 잘못 참조하는 문제까지 검증한 것은 아님.
- 직접 확인: Unity 임포트 후 보스 선택 → Animation → BossWalk_SouthEast → 0프레임에서 보스 본체가 보여야 함. 여전히 조각만 보이면 클립 Reimport 후 첫 Sprite 키가 0_1인지와 Console 오류 확인.

## 2026-11-03 Enemy/Boss 플레이어 접촉 밀림 방지

- 시작 상태: BossId, HEAD 964ff51. 이전 Sprite Rect 메타 61개, BossWalk_SouthEast.anim, Handoff 및 기존 설정 3개 변경을 보존.
- 원인: EnemyFollow.Awake가 일반 적/보스 Rigidbody2D를 Dynamic으로 설정하여 플레이어 충돌의 물리 반발에 밀림. FixedUpdate에서 속도를 0으로 만들어도 다음 물리 스텝의 위치 보정까지 막지는 못했으며 보스 공격 시 Follow 비활성 상태에서도 밀림.
- 수정: 이동/물리 책임을 가진 EnemyFollow에서 bodyType을 Kinematic으로 설정하고 useFullKinematicContacts=true로 접촉 콜백 유지. EnemyContactDamage, BossController, 씬/프리팹/Collider/공격 수치는 변경하지 않음.
- 추가: Assets/Tests/EnemyPhysics에 독립 PlayMode 테스트 어셈블리와 접촉 데미지 수신용 테스트 컴포넌트. 기존 런타임 어셈블리 이동 없음.
- 재현/검증: Unity 6000.5.0f1의 별도 검증 프로젝트에서 실제 EnemyFollow/EnemyContactDamage/IContactDamageReceiver 소스 사용. 수정 전 10개 중 5개 실패(플레이어 Dynamic/Kinematic 각각 Follow 켜짐/꺼짐 밀림 4개, 재사용 후 외부 힘 1개), 수정 후 10/10 통과·생략 0. solid/trigger 접촉 데미지 및 쿨다운 4개, 추적과 정지 거리 1개, Follow 비활성 중 MovePosition 이동 및 재사용 후 외부 힘 저항 1개 포함.
- 초기 검증 6개 중 일부는 테스트 씬 비동기 정리 문제로 실패했으며 UnityTearDown으로 수정한 뒤 위 10개를 다시 실행함. 이를 게임 결함으로 집계하지 않음.
- 검증 경로: .utmp/EnemyPhysics/reproduced.xml, after.xml 및 해당 로그. 원본 씬/프리팹/클립/기존 메타/Project Settings 작업 전후 해시 동일. git diff --check 통과.
- 제한: 전체 프로젝트 컴파일/전체 회귀 테스트/실제 MainTestScene 보스 패턴 재생은 미실행. 테스트는 실제 BossController 전체 대신 Follow 비활성 중 Rigidbody2D.MovePosition 경로를 검증함.
- 물리 동작: Kinematic은 접촉 힘으로 밀리지 않으며 코드로 이동함. 적끼리 및 Kinematic/Static 상대와의 접촉은 콜백은 발생하지만 서로 물리적으로 밀어내거나 분리하지 않음. 실제 군집/벽 및 Kinematic 플레이어 겹침 여부는 씬 확인 필요.
- 직접 확인: Unity 컴파일 완료 → MainTestScene Play → 일반 적/보스의 몸통에 플레이어를 계속 이동. 적/보스가 플레이어 때문에 밀려나지 않고 추적 및 접촉 HP 감소가 유지되어야 함. 보스 공격 대기·공격 중에도 확인. 오류 시 Console 컴파일 오류, 실행 중 Enemy/Boss Rigidbody2D의 Kinematic 및 Full Kinematic Contacts, Collider 활성/Is Trigger, 플레이어 Rigidbody 유형을 확인.
- 다음 작업: 실제 씬에서 밀림 방지·접촉 데미지·공격·돌진·군집 동작 확인. 에디터에서 새로 Play해야 Awake 설정이 적용됨.

## 2026-11-03 밀림 방지 + 몸통 관통 방지 재수정

- 사용자 의도 확정: 플레이어가 Enemy/Boss를 밀 수 없으며 서로의 solid 몸통을 통과해서도 안 됨. 이전 Kinematic 단독 변경은 이 의도를 충족하지 못하므로 이번 수정으로 보완.
- 시작 상태: BossId/964ff51. 기존 코드·이미지 메타 변경 외에 사용자 MainTestScene.unity 및 AddressableAssetSettings.asset 변경이 존재했으며 저장/덮어쓰기하지 않음.
- 공통 CollisionMovement2D(Combat.Runtime)를 추가: 실제 활성 non-trigger Collider2D 형상을 이동 거리 전체에 Cast하여 가장 가까운 solid 앞에서 이동 제한. 접촉면 방향으로 잔여 이동을 다시 검사하여 벽면 이동/모서리 차단 지원. Trigger는 이동 차단에서 제외하고 기존 접촉 데미지 콜백 유지. Collider/레이어 충돌 설정과 IgnoreCollision을 반영. 매 액터 이동 위치를 즉시 반영해 다른 액터의 다음 검사가 갱신된 위치를 보도록 함.
- EnemyFollow: Kinematic+Full Kinematic Contacts 유지(외력에 밀리지 않음), 추적에 위 이동 검사 적용. 공개 MoveTo를 보스 패턴에서도 사용 가능하도록 추가.
- BossController: 돌진의 직접 MovePosition을 Follow.MoveTo로 교체. solid에 막혀 이동이 불가능하면 돌진 루프를 끝내어 장애물 뒤에서 무한 대기하지 않도록 처리. 공격 수치/이벤트/일반 패턴 규칙 변경 없음.
- PlayerMovement: 키보드 Tick과 DashRoutine의 직접 Transform 이동을 같은 검사 경로로 교체. Rigidbody 없는 기존 호출에만 기존 Transform 이동 fallback 유지.
- PlayerController2D: 조이스틱의 직접 속도 이동을 같은 검사 경로로 교체. PlayerMvpMovement도 직접 MovePosition을 같은 검사로 교체. 기존 입력/속도/대시 시간·MP 규칙 유지.
- 재현: 이전 코드에 몸통 차단 검증을 추가한 14개 테스트 중 5개 실패(적→플레이어 관통, 적끼리 관통, 대시 관통, Kinematic 플레이어 관통 2개). 이후 실제 프레임 진행을 검증하는 UnityTest와 벽면/Trigger 검사를 추가.
- 최종 검증: Unity 6000.5.0f1 별도 PlayMode 프로젝트에서 18/18 통과, 생략 0. 실제 EnemyFollow, EnemyContactDamage, CollisionMovement2D, PlayerMovement, PlayerData 소스 사용. Dynamic/Kinematic 플레이어의 밀림·차단, 실제 PlayerMovement 키보드/대시, 적끼리 차단, Follow 비활성 중 보스 MoveTo 차단, 추적 정지 거리, 재사용/외력 저항, solid/trigger 접촉 데미지와 쿨다운, 큰 Trigger 반경 무시, 벽면 이동 및 후퇴 검증.
- 결과: .utmp/EnemyPhysics/blocking-final.xml 및 blocking-final.log. 초기 접촉면 방향 보완 후, UnityTest 프레임 시간과 Cast 접촉 여유를 반영해 최종 재실행. 이전 10개 통과 기록을 재사용한 결과가 아님.
- 한계: 실제 MainTestScene 전투/보스 전체 패턴/조이스틱 입력/프로토타입 InputAction의 화면 재생은 미실행. 별도 프로젝트에서는 공통 이동과 실제 PlayerMovement 경로를 검증했고, 모바일/프로토타입/전체 BossController 시나리오까지 통과했다고 주장하지 않음. 원본 에디터에서 변경된 Runtime/Assembly-CSharp 어셈블리 생성 시간은 확인함.
- 직접 확인: Unity 컴파일 완료 후 Play 재시작 → MainTestScene에서 키보드와 조이스틱으로 적/보스 몸통을 계속 밀기 → 대시로 몸통을 넘으려 하기 → 보스 공격 대기 및 돌진 중 반복. 플레이어는 몸통 앞에서 멈추고 적/보스는 밀리지 않아야 함. 붙은 뒤 반대 방향 이동과 옆 이동은 가능하고 접촉 HP 감소/공격은 유지되는 것이 정상. 적끼리도 관통하지 않아야 함.
- 오류 확인: Console 컴파일 오류, 양쪽의 활성 solid Collider 존재(Is Trigger=false), 충돌 레이어 및 IgnoreCollision, 실행 중 Enemy Rigidbody Kinematic+Full Kinematic Contacts, 이동 스크립트가 최신인지 확인. Collider가 Trigger뿐인 대상은 의도적으로 통과 가능.
- 다음 작업: 사용자 실제 씬에서 모든 입력 경로·군집·벽·보스 공격/돌진 확인. 이번 작업에서 씬/프리팹/Project Settings/패키지를 수정하지 않음.
