# 작업 인수인계

## 2026-11-04 스킬 개요 PPT

- 완료: `docs/스킬_개요.pptx` 12장. 레어도 테두리 7장 뒤에, 약공격 21·강공격 15는 해당 등급 테두리 안에 문양을 넣은 모습, 전략 10개는 등급이 없어 문양만 넣었다. 이 합성 그림은 PPT에만 있고 프로젝트 아이콘 파일로 추가하지 않았다.
- 검증: 슬라이드 7장과 표 헤더를 읽었다. PowerPoint에서 화면 재생은 확인하지 않음.

## 2026-11-04 스킬 아이콘

- 시작 상태: 스킬 표의 아이콘 경로는 비어 있었다. 상태창은 `SkillData.IconLabel`이 있고 미리 로드된 주소가 있을 때만 그림을 그린다.
- 완료: 스킬별 아이콘 46장은 삭제했다. 레어도 테두리는 등급당 1장, 총 7장이다. `Assets/2.Resources/Art/SkillIcons/SkillFrame_{Common,Uncommon,Rare,Epic,Legendary,Mythic,Fusion}.png`. 64×64, 속은 투명, Point 필터. `Skill_Rarity`의 Rarity_Frame_Res와 Rarity_Color_Hex에 경로와 색을 넣었다. 스킬 표의 Skill_Icon 경로는 비웠다.
- 검증: 46장 연락표를 눈으로 확인했다. Unity 임포트와 상태창 표시는 확인하지 않음. Addressables 그룹은 비어 있어 아이콘 주소를 넣지 않았다.
- 다음 확인: Unity가 임포트를 끝내면 Project의 `Art/SkillIcons`에서 그림이 보이는지 본다. 상태창에 그림이 없고 이름만 보이면 아이콘 주소가 아직 사전 로딩되지 않은 것이다.

## 2026-11-04 Area_Typetable

- 시작 상태: `Area_Typetable` 시트만 있고 칸은 비어 있었다. `AreaType`의 관련 수치 목차는 반경, 각도, 너비, 길이 네 가지다.
- 완료: 그 네 목차와 스킬 아이디·이름·범위 모양을 넣고, 범위가 이미 있는 약공격 21개와 강공격 10개(20006~20015)를 기입했다. 모양에 쓰는 칸만 숫자를 넣었다. 투사체·직선은 길이와 너비, 원·기둥은 반경, 부채꼴·호는 반경과 각도, 고리는 반경과 너비, 십자는 길이와 너비다. 불꽃 탄환의 사거리 칸은 단어 Reach라 길이는 같은 기본 탄환과 같이 7로 두었다. 강공격 20001~20005와 전략 스킬은 범위 모양이 없어 넣지 않았다.
- 검증: 저장 파일에서 헤더와 31행의 아이디·모양·해당 수치를 다시 읽었다. Unity 테스트 0개.
- 다음 확인: 엑셀 `Area_Typetable` 1행에 반경·각도·너비·길이가 있고, 3행 불꽃 탄환부터 33행 태양 붕괴까지 이어지면 된다.

## 2026-11-04 약공격 10002 이후 수치

- 시작 상태: `main`. 열린 `SkillDatatable.xlsx`에서 10002~10007과 10018~10021은 이름·등급·속성만 있고 전투 수치가 없었다.
- 완료: 그 10행에 MP, 쿨타임 0, Instant, 사거리, 범위, 투사체, 타격, 기본 수치, ATK 계수, 설명을 채웠다. 10001 불꽃 탄환과 10008~10017, 강공격 표는 수치를 바꾸지 않았다. 아이콘·애니메이션·프리팹·사운드는 그대로 비어 있다.
- 검증: 저장 후 엑셀에서 10행의 ID·MP·기본 수치·계수를 다시 읽었다. Unity 테스트 0개. 전투 로딩은 확인하지 않음.
- 다음 확인: LightSkilltable 4~9행과 20~23행. 강공격 20001~20005는 여전히 수치가 없다.

