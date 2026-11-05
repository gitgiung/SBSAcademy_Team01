# 작업 인수인계

## 2026-11-04 보스 경계 정체 및 할퀴기 중단 복구 수정

- 사용자 승인: 직전 발견의 추적 정지 경계와 리뷰4의 애니메이션 중단 대기를 함께 수정. 리뷰1 돌진 피해 중심은 유지,2 표시 수정은 다른 브랜치 담당,3 벽 재시도는 보류라는 사용자 판단을 유지했다. 이번 런타임 수정은 BossController에만 적용하며 씬/프리팹/공용 이동/피해/표시·패턴 간격은 바꾸지 않았다.
- 경계 수정: FollowStoppingDistance=Mathf.Max(0,closeRange-0.001f). 초기 활성화·SetTarget·할퀴기 복귀·돌진 복귀의 추적 정지점을 이 값으로 통일했다. 근접 공격 선택 조건 distance<=closeRange 자체는 유지하므로 공격 범위를 늘리지 않는다. 실제 이동의 작은 이동량 무시 경계보다 안쪽까지 접근하여4.00005에서 멈추는 현상을 방지한다.
- 중단 복구: PlaySlashAnimation의 Animator 재생/대기를 try/finally로 감싸 공격 실행 플래그와 추적 상태를 정리한다. 대기 중 Animator 비활성화/삭제, runtimeAnimatorController 제거, 현재 상태 변경, 다른 상태로 전이, normalizedTime>=1을 감지해 종료한다. 정상 종료 이벤트는 계속 우선 사용하고, 이벤트 누락 시 피해를 임의로 발생시키지 않는다. 일시정지는 정상 애니메이션 진행을 기다리며 보스 비활성화/사망 시 추적을 재활성화하지 않는다.
- 회귀 테스트: BossAttackRecoveryTests.cs 및 meta 추가(7개). 거리4.00005/3.99 접근 후 근접 선택과 기존 공격 범위 미확장, Animator 해제/컨트롤러 제거/상태 변경 복구, 종료 이벤트 제거 후 복구, 일시정지 유지/재개를 검사한다.
- 실행 결과: Unity6000.5.0f1 별도 최소 복사본 컴파일·PlayMode25/25 통과(신규7+직전 경계 재현2+돌진 시작/루프16), 실패0/생략0, exit0. 동일 테스트7개를 복사본에서만 수정 전 두 로직으로 비교 실행하여2통과/5실패(exit2): 경계4.00005와 중단4종이 의도대로 실패했다. 원본은 계속 수정본을 유지했고 비교 종료 후 복사본도 수정본으로 복구했다. 수정본/테스트 해시 일치 및 대상 diff --check 통과.
- 자료/한계: .utmp/ChargeAnimation/recovery-green.xml/log, recovery-before.xml/log 및 BossController.recovery-green.txt(Git 제외). 원본 씬 실제 조작·전체 프로젝트 회귀·Android 기기는 이번에 검사하지 않았다. 과거50개 검사 결과를 새 검증 수로 재사용하지 않는다.
- 직접 확인: Unity 컴파일 완료 → Tools > Play From Title 해제 → MainTestScene Play → 보스가 접근한 뒤 가만히 있어도 기존 패턴 간격 후 계속 공격하는지 확인. 실행 중 할퀴기 준비에서 Animator만 꺼 보면 공격 대기가 해제되고 추적이 복구되어야 한다. 정상 일시정지 후 복귀하면 해당 공격은 계속 진행되어야 한다. 임시 Animator 변경은 Play 종료로 되돌리고 씬을 저장하지 않는다.
- 오류 확인/다음: Console 첫 오류, 실제 중심 거리/closeRange와 EnemyFollow stoppingDistance, Animator 활성/현재 상태/종료 이벤트를 확인한다. 생존·활성 보스에 한해 추적이 켜져야 한다. 다음은 사용자 원본 씬에서 정지 후 패턴 반복 및 중단 복구 체감 확인이다.

## 2026-11-04 보스 추적 정지 경계에서 패턴 미선택 조사

- 사용자 판단 반영: 이전 리뷰1의 플레이어 중심 돌진 피해는 현 상태 허용,2의 범위 방향은 다른 브랜치에서 수정됨,3의 벽 차단 후 반복은 추후 벽 추가 시 검토. 4의 애니메이션 중단 복구는 수정 승인 뒤 사용자가 잠시 보류하고 본 현상 조사를 요청했다. 아직4번 런타임 수정을 시작하지 않았으며 이번에도 원본 코드·씬·프리팹은 변경하지 않았다.
- 현상: 추적하다 범위 안에 온 것으로 보이지만 공격하지 않고 멈추며 플레이어가 움직이면 다시 공격. 유력 원인은 추적 정지 거리와 근접 패턴 시작 거리를 같은 closeRange로 사용하는 경계 정체다. SelectAvailablePattern은 distance>closeRange면 None, EnemyFollow는 remaining만큼 이동하려 하지만 CollisionMovement2D는 이동 제곱값1e-8 이하를 처리하지 않는다. closeRange4에서 실제 거리4.00005가 남으면 추가 이동도 공격 선택도 되지 않는다.
- 재현: 원본 BossController를 별도 최소 Unity6000.5.0f1 검증본에 복사하고 실제 EnemyFollow/CollisionMovement2D로 정지 표적에 대해 FixedUpdate60회 호출. 거리4.00005는 그대로/selected=None/following=True여서 근접 공격 기대 검사가 실패했고, 거리3.99 비교는 selected=Pattern1로 통과했다. 최종2개 중1통과/1실패, exit2. 원본 씬에서 사용자가 목격한 순간의 거리나 nextPatternTime은 측정하지 않아 동일 원인으로 확정하지는 않는다.
- 자료/검증 제한: .utmp/ChargeAnimation/boundary-confirm.xml 및 boundary-confirm.log, .utmp/c/Assets/Tests/BossBoundaryInvestigationTests.cs(Git 제외). 최초 비교 검사는 검증용 Animator 누락으로 불필요한 오류가 있어 표적을 활성화 후 이동하는 비교로 정리하고 최종 검사를 재실행했다. 결과는 동작 검증 통과가 아니라 버그 재현이다. 실제 프레임 진행/원본 씬 플레이 재현이나 수정 후 테스트는 하지 않았다.
- 권장 수정: 공격 반경 자체를 늘리기보다 보스의 추적 정지 거리를 근접 판정 경계보다 작은 오차 여유만큼 안쪽으로 두어 경계 정체를 없앤다. 이번은 조사 요청이므로 적용하지 않았다. 사용자 확인은 MainTestScene Play → 보스 추적 후 가만히 대기 → 장시간 미공격이면 실제 중심 거리와 closeRange/nextPatternTime 확인 → 조금 보스 쪽으로 이동했을 때 공격 재개 여부 확인. 정상 패턴 간 대기1~2초와 지속적인 경계 정체를 구분한다. 오류 시 Console 첫 오류 및 Animator 종료 상태도 함께 본다.

## 2026-11-04 보스 중심 코드 정적 검토

- 사용자 요청은 검토/보고이며 런타임 코드·씬·프리팹을 수정하지 않았다. BossBeastZombie/88f436b 및 기존 다수 변경을 확인하고 보존했다. BossController, PatternSelector/RuleSelector, BossPatternRangeView, BossEncounterController, EnemyHealth, 보스 프리팹과 MainTestScene override 및 테스트 범위를 읽었다.
- 주요 발견1: BossController:344가 돌진 도착 피해 중심으로 target.position을 사용한다. 현재 보스 closeRange4/dashEndDamageRadius2에서 플레이어3거리도 도착으로 판단하고 플레이어 중심 원으로 명중시킨다. 보스 주변 타격 의도라면 중심을 body.position으로 통일하고 범위 표시도 함께 변경해야 한다. 현재 코드는 플레이어 중심 타격이며 보스 중심 반경2 판정이 아니다.
- 주요 발견2: BossController:196은 attackForward를 시작 시 고정하지만 BossPatternRangeView:76~82는 매 프레임 현재 target 방향으로 부채꼴 표시를 회전시킨다. 준비 중 플레이어가 옆으로 움직이면 보이는 범위와 실제 피해 방향이 다르다. 표시가 고정된 공격 방향을 참조해야 한다.
- 주요 발견3: 돌진에서 벽에 막히면 MoveTo 이동0으로 종료하지만 PatternLoop:165가 다음 공격 시간을 즉시 현재 시간으로 설정한다. 벽 너머 표적이 특수 거리 밖에 남으면 반복적으로 돌진을 재시작해 시작 동작/로그가 되풀이된다. 차단 종료 후 재시도 간격 또는 추적 복귀 정책을 정해야 한다. 단순 반복 시작을 피할 필요가 있으며 시간 수치는 임의 확정하지 않았다.
- 조건부 발견4: PlaySlashAnimation:214는 종료 이벤트만 기다리고 Animator 비활성화/상태 교체/이벤트 누락을 감지하지 않는다. 공격 도중 Animator만 꺼지거나 종료 이벤트가 제거되면 follow=false 상태에서 패턴 대기가 끝나지 않을 수 있다. 현재 정상 클립에 종료 이벤트가 있는 것과 중단 복구 부재는 구분한다.
- 현재 흐름: 근접은 규칙 선택기로 단발→연속 할퀴기 교대, 중거리는 추적, 원거리는 돌진. 할퀴기는 애니메이션 이벤트에서 피해, 돌진은 시작1회→지속 루프 및 도착 피해. 보스 출현은 진행 타이머/일반 스폰을 정지시키며 Time.timeScale을0으로 만들지 않는다. 보스 사망 후 AfterBoss/스폰 재개이며 곧바로 최종 승리로 연결하지 않은 기존 설계는 임의로 버그로 분류하지 않았다.
- 검증: 이번 Unity 실행/컴파일/회귀 테스트0개. 위 내용은 소스·직렬화 설정 기반 정적 검토이며 실제 씬 재현 완료로 보고하지 않는다. 과거50/16개 통과는 이번 검토 결과를 대신하지 않는다.
- 직접 확인/다음: MainTestScene에서 보스 할퀴기 준비 중 옆으로 이동해 부채꼴과 실제 타격 비교 → 보스와3거리/몸통 반경2 밖에서 돌진 도착 피해 확인 → 벽 너머로 원거리 유도해 반복 돌진 확인 → 임시 Play 상태에서 공격 도중 Animator만 해제해 추적/패턴 중단 여부 확인. 마지막 조건은 Play 종료로 원복하고 저장하지 않는다. 오류는 Console 첫 오류/패턴3 반복 로그 및 Animator 상태·이벤트·실제 prefab override를 확인한다. 수정 요청 시 위 조건을 회귀 테스트로 먼저 재현하고 판정 중심·표시 동기화부터 처리한다.

## 2026-11-04 돌진 시작/지속 루프 클립 분리

- 사용자 요청: body-charge 전체 반복으로 서 있는 준비 자세가 매번 나오는 부자연스러움 제거. BossBeastZombie의 기존 코드·씬·프리팹·패링/피격/카메라 연출 변경을 보존했다. 이번 변경은 보스 애니메이션 클립과 Boss.controller에만 적용했다.
- 완료: 원본 south 3/4/6/8/10 그림을 확인해 0~3은 준비, 4~10은 몸을 낮춘 돌진 자세로 나눴다. 기존 body-charge_{방향}.anim 8개는 0~3/12fps/비반복(약0.333초)으로 변경하고, 새 body-charge-loop_{방향}.anim 8개는 원본4~10/12fps/반복(약0.583초)으로 만들었다. 원본 PNG/meta 및 기존 클립 GUID는 유지한다.
- Animator: body-charge(기존 진입 상태) → exitTime1/전환시간0 → body-charge-loop(새8방향 BlendTree)로 연결했다. BossController.StartDashAnimation은 기존 이름으로 진입하므로 코드를 바꾸지 않았다. StopDashAnimation은 반복 중에도 기존 Idle로 종료한다. 이동/추적/피해/돌진 시간 규칙은 변경하지 않았다.
- 검증: Unity6000.5.0f1 별도 최소 검증 복사본에서 Animator PlayMode 검사16/16 통과, 실패0/생략0, exit0. 방향별8개는 시작4키/루프7키·참조 PNG번호·반복 여부·길이를 검사, 방향별8개는 실제 Animator 시작→루프 전환 후 여러 반복 동안4~10 PNG만 표시됨과 Idle 종료/새 돌진 재시작을 검사했다. 클립/meta 해시가 검증본과 같고 controller는 검증 후 새 빈값 뒤 공백만 정리했다(그 외 내용 일치). 대상 diff --check 통과.
- 자료/한계: .utmp/ChargeAnimation/split-loop.xml 및 split-loop.log, 임시 검사 .utmp/c/Assets/Tests/ChargeLoopSplitTests.cs(Git 제외). 이번16개는 Animator 자산 검사이며 실제 MainTestScene 전체 돌진·벽 충돌·모바일 기기를 새로 검사한 결과는 아니다. 과거50개 테스트를 이번 실행 수로 재사용하지 않는다.
- 직접 확인: Unity 임포트 완료 → Tools > Play From Title 해제 → MainTestScene Play → 보스와 거리를 벌려 긴 돌진 유도. 처음에만 몸을 낮추고 이후 돌진 자세가 반복되어 서 있는 자세가 중간에 끼지 않아야 한다. Animator 창에서 body-charge→body-charge-loop 전환을 확인하고 돌진 종료 시 Idle/일반 공격으로 복귀하는지 본다. 8방향도 반복 확인한다.
- 오류 확인/다음: Console 첫 임포트 오류, 보스의 실제 Boss.controller 연결, body-charge 상태 전이 및 body-charge-loop의 방향별 클립 참조를 확인한다. 다음은 원본 화면에서 루프 이음새 체감 확인이며 필요하면 반복 프레임 범위를 조정한다.

## 2026-11-04 패링 성공 카메라 줌인/복귀

