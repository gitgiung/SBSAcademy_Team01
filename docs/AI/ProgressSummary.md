# 좀비소울 진행 요약

기준: **2026-11-02**. 기획 원문은 `SBSAcademy_Team01_기획서.docx`와 `Zomso_Skill_기획서.docx`, `Zomso_Parrying_기획서.docx`, `Zomso_Build_기획서.docx`다. 수치 표는 `SkillDatatable.xlsx`, `Strengthentable.xlsx`에 있다.

## 기획

가칭 **좀비소울**. 안드로이드용 2D 탑다운 보스 러쉬 액션 로그라이트다. 일반 구간은 자동 추적 탄환과 이동이고, 보스전은 패링·가드·회피·약공격·차징 강공격과 MP 관리다.

한 판은 18분이다. 보스는 3·6·9·12·15·18분에 6마리가 나온다. 보스전에서는 진행 타이머만 멈추고, 레벨업·보상 팝업은 전투 전체를 멈춘다. 일반 몬스터는 동시에 130마리까지다. 성장은 DNA 경험치, 레벨업 선택(획득·강화·삭제), 시작 룰렛, 유물·증강·융합이다. 자원 이름은 MP다.

약공격에는 쿨타임을 두지 않는다. 인게임 HUD는 좌측에 강공격·약공격·회피·방어, 우측에 가상 조이스틱과 보조 스킬, 좌측 상단에 HP·MP·EXP, 우측 상단에 18분 타이머와 일시정지다. 보스 체력바는 하단 중앙이다. 일반·엘리트 체력바는 맞은 뒤부터 머리 위에 보인다.

문서에 미정으로 남은 항목은 필드 전체 크기 N×M, 7번째 히든 보스를 넣는 방법, 퀘스트 조건과 보상, 점수식, 몬스터 HP 바 가시성, 바닥 경고 연출, 씬별 UI의 남은 설명이다. 스킬 표는 런타임 데이터로 연결되지 않았다.

## 플레이에 연결된 구현

공통 매니저는 초기화와 게임 데이터 준비를 나누고, 리소스 로딩·팝업 재사용·사운드·씬 전환 정리를 갖고 있다.

플레이어는 이동, 대시, 가장 가까운 적을 향한 자동 탄환, 피격·사망, 가드·패링 판정을 한다. 패링과 가드는 화면에서 색이 바뀌는 테스트 표시다. MP 소모와 자연 회복은 없다.

일반 몬스터는 플레이어를 추적하고, 맞으면 색이 바뀌며, 죽으면 풀로 돌아가 다시 나온다. 동시에 130마리까지다. 스폰은 화면 밖에서 하고, 분마다 간격·총량·배치를 바꿀 수 있다. 시간대별 지수 증가와 엘리트는 없다.

보스는 가까이서 전방 할퀴기, 두 번 할퀴기, 돌진을 한다. 할퀴기 피해는 동작 시작과 함께 들어간다. 등장 시 일반 몬스터 스폰과 진행 시간만 멈추고, 처치하면 재개한다. 전투 전체는 멈추지 않는다. 기본 등장 시각은 10초이고, 한 번만 나온다. 18분·6회·그로기·붉은 안광·패링 불가 장판은 없다.

사망은 패배다. 최종 승리는 보스 한 마리 처치와 별도인 승리 신호로만 난다. 결과 화면에서 재시작과 타이틀 복귀가 된다. GiungTestScene의 재시작은 그 테스트 씬으로 다시 들어가는 임시 설정이다.

전투 UI는 일시정지와 제목만 있는 레벨업 팝업이다. 선택지, 보상, 경험치, HP/MP/EXP 게이지는 없다.

카메라는 MainTestScene에서 플레이어를 화면 중앙에 고정한다. 맵 경계 제한과 화면 흔들림은 없다.

## 연출로 만들어 둔 것

보스 스프라이트와 애니메이션 연결은 구현되어 있다. `Boss.controller`가 `BossMvp`에 붙어 있고, 내용은 아래와 같다.

