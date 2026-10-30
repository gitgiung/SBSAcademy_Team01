# GiungTestScene 몬스터 풀 스폰

## 설정 위치

1. `Assets/9.Scenes/GiungTestScene.unity`를 연다.
2. Hierarchy의 **Enemy Spawn System**을 선택한다.
3. Inspector의 **Enemy Spawn Director**에서 설정한다. 실제 적용값은 기존 Enemy Spawner의 필드가 아닌 이 컴포넌트의 설정이다.

| 필드 | 의미 |
|---|---|
| Spawner Count | 생성 작업을 수행할 스포너 개수. Play 이전에 설정한다. 0이면 생성하지 않는다. |
| Offscreen Margin | 카메라 경계 바깥으로 떨어뜨릴 월드 거리. |
| Minutes | 분별 설정 목록. Element 0은 0~59초, Element 1은 60~119초다. |
| Enemy Prefabs | 해당 분에 생성할 일반 몬스터 종류. 여러 개면 목록 순서대로 순환한다. |
| Spawn Interval | **n**: 배치 사이 간격(초). 첫 배치도 n초 후 생성한다. |
| Total Spawn Count | **m**: **스포너 하나당** 총 생성량. 살아 있는 수가 아니라 해당 작업에서 생성한 누적 수다. |
| Batch Spawn Count | **l**: 간격마다 생성할 마릿수. 마지막에는 남은 수만 생성한다. |

씬의 최초 설정은 기존 값을 이어받은 스포너 1개, n=1, m=5, l=1, EnemyMvp 1종이다. 정식 밸런스를 확정한 값이 아니다. Minutes 항목을 늘려 개발자가 실제 분별 수치와 프리팹을 등록한다. 추가 종류는 루트에 활성 EnemyFollow와 EnemyHealth가 있는 일반 몬스터 프리팹을 준비해 등록한다.

## 진행 규칙

- 마지막 Minutes 항목 이후에는 마지막 설정을 매분 다시 사용한다.
- 각 스포너는 한 분에 최대 한 작업을 시작한다. m마리를 모두 생성하면 GameObject가 비활성화되고 다음 분을 기다린다.
- 분이 바뀌어도 진행 중인 작업은 이전 종류·n·m·l을 유지해 끝까지 완료한다. 이후 현재 분의 설정으로 다음 작업을 시작한다. 놓친 분의 작업을 몰아서 실행하지 않는다.
- m=0이면 생성 없이 완료한다. l이 m보다 커도 m마리까지만 생성한다.
- 한 프레임에는 스포너당 한 배치만 생성한다. 긴 프레임 이후 여러 배치를 한꺼번에 생성하지 않는다.
- 작업 시작 때 현재 카메라 경계 바깥에 배치한다. 배치 생성 전에 화면 안에 들어왔으면 다시 화면 밖으로 옮긴다.
- 몬스터는 스포너의 자식이 아니므로 스포너가 꺼져도 추적한다.
- 이 시스템의 모든 프리팹 종류를 합해 일반 몬스터 최대 130마리까지 대여한다. 제한에 걸린 미생성분은 m에서 차감하지 않고 다음 n초 간격에 재시도한다. PoolManager의 maxSize는 이 제한과 별개다.
- 보스전에는 일반 생성과 분별 시간이 정지한다. 기존 일반 몬스터는 풀로 반납한다. 보스 처치 후 미완료 작업과 시간이 이어진다. 보스 출현으로 Time.timeScale을 변경하지 않는다.
- Unity의 Time.timeScale이 0인 게임 일시정지 중에는 분별 시간·생성 간격·추적이 함께 멈춘다.

## 연결과 수명

- EnemySpawnDirector: 분별 설정, 스포너 개수, 진행 시간과 배치.
- EnemySpawner: 한 작업의 n/m/l, 종류 순환과 생성 누적 수.
- SceneEnemyPool: 기존 Managers.Pool.Pop/Push 연동, 활성 수 제한, 이 씬의 대여 소유권.
- EnemyHealth: 사망 시 반납, 재대여 시 체력·사망 상태·충돌·물리·추적·피격 색상 복구.

새 전역 매니저·풀 매니저·싱글톤은 추가하지 않았다. 기존 단독 스포너 API와 씬 참조는 유지한다. 기존 Boss Encounter에는 Spawn Director 참조만 추가했으며 이 참조가 없는 다른 씬은 기존 스포너 목록을 사용한다.

## 직접 확인할 순서

1. 컴파일 완료 후 Console의 새 빨간 오류가 없는지 확인한다. `Tools → Play From Title`이 켜져 있으면 테스트 동안 끄고 GiungTestScene을 연다.
2. Enemy Spawn System을 선택해 위 설정과 Pool Provider, Player, Player Camera, Spawner Template 참조가 모두 연결됐는지 확인한다.
3. Play를 누른다. 기본값에서는 1초 후부터 화면 밖에서 한 마리씩 나와 플레이어를 추적하고, 다섯 번째 생성 후 **Enemy Spawner 1**이 비활성화되면 정상이다. Scene 뷰에서 스포너가 Game 카메라 사각형 밖에 있는지 확인한다.
4. 생성한 몬스터를 공격해 처치한다. 죽은 객체가 비활성 풀로 돌아가고 다음 작업에서 재사용한 몬스터가 정상 체력과 충돌로 다시 추적하는지 확인한다. 풀 대기 객체는 DontDestroyOnLoad의 `@Managers → @Pools → @EnemyMvpPool`에서 확인할 수 있다.
5. 60초에 다음 작업이 시작되는지 확인한다. Minutes를 두 항목 이상 등록했다면 해당 항목의 종류와 n/m/l이 적용돼야 한다. 현재 씬은 사용자 변경으로 **100초에 보스 등장**하도록 설정돼 있다. 보스 등장 후 타이머·일반 생성이 멈추고, 보스 처치 후 이어지면 정상이다.
6. 여러 스포너·배치를 빠르게 확인하려면 Play 이전에 테스트용으로 Spawner Count=3, n=2, m=5, l=2를 입력한다. 각 스포너에서 2초에 2마리, 4초에 2마리, 6초에 1마리씩 생성해 총 15마리이며, 세 스포너가 모두 비활성화돼야 한다. 이 값은 확인용 예시다.
7. `Window → General → Test Runner → PlayMode → Run All`로 전체 회귀 테스트를 실행한다. 최신 실행 결과는 Handoff.md의 이번 작업 기록을 확인한다.

생성이 안 되면 Console의 첫 빨간 오류, Director의 참조와 Minutes 프리팹, Spawner Count, 현재 보스전 여부, 이미 m을 채웠는지, 활성 130마리 제한 여부를 순서대로 확인한다. 잘못된 분별 설정은 EnemySpawnDirector 오류를 기록하고 생성을 중단한다.