## 2026-11-03 불속성 스킬 20개 추가

- 시작 상태: `main`, HEAD `99a471d`. `SkillDatatable.xlsx`는 엑셀에서 열려 있었고, 약공격 불꽃 탄환만 전투 수치가 있었다. 약공격 불 스킬 7개, 강공격 불 스킬 5개.
- 완료: 열린 통합 문서에 저장했다. LightSkilltable 10008~10017에 불속성 약공격 10개, HeavySkilltable 20006~20015에 불속성 강공격 10개를 이름·등급·MP·쿨타임·시전·사거리·범위·타격·기본 수치·계수·설명까지 넣었다. 약공격은 쿨타임 0, Instant. 강공격은 쿨타임 0, Charge, 차징 0.45~0.95초. 아이콘·애니메이션·프리팹·사운드는 비워 두었다. 기존 불꽃 탄환~태양 검, 화염 망치~운석검의 빈 수치는 바꾸지 않았다.
- 배치: 약공격 새 행은 태양 검 다음이다. 얼음 탄환·바람 화살·암석포·전기 화살은 21~23행이 아니라 20~23행으로 내려갔고, ID 수식이 10018~10021을 붙였다. 이름 없는 10008행의 Mystic 칸은 열풍 탄이 대신한다. FusionSkilltable은 그대로 태양 검·화염 강타·플레어 탄환을 가리킨다.
- 검증: 엑셀에서 새 20행의 ID·이름·MP·기본 수치·계수를 다시 읽었다. Unity 테스트 0개. 게임 로딩과 전투는 확인하지 않음.
- 다음 확인: 열린 엑셀의 LightSkilltable 10~19행, HeavySkilltable 8~17행을 본다. 저장 후 다시 열어도 같은 이름이면 반영된 것이다. 이 표는 아직 전투 실행에 연결되어 있지 않다.

## 2026-11-03 body-charge 주먹 이펙트

- 시작 상태: `main`, HEAD `99a471d`. `body-charge/east` 0~7.png는 사용자가 주먹의 붉은 이펙트를 지운 상태였다.
- 완료: 8방향 `Idle/animations/body-charge`에서 주먹 밖으로 뻗은 붉은 선을 제거했다. 동쪽 0~7은 사용자 수정이라 그대로 두었고, 동쪽 8~10과 나머지 7방향 0~10을 처리했다. 북서 7~10, 남동 6은 이펙트가 없어 픽셀을 바꾸지 않았다. 어깨·등·가슴·허리·무릎의 빨간 장식은 남겼다. 이펙트가 몸을 덮던 자리는 주변 피부색으로 메웠다. 캔버스 188×188, RGBA 88장 유지.
- 남서 재처리: 첫 통과는 남서의 어두운 붉은 선을 남겼다. `south-west/0.png`~`10.png` 11장을 다시 지웠다. 빈 공간으로 뻗은 붉은 픽셀은 프레임 0의 2개를 빼고 0이다. 어깨 원, 무릎, 팔·가슴의 붉은 상처 선은 남겼다.
- 검증: 남서 0·4·8 프레임을 확대해 주먹 바깥 별 모양 선이 줄었는지 확인. Unity 테스트 0개. 에디터 재생은 확인하지 않음.
- 다음 확인: Unity에서 `body-charge/south-west`를 우클릭해 Reimport한 뒤 0~10 프레임을 본다. 주먹 밖으로 뻗은 붉은 선이 없고 어깨·무릎 장식은 남아 있으면 정상이다. 예전 그림이면 Console 임포트 오류를 확인한다.
- 씬, 프리팹, 코드, 메타는 변경하지 않음.

## 2026-11-03 Aseprite 소스 빌드