- 8방향 대기와 걷기. 멀리 있으면 플레이어 방향을 보며 걷고, 가까우면 그 방향 대기로 멈춘다. 걷기와 정지가 끊기던 문제는 맞춰 두었다.
- 8방향 할퀴기(`swipe`)와 8방향 반대손 할퀴기(`swipe-left`). 패턴 1은 할퀴기 한 번, 패턴 2는 할퀴기 다음 반대손 할퀴기다.
- 공격이 시작되면 방향을 고정하고 해당 클립을 재생한다. 피해는 동작 시작이 아니라 휘두르는 프레임의 `AnimationEvent_ApplySlashDamage`에서 한 번 들어간다. 클립이 끝나면 추적을 다시 켠다.
- 잘려 보이던 동·서·북동 등 일부 프레임은 몸 전체가 나오게 `Animator/FrameSources/`의 통짜 스프라이트로 바꿔 두었다.

지금 열려 있는 `BossController`와 `BossMvp`에는 이 Animator가 없다. 이쪽은 스프라이트 한 장이고, 할퀴기 피해가 동작 시작과 함께 들어간다. 연결된 재생을 보려면 보스 애니메이션이 들어 있는 쪽을 열어야 한다.

그 뒤에 다시 그린 `swipe-both` 8방향 그림은 발톱을 고친 최신 그림이다. 재생 중인 클립은 `swipe`, `swipe-left`와 FrameSources 복사를 보므로, 이 최신 그림까지 클립에 다시 붙인 상태는 아니다.

플레이어 Animator도 따로 있다. 8방향 대기·이동·대시(Charge)·패링·가드다. 패링은 한 번 재생하고, 가드는 한 번 재생한 뒤 마지막 자세를 유지한다. 방어 중 스프라이트 색은 바꾸지 않는다. 이 재생은 지금 전투에 들어가는 색 변경 표시와 아직 한 흐름이 아니다.

## 아직 없는 것

- 18분 타임라인, 보스 6회, 6번째 보스 클리어, 히든 보스.
- MP, DNA·레벨, 룰렛, 유물·증강·융합, 퀘스트, 점수, 부활.
- 엘리트, 시간대별 스폰, 피격 후에만 보이는 몬스터 체력바.
- 기획서대로의 HUD.
- 보스 붉은 안광, 패링 불가 장판, 그로기, 사망 연출 뒤 보상.
- 다시 그린 `swipe-both` 그림을 이미 연결된 할퀴기 클립에 교체하는 것. 클립과 프리팹 연결 자체는 있다.
- 스킬 표의 런타임 연결, Android 빌드, 실기기.

다음에 이을 구현은 다시 그린 `swipe-both`를 이미 있는 할퀴기 클립에 바꾸는 것, 10초·1회 보스전을 18분·6회로 바꾸는 것, 확정된 기획 수치로 DNA·레벨업·보상을 붙이는 것이다. 히든 보스 조건과 퀘스트 보상은 기획이 미정이라 만들지 않는다.

## 코드 구조

코드는 `Assets/0.Scripts`에 있고, 한 어셈블리에서 동작한다. 공통 기능과 한 판의 승패, 플레이어 규칙을 서로 다른 클래스가 맡는다.

```text
TitleTestScene → LoadTestScene → MainTestScene
                      └─ 전투 시험: GiungTestScene

Managers          씬이 바뀌어도 남는 공통 서비스
GameManager       한 판의 진행·승패·결과 화면
Player            입력과 HP·이동·탄환·패링
Enemy / Boss      일반 몬스터와 보스 패턴
Encounter         진행 시간과 보스전 전환
```

**씬.** 타이틀에서 로딩을 거쳐 본 씬으로 들어간다. 로딩은 `Managers.PrepareGame`으로 공통 서비스를 준비한 뒤 본 씬을 연다. 빈 데이터로 시험을 시작할 수 있어, 시작이 곧 스킬 데이터 완료는 아니다. `GiungTestScene`은 같은 전투 코드로 스폰·보스·팝업을 보는 시험 씬이다.

**공통 서비스.** `Managers`는 `@Managers` 하나로 남고, 풀과 사운드 루트를 자식으로 둔다. 바깥에서는 `Managers.Data`, `Pool`, `Resource`, `Scene`, `Sound`, `UI`로만 접근한다.

| 클래스 | 하는 일 |
|---|---|
| `DataManager` | 스킬 등 게임 데이터 검증. 빈 목록·중복 ID·JSON 오류면 실패 |
| `ResourceManager` | 주소로 리소스를 읽고, 같은 요청은 한 번만 로드 |
| `PoolManager` | 몬스터를 꺼내고 되돌림. 보관 한도는 동시 출현 수와 별개 |
| `SceneManagerEx` | 씬을 바꾸기 전에 풀·UI·사운드를 정리. 데이터와 매니저는 유지 |
| `SoundManager` | BGM과 효과음 |
| `UIManager` | 팝업을 하나만 재사용하고, 연 순서대로 위에 쌓음 |