- 사용자 요청: 패링 성공 시 Main Camera가 살짝 확대됐다 원래대로 돌아오는 연출. 현재 MainTestScene의 CinemachineBrain과 관련 PlayerCameraTests를 확인했다. 다른 작업으로 이동된 PlayerHitFeedbackTest의 Test 폴더 위치를 유지했다. 씬·프리팹·Cinemachine 설정은 이번에 변경하지 않았다.
- 완료: Player.ParryCamera.cs 부분 클래스를 추가하고 기존 HandleParrySucceeded 성공 경로에서 PlayParryCameraZoom을 호출한다. 기본 줌 비율0.05(렌즈 크기/FOV를95%로), 확대0.06초, 복귀0.18초. Inspector의 패링 성공 카메라 줌에서 조절하며 비율0으로 끈다. 실제 시간 기준으로 부드럽게 변화해 히트스톱 중에도 연출이 진행된다.
- 카메라: Camera.main에 활성 CinemachineBrain이 있으면 ActiveVirtualCamera의 CinemachineCamera.Lens를 조절하여 Brain 덮어쓰기와 경쟁하지 않는다. Brain이 없는 일반 Camera는 orthographicSize 또는 fieldOfView를 조절한다. 위치·회전·추적 대상은 유지한다. 카메라가 없거나 지원되는 활성 가상 카메라가 없으면 게임 로직을 막지 않고 줌만 생략한다.
- 수명: 재발동 전 이전 렌즈를 복원해 누적 확대를 방지하며 플레이어 비활성화/사망 때 진행 중 코루틴을 취소하고 원래 렌즈를 복구한다. 가상 카메라 전환 중에는 새 카메라를 덮어쓰지 않고 기존 렌즈를 복원한다. 가드/일반 피격/공격 없이 누른 패링에는 성공 줌을 연결하지 않았다.
- 검증: Unity6000.5.0f1 별도 검증 복사본 컴파일 및 PlayMode50/50 통과, 실패0/생략0, exit0. 신규 카메라5 + 기존 피격5/패링8/MP·회피19/팝업13. 히트스톱 중 직교 줌/정확 복구, 원근 FOV 복구, 비활성화 즉시 복원, 공격 없는 패링 미발동, 실제 Cinemachine 가상 렌즈와 Main Camera 출력 확대/복귀를 검사했다. 변경 소스/새 테스트 해시 일치, 대상 diff --check 통과.
- 자료/한계: .utmp/ParryFeedback/camera-zoom.xml 및 camera-zoom.log(Git 제외). 이번 검사는 실제 MainTestScene 씬을 로드한 전체 플레이 검사나 화면 캡처가 아니며 실제 원본 씬 조작/모바일 기기는 미검증이다.
- 직접 확인: Unity 컴파일 완료 → Tools > Play From Title 해제 → MainTestScene Play → 보스 타격에 맞춰 E키 패링. 짧은 정지 중 화면이 약간 확대됐다 곧 원래 구도로 돌아오고 플레이어 추적/정지/밀림이 유지되어야 한다. Player Inspector의 패링 성공 카메라 줌 세 값으로 체감을 조절한다.
- 오류 확인/다음: Console 첫 컴파일 오류, MainCamera 태그, Brain의 ActiveVirtualCamera가 CinemachineCamera인지, 줌 비율이0인지 확인한다. 다른 카메라 제어기가 렌즈를 변경하는지도 확인한다. 다음은 원본 씬에서 실제 줌 강도와 복귀 시간의 체감 확인이다.

## 2026-11-04 BossParry 사용 폐기 정리

- 사용자 요청: 더 이상 사용하지 않는 BossParry 관련 코드 정리. 시작 상태 BossBeastZombie/88f436b에서 사용자가 BossParry.cs와 meta를 이미 삭제한 상태였다. 삭제는 보존하고 복원하지 않았다. 기존 보스/패링·피격 연출 변경을 유지했다.
- 코드: Player의 BossParry 직렬화 필드 및 PlayerDefense 생성자 인자 제거. PlayerDefense의 BossParry 필드/생성자 의존성과 boss.TryParry 주기 조회를 제거했다. 성공 여부는 실제 TakeDamage 차단 경로에서 받은 parrySucceeded만 사용한다. LastParrySource, 기존 성공 이벤트·MP 회복·히트스톱·밀림·가드 규칙은 유지했다. 참조 없는 옛 색상 타이밍 테스트 BossParryTest.cs와 meta도 삭제했다.
- 직렬화: BossMvp.prefab의 옛 컴포넌트 블록/목록 항목, MainTestScene·GiungTestScene 및 _Recovery의 0.unity/0 (1).unity의 stripped 블록과 해당 컴포넌트 전용 override만 제거했다. Player 스크립트 블록의 폐기된 boss 필드는 dongjinScenes까지 포함해 제거했다. BossEncounterController가 실제 BossController를 참조하는 boss 필드는 유지했다. 씬을 Unity로 전체 재저장하지 않고 나머지 YAML/기존 사용자 프리팹 변경을 보존했다.
- 테스트: 옛 BossParry 전용 성공 테스트를 공격 없이 패링을 눌러도 정지/밀림이 없다는 검사로 교체했다. 실제 공격자 차단 성공 검사는 유지된다. Unity6000.5.0f1 별도 검증 복사본에서 컴파일 및 관련 PlayMode 45/45 통과(피격5·패링8·MP/회피19·팝업13), 실패0/생략0, exit0. 최종 원본 Player/PlayerDefense/수정 테스트는 검증본과 해시 일치. 대상 코드·씬 diff --check 통과. Assets의 C#/씬/프리팹에서 BossParry 이름·두 폐기 스크립트 GUID·폐기 컴포넌트 fileID 잔존 검색0건.
- 자료/한계: .utmp/BossParryCleanup/results.xml 및 validation.log, 직렬화 변경 전 사본(Git 제외). 원본 씬의 실제 재생/Android 기기는 미확인. 새 씬 로드 검사는 이번 별도 코드 테스트에 포함하지 않았다.
- 사용자 확인: Unity 컴파일 완료 → Console에 BossParry 타입 누락 오류가 없는지 확인 → Tools > Play From Title 해제 → MainTestScene Play → 공격 없이 E 입력 시 성공 연출 없음 → 보스 타격을 E로 막으면 MP 회복/히트스톱/밀림 유지 → PlayerHitFeedbackTest 부착 시 일반 피격 빨간색 연출 확인. BossMvp와 두 테스트 씬에서 폐기 BossParry의 Missing Script가 없어야 한다. 오류 시 Console 첫 오류와 실제 Player 입력 에셋을 확인한다. 다음은 사용자 실제 씬 확인이다.

## 2026-11-04 분리형 일반 피격 테스트 연출

- 사용자 요청: 패링 성공 외에도 피격 시 밀림·짧은 정지·빨간색 표시를 테스트용 별도 스크립트로 구현. 기존 변경을 유지하고 씬·프리팹에 자동 부착하거나 저장하지 않았다.
- 추가: PlayerHitFeedbackTest 컴포넌트. Player 오브젝트에 수동 Add Component하면 활성화되고 체크 해제/제거로 끌 수 있다. 기본 빨간색0.25초(scaled), 실제시간 정지0.06초, 공격자 반대 방향 거리0.25/시간0.12초. 색은 기존 alpha를 유지하고 반복 피격/컴포넌트 해제 시 원래 색으로 복구한다. 대상 SpriteRenderer는 Inspector 지정 또는 자식에서 찾는다.
- 연결: Player에 실제 비치명 피해 적용 후 Damaged(float, GameObject) 이벤트만 추가했다. 가드/패링/회피로 막힌 공격 및 무효 피해에는 이벤트가 없고, 치명타는 기존 사망 비활성화 경로를 유지한다. 컴포넌트가 없으면 기존 일반 피격 동작 그대로다. 공격자 정보가 없거나 같은 위치면 임의 밀림 방향을 만들지 않는다.
- 정지/이동 재사용: Player.ParryFeedback의 기존 충돌 이동·시간 소유권 경로에 내부 테스트 진입/취소 함수를 추가했다. 별도 전역 매니저나 두 번째 Time.timeScale 제어 루프는 만들지 않았다. 연출 순서는 패링처럼 정지 후 밀림이며 빨간색 표시는 즉시 시작한다. 실제 피격은 진행 중 반응을 새 피격 기준으로 교체하고, 테스트 컴포넌트 해제는 자신이 시작한 연출만 취소한다. 기존 패링 수치는 유지했다.
- 검증: Unity6000.5.0f1 별도 복사본에서 관련 코드 컴파일 및 PlayMode 45/45 통과, 실패0·생략0, exit0. 새 PlayerHitFeedbackTests5 + 기존 패링8/MP·회피19/전투 팝업13. 실제 피해 시 색·정지·밀림 및 복구, 미부착 시 미발동, 패링 차단 시 빨간색 미발동, 컴포넌트 해제 시 시간/색/입력잠금 복구, 연속 피격 후 원래 색 복구를 검사했다. API 경고 수정 뒤 코드도 이번 실행에 포함했고 이번 연출/수정 검색 호출의 경고는 없었다. 대상 git diff --check 및 테스트 소스 해시 일치 확인.
- 검증 자료/한계: .utmp/ParryFeedback/hit-feedback.xml 및 hit-feedback.log, .utmp/p 복사본(Git 제외). 원본 에디터 화면 재생·실제 MainTestScene 조작·Android 기기는 미실행. 테스트용 컴포넌트는 원본 씬에 아직 부착하지 않았다.
- 직접 확인: Unity 컴파일 완료 → Tools > Play From Title 해제 → MainTestScene의 Player 선택 → Add Component > PlayerHitFeedbackTest → Play → 방어 없이 보스/일반 적에게 맞는다. HP가 줄면서 빨간색, 짧은 정지 후 공격자 반대쪽 밀림, 원래 색 복구가 정상이다. 컴포넌트 체크 해제 후 같은 피격에서는 기존 HP 감소만 나와야 한다. 패링 성공 시 기존 패링 연출만 나오고 빨간색 피격 표시는 없어야 한다.
- 오류 확인/다음: Console 첫 오류, 같은 오브젝트의 Player 존재, 컴포넌트 enabled, Target Renderer 및 네 시간/거리 값, attacker/source 유무를 확인한다. 색만 안 바뀌면 실제 화면을 그리는 SpriteRenderer를 Target Renderer로 지정한다. 다음은 사용자 씬에서 연출 체감 조절이며 테스트 종료 시 컴포넌트를 제거하면 된다. 실제 피해/무적/사망 규칙은 변경하지 않았다.

## 2026-11-04 패링 연출 검색 API 경고 수정

- 사용자 지적 확인: 앞선 green.log에 Player.ParryFeedback.cs 및 CombatPopupController.cs의 FindObjectsSortMode 인자 사용에 대한 CS0618 경고가 있었다. 앞선 40/40 통과는 경고가 없다는 의미가 아니며 경고를 놓쳤다.
- 수정: 이번 연출에서 추가한 두 검색만 FindObjectsByType<GameManager>() 및 FindObjectsByType<Player>()로 변경했다. Unity6000.5 컴파일 경고의 공식 대체 호출 안내에 따른 수정이며 씬 필터·승패/팝업 정지 소유권 로직은 유지한다. 관련 없는 기존 호출은 일괄 변경하지 않았다.
- 검증: 변경 호출 정적 확인 및 대상 git diff --check 통과. 이번 컴파일/Unity 테스트 재실행0개. 앞선 40개 결과를 이번 수정의 재실행 결과로 보고하지 않는다.
- 직접 확인: Unity 컴파일 완료 → Console Clear 후 해당 파일의 CS0618이 새로 발생하는지 확인 → MainTestScene에서 패링 성공 및 일시정지/복귀 확인. 해당 두 호출 경고가 사라지고 정지/밀림/팝업 복귀가 유지되면 정상이다. 오류 시 첫 컴파일 메시지와 실제 Unity 버전을 확인한다. 다음은 원본 에디터 컴파일/실제 화면 확인이다.

## 2026-11-04 패링 성공 히트스톱 및 플레이어 밀림

- 사용자 요청: 패링 성공 시 잠깐 게임을 멈추고 플레이어를 보스 반대 방향으로 조금 밀어내는 연출. 시작 브랜치 BossBeastZombie/88f436b. 기존 BossController 복구·사용자 BossMvp.prefab·Animator·할퀴기 및 body-charge 클립 변경을 보존했다. 기존 수정 파일은 작업 전후 SHA-256 동일. 이번에 씬·프리팹·Project Settings·패키지를 수정하지 않았다.
- 완료: Player.ParryFeedback.cs 부분 클래스에서 성공 연출을 소유한다. 기본 정지 0.08초(실제 시간), 정지 해제 후 공격자 반대 방향으로 거리0.35를 0.12초 동안 이동한다. Player Inspector의 패링 성공 연출에서 세 값을 조절한다. 연출 동안 플레이 입력을 잠깐 잠그고 기존 PlayerMovement/CollisionMovement2D를 사용해 벽을 통과하거나 몸통을 밀지 않게 한다. 새 전역 매니저/싱글톤은 추가하지 않았다.
- 연결: PlayerDefense의 기존 ParrySucceeded 이벤트와 무인자 NotifyIncomingAttackBlocked API를 유지했다. LastParrySource 및 NotifyIncomingAttackBlockedFrom으로 실제 피해 공격자 또는 기존 BossParry 참조를 성공 시 전달한다. 가드·실패에는 연출이 없으며 같은 패링의 다중 Collider 접촉은 기존 성공 플래그로 한 번만 연출/MP 회복한다. 공격자 미상/동일 위치면 임의 방향을 만들지 않고 정지만 적용한다. MP 회복 중 비활성화/재활성화된 경우 새 생명주기에 연출을 시작하지 않는다.
- 정지 수명: Player 비활성화/파괴 시 소유한 정지와 연출을 정리한다. 성공 전 시간 배율을 저장해 복구하며 승패 정지는 GameManager.state로 확인해 유지한다. CombatPopupController.AcquirePause는 플레이어 히트스톱을 먼저 종료한 뒤 원래 배율을 저장하고 팝업 정지를 인수한다. 따라서 히트스톱 중 일시정지·레벨업 팝업이 겹쳐도 조기 재개하거나 팝업 종료 후 0배율에 갇히지 않는다. 팝업 중 밀림 진행도 scaled deltaTime으로 정지한다.
- 회귀: PlayerParryFeedbackTests 8개 추가. 구현 전 별도 Unity 복사본에서 8개 중3 통과/5 실패로 정지·밀림 부재 재현(exit2). 구현 후 Unity6000.5.0f1 컴파일 및 PlayMode 40/40 통과, 실패0·생략0, exit0. 새 연출8 + 기존 PlayerResourceActionTests19 + CombatPopupTests13. 실제 피해 경로 성공/중복 접촉, 일반 가드 미발동, 벽 차단, 팝업 중첩과 기존0.4배율 복구, 비활성화 정지 해제, 승패 정지 유지, 기존0.5배율 복구, 구 BossParry 경로를 검사했다.
- 검증 근거: .utmp/ParryFeedback/red.xml/red.log, green.xml/green.log 및 .utmp/p 검증 복사본(생성물 Git 제외). 변경 런타임/새 테스트와 복사본의 SHA-256 동일, 대상 git diff --check 통과. 검증 복사본에는 전체0.Scripts와 Tests 및 Joystick Pack을 복사하고 기존 로컬 패키지 캐시를 사용했다. 복사본 PlayFromTitle에만 batchmode guard가 있으며 원본 옵션을 변경하지 않았다. 전체 게임 회귀·원본 MainTestScene 실제 화면/모바일 기기는 이번에 실행하지 않았다.
- 사용자 확인: Unity 컴파일 완료 → Tools > Play From Title 해제 → MainTestScene Play → 보스의 손 내리기 타격 직전에 Player/Parry 입력 → 성공 순간 약0.08초 멈췄다가 플레이어가 보스 반대쪽으로 조금 밀리는지 확인. 실패/일반 가드에는 정지·밀림이 없어야 한다. 벽을 등지고 성공해도 통과하지 않아야 하며, 성공 직후 일시정지나 레벨업 팝업을 열었다 닫아도 정상 복귀해야 한다.
- 오류 확인/다음: Console 첫 컴파일 오류, Player Inspector의 패링 성공 연출 세 값, 입력 에셋 Player/Parry 연결, 실제 피격 호출의 source/attacker 전달을 확인한다. 밀림 방향이 없으면 공격자가 null이거나 플레이어와 동일 위치인지 확인한다. 다음은 원본 씬에서 정지 길이/거리 체감 및 실제 터치 입력 확인이다. 추가 무적·보스 넉백·보상·패링 수치 규칙은 이번 연출에 넣지 않았다.

## 2026-11-04 삭제된 보스 돌진 애니메이션 코드 복구

