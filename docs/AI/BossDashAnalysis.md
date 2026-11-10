# BeastZombie 돌진 패턴 분석 — 2026-11-10

> 후속 변경: 2페이즈 돌진은 BossRun을 사용하고, 보간 기준 시간은 Dash Duration×0.5다(현재1.5→0.75초). 1페이즈는 기존 body-charge와 원래 시간이다. Inspector 원본값은 유지한다. 아래 공통 body-charge/시간 설명은 변경 전 기준이다.

> 후속 변경: 사용자 요청으로 돌진 도착 판정은 `Close Range`에서 분리한 `Dash Arrival Distance`(기본4)를 사용한다. 이동 루프와 최종 도착 검사 모두 적용한다. 아래 본문의 '도착 Close Range4'는 분리 전 설명이며 현재는 독립 설정이다. 돌진 선택 조건·일반 추격 정지·근접 공격은 여전히 Close Range를 사용한다.

## 범위

현재 작업 트리의 BossController·EnemyFollow·CollisionMovement2D·BossAttackHitVfx·Boss.controller·BeastZombie 프리팹을 읽었다. 코드·씬·프리팹 수정 없이 분석했다. 기존 사용자 변경과 직전 등장 반경/피해 및 HUD 작업은 보존했다. 컴파일·자동 테스트·Unity Play 실행은 이번 0회다.

## 선택 조건

- Pattern3가 돌진이다. 근접 공격·원형 공격 실행 도중에는 PatternLoop가 해당 코루틴을 기다리므로 원거리로 이동했다는 이유만으로 즉시 중단하고 돌진하지 않는다.
- 다음 패턴을 고를 수 있을 때 거리>=specialPatternDistance이면 돌진한다. 현재 프리팹은14.01이다. 단, 최초2페이즈 진입 예약 원형 패턴이 이 거리 검사보다 우선한다.
- 별도 경로: 선택 결과가 None이고 closeRange<거리<specialPatternDistance이면 chaseOnlyTime이 chaseDashDelay 이상 누적될 때 돌진한다. 현재 closeRange4, delay4초다. 근접 안에서는 누적을0으로 만들고 어떤 패턴이든 선택하면 초기화한다.
- chaseOnlyTime은 실제 이동 거리가 아니라 '공격을 선택하지 않은 시간'이다. 벽에 막혀 있어도 거리가 근접 밖이면 쌓인다. 패턴 쿨다운 중에도 None 경로로 누적되고 조건 충족 시 돌진할 수 있다. 타깃 없음·전투 시간 정지 중에는 누적하지 않는다.
- 원형 뒤 일반1·2가 대기 중이어도 원거리 돌진은 허용한다. 돌진은 대기 중 일반 패턴 순서와 areaFollowupPending을 지우지 않는다.

## 실행 순서

1. ActivePattern=Pattern3, `돌진 시작!` 로그.
2. 일반 추격 EnemyFollow.enabled=false. Animator의 Base Layer.body-charge를 직접 재생한다. 별도 코드상 준비 대기는 없으므로 이동은 바로 시작된다.
3. 시작 위치를 한 번 저장한다. 매 FixedUpdate마다 현재 플레이어 위치를 다시 읽고, `Lerp(시작 위치, 현재 플레이어 위치, Clamp01(경과시간 / dashDuration))`로 목적 위치를 계산한다.
4. EnemyFollow.MoveTo를 직접 호출한다. 이 메서드는 추격 컴포넌트가 꺼져 있어도 호출 가능하며 CollisionMovement2D가 고체 충돌을 검사·이동·벽면 미끄러짐을 처리한다.
5. 중심 거리가 closeRange 이내면 루프를 끝낸다. 실제 이동량 제곱<1e-8(약0.0001 미만 이동)이면 벽 등에 막힌 것으로 보고 종료한다. 타깃 무효·보스 사망도 종료 조건이다.
6. 추가 FixedUpdate 한 번 대기 후 현재 거리<=closeRange인지 다시 판정한다. 이 순간 조건을 만족할 때만 종료 피해와 표시를 낸다.
7. StopDashAnimation으로 BossIdle 복귀, 추격 타깃·정지거리 복원, EnemyFollow 활성. PatternLoop로 돌아가 nextPatternTime=현재시각으로 설정하므로 돌진 전용 후딜·쿨타임 없이 다음 공격을 선택한다.

## 이동의 의미

- 목표 방향 고정 직진이 아니라 계속 꺾이는 추적형이다. FacingX/Y도 플레이어 방향을 따라 갱신된다.
- Dash Duration 현재1.5초는 최대 지속시간이 아니다. 근접에 못 들어가면1.5초 뒤에도 계속 시도한다.
- progress=1 이후 목적 위치는 매번 플레이어 현재 위치다. 고정 최대속도 제한이 별도로 없어 열린 공간에서는 한 물리 프레임에 큰 거리를 좁힐 수 있다. 충돌 검사는 그대로 수행하므로 무조건 관통하는 텔레포트라고 해석하면 안 된다.
- 플레이어가 정지·장애물 없음이면 대략 시작거리/dashDuration의 이동속도가 되며, closeRange에 닿는 즉시 끝나므로 항상1.5초 내내 재생하지 않는다. 플레이어가 움직이면 이 속도 해석도 달라진다.
- EnemyFollow.moveSpeed는 일반 추격에 사용하며 돌진 보간에는 사용하지 않는다.2페이즈의 moveSpeed×1.5만으로 돌진 보간 시간이 짧아지지는 않는다.

