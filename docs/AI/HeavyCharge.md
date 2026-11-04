# 강공격 차징 기반

`Player`가 입력·행동 연결을, `PlayerHeavyCharge`가 MP 비용·단계·실행 스냅샷을, `PlayerView`가 임시 조준 표시를 담당한다. 정식 스킬 테이블·획득/장착·강공격 피해·공격 애니메이션·그로기는 후속 작업이다.

## 설정과 API

- MainTestScene의 Player > Heavy Attack Button은 기존 C_Attack이다. Heavy Charge Settings의 Configured가 켜진 씬에서만 임시 PC Q 입력을 활성화한다. 기존 Player/HeavyAttack 액션이 있으면 그 복제를 사용한다.
- 승인된 테스트 설정: MPCost=10, StageTimes=[0, .4, .8], StageBonuses=[.1, .1, .5], SuperArmor=false. 배율은 1에 증분을 누적하므로 1.1/1.2/1.7이다. 모두 Inspector에서 수정 가능하며 최종 밸런스가 아니다.
- 세 단계의 시점은 첫 값 0, 이후 엄격한 오름차순이어야 한다. 배열 길이는 각각 3, 비용과 증분은 유한한 음이 아닌 값이어야 한다. 미설정/잘못된 설정은 MP 소모 없이 거절한다.
- `TryBeginHeavyCharge()`가 성공하면 MP를 한 번 차감한다. `HeavyChargeId`를 보관해 `TryReleaseHeavyCharge(id)` 또는 `CancelHeavyCharge(id)`에 전달한다. 이전 실행의 해제는 무시한다.
- 해제 성공 시 `HeavyChargeReleased` 이벤트가 `ChargeId`, `Stage`, `Multiplier`, `Direction`, `Elapsed` 스냅샷을 한 번 전달하고 `LastHeavyCharge`에 저장한다. 실제 피해 실행부는 아직 연결하지 않았다.
- 청록/노랑/주황 화살표는 각각 1/2/3단계의 임시 조준 피드백이다. 길이는 진행도를 보여주며 공격 사거리나 피해 범위가 아니다. 최대 단계에서 놓을 때까지 유지한다.

## 행동과 취소

차징 중 이동·약공격·자동 패시브의 다음 발사를 막고 이동 키/조이스틱으로 조준만 변경한다. 이미 발사한 탄환은 유지한다. 방어·회피가 실제 시작하거나 SuperArmor=false에서 실제 HP가 감소하면 취소하며 MP는 반환하지 않는다. 회피 실패는 차징을 유지한다. SuperArmor=true는 피해를 막지 않으며 사망·비활성화는 항상 정리한다.

팝업 일시정지 중 진행도와 표시를 유지한다. 정지 중 버튼/키 해제, 포커스 상실, 앱 일시중단은 취소하여 복귀 후 공격을 예약하지 않는다. 차징을 실제 시작하지 못한 동기 MP 알림 중 비활성화/사망/정지에만 시작 비용을 복구한다.

## 직접 확인

1. Unity 컴파일 완료 후 Console을 비우고 `Tools > Play From Title`을 끈다. `Assets/9.Scenes/MainTestScene.unity`를 열고 Play한다.
2. C_Attack을 누른 채 유지한다. MP가 10 한 번 줄고 화살표가 청록 → 약 .4초 노랑 → 약 .8초 주황으로 바뀌어야 한다. 기존 MP 회복은 유지되므로 이후 서서히 회복한다.
3. 조이스틱을 움직인다. 위치는 고정되고 화살표 방향만 바뀌어야 한다. 조이스틱을 중립으로 놓아도 마지막 방향을 유지한다. C_Attack 해제 시 화살표가 사라지고 이동·자동 패시브가 재개된다. 강공격 피해가 없는 것은 현재 범위다.
4. PC 검증은 Player의 PlayerController2D 컴포넌트를 Play 중 끈 뒤 Q 홀드와 이동 키로 확인한다. 방어·회피 성공은 즉시 취소, 실패한 회피는 유지되는지 확인한다. 취소된 차징 비용은 반환되지 않는다.
5. 차징 중 일시정지 팝업을 열어 색/길이/방향이 멈추는지 확인한다. 계속 홀드한 채 닫으면 이어지고, 정지 중 해제하거나 앱 포커스를 잃으면 취소되어야 한다. 피격/사망 취소도 확인한다. Play 중 임시 변경은 씬에 저장하지 않는다.

오류가 있으면 Console의 첫 새 오류, Player의 Heavy Attack Button=C_Attack, Configured와 두 배열, MP 잔량, 입력 소스와 포커스를 확인한다. 약공격/방어/회피 중 시작 거절은 정상이다. `SuperArmor`를 시험할 때도 HP 피해는 적용되며 사망하면 차징이 끝나야 한다.
