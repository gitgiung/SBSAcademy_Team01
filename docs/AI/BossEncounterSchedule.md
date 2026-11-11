# 보스 등장 일정 설정

## MainScene 설정

1. Play를 종료하고 `Assets/9.Scenes/MainScene.unity`를 다시 연다. 저장하지 않은 사용자 씬 변경이 있다면 먼저 보존한다.
2. Hierarchy의 `Boss Encounter`를 선택한다. `Boss Encounter Controller > Schedule`은 `MainBossEncounterSchedule`, `Boss Target`과 `Boss Spawn Anchor`는 기존 Player에 연결되어 있다.
3. Project에서 `Assets/3.Datas/MainBossEncounterSchedule.asset`을 선택한다. `Entries`의 Element 0~5가 각각 1~6회차다.
4. `Appear Time`은 초 단위이며 기본값은 180·360·540·720·900·1080이다. `Boss Prefab`에 회차별 프리팹을 넣는다. 루트에 `EnemyHealth`와 `BossController`, `HordeLordBoss`, `ZombieSlimeBoss` 중 하나가 필요하다. 야수는 `BossPatternSelector`도 필요하다.
5. `Spawn Offset`은 anchor에서 떨어진 월드 좌표다. 첫 야수는 기존 배치를 유지하는 `(0, 9.26, 0)`이다. 나머지는 `(0, 0, 0)`이므로 프리팹을 연결할 때 의도한 등장 위치도 설정한다.

초기 배포 설정은 3분의 `BeastZombie`만 연결되어 있다. 6~18분의 보스 순서와 새 보스 종류는 미정이므로 나머지 5칸은 비워 두었다. 현재 지원하는 다른 프리팹은 `Assets/1.Prefabs/HordeLordBoss.prefab`, `Assets/1.Prefabs/ZombieSlimeBoss.prefab`이다. 같은 프리팹을 여러 회차에 넣어도 매번 새 인스턴스를 생성한다.

새 일정을 만들려면 Project의 `Create > Game > Boss Encounter Schedule`을 사용하고 해당 씬의 Schedule에 연결한다. 공용 에셋을 여러 씬에서 참조하면 설정도 공유한다. Play 중 일정 에셋을 편집해도 현재 판의 예약 목록은 바뀌지 않으며 다음 Play부터 적용된다.

## 진행 규칙

- 시각은 보스전 시간을 제외한 진행 타이머 기준이다. 3분 보스전이 실제 1분 걸려도 다음 보스는 화면 타이머 6분에 나온다.
- 보스 등장 시 일반 적을 보상 없이 정리하고, 일반 스폰과 진행 시계만 멈춘다. 전투 전체의 `Time.timeScale`은 바꾸지 않는다.
- 보스에게 플레이어 대상과 공용 HP바를 연결하고, 야수는 기존 등장 원형 연출을 실행한다. HP바는 기존 Fade Duration을 사용한다.
- 처치하면 다음 회차를 예약하고 일반 전투를 재개한다. 정확히 18분에도 마지막 보스가 나온다.
- Controller를 꺼 둔 사이 여러 등장 시각이 지났다면, 다시 켤 때 이미 지난 회차를 차례로 시작한다. 각 보스를 처치하면 다음 지난 회차가 즉시 시작하며, 과거 경계 때문에 시계가 잠기지 않는다.
- 마지막 처치는 일정 종료만 뜻한다. 자동 승리·보상·히든 보스를 추가하지 않았다. Director의 기존 18분 진행/스폰 한도는 유지한다.
- Schedule이 없는 기존 씬은 기존 Boss/Horde Lord/Zombie Slime, Boss Appear Time 단일 등장 경로를 유지한다. Schedule을 연결하면 그 아래의 옛 단일 등장 시각은 사용하지 않는다.

## 미지정 및 잘못된 설정

- 프리팹이 비어 있는 회차는 시작 시 회차·시각을 포함한 경고를 한 번 기록하고 건너뛴다. 모든 회차가 비어 있어도 옛 Boss로 대체하거나 시계를 멈추지 않는다.
- 잘못된 프리팹, Director 진행 한도 이후의 시각은 해당 회차의 오류를 기록하고 건너뛴다. Boss Target을 찾을 수 없는 경우도 해당 시각에 오류를 기록하고 건너뛴다.
- 시각은 양수·유한값·오름차순이어야 한다. 중복 시각, 역순, null 항목, 유효하지 않은 오프셋은 일정 전체 오류다. 이때 일정 보스는 실행하지 않으며 시계를 잠그지 않는다. 자동 정렬이나 다른 보스로 대체하지 않으므로 설정을 고친 뒤 다시 Play한다.

## 직접 확인 순서

1. Unity 임포트/컴파일이 끝난 뒤 Console의 첫 오류를 확인한다. `Tools > Play From Title`을 해제하고 MainScene을 연다.
2. 위 Inspector에서 일정 6개 시각·프리팹·등장 위치를 확인한다. 기본 설정에서는 미지정 2~6회차 경고 5개가 예상된다.
3. Play 후 `Enemy Spawn System`의 `EnemySpawnDirector` 컴포넌트 메뉴 `테스트/진행시간 30초 앞으로`를 사용하면 실제 3분을 기다리지 않고 경계로 진행할 수 있다. 보스전 중에는 이 메뉴도 진행 시간을 늘리지 않는다.
4. 03:00에 기존 위치의 야수 등장, 진행 타이머 정지, 원형 등장 연출 이후 HP바 페이드를 확인한다. 플레이어 이동·공격은 계속 가능해야 한다.
5. 처치 후 HP바 숨김과 타이머·일반 스폰 재개를 확인한다. 테스트용으로 다른 회차에 보스를 직접 지정했다면 그 회차에서도 같은 흐름과 해당 보스의 HP바 감소를 확인한다. Play 중 임시로 연결할 경우 다음 Play부터 반영된다.
6. 최종 회차를 지정해 확인할 때는 18:00에 보스가 나오고, 처치 후 자동 승리 팝업이 추가로 열리지 않는지 확인한다.

보스가 안 나오면 Console의 회차별 오류, Schedule 연결과 Boss Prefab 루트 컴포넌트, Boss Target, Director 진행 한도를 확인한다. HP바가 없으면 Boss Health Slider 안에 실제 Slider가 있는지 확인한다. 과거 설정이 보이면 MainScene을 다시 열고 현재 씬과 일정 에셋을 확인한다.

## 검증 범위

Unity 6000.5.0f1의 짧은 임시 프로젝트에 실제 Combat/Enemy/Boss/Encounter 소스와 `Assets/Tests/PlayMode/Encounter` 테스트를 복사해 검증했다. 원본 프로젝트의 두 번째 Editor를 열거나 원본 씬·프리팹을 Unity로 전체 재저장하지 않았다. Test Runner의 PlayMode에서 `BossEncounterScheduleTests`를 선택해 같은 테스트를 실행할 수 있다.

최종 PlayMode **25/25 통과**. 관련 .NET 검증은 등장 VFX 계산 **6/6**, 플레이어 보스전 피격 분기 **7/7** 통과. Unity Roslyn의 관련 런타임/Editor 어셈블리 6개 컴파일 성공, 기존 경고 3개가 남아 있다.

실제 MainScene 화면·모든 보스 프리팹의 애니메이션·리소스 로딩·18분 실제 플레이·번들·Android 빌드·기기는 별도 확인 대상이다. 자동 테스트는 분리 프로젝트의 실제 Unity 컴포넌트와 시계/체력/생명주기 흐름을 검증하며, MainScene을 직접 플레이한 결과로 확대 해석하지 않는다.
