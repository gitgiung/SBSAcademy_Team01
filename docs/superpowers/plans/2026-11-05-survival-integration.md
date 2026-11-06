# 18분 성장·스폰 통합 실행 계획

**승인 범위:** 사용자가 다음 작업 전체를 맡기고 자리를 비웠다. 기존 명세/승인된 보스·카드 정책을 유지하며 단계3~5를 구현·검증한다. 실제 기기/18분 플레이 측정은 실행한 경우에만 완료로 보고한다.

**시작:** feature/player-growth-design / 60fd1a4. 직전 경험치 색상 코드/문서/검증 도구 미커밋 변경을 보존한다. 커밋·브랜치 전환·원본 씬 자동 저장 없음.

## 구조 결정

- 단일 시계는 기존 EnemySpawnDirector. BossEncounterController는 이 시계에 구독하고 기존 보스 등장 시각에 정확히 멈추도록 경계를 전달한다. 보스 종료 후 같은 시계를 재개한다. Director가 없는 기존 단독 보스 씬은 이전 로컬 시계를 유지한다.
- EnemyMvp.Runtime에 순수 설정 SurvivalBalanceSettings와 회수 보정 상태 SurvivalExperienceBonus를 둔다. PlayerData/SceneEnemyPool이 기존 경계 인터페이스로 연결한다. 새 전역 매니저/이벤트 버스/풀 없음.
- 30초36구간 총8100회는 씬 전체 목표다. 기존 스포너 개수로 곱하지 않는다. 각 구간 내 반 간격부터 균등 분산하고 프레임당 시도 상한 기본8, 밀린 시도는 기록 후 폐기한다. 차단/풀 실패도 시도를 소비한다. 기존 minute 항목의 프리팹 종류는 유지하고 수치 스케줄만 명세로 교체한다. 단독 EnemySpawner.Configure API는 보존한다.
- 일반 생성 때 rawXP를 EnemyHealth에 고정한다. SceneEnemyPool이 기존 Managers.Pool로 XP를 빌리고 기존 ExperiencePickup에 rawXP를 전달한다. 죽음만 한 번 드롭하고 반환은 드롭하지 않는다. 보스의 기존 보상량은 임의로 명세 일반XP로 교체하지 않는다.
- PooledObject의 선택적 대여 종료 알림으로 SceneEnemyPool의 활성카운트를 정산한다. 조회마다 전체 목록을 순회하지 않는다. lease를 비교해 지연/중복 반환을 막는다.
- 회수는 PlayerData.TryCollectExperience에서 체크포인트 보정과 소수carry를 적용한 뒤 기존 AddEXP로 연결한다. 일반 AddEXP/테스트XP는 raw보석 보정 경로와 구분한다. 보석 색은 원래 rawXP에 기반한다.
- 30초 샘플에 목표/시도/성공/상한/풀실패/프레임폐기/처치/XP드롭·회수/레벨/활성최대/보정을 기록한다. CSV는 Editor/Development에서만 선택적으로 출력하고 기본 비활성이다.

## 단계

- [x] 수치/시도 스케줄/보정 경계 테스트를 먼저 작성하고 기능 부재 실패를 확인한다. 8100합, 29.999/30, 179.999/180, 1080, 차이3/1/2, 보정2XP두번=5, carry보존을 검증한다.
- [x] 설정/순수 계산 구현 후 기존 성장17개와 함께 통과시킨다.
- [x] 풀·처치·중복회수·재대여·130상한·외부반환 회귀를 먼저 추가하고 실제 PoolManager/EnemyHealth/ExperiencePickup/SceneEnemyPool을 격리 Unity에서 실행한다.
- [x] Director 시도예산·보스 시계·관측과 PlayerData 회수 보정 연결을 구현한다. 실제 어셈블리 경계를 유지하고 관련 전체 어셈블리를 컴파일한다.
- [x] 통합 Unity 검사(스폰5개 전체50회, 실패소비, 프레임급증 상한, 일시정지/보스/18분 중단, 정상 처치→색→회수→카드, 청소무드롭)를 실행한다.
- [x] 독립 리뷰 후 중요 문제를 회귀 검증해 수정한다. 최종 Handoff/NextCodex 및 직접 확인 메뉴/정상 결과/오류 확인 내용을 갱신한다.