- 게임 저장소 파일은 이 작업으로 바꾸지 않음. 실행 파일은 `C:\Users\User\source\aseprite\build\bin\aseprite.exe`.
- 완료: 태그 `v1.3.18.6`, Skia `m124-08a5439a6b` Windows Release x64, Visual Studio 2022의 `vcvars64`와 Ninja로 1562개 타깃 링크. `aseprite -b --version` 결과는 `Aseprite 1.3.18.6-dev`.
- 다음 확인: 그 exe를 더블클릭해 창이 열리는지 본다. PATH에는 넣지 않음.

## 2026-11-03 좀비 하운드 견종 교체

- 시작 상태: `main`, HEAD `b71b791`. 작업 트리의 `Zombie_Hound` 그림은 빠져 있었고, 프리팹 `ZombieHound`만 수정돼 있었다. 직전 들개는 짧은 주둥이의 소형 개였고, 몬스터 기획서의 들개·박쥐 수치는 여전히 없다.
- 완료: 잉글리시 폭스하운드 골격의 좀비 들개로 교체했다. PixelLab 캐릭터 `30e3b20a-c9f2-4ed9-aca7-cda990aae8c0`. 늘어진 귀, 긴 주둥이, 구분되는 앞·뒷다리. 8방향 회전, idle 8프레임, run 8프레임. 캔버스 96×96. 클립·컨트롤러·스프라이트 GUID는 유지하고 PNG와 스프라이트 rect, 프리팹 `oldSize`만 바꿨다. 체력 3, 접촉 피해 10, 이동 속도 3. 박스 콜라이더 0.32, 원 반지름 0.16은 그대로다. 스폰 타임라인에는 넣지 않음.
- 검증: 남·동·북·서·남동 회전과 남·북 idle, 남·동·서·북동 run을 눈으로 확인. PNG 136장 모두 96×96, rect 96인 메타 136개, Point 필터와 GUID 유지. Unity 테스트 0개. Unity가 이미 열려 있어 에디터 재생은 확인하지 않음.
- 다음 확인: 열린 Unity의 임포트가 끝나면 `ZombieHound` 프리팹을 테스트 씬에 두거나 `Enemy Spawn System`의 Enemy Prefabs에 넣는다. 플레이어를 따라 달리면 run, 붙으면 idle. 그림이 비거나 예전 소형 개면 `Zombie_Hound` Reimport. 몸이 접촉 판정보다 커 보이면 콜라이더를 맞춘다.
- 속도·무리 스폰 수치는 기획 미정이라 확정하지 않음.
## 2026-11-04 Restart 네이티브 교착 조사 및 Burst 생성 DLL 복구

