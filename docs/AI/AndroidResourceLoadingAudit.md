# Android 리소스 배포 전환 조사

조사일: 2026-11-17. 브랜치: `adressable-resources-loading`. HEAD: `90c00c0`. 시작 시 `git status --short`는 빈 출력이었다. 이전 대화에서 관찰한 `main`과 달라졌으므로 현재 조회값을 기준으로 한다.

이 보고서와 CSV/JSON은 **다운로드 기반 API 구현 전 조사 시점의 스냅샷**이다. 이후 추가된 파일·사용자 씬 삭제·검증 결과는 Handoff의 최신 항목을 기준으로 한다. 아래 미실행/변경 없음 표기는 초기 조사 단계에 관한 기록이다.

## 범위와 측정 의미

- 목적: Android 최초 설치 크기를 줄이고 첫 실행 다운로드와 출시 후 리소스 패치를 지원하기 위한 현재 포함 경로 조사.
- 런타임 코드·씬·프리팹·meta·Project Settings·패키지 변경 없음. GiungScene 내용은 읽거나 검증하지 않았다.
- Unity `6000.5.0f1`, Addressables `2.9.1`. 사용 가능한 Unity MCP 도구가 없어 파일 기반으로 조사했다. 에디터 실행, 임포트, 컴파일, PlayMode, Android 빌드·기기 실행은 하지 않았다.
- **아래 수치는 원본 파일 크기이며 APK/AAB 다운로드 크기·설치 후 저장 공간·GPU/실행 메모리가 아니다.** MiB는 1,048,576바이트다. 압축 PNG와 YAML 에셋 크기를 Android 패키지 절감량으로 환산하지 않는다.
- 영속 근거: [개별 파일 CSV](ResourceLoadingInventory.csv), [요약 JSON](ResourceLoadingInventory.json). 임시 검증 결과에 의존하지 않는다.

## 집계 결과

| 범위 | 파일 수 | 원본 MiB | 해석 |
|---|---:|---:|---|
| 조사한 Assets 전체 | 3,327 | 75.87 | meta와 GiungScene 제외. 이 전체가 앱에 포함된다는 뜻이 아님 |
| Assets/Plugins | 264 | 25.24 | 공급업체·예제 포함. 폴더 전체 삭제나 원격 이전 대상으로 간주하지 않음 |
| Assets/2.Resources | 2,221 | 22.02 | 정확한 이름이 Resources가 아니므로 자동 포함 폴더가 아님 |
| Assets/Resources | 505 | 9.42 | 런타임 Resources 루트 |
| Assets/TextMesh Pro/Resources | 8 | 2.18 | 별도의 런타임 Resources 루트 |
| Assets/UI | 6 | 8.62 | 주로 폰트. 씬 참조에 따라 포함되는 후보 |
| Assets/Resources/LightSkills | 283 | 8.61 | Fx 7.08 MiB, Blaze 1.46 MiB 등이 포함됨 |

### 씬·Resources에서 시작한 GUID 참조 후보

현재 `ProjectSettings/EditorBuildSettings.asset`에는 TitleScene, LoadScene, LobbyScene, MainScene이 활성 상태다. 다른 Build Profile의 적용 여부는 에디터에서 확인하지 않았다. 시작·로딩·로비는 진입 경로 확인을 위해 읽기만 했으며, 씬 변경·플레이 검증 대상은 MainScene으로 제한한다.

| 시작점 | GUID 도달 파일 수 | 원본 MiB |
|---|---:|---:|
| TitleScene | 4 | 3.87 |
| LoadScene | 9 | 8.13 |
| LobbyScene | 8 | 8.22 |
| MainScene | 973 | 20.04 |
| 런타임 Resources 및 그 GUID 참조 | 847 | 13.14 |
| 위 전체 중복 제거 합집합 | 1,826 | 34.68 |
| 시작·로딩·로비 합집합 | 18 | 12.13 |

