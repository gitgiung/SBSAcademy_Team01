# 보스 등장·전투 연출 구조 분석 (2026-11-10)

> 추가 변경: 원형 예고에 확장 원을 추가했다. Phase Two Area Delay 동안 0→최대 반경으로 커지며 완충 시 모션 시작과 피해를 동시에 적용한다. 기존 normalizedTime0.5 타격은 제거했고 늦은 AnimationEvent 중복은 가드한다. 등장 공개/큰 확산/HP바도 이 완충 시점에 시작한다. 아래 분석의 중간 타격 시점과 예상시간은 변경 전 기록이다.

> 후속 승인 변경: 아래 본문은 분석 당시 동작이다. 이후 사용자 지정으로 등장 예고·피해 반경을 별도 `Entrance Area Radius` 기본3.5로 분리하고, 기존 공개 타격 시 보스 Damage(현재30)로 피해를 주도록 변경했다. 전투 원형 반경14와 큰 빨간 확산은 유지했다. 최신 검증·확인 절차는 Handoff의 '등장 예고·피해 반경 3.5 연결'을 따른다.

## 범위와 기준

- `Effect,HP,MP,EXP` / `34eacf7` 위의 현재 작업 트리를 읽었다. 분석 대상은 MainScene의 BeastZombie 및 BossController 경로다. HordeLordBoss·ZombieSlimeBoss는 별도 구현이므로 같은 등장 연출로 일반화하지 않는다.
- 이번에는 게임 코드·씬·프리팹을 수정하지 않았다. 시작 당시 PlayerView 숫자 표시·보스 코드/프리팹·셰이더 변경을 모두 보존했다. 실제 Play 정지의 원인을 확정한 보고가 아니다.
- 현재 보스 diff: BossController의 Header 문구·damage 필드 기본값(10→30), BossAttackHitVfx의 Header 문구 변경. 프리팹에는 크기6·원형 선택거리(6,12)·돌진시간1.5·추격돌진대기4 등의 미커밋 조정이 있다. 파일 기본값보다 실제 씬/프리팹 값을 우선해 읽는다.

## 책임과 연결

| 담당 | 소유하는 것 | 다른 코드에 의존하는 지점 |
|---|---|---|
| BossEncounterController | 보스 등장 시각, 일반 스폰·진행 시계 정지, 일반 적 정리, 위치 배치, HP바 페이드, 사망 후 진행 재개 | 활성화 전에 QueueEntrance 호출, IsEntering이 false일 때 HP바 페이드 시작 |
| BossController | 등장 예약/실행, 보스 몸체 숨김·공개, 패턴 루프, 추격 중지·재개, 애니메이션 실행과 타격 판정 | Animator 상태와 진행 시간, Target·EnemyHealth·EnemyFollow, BossAttackHitVfx |
| BossAttackHitVfx | 예고 원, 할퀴기 범위, 돌진 타격, 빨간 확산의 렌더링·수명 | 호출을 받아 표시하며, AI나 HP바를 진행시키지 않음 |
| Animator/BossSwipeBoth | 8방향 양손 공격 그림 재생, 타격·종료 AnimationEvent | BossController가 직접 상태를 시작하고 진행 상태도 검사 |
| EnemyFollow | Rigidbody2D 기반 추격과 충돌 이동 | BossController가 enabled와 타깃을 제어 |
| EnemyHealth | HP·사망·몸체 원본 상태 캐시·피격 색 | 등장 전에 EnsureInitialized, 사망 신호 구독 |
| BossPatternRangeView | 선택된 보스의 Scene 뷰 편집용 범위 | 인게임 예고·폭발을 그리지 않음. BossController 자체 Gizmos도 별도로 존재 |

## 실제 등장 순서