- 시작 상태: `EnemyEXP`, HEAD `b5e1ad3`. WorldWrap 삭제·MainTestScene 컴포넌트 제거·기존 테스트 삭제·인수인계 변경·사용자 복구 씬과 meta를 보존했다. WorldWrap은 복원하지 않았다.
- 시작 오류 확정 원인: 사용자 첨부 `Failed to find entry-points / Mono.Cecil.AssemblyResolutionException`의 대상인 `Library/ScriptAssemblies/Unity.RenderPipelines.Core.ShaderLibrary.dll`은 4,608바이트 파일이 있지만 PE 헤더가 손상돼 .NET에서 Unknown file format으로 거절됐다. ScriptAssemblies의 다른 DLL 헤더는 정상이었다. 같은 프로젝트 `Library/Bee/artifacts/1900b0aE.dag/post-processed/`의 동일 어셈블리는 정상이어서 손상본을 별도 보관 후 이 파일 하나만 복구했다. 복구 후 어셈블리 이름 읽기와 SHA-256 일치를 확인했고, 실제 에디터의 후속 컴파일·도메인 리로드에서 Burst 오류가 새로 발생하지 않았다. 손상 원인 자체(강제 종료 등)는 확정하지 않았다. 전체 Library 삭제·패키지/Unity 버전 변경은 하지 않았다.
- Restart 재현: 실제 원본 에디터(6000.5.0f1, Direct3D12)에서 적이 스폰된 MainTestScene의 사망·Restart 버튼 입력 후 정지를 관찰했다. 임시 단계 로그상 scene/pool/UI/sound Clear가 모두 종료되고 native LoadScene(MainTestScene)이 반환된 후 다음 씬 완료 이벤트와 heartbeat가 중단됐다. 로그를 읽을 때 이번 세션의 최신 파일은 `%LOCALAPPDATA%/Unity/Editor/Editor.log`였으며, 프로젝트 `Logs/Editor.log`는 09:53 시점의 오래된 파일이었다. 파일 시각을 먼저 확인해야 한다.
- 네이티브 스택: 메인 스레드가 `DelayedDestroyCallback → SpriteRenderer.MainThreadCleanup → AssetChangedNotifier.UnsubscribeFromAll → PersistentManager.Lock`에서 대기했다. 동시에 로딩 스레드는 `PreloadManager → LoadSceneOperation → RenderSettings.Transfer → PersistentManager.PreallocateObjectThreaded → SetObjectLockForRead`에서 대기했다. 종료·스레드 대기는 로컬 PDB로 확인했으며 사용자 에디터는 강제 종료하지 않았다. 증거는 지연 삭제와 새 씬 읽기의 잠금 교착을 가리킨다. 단순 GPU 정지나 WorldWrap 원인으로 단정하지 않는다.
- 수정: GameManager 결과 화면 전환만 `SceneManagerEx.PrepareSceneLoad`로 검증·정리를 먼저 수행하고, 기존 Managers 소유 코루틴에서 한 프레임 뒤 실제 LoadScene을 시작한다. 이는 임의 시간 지연이 아니라 예약된 Destroy를 끝낼 프레임 분리다. 기존 public LoadScene/LoadSceneAsync API 동작과 기존 isTransitioning 가드를 보존한다. 잘못된 씬의 동기 예외 및 다음 프레임의 실제 로드 예외 모두 기존 시간 배율·재시도 가드를 복구한다.
- 회귀: `RestartSceneTransitionTests.RestartButtonAfterSpawningEnemiesCanReloadThreeTimes` 추가. 실제 MainTestScene에서 적 스폰, 렌더러 등록 프레임, 플레이어 사망, 직렬화된 Restart 버튼 이벤트, 이전 적 삭제 완료·이전 씬 유지 프레임, 새 씬 진입을 3회 검사한다. 수정 전 프레임 보장 조건에서 1개 실패, 수정 후 Direct3D12 활성화 검증 복사본에서 **1/1 통과**, exit 0. batchmode 스트레스만으로는 원래 네이티브 교착이 재현되지 않아, 실제 에디터 스택과 프레임 순서 검증을 함께 근거로 사용했다. `GameResultFlowTests`의 단일 프레임 완료 가정은 씬 교체 완료를 제한 시간까지 기다리도록 수정했다.
- 전체 테스트: 최종 코드의 Unity 6000.5.0f1 컴파일 및 PlayMode **181개 중 163 통과/18 실패**, 생략 0, exit 2. 기존 삭제 후 기준(180개 중 162 통과/18 실패)과 실패 18개의 이름·원인이 동일하고 새 실패는 없다. 아래 과거 조사에 기존 실패의 전체 이름과 원인이 기록돼 있다. 새 Restart 회귀는 전체 실행에서도 통과했다. 생성물 `.utmp/g/restart-barrier-before.xml`, `restart-after.xml`, `restart-final-suite.xml` 및 로그. 로컬 진단 스택·손상 DLL 보관본은 `.utmp/RestartInvestigation`에 있으며 Git 제외다.
- 임시 진단 정리: `RestartInvestigationProbe.cs`와 meta, `[RestartProbe]` 런타임 로그를 모두 제거했다. 진단 중 비활성 창에서도 Play를 진행하기 위해 바꾼 runInBackground는 프로세스 내 런타임 값이며 ProjectSettings의 원래 0은 유지했다. 원본 에디터가 이미 교착돼 실행 중인 메모리에는 제거된 진단 코드가 남을 수 있으므로 재실행 후 확인해야 한다. 씬·프리팹·Project Settings·패키지의 이번 요청 추가 변경은 없다.
- 사용자 확인 완료: 사용자가 Unity 종료·재실행 후 **“시작 오류 없고 Restart도 정상”**이라고 확인했다. 확인 절차는 컴파일 완료 → Console의 ShaderLibrary/Burst 시작 오류 확인 → MainTestScene Play → 적 접촉 사망 → Game Over의 Restart 반복이다. 정상 결과는 시작 오류가 없고 재진입 뒤 새 플레이어 이동이 가능한 상태다. 다시 정지하면 강제 종료 전에 정지 단계와 최신 Editor.log/네이티브 스택을 수집한다. Android 빌드·기기는 미검증이다.
- 독립 코드 리뷰: 실제 변경 파일의 검증·정리 프레임 분리, 기존 공개 API 보존, 중복 전환 가드, 동기/지연 예외 복구, 테스트 정리를 확인했고 추가 수정이 필요한 결함은 없었다. `git diff --check` 통과.
- 다음 우선순위: WorldWrap 폐기 상태를 유지한다. 별도 요청 시 기존 GiungTestScene Input Action Asset 참조 누락 16개 및 팝업 관련 2개 테스트 실패를 조사한다. 운영용 전체 카탈로그·번들·Android 빌드·기기 검증은 남아 있다.

