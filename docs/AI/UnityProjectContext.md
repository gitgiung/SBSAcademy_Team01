# Unity 프로젝트 컨텍스트

## 최신 상태 — 2026-10-28

- 새 세션의 시작 문서는 [Handoff.md](Handoff.md)와 루트 [AGENTS.md](../../AGENTS.md)다. 이 문서 아래의 10월 18일 내용은 역사 기록이며 최신 상태를 대신하지 않는다.
- 확인 시점 HEAD `7f31651` (`초기화, 로딩, 리소스 로딩 수정`), 브랜치 `feature/poolmanager`. 복귀 시 실제 Git 상태를 재확인한다.
- Unity 6000.5.0f1, URP 17.5.0, Addressables 2.9.1, Input System 1.19.0, Unity Test Framework 1.7.0.
- 현재 `activeInputHandler: 2` (Both)다. 아래의 `1`은 이전 기록이다.
- 현재 활성 빌드 씬은 `Assets/9.Scenes/TitleTestScene.unity`, `LoadTestScene.unity`, `MainTestScene.unity`다. 과거 SampleScene 누락 설명은 현재 상태가 아니다.
- Managers/Core 초기화·준비 상태 분리와 리소스 로딩 통합 완료. 다음은 UIManager 팝업 중복·표시 순서 수정이다.
- 마지막 실행 검증은 2026-10-28 PlayMode 36/36 통과다. 아래 26/33/41은 당시 다른 시점의 기록이며 현재 테스트 수가 아니다.
- MP 명칭 통일, 미정 기획 유지, 완료 시 사용자 직접 확인 절차 안내를 적용한다.
- 작업 경로·임시 폴더·에디터 실행 상태는 매 세션 확인한다. 상세 사용법은 [CoreInitialization.md](CoreInitialization.md), [ResourceLoading.md](ResourceLoading.md)를 참고한다.

## 2026-10-18 역사 기록

확인일: 2026-10-18. 기준 커밋: `8373720`.

- 프로젝트 루트: `C:/GitHub/SBSAcademy_Team01`.
- 확인됨: Unity `6000.5.0f1` (`ProjectSettings/ProjectVersion.txt`).
- 확인됨: URP 17.5.0 및 2D Renderer 설정 (`Packages/manifest.json`, `Assets/Settings/Renderer2D.asset`).
- 확인됨: Input System 1.19.0, activeInputHandler=1. 기존 Player/Move, Player/Attack 액션을 플레이어 MVP 컴포넌트가 복제해 사용한다.
- 확인됨: Unity Test Framework 1.7.0 설치. PlayMode 테스트와 EnemyMvp 프리팹이 추가됐다.
- 폴더: `Assets/0.Scripts/Game/{Combat,Player,Enemy/{NormalEnemy,Boss}}`, `Assets/1.Prefabs`, `Assets/2.Resources`, `Assets/3.Datas`, `Assets/9.Scenes`.
- 씬: MainTestScene, GiungTestScene, DonjinTestScene. 직렬화된 오브젝트는 카메라와 MainTestScene의 2D 조명이다.
- 기존 Build Settings는 현재 없는 `Assets/Scenes/SampleScene.unity`를 가리킨다. 이번 기능에서 빌드 설정은 변경하지 않는다.
- 구조: Combat.Runtime에 IDamageable, EnemyMvp.Runtime에 EnemyFollow/EnemySpawner/EnemyHealth, PlayerMvp.Runtime에 PlayerMvpMovement/PlayerMeleeAttack, Boss.Runtime에 BossController 및 교체 가능한 BossPatternSelector. 명시적 참조와 작은 MonoBehaviour로 구성한다.
- 저장소 지침: README는 3인 게임 개발 프로젝트임을 명시한다. 루트 및 Assets에서 AGENTS.md 발견되지 않음.
- Unity MCP는 확인되지 않음. 설치된 Unity.com 6000.5.0f1 배치 실행으로 컴파일·PlayMode 테스트가 가능하다. 원본 에디터와 충돌을 피하기 위해 Git 무시 경로 .utmp/EnemyMvpValidation에서 검증한다.
- 사용자 확정: 탑다운, 장애물 없는 공간. 기존 EnemyMvpTestScene에서 적 생성·추적에 WASD 이동, 마우스 방향 근접 공격, 적 피격·사망을 추가한다. 공통 계약은 void TakeDamage(int damage).
- 에디터 도구: EnemyMvpSceneBuilder가 별도 테스트 씬 생성 및 기존 씬의 전투 기능 업그레이드를 제공한다.
- 적 몸체 충돌: EnemyFollow는 Dynamic/Continuous Rigidbody2D와 일반 BoxCollider2D를 사용한다. EnemyMvp 프리팹의 기존 CircleCollider2D Trigger는 피격 검출용으로 유지한다. 같은 위치 생성 시 물리 프레임을 거쳐 겹침을 해소한다. 전체 PlayMode 테스트 26개 통과.

- 보스: EnemyMvpTestScene 위쪽에 BossMvp 프리팹(보라색, 체력10, 속도1.5). 거리2에서 정지하고 2초마다 랜덤 임시패턴 로그만 출력. BossPatternSelector를 상속한 컴포넌트로 판단을 교체한다. EnemyHealth.Died를 구독해 사망 즉시 판단을 중단한다. 전체 PlayMode 테스트33개 통과.
- 타이머·보스전: Encounter.Runtime에 BossEncounterController와 EncounterTimerView. EnemyMvpTestScene HUD의00:00이10초에서 정지하고 보스 처치 메시지로 전환된다. 일반 적 생성 중지·씬의 활성/비활성 일반 적 정리 후 숨겨둔 보스 활성화. 보스 사망 시00:10부터 재개하고 스포너 생성 수를 초기화해 새 주기 시작. 보스 한 번만 등장. PlayMode 전체41개 통과.