각 행은 서로 겹치므로 더하면 안 된다. MainScene에서만 도달하고 시작·로딩·로비·Resources에서는 도달하지 않은 후보는 962개, 원본 9.41 MiB다. 이는 의존성 분리 검토 대상이지 그대로 원격 이전하거나 절감할 수 있다는 보장이 아니다.

MainScene과 Resources가 함께 가리키는 후보는 8개, 원본 2.54 MiB다. 이는 **참조 경로 중첩**이며 현재 빌드의 중복 복사량을 측정한 결과가 아니다. Addressables로 이전할 때 Analyze/Build Layout으로 중복 여부를 검증해야 한다.

### 재현 가능한 집계 규칙

1. Assets의 파일을 열거한다. GiungScene 파일·meta의 내용과 크기 집계는 제외하고, 나머지 meta의 `guid`로 경로 사전을 만든다. 디렉터리 meta는 에셋 파일 수에 더하지 않는다.
2. `.meta`를 제외한 실제 파일 길이를 합산한다. Editor 폴더 아래를 제외하고, 경로 구성요소가 정확히 `Resources`인 파일들을 런타임 Resources 시작점으로 분류한다.
3. 위 4개 씬과 런타임 Resources에서 `.unity/.prefab/.asset/.mat/.controller/.overridecontroller/.anim/.spriteatlas/.shader/.shadervariants/.rendertexture/.playable/.mask/.lighting/.scenetemplate`의 텍스트 및 도달한 파일의 importer meta에서 `guid: <32자리>`를 따라 방문한다. 파일 경로별 한 번만 합산한다. 자기 자신의 meta GUID는 제외한다.
4. PNG는 헤더에서 가로·세로만 읽는다. 파일 픽셀 크기는 실제 Android 임포트 결과나 메모리 크기와 같지 않다.
5. CSV는 전체 3,327개 원본 파일에 대해 Resources 자체 여부와 시작점별 도달 여부를 기록한다. JSON은 집계와 첫 번째 코드 트리의 직접 Resources 호출 위치를 기록한다.

한계: 이 방식은 Unity AssetDatabase 의존성 분석이 아니다. importer의 편집기 전용 참조·원본 C#도 후보에 들어가고, 빌드 시 제거되거나 변환될 수 있다. 패키지 에셋, Graphics/PlayerSettings의 추가 루트, 코드 기반 탐색, 자동 생성 아틀라스, 네이티브 플러그인, 활성 Build Profile은 완전히 모델링하지 않는다. 후보에서 빠졌다는 이유로 미사용이라고 판단하거나 삭제하면 안 된다. 원본 PNG에서 Git LFS 포인터는 발견하지 않았다.

## 현재 진입·로딩 계약