`Managers.Initialized`는 이 공통 서비스가 준비됐다는 뜻이다. 게임 데이터가 준비됐는지는 `PrepareGame`의 상태(`NotStarted`, `Preparing`, `TestReady`, `Ready`, `Failed`)로 구분한다.

**한 판.** `GameManager`는 `Managers`와 별도다. 플레이 중 HP가 0이면 패배, 따로 준 승리 신호면 승리로 바꾸고 결과 창을 연다. 결과 뒤에는 시간이 멈추고, 재시작이나 타이틀로 돌아간다. 보스 한 마리가 죽었다고 바로 승리하지는 않는다.

**플레이어.** `Player`가 입력과 Unity 수명을 받고, 규칙은 아래 클래스로 나눈다.

| 클래스 | 하는 일 |
|---|---|
| `PlayerData` | 체력, 공격력, 이동·탄환 속도 |
| `PlayerMovement` | 이동과 대시 |
| `PlayerCombat` | 가장 가까운 적에게 자동 탄환 |
| `PlayerDefense` | 패링 타이밍, 가드 유지·해제·쿨타임 |
| `PlayerView` | 체력 바와 피격·방어 색 |
| `Bullet` | 날아가는 탄환 |

입력은 `Player/Move`, `Player/Dash`, `Player/Parry`다. 맞는 쪽은 `IContactDamageReceiver`, 체력이 깎이는 쪽은 `IDamageable.TakeDamage`를 쓴다. 가상 조이스틱 이동은 `Mobile/PlayerController2D`가 따로 있다.

**일반 몬스터.** `EnemyFollow`가 추적하고, `EnemyHealth`가 체력과 사망을 맡고, `EnemyContactDamage`가 접촉 피해를 준다. 죽으면 `SceneEnemyPool`이 `Managers.Pool`로 되돌리고 체력·물리·색을 초기화한다. 동시에 나와 있는 수는 130마리다.

스폰은 `EnemySpawnDirector`가 맡는다. 화면 밖에 스포너를 두고, 분 설정(`EnemySpawnMinute`)의 간격·총량·한 번에 나오는 수를 `EnemySpawner`가 수행한다. 이전 분의 남은 양은 끝까지 뽑고, 그다음 분 설정을 쓴다.

**보스.** `BossController`가 추적 거리와 패턴 간격을 갖고, `BossPatternSelector`가 다음에 쓸 패턴을 고른다. 선택 규칙만 바꿀 때는 컨트롤러 대신 셀렉터를 교체한다. 체력은 일반 몬스터와 같은 `EnemyHealth`다. 보스 자신은 풀로 돌리지 않는다. `BossMvp`에 붙어 있는 선택기는 `BossPatternRuleSelector`다.

### 보스 패턴 코드

패턴은 세 개다. 가까운 일반 공격 둘과, 멀리 있을 때의 돌진이다.

```csharp
public enum BossPattern
{
    None = -1,
    Pattern1 = 0, // 전방 할퀴기 1회
    Pattern2 = 1, // 할퀴기 2회
    Pattern3 = 2  // 플레이어를 추적하는 돌진
}
```

거리는 컨트롤러가 먼저 자른다. `BossMvp` 기준 10 이상이면 돌진, 2보다 멀고 10보다 가까우면 공격 없이 추적, 2 이내면 선택기에게 일반 패턴을 맡긴다.

```csharp
private BossPattern SelectAvailablePattern(float distance)
{
    if (distance >= specialPatternDistance)
        return BossPattern.Pattern3;

    if (distance > closeRange)
        return BossPattern.None;

    return patternSelector.SelectPattern(distance, (int)health.CurrentHealth, nextNormalPattern);
}
```

`BossMvp` 수치는 근접 2, 돌진 시작 10, 패턴 1 다음 대기 3초, 패턴 2 다음 대기 5초, 두 할퀴기 사이 0.5초, 할퀴기 반경 2·각도 90·피해 10, 돌진 기본 시간 3초·도착 피해 반경 2다.