1. Encounter.Awake에서 보스·HP바를 비활성화하고 플레이어 기준 초기 오프셋을 저장한다.
2. MainScene의 진행 시간이 180초에 도달하면 BeginBossFight가 실행된다. SpawnDirector만 정지하고 일반 적을 정리한다. Time.timeScale=0을 설정하지 않으므로 플레이어 전투는 계속된다.
3. 저장한 오프셋으로 보스 위치를 옮기고 QueueEntrance 후 보스 GameObject를 켠다. MainScene은 BossController.Target을 Player Transform으로 오버라이드하고 있다.
4. BossController.OnEnable이 예약을 소비해 EntranceThenPatternLoop를 시작한다. EnemyFollow를 끄고 보스 그림(forceRenderingOff)·콜라이더(enabled)의 원래 상태를 보관해 숨긴다. HitFlashExcluded가 붙은 VFX는 숨김에서 제외한다.
5. 한 프레임 대기 후 ActivePattern=PhaseTwoArea로 설정하고 기존 PerformPhaseTwoAreaAttack을 실행한다. **등장 전용 공격 시퀀스가 아니라 2페이즈 원형 공격 재사용이다.**
6. ShowArea로 작은 예고 원을 표시한다. 현재 프리팹 phaseTwoAreaRadius=14, phaseTwoAreaDelay=1.5초다. 예고 뒤 PlaySlashAnimation(useArea=true)이 Base Layer.BossSwipeBoth를 재생한다.
7. AnimationEvent_ApplyAreaDamage 또는 애니메이션 진행률 0.5 검사에서 타격 처리를 한 번 실행한다. 등장 여부를 먼저 저장하고 RevealEntrance로 보스 그림·콜라이더를 원래대로 돌린다. 등장이라면 ShowEntranceBurst를 호출하고 피해는 주지 않는다. IsEntering이 false가 되어 HP바 페이드도 시작한다.
8. 종료 이벤트, 현재 상태 이탈, 다른 상태로의 전환 또는 normalizedTime>=1 중 하나로 공격 대기가 끝난다. finally로 예고 원을 숨기고, 바깥 등장 코루틴이 EnemyFollow를 켜며 PatternLoop를 시작한다.
9. 빨간 확산은 VFX.LateUpdate에서 독립적으로 커지고 사라진다. **등장 폭발 4초 완료를 AI가 기다리지 않는다.** 폭발이 남은 상태로 보스가 추격·공격하는 것이 현재 코드의 정상 흐름이다.

## 서로 다른 원형 표시

| 표시 | 실행 메서드 | 크기 | 시간·피해 |
|---|---|---|---|
| 등장/전투 공통 예고 원 | ShowArea | phaseTwoAreaRadius=14 | 준비와 모션 동안 표시. 표시 자체에는 피해 없음 |
| 등장 빨간 확산 | ShowEntranceBurst | max(공격 반경, Entrance Burst Minimum Size=200, 보스→카메라 모서리 거리×4) | Entrance Area Burst Duration=4초, 등장 피해 없음 |
| 전투 원형 타격 확산 | ShowAreaBurst | 저장한 공격 반경=14 | Area Burst Duration=0.8초, 별도 원형 피해 적용 |
| 편집용 거리 원 | Gizmos/Handles | 근거리·돌진·원형 선택거리 등 | Scene 확인용. 인게임 VFX 변경으로 제어하면 안 됨 |

예고 원은 rangeColor(현재 주황 계열·알파0.4)와 rangeMaterial을 사용한다. 빨간 확산은 areaBurstSprite와 areaBurstColor(빨강)를 사용한다. 두 그림은 별도 SpriteRenderer이며 색·크기 설정도 다르다. areaBurstSprite는 현재 Boss/Test/ParryFlash_03_Red.png다.

Entrance Burst Minimum Size는 이름에 Size라고 되어 있지만 계산상 **반경의 하한**이다. 200이면 목표 지름이 최소 400이다. 실제 시작은 목표 지름의 10%이며 cubic ease-out으로 커지고 알파는 선형으로 줄어든다. 카메라 보정이 있으므로 하한만 줄여도 최종 크기가 줄지 않을 수 있다. 부모 배율은 LocalScale 계산에서 보정하므로 보스 그림 크기 4→6 변경이 효과 반경을 그대로 1.5배 만들지는 않는다.

## 저장된 설정과 시간 해석

- MainScene: Boss Appear Time=180초, Boss Health Fade Duration=0.5초, Spawn Anchor=Player.
- BeastZombie 프리팹: Phase Two Area Delay=1.5초, Radius=14, Entrance Burst Minimum Size=200, Entrance Area Burst Duration=4초, 일반 Area Burst Duration=0.8초.
- BossSwipeBoth 8개 클립을 정적으로 확인했다. 모두 0.6666667초·비반복, 타격 이벤트 1개(0.5833333초)·종료 이벤트 1개(0.6666667초)다.
- 코드에는 normalizedTime>=0.5에서 타격 메서드를 호출하는 보완 경로가 있다. 따라서 기본 속도1에서는 약 0.333초에 공개되며, 뒤의 0.583초 이벤트는 attackHitApplied가 막는다. 이름이 AnimationEvent라고 해서 이벤트에서만 실행되는 것이 아니다.
- 프레임 지연·정지를 제외한 예상: 예고 시작 후 약 1.83초에 공개/폭발/HP바 페이드 시작, 약 2.17초에 등장 모션 종료 후 패턴 루프, 폭발은 공개 뒤 4초까지 남을 수 있다. 이는 코드 계산이며 실제 프레임 계측 결과가 아니다.
- 준비·애니메이션·확산·HP바는 전투 시간의 영향을 받는다. 팝업 정지/패링 히트스톱 등으로 Time.timeScale이 멈추면 지연된다.

## 설계상 결합과 변경 경계