- 사용자 요청으로 BossController.cs에서 삭제된 body-charge 연결을 복구했다. 시작 브랜치 BossBeastZombie/88f436b. 기존 BossMvp.prefab·Animator controller·할퀴기 클립·새 body-charge 클립·인수인계 변경은 보존했다. body-charge 상태와 8방향 클립은 남아 있어 다시 만들지 않았다.
- 복구: dashAnimationRunning 필드, 돌진 시작의 StartDashAnimation 호출, 돌진 종료 및 OnDied/OnDisable의 StopDashAnimation 호출, 두 보조 메서드. 현재 피해 중심(target.position)과 돌진 이동 규칙은 유지했다.
- 검증: 복구 코드 SHA-256이 앞선 검증 복사본의 BossController.cs와 일치하고 git diff --check 통과. 이번 Unity 실행 테스트 0개이며 앞선 5/5 결과를 이번 재실행 결과로 취급하지 않는다.
- 직접 확인: Unity 컴파일 완료 → Tools > Play From Title 해제 → MainTestScene Play → 보스와 10 이상 거리로 벌려 돌진 유도. body-charge로 돌진하고 종료 후 일반 동작으로 돌아오면 정상. 오류 시 Console 첫 컴파일 오류와 missing body-charge state 경고, Boss.controller 연결을 확인한다. 다음 작업은 원본 씬 실제 재생 확인이다.

## 2026-11-04 보스 돌진 body-charge 애니메이션 연결

- 시작 상태: BossBeastZombie, HEAD 88f436b. 이전 할퀴기 선딜레이 16클립·인수인계 변경과 사용자 BossMvp.prefab 수정을 보존했다. 이번 작업 전후 사용자 프리팹 SHA-256 동일. 브랜치·씬·기존 스프라이트 PNG/meta·이전 공격 클립을 변경하지 않았다.
- 완료: 기존 body-charge 원본 8방향×11장을 사용하는 body-charge_{방향}.anim 8개와 meta 추가. 기존 보스 애니메이션과 같은 12fps, 반복 재생. Boss.controller에 body-charge 상태와 FacingX/FacingY를 쓰는 8방향 BlendTree를 추가했고 기존 상태/전이 내용은 그대로 유지했다.
- 실행 연결: BossController.DashToTrackedTarget에서 돌진 시작 시 body-charge를 한 번 재생한다. 기존 추적 방향 갱신으로 방향이 바뀌어도 재생 시간을 매 프레임 초기화하지 않는다. 돌진 완료·장애물 차단 후 종료·대상 소실에 따른 정상 루프 종료 시 Idle로 복귀하며, 사망/컴포넌트 비활성화 때도 재생 상태를 정리한다. 이동·돌진 시간·피해 수치·도착 피해 시점은 기존 규칙을 유지했다. 상태가 없는 다른 Animator를 쓰는 경우 경고 후 기존 돌진 이동은 유지한다.
- 검증: Unity 6000.5.0f1 별도 최소 검증 프로젝트에서 실제 Boss/NormalEnemy/Combat 소스와 보스 아트를 임포트·컴파일, 임시 PlayMode 검사 5/5 통과(실패0). 8방향 11개 Sprite 참조·반복 설정, 실제 돌진 이동 중 body-charge 진입/종료, 방향 변경 시 재생 진행 유지, 비활성화/재활성화, 일시정지 중 이동/애니메이션 정지 및 복귀를 확인했다. 원본 BossController·controller·새 클립/meta와 검증본 해시 일치. 새 controller ID 중복 없음 및 기존 controller 내용 보존 검사 통과.
- 검증 환경/한계: 최초 샌드박스 실행은 라이선스 IPC 연결 거부/초기화 시간 초과로 테스트에 도달하지 못했다. 승인 후 검증 복사본 에디터만 재실행해 위 5개를 완료했다. 원본 에디터를 추가로 열거나 종료하지 않았다. 결과 .utmp/ChargeAnimation/results.xml 및 validation-elevated.log, 임시 검사 코드는 .utmp/c/Assets/Tests(모두 Git 제외). 전체 프로젝트 회귀·원본 MainTestScene 화면·Android 기기는 미검증이다. 별도 임포트 중 기존 idle/north-west 일부 Sprite Rect 경고가 있었으며 이번 body-charge Sprite 참조 검사는 통과했다.
- 차이 검사: 이번 BossController/Boss.controller의 git diff --check 통과. 전체 diff에는 작업 전부터 있던 사용자 BossMvp.prefab의 빈 YAML 값 뒤 공백 3곳이 있어 이를 임의 정리하지 않았다.
- 사용자 확인 순서: Unity 임포트/컴파일 완료 → Tools > Play From Title 해제 → MainTestScene Play → 보스 출현 후 10 이상 거리를 벌려 패턴3 돌진 유도 → 돌진 중 옆으로 움직여 방향 전환 확인 → 도착 후 일반 동작/할퀴기 복귀 확인. 정상은 몸을 낮추는 body-charge 동작이 돌진 동안 반복되고 추적 방향을 따라 바뀌며, 돌진 종료 후 그 자세에 고정되지 않는 상태다. 일시정지/복귀도 확인한다.
- 오류 확인/다음: Console 첫 컴파일/임포트 오류와 missing body-charge state 경고, BossMvp Animator의 Boss.controller 연결, Animator 창의 body-charge 상태 및 각 방향 클립 Sprite 참조를 확인한다. 다음은 실제 원본 씬에서 방향별 화면·기존 할퀴기 선딜레이와의 연결 체감 확인이며 돌진 선딜레이·피해 규칙을 추가로 바꾸지는 않았다.

## 2026-11-04 보스 할퀴기 선딜레이 4프레임 추가

- 사용자 요청: 손을 올린 자세를 약 4프레임 늘려 공격을 보고 대응할 시간을 제공한다. 현재 브랜치 BossBeastZombie, HEAD 88f436b. 변경 전 작업 트리는 깨끗했고 브랜치를 전환하지 않았다.
- 완료: Beast_Zombie_Boss/Animator/Clips의 Boss_Swipe 및 Boss_SwipeLeft 각 8방향, 총 16개 클립. south 원본 그림을 확인해 3.png가 손을 올린 자세이고 4.png가 내려치는 동작임을 확인했다. 3번 자세를 기존보다 4샘플 더 유지하도록 4번 이후 키를 4/12초 뒤로 이동했다. 12fps 기준 추가 선딜레이 약 0.333초. 피해 이벤트 0.4583→0.7917초, 종료 이벤트 0.6583→0.9917초, 클립 길이 0.75→1.0833초. 스프라이트 순서·참조·GUID 및 휘두르는 구간의 속도는 보존했다.
- 검증: PowerShell 정적 검사에서 16/16 클립의 키 개수·스프라이트 참조/순서·4샘플 지연·피해/종료 이벤트·클립 길이를 HEAD와 비교해 통과. 시간 필드 외 내용 동일 확인. git diff --check 통과. Unity 실행 테스트 0개이며 실제 Animator 재생·기기 검증은 하지 않았다. 씬·프리팹·meta·런타임 코드는 수정하지 않았다.
- 참고: BossMvp에는 별도의 EnemyContactDamage가 활성화되어 있어 몸통 접촉 피해는 할퀴기 이벤트와 무관하다. 이번 요청에서 접촉 피해 규칙을 변경하지 않았다. 앞선 EnemyContactDamage 리뷰의 활성 상태 검사 누락/풀 재사용 시 타이머 미초기화도 미수정 상태다.
- 사용자 확인: Unity 임포트 완료 → Tools > Play From Title 해제 → MainTestScene Play → 보스 출현 후 몸통에 닿지 않는 할퀴기 범위에서 오른손·왼손 공격 확인. 손을 올린 자세가 약 0.33초 더 유지되고 내려치는 시점에 할퀴기 피해가 들어오면 정상. Window > Animation > Animation에서 Boss_Swipe_South 및 Boss_SwipeLeft_South를 선택해도 확인할 수 있다. 오류 시 Console 첫 임포트 오류, 실제 Animator의 Boss.controller 연결, 클립 피해 이벤트 위치를 확인한다. 준비 동작 중 HP가 감소하면 몸통 접촉과 다른 적 공격을 먼저 분리한다.
- 다음 작업: 원본 에디터에서 8방향 공격 예고/피해 동기화 및 패턴2 연속 공격의 체감 확인. 몸통 접촉 피해까지 선딜레이 동안 막을지는 별도 동작 변경이므로 이번에 임의 적용하지 않았다.
## 2026-11-04 main에 EnemyEXP·feat/PlayerSkill 병합

- 사용자 요청: `EnemyEXP`와 `feat/PlayerSkill`을 `main`에 병합. 시작 작업 트리는 깨끗한 `EnemyEXP`, HEAD `f42760d`였다. origin fetch 후 main `53c7b5d`, EnemyEXP `f42760d`, feat/PlayerSkill `3d0974b`가 로컬·원격에서 일치함을 확인했다. 기존 브랜치는 보존한다.
- 병합 순서: main에 EnemyEXP를 `f5a3783` merge commit으로 병합한 뒤 feat/PlayerSkill을 병합했다. 두 병합 모두 실제 충돌은 Handoff.md뿐이었다. main·EnemyEXP·스킬 브랜치의 고유 인수인계 섹션을 모두 보존했으며, 아래 과거 기록의 브랜치·HEAD·테스트 수는 기록 당시 값이다. 현재 작업 방향은 이 최신 항목과 각 기능 상세 문서를 기준으로 판단한다.
- 보존 확인: EnemyHealth의 DNA 드롭과 표시 전용 HP 갱신, PlayerData의 EXP API/획득 거리, 스킬 데이터·약공격/패시브·강공격 차징, main의 기존 아트·데이터 수정이 함께 반영됐다. WorldWrap은 폐기 상태이며 Restart의 정리 프레임 분리는 유지된다. MainTestScene의 보스 출현 30초 및 스킬 설정은 feat/PlayerSkill의 원래 직렬화 값이며 병합 과정에서 새 기획 수치를 정하지 않았다.
- 실행 검증: Unity 6000.5.0f1 별도 전체 프로젝트 복사본. EnemyEXP 기준 **181개 중 163 통과/18 실패**, feat/PlayerSkill 단독 기준 **249개 중 231 통과/18 실패**, 병합 결과 **250개 중 232 통과/18 실패**. 모두 생략 0, exit 2. 병합 결과의 실패 18개는 feature 단독 기준과 이름·실패 메시지가 전부 일치하고 새 실패는 없다. 강공격 차징 32/32, 약공격 13/13, 적 HP 표시 7/7, Restart 회귀 1/1 통과. 실패가 없는 전체 테스트로 보고하지 않는다.
- 현재 실패 목록: `docs/AI/HeavyChargeValidation.md`의 18개 목록과 동일하다. GiungTestScene Player 입력 에셋 참조 누락 16개, `PlayerPassiveSkillTests.OutsideRangeOrViewportDoesNotConsumeReadyShot`(탄환 0 기대/1 관측), `PlayerStatusTests.KeyboardSubmitCanEnterAdjustAndLeaveOptionsWithoutLosingSelection`(MasterVolume 대신 OptionsButton 선택). EnemyEXP 기준의 CombatPausePopup 중복 등록 실패는 병합 결과에서는 통과했다. 서로 다른 시점의 18개 실패를 같은 목록이라고 취급하지 않는다.
- 비교 자료: `.utmp/g/merge-enemy-baseline.xml`, `merge-combined-suite.xml`, `merge-skill-baseline.xml`과 대응 로그(Git 제외). 복사본 PlayFromTitle에만 batchmode guard를 넣었고 원본 씬·프리팹·Project Settings·패키지는 Unity로 자동 저장하지 않았다. 원격 push는 이 요청에서 수행하지 않는다.
- 독립 리뷰: 자동 병합 교차 부분의 DNA+HP 표시, EXP+스킬 모델, 삭제된 스크립트 참조, 씬 연결, WorldWrap 폐기·Restart 처리 보존을 확인했다. 추가 회귀나 자동 병합 결함 지적은 없었다. Unity 직렬화 파일의 원래 빈 값 뒤 공백을 보존하며 문서 충돌 해결에 공백 일괄 정리를 섞지 않았다.
- 사용자 확인 순서: main에서 Unity 임포트/컴파일 완료 → `Tools > Play From Title` 체크 시 해제 → MainTestScene Play → N_Attack 약공격 발사와 C_Attack 홀드/방향/단계 표시·해제 확인 → 적 처치 시 DNA 드롭 확인 → 사망 후 Game Over Restart 반복. 정상 결과는 불꽃 탄환·차징 표시가 동작하고, DNA가 생성되며, Restart 후 새 플레이어로 재진입하는 상태다. DNA 유도 거리/속도/EXP가 0이면 드롭 이후 자동 유도·EXP 증가를 기대하지 않는다. 오류 시 Console 첫 오류, Player의 입력 에셋·버튼 참조·MP, EnemyHealth의 픽업 프리팹 연결을 확인한다.
- 남은 검증/다음 작업: 원본 에디터의 병합 후 실제 UI 입력·터치와 Android 빌드/기기 확인. 강공격의 실제 피해·정식 스킬 실행은 이 브랜치에서도 후속이며 차징 기반을 완료된 피해 구현으로 취급하지 않는다. 기존 GiungTestScene 입력 참조·테스트 격리 실패는 별도 범위로 남긴다.

## 2026-11-04 Restart 네이티브 교착 조사 및 Burst 생성 DLL 복구