## 2026-11-04 사용자 요청으로 무한 맵 구현 폐기

- 현재 결정: WorldWrap2D 기반 무한 맵을 전부 폐기했다. 아래 세 개의 관련 항목은 과거 조사 기록이며, 경계 래핑·시각 복제·World Bounds 설정을 다시 구현하거나 복원할 지침이 아니다.
- 시작 상태: `EnemyEXP`, HEAD `b5e1ad3`. 이전 작업의 WorldWrap 수정·새 전용 테스트와 기존 사용자 삭제 `WorldWrap2DRestartTests.cs`가 있었고, 새 사용자 파일 `Assets/_Recovery/0 (1).unity` 및 meta가 있었다. 기존 테스트 삭제는 유지했다.
- 제거: `Assets/0.Scripts/Game/WorldWrap2D.cs`와 meta, 새 WorldWrap 전용 테스트 3개와 meta. MainTestScene과 복구 씬에서는 WorldWrap2D MonoBehaviour YAML 블록과 플레이어의 컴포넌트 목록 항목만 제거했다. 복구 씬의 나머지 바이트와 meta, 기본 Ground 타일맵·배치, DNA 드롭·EXP 픽업 코드는 보존했다.
- 현재 동작: 맵 경계를 넘어도 반대편으로 순간이동하지 않고 주변 8장 타일맵을 생성하지 않는다. 맵 밖 이동 제한이나 새로운 경계 처리는 추가하지 않았다.
- 검증: Assets 전체 WorldWrap 이름·스크립트 GUID 참조 0건, 씬의 제거 컴포넌트 fileID 참조 0건, `git diff --check` 통과. 두 씬에서 해당 컴포넌트와 목록 항목을 제거한 것 외의 바이트·복구 씬 meta는 작업 직전과 동일함을 대조했다. 별도 전체 프로젝트 복사본에서 Unity 6000.5.0f1 컴파일 및 삭제 후 PlayMode **180개 중 162 통과/18 실패**, 생략 0, exit 2. 실패 18개는 아래 과거 조사에 명시한 기존 실패와 이름·원인이 동일하며 새 실패는 없다. 결과 파일은 `.utmp/g/removal-suite.xml`, 로그는 `removal-suite.log`(Git 제외). 이전의 5/5 통과 기록은 삭제 전 코드에 대한 기록으로 재사용하지 않는다.
- 다음 확인: Unity 임포트·컴파일 완료 → `Tools > Play From Title` 체크 시 해제 → `Assets/9.Scenes/MainTestScene.unity` 열기 → `Player Target` Inspector에서 WorldWrap2D/Missing Script가 없는지 확인 → Play. 기존 경계를 넘어도 순간이동·반복 맵 생성이 없어야 한다. 적 접촉으로 사망 후 Game Over의 Restart를 선택해 재진입과 정상 이동을 확인한다. 오류 시 Console 첫 오류와 Missing Script 위치를 확인한다. 실제 에디터 정지 재발 여부·화면·Android 빌드·기기는 별도 확인이 필요하다.

