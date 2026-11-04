# 작업 인수인계

## 2026-11-04 WorldWrap2D 리스타트 시 Unity 정지

- 시작 상태: `EnemyEXP`, HEAD `dec1a74`. `main`에는 없고 이 브랜치의 `WorldWrap2D`만 해당.
- 원인: MainTestScene Ground 타일맵(약 2,262칸) Grid를 Play 진입 때 8장 동기 `Instantiate`하고, Play 종료·씬 리스타트 때 `Destroy` 지연 정리에 의존했다. 대용량 Tilemap 사본이 한 프레임에 생성·반쯤 남은 채로 정리되면 에디터까지 멈춘 것처럼 보인다.
- 수정: `Assets/0.Scripts/Game/WorldWrap2D.cs`에서 래핑은 즉시 유지하고, 시각 복제는 프레임당 1장 생성. 사본은 `DontSave`로 씬에 저장되지 않게 하며 `WorldWrap Visuals` 아래에 둔다. `OnDisable`에서 코루틴 중지 후 에디터는 `DestroyImmediate`로 즉시 제거. 사본의 Collider/Rigidbody/MonoBehaviour는 제거해 게임플레이 중복을 막는다.
- 회귀 테스트: `Assets/Tests/PlayMode/WorldWrap2DRestartTests.cs` — 비활성/재활성/파괴 후 `(Wrap …)` 사본과 `WorldWrap Visuals`가 남지 않는지 검사.
- 검증: 코드 정적 확인. batchmode Unity는 라이선스 없음(exit 198)으로 테스트 실행 실패. 에디터 Play 확인은 사용자 환경에서 필요. 실행한 통과 테스트 0개.
- 확인 순서: EnemyEXP에서 Unity로 MainTestScene → Play → 가장자리 통과해 반대편·바닥 반복 확인 → Game Over 리스타트 또는 Play 중지 후 다시 Play. Hierarchy에 `Ground (Wrap …)` / `WorldWrap Visuals`가 Play 종료 후 남지 않고, Unity가 멈추지 않으면 정상. 오류 시 Console 첫 줄과 Player Target의 WorldWrap2D Source Tilemap 연결을 본다.

## 2026-11-04 MainTestScene 반복 맵과 플레이어 경계 래핑

- 작업 시작 시 `EnemyEXP`, HEAD `1952999`. 적 EXP DNA 변경이 작업 트리에 이미 있어 보존했다. MainTestScene은 작업 전 미변경 상태였다.
- `Assets/9.Scenes/MainTestScene.unity`의 `Player Target > WorldWrap2D`가 플레이어 경계 래핑과 반복 타일맵 복제를 담당한다. 경계는 Inspector의 `World Bounds` Rect(X/Y/Width/Height, 월드 좌표)로 지정하며 현재 기본값은 타일맵 크기에 맞춘 (-37, -21, 67, 37)이다. 플레이어가 x/y 경계를 넘으면 반대편 좌표로 래핑한다. Scene 뷰에서 Player Target을 선택하면 청록색 와이어 사각형과 크기 라벨이 보인다.
- 타일맵 Grid는 8방향으로 런타임 복제되어 경계 바깥을 채운다. 실행 중 원본 맵을 설정한 Rect 중앙에 정렬하며, Rect 크기가 실제 타일맵 크기와 다르면 연속 이음새가 맞지 않을 수 있어 경고를 남긴다. 복제 오브젝트는 플레이어 오브젝트가 파괴될 때 정리되며 씬 파일에는 원본 Ground와 플레이어 컴포넌트만 저장된다.
- 확인 순서: Unity 임포트/컴파일 후 MainTestScene의 Hierarchy에서 `Player Target` 선택 → Inspector의 `World Bounds`에서 X/Y/Width/Height 조정 → Scene 뷰의 청록색 사각형이 경계와 일치하는지 확인 → Play → 기존 이동 입력으로 가장자리/모서리를 통과한다. 반대편에서 나타나고 바닥이 이어져야 한다. Play 종료/재시작 후 `Ground (Wrap …)` 런타임 복제물이 남지 않아야 한다. 오류 시 Console 첫 오류, Source Tilemap 연결, Rect와 타일맵 크기 일치 여부를 확인한다.
- 검증: 코드·씬 연결과 `git diff --check` 정적 확인. Unity 컴파일/Play 및 경계 타일의 실제 화면 이음새는 확인하지 않음. 테스트 추가/실행 0개.
- 다음 작업: 위 Inspector·Scene 뷰·Play 절차로 경계 크기 조정과 가장자리 이음새를 확인한다. 적과 투사체 등 다른 월드 오브젝트의 래핑은 이번 범위에 추가하지 않았다.
- 후속 문서화: `WorldWrap2D.cs`의 필드와 Unity 생명주기 메서드, 경계 계산·래핑·타일맵 복제·Scene 기즈모·정리 코드에 한국어 설명 주석을 추가했다. 런타임 동작은 변경하지 않았다.

## 2026-11-04 ExperiencePickup 참조 컴파일 오류 수정