## 피해와 시각 효과

- 돌진 종료 판정은 '보스 중심과 플레이어 중심이4 이내'다. 최종 실제 피해 원은 **그 순간 플레이어 위치 중심·반경3**이다. 보스 중심이나 돌진 시작 때의 목표 위치가 아니다.
- `ApplyCircleDamage`는 IContactDamageReceiver를 가진 Collider를 OverlapCircleAll로 모으고 같은 수신자는1회만 처리한다. Collider 가장자리의 겹침도 검사 대상이다.
- 피해는 기존 Damage30을 사용한다. 현재2페이즈 진입 시 Damage×1.5가 적용되므로 통상45다. 최종 플레이어 HP 감소는 가드·패링·회피·보호막 등 기존 방어 처리를 따른다.
- BossController의 이 돌진 함수에는 이동 도중 연속 피해가 없다. 종료 피해는 AnimationEvent가 아닌 이동 코루틴 종료에서 실행한다. 현재 BeastZombie 프리팹에는 EnemyContactDamage 컴포넌트도 없다. 다른 씬에서 별도 접촉 피해를 붙이면 그 경로는 별도로 확인해야 한다.
- 종료 원은 ShowDashImpact로 피해와 같은 중심·반경에 표시하고 현재 Dash Display Duration0.2초 동안 페이드한다. 등장/2페이즈의 빨간 확산과 다른 dashRenderer다. 돌진 경로를 미리 표시하는 코드는 없다.
- Animator는 body-charge 시작→body-charge-loop 반복으로 구성되며 코드가 돌진 종료 때 Idle로 복귀한다. 시작 모션이 끝나기 전에 근접에 도착하면 코드가 모션을 중단할 수 있다. Animator 누락·상태 누락은 이동을 막는 조건이 아니므로 모션 없이 이동할 수도 있다.

## 설계 판단과 확인할 경계

- '플레이어 중심 도착 피해'와 '벽 차단 후 재돌진 가능성'은 과거 인수인계에 각각 유지/후속 보류로 기록되어 있다. 이번 분석에서 새 버그로 단정하거나 수정하지 않았다.
- 벽에 막히면0이동으로 종료한다. 이후 거리가 여전히14.01 이상이면 다음 루프에서 바로 재선택될 수 있다. 돌진 전용 실패 대기시간이 없기 때문이다.
- 이동은 벽을 따라 약간이라도 진행하면 계속되고 독립적인 최대 제한 시간이 없어, 좁은 지형에서는 지속시간을 실제 계측해야 한다.
- 이동 종료 후 추가 물리 프레임 사이에 플레이어가 근접 밖으로 나가면 도착 피해를 취소한다. 반대로 피해 원의 중심이 현재 플레이어이므로 발동이 확정된 순간의 대상은 원 중심에 있다.
- 일반 종료에는 추격 복원 코드가 있지만 이동 함수 자체는 try/finally로 감싸지 않았다. 사망·비활성화·2페이즈 전환에는 BossController의 별도 정리 경로가 있다. 그 밖의 예외/외부 중단은 별도 검증 대상이다.

## 직접 확인

1. Play 종료→컴파일 완료→Tools > Play From Title 해제→MainScene Play→보스 등장 완료.
2. 실제 BeastZombie Inspector에서 Close Range4 / Special Pattern Distance14.01 / Chase Dash Delay4 / Dash Duration1.5 / Dash End Damage Radius3 확인(미저장 Inspector 수정·씬 오버라이드는 별도).
3. 중심 거리14.01 이상으로 이동: 공격 선택 가능 시 돌진. 중간 거리에서 다른 패턴 없이4초 추격: 지연 돌진. 플레이어를 옆으로 움직이면 경로가 따라 바뀌어야 한다.
4. 도착 시 플레이어 자리의 짧은 원과 피해·패링, 직후 추격/다음 공격을 확인한다. 벽이 있는 경우 차단·종료·재시도도 별도로 관찰한다.
5. 이상하면 Console 첫 오류와 `돌진 시작!` 반복 여부, Target·EnemyFollow·Rigidbody2D.simulated·충돌 레이어, Animator body-charge 상태를 확인한다. Animator가 멀쩡해도 이동이 정상이라는 증거는 아니며 그 반대도 같다.

주요 위치: BossController.cs의 PatternLoop/SelectAvailablePattern/DashToTrackedTarget/ApplyDashEndDamage, EnemyFollow.cs의 MoveTo, CollisionMovement2D.cs의 Move, BossAttackHitVfx.cs의 ShowDashImpact.