## [폐기된 구현 기록] 2026-11-04 사망 물리 콜백의 WorldWrap2D 정리 오류 조사

- 시작 상태: `EnemyEXP`, HEAD `b5e1ad3`. 사용자 변경으로 `Assets/Tests/PlayMode/WorldWrap2DRestartTests.cs`가 이미 삭제돼 있었으며 복원하지 않았다. 씬·프리팹·Project Settings·패키지는 원본에서 변경하지 않았다.
- 조사 대상: `dec1a74`(DNA 드롭/무한 맵), `b5e1ad3`(Restart 정지 대응). 후자에서 `UNITY_EDITOR`이면 Play 중에도 `DestroyImmediate`를 호출하도록 바뀌었다. 실제 `Logs/Editor.log`와 사용자 스택에서 `EnemyContactDamage → Player.TakeDamage → SetActive(false) → WorldWrap2D.OnDisable` 경로로 삭제가 거부됨을 확인했다. 실패 후에도 사본 목록·루트 참조를 비우므로 사망 시 정리되지 않은 맵이 남을 수 있다. DNA 드롭 코드가 이 오류를 발생시킨다는 근거는 없다.
- 수정: `WorldWrap2D.ClearVisualCopies`에서 루트를 비활성화하고 자식 8장을 루트와 함께 한 번에 정리한다. 에디터도 `Application.isPlaying`일 때 `Destroy`를 쓰며, Play 밖에서만 `DestroyImmediate`를 쓴다. 프레임당 사본 1장 생성과 경계 래핑은 유지한다.
- 회귀 테스트: 새 `WorldWrapPhysicsCleanupTests` 4개(실제 Collision2D 콜백 중 비활성화+timeScale 0, 같은 프레임 재활성화 3회, 생성 도중 파괴, 씬 언로드 3회). `WorldWrapContactDisableProbe`는 테스트 어셈블리에만 둔다. 기본 Assembly-CSharp 타입은 reflection으로 접근해 기존 asmdef 구조를 바꾸지 않는다. `WorldWrapGameRestartTests`는 실제 MainTestScene에서 사망/Restart 3회 경로를 검사한다.
- 실행 검증: 별도 최소 프로젝트, Unity 6000.5.0f1에서 수정 전 4개 중 1개가 사용자와 동일한 물리 콜백 DestroyImmediate 오류로 실패(3개 통과), 수정 후 **4/4 통과**, exit 0. 전체 프로젝트 복사본에서 새 테스트를 포함한 PlayMode 185개 실행: HEAD의 수정 전 WorldWrap 코드 **166 통과/19 실패**, 최종 코드 **167 통과/18 실패**, 생략 0, 양쪽 exit 2. 사라진 실패는 물리 콜백 테스트 1개뿐이며 나머지 18개는 이름·실패 원인이 동일하다. 최종 WorldWrap 테스트 **5/5 통과**(실제 MainTestScene의 TakeDamage/RestartGame 직접 호출 3회 포함). 실제 접촉·버튼 입력·화면은 수동 확인이 필요하다. 원본 사용자 에디터를 추가로 열지 않았고, 복사본 PlayFromTitle 초기화에만 batchmode guard를 넣었다.
- 검증 자료 위치(생성물, Git 제외): `.utmp/WrapCleanupCheck/before.xml`, `after.xml`, `.utmp/g/baseline-suite.xml`, `final-suite.xml` 및 각각의 로그. 영속 검증 요약은 이 항목의 수치와 실패 목록을 기준으로 한다. 초기 sandbox 검증은 라이선스 연결에서 진행되지 않아 별도 검증 프로세스를 경로 대조 후 종료했고, 권한 제한 밖의 독립 복사본에서 실제 테스트를 수행했다.
- 기존 실패 16개는 `Player requires an Input Action Asset reference.`로 GiungTestScene 진입 중 발생한다. `CombatPopupSceneTests`: `BossFightKeepsTimerPausedButCombatResumesAfterBack`, `BringingAnOpenPausePopupToFrontRestoresButtonFocus`, `KoreanLabelsRenderAndLevelContentRemainsEmpty`, `PausedPlayerDoesNotStartAutoAttackWhenCooldownReady`, `PopupRaycastBlocksWorldAndBackWorksWhilePaused`, `SceneButtonsFreezeCombatAndResumeOnlyAfterBothPopupsClose`, `SceneUnloadReleasesPauseAndSceneOwnedPopupInstances`. `GameResultFlowTests`: `ActualDeathNotificationIsSentOnce`, `ActualPlayerDeathReachesDefeat`, `DeathWinsWhenBothSignalsArriveInSameFrame`, `InvalidRestartPreservesResultAndAllowsRetry`, `RestartReturnsToGiungWithFreshStateAndOwnedManagers`, `RestoringLoadingRouteReachesMainWithPreparationReady`, `ReturnToTitleCleansPopupsPoolAndSound`, `VictoryClosesBothPopupsAndFocusesResult`. `PooledEnemySpawningTests.GiungSceneUsesPoolAndBossPauseReturnsNormalEnemies`.
- 기존 실패 나머지 2개: `PlayerStatusTests.ActualPausePrefabKeepsPauseThroughOptionsAndResumesOnBack`(CombatPausePopup 중복 등록), `PlayerStatusTests.KeyboardSubmitCanEnterAdjustAndLeaveOptionsWithoutLosingSelection`(MasterVolume 대신 OptionsButton 선택). 이번 맵 정리 수정 범위에서는 변경하지 않았다.
- 판단 제한: 이전 항목의 “지연 Destroy가 에디터 정지의 원인”이라는 설명은 실행 검증 없이 기록된 가설이었고 확정 사실로 사용하면 안 된다. 실제 씬의 사망/Restart 3회 자동 테스트는 수정 전에도 통과했으므로 사용자 환경의 에디터 전체 멈춤 자체는 이번 batchmode 검증에서 재현되지 않았다. 물리 오류의 원인과 수정 효과만 확정할 수 있다. 화면 이음새·Android 빌드·기기는 미검증이다.
- 독립 리뷰: 런타임 변경에 추가 지적 없음. 실제 씬 테스트의 종료 후 씬·영속 루트 정리 누락 1건을 수정하고 재검토 완료. 최종 테스트는 이 정리와 정상 정보 로그를 실패로 취급하던 검사 제거까지 반영한 코드로 실행했다. `git diff --check` 통과.
- 다음 우선순위: 아래 사용자 확인 절차로 원본 에디터의 실제 사망/Restart 정지 재발 여부 확인. 재발하면 클릭 전후 마지막 Console/Editor 로그와 에디터 스택을 수집해 씬 정리·새 맵 생성 중 정지 단계를 분리한다. 별도 작업으로 GiungTestScene 입력 참조 누락과 팝업 테스트 기존 실패 18개를 조사한다.
- 사용자 확인: Unity 컴파일 완료 → `Tools > Play From Title` 체크 시 해제 후 `Assets/9.Scenes/MainTestScene.unity` 열기 → Play → 적 접촉으로 실제 사망 → Game Over의 Restart 선택을 3회 반복 → Play 중지/재진입. 사망 프레임 다음에 `WorldWrap Visuals`가 제거되고, 재시작 뒤 루트 1개와 자식 8개가 다시 생성되며 플레이어 이동/경계 래핑이 동작해야 한다. Console에 물리 콜백 DestroyImmediate 오류가 없어야 한다. 재발 시 첫 오류와 마지막 정상 동작(사망/클릭/씬 로딩/새 플레이어 생성 중 어느 단계인지)을 기록한다. 정지 상태에서 사용자 에디터를 강제 종료하거나 미저장 씬을 덮어쓰지 않는다.
- 자동 테스트 메뉴: `Window > General > Test Runner > PlayMode`에서 `WorldWrapPhysicsCleanupTests`, `WorldWrapGameRestartTests` 실행. 환경에 따라 `Window > Test Runner`로 표시될 수 있다.