- 실제 버튼 경로는 `UI_TitleScene.OnTouchStart → LoadSceneThroughLoading(LobbyScene) → LoadScene → LobbyScene`, 로비 시작 버튼은 다시 `LoadSceneThroughLoading(MainScene)`이다. CoreInitialization의 단순한 타이틀→로딩→게임 설명보다 실제 경로에 로비가 추가돼 있다.
- `LoadScene` 설정은 `preloadLabel` 빈 값, `requireSkillData = false`. `Managers.PrepareGame`은 이때 Addressables 사전 로딩을 건너뛴다.
- `AddressableAssetSettings.asset`: `m_BuildRemoteCatalog: 0`, `Remote.LoadPath: <undefined>`. 등록 그룹은 Default Local Group 하나이며 `m_SerializeEntries: []`다.
- `ResourceManager.Load<T>`는 이미 읽은 캐시만 조회한다. 이름과 달리 디스크/네트워크 로딩을 시작하지 않는다. `PreloadAsync`는 라벨 에셋을 메모리에 로드하는 기존 경로이며 다운로드 전용 API가 아니다.
- `Assets/0.Scripts`에서 `Resources.Load/LoadAll<T>` 직접 호출 20곳, 파일 11개를 확인했다. 위치는 JSON의 `resourcesLoadSites`에 기록했다. 다른 패키지·플러그인의 호출 수는 이 수에 포함하지 않았다.
- 실제 전투 표는 `Resources/Data/CombatSkillTable.json`, 성장 카드 표는 `Resources/Data/PlayerStatTable.json`이다. `DataManager.Init`이 읽는 별도 `SkillData`와 혼동하면 안 된다. `requireSkillData`를 켜는 것만으로 실제 전투 표 준비를 검증할 수 없다.
- `LightSkillVisuals`는 Texture·프레임 JSON을 읽은 뒤 `Sprite.Create`를 수행하며 정적 사전에 보관한다. `CombatSkillTable`, `PlayerStatTable`도 정적 캐시다. 패치 버전 변경 시 기존 캐시 무효화·소유권 정리가 필요하다.
- 현재 `Managers.Clear`는 씬/풀/UI/사운드를 정리하고 Resource 캐시는 보존한다. `ResourceManager.Clear`는 진행 중 요청과 에셋 핸들을 해제하며, 영속 Managers 파괴 시 호출된다. 디스크 다운로드 캐시와는 별개다.

## 우선순위별 발견

### R1 — 원격 패치 진입 경로가 없음

- 심각도: 높음(출시 목표 기준). 확신: 확인됨. 현재 로컬 게임 버그라고 판정한 것은 아니다.
- 근거: 빈 Addressables 그룹, 원격 카탈로그 꺼짐, 미지정 Remote.LoadPath, 다운로드 용량 조회·다운로드·패치 버전 선택 코드 없음.
- 영향: 현재 빌드를 배포해도 첫 실행 원격 다운로드·리소스 단독 패치를 제공하지 못한다.
- 조치: 기존 Managers/ResourceManager를 확장하고 앱 호환 버전 선택과 다운로드 단계를 메모리 로딩 앞에 둔다. 규모: 큼.
- 검증: Android 신규 설치/재실행/콘텐츠 패치/구버전 호환/네트워크 실패를 실제 원격 번들로 확인.

### R2 — 앱 포함 경로와 원격 참조 분리가 필요함

- 심각도: 높음(설치 용량 목표 기준). 확신: 참조 존재는 확인됨, 실제 절감·중복 크기는 미확인.
- 근거: MainScene의 HUD·적 프리팹 직접 참조, 런타임 Resources 513개, 정적 GUID 후보 집계.
- 구체 경로: `MainScene → BeastZombie.prefab → BossAttackHitVfx.mat → AllIn1SpriteShader/Demo/Textures/coals.png`. Demo 폴더 안에도 사용 후보가 있으므로 통째로 제거하면 안 된다.
- 영향: 원격 등록만 하고 로컬 씬/Resources 참조를 남기면 설치 크기 유지 또는 중복 포함 가능성이 있다.
- 조치: 작은 시범 에셋부터 Resources 밖으로 GUID를 보존해 이동하고 참조·공유 의존성을 분리한다. MainScene 전면 원격 이전은 별도 단계로 검토한다. 규모: 큼.
- 검증: Addressables Analyze의 Scene/Resources 중복 검사와 Build Layout, Android BuildReport로 실제 포함과 절감량 비교.

### R3 — 시작 화면 공통 폰트와 대형 텍스처가 용량 하한에 영향