- 시작 상태: `EnemyEXP`, HEAD `b5e1ad3`. WorldWrap 삭제·MainTestScene 컴포넌트 제거·기존 테스트 삭제·인수인계 변경·사용자 복구 씬과 meta를 보존했다. WorldWrap은 복원하지 않았다.
- 시작 오류 확정 원인: 사용자 첨부 `Failed to find entry-points / Mono.Cecil.AssemblyResolutionException`의 대상인 `Library/ScriptAssemblies/Unity.RenderPipelines.Core.ShaderLibrary.dll`은 4,608바이트 파일이 있지만 PE 헤더가 손상돼 .NET에서 Unknown file format으로 거절됐다. ScriptAssemblies의 다른 DLL 헤더는 정상이었다. 같은 프로젝트 `Library/Bee/artifacts/1900b0aE.dag/post-processed/`의 동일 어셈블리는 정상이어서 손상본을 별도 보관 후 이 파일 하나만 복구했다. 복구 후 어셈블리 이름 읽기와 SHA-256 일치를 확인했고, 실제 에디터의 후속 컴파일·도메인 리로드에서 Burst 오류가 새로 발생하지 않았다. 손상 원인 자체(강제 종료 등)는 확정하지 않았다. 전체 Library 삭제·패키지/Unity 버전 변경은 하지 않았다.
- Restart 재현: 실제 원본 에디터(6000.5.0f1, Direct3D12)에서 적이 스폰된 MainTestScene의 사망·Restart 버튼 입력 후 정지를 관찰했다. 임시 단계 로그상 scene/pool/UI/sound Clear가 모두 종료되고 native LoadScene(MainTestScene)이 반환된 후 다음 씬 완료 이벤트와 heartbeat가 중단됐다. 로그를 읽을 때 이번 세션의 최신 파일은 `%LOCALAPPDATA%/Unity/Editor/Editor.log`였으며, 프로젝트 `Logs/Editor.log`는 09:53 시점의 오래된 파일이었다. 파일 시각을 먼저 확인해야 한다.
- 네이티브 스택: 메인 스레드가 `DelayedDestroyCallback → SpriteRenderer.MainThreadCleanup → AssetChangedNotifier.UnsubscribeFromAll → PersistentManager.Lock`에서 대기했다. 동시에 로딩 스레드는 `PreloadManager → LoadSceneOperation → RenderSettings.Transfer → PersistentManager.PreallocateObjectThreaded → SetObjectLockForRead`에서 대기했다. 종료·스레드 대기는 로컬 PDB로 확인했으며 사용자 에디터는 강제 종료하지 않았다. 증거는 지연 삭제와 새 씬 읽기의 잠금 교착을 가리킨다. 단순 GPU 정지나 WorldWrap 원인으로 단정하지 않는다.
- 수정: GameManager 결과 화면 전환만 `SceneManagerEx.PrepareSceneLoad`로 검증·정리를 먼저 수행하고, 기존 Managers 소유 코루틴에서 한 프레임 뒤 실제 LoadScene을 시작한다. 이는 임의 시간 지연이 아니라 예약된 Destroy를 끝낼 프레임 분리다. 기존 public LoadScene/LoadSceneAsync API 동작과 기존 isTransitioning 가드를 보존한다. 잘못된 씬의 동기 예외 및 다음 프레임의 실제 로드 예외 모두 기존 시간 배율·재시도 가드를 복구한다.
- 회귀: `RestartSceneTransitionTests.RestartButtonAfterSpawningEnemiesCanReloadThreeTimes` 추가. 실제 MainTestScene에서 적 스폰, 렌더러 등록 프레임, 플레이어 사망, 직렬화된 Restart 버튼 이벤트, 이전 적 삭제 완료·이전 씬 유지 프레임, 새 씬 진입을 3회 검사한다. 수정 전 프레임 보장 조건에서 1개 실패, 수정 후 Direct3D12 활성화 검증 복사본에서 **1/1 통과**, exit 0. batchmode 스트레스만으로는 원래 네이티브 교착이 재현되지 않아, 실제 에디터 스택과 프레임 순서 검증을 함께 근거로 사용했다. `GameResultFlowTests`의 단일 프레임 완료 가정은 씬 교체 완료를 제한 시간까지 기다리도록 수정했다.
- 전체 테스트: 최종 코드의 Unity 6000.5.0f1 컴파일 및 PlayMode **181개 중 163 통과/18 실패**, 생략 0, exit 2. 기존 삭제 후 기준(180개 중 162 통과/18 실패)과 실패 18개의 이름·원인이 동일하고 새 실패는 없다. 아래 과거 조사에 기존 실패의 전체 이름과 원인이 기록돼 있다. 새 Restart 회귀는 전체 실행에서도 통과했다. 생성물 `.utmp/g/restart-barrier-before.xml`, `restart-after.xml`, `restart-final-suite.xml` 및 로그. 로컬 진단 스택·손상 DLL 보관본은 `.utmp/RestartInvestigation`에 있으며 Git 제외다.
- 임시 진단 정리: `RestartInvestigationProbe.cs`와 meta, `[RestartProbe]` 런타임 로그를 모두 제거했다. 진단 중 비활성 창에서도 Play를 진행하기 위해 바꾼 runInBackground는 프로세스 내 런타임 값이며 ProjectSettings의 원래 0은 유지했다. 원본 에디터가 이미 교착돼 실행 중인 메모리에는 제거된 진단 코드가 남을 수 있으므로 재실행 후 확인해야 한다. 씬·프리팹·Project Settings·패키지의 이번 요청 추가 변경은 없다.
- 사용자 확인 완료: 사용자가 Unity 종료·재실행 후 **“시작 오류 없고 Restart도 정상”**이라고 확인했다. 확인 절차는 컴파일 완료 → Console의 ShaderLibrary/Burst 시작 오류 확인 → MainTestScene Play → 적 접촉 사망 → Game Over의 Restart 반복이다. 정상 결과는 시작 오류가 없고 재진입 뒤 새 플레이어 이동이 가능한 상태다. 다시 정지하면 강제 종료 전에 정지 단계와 최신 Editor.log/네이티브 스택을 수집한다. Android 빌드·기기는 미검증이다.
- 독립 코드 리뷰: 실제 변경 파일의 검증·정리 프레임 분리, 기존 공개 API 보존, 중복 전환 가드, 동기/지연 예외 복구, 테스트 정리를 확인했고 추가 수정이 필요한 결함은 없었다. `git diff --check` 통과.
- 다음 우선순위: WorldWrap 폐기 상태를 유지한다. 별도 요청 시 기존 GiungTestScene Input Action Asset 참조 누락 16개 및 팝업 관련 2개 테스트 실패를 조사한다. 운영용 전체 카탈로그·번들·Android 빌드·기기 검증은 남아 있다.

## 2026-11-04 사용자 요청으로 무한 맵 구현 폐기

- 현재 결정: WorldWrap2D 기반 무한 맵을 전부 폐기했다. 아래 세 개의 관련 항목은 과거 조사 기록이며, 경계 래핑·시각 복제·World Bounds 설정을 다시 구현하거나 복원할 지침이 아니다.
- 시작 상태: `EnemyEXP`, HEAD `b5e1ad3`. 이전 작업의 WorldWrap 수정·새 전용 테스트와 기존 사용자 삭제 `WorldWrap2DRestartTests.cs`가 있었고, 새 사용자 파일 `Assets/_Recovery/0 (1).unity` 및 meta가 있었다. 기존 테스트 삭제는 유지했다.
- 제거: `Assets/0.Scripts/Game/WorldWrap2D.cs`와 meta, 새 WorldWrap 전용 테스트 3개와 meta. MainTestScene과 복구 씬에서는 WorldWrap2D MonoBehaviour YAML 블록과 플레이어의 컴포넌트 목록 항목만 제거했다. 복구 씬의 나머지 바이트와 meta, 기본 Ground 타일맵·배치, DNA 드롭·EXP 픽업 코드는 보존했다.
- 현재 동작: 맵 경계를 넘어도 반대편으로 순간이동하지 않고 주변 8장 타일맵을 생성하지 않는다. 맵 밖 이동 제한이나 새로운 경계 처리는 추가하지 않았다.
- 검증: Assets 전체 WorldWrap 이름·스크립트 GUID 참조 0건, 씬의 제거 컴포넌트 fileID 참조 0건, `git diff --check` 통과. 두 씬에서 해당 컴포넌트와 목록 항목을 제거한 것 외의 바이트·복구 씬 meta는 작업 직전과 동일함을 대조했다. 별도 전체 프로젝트 복사본에서 Unity 6000.5.0f1 컴파일 및 삭제 후 PlayMode **180개 중 162 통과/18 실패**, 생략 0, exit 2. 실패 18개는 아래 과거 조사에 명시한 기존 실패와 이름·원인이 동일하며 새 실패는 없다. 결과 파일은 `.utmp/g/removal-suite.xml`, 로그는 `removal-suite.log`(Git 제외). 이전의 5/5 통과 기록은 삭제 전 코드에 대한 기록으로 재사용하지 않는다.
- 다음 확인: Unity 임포트·컴파일 완료 → `Tools > Play From Title` 체크 시 해제 → `Assets/9.Scenes/MainTestScene.unity` 열기 → `Player Target` Inspector에서 WorldWrap2D/Missing Script가 없는지 확인 → Play. 기존 경계를 넘어도 순간이동·반복 맵 생성이 없어야 한다. 적 접촉으로 사망 후 Game Over의 Restart를 선택해 재진입과 정상 이동을 확인한다. 오류 시 Console 첫 오류와 Missing Script 위치를 확인한다. 실제 에디터 정지 재발 여부·화면·Android 빌드·기기는 별도 확인이 필요하다.

## [폐기된 구현 기록] 2026-11-04 사망 물리 콜백의 WorldWrap2D 정리 오류 조사

- 시작 상태: `EnemyEXP`, HEAD `b5e1ad3`. 사용자 변경으로 `Assets/Tests/PlayMode/WorldWrap2DRestartTests.cs`가 이미 삭제돼 있었으며 복원하지 않았다. 씬·프리팹·Project Settings·패키지는 원본에서 변경하지 않았다.
- 조사 대상: `dec1a74`(DNA 드롭/무한 맵), `b5e1ad3`(Restart 정지 대응). 후자에서 `UNITY_EDITOR`이면 Play 중에도 `DestroyImmediate`를 호출하도록 바뀌었다. 실제 `Logs/Editor.log`와 사용자 스택에서 `EnemyContactDamage → Player.TakeDamage → SetActive(false) → WorldWrap2D.OnDisable` 경로로 삭제가 거부됨을 확인했다. 실패 후에도 사본 목록·루트 참조를 비우므로 사망 시 정리되지 않은 맵이 남을 수 있다. DNA 드롭 코드가 이 오류를 발생시킨다는 근거는 없다.
- 수정: `WorldWrap2D.ClearVisualCopies`에서 루트를 비활성화하고 자식 8장을 루트와 함께 한 번에 정리한다. 에디터도 `Application.isPlaying`일 때 `Destroy`를 쓰며, Play 밖에서만 `DestroyImmediate`를 쓴다. 프레임당 사본 1장 생성과 경계 래핑은 유지한다.
- 회귀 테스트: 새 `WorldWrapPhysicsCleanupTests` 4개(실제 Collision2D 콜백 중 비활성화+timeScale 0, 같은 프레임 재활성화 3회, 생성 도중 파괴, 씬 언로드 3회). `WorldWrapContactDisableProbe`는 테스트 어셈블리에만 둔다. 기본 Assembly-CSharp 타입은 reflection으로 접근해 기존 asmdef 구조를 바꾸지 않는다. `WorldWrapGameRestartTests`는 실제 MainTestScene에서 사망/Restart 3회 경로를 검사한다.
- 실행 검증: 별도 최소 프로젝트, Unity 6000.5.0f1에서 수정 전 4개 중 1개가 사용자와 동일한 물리 콜백 DestroyImmediate 오류로 실패(3개 통과), 수정 후 **4/4 통과**, exit 0. 전체 프로젝트 복사본에서 새 테스트를 포함한 PlayMode 185개 실행: HEAD의 수정 전 WorldWrap 코드 **166 통과/19 실패**, 최종 코드 **167 통과/18 실패**, 생략 0, 양쪽 exit 2. 사라진 실패는 물리 콜백 테스트 1개뿐이며 나머지 18개는 이름·실패 원인이 동일하다. 최종 WorldWrap 테스트 **5/5 통과**(실제 MainTestScene의 TakeDamage/RestartGame 직접 호출 3회 포함). 실제 접촉·버튼 입력·화면은 수동 확인이 필요하다. 원본 사용자 에디터를 추가로 열지 않았고, 복사본 PlayFromTitle 초기화에만 batchmode guard를 넣었다.
- 검증 자료 위치(생성물, Git 제외): `.utmp/WrapCleanupCheck/before.xml`, `after.xml`, `.utmp/g/baseline-suite.xml`, `final-suite.xml` 및 각각의 로그. 영속 검증 요약은 이 항목의 수치와 실패 목록을 기준으로 한다. 초기 sandbox 검증은 라이선스 연결에서 진행되지 않아 별도 검증 프로세스를 경로 대조 후 종료했고, 권한 제한 밖의 독립 복사본에서 실제 테스트를 수행했다.
- 기존 실패 16개는 `Player requires an Input Action Asset reference.`로 GiungTestScene 진입 중 발생한다. `CombatPopupSceneTests`: `BossFightKeepsTimerPausedButCombatResumesAfterBack`, `BringingAnOpenPausePopupToFrontRestoresButtonFocus`, `KoreanLabelsRenderAndLevelContentRemainsEmpty`, `PausedPlayerDoesNotStartAutoAttackWhenCooldownReady`, `PopupRaycastBlocksWorldAndBackWorksWhilePaused`, `SceneButtonsFreezeCombatAndResumeOnlyAfterBothPopupsClose`, `SceneUnloadReleasesPauseAndSceneOwnedPopupInstances`. `GameResultFlowTests`: `ActualDeathNotificationIsSentOnce`, `ActualPlayerDeathReachesDefeat`, `DeathWinsWhenBothSignalsArriveInSameFrame`, `InvalidRestartPreservesResultAndAllowsRetry`, `RestartReturnsToGiungWithFreshStateAndOwnedManagers`, `RestoringLoadingRouteReachesMainWithPreparationReady`, `ReturnToTitleCleansPopupsPoolAndSound`, `VictoryClosesBothPopupsAndFocusesResult`. `PooledEnemySpawningTests.GiungSceneUsesPoolAndBossPauseReturnsNormalEnemies`.
- 기존 실패 나머지 2개: `PlayerStatusTests.ActualPausePrefabKeepsPauseThroughOptionsAndResumesOnBack`(CombatPausePopup 중복 등록), `PlayerStatusTests.KeyboardSubmitCanEnterAdjustAndLeaveOptionsWithoutLosingSelection`(MasterVolume 대신 OptionsButton 선택). 이번 맵 정리 수정 범위에서는 변경하지 않았다.
- 판단 제한: 이전 항목의 “지연 Destroy가 에디터 정지의 원인”이라는 설명은 실행 검증 없이 기록된 가설이었고 확정 사실로 사용하면 안 된다. 실제 씬의 사망/Restart 3회 자동 테스트는 수정 전에도 통과했으므로 사용자 환경의 에디터 전체 멈춤 자체는 이번 batchmode 검증에서 재현되지 않았다. 물리 오류의 원인과 수정 효과만 확정할 수 있다. 화면 이음새·Android 빌드·기기는 미검증이다.
- 독립 리뷰: 런타임 변경에 추가 지적 없음. 실제 씬 테스트의 종료 후 씬·영속 루트 정리 누락 1건을 수정하고 재검토 완료. 최종 테스트는 이 정리와 정상 정보 로그를 실패로 취급하던 검사 제거까지 반영한 코드로 실행했다. `git diff --check` 통과.
- 다음 우선순위: 아래 사용자 확인 절차로 원본 에디터의 실제 사망/Restart 정지 재발 여부 확인. 재발하면 클릭 전후 마지막 Console/Editor 로그와 에디터 스택을 수집해 씬 정리·새 맵 생성 중 정지 단계를 분리한다. 별도 작업으로 GiungTestScene 입력 참조 누락과 팝업 테스트 기존 실패 18개를 조사한다.
- 사용자 확인: Unity 컴파일 완료 → `Tools > Play From Title` 체크 시 해제 후 `Assets/9.Scenes/MainTestScene.unity` 열기 → Play → 적 접촉으로 실제 사망 → Game Over의 Restart 선택을 3회 반복 → Play 중지/재진입. 사망 프레임 다음에 `WorldWrap Visuals`가 제거되고, 재시작 뒤 루트 1개와 자식 8개가 다시 생성되며 플레이어 이동/경계 래핑이 동작해야 한다. Console에 물리 콜백 DestroyImmediate 오류가 없어야 한다. 재발 시 첫 오류와 마지막 정상 동작(사망/클릭/씬 로딩/새 플레이어 생성 중 어느 단계인지)을 기록한다. 정지 상태에서 사용자 에디터를 강제 종료하거나 미저장 씬을 덮어쓰지 않는다.
- 자동 테스트 메뉴: `Window > General > Test Runner > PlayMode`에서 `WorldWrapPhysicsCleanupTests`, `WorldWrapGameRestartTests` 실행. 환경에 따라 `Window > Test Runner`로 표시될 수 있다.

## [폐기된 구현 기록] 2026-11-04 WorldWrap2D 리스타트 시 Unity 정지