## [폐기된 구현 기록] 2026-11-04 WorldWrap2D 리스타트 시 Unity 정지

- 시작 상태: `EnemyEXP`, HEAD `dec1a74`. `main`에는 없고 이 브랜치의 `WorldWrap2D`만 해당.
- 원인: MainTestScene Ground 타일맵(약 2,262칸) Grid를 Play 진입 때 8장 동기 `Instantiate`하고, Play 종료·씬 리스타트 때 `Destroy` 지연 정리에 의존했다. 대용량 Tilemap 사본이 한 프레임에 생성·반쯤 남은 채로 정리되면 에디터까지 멈춘 것처럼 보인다.
- 수정: `Assets/0.Scripts/Game/WorldWrap2D.cs`에서 래핑은 즉시 유지하고, 시각 복제는 프레임당 1장 생성. 사본은 `DontSave`로 씬에 저장되지 않게 하며 `WorldWrap Visuals` 아래에 둔다. `OnDisable`에서 코루틴 중지 후 에디터는 `DestroyImmediate`로 즉시 제거. 사본의 Collider/Rigidbody/MonoBehaviour는 제거해 게임플레이 중복을 막는다.
- 회귀 테스트: `Assets/Tests/PlayMode/WorldWrap2DRestartTests.cs` — 비활성/재활성/파괴 후 `(Wrap …)` 사본과 `WorldWrap Visuals`가 남지 않는지 검사.
- 검증: 코드 정적 확인. batchmode Unity는 라이선스 없음(exit 198)으로 테스트 실행 실패. 에디터 Play 확인은 사용자 환경에서 필요. 실행한 통과 테스트 0개.
- 확인 순서: EnemyEXP에서 Unity로 MainTestScene → Play → 가장자리 통과해 반대편·바닥 반복 확인 → Game Over 리스타트 또는 Play 중지 후 다시 Play. Hierarchy에 `Ground (Wrap …)` / `WorldWrap Visuals`가 Play 종료 후 남지 않고, Unity가 멈추지 않으면 정상. 오류 시 Console 첫 줄과 Player Target의 WorldWrap2D Source Tilemap 연결을 본다.

## [폐기된 구현 기록] 2026-11-04 MainTestScene 반복 맵과 플레이어 경계 래핑

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
- 완료: 8방향 좀비 들개. 앞다리와 뒷다리가 겹쳐 한 쌍처럼 보이던 64px 그림을 버리고, 개 골격으로 앞·뒷다리가 구분되는 92px 그림으로 교체했다. 회전, idle, run 각 8프레임. 클립·컨트롤러·프리팹 GUID는 유지하고 PNG와 스프라이트 rect만 바꿨다. 체력 3, 접촉 피해 10, 이동 속도 3. 스폰 타임라인에는 넣지 않음.
- 검증: 남·동 회전과 남쪽 idle, 남·동 run에서 앞다리와 뒷다리가 따로 보이는지 확인. PNG 136장 모두 92×92, rect 92인 메타 136개. Unity 테스트 0개. 에디터 재생은 확인하지 않음.
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
