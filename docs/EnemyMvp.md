# 적 생성·추적·피격·사망 MVP

## 실행과 조작

1. Assets/9.Scenes/EnemyMvpTestScene.unity를 열고 Play를 누른다.
2. Game 뷰에 포커스를 두고 WASD로 하늘색 플레이어를 이동한다. 대각선도 같은 속도다.
3. 마우스로 방향을 가리키고 좌클릭하면 해당 방향으로 근접 공격한다. 누르고 있는 동안 자동 연타하지 않는다.
4. 빨간 적은 좌우에서 1초 간격으로 총 5마리 생성되고 플레이어를 추적한다.
5. 적을 맞히면 하얗게 깜박인다. 기본 체력 3, 피해 1이므로 세 번 맞으면 사라진다.
6. 공격 시 노란 호가 잠깐 표시된다. 공격 간격은 0.3초다. Play를 다시 시작하면 새로 생성한다.

## Inspector 설정

- Enemy Spawner: Enemy Prefab, Player, Spawn Points, Spawn Interval, Total Spawn Count.
- EnemyMvp 프리팹 / EnemyFollow: Move Speed(2), Stopping Distance(0.5).
- EnemyMvp 프리팹 / EnemyHealth: Max Health(3), Hit Flash Duration(0.1), Hit Color.
- 플레이어 / PlayerMvpMovement: Input Actions(기존 InputSystem_Actions), Move Speed(4).
- 플레이어 / PlayerMeleeAttack: Input Actions, Aim Camera, Attack Indicator, Damage(1), Attack Range(1.5), Attack Angle(100도), Attack Cooldown(0.3), Damage Layers.
- 적의 CircleCollider2D는 공격 검출용 Trigger다. 별도 BoxCollider2D와 Dynamic Rigidbody2D가 적끼리의 몸체 충돌을 처리한다. 플레이어에는 몸체 콜라이더가 없어 플레이어·적 간 밀어내기는 없다.

공격 판정은 플레이어 중심의 원형 검색 범위와 마우스 방향의 각도 조건을 함께 사용한다. 여러 적을 동시에 맞힐 수 있고, 한 적에게 콜라이더가 여러 개 있어도 공격당 한 번만 피해를 전달한다. 범위 경계는 콜라이더 겹침, 각도는 콜라이더 중심을 기준으로 한다.

## 다른 개발자의 플레이어 코드와 연결

공통 계약은 Assets/0.Scripts/Game/Combat/IDamageable.cs이며 Combat.Runtime 어셈블리에 있다.

```csharp
public interface IDamageable
{
    void TakeDamage(int damage);
}
```

EnemyHealth가 이 인터페이스를 구현한다. PlayerMeleeAttack은 콜라이더의 부모 계층에서 IDamageable을 찾아 TakeDamage(damage)를 호출하며, 구체적인 EnemyHealth 타입을 참조하지 않는다. 0 이하 피해는 무시한다. 사망한 적은 추가 피해를 무시하고 추적·충돌·물리를 즉시 중단한 뒤 프레임 끝에 제거된다.

플레이어 이동과 공격은 별도 컴포넌트다. 다른 이동 코드를 붙일 때 PlayerMvpMovement를 제거하거나 비활성화한다. 공격 입력을 교체할 경우 PlayerMeleeAttack의 기존 입력 구독을 함께 교체하고 TryAttack(Vector2 aimDirection)에 월드 XY 방향을 전달한다. 이 메서드는 공격 성공 여부를 반환하고 공격 간격도 검사한다. 같은 Player/Attack 입력을 두 곳에서 중복 호출하지 않는다.

입력 컴포넌트는 기존 Player/Move 및 Player/Attack 액션을 복제해 사용한다. 이 컴포넌트의 비활성화가 공유 액션을 끄지 않는다.

## 생성과 추적 규칙

생성 지점은 순서대로 순환하며, 빈 목록이나 None 슬롯은 스포너 위치를 사용한다. Total Spawn Count는 누적 생성 수이며 적이 죽어도 보충 생성하지 않는다. 몬스터끼리는 통과하지 않고 서로 막거나 밀린다. 같은 위치에 생성되면 물리 프레임을 거쳐 분리된다. 스포너를 껐다 켜도 누적 수를 유지한다.

EnemyFollow는 스포너가 SetTarget(Transform)으로 대상을 지정한다. 플레이어가 사라지거나 비활성화되면 이동을 멈춘다. 장애물 길찾기와 플레이어 체력은 없다. 보스의 추적·랜덤 패턴 로그·피격·사망은 BossMvp.md를 참고한다.

## 테스트와 에디터 도구

Window > General > Test Runner의 PlayMode에서 전체 테스트를 실행한다. 실제 Input System의 합성 키보드·마우스 입력과 실제 씬에서 생성된 적의 처치까지 검증한다.

- Tools > Enemy MVP > Upgrade Enemy Body Collision: 기존 적 프리팹에 몸체 충돌을 적용한다. 현재 프리팹에는 이미 적용돼 있다.
- Tools > Enemy MVP > Create Test Scene: 기본 씬 생성. 기존 씬을 덮어쓰지 않는다.
- Tools > Enemy MVP > Upgrade Test Scene With Combat: 기존 MVP 씬과 EnemyMvp 프리팹에 전투 컴포넌트를 연결한다. 현재 제공된 씬에는 이미 적용돼 있다.

Unity 6000.5.0f1에서 전체 PlayMode 테스트 26개 통과(실패·건너뜀 0). 결과는 Logs/collision-final.xml이다. 다른 에디터가 연 원본과 충돌하지 않도록 .utmp/EnemyMvpValidation 복사본에서 실행했다. 그래픽 없는 배치 모드이므로 화면 외형은 Play Mode에서 수동 확인한다.





## 보스

기존 테스트 씬 위쪽에 큰 보라색 보스가 배치되어 있다. 체력10, 이동속도1.5, 공격 거리2, 패턴 간격2초이며 세 임시 후보를 랜덤으로 선택해 로그만 출력한다. 기존 플레이어 근접 공격으로 피격·사망한다. 선택 판단 교체 방법은 [BossMvp.md](BossMvp.md)를 참고한다.
- 타이머 추가: 보스는 시작 시 숨겨져 있고10초에 등장한다. 보스전 중 시간·일반 적 생성은 중지되며 남은 일반 적은 정리한다. 처치 후00:10부터 시간과 새 생성 주기가 재개된다. 상세: [BossEncounter.md](BossEncounter.md).