- 심각도: 중간. 확신: 원본·참조·임포트 설정은 확인됨, Android 비용은 미확인.
- 근거: Title_Background는 1920×1080/원본 3.85 MiB. LoadScene/LobbyScene도 원본 6.12 MiB `NanumGothic Combat SDF.asset`과 1.96 MiB TTF를 참조하는 후보이며, 해당 폰트는 동적 아틀라스(`m_AtlasPopulationMode: 1`)다. 한글 표시를 임의로 정적 축소하면 누락 위험이 있다.
- 예: `Hellfire.png`는 원본 약 0.65 MiB지만 픽셀은 1310×1475다. 샘플 meta는 Point·mipmap 없음·기본 무압축이며 Android override도 꺼져 있다. 실제 포맷과 메모리는 빌드/기기로 확인해야 한다.
- 조치: 다운로드 UI에 필요한 글자 범위와 게임 폰트를 구분할 수 있는지 검토하고, 이펙트·UI Android 압축을 시각 비교한다. 이번 조사에서는 변경하지 않았다. 규모: 중간.
- 검증: 한글·숫자·오류 안내 글리프, 투명 경계·픽셀 효과 품질, 실기기 메모리 및 번들 크기 비교.

### R4 — 데이터·효과 캐시가 패치 세대와 연결돼 있지 않음

- 심각도: 중간. 확신: 정적 캐시·생성 경로는 확인됨. 패치 중 불일치는 아직 재현 대상이 아니다.
- 근거: `CombatSkillTable.EnsureLoaded`, `PlayerStatTable.LoadCards`, `LightSkillVisuals.Baked/Frames/MaterialFor`.
- 영향: 카탈로그만 바꿔도 이미 만들어진 전투 표와 Sprite가 자동 교체되지 않는다. 동일 주소의 위치 변경은 기존 ResourceManager 충돌 규칙과도 조정이 필요하다.
- 조치: 전투 중 패치 적용 금지, 앱 재시작 경계에서 릴리스 고정, 캐시·생성 Sprite/Material·핸들 수명 일치. 규모: 중간~큼.
- 검증: 같은 앱으로 리소스 v1→v2 재시작 후 수치와 효과 모두 v2, 재시작·타이틀 복귀 반복 시 해제 확인.

## 유지할 기반

- 기존 ResourceManager의 동일 요청 공유·실패 재시도·Clear 세대 방어·핸들 소유권 구조를 확장할 수 있다. 과거 테스트 결과를 이번 검증으로 재사용하지 않는다.
- LoadScene 목적지 예약과 실패 시 전환 차단, Managers의 영속 준비 작업 소유권을 재사용한다.
- 새로운 전역 다운로드 매니저·싱글톤·이벤트 프레임워크는 필요하지 않다.

## 다음 순서와 직접 확인

1. [전환 설계 초안](../superpowers/specs/2026-11-17-android-remote-content-design.md)을 검토한다. 구현·호스팅 선택·배포는 아직 하지 않았다.
2. Unity에서 Play 종료 후 `Window > Asset Management > Addressables > Groups`를 열어 등록 에셋이 없는지, Settings에서 Build Remote Catalog가 꺼져 있는지 **읽기만** 확인한다. 정상은 이번 조사와 일치하는 현재 미설정 상태다.
3. Project의 실제 `Assets/Resources`와 `Assets/2.Resources`를 구분하고 CSV의 경로·바이트·MainScene 참조 후보를 확인한다. 실제 패키지 포함 여부는 다음 단계의 Android BuildReport와 비교한다.
4. 씬 확인이 필요하면 `Tools > Play From Title`을 해제하고 MainScene을 열어 Inspector의 기존 HUD/적 프리팹 연결을 읽기만 확인한다. 이번 문서 작성으로 연결이나 실행 동작은 달라지지 않아야 한다.
5. 경로나 수치가 다르면 먼저 현재 브랜치/HEAD/저장되지 않은 변경, Build Profile override, 해당 meta GUID를 확인한다. Unity 오류는 Console 첫 오류를 보존하고, 기존 오류와 새 변경을 구분한다. GiungScene은 확인 대상이 아니다.

실행 테스트 0개. Android SDK/빌드 지원 설치 여부, 카탈로그·번들·AAB·기기·CDN·실제 설치 크기는 미검증이다.