- 장점: 원형 준비/양손 모션/표시를 재사용하며, 중복 타격 방지와 finally/OnDisable 복원 경로가 있다. 그림과 피해 반경 계산도 분리되어 있다.
- 결합: IsEntering이 그림·콜라이더 공개, 등장/실전 피해 분기, 큰/작은 폭발 분기, HP바 시작을 함께 좌우한다. 단순히 '등장 원형 없애기'라고 해도 예고만 제거할지, 확산만 제거할지, 재사용 모션까지 없앨지 먼저 구분해야 한다.
- 작은 시각 조정은 BossAttackHitVfx의 표시 경로에서 제한하는 것이 적절하다. QueueEntrance, RevealEntrance, 공격 종료, 추격 재개를 함께 건드릴 이유는 없다.
- 등장 동작 전체를 바꾸려면 기존 BossController 안에서 등장 시작/공개/종료 책임을 분명히 하고 시각 호출을 구분하는 방안을 검토할 수 있다. 이번에 리팩터링하거나 새 매니저를 추가하지 않았다.
- HP바 시작은 '전체 연출 종료'가 아니라 '모습 공개'에 맞춰져 있다. 의도와 다른 경우 별도 기획 판단이 필요하다.

## 멈춤을 조사할 때 구분할 것

1. 보스도 안 보이고 예고만 계속 남음: 등장 준비 대기 또는 애니메이션 진행 대기 확인. Animator.speed=0/진행률 정지이면 종료 이벤트·normalizedTime 보완이 모두 진행되지 않을 수 있다. 이 대기에는 독립적인 최대 제한 시간이 없다.
2. 보스는 보이는데 추격하지 않음: RevealEntrance는 실행됐어도 PlaySlashAnimation이 아직 끝나지 않을 수 있다. 실제 ActivePattern·attackAnimationRunning·Animator normalizedTime·EnemyFollow.enabled를 함께 봐야 한다.
3. 등장 원/폭발 모두 생략되고 보스만 정지: 비어 있거나 비활성인 Target은 CanAttackTarget에서 조용히 실패하고 PatternLoop에서도 조용히 대기할 수 있다. 저장된 MainScene 연결이 정상이어도 실행 중 인스턴스가 같은지 확인해야 한다.
4. 폭발만 안 보임: VFX 비활성·Sprite/Material 누락이면 BeginBurst가 조용히 반환한다. 이 반환 자체가 패턴 루프를 기다리게 하지는 않는다.
5. Animator/상태 누락: 현재 코드가 오류를 출력하고 해당 공격을 종료한다. 종료 이벤트 하나만 누락돼도 무조건 영구 정지하는 구조는 아니다(상태 진행률/이탈 보완 존재).
6. HP바만 안 보임: IsEntering 대기 또는 CanvasGroup/대상 오브젝트 활성·참조 확인. HP바 코루틴이 보스 AI를 정지시키지는 않는다.
7. 비활성/재활성·페이즈 전환은 코루틴 중단 및 추격 상태 정리를 동반한다. 발생 시점을 따로 계측해야 하며 과거 정지의 원인으로 확정하지 않았다.

## 검증 범위와 직접 확인

- 이번 실행은 소스·씬·프리팹·8개 클립 이벤트의 정적 분석이다. 컴파일/자동 테스트/Unity Play 실행 0회. 과거 사용자의 '재시작해도 보스만 정지, 빨간 오류 없음' 보고를 해결됐다고 판단하지 않는다.
- Play 종료 → 임포트 완료 → Tools > Play From Title 해제 → MainScene Play → 저장 설정 기준 180초까지 진행한다.
- 정상 순서: 진행 시계 정지/일반 적 정리 → 보스 숨김과 예고 → 모션 중 공개/큰 빨간 확산/HP바 페이드 → 추격·공격. 확산이 남아 있어도 AI는 진행한다.
- 오류 시 실제 BeastZombie 인스턴스의 Target·Animator·EnemyFollow 활성, BossController 상태, Animator 상태 진행률을 확인한다. Console 첫 오류와 VFX Sprite/Material 참조도 함께 확인한다. Edit 모드 프리팹 설정만으로 실행 상태를 추정하지 않는다.

## 주요 소스 위치

- Assets/0.Scripts/Game/Encounter/BossEncounterController.cs:116 (진입), :195 (HP바 대기)
- Assets/0.Scripts/Game/Enemy/Boss/BossController.cs:152 (등장), :357 (원형), :384 (모션 대기), :458 (공개·피해 분기)
- Assets/0.Scripts/Game/Enemy/Boss/BossAttackHitVfx.cs:86 (예고), :114 (등장 크기), :133 (확산)
- Assets/0.Scripts/Game/Enemy/Boss/BossAttackHitVfxShape.cs:8 (카메라 거리 보정)