- 시작 상태: `EnemyEXP`, HEAD `dec1a74`. `main`에는 없고 이 브랜치의 `WorldWrap2D`만 해당.
- 원인: MainTestScene Ground 타일맵(약 2,262칸) Grid를 Play 진입 때 8장 동기 `Instantiate`하고, Play 종료·씬 리스타트 때 `Destroy` 지연 정리에 의존했다. 대용량 Tilemap 사본이 한 프레임에 생성·반쯤 남은 채로 정리되면 에디터까지 멈춘 것처럼 보인다.
- 수정: `Assets/0.Scripts/Game/WorldWrap2D.cs`에서 래핑은 즉시 유지하고, 시각 복제는 프레임당 1장 생성. 사본은 `DontSave`로 씬에 저장되지 않게 하며 `WorldWrap Visuals` 아래에 둔다. `OnDisable`에서 코루틴 중지 후 에디터는 `DestroyImmediate`로 즉시 제거. 사본의 Collider/Rigidbody/MonoBehaviour는 제거해 게임플레이 중복을 막는다.
- 회귀 테스트: `Assets/Tests/PlayMode/WorldWrap2DRestartTests.cs` — 비활성/재활성/파괴 후 `(Wrap …)` 사본과 `WorldWrap Visuals`가 남지 않는지 검사.
- 검증: 코드 정적 확인. batchmode Unity는 라이선스 없음(exit 198)으로 테스트 실행 실패. 에디터 Play 확인은 사용자 환경에서 필요. 실행한 통과 테스트 0개.
- 확인 순서: EnemyEXP에서 Unity로 MainTestScene → Play → 가장자리 통과해 반대편·바닥 반복 확인 → Game Over 리스타트 또는 Play 중지 후 다시 Play. Hierarchy에 `Ground (Wrap …)` / `WorldWrap Visuals`가 Play 종료 후 남지 않고, Unity가 멈추지 않으면 정상. 오류 시 Console 첫 줄과 Player Target의 WorldWrap2D Source Tilemap 연결을 본다.

## [폐기된 구현 기록] 2026-11-04 MainTestScene 반복 맵과 플레이어 경계 래핑

- 작업 시작 시 `EnemyEXP`, HEAD `1952999`. 적 EXP DNA 변경이 작업 트리에 이미 있어 보존했다. MainTestScene은 작업 전 미변경 상태였다.
- `Assets/9.Scenes/MainTestScene.unity`의 `Player Target > WorldWrap2D`가 플레이어 경계 래핑과 반복 타일맵 복제를 담당한다. 경계는 Inspector의 `World Bounds` Rect(X/Y/Width/Height, 월드 좌표)로 지정하며 현재 기본값은 타일맵 크기에 맞춘 (-37, -21, 67, 37)이다. 플레이어가 x/y 경계를 넘으면 반대편 좌표로 래핑한다. Scene 뷰에서 Player Target을 선택하면 청록색 와이어 사각형과 크기 라벨이 보인다.
- 타일맵 Grid는 8방향으로 런타임 복제되어 경계 바깥을 채운다. 실행 중 원본 맵을 설정한 Rect 중앙에 정렬하며, Rect 크기가 실제 타일맵 크기와 다르면 연속 이음새가 맞지 않을 수 있어 경고를 남긴다. 복제 오브젝트는 플레이어 오브젝트가 파괴될 때 정리되며 씬 파일에는 원본 Ground와 플레이어 컴포넌트만 저장된다.
- 확인 순서: Unity 임포트/컴파일 후 MainTestScene의 Hierarchy에서 `Player Target` 선택 → Inspector의 `World Bounds`에서 X/Y/Width/Height 조정 → Scene 뷰의 청록색 사각형이 경계와 일치하는지 확인 → Play → 기존 이동 입력으로 가장자리/모서리를 통과한다. 반대편에서 나타나고 바닥이 이어져야 한다. Play 종료/재시작 후 `Ground (Wrap …)` 런타임 복제물이 남지 않아야 한다. 오류 시 Console 첫 오류, Source Tilemap 연결, Rect와 타일맵 크기 일치 여부를 확인한다.
- 검증: 코드·씬 연결과 `git diff --check` 정적 확인. Unity 컴파일/Play 및 경계 타일의 실제 화면 이음새는 확인하지 않음. 테스트 추가/실행 0개.
- 다음 작업: 위 Inspector·Scene 뷰·Play 절차로 경계 크기 조정과 가장자리 이음새를 확인한다. 적과 투사체 등 다른 월드 오브젝트의 래핑은 이번 범위에 추가하지 않았다.
- 후속 문서화: `WorldWrap2D.cs`의 필드와 Unity 생명주기 메서드, 경계 계산·래핑·타일맵 복제·Scene 기즈모·정리 코드에 한국어 설명 주석을 추가했다. 런타임 동작은 변경하지 않았다.

## 2026-11-04 ExperiencePickup 참조 컴파일 오류 수정

- 사용자 보고: `FindFirstObjectByType<PlayerData>()`가 더 이상 사용되지 않는다는 표시와 `ExperiencePickup` 이름을 찾지 못하는 오류.
- 원인: `EnemyHealth`는 `EnemyMvp.Runtime` asmdef 안에 있고 `ExperiencePickup`은 기본 `Assembly-CSharp`에 있어, asmdef가 기본 어셈블리 타입을 직접 참조할 수 없었다.
- 수정: `EnemyMvp.Runtime`에 `IExperiencePickup` 계약을 추가하고 `ExperiencePickup`이 구현하도록 변경. `EnemyHealth`는 인터페이스만 참조해 픽업 목표를 설정한다. 검색 API는 `FindAnyObjectByType<PlayerData>()`로 변경했다.
- 검증: Unity Editor에서 강제 스크립트 재컴파일 및 도메인 리로드 완료(리로드 프로파일 약 10.4초). 최신 `Logs/Editor.log`에서 `ExperiencePickup`/`IExperiencePickup` 관련 컴파일 오류가 더 이상 보이지 않음. Unity 테스트/Play는 실행하지 않음. `CombatPopupController.cs`의 `FindObjectsByType<T>(FindObjectsSortMode)` obsolete 경고는 별도로 남아 있음.
- 다음 확인: 앞 항목의 DNA 드롭/유도 절차를 진행한다. 해당 경고는 이번 픽업 오류와 별개이므로 별도 정리 작업에서 다룬다.

## 2026-11-04 적 사망 EXP DNA 픽업

- 시작 상태: `EnemyEXP`, HEAD `1952999`, 작업 트리에서 이 작업 전에는 변경 없음. 작업 범위는 적 사망 시 `Ingame_DNA_0` 생성과 플레이어 EXP 획득 반경/유도/회수 연결.
- 완료: `Assets/1.Prefabs/Ingame_DNA_0.prefab`을 기존 `Ingame_DNA_0` 스프라이트로 생성하고 trigger collider와 `ExperiencePickup`을 추가. `EnemyHealth` 사망 시 연결된 프리팹을 사망 위치에서 생성하며, `EnemyFollow.Target`을 픽업에 넘김. EnemyMvp·ZombieHound·BossMvp의 `EnemyHealth`에 해당 프리팹 참조를 연결.
- `PlayerData.experiencePickupDistance`를 추가. 픽업은 플레이어 중심의 설정 거리 안에서 `attractionSpeed`로 이동하고, 플레이어와 접촉하면 설정된 `experienceAmount`를 `PlayerData.AddEXP`로 더한 뒤 사라짐. EXP 요구량을 넘는 값은 현재 모델 계약대로 보존.
- 기획 수치는 미정으로 합의됨: 플레이어 획득 거리, 프리팹 유도 속도, DNA당 EXP를 Inspector 설정으로 노출하고 모두 0으로 둠. 값이 설정되기 전에는 자동 유도/EXP 증가는 작동하지 않음.
- 검증: 코드·프리팹 참조 정적 확인 및 `git diff --check` 통과. Unity 자동 테스트와 에디터 컴파일/Play 검증은 실행하지 않음. `.meta` GUID 및 프리팹 Inspector에서 실제 참조, Trigger 수집 동작은 다음 확인 필요.
- 다음 확인: Unity 임포트/컴파일 후 `Ingame_DNA_0` 프리팹의 Experience Amount와 Attraction Speed, PlayerData의 Experience Pickup Distance를 기획값으로 입력한다. Play에서 적 처치 → 사망 지점 DNA 생성 → 반경 밖에서는 정지 → 반경 안에서는 플레이어로 이동 → 접촉 시 EXP와 HUD 갱신을 확인하고 Console을 확인한다.

## 2026-11-03 EnemyMvp 애니메이션 및 유실 설정 복원

- 시작 상태: EnemyEXP,MAP, HEAD b71b791. Git 변경은 Assets/_Recovery 및 meta만 있었고 이전 대화에서 추가한 애니메이션과 사용자 수정이 사라진 상태였다. _Recovery는 보존했다.
- 복원: Zombie/Animator에 8방향 대기 4프레임·이동 8프레임의 12fps 루프 클립 16개와 Zombie.controller 재생성. EnemyMvp에 Animator와 기존 EnemyDirectionAnimator 연결. 대기·이동 상태 Speed 0.5로 실효 6fps(프레임 간격 약 0.167초).
- 대화에서 확인한 사용자 설정 복원: EnemyMvp의 남쪽 좀비 스프라이트, maxHealth 10, 빨간 피격 색, 콜라이더 oldSize 0.3×0.6, EnemyHealth UI 헤더. 콜라이더 실제 크기와 이동 속도 3 유지. 컴포넌트 직렬화 순서는 현재 순서 유지. 씬과 복구 파일은 수정하지 않음.
- 검증: 클립 16개·소스 스프라이트 96개 참조와 컨트롤러 참조 16개, Speed 0.5 두 상태, 프리팹 ID 중복 없음 검사 통과. Unity 테스트 0개. 에디터 임포트·컴파일·재생 및 빌드·기기 미검증. 이전 Unity 먹통 원인은 확정하지 못함.
- 다음 확인: Unity 임포트 완료 → Assets/1.Prefabs/EnemyMvp 선택 → Animator Controller Zombie, EnemyDirectionAnimator 연결, EnemyHealth Max Health 10과 빨간 Hit Color 확인 → MainTestScene 등 EnemyMvp 사용 씬에서 Play → 8방향 추적 중 이동 모션, 정지 시 대기 모션을 느리게 반복하는지 확인. 오류 시 Console과 Controller 참조 확인. 모르는 미저장 씬 변경은 이 복원에 포함되지 않음.

## 최신 작업 상태 — 2026-11-03 강공격 차징 기반 완료

- `docs/AI/HeavyChargeHandoff.md`, 사용법 `docs/AI/HeavyCharge.md`, 상세 검증 `docs/AI/HeavyChargeValidation.md`를 함께 읽는다. 아래의 ‘종료 요청: 구현 중’ 기록은 이전 세션의 역사이며 현재 상태가 아니다.
- 시작/종료 브랜치 `feat/PlayerSkill`, HEAD `526aaf6` (`강공격 작업중`), 시작 작업 트리 깨끗함. 이전 초안은 이미 커밋되어 있었다. 이번 작업은 미커밋으로 유지했으며 브랜치 전환 없음.
- PlayerView의 빠진 SetHeavyChargeIndicator를 구현했다. 재사용하는 LineRenderer 화살표가 청록/노랑/주황으로 세 단계를 표시하고 진행도에 따라 길이가 늘어난다. 해제/취소/비활성화 시 숨김, Dispose 시 자식과 Material 정리. 이 길이는 피해 범위가 아니다.
- MainTestScene에 C_Attack Button452539071과 승인 설정 MP10/단계0,.4,.8초/증분.1,.1,.5/SuperArmor=false만 13줄 추가했다. 새 런타임3·테스트2개의 Unity 생성 meta를 반영했다. 기존 GUID·다른 씬·프리팹·설정·패키지 보존.
- 리뷰 결함4개를 실패 테스트로 재현 후 수정: 정지 중 Q 해제 즉시취소, 키 해제와 동시 입력된 실제 방어/회피 우선취소, 포커스 복귀 후 누락된 PointerUp 예약 정리, 조이스틱 중립에서 마지막 실제 각도 유지. 실패한 회피는 유지하고 취소 MP는 반환하지 않는다.
- Unity6000.5.0f1 별도 전체 자산 검증본에서 C# 컴파일 성공. 최종 차징 **32/32 통과**(단위12+통합20). 전체 PlayMode **249개 중231통과/18실패/생략0**. HEAD 입력 코드를 검증본에만 적용한 기준 비교는227통과/22실패였고, 차징4개만 통과로 바뀌었으며 나머지18개는 같은 실패다. 원래 HEAD는 View 메서드가 없어 컴파일 불가이므로 기준 비교에도 완성 View를 유지했다.
- 기존18실패: GiungTestScene Player의 InputAction 참조 누락으로 CombatPopupSceneTests7/GameResultFlowTests8/풀링 씬1, 전체 실행 시 이전 씬 카메라·탄환 영향 PlayerPassiveSkillTests1, EventSystem 포커스 간섭 PlayerStatusTests1. 이름과 실제 메시지는 HeavyChargeValidation.md에 기록. 이번 차징 범위 밖의 씬/기존 테스트를 일괄 수정하지 않았다.
- 별도 전체 자산 검증본에서 MainTestScene을 실제 PlayMode 로드해 직렬화된 C_Attack의 PointerDown/Up → MP10 차감 → 3단계/1.7배 해제 → 표시 숨김을 확인했다. 씬 연결 smoke **1/1 통과**. 원본 에디터 수동 조작/실제 터치 검증은 아니다.
- 그래픽 사용 오프스크린 화살표 검증1/1 통과 및 PNG 직접 확인. 독립 리뷰4건 모두 수정·재검증. 실제 원본 에디터 수동 플레이/실제 터치·Android/실제 카탈로그·번들은 미검증. 실제 피해·스킬ID·공격 애니메이션·그로기는 승인된 후속 범위다.
- 직접 확인: Tools > Play From Title 끄기 → MainTestScene Play → C_Attack 홀드 → MP10 한 번 차감·청록→.4초 노랑→.8초 주황·이동 고정/조준 변경 → 해제 시 숨김·이동/패시브 복구. PC는 PlayerController2D를 Play 중 끄고 Q+이동 키. 방어/회피 성공·피격/사망·정지 중 해제/포커스 취소도 확인한다. 오류 시 Console 첫 새 오류와 C_Attack 참조/Configured/배열/MP/입력 소스를 확인한다. 자세한 순서는 HeavyCharge.md.
- 다음: 사용자 실제 조작 피드백 확인. 별도 작업으로 기존 씬 입력 연결/테스트 간 간섭18개를 정리하고, 정식 테이블이 준비되면 강공격 결과 이벤트를 실제 스킬 실행에 연결한다. 미정 수치·보상·피격 경직·그로기 조건은 임의 구현하지 않는다.

## 이전 세션 기록 — 2026-11-03 종료 요청: 강공격 차징 구현 중

**다음 세션은 이 절과 `docs/AI/HeavyChargeHandoff.md`부터 읽는다.** 사용자가 컴퓨터를 끄기 위해 작업을 중단하고 인수인계를 요청했다. 강공격은 아직 완료/컴파일 검증되지 않았다. 아래의 이전 완료 기록을 현재 차징 변경의 통과 결과로 쓰지 않는다.