- 사용자 보고: `FindFirstObjectByType<PlayerData>()`가 더 이상 사용되지 않는다는 표시와 `ExperiencePickup` 이름을 찾지 못하는 오류.
- 원인: `EnemyHealth`는 `EnemyMvp.Runtime` asmdef 안에 있고 `ExperiencePickup`은 기본 `Assembly-CSharp`에 있어, asmdef가 기본 어셈블리 타입을 직접 참조할 수 없었다.
- 수정: `EnemyMvp.Runtime`에 `IExperiencePickup` 계약을 추가하고 `ExperiencePickup`이 구현하도록 변경. `EnemyHealth`는 인터페이스만 참조해 픽업 목표를 설정한다. 검색 API는 `FindAnyObjectByType<PlayerData>()`로 변경했다.
- 검증: Unity Editor에서 강제 스크립트 재컴파일 및 도메인 리로드 완료(리로드 프로파일 약 10.4초). 최신 `Logs/Editor.log`에서 `ExperiencePickup`/`IExperiencePickup` 관련 컴파일 오류가 더 이상 보이지 않음. Unity 테스트/Play는 실행하지 않음. `CombatPopupController.cs`의 `FindObjectsByType<T>(FindObjectsSortMode)` obsolete 경고는 별도로 남아 있음.
- 다음 확인: 앞 항목의 DNA 드롭/유도 절차를 진행한다. 해당 경고는 이번 픽업 오류와 별개이므로 별도 정리 작업에서 다룬다.

## 2026-11-04 적 사망 EXP DNA 픽업

- 시작 상태: `EnemyEXP`, HEAD `1952999`, 작업 트리에서 이 작업 전에는 변경 없음. 작업 범위는 적 사망 시 `Ingame_DNA_0` 생성과 플레이어 EXP 획득 반경/유도/회수 연결.
- 완료: `Assets/1.Prefabs/Ingame_DNA_0.prefab`을 기존 `Ingame_DNA_0` 스프라이트로 생성하고 trigger collider와 `ExperiencePickup`을 추가. `EnemyHealth` 사망 시 연결된 프리팹을 사망 위치에서 생성하며, `EnemyFollow.Target`을 픽업에 넘김. EnemyMvp·ZombieHound·BossMvp의 `EnemyHealth`에 해당 프리팹 참조를 연결.
- `PlayerData.experiencePickupDistance`를 추가. 픽업은 플레이어 중심의 설정 거리 안에서 `attractionSpeed`로 이동하고, 플레이어와 접촉하면 설정된 `experienceAmount`를 `PlayerData.AddEXP`로 더한 뒤 사라짐. EXP 요구량을 넘는 값은 현재 모델 계약대로 보존.
- 기획 수치는 미정으로 합의됨: 플레이어 획득 거리, 프리팹 유도 속도, DNA당 EXP를 Inspector 설정으로 노출하고 모두 0으로 둠. 값이 설정되기 전에는 자동 유도/EXP 증가는 작동하지 않음.
- 검증: 코드·프리팹 참조 정적 확인 및 `git diff --check` 통과. Unity 자동 테스트와 에디터 컴파일/Play 검증은 실행하지 않음. `.meta` GUID 및 프리팹 Inspector에서 실제 참조, Trigger 수집 동작은 다음 확인 필요.
- 다음 확인: Unity 임포트/컴파일 후 `Ingame_DNA_0` 프리팹의 Experience Amount와 Attraction Speed, PlayerData의 Experience Pickup Distance를 기획값으로 입력한다. Play에서 적 처치 → 사망 지점 DNA 생성 → 반경 밖에서는 정지 → 반경 안에서는 플레이어로 이동 → 접촉 시 EXP와 HUD 갱신을 확인하고 Console을 확인한다.

## 2026-11-03 EnemyMvp 애니메이션 및 유실 설정 복원

- 시작 상태: EnemyEXP,MAP, HEAD b71b791. Git 변경은 Assets/_Recovery 및 meta만 있었고 이전 대화에서 추가한 애니메이션과 사용자 수정이 사라진 상태였다. _Recovery는 보존했다.
- 복원: Zombie/Animator에 8방향 대기 4프레임·이동 8프레임의 12fps 루프 클립 16개와 Zombie.controller 재생성. EnemyMvp에 Animator와 기존 EnemyDirectionAnimator 연결. 대기·이동 상태 Speed 0.5로 실효 6fps(프레임 간격 약 0.167초).
- 대화에서 확인한 사용자 설정 복원: EnemyMvp의 남쪽 좀비 스프라이트, maxHealth 10, 빨간 피격 색, 콜라이더 oldSize 0.3×0.6, EnemyHealth UI 헤더. 콜라이더 실제 크기와 이동 속도 3 유지. 컴포넌트 직렬화 순서는 현재 순서 유지. 씬과 복구 파일은 수정하지 않음.
- 검증: 클립 16개·소스 스프라이트 96개 참조와 컨트롤러 참조 16개, Speed 0.5 두 상태, 프리팹 ID 중복 없음 검사 통과. Unity 테스트 0개. 에디터 임포트·컴파일·재생 및 빌드·기기 미검증. 이전 Unity 먹통 원인은 확정하지 못함.
- 다음 확인: Unity 임포트 완료 → Assets/1.Prefabs/EnemyMvp 선택 → Animator Controller Zombie, EnemyDirectionAnimator 연결, EnemyHealth Max Health 10과 빨간 Hit Color 확인 → MainTestScene 등 EnemyMvp 사용 씬에서 Play → 8방향 추적 중 이동 모션, 정지 시 대기 모션을 느리게 반복하는지 확인. 오류 시 Console과 Controller 참조 확인. 모르는 미저장 씬 변경은 이 복원에 포함되지 않음.