지금 프리팹의 선택기는 체력이나 거리를 보지 않고, 컨트롤러가 기억한 다음 일반 패턴만 돌려준다. 패턴 1이 끝나면 다음은 2, 패턴 2가 끝나면 다음은 1이다. 돌진은 이 순서를 바꾸지 않고, 기다리던 일반 패턴을 이어서 한다.

```csharp
public sealed class BossPatternRuleSelector : BossPatternSelector
{
    public override BossPattern SelectPattern(float distanceToPlayer, int currentHealth, BossPattern expectedPattern)
    {
        return expectedPattern == BossPattern.Pattern2 ? BossPattern.Pattern2 : BossPattern.Pattern1;
    }
}
```

`RandomBossPatternSelector`는 교체용이다. 0·1·2 중 하나를 무작위로 고르며, 프리팹에는 붙어 있지 않다.

실행은 `PatternLoop`가 맡는다. 대상이 없거나 시간이 멈추었거나 다음 패턴 시각이 아니면 기다린다. 죽으면 루프를 멈춘다.

```csharp
switch (selected)
{
    case BossPattern.Pattern1:
        ActivePattern = BossPattern.Pattern1;
        PerformForwardSlash();
        yield return null;
        ActivePattern = BossPattern.None;
        nextNormalPattern = BossPattern.Pattern2;
        nextPatternTime = Time.time + pattern1ToPattern2Delay;
        break;

    case BossPattern.Pattern2:
        ActivePattern = BossPattern.Pattern2;
        PerformForwardSlash();
        if (doubleSlashInterval > 0f)
            yield return new WaitForSeconds(doubleSlashInterval);
        if (CanAttackTarget()) PerformForwardSlash();
        yield return null;
        ActivePattern = BossPattern.None;
        nextNormalPattern = BossPattern.Pattern1;
        nextPatternTime = Time.time + pattern2ToPattern1Delay;
        break;

    case BossPattern.Pattern3:
        ActivePattern = BossPattern.Pattern3;
        yield return DashToTrackedTarget();
        ActivePattern = BossPattern.None;
        // 특수 패턴 전에 대기 중이던 일반 패턴을 이어간다.
        nextPatternTime = Time.time;
        break;
}
```

할퀴기는 보스 위치에서 반경 2, 플레이어 방향 좌우 45도(합계 90도) 안의 `IContactDamageReceiver`에게 피해 10을 준다. 돌진은 추적을 끄고 시작 지점에서 플레이어 쪽으로 최대 3초 이동하며, 근접 거리 안에 들어오면 그 자리에 반경 2의 피해를 주고 추적을 다시 켠다. 3초가 지나도 아직 멀면 도착할 때까지 따라간다.

애니메이션이 연결된 쪽은 패턴 번호와 거리 규칙은 같고, 할퀴기 두 줄만 클립 재생으로 바뀐다. 패턴 1은 `swipe`, 패턴 2는 `swipe` 다음 `swipe-left`다. 피해는 `PerformForwardSlash`를 바로 호출하지 않고, 휘두르는 프레임의 `AnimationEvent_ApplySlashDamage`에서 같은 부채꼴 판정을 한 번 한다. 돌진은 그대로다.

**보스전 전환.** `BossEncounterController`가 진행 시간을 세다가 보스 등장 시각에 일반 스폰을 멈추고, 나와 있는 일반 몬스터를 거둔 뒤 보스를 켠다. 보스가 죽으면 시간을 다시 흐르게 한다. 이 정지는 진행 시간과 스폰만 해당한다. 일시정지나 레벨업처럼 전투 전체를 `Time.timeScale = 0`으로 멈추는 일과는 다르다. 시간 표시는 `EncounterTimerView`가 한다.

**전투 UI.** `CombatPopupController`가 일시정지와 레벨업 창을 `UIManager`에 올린다. 겹친 창이 모두 닫혀야 멈추기 전 시간으로 돌아간다. 레벨업 창은 제목과 빈 내용 영역만 있다. 결과 창이 열리면 이 팝업과 정지 권한을 정리한다.

**발표할 때 열지 않을 코드.** `Framework/PoolingExamples`, `Framework/Controllers`, `Game/Player_GiungTest`는 예제와 이전 시험용이다. 지금 전투는 `Game/Player/Player.cs`와 `Game/Enemy`를 쓴다.