- 실제 확인한 상태: `feat/PlayerSkill`, HEAD `3da8308` (`테스트용 불꽃 탄환`). 앞선 약공격·패시브·보스 HP 수정은 이 커밋에 포함되어 현재 작업 트리 변경 목록에는 없음. 다음 세션에서도 git 상태를 새로 확인한다.
- 미커밋 변경: Player.cs 및 Mobile/PlayerController2D.cs. 미추적: Player.HeavyCharge.cs, PlayerHeavyCharge.cs, PlayerHeavyAttackButton.cs, PlayerHeavyChargeTests.cs, PlayerHeavyChargeIntegrationTests.cs, 강공격 계획 문서. 이 인수인계 문서 변경도 추가됨. reset/clean/restore 또는 브랜치 전환으로 없애지 않는다.
- 승인 완료: 차징 중 이동 차단·조준 변경 허용. 방어/회피/피격 취소 시 MP 반환 없음. 테스트 MP10, 단계 진입0/.4/.8초, 누적 배율1.1/1.2/1.7. 이 수치는 Inspector에서 변경 가능하게 한다. 정식 데이터 테이블 연결은 준비될 때까지 미룬다.
- 구현 초안: 비용1회/스냅샷/3단계/해제 결과/취소 엔진, C_Attack 포인터 bridge, Player partial 연결, 모바일 이동·조준 분리까지 작성. 실제 강공격 피해/스킬 ID/공격 애니메이션/그로기는 이번 기반 범위가 아님.
- **현재 막힌 지점:** Player.HeavyCharge.cs가 호출하는 `PlayerView.SetHeavyChargeIndicator`가 아직 구현되지 않음. 현 소스는 컴파일 가능한 완료 상태가 아니며 실제 변경 후 컴파일은 아직 실행하지 않았다. PlayerView 표시/정리, MainTestScene의 C_Attack 참조와 승인 설정, 새 파일 meta가 미완료.
- 검증: 구현 전 새 단위 테스트12개 모두 실패(차징 타입 부재)를 확인한 것뿐. 구현 후 재컴파일/통과 테스트/통합 테스트/리뷰는 미실행. 마지막 상태 확인 시 실행 중 Unity 프로세스 없음. 컴퓨터 종료/재시작 명령을 실행한 적 없음.
- 다음 순서: 표시 메서드와 정리 구현 → C_Attack/테스트 설정 최소 연결 → meta 준비 → 별도 검증 프로젝트에 최신 파일 반영 → 차징 단위·통합 및 관련 회귀 테스트 → 코드 리뷰/수정 → 실제 확인 절차·Handoff 갱신. 상세 API/파일/주의점은 HeavyChargeHandoff.md에 기록함.

## 2026-11-03 첫 약공격 불꽃 탄환 연결

- 사용자 요청으로 이전 작업순서1(병합 전체 선행 재검증)을 생략하고2(첫 스킬 연결)를 진행했다. 시작/현재 `feat/PlayerSkill`, HEAD `ccac76f`, 시작 작업 트리 깨끗함. 임의 브랜치 전환·커밋 없음.
- 사용자 확정: 불꽃 탄환10001, MP10, 피해10, 기존 Bullet 재사용+임시 빨간 원, 기존8방향 Attack 프레임, 모바일 버튼 N_Attack. 기타 미정 스킬 수치를 채우지 않았다.
- Data.SkillData에 ActiveConfigured/MPCost/BaseDamage를 추가하고 기존 DamageMultiplier와 함께 실행 설정을 검증한다. PlayerLightSkill이 보유/약공격 장착·리소스·애니메이션·MP를 확인한 뒤 방향/피해/속도/타이밍을 캡처해 시전한다. 메타데이터만 있는 스킬은 무소모·미발동.
- PlayerView와 LightAttackAnimationState가 Animator의 실제 시작/진행/종료를 실행 ID로 연결한다. 초기 즉발 시점0에서 풀의 빨간 탄환을 한 번 발사하고 상태 종료 시 잠금을 해제한다. 약공격 중 이동/공격/방어/회피 거절, 중복/예약 입력 방지, 정지·사망·비활성화/Animator중단 정리. MP 알림 중 pause/Animator무효화는 시작 전 환불하며 수락한 설정은 스냅샷으로 유지한다.
- 기존 Player/Attack을 선택 복제해 PC 입력을 재사용했다. N_Attack은 런타임 PointerDown/Submit bridge로 같은 API를 호출한다. Touchscreen의 일반 tap 입력은 중복 처리하지 않으며 Mouse는 현재 위치 GraphicRaycaster 검사로 UI 클릭을 배제한다. InputAction 에셋·기존 N_Attack onClick은 변경하지 않았다.
- Bullet.InitDirection을 추가하되 기존 Init API와 풀링 계약 유지. 새 FlameBullet은 기존 Bullet/Collider/Rigidbody를 복제 재사용하고 빨간 Sprite로 표시한다. 원본 Bullet.prefab 해시 유지.
- 기존 Player.controller20개 객체를 모두 보존하고 LightAttack 상태/트리/Behaviour/종료 전이4개를 추가했다. Controller meta/GUID 유지. 새8방향 클립은9프레임/12fps, Unity 조회0.75초의 초기 연출값이다. 처음에는 끝 중복 키로0.8333초였음을 검사로 발견해 중복 제거/stopTime 명시 후 재검증했다. 기존 Animator 전체 Generate는 실행하지 않았다.
- MainTestScene의 기존 스탯/MP설정(공격력3, 탄속3, 최대MP100·회복1/s 등)을 유지하고 Player N_Attack Button1415531113/새 투사체 참조, 첫 빈 보유 슬롯10001/LightAttack/MP10/Base10/ActiveConfigured, lightSkillId10001만 최소 변경했다(추가9줄/교체4줄). 이 씬의 승인된 검증 설정이며 모든 PlayerData 기본값에 자동 지급하지 않는다. 전투 피해는 Base10+기본공격력×Multiplier0=10이다.
- 새 리소스 Assets/Resources/SkillProjectiles/FlameBullet.prefab·FlameBulletCircle.png, LightAttack_*.anim8개, Editor의 PlayerLightAttackSetup 및 런타임3개 클래스/테스트 추가. 원본 워크북/다른 씬/기존 프리팹/입력 에셋/패키지 수정 없음. 생성 작업은 이전 검증 복사본에서 Editor API로 수행해 확인 후 원본에 반영했다.
- TDD: 최초7개 실패 → 구현7/7 통과. MP 알림 pause/Animator변경·메타데이터 스냅샷 실패를 추가 재현해 수정. PC WasPressed는 테스트 InputSystem 갱신 프레임에서 검사하도록 보완했다. UI 첫 클릭은 유효한 화면 배치/포인터 캐시 미갱신 상태로 재현해 현재 위치 Raycast로 수정했다.
- 최종 Unity6000.5.0f1 변경 관련 검증 **73/73 통과**, 실패/생략0: 신규 PlayerLightSkillTests13 + MP19 + 탄환6 + HUD12 + 상태창9 + 팝업13 + 임시 그래픽 렌더1. 현재0.Scripts 소스를 검증본에 반영했고 원본에서 삭제된 과거 MVP파일은 검증 복사본에서만 정리했다. 이는 요청대로 전체 병합 회귀/보스 통합 검증은 아니다.
- 독립 리뷰에서 MP 재진입 경계와 클립 길이를 확인해 수정 후 재검증했다. 추가 확정 런타임 결함은 없다는 정적 리뷰 결과를 받았다. 실제 공격 자세/빨간 원을 오프스크린 카메라로 렌더해 픽셀과 이미지로 확인했다. 원본 MainTestScene 실제 플레이/터치·보스 전체·Android 빌드/기기는 미검증.
- 영속 테스트 Assets/Tests/PlayMode/PlayerLightSkillTests.cs(13), 계획 docs/superpowers/plans/2026-11-03-flame-light-skill.md, 사용법 docs/AI/FlameBullet.md. 결과 `.utmp/FlameSkill/red.xml`, `review-red.xml`, `final.xml`, `final.log`, `flame-cast.png`, 검증 위치 validation-path.txt. 렌더 헬퍼는 같은 임시 폴더에만 있으며 원본 테스트 에셋에는 넣지 않았다.
- 직접 확인: 컴파일 완료 → MainTestScene → Play(필요하면 Tools > Play From Title 끄기) → N_Attack → 빨간 직선탄/MP10 차감/Attack 모션 중 이동·연타·패링·회피 거절/종료 후 복구 확인. PC는 PlayerController2D를 끄고 월드 마우스 왼쪽 클릭 또는 UI 선택이 없는 Enter. 이동 입력/마지막 바라본 방향이며 마우스 위치 유도는 아니다. 기존 MP회복1/s는 유지하므로 차감 후 서서히 회복한다. 일시정지 상태창 첫 칸의 불꽃 탄환 보유도 확인한다.
- 오류 시 Console 첫 오류, Player Light Attack Button=N_Attack, Light Attack Projectile=FlameBullet, Loadout10001 장착/ActiveConfigured/MPCost/BaseDamage/AnimName 및 Animator LightAttack Behaviour/Idle 전이를 확인한다. 정상 자동 탄환은 별도로 계속 발사된다. 분리 검증은 Play 중 기존 Player.bulletPrefab만 잠시 비우고 저장하지 않는다.
- 다음 우선순위: 사용자 실제 플레이 확인 후 첫 스킬 연출속도/조준·입력 피드백 조정, 스킬 테이블의 승인 수치/리소스 정식 로딩과 획득 경로 연결. 보유 전체 패시브 발사, 다른 약공격, 강공격·전략·성장·MP 부족 점멸·미정 그로기는 아직 후속이다.

## 2026-11-03 CombatPopupController CS0618 경고 수정

- 현재 브랜치 `main`, HEAD `756c2ee`. 이전 작업은 `dcc8e43`에 커밋됐고 이후 보스 병합이 들어온 상태다. 시작 시 Player_GiungTest 스크립트/asmdef 및 meta 삭제, Boss.controller·MainTestScene·NanumGothic Combat SDF 사용자 변경이 있었으며 건드리지 않았다.
- CombatPopupController.ShowPause의 `FindObjectsByType<Player>(FindObjectsSortMode.None)`를 Unity6000.5의 인자 없는 `FindObjectsByType<Player>()`로 한 줄 변경했다. 같은 씬 필터와 첫 Player 선택 동작은 유지한다. 다른 파일의 검색 호출이나 사용자 변경은 일괄 수정하지 않았다.
- 검증: 이전 임시 Unity6000.5 검증본에 해당 파일을 반영해 C# 컴파일 및 CombatPopupTests13 + PlayerStatusTests9 = **22/22 통과**, 실패/생략0. 해당 파일의 CS0618/컴파일 오류 로그가 없는지 확인했다. git diff --check 통과. 현재 보스 병합·사용자 변경까지 포함한 원본 전체 프로젝트 검증은 아니다.
- 결과 `.utmp/PopupApiWarning/final.xml`, `final.log`. 새 테스트는 추가하지 않고 기존 관련 회귀를 실행했다. 커밋·브랜치 전환 없음.
- 직접 확인: Unity 재컴파일 완료 → Console에서 CombatPopupController의 해당 CS0618 경고가 새로 발생하지 않는지 확인 → MainTestScene → Play → 일시정지 상태창 열기/뒤로가기. 기존과 같이 스탯·보유 목록 표시 및 전투 정지/재개가 정상이어야 한다. 오류가 남으면 파일/줄 번호가 같은지, Console 첫 새 오류와 상세 스택을 확인한다.
- 다음 작업은 기존 우선순위를 유지한다: 실제 스킬 실행/약공격 버튼·애니메이션·타격 연결. 병합 후 전체 프로젝트 기준 검증과 실제 원본 씬/기기 검증은 별도다.

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

## 2026-11-03 보유 스킬 패시브 자동 발사 연결

- 시작 상태: feat/PlayerSkill, HEAD ccac76f. 이전 불꽃 탄환 약공격 관련 코드/씬/컨트롤러/리소스/테스트/문서 변경과 열린 테이블의 임시 잠금 파일을 보존. 커밋·브랜치 변경·테이블 수정 없음.
- 사용자 승인: 불꽃 탄환10001 패시브 피해6/간격3초/탄속3/사거리6. 빨간 원 프리팹 재사용. 약공격 MP10/피해10 유지.
- 완료: PlayerCombat의 기본탄 자동 발사를 보유 공격 스킬 순회로 교체. 장착 버튼과 무관하게 각 보유 스킬의 독립 주기로 발사. 빈 보유/미작성 패시브/잘못된 수치/누락 리소스는 발사하지 않음. 스킬 제거·교체 및 Player 비활성화에 실행 상태 반영. 첫 유효 표적에는 즉시 발사하며 지연 프레임의 누적 연사는 하지 않음.
- 조준: 사거리 안이면서 MainCamera 화면 안의 Enemy 태그 대상 중 가장 가까운 위치를 매 발사 시점에 선택. 일반 적과 BossMvp의 Enemy 태그 확인. 이미 발사한 탄환은 기존 Bullet.InitDirection의 직선 비행 유지. 기존 풀을 사용하며 새 매니저/로딩 체계는 추가하지 않음.
- 설정: Data.SkillData에 PassiveConfigured/PassivePrefabKey/PassiveDamage/PassiveInterval/PassiveSpeed/PassiveRange 추가. Player.Passive Projectiles 직접 바인딩 또는 기존 ResourceManager의 준비된 캐시를 사용. MainTestScene에는10001의 승인 설정과 FlameBullet 바인딩만 추가(이 작업의 씬 추가9줄). 기존 기본 공격 수치와 Player.bulletPrefab 참조는 보존하되 기본탄 중복 발사는 제거. bulletPrefab을 비워도 이제 패시브를 끄지 않음.
- 시간/입력: MP 무소모. 일시정지·방어 모션 중 남은 패시브 주기 정지. 약공격 입력 잠금 중에는 기존대로 패시브 허용. 비활성화 후 재활성화는 첫 유효 표적부터 새 주기로 시작.
- 재현/검증: 새13개 테스트가 기존 구현에서13개 실패(빈 보유 상태 기본탄 생성/패시브 정의 부재). 구현 후 관련85/85 통과. 실제 Player 바인딩/빨간 프리팹 물리 충돌 피해6/비활성화/방어와 약공격 잠금/씬 설정 테스트 추가 후 Unity6000.5.0f1 별도 검증 프로젝트에서 최종89/89 통과, 실패0·생략0. 패시브17, 기존 풀6/약공격13/MP행동19/HUD12/상태창9/팝업13. 이번 실행 결과이며 이전73개 기록 재사용 아님.
- 검증 파일: .utmp/PassiveSkill/red.xml, final.xml, final.log. 변경 코드·테스트·MainTestScene은 테스트 복사본과 해시 동일. 전체0.Scripts의 차이는 검증 복사본 PlayFromTitle 배치 guard뿐. git diff --check 통과. 기존 테스트의 CS0618 경고가 남아 있으며 이번 패시브 코드/새 테스트에는 구 FindObjectsSortMode 호출 없음. 독립 코드 리뷰에서 중요한 결함 없음, 지적된 통합 검증 공백을 위 테스트로 보완.
- 한계: 사용자 요청으로 생략한 병합 전체 선행 검증은 수행하지 않음. 실제 원본 MainTestScene 전투/보스 전체 패턴·전체 카탈로그·Android 빌드/기기 미확인. 다른 스킬의 수치·효과·획득/성장·강공격·전략·미정 그로기는 확정하지 않음.
- 직접 확인: Unity 컴파일 완료 → Tools > Play From Title 끄기 → MainTestScene Play → 버튼 없이 화면 안의 적에게 사거리6까지 접근. 빨간 원이 첫 발사 후3초마다 직진하며 MP가 줄지 않는지 확인. 적이 움직여도 기존 탄환은 꺾이지 않고 다음 탄환이 가까운 적을 다시 조준해야 함. N_Attack 약공격은 MP10/피해10·입력 잠금을 유지. 패링/가드 및 일시정지 중 주기가 멈추고 복귀 후 이어지는지 확인. PC 이동은 PlayerController2D 비활성화.
- 오류 확인: Console 첫 오류, Player.Passive Projectiles의 Key=FlameBullet/Prefab=FlameBullet, PlayerData.Loadout의10001 보유와 PassiveConfigured·수치·키 일치, Enemy 태그/활성 Collider/MainCamera 태그·화면·거리를 확인. 패시브만 잠시 끄려면 Play 중10001의 PassiveConfigured를 해제하고 임시 설정은 저장하지 않음.
- 문서: docs/AI/PlayerPassiveSkills.md 및 docs/superpowers/plans/2026-11-03-owned-passive-skills.md. 다음은 실제 플레이 피드백 반영 및 정식 스킬 테이블/리소스·획득 경로 연결, 이후 강공격 차징 구현. 해당 단계의 미작성 수치는 별도 사용자 결정이 필요함.