## 2026-11-03 좀비 들개

- 시작 상태: `main`, HEAD `13e4abe`. 몬스터 기획서에는 기본 좀비 HP 3·공격력 10만 확정돼 있고, 들개·박쥐 대응 수치는 없다. `EnemyDatatable.xlsx`는 엑셀이 열어 둔 상태라 읽거나 수정하지 않음.
- 완료: 64px 8방향 좀비 들개. 회전, idle 8프레임, run 8프레임. 클립 `HoundIdle_*`·`HoundRun_*`, 컨트롤러 `ZombieHound.controller`, 프리팹 `Assets/1.Prefabs/ZombieHound.prefab`. 이동 방향에 따라 idle/run이 바뀐다. 체력 3, 접촉 피해 10, 이동 속도 3은 기본 일반 몬스터와 `EnemyMvp`와 같다. 스폰 타임라인에는 넣지 않음.
- 검증: 남·동·북 회전과 남쪽 idle·run 프레임을 눈으로 확인. PNG 136장, Point 메타 136개, 클립 16개. Unity 테스트 0개. 에디터 재생, 씬, 빌드, 기기는 확인하지 않음.
- 다음 확인: Unity 임포트 후 `ZombieHound` 프리팹을 테스트 씬에 두거나 `Enemy Spawn System`의 Enemy Prefabs에 넣는다. 플레이어를 따라 달리면 run, 붙으면 idle. 그림이 비면 `Zombie_Hound` Reimport.
- 속도·무리 스폰 수치는 기획 미정이라 확정하지 않음.

## 2026-11-03 Beast Zombie Boss idle 모션

- 시작 상태: `main`, HEAD `7cd15af`. Beast Zombie Boss의 8방향 `BossIdle_*` 클립은 회전 스프라이트 1장을 두 번 붙잡은 정지 클립이었다.
- 완료: PixelLab 캐릭터 `45afeb63-7461-402b-9282-8d0bbcc7bdfe`에 `breathing-idle` 템플릿으로 8방향 4프레임을 생성했다. 그룹 `c0647bc4-943d-4c82-890a-78321270802c`. 프레임은 `Idle/animations/idle/{방향}/0.png`~`3.png` 32장. 기존 `BossIdle_South`부터 `BossIdle_NorthWest`까지 8개 클립이 이 프레임을 12샘플, 1초 루프로 재생한다. 컨트롤러 `Boss Idle Directions`와 클립 GUID는 유지했다.
- 검증: 남·북·동 프레임을 눈으로 확인했고, 같은 보스 자세에서 가슴이 늘었다 줄었다. PNG 32장 모두 96×96, Point 필터 메타 32개, 방향 클립 8개가 각 4프레임을 가리킨다. Unity 테스트 0개. Unity가 꺼져 있어 에디터 재생, 화면, 빌드, 기기는 확인하지 않음.
- 다음 확인: Unity로 프로젝트를 연 뒤 임포트가 끝나면 `Boss.controller`의 BossIdle 상태를 재생하거나, 보스전이 있는 씬에서 보스가 멈춘 채 숨 쉬는 모션이 도는지 본다. 스프라이트가 비면 `Idle/animations/idle` 우클릭 → Reimport 후 Console을 확인한다.
- 씬, 프리팹, 코드, Project Settings, 패키지는 변경하지 않음.

## 2026-11-03 스프라이트 필터 설정

- 시작 상태: `main`, HEAD `b3b0de6`, 작업 트리 깨끗함. 기존 Handoff.md는 없었음.
- 완료: `Assets/2.Resources` 하위 TextureImporter의 Sprite(`textureType: 8`) 1,471개를 확인하고 Bilinear였던 908개의 `.meta`를 Point(`filterMode: 0`)로 변경. 기존 Point 563개 유지.
- 검증: 전체 Sprite 1,471개에서 Point가 아닌 항목 0개. Git diff에서 필터 값 외 변경 0줄. `git diff --check` 통과. GUID와 다른 임포트 설정은 유지.
- 실행한 Unity 테스트: 0개. 설정 파일 검사만 수행했으며 Unity 재임포트, 화면 표시, 빌드와 기기는 검증하지 않음.
- 다음 확인: Unity로 돌아가 자동 임포트 완료 후 Project → Assets/2.Resources에서 스프라이트 선택 → Inspector → Filter Mode가 `Point (no filter)`인지 확인. 이전 값이 보이면 해당 에셋 우클릭 → Reimport 후 Console의 임포트 오류 확인.
- 이번 작업에서 씬, 프리팹, 코드, Project Settings, 패키지는 변경하지 않음.
