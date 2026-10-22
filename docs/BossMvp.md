# 보스 MVP

## 실행

`Assets/9.Scenes/EnemyMvpTestScene.unity`를 열고 Play한다. 타이머10초에 위쪽의 큰 보라색 보스가 등장한다. 기존 WASD 이동과 마우스 방향 좌클릭 근접 공격을 그대로 사용한다. 등장·처치 후 시간 및 일반 적 생성 흐름은 [BossEncounter.md](BossEncounter.md)를 참고한다.

- 보스는 플레이어를 추적하고 중심 간 거리 2에서 멈춘다. 플레이어가 멀어지면 다시 추적한다. 가까이 들어온 플레이어에게서 뒤로 물러나는 동작은 없다.
- 공격 거리 안에서는 2초 간격으로 임시 후보 3개 중 하나를 랜덤 선택한다. 같은 후보가 연속으로 나올 수 있다.
- Console에 `[Boss] 임시 패턴1: 연속 근접 3연 공격 (로그 전용)` 같은 로그만 출력한다. 투사체·돌진·공격 판정·플레이어 피해는 발생하지 않는다.
- 세 후보의 이름과 행동은 확정된 기획이 아니다. 현재 후보 ID는 Pattern1, Pattern2, Pattern3다.
- 기본 체력 10, 플레이어 피해 1. 피격 시 잠깐 흰색으로 변하고 0이 되면 추적·패턴 선택·충돌을 즉시 중단하고 프레임 끝에 제거된다.

## 설정

- 보스의 BossController: Target, Pattern Selector, Attack Distance(2), Pattern Interval(2초).
- 보스의 EnemyFollow: Move Speed(1.5). 정지 거리는 BossController의 Attack Distance가 실행 중 전달하므로 보스에서는 Attack Distance를 조정한다.
- 보스의 EnemyHealth: Max Health(10), Hit Flash Duration(0.1), Hit Color.
- 프리팹: `Assets/1.Prefabs/BossMvp.prefab`. 몸체 BoxCollider2D와 피격용 Circle Trigger를 함께 사용한다. 공격 한 번에 중복 피해를 받지 않는다.

## 패턴 선택 판단 확장

`Assets/0.Scripts/Game/Enemy/Boss/BossPatternSelector.cs`의 추상 컴포넌트를 상속해 새 선택기를 작성하고 보스의 Pattern Selector 슬롯에 연결한다.

```csharp
public override BossPattern SelectPattern(float distanceToPlayer, int currentHealth)
{
    return currentHealth <= 3 ? BossPattern.Pattern3 : BossPattern.Pattern1;
}
```

현재 연결된 RandomBossPatternSelector는 판단 인자를 사용하지 않고 동일 확률로 선택한다. 앞으로 거리·체력 등을 판단해도 이동과 체력 코드를 바꿀 필요가 없다. 실제 패턴 시전은 BossController.LogPattern의 경계를 별도 실행 구조로 교체할 때 추가한다.

## 연결 구조

BossController가 기존 EnemyFollow와 EnemyHealth를 조합한다. 공통 피해 계약은 기존 IDamageable.TakeDamage(int damage)를 유지한다. EnemyHealth.Died 이벤트를 구독해 사망한 프레임에 BossController를 비활성화하고, 비활성화 시 이벤트 구독을 해제한다.

Tools > Enemy MVP > Add Boss To Test Scene으로 기존 MVP 씬에 보스를 연결할 수 있다. 같은 씬에 보스가 있으면 추가로 복제하지 않는다. 현재 제공된 씬에는 이미 연결되어 있다.

## 검증 기록

보스 추적·거리 정지·재추적, 대체 선택기와 거리·체력 전달, 선택 대기 시간, 대상 비활성화·파괴, 피격·사망, 비활성화·재활성화, 랜덤 후보와 실제 씬 근접 공격 연동을 PlayMode 테스트로 검증한다. 그래픽 없는 Unity 배치 모드이므로 화면 외형은 수동 확인한다.
- 최종 결과: Unity 6000.5.0f1 PlayMode 전체 33개 통과(실패·건너뜀 0). Logs/boss-final.xml. 검증 복사본의 소스·보스 프리팹·씬은 원본 최종 파일과 동일하다.