## 2026-11-03 보스 HP 바 좌우 입력 버그 수정

- 시작 상태: feat/PlayerSkill/ccac76f. 앞선 약공격·패시브 관련 코드/씬/리소스/테스트/문서 및 열린 테이블 잠금 파일을 보존.
- 원인: MainTestScene의 Boss_HP Slider(1137732343553467738)가 Interactable=true, Navigation=Automatic으로 설정되어 있음. 보스는 PlayerView가 아닌 공용 EnemyHealth의 healthSlider 경로를 사용하여 앞선 플레이어 HUD 표시 전용 처리가 적용되지 않았음. 실제 HP와 무관하게 선택된 Slider가 좌우/포인터 입력으로 편집됨.
- 재현: 실제 EnemyHealth+Slider+EventSystem 테스트에서 HP50 상태의 왼쪽 이동만으로 표시 비율0.5→0.4, 숨김 후 표시된 바도0.7→0.6, 포인터/드래그로0.5→1이 됨. 수정 전 새7개 중6개 실패·HP바 없는 적1개 통과.
- 수정: EnemyHealth 초기화/Start/피격/풀 재등장 시 HP 바를 표시 전용으로 설정(Interactable=false, Navigation=None, Transition=None). 비활성 색상 전환을 제거하여 색상 유지. 체력 비율은 Slider의 min/max 범위로 매핑하고 SetValueWithoutNotify로 표시하여 조작 이벤트를 발생시키지 않음. 체력/피해/보스 패턴·스폰 수치 변경 없음.
- 검증: Unity6000.5.0f1 별도 검증 프로젝트에서55/55 통과, 실패0·생략0. EnemyHealthHudTests7 + PlayerHudTests12 + PlayerPassiveSkillTests17 + PlayerBulletPoolingTests6 + PlayerLightSkillTests13. 좌우/포인터 차단, 실제 피격·사용자 지정 범위·이벤트 무발생, 풀 재등장, 숨김/표시, UI 없는 적 검증. 결과 .utmp/BossHealthHud/red.xml 및 final.xml/final.log. 새 결과이며 앞선89개 기록 재사용 아님.
- 보존 확인: MainTestScene/BossMvp.prefab/EnemyMvp.prefab 작업 전후 해시 동일. 변경 소스·테스트는 검증 복사본과 해시 동일. git diff --check 통과. 씬·프리팹·Project Settings 변경 없음. 기존 다른 테스트의 CS0618 경고는 별개로 남아 있음.
- 직접 확인: Unity 컴파일 완료 → Tools > Play From Title 끄기 → MainTestScene Play 재시작 → 보스 출현 후 Boss_HP를 클릭/드래그한 다음 왼쪽·오른쪽 이동. 공격하지 않을 때 바가 입력만으로 줄거나 늘지 않고 실제 보스 피격에만 줄어야 함. 일시정지/복귀 후에도 반복. PC 이동 확인은 PlayerController2D 비활성화.
- 오류 확인: Console 첫 컴파일 오류, 실행 중 보스 EnemyHealth.healthSlider가 Boss_HP Slider를 참조하는지, Boss_HP의 Interactable=false/Navigation=None인지 확인. 기존 Play를 종료하고 재시작하여 새 초기화 적용. 소스만으로 표시 전용을 보장하므로 씬에서 수동 참조 재연결이나 HP값 조정은 필요 없음.
- 한계/다음: 실제 원본 MainTestScene 보스 전투 화면과 Android 기기는 미확인. 위 사용자 재현 절차 확인 후 기존 패시브 피드백/정식 데이터·획득 연결 및 강공격 후속으로 복귀.## 2026-11-05 몬스터 기획서 PPT 이어쓰기

- 시작 상태: `Zomso_Monster_기획서.pptx` 8장. 1.1 기본 좀비만 수치가 있고, 1.2·1.3은 `0 -` 자리표시자, 1.4 이후와 엘리트·보스는 없었다. 4장과 8장은 내용 없는 빈 템플릿이었다.
- 완료: 총 15장. 1.2 여성 좀비·1.3 사무 좀비_남의 수치 칸을 미정으로 바꾸고 가운데 상자 제목 "몬스터 이름"을 실제 이름으로 바꿨다. 1.4 사무 좀비_여, 1.5 연구 좀비_남, 1.6 연구 좀비_여, 1.7 공사장 좀비, 1.8 간호사 좀비를 추가했다(카드 그림은 3장 목록의 그림 사용, 특징 문구는 3장 목록과 같음). 2 엘리트, 2.1 엘리트 접두사, 3 보스, 3.1 보스 등장 순서를 추가했다. 목차와 개정 표(26.11.05)를 갱신했다. 사용한 빈 템플릿 2장은 삭제했다.
- 수치 기준: 사용자 결정에 따라 기본 좀비 외 몬스터 수치는 미정으로 두었다. 슬라이드에 적은 근거는 3장 목록의 특징 문구(공사장=방어형·높은 방어/넉백 저항, 연구_남=자폭형, 연구_여=원거리, 간호사=지원형), 시스템 기획서의 엘리트·보스 규칙(1.5×1.5 타일, 3분 주기 6마리, 타이머 정지), `EnemyDatatable.xlsx`의 엘리트 접두사 2개(강인한 체력 1.1, 빠른 이동속도 1.5)다. 접두사 값은 데이터 표 기재값이며 적용 방식은 미정이라고 적었다.
- 넣지 않음: 엑셀의 여성 좀비(체력 2·공격력 8·이동속도 0.1), 사무 좀비_남 체력 8, 야수 좀비 보스(체력 300·공격력 15·방어력 10). 기획서 문구 "모든 수치가 표준치"와 충돌하거나 확정 여부가 불분명해서다. 보스 순서·이름, 히든 보스, 엘리트 주기·종류도 미정.
- 불일치(고치지 않음): 1.2 카드 부제 "기본형 / 대량 생성"은 3장 목록의 "기본형 / 군집 추적"과 다르다. 1.3 카드 부제 "기본형 / 돌진 가속"은 3장의 "균형형 / 돌발 가속"과 다르다.
- 검증: 파워포인트로 15장 모두 PNG로 내보내 눈으로 확인했다. 원본은 작업 전 `C:\tmp\mon\src.pptx`에 복사해 두었다. Unity 테스트 0개. 씬, 프리팹, 코드는 변경하지 않음.
- 다음 확인: 파워포인트에서 파일을 열어(열려 있던 창은 닫고 다시 연다) 2장 목차, 5~11장 몬스터 카드, 12~15장 엘리트·보스를 본다. 불일치 두 곳과 미정 항목의 확정이 다음 작업이다.

## 2026-11-05 적 기획서 새로 작성

- 원인: `Zomso_Enemy_기획서.docx`는 처음부터 시스템 기획서 본문을 복사한 파일이었다. 파일이 깨진 것이 아니라 내용이 잘못된 문서였다. 이전 내용은 git 이력에 남아 있다.
- 완료: 패링 기획서와 같은 양식(제목, 개정 표 Date/Author/Description, INDEX, 제목 번호, 머리글·바닥글, 표 모양)으로 `적 시스템` 문서를 새로 썼다. 개정 표에는 26.11.05 행 하나만 있다. 1 문서 개요, 2 적 분류와 공통 규칙, 3 일반 몬스터, 4 엘리트 몬스터, 5 보스 몬스터, 6 등장(스폰) 규칙, 7 데이터 표, 8 구현 현황과 기획 차이, 9 미정 사항, 10 테스트 케이스(TC-01~12). 표 16개와 QA 표. 14쪽.
- 근거: 시스템 기획서, `Zomso_Monster_기획서.pptx`, `EnemyDatatable.xlsx`, 10월 20일 팀회의록, `Assets/0.Scripts/Game/Enemy/**`, `Game/Encounter/BossEncounterController.cs`, `Game/Spawning/SceneEnemyPool.cs`. 확정된 값은 기본 좀비 HP 3·공격력 10, 크기 비율, 거리 구간, 동시 130마리 스폰 제한, 보스 3·6·9·12·15·18분과 타이머 정지, 체력바 규칙뿐이다. 나머지 몬스터 수치, 엘리트 주기·보상, 히든 보스 조건, 스테이지와 18분 타임라인의 관계, 적 종족(인간·외계인 대 좀비), 바닥 경고, 그로기는 미정으로 적었다.
- 충돌한 자료(어느 쪽도 고르지 않음): 몬스터 PPT와 엑셀의 여성 좀비(HP·공격력·속도), 사무 좀비 남 HP 8과 특징 설명, PPT 목록의 TYPE 글자 순서, 보스 엑셀의 크기 1·이동 속도 0(자리표시), 보스 공격력 15와 구현 할퀴기 피해 10.
- 코드와 기획 차이(8장 표): 보스 시작 때 일반 몬스터를 모두 거두는 구현과 보스 사망 때 정리한다는 기획, 보스 등장 기본 10초 1회와 3분 6회, 돌진 발동 거리 10(기획 뷰포트 가로 절반 8타일보다 멀다), 접촉 피해 간격 0.5초, 엘리트·그로기·붉은 안광·바닥 경고·잡기는 코드에 없음. 코드는 바꾸지 않았다.
- 검증: 저장 파일을 워드로 다시 열어 제목 번호(1~10, 하위 번호), INDEX, 표 16개, 쪽수 14를 확인했고 PDF로 내보내 페이지를 눈으로 봤다. Unity 테스트 0개. 씬, 프리팹, 코드, 엑셀은 변경하지 않음.
- 남은 일: 패링 기획서 TC-02의 `스태미나 차감` ST 구표기는 이번에 고치지 않았다. 노션은 `기획서 작성` 페이지의 개정 이력 댓글 스레드에 2026.11.05 세부 기획서 답글을 달았다. `세부 기획서 작성`·`점수 구현 기획` 페이지 본문은 고치지 않았다.
- 다음 확인: 워드에서 `Zomso_Enemy_기획서.docx`를 연다. 제목이 `적 시스템`이고, 개정 표에 26.11.05 구동현 한 줄, 2쪽 INDEX가 1~10장, 4쪽부터 본문이 보여야 한다. 시스템 기획서 내용이 보이면 예전 창이므로 닫고(저장 안 함) 파일을 다시 연다. 목차 쪽수가 어긋나 보이면 INDEX를 우클릭해 필드 업데이트 → 전체 업데이트를 한다.

## 2026-11-04 기획서 정합

- 완료: 보스전 중에는 인게임 타이머가 멈춘다는 기준으로 점수식을 적었다. 플레이 타임은 그 타이머이고, 보스전 시간은 넣지 않는다. 18분에 6번째 보스가 나오면 타이머는 1080초에서 멈추므로, 보스전을 오래 해도 클리어 보너스는 음수가 되지 않는다. 18분까지 진행한 클리어의 시간 보너스는 0이다.
- 완료: 시스템 기획서의 끊긴 문장, 오타, 거리 경계, 사망 시 부활 순서, 히든 보스·퀘스트·바닥 경고·골드·스킬 장착·몬스터 개별 수치의 미정 표기, 엘리트 보상 후보 표기. (적 기획서에 시스템 기획서 본문이 복사돼 있었다는 것은 이후 확인됐고, 2026-11-05에 새로 작성했다. 아래 새 항목 참고.) 스킬 기획서는 자동 탄환 대상, 강공격 차징 시간(표의 0.45~0.95초), 전략 쿨타임 6초, 차징 배율 미정, 동작 우선순위를 맞췄다. 빌드 기획서의 융합 예시는 태양 검 10007 = 화염 강타 10003 + 플레어 탄환 10006, 제목과 최대 MP, Uncommon. 패링 기획서는 가드 경감, 10프레임 탭, 2초 가드 해제. 스킬 개요 11장의 0.8/1/1.2 배율은 미정으로 바꿨다. 몬스터 문서에서 여성 좀비는 TYPE C, 사무 좀비 남은 TYPE E. 강공격·전략 표의 헤더 Cooldawn은 Cooldown.
- 넣지 않음: 플레이어 HP/MP, 차징 배율, 히든 보스 조건, 퀘스트 보상, 골드 가격, 20001~20005 차징 시간, 기본 좀비 외 몬스터 수치. 융합 표의 수식은 원래 위 칸을 가리키고 있어 값을 덮어쓰지 않았다.
- 검증: 저장 파일에서 해당 문장을 다시 읽었다. 2026-11-05에 시스템 기획서가 워드에서 열리지 않아, 히든 보스 문장의 닫는 태그가 하나 더 있던 것을 지우고 워드로 다시 열었다. Unity 테스트 0개. 씬, 프리팹, 코드는 변경하지 않음.
- 이력: 2026-11-05 개정 표는 목차 번호와 제목 참조로 다시 썼다. 예: 1.2.1 최종 점수 산정 방식. 제목을 누르면 해당 목차로 이동한다.
- 다음 확인: 워드에서 시스템 기획서의 개정 표 마지막 내용 행이 26.11.05인지, 점수식 다음 문장과 빌드 기획서의 태양 검 10007을 본다. 예전에 열어 둔 창이면 닫고 파일을 다시 연다.

## 2026-11-05 도트 연출 기획서 문체 정리

- 완료: 프로젝트 루트 `Zomso_도트연출_기획서.docx`의 본문을 자연스러운 서술형 문장으로 전부 다시 썼다. 변경 이력 표(작성자·설명), `가칭` 삭제, `들개` 삭제는 사용자가 직접 고친 것이라 그대로 두고, 들개를 문서 어디에도 쓰지 않도록 맞췄다. 장 제목은 2 현재 구현 기준, 3.1 지금 제작 방식의 한계, 3.2 코드로 도트를 만드는 방식, 3.3 참고한 게임 화면, 4.1 도트 제작 기준, 4.2 종류별 제작 기준, 4.3 제작 순서, 5 현재 프로젝트 적용 범위, 6 미정 사항, 7 첫 제작 대상, 8 확인 항목으로 바꿨다. 작업 확인용 문구(파일 위치, 양식 설명)는 본문에서 빼고 8장을 도트 제작 확인 항목으로 바꿨다. 내용상 결정(포켓몬 시트·코드 미사용, 3D 맵 미도입, 수치·색·프레임 수·바닥 경고·상태 표시 위치 미정)은 그대로다.
- 검증: 저장 파일의 전체 문단과 표 3개를 읽었다. 워드 쪽수 8, 바닥글 `페이지 1 / 8`, 목차는 새 제목으로 갱신. Unity 테스트 0개. 씬, 프리팹, 코드, 스프라이트, 엑셀은 변경하지 않음.
- 다음 확인: 워드에서 파일을 열어 읽히는 흐름을 본다. 이전 창이 열려 있으면 닫고 루트의 파일을 다시 연다. 상태 표시 위치, 약·강공격 이펙트를 별도 스프라이트로 둘지, 캔버스 크기는 계속 미정이다.

## 2026-11-04 스킬 개요 PPT

- 완료: `docs/스킬_개요.pptx` 12장. 레어도 테두리 7장 뒤에, 약공격 21·강공격 15는 해당 등급 테두리 안에 문양을 넣은 모습, 전략 10개는 등급이 없어 문양만 넣었다. 이 합성 그림은 PPT에만 있고 프로젝트 아이콘 파일로 추가하지 않았다.
- 검증: 슬라이드 7장과 표 헤더를 읽었다. PowerPoint에서 화면 재생은 확인하지 않음.

## 2026-11-04 스킬 아이콘

- 시작 상태: 스킬 표의 아이콘 경로는 비어 있었다. 상태창은 `SkillData.IconLabel`이 있고 미리 로드된 주소가 있을 때만 그림을 그린다.
- 완료: 스킬별 아이콘 46장을 다시 넣었다. `Assets/2.Resources/Art/SkillIcons/SkillIcon_10001.png`~`SkillIcon_30010.png`. 32×32, 투명 배경, 동그란 배지와 레어도 테두리는 그리지 않았다. 레어도 테두리는 등급당 1장, 총 7장이다. `SkillFrame_{Common,Uncommon,Rare,Epic,Legendary,Mythic,Fusion}.png`. 64×64, 속은 투명, Point 필터. `Skill_Rarity`의 Rarity_Frame_Res와 Rarity_Color_Hex에 경로와 색이 있다. 약공격·강공격·전략 표의 Skill_Icon은 `SkillIcons/SkillIcon_{아이디}`다.
- 검증: 46장 연락표를 눈으로 확인했다. 표의 아이콘 경로 46칸과 약공격 ID 수식을 다시 읽었다. Unity 임포트와 상태창 표시는 확인하지 않음. Addressables 그룹은 비어 있어 아이콘 주소를 넣지 않았다.
- 다음 확인: Unity가 임포트를 끝내면 Project의 `Art/SkillIcons`에서 `SkillIcon_10001` 불꽃 탄환과 테두리 7장이 같이 보이는지 본다. 상태창에 그림이 없고 이름만 보이면 아이콘 주소가 아직 사전 로딩되지 않은 것이다. 엑셀 Skill_Icon이 비어 있으면 파일을 다시 연다.

## 2026-11-04 Area_Typetable

- 시작 상태: `Area_Typetable` 시트만 있고 칸은 비어 있었다. `AreaType`의 관련 수치 목차는 반경, 각도, 너비, 길이 네 가지다.
- 완료: 그 네 목차와 스킬 아이디·이름·범위 모양을 넣고, 범위가 이미 있는 약공격 21개와 강공격 10개(20006~20015)를 기입했다. 모양에 쓰는 칸만 숫자를 넣었다. 투사체·직선은 길이와 너비, 원·기둥은 반경, 부채꼴·호는 반경과 각도, 고리는 반경과 너비, 십자는 길이와 너비다. 불꽃 탄환의 사거리 칸은 단어 Reach라 길이는 같은 기본 탄환과 같이 7로 두었다. 강공격 20001~20005와 전략 스킬은 범위 모양이 없어 넣지 않았다.
- 검증: 저장 파일에서 헤더와 31행의 아이디·모양·해당 수치를 다시 읽었다. Unity 테스트 0개.
- 다음 확인: 엑셀 `Area_Typetable` 1행에 반경·각도·너비·길이가 있고, 3행 불꽃 탄환부터 33행 태양 붕괴까지 이어지면 된다.

## 2026-11-04 약공격 10002 이후 수치

- 시작 상태: `main`. 열린 `SkillDatatable.xlsx`에서 10002~10007과 10018~10021은 이름·등급·속성만 있고 전투 수치가 없었다.
- 완료: 그 10행에 MP, 쿨타임 0, Instant, 사거리, 범위, 투사체, 타격, 기본 수치, ATK 계수, 설명을 채웠다. 10001 불꽃 탄환과 10008~10017, 강공격 표는 수치를 바꾸지 않았다. 아이콘·애니메이션·프리팹·사운드는 그대로 비어 있다.
- 검증: 저장 후 엑셀에서 10행의 ID·MP·기본 수치·계수를 다시 읽었다. Unity 테스트 0개. 전투 로딩은 확인하지 않음.
- 다음 확인: LightSkilltable 4~9행과 20~23행. 강공격 20001~20005는 여전히 수치가 없다.

## 2026-11-03 불속성 스킬 20개 추가

- 시작 상태: `main`, HEAD `99a471d`. `SkillDatatable.xlsx`는 엑셀에서 열려 있었고, 약공격 불꽃 탄환만 전투 수치가 있었다. 약공격 불 스킬 7개, 강공격 불 스킬 5개.
- 완료: 열린 통합 문서에 저장했다. LightSkilltable 10008~10017에 불속성 약공격 10개, HeavySkilltable 20006~20015에 불속성 강공격 10개를 이름·등급·MP·쿨타임·시전·사거리·범위·타격·기본 수치·계수·설명까지 넣었다. 약공격은 쿨타임 0, Instant. 강공격은 쿨타임 0, Charge, 차징 0.45~0.95초. 아이콘·애니메이션·프리팹·사운드는 비워 두었다. 기존 불꽃 탄환~태양 검, 화염 망치~운석검의 빈 수치는 바꾸지 않았다.
- 배치: 약공격 새 행은 태양 검 다음이다. 얼음 탄환·바람 화살·암석포·전기 화살은 21~23행이 아니라 20~23행으로 내려갔고, ID 수식이 10018~10021을 붙였다. 이름 없는 10008행의 Mystic 칸은 열풍 탄이 대신한다. FusionSkilltable은 그대로 태양 검·화염 강타·플레어 탄환을 가리킨다.
- 검증: 엑셀에서 새 20행의 ID·이름·MP·기본 수치·계수를 다시 읽었다. Unity 테스트 0개. 게임 로딩과 전투는 확인하지 않음.
- 다음 확인: 열린 엑셀의 LightSkilltable 10~19행, HeavySkilltable 8~17행을 본다. 저장 후 다시 열어도 같은 이름이면 반영된 것이다. 이 표는 아직 전투 실행에 연결되어 있지 않다.

## 2026-11-03 body-charge 주먹 이펙트

- 시작 상태: `main`, HEAD `99a471d`. `body-charge/east` 0~7.png는 사용자가 주먹의 붉은 이펙트를 지운 상태였다.
- 완료: 8방향 `Idle/animations/body-charge`에서 주먹 밖으로 뻗은 붉은 선을 제거했다. 동쪽 0~7은 사용자 수정이라 그대로 두었고, 동쪽 8~10과 나머지 7방향 0~10을 처리했다. 북서 7~10, 남동 6은 이펙트가 없어 픽셀을 바꾸지 않았다. 어깨·등·가슴·허리·무릎의 빨간 장식은 남겼다. 이펙트가 몸을 덮던 자리는 주변 피부색으로 메웠다. 캔버스 188×188, RGBA 88장 유지.
- 남서 재처리: 첫 통과는 남서의 어두운 붉은 선을 남겼다. `south-west/0.png`~`10.png` 11장을 다시 지웠다. 빈 공간으로 뻗은 붉은 픽셀은 프레임 0의 2개를 빼고 0이다. 어깨 원, 무릎, 팔·가슴의 붉은 상처 선은 남겼다.
- 검증: 남서 0·4·8 프레임을 확대해 주먹 바깥 별 모양 선이 줄었는지 확인. Unity 테스트 0개. 에디터 재생은 확인하지 않음.
- 다음 확인: Unity에서 `body-charge/south-west`를 우클릭해 Reimport한 뒤 0~10 프레임을 본다. 주먹 밖으로 뻗은 붉은 선이 없고 어깨·무릎 장식은 남아 있으면 정상이다. 예전 그림이면 Console 임포트 오류를 확인한다.
- 씬, 프리팹, 코드, 메타는 변경하지 않음.

## 2026-11-03 Aseprite 소스 빌드

- 게임 저장소 파일은 이 작업으로 바꾸지 않음. 실행 파일은 `C:\Users\User\source\aseprite\build\bin\aseprite.exe`.
- 완료: 태그 `v1.3.18.6`, Skia `m124-08a5439a6b` Windows Release x64, Visual Studio 2022의 `vcvars64`와 Ninja로 1562개 타깃 링크. `aseprite -b --version` 결과는 `Aseprite 1.3.18.6-dev`.
- 다음 확인: 그 exe를 더블클릭해 창이 열리는지 본다. PATH에는 넣지 않음.

## 2026-11-03 좀비 하운드 견종 교체

- 시작 상태: `main`, HEAD `b71b791`. 작업 트리의 `Zombie_Hound` 그림은 빠져 있었고, 프리팹 `ZombieHound`만 수정돼 있었다. 직전 들개는 짧은 주둥이의 소형 개였고, 몬스터 기획서의 들개·박쥐 수치는 여전히 없다.
- 완료: 잉글리시 폭스하운드 골격의 좀비 들개로 교체했다. PixelLab 캐릭터 `30e3b20a-c9f2-4ed9-aca7-cda990aae8c0`. 늘어진 귀, 긴 주둥이, 구분되는 앞·뒷다리. 8방향 회전, idle 8프레임, run 8프레임. 캔버스 96×96. 클립·컨트롤러·스프라이트 GUID는 유지하고 PNG와 스프라이트 rect, 프리팹 `oldSize`만 바꿨다. 체력 3, 접촉 피해 10, 이동 속도 3. 박스 콜라이더 0.32, 원 반지름 0.16은 그대로다. 스폰 타임라인에는 넣지 않음.
- 검증: 남·동·북·서·남동 회전과 남·북 idle, 남·동·서·북동 run을 눈으로 확인. PNG 136장 모두 96×96, rect 96인 메타 136개, Point 필터와 GUID 유지. Unity 테스트 0개. Unity가 이미 열려 있어 에디터 재생은 확인하지 않음.
- 다음 확인: 열린 Unity의 임포트가 끝나면 `ZombieHound` 프리팹을 테스트 씬에 두거나 `Enemy Spawn System`의 Enemy Prefabs에 넣는다. 플레이어를 따라 달리면 run, 붙으면 idle. 그림이 비거나 예전 소형 개면 `Zombie_Hound` Reimport. 몸이 접촉 판정보다 커 보이면 콜라이더를 맞춘다.
- 속도·무리 스폰 수치는 기획 미정이라 확정하지 않음.

## 2026-11-03 좀비 들개

- 시작 상태: `main`, HEAD `13e4abe`. 몬스터 기획서에는 기본 좀비 HP 3·공격력 10만 확정돼 있고, 들개·박쥐 대응 수치는 없다. `EnemyDatatable.xlsx`는 엑셀이 열어 둔 상태라 읽거나 수정하지 않음.
- 완료: 8방향 좀비 들개. 앞다리와 뒷다리가 겹쳐 한 쌍처럼 보이던 64px 그림을 버리고, 개 골격으로 앞·뒷다리가 구분되는 92px 그림으로 교체했다. 회전, idle, run 각 8프레임. 클립·컨트롤러·프리팹 GUID는 유지하고 PNG와 스프라이트 rect만 바꿨다. 체력 3, 접촉 피해 10, 이동 속도 3. 스폰 타임라인에는 넣지 않음.
- 검증: 남·동 회전과 남쪽 idle, 남·동 run에서 앞다리와 뒷다리가 따로 보이는지 확인. PNG 136장 모두 92×92, rect 92인 메타 136개. Unity 테스트 0개. 에디터 재생은 확인하지 않음.
- 다음 확인: Unity 임포트 후 `ZombieHound` 프리팹을 테스트 씬에 두거나 `Enemy Spawn System`의 Enemy Prefabs에 넣는다. 플레이어를 따라 달리면 run, 붙으면 idle. 그림이 비면 `Zombie_Hound` Reimport.
- 속도·무리 스폰 수치는 기획 미정이라 확정하지 않음.

## 2026-11-03 Beast Zombie Boss idle 모션

- 시작 상태: `main`, HEAD `7cd15af`. Beast Zombie Boss의 8방향 `BossIdle_*` 클립은 회전 스프라이트 1장을 두 번 붙잡은 정지 클립이었다.
- 완료: PixelLab 캐릭터 `45afeb63-7461-402b-9282-8d0bbcc7bdfe`에 `breathing-idle` 템플릿으로 8방향 4프레임을 생성했다. 그룹 `c0647bc4-943d-4c82-890a-78321270802c`. 프레임은 `Idle/animations/idle/{방향}/0.png`~`3.png` 32장. 기존 `BossIdle_South`부터 `BossIdle_NorthWest`까지 8개 클립이 이 프레임을 12샘플, 1초 루프로 재생한다. 컨트롤러 `Boss Idle Directions`와 클립 GUID는 유지했다.
- 검증: 남·북·동 프레임을 눈으로 확인했고, 같은 보스 자세에서 가슴이 늘었다 줄었다. PNG 32장 모두 96×96, Point 필터 메타 32개, 방향 클립 8개가 각 4프레임을 가리킨다. Unity 테스트 0개. Unity가 꺼져 있어 에디터 재생, 화면, 빌드, 기기는 확인하지 않음.
- 다음 확인: Unity로 프로젝트를 연 뒤 임포트가 끝나면 `Boss.controller`의 BossIdle 상태를 재생하거나, 보스전이 있는 씬에서 보스가 멈춘 채 숨 쉬는 모션이 도는지 본다. 스프라이트가 비면 `Idle/animations/idle` 우클릭 → Reimport 후 Console을 확인한다.
- 씬, 프리팹, 코드, Project Settings, 패키지는 변경하지 않음.

## 2026-11-03 스프라이트 필터 설정

- 시작 상태: `main`, HEAD `b3b0de6`, 작업 트리 깨끗함. 기존 Handoff.md는 없었음.
- 완료: `Assets/2.Resources` 하위 TextureImporter의 Sprite(`textureType: 8`) 1,471개를 확인하고 Bilinear였던 908개의 `.meta`를 Point(`filterMode: 0`)로 변경. 기존 Point 563개 유지.
- 검증: 전체 Sprite 1,471개에서 Point가 아닌 항목 0개. Git diff에서 필터 값 외 변경 0줄. `git diff --check` 통과. GUID와 다른 임포트 설정은 유지.
- 실행한 Unity 테스트: 0개. 설정 파일 검사만 수행했으며 Unity 재임포트, 화면 표시, 빌드와 기기는 검증하지 않음.
- 다음 확인: Unity로 돌아가 자동 임포트 완료 후 Project → Assets/2.Resources에서 스프라이트 선택 → Inspector → Filter Mode가 `Point (no filter)`인지 확인. 이전 값이 보이면 해당 에셋 우클릭 → Reimport 후 Console의 임포트 오류 확인.
- 이번 작업에서 씬, 프리팹, 코드, Project Settings, 패키지는 변경하지 않음.
