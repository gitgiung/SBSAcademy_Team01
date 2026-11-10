# 작업 인수인계

## 2026-11-10 강공격 카드와 공통 시전 연결

- 기준은 `main`의 `13e2e0a`. 시작 시 태양 검 JSON·검증 도구·인수인계 변경이 있었으며 보존했다. 기존 범위 판정을 사용하는 첫 적용 범위로 강공격 13종을 약공격과 함께 추첨한다. 화염 돌진(20002)과 화염 십자(20012)는 전용 이동·십자 판정 연결 전까지 후보에서 제외했다. 공통 시전은 범위 피해를 적용하며 밀치기는 후속으로 남겼다. 밀치기 데이터가 있는 카드에는 이 제한을 표시한다.
- 강공격 카드 선택 시 보유·강화 후 `C_Attack`에 즉시 장착한다. 상태창에서 지원하는 보유 강공격을 교체·해제할 수 있고 약공격 장착은 독립적이다. 보유 슬롯이 가득 차면 강화 가능한 보유 스킬만 후보에 남고 최대 스택은 제외한다.
- `C_Attack` 또는 Q로 차징을 시작하면 배포 JSON의 해당 MP를 한 번 차감한다. 표의 `CastTime`이 최대 차징 시간이며 기존 씬의 중간 단계 시간 비율과 단계별 피해 증가량을 사용한다. 해제하면 공통 범위 타격에 차징·보유 스택 배율을 곱한다. 차징 중 이동·자동 공격을 멈추고 성공한 방어·회피 또는 슈퍼아머 없는 피격은 취소한다. 씬·프리팹은 수정하지 않았다.
- 검증: 설치 Unity 6000.5.0f1 Roslyn으로 `Assembly-CSharp` 컴파일 성공, 오류 0, 기존 경고 3개. `CombatSkillTableValidation` **22/22**, `PlayerLoadoutValidation` **5/5**, `LevelUpMixedDraftValidation` **5/5**. Unity PlayMode와 실제 에디터 입력·렌더링·기기는 미검증이다. 배포 JSON을 대상으로 후보·비용을 검증했다. 현재 엑셀에서 화염 망치의 `CostType`·`CastType`·`AreaType`을 읽으면 빈 값이므로 표를 다시 내보내기 전에 데이터 차이를 확인해야 한다.
- 직접 확인: `Tools > Play From Title` 해제 → `MainScene` Play → `CombatUI`의 `CombatPopupController` 메뉴에서 `테스트/경험치 13 획득`을 실행 → `액티브 C` 카드가 나오면 선택한다. 적 가까이에서 `C_Attack` 버튼이나 Q를 길게 눌렀다가 뗀다. 누를 때 MP가 한 번 줄고 차징 표시가 바뀌며, 뗄 때 공격 범위 안 적의 체력이 줄어야 한다. 이상하면 Console 첫 오류, 상태창 강공격의 장착 표시, 현재 MP, Player의 `Heavy Attack Button` 및 차징 `Configured`를 확인한다. 다음은 직접 플레이 확인과 전용 이동·십자·밀치기 연결이다.

## 2026-11-10 태양 검 패시브 쿨타임 동기화

- 사용자 지정에 따라 배포 JSON의 태양 검(10007) `PassiveCooldown`을 엑셀과 같은 2초로 수정했다. 전투 표 회귀 검증도 2초 및 보유 패시브 활성화를 기대하도록 갱신했다.
- `tools/CombatSkillTableValidation/Run.ps1`에서 JSON 수정 전 18/19, 수정 후 **19/19 통과**. Unity 플레이는 실행하지 않았다. 다음 스킬 UI 작업은 상세의 MP·쿨다운 표시다.
- 직접 확인은 Unity에서 `Assets > Refresh` 후 Console 첫 오류 확인, `Tools > Play From Title` 해제 후 `MainScene` Play 순서다. 기본 레벨업 추첨에서 태양 검이 나오지 않는 상태가 정상이다. 값은 배포 JSON의 ID 10007 행과 `Document/SkillDatatable.xlsx` 약공격 시트 AH9에서 확인한다.

## 2026-11-10 현재 main의 전투 스킬 연결 점검

- 기준은 `main`의 `13e2e0a`이며 시작 작업 트리는 깨끗했다. 전투 JSON에 약공격 21개·강공격 15개·전략 10개가 있다. 약공격은 레벨업 카드 획득 → 상태창의 보유 슬롯에서 장착·해제 → 약공격 버튼 시전으로 연결되어 있다. 보유 패시브는 장착과 별개로 발동하며, 같은 스킬을 다시 고르면 액티브·패시브 피해에 스택 배율이 적용된다.
- 강공격 버튼은 차징·방향 표시·해제 이벤트까지 연결되어 있다. 해제 이벤트를 받아 실제 강공격을 시전하는 코드는 아직 없다. 강공격을 보유하면 지원하는 도형에 한해 패시브로 발동하지만 기본 레벨업 후보는 약공격만 제공한다. 화염 십자(20012)의 `Cross` 도형은 미지원이다. 전략 버튼 `Skill`은 물의 방패(30009)에 연결되어 있으며 장착한 전략 스킬을 공통으로 실행하는 경로는 아직 없다.
- 남은 연결은 상태창 스킬 상세의 MP·쿨다운 표시, 강공격 해제 후 실제 공격 실행, 전략 스킬 공통 실행, 레벨업 스킬·스탯 혼합 지급이다. 상태창 슬롯을 조회 전용으로 설명한 11월 5일 기록과 달리 현재 약공격 슬롯은 클릭으로 장착·해제된다. 현재 전투 표는 `Resources` JSON을 사용하며 `LoadScene`의 Addressables `SkillData` 필수 설정은 꺼져 있다.
- 이번 검증은 `tools/CombatSkillTableValidation/Run.ps1` **17/19 통과**. 실패 2개는 태양 검(10007)의 `PassiveCooldown`이 엑셀 2초, 배포 JSON 0초인 데서 발생한다. 다음 데이터 작업 때 사용할 값을 정한 뒤 두 파일을 동기화한다. Unity 컴파일·PlayMode·Android는 이번에 실행하지 않았다.
- 직접 확인은 `Tools > Play From Title` 해제 → `MainScene` Play → `CombatUI`의 `CombatPopupController` 메뉴에서 `테스트/경험치 13 획득` → 약공격 카드 선택 순서다. 화면 안에 적이 있으면 미장착 상태에서도 MP 소모 없이 패시브가 발동한다. 일시정지 상태창에서 획득한 약공격 슬롯을 눌러 장착한 뒤 전투로 돌아와 약공격 버튼을 누르면 MP를 소모하고 시전한다. 이상하면 Console 첫 오류, 장착 표시, 적의 `Enemy` 태그와 사거리, 배포 JSON의 해당 스킬 행을 확인한다.

## 2026-11-09 화염구 폭발과 경험치 구슬 흔들림

- 화염구(10004)와 작열 구체(10015)는 적에게 맞으면 기존 원형 폭발 그림으로 터진다. 맞은 대상과 주변은 그 스킬의 기존 피해를 한 번씩 받는다. 표에 폭발 반경이 없어 가로 폭의 두 배를 임시 반경으로 쓴다. 화염구 1.8, 작열 구체 2.2. 탄환·화살·산탄은 그대로 한 점 피격이다. 사거리 끝에서 저절로 터지지는 않는다.
- 경험치 구슬은 추적 위치와 별도로 좌우 0.07, 크기 약 7%로 흔들린다. 흡수 계산은 흔들림 전 위치를 쓴다.
- 검증: `tools/ExperienceAttractionValidation/Run.ps1` **10/10**. Unity Play·PlayMode는 실행하지 않았다.
- 직접 확인: Unity 임포트 후 Console 오류 없음 → `Tools > Play From Title` 끄기 → MainScene Play. 화염구·작열 구체를 장착해 적이 뭉친 곳에 맞추면 폭발이 보이고 맞은 적과 바로 옆 적이 같이 피해를 입는다. 불꽃 탄환은 폭발 없이 한 명만 맞는다. 바닥에 떨어진 DNA는 가만히 있어도 좌우로 흔들리고 살짝 커졌다 작아진다. 이상하면 Console 첫 오류, 스킬 ID 10004·10015, DNA 프리팹의 스케일 1.5를 확인한다.
- 노션: https://app.notion.com/p/413283dd2479817b9552cf96224dec6b 개발, 플레이어·성장·밸런싱, 중간, 2026-11-09, 할당 비움, 커밋 c612021.

## 2026-11-09 전투 피드백 — 팝업·등장·DNA·연사·산탄·보호막·보스

- 팝업: 전투 팝업의 회색 바깥을 클릭·터치하면 뒤로가기와 같이 닫힌다. ESC도 최상단 팝업의 뒤로가기다. 설정 화면이 열려 있으면 ESC는 상태창으로 한 단계만 돌아간다. 자동 레벨업은 기존처럼 선택 전에는 닫히지 않는다.
- 보스 등장: 1안. 큰 등장 폭발과 모습 공개는 유지하고, 등장 순간의 원형 피해는 넣지 않는다.
- DNA: 한 번 흡수 범위에 들어오면 플레이어가 벗어나도 따라온다. 프리팹 속도 1→10, 가속도 3→18. 플레이어 이동 속도는 7이다.
- 화염 연사(10013): 호 모양 베기 대신 정면으로 4발을 짧게 연사한다.
- 불씨 산탄(10010): 4발을 한 번에 흩뿌리고, 이동 잠금은 0.15초만 남긴다.
- 보호막: 모바일 `Skill` 버튼이 표의 물의 방패(최대 체력×0.3, 8초, 쿨타임 6초)를 건다. 유지 중 캐릭터 위에 남은 보호막 체력 막대가 보이고, 피해는 보호막부터 깎인다. 일시정지 스탯의 방어막 체력도 같은 값이다.
- 보스 추격: 근거리와 원거리 돌진 사이에서 공격 없이 2.5초(Inspector `Chase Dash Delay`) 추격하면 돌진한다.
- 2페이즈 양손 공격: 피해 시점을 클립 후반(회복 자세)에서 내려찍는 중간(정규화 시간 0.5)으로 옮겼다. 보이는 내리찍기와 패링 판정이 맞는다.
- 사망: 패배 팝업이 열리면 떠 있던 피해 숫자를 지우고 새로 띄우지 않는다.
- 검증: `tools/ExperienceAttractionValidation/Run.ps1` **10/10**. Unity 컴파일·Play·PlayMode는 실행하지 않았다.
- 직접 확인: 아래 사용자 안내를 따른다.
- 노션: `팀프로젝트 - 1팀 / 작업`에 동일 제목으로 완료 기록. https://app.notion.com/p/413283dd247981fb9fcffdf82af2f907 개발, UI·보스·플레이어, 중간, 시작일·완료일 2026-11-09. 할당 대상은 비움. 커밋 `d924514`.

## 2026-11-09 브랜치 병합

- `MapObject`에 `main`(`14cd586`, 11/09_1)과 `passive-inspection`(`9f16782`)을 병합했다. 원격에는 푸시하지 않았다.
- 씬 파일은 main의 이름 `MainScene`, `GiungScene`을 유지했다. `MainScene`에는 MapObject의 보스 HP 페이드(`bossHealthFadeDuration` 0.5)와 `SprintCooldownUI`를 합쳤다. `GiungScene`은 main 내용을 유지했다. 자동 병합이 MapObject의 `MainTestScene` 변경을 `GiungScene`에 붙이려 해서 그 결합은 버리고, `MainTestScene`과 `MainScene`만 다시 맞췄다.
- `NanumGothic Combat SDF`는 아틀라스 좌표가 서로 달라 main 쪽 파일을 유지했다.
- `BeastZombie`는 이름과 `Enemy.Runtime`을 유지하고, MapObject에서 나중에 저장한 체력 1000, 공격력 30, 원형 반경 14, 등장 폭발 최소 크기 200, 등장 폭발 시간 4를 남겼다. main의 체력 500·공격력 15는 넣지 않았다.
- `ZombieHound` 접촉 피해는 5, 스크립트 이름은 `Enemy.Runtime`이다.
- 이번 병합의 Unity 컴파일·테스트는 실행하지 않았다. 씬 이름이 `MainScene`으로 바뀌었으므로, 열려 있던 `MainTestScene`은 저장하지 말고 `MainScene`을 다시 연다.

## 2026-11-09 레벨업 스탯 혼합 — 구조만 추가(미연결)

- 요청: 레벨업 3장에 스킬+스탯을 섞고(C), 스탯은 Uncommon~Mythic 차등. OOP·함수형으로 **나중에 꽂을 구조만** 추가. 팝업·전투·증강 실지급은 연결하지 않음.
- 추가: `Assets/0.Scripts/Game/UI/LevelUp/` — `ILevelUpReward`, `LevelUpSkillChoice`, `LevelUpStatChoice`/`LevelUpStatKind`, `LevelUpStatBonusTable`(temporary 피해 가산), `LevelUpMixedDraft.DrawChoices`(순수, 기존 `LevelUpSkillDraft` 재사용), `ILevelUpRewardApplier`/`NullLevelUpRewardApplier`.
- 임시 가산(확정 아님): Uncommon 0.05 / Rare 0.08 / Epic 0.12 / Legendary 0.18 / Mythic 0.25. Common·Fusion 없음. 기본 혼합 비율 상수 스킬 0.75·스탯 0.25.
- 건드리지 않음: `CombatPopupController.PrepareLevelUpChoices`, `LevelUpSkillReward`, `Player` 피해식, 씬·프리팹. 기존 레벨업은 스킬만 그대로.
- 검증: `tools/LevelUpMixedDraftValidation/Run.ps1` **5/5**. Unity Play·PlayMode 회귀 미실행.
- 직접 확인: Unity 임포트 후 Console 컴파일 오류 없음 → MainScene 레벨업은 **이전과 같이 스킬 카드만**. 스탯 카드·증강·피해 증가는 아직 없어야 정상. 이상하면 Console 첫 오류와 `LevelUp` 폴더 스크립트 Missing Script를 확인.
- 다음 연결 시: `PrepareLevelUpChoices` → `LevelUpMixedDraft.DrawChoices`, 선택 시 실 `ILevelUpRewardApplier`, 피해에 배율 한 지점.
- 노션: `팀프로젝트 - 1팀 / 작업`에 동일 제목으로 완료 기록. https://app.notion.com/p/413283dd2479813ba2fad3aae626733b 개발, 성장·플레이어, 중간, 시작일·완료일 2026-11-09. 할당 대상은 비움. 커밋 `33da77b`.

## 2026-11-09 레벨업 동일 스킬 중복 강화(1.2·1.5배)

- 요청: 레벨업 보상에서 같은 스킬을 다시 고르면 피해가 강해지게 한다.
- 규칙: 1스택=×1, 2스택=×1.2, 3스택=×1.5. 최대 3스택. 슬롯을 추가하지 않고 기존 보유 칸의 스택만 올린다. 최대 스택인 스킬만 추첨에서 제외한다. 빈 슬롯이 있으면 신규+강화, 없으면 강화만, 강화 여지도 없으면 슬롯 가득 참 안내.
- 적용: 장착 약공격 버튼 시전(`CastLightSkill`)과 보유 패시브 자동발사(`TickOwnedPassives`) 피해에 스택 배율을 곱한다. 패시브의 `PassiveScale`(기본 0.2)은 그대로 두고 그 결과에 스택 배율을 추가한다.
- 코드: `PlayerLoadout`에 `attackSkillStacks`, `TryAddOrUpgradeAttackSkill`, `GetAttackSkillStack`, `GetAttackSkillDamageMultiplier`, `IsAttackSkillAtMaxStack`, `DamageMultiplierForStack`. `LevelUpSkillReward.TryAcquire`는 업그레이드 경로를 쓴다. `CombatPopupController.PrepareLevelUpChoices`는 최대 스택만 제외하고, 강화 카드 설명에 `강화 1→2 (피해 ×1.2)` 문구를 붙인다.
- 검증: `tools/PlayerLoadoutValidation/Run.ps1` **5/5**. `tools/CombatSkillTableValidation/Run.ps1`에서 이번 스택 관련 5건(획득·중복 강화·슬롯 가득 시 강화·제외·최대 스택 제외)은 통과. 같은 실행의 다른 2건(`PassiveCooldown` 엑셀·배포 JSON 불일치)은 이전 패시브 쿨타임 작업 잔여로 보이며 이번 범위에서 고치지 않음. Unity 에디터 Play·전체 PlayMode 회귀는 실행하지 않음.
- 직접 확인: MainScene Play → 경험치로 레벨업 → 같은 약공격을 두 번·세 번 고른다. 두 번째 카드에 강화 문구가 보이고, 슬롯 수는 늘 않으며, 버튼 시전과 보유 패시브 피해가 커져야 한다. 세 번 강화한 뒤에는 그 스킬이 카드에 나오지 않는다. 이상하면 Console 첫 오류와 일시정지 상태창의 보유 칸 수를 확인한다.
- 노션: 팀 작업 DB에 동일 제목으로 기록함.
- 다음: 스킬 자세히 보기의 MP·쿨다운, 로딩·로비 배경 원본 아트, 탭·아이콘 화면 연결.

## 2026-11-09 타이틀·로딩·로비 씬 UI 연결

- 기준: `Document/씬 별 UI.pptx` 2~7번 슬라이드(타이틀·로딩·로비). 와이어프레임(2·4·6번)의 구성을 따르고, 시안 이미지(3·5·7번)는 모양 참고로만 썼다. 로딩·로비 시안은 글자·버튼 프레임이 그림에 구워져 있어 배경으로 쓸 수 없다.
- 흐름: `TitleScene`(터치) → `LoadScene` → `LobbyScene`(게임 시작) → `LoadScene` → `MainScene`. `SceneManagerEx.LoadSceneThroughLoading(목적지)`가 목적지를 예약하고 로딩 씬이 준비 후 한 번만 꺼내 쓴다. 예약이 없으면 `MainScene`으로 가므로 `GameManager`의 재시작(`LoadScene` 경유)은 그대로다. `Define.EScene`에 `LobbyScene`을 맨 끝에 추가했고 기존 값·빌드 목록 순서는 보존했다(로비는 목록 맨 끝, GUID `a7ae3e61…`).
- 타이틀: `Assets/2.Resources/UI/씬 UI/Title_Background.png`(시안 이미지를 1920×1080으로 변환, 로고·GAME START·저작권이 그림에 포함)을 전체 배경으로 깔았다. 임시 "Title Scene" 문구와 "Touch to start" 문구는 제거했다. `StartTouchArea` 버튼은 그대로다.
- 로딩: 팁 문구(기획서 예시 그대로)와 우하단 회전 고리+좀비 소녀(`UI_LoadScene`). 배경은 **임시 그라데이션**이다. 기존 "Loading . . ." 문구는 시안에 없어 제거했다.
- 로비(`UI_LobbyScene`): 좌상단 프로필(초상·이름·골드), 우상단 설정·우편·알림, 중앙 캐릭터(`Pretty_Magical_Girl/Idle/rotations/south`), 게임 시작, 하단 탭(상점·장비·메인·업그레이드·도감, 메인 강조). 배경은 **임시 그라데이션**이다. 이름은 "플레이어 이름", 골드는 0 고정이며 `SetProfile(이름, 골드)`로 나중에 연결한다. **동작하는 것은 게임 시작뿐**이다. 탭·아이콘은 대상 화면이 정해지지 않아 눌러도 아무 일도 없다.
- 미정으로 둠: 탭·아이콘의 화면(상점·장비·업그레이드·도감·설정·우편·알림), 프로필·골드 데이터, 로딩 팁 목록, 로딩·로비의 실제 배경 아트(글자·UI가 없는 깨끗한 원본이 필요하다).
- 씬은 Unity에서 한 번 생성했다. 이후에는 직접 편집하면 된다. 생성 도구는 저장소에 넣지 않았다.
- 변경 보존: 작업 전 사용자 변경 4건(`AllIn1SpriteShaderLit*.shader` 2개, `NanumGothic Combat SDF.asset`, `EditorBuildSettings.asset`의 줄바꿈만 다름)은 건드리지 않았다. 빌드 설정에는 로비 한 줄만 추가했다. TitleScene·LoadScene은 Unity가 다시 저장해 직렬화 순서·ID가 일부 바뀌었다.
- 검증: 별도 검증 복사본(`.utmp/sv`, Git 제외)에서 Unity 6000.5.0f1 PlayMode 신규 `SceneFlowTests` **4/4 통과**(타이틀→로비, 로비 UI 구성, 로비→메인, 목적지 없는 로딩→메인). 씬을 이미지로 렌더링해 세 화면을 눈으로 확인했다. 기존 전체 PlayMode 회귀는 실행하지 않았다. 실기기·Android·해상도별 확인 없음.
- 직접 확인: Unity에서 임포트 완료 → `Tools > Play From Title` 켜기 → Play → 타이틀 화면 아무 곳이나 클릭 → 로딩 화면 → 로비(프로필·캐릭터·탭이 보임) → 게임 시작 → 로딩 → `MainScene`. 정상이면 로비의 탭·아이콘은 눌러도 반응이 없다. 이상하면 Console 첫 오류, `File > Build Profiles`의 Scene List에 `LobbyScene`이 있는지, 로비 Canvas에서 `UI_LobbyScene`의 Missing Script 여부, 한글이 네모로 나오면 TMP 폰트(`NanumGothic Combat SDF`)를 확인한다. 에디터 `Window > General > Test Runner > PlayMode`의 `SceneFlowTests`(테스트 중에는 Play From Title을 끈다)로도 확인된다.
- 다음: 로딩·로비 배경 원본 아트 교체, 탭·아이콘 화면 정의 후 연결, 로비에서 플레이어 이름·골드 데이터 연결, 기존 "스킬 자세히 보기의 MP·쿨다운" 작업.

## 2026-11-06 보스 등장에 맞춰 HP바 페이드인

- 요청: 보스 HP바도 천천히 나타나게 한다. BossEncounterController에 Boss Health Fade Duration(기본 1초, 0은 즉시 표시)을 추가했다. 기존 Boss Health Slider 오브젝트에 런타임 CanvasGroup을 확보하고 알파를 0→1로 올린다. 원형 등장 연출 동안은 투명하게 유지하고 BossController.IsEntering이 끝나는 공개 타격 시점부터 페이드한다. 별도 등장 패턴이 없는 다른 보스는 활성화 직후 페이드한다. 체력 수치/Slider value는 건드리지 않아 기존 EnemyHealth 표시와 충돌하지 않는다.
- 보스 사망/Encounter 비활성 시 진행 중 페이드 코루틴을 정리하고 HP바를 숨긴다. 재활성 시 보스전이 진행 중이면 다시 표시 경로를 시작한다. Time.deltaTime을 써 일시정지 중 페이드도 멈춘다. 기존 CanvasGroup이 있으면 알파만 사용하며 상호작용/레이캐스트 설정은 변경하지 않는다. 씬·프리팹은 저장하지 않았다. 이전 미커밋 작업 및 사용자의 DNA(EXP)/ZombieHound 변경을 보존했다.
- 검증: 설치 Unity 6000.5.0f1 Roslyn으로 관련 런타임/Editor 6개 컴파일 모두 종료 코드 0, 기존 Player.ParryCamera.cs:56 CS0252 경고 1개. git diff --check 통과. 이번 페이드 변경 Unity 테스트 0개, 실제 렌더링·시점·일시정지는 미검증이다.
- 직접 확인: Unity 컴파일 완료 → MainTestScene의 Boss Encounter > Boss Health Fade Duration 지정 → Tools > Play From Title 해제 → Play → 보스 등장. 원형 예고 동안 HP바는 보이지 않고 보스 공개와 함께 약 1초에 걸쳐 선명해져야 한다. 보스 피격 시 기존 체력 값이 반영되고 처치 시 바로 숨겨져야 한다. 오류 시 Console 첫 오류, Boss Health Slider 참조, 해당 오브젝트의 상위 활성 상태/CanvasGroup 알파, Boss Health Fade Duration 및 씬 Overrides를 확인한다.

## 2026-11-06 등장 폭발 지속 시간 별도 설정

- BossAttackHitVfx에 `Entrance Area Burst Duration`(entranceAreaBurstDuration)을 추가했다. 양수면 등장 폭발만 해당 초 동안 확산·페이드하며, 0이면 기존 Area Burst Duration을 사용해 기존 설정을 보존한다. 일반 원형 공격은 기존 Area Burst Duration을 계속 사용한다. 등장 전 예고 시간/보스 공개 타격 시점/공격 범위는 바꾸지 않았다.
- BeginBurst에서 선택한 시간을 activeBurstDuration으로 보관해 남은 시간뿐 아니라 확산·알파 진행률에도 동일하게 적용한다. 표시 중 Inspector 값을 바꿔도 현재 VFX의 진행률은 바뀌지 않고 다음 실행부터 적용한다. 씬·프리팹은 수정하지 않았다.
- 검증: Unity 6000.5.0f1 설치 Roslyn으로 관련 런타임/Editor 6개 어셈블리 컴파일 모두 종료 코드 0, 기존 Player.ParryCamera.cs:56 CS0252 경고 1개. git diff --check 통과. 이번 설정 변경 Unity 테스트 0개, 실제 표시 시간/화면은 미검증이다.
- 직접 확인: Play 종료 → Unity 컴파일 완료 → 보스 오브젝트 또는 BossMvp 프리팹의 Boss Attack Hit Vfx → 등장 연출 전용 VFX > Entrance Area Burst Duration에 원하는 초 지정 → MainTestScene Play → 보스 등장 폭발이 지정한 시간 동안 확산·사라지는지 확인. 예를 들어 2는 2초, 0은 기존 Area Burst Duration 사용이다. 이후 일반 원형 패턴은 기존 지속 시간이어야 한다. 이상 시 Console 첫 오류, 씬 인스턴스 Overrides와 실제 보스의 두 Duration 값, Time.timeScale 및 폭발 Sprite 참조를 확인한다. 기존 사용자 변경은 보존했다.

## 2026-11-06 기존 원형 패턴으로 보스 등장·등장 폭발만 크게 표시

- 사용자 확정: 별도 등장 리소스/배너/카메라 연출을 만들지 않고 기존 원형 패턴을 그대로 쓴다. 보스 모습을 숨기고 원형 타격 VFX가 터질 때 공개한다. 이 등장 연출의 폭발 VFX만 맵 어디에 있든 보이도록 크게 한다. 초기 아이디어의 별도 3초 대기/피해 없는 새 패턴은 추가하지 않았다.
- 연결: BossEncounterController가 기존 보스를 켜기 직전 BossController.QueueEntrance를 호출한다. 현재 MainTestScene의 BossController 야수 보스에 적용한다. HordeLordBoss/ZombieSlimeBoss처럼 해당 원형 패턴이 없는 다른 보스는 기존 등장 경로를 유지한다. 씬·프리팹·애니메이션·meta는 이번 요청으로 수정하지 않았다.
- BossController: 등장에서 추적을 끄고 보스 SpriteRenderer.forceRenderingOff와 Collider2D.enabled의 원래 상태를 보관해 숨긴다. HitFlashExcluded가 붙은 기존 VFX는 숨기지 않는다. EnemyHealth.EnsureInitialized를 공개해 원래 피격/몸체 캐시를 먼저 준비한 뒤 콜라이더를 임시 비활성화한다. 실제 기존 PerformPhaseTwoAreaAttack(예고→양손 공격 애니메이션→타격 이벤트)을 재사용한다. AnimationEvent_ApplyAreaDamage에서 보스 모습/콜라이더를 복원하고 등장용 큰 VFX를 실행한다. 원형 피해는 원래 activeAreaRadius로 처리한다. 애니메이션 종료 후 일반 패턴 루프로 이어진다. 중단/비활성/타격 이벤트 누락 시 finally와 OnDisable에서 숨김 상태를 복원한다.
- VFX: BossAttackHitVfx.ShowEntranceBurst만 별도로 큰 표시 크기를 계산한다. Inspector의 Entrance Burst Minimum Size 기본 100(시각 연출용)을 하한으로 하며, Main Camera 뷰포트 네 모서리까지의 최대 거리×4까지 확장한다. 기존 폭발 스프라이트/색/확산 시간은 그대로 사용한다. 일반 전투 ShowAreaBurst, 예고 원 반경, 피해 반경은 확대하지 않는다. 커진 VFX는 원래 패턴 반경 밖까지 보이지만 그 밖에 피해를 추가하지 않는다. 카메라가 없으면 최소 표시 크기를 사용한다.
- 검증: tools/BossEntranceValidation에서 실제 BossAttackHitVfxShape의 최소 크기/원거리 카메라/보스 위치 오프셋/가장 먼 모서리 유지/기존 공격 직경/부모 배율 보정 6/6 통과. Unity API 계산 경계는 대역이므로 화면 검증이 아니다. 기존 양손 클립의 타격/종료 이벤트와 BossMvp의 폭발 Sprite 참조를 정적 확인했다. 설치 Unity 6000.5.0f1 Roslyn으로 Combat.Runtime→EnemyMvp.Runtime→Boss.Runtime→Encounter.Runtime→Assembly-CSharp→Editor 6개 컴파일 모두 종료 코드 0. 기존 Player.ParryCamera.cs:56 CS0252 경고 1개. 결과는 .utmp/BossEntrance/Compile 생성물. git diff --check 통과. Unity 테스트 0개. 실제 씬/애니메이션 진행/중단/렌더링/기기는 미검증이다.
- 보존: 시작 시 직전 Sprint 코드/테스트/.gitignore/Handoff 미커밋 변경 및 사용자의 DNA(EXP).prefab·ZombieHound.prefab 변경을 보존했다. 사용자 변경과 별개로 씬이나 프리팹을 재저장하지 않았다.
- 직접 확인: Unity 컴파일 완료 → Tools > Play From Title 해제 → MainTestScene Play → 기존 보스 등장 시각까지 진행. 보스는 보이지 않고 원형 예고만 나타난 뒤 타격 폭발과 함께 모습을 드러내야 한다. 이후 추적/공격으로 이어져야 한다. 멀리 떨어진 위치에서도 큰 폭발이 보이는지 확인한다. 이후 HP 절반 이하의 기존 원형 패턴 폭발은 원래 크기여야 한다. 큰 등장 VFX의 바깥 시각 영역만으로 추가 피해를 받으면 안 된다(원래 반경 안에서는 기존 피해가 있다). 등장 중 일시정지는 기존처럼 Time.deltaTime에 따라 멈춘다.
- 오류 확인: Console 첫 오류, Boss Encounter의 Boss/Target 참조, Boss Animator의 Base Layer.BossSwipeBoth 및 AnimationEvent_ApplyAreaDamage, BossAttackHitVfx의 Range Material/Area Burst Sprite/MainCamera 태그를 확인한다. 과하게 크면 BossAttackHitVfx의 Entrance Burst Minimum Size를 조절할 수 있지만 카메라 거리 기반 하한도 적용된다. 실제 플레이 검증과 기존 스킬 자세히 보기 MP·쿨다운 후속은 남아 있다.

## 2026-11-06 Sprint 쿨타임 남은 초 표시

- 후속 요청: Cooldown 이미지와 같은 표시 수명으로 숫자도 표시한다. SprintCooldownUI에 TMP_Text cooldownText를 추가했다. Inspector에서 기존 TMP 텍스트를 연결할 수 있고, 미지정이면 Awake에서 Cooldown Image 아래 중앙 정렬의 Cooldown Seconds(TextMeshProUGUI, 36 크기, 흰색 Bold)를 생성한다. 별도 폰트 에셋은 만들지 않는다. 생성 텍스트는 컴포넌트 파괴 시 함께 제거한다.
- 동작: 실제 DashCooldownRemaining을 올림 정수로 표시한다(3초라면 3→2→1). 0초와 미사용/플레이어 없음에는 텍스트를 비우고 표시 컴포넌트·별도 오브젝트를 끈다. 쿨타임 중에만 이미지와 함께 켠다. 스크립트 비활성 시에도 숨기며, TMP raycastTarget=false로 클릭을 가로채지 않는다. Sprint 버튼 자신이나 이를 포함한 상위 오브젝트는 끄지 않는다. 이미지의 0→1 시계방향 진행은 유지한다.
- 검증: Unity 6000.5.0f1 설치 Roslyn + 격리 runtime.rsp로 Assembly-CSharp 컴파일 종료 코드 0, 오류 0, 기존 Player.ParryCamera.cs:56 CS0252 경고 1개. git diff --check 통과. Unity 테스트 0개, 실제 TMP 렌더링/모바일 조작은 미검증. 씬·프리팹은 수정하지 않았다.
- 직접 확인: Unity 컴파일 완료 → Sprint의 SprintCooldownUI에서 기존 Player/Cooldown Image 연결 유지 → Cooldown Text는 비워 자동 생성하거나 직접 만든 TMP 텍스트 연결 → Play. 시작 때 숫자 숨김, 대시 성공 시 남은 초 표시, 쿨 종료 시 이미지와 숫자 모두 숨김, 재사용 시 다시 표시되어야 한다. 일시정지 중 숫자도 그대로여야 한다. 안 보이면 Console 첫 오류, TMP 기본 폰트/지정 폰트, 텍스트 RectTransform 크기·부모 활성 상태, Player/Cooldown Image 참조를 확인한다.

## 2026-11-06 Sprint Cooldown 오브젝트 표시 수명

- 후속 요청에 따라 SprintCooldownUI의 별도 Cooldown Image 오브젝트는 미사용/쿨타임 완료/플레이어 없음에는 비활성, 실제 남은 쿨타임이 양수일 때만 활성화한다. 쿨타임 중 Fill Amount 0→1과 시계방향 설정은 유지한다. 스크립트 비활성 시에도 별도 표시를 숨긴다.
- 스크립트는 계속 켜져 있는 Sprint 버튼에 두고, Cooldown Image는 별도 자식(또는 버튼을 포함하지 않는 별도 오브젝트)의 Image를 연결한다. 버튼 자신이나 버튼을 포함한 상위 Image를 연결한 경우에는 버튼/갱신 루프를 끄지 않기 위해 오브젝트 비활성화를 생략한다. 기존 자신 Image 대체 경로는 호환용으로 남겼다. 씬·프리팹은 수정하지 않았다.
- 검증: 설치 Unity Roslyn으로 변경 소스를 포함한 Assembly-CSharp 컴파일 종료 코드 0, 오류 0, 기존 Player.ParryCamera.cs:56 CS0252 경고 1개. git diff --check 통과. 이번 표시 변경 Unity 테스트 0개, 실제 클릭/화면 미검증.
- 직접 확인: Sprint 버튼의 SprintCooldownUI > Cooldown Image에 자식 Cooldown Image 지정 → Play 시 Cooldown 꺼짐 → 대시 성공 시 켜지고 0→1 → 쿨타임 종료 시 꺼짐 → 재사용 시 다시 켜짐. MP 부족 등 실패 때는 켜지지 않아야 한다. Sprint 버튼은 계속 활성 상태여야 한다. 이상 시 Console 첫 오류, 스크립트가 Sprint 버튼에 있는지, Cooldown Image가 별도 오브젝트인지, Player 참조와 실제 대시 성공 여부를 확인한다.

## 2026-11-06 Sprint 모바일 버튼 시계방향 쿨타임 Image 연결

- 요청: 사용자가 만든 Sprint 모바일 UI의 회전 쿨타임 Image를 실제 대시와 연결하는 스크립트.
- 완료: `Assets/0.Scripts/Mobile/SprintCooldownUI.cs`와 meta 추가. Sprint Button에 붙이고 Player/Cooldown Image를 Inspector에서 지정한다. Player 미지정 시 같은 씬 플레이어를 찾고, Image 미지정 시 버튼 자신의 Image를 사용한다. Image는 Filled/Radial360/Top/Clockwise로 설정하며 대시 성공 직후 0에서 재사용 가능 시 1까지 시계방향으로 채운다. 자식 표시 Image는 raycastTarget을 꺼 클릭을 가로채지 않게 한다. 버튼 자체 Image의 클릭 수신은 유지한다.
- 연결: PlayerMovement의 실제 nextDashTime과 승인된 쿨타임 길이로 DashCooldownRemaining/Progress를 읽는다. Player는 이를 읽기 전용으로 공개하고 TryUseDash()로 현재 조준 방향의 기존 TryDash를 호출한다. SprintCooldownUI는 Button.onClick에 활성/비활성 수명에 맞춰 리스너를 연결/해제한다. 별도 UI 타이머가 없으므로 키보드 대시도 반영하고, Time.time 정지 중 UI도 진행하지 않는다. MP 부족/행동 잠금/재사용 대기 등 기존 대시 실패는 타이머를 시작하지 않는다. UI만을 이유로 Button.interactable은 변경하지 않는다.
- 보존: 시작 시 main / b98949a, git status 깨끗함. 셰이더와 Project Settings는 수정하지 않았다. 씬/프리팹은 수정하지 않았다. 현재 저장된 MainTestScene/GiungTestScene의 Sprint는 Image/Button만 있고 이벤트가 비어 있어서 사용자가 새 컴포넌트를 붙이는 단계가 필요하다. 별도 Button On Click 등록은 하지 않는다(스크립트가 자동 등록).
- 검증: `tools/DashCooldownValidation`에서 실제 PlayerMovement와 Unity 시간/이동 대역으로 8/8 통과(초기/성공/절반·완료/재입력 실패/방향 없음/0쿨/시간 정지/재사용). UI 클릭·렌더링은 대역 검증 범위 밖이다. Unity 6000.5.0f1 Roslyn + 현재 Bee 응답 파일에 새 UI 소스를 포함해 Assembly-CSharp 종료 코드 0, 오류 0, 기존 Player.ParryCamera.cs:56 CS0252 경고 1개. 출력은 `.utmp/DashCooldown`로 격리. git diff --check 통과. Unity 테스트 0개, 실제 화면·모바일 기기는 미검증이다.
- 직접 확인: Play 종료 → Unity 컴파일 완료 → Hierarchy MobileUI 아래 Sprint 선택 → Add Component > Sprint Cooldown UI → Player 칸에 씬 Player 드래그 → Cooldown Image에 사용자가 만든 회전용 Image 드래그(비우면 Sprint 자신의 아이콘이 채워짐) → Play. 방향 입력 후 Sprint 클릭 시 실제 대시와 함께 Image가 0→1로 시계방향 채워져야 한다. 키보드 대시도 같은 표시, 쿨 중 연타는 표시를 초기화하지 않음, 일시정지 중 진행 정지를 확인한다. 기존 Button On Click에 TryUseDash를 중복 등록하지 않는다.
- 오류 확인: Console 첫 오류, SprintCooldownUI 활성 상태/Player와 Image 참조, 대시용 MP와 방향 입력, Player Dash Cooldown, Image Source Image와 색상 알파, 상위 UI의 raycast 차단 여부를 본다. 이번 표시는 쿨타임 준비만 나타내므로 가득 차 있어도 MP 부족이나 다른 행동 중에는 대시가 거절될 수 있다.

## 2026-11-06 보유 패시브 연결 검증

- 기준은 `passive-inspection`의 `6b5deeb`이며 시작 작업 트리는 깨끗했다. `MainScene`의 `CombatUI`가 경험치로 레벨업 창을 열고, 카드가 약공격을 보유 슬롯에 추가한다. `Player.TickOwnedPassives`는 장착과 별개로 보유 ID를 런타임 JSON에서 찾아 자동 시전한다. 대상이 있으면 기본 피해 공식에 배율 0.2를 적용하고 MP를 소모하지 않는다. 강공격도 보유하면 이 경로를 쓰지만, 현재 기본 레벨업 추첨은 약공격만 제공한다. 카드의 패시브 설명 필드는 채워지지 않는다.
- 배포 JSON의 약·강 공격 36개 중 35개에 양수 쿨타임이 있다. 그중 화염 십자(20012)는 7초가 설정되어도 `LightSkillCast.TryShape`가 `Cross`를 지원하지 않아 건너뛴다. 태양 검(10007)은 엑셀 약공격 시트 AH9가 2초, 런타임 JSON은 0초라 게임에서 발동하지 않는다. 두 파일의 패시브 쿨타임·배율 비교에서 이 한 행만 달랐다. 엑셀과 JSON 중 태양 검 값을 어느 쪽에 맞출지는 검증 범위 밖으로 남겼다.
- Unity 6000.5.0f1에서 격리 복사본 컴파일 성공. 처음 배치 실행은 라이선스 IPC 실패로 중단했고, 재시도에서 `MainScene` 대상 임시 PlayMode 3개를 실행해 2개 통과·1개 실패·생략 0. 기본 자동 공격을 분리한 상태에서 경험치 13 → 레벨업 카드 클릭 → 미장착 전기 화살(10021) 피해 1.85 및 MP 100 유지, 화염 망치(20001) 보유 등록 API → 피해 3.3 및 MP 100 유지를 확인했다. 화염 십자(20012)는 같은 등록·표적 조건에서 피해 0회를 재현했다. `.utmp/PassiveInspection/playmode.xml` 및 `unity-retry.log`에 결과가 있다. `tools/CombatSkillTableValidation/Run.ps1`은 16/18 통과. 실패 2개는 태양 검의 Fusion 쿨타임 0 기대 및 엑셀과 JSON 일치 검사다.
- 직접 확인은 Unity `Tools > Play From Title` 해제 → `MainScene` Play → Hierarchy의 `CombatUI` → Inspector에서 `CombatPopupController` 메뉴의 `테스트/경험치 13 획득` → 약공격 카드 선택 → 공격 버튼을 누르지 않고 화면 안의 적에 접근하는 순서다. 일시정지의 공격 스킬 목록에 선택한 스킬이 보이고 미장착 상태에서도 해당 효과와 피해가 생기며 MP가 줄지 않아야 한다. 시작 직후 보이는 기본 탄환은 별도 동작이다. 문제가 있으면 Console 첫 오류, 보유 목록 등록, 표적의 화면 안 위치·사거리·`Enemy` 태그를 확인한다. 이후 확인할 항목은 전체 스킬의 반복 발동 주기와 화면 연출이며, 이번에는 Windows 에디터의 배치 PlayMode만 검증했다. 원본 게임 코드·씬·데이터는 수정하지 않았다.

## 2026-11-06 야수 좀비 보스 스프라이트의 주먹 스파크 일괄 분리

- 요청: 주먹에 그림으로 구워진 붉은 스파크(VFX)를 모든 방향·동작에서 떼어 낸다. 스파크는 나중에 별도 이펙트로 붙인다.
- 완료: `Assets/2.Resources/Art/Beast_Zombie_Boss/Idle/animations`의 idle·move·run·sprint·swipe·swipe-both·swipe-left·body-charge 중 이미 작업한 3곳(body-charge/east, move/north-east, move/east)을 뺀 477장을 자동 처리했다. 픽셀이 실제로 바뀐 것은 395장이다. 나머지는 스파크가 없어 HEAD 바이트 그대로 되돌렸다. `rotations/`는 원래 스파크가 없어 건드리지 않았다.
- 방법(생성물, Git 제외 `.utmp/BeastSpark`): ① 몸 밖으로 뻗은 가는 붉은 줄과 밝은 빨강(R≥225) 스파크 덩어리를 찾는다. ② 그 주변의 붉은 틴트·가는 줄을 지운다. ③ 몸 안쪽은 프레임 안의 원래 피부·바지 팔레트 색 중 밝기가 비슷한 색으로 바꾸고, 몸 밖은 투명 처리한다. ④ 5×5 이상 덩어리 붉은 상처(어깨·무릎)는 보호한다. ⑤ 몸에서 떨어진 20픽셀 미만 조각은 지운다. idle 옆모습 일부(east, west, north-east, north-west)는 주먹 영역을 손으로 지정했고, sprint/south-west 2·3번은 얼굴·가슴 영역을 보호했다. 캔버스 크기·알파(0/255)·meta는 그대로다.
- 유지함: 어깨·무릎의 붉은 상처, 가슴·등의 붉은 핏줄, 얼굴 피, 팔뚝의 분홍 흉터 줄, 스와이프의 붉은 베기 궤적(별도 이펙트로 볼 수 있어 이번에 지우지 않았다. 지우려면 따로 요청). 스프라이트 GUID·애니메이터·프리팹·코드는 바꾸지 않았다.
- 알려진 한계(자동 처리라 완벽하지 않다): ① run·sprint·body-charge 일부 프레임에서 주먹이 은은한 붉은 장갑색으로 남았다. ② (해결) body-charge/south-west 3~10번 주먹 주변의 검은 가시·테두리 덩어리는 디자인이 아니라 스파크의 검은 테두리가 남은 것이라는 사용자 확인에 따라 주먹 영역(박스 `36,60,76,106;100,82,142,116`)에서 가장자리부터 벗겨 냈다(검정·짙은 빨강 픽셀 중 몸 색 이웃이 없는 것을 반복 제거 후 얇은 검은 줄 제거). 다른 방향·다른 동작은 같은 검사에서 가시가 나오지 않아 건드리지 않았다. south-west 0~2번은 가시가 적어 제외. ③ 스파크가 얼굴·가슴을 덮은 프레임은 얼굴 피·핏줄이 일부 지워졌을 수 있다. ④ 팔레트 색으로 메운 부분이 평평해 보일 수 있다. 어색한 프레임은 수작업 보정이 필요하다.
- 백업·되돌리기: 원본은 Git HEAD에 있다(이 폴더는 이후 커밋에서 바뀐 적 없음). 변경은 커밋 전이다. 되돌릴 프레임은 해당 PNG만 `git checkout -- 경로`로 복원한다.
- 확인용 문서(`Document/`, 미추적, 기존 MotionCheck 덱과 같은 형식, PowerPoint COM으로 생성, 생성 스크립트는 `.utmp/BeastSpark/mkdeck.ps1`): `BeastZombie_MotionCheck_rotation_SparkRemoved.pptx`(방향 8장, 행=동작·열=프레임), `BeastZombie_MotionCheck_SparkRemoved.pptx`(프레임 0~10, 11장, 행=동작·열=방향), `BeastZombie_SparkRemoval_Compare.pptx`(동작별 8장, 프레임 0·3의 원본(HEAD)/수정 비교). 기존 MotionCheck 두 덱은 스파크가 있는 수정 전 상태라 그대로 A/B 비교에 쓴다. PNG를 다시 고치면 이 덱은 다시 생성해야 한다.
- 검증: 전/후 비교 시트를 눈으로 확인했다. Unity 컴파일·테스트는 하지 않았다(테스트 0개). Unity 재생, 실제 씬 표시, Android는 확인하지 않았다.
- 다음 확인: Unity가 PNG를 다시 임포트할 때까지 기다린다. Project에서 `Assets/2.Resources/Art/Beast_Zombie_Boss/Idle/animations/idle/south`의 0~3번을 열어 주먹 주변 붉은 번개가 없는지 본다. 이어 `run`, `sprint` 아래 여러 방향도 열어 본다. 보스 프리팹을 씬에 두고 Play하면 대기·이동·돌진 때 주먹 스파크가 없고 어깨 상처·핏줄은 남아 있어야 한다. 그림이 비거나 분홍이면 해당 폴더를 우클릭해 Reimport하고 Console 첫 오류를 확인한다. 별도 스파크 VFX 제작과 남은 주먹 장갑색 보정은 다음 작업이다.

## 2026-11-06 보유 패시브 쿨타임 등급별 값

- 사용자 요청으로 약·강 스킬 35개의 `PassiveCooldown`을 등급별로 넣었다. 약공격 기준: Common 3 / Uncommon 3.5 / Rare 4 / Epic 5 / Legendary 6 / Mythic 7초. 강공격은 같은 등급에서 +1초. 탄 수×타격 수가 3 이상이면 +1초, 2면 +0.5초(10002·10010 4.5, 10012 5, 10013 6, 10016 7, 20004 7, 20009 6, 20015 8.5). 피해가 이미 20%라 등급이 오를수록 쿨타임만 조금 늘렸다. 확정 수치가 아니라 테스트 후 조정할 초기 균형값이다. `PassiveScale`은 그대로 비워 0.2를 쓴다.
- 비워 둠: Fusion 등급 태양 검(10007)은 획득 확률 0이고 규칙이 없어 쿨타임 0(자동 발사 없음). 전략 스킬 제외.
- 알려진 한계: 화염 십자(20012)의 범위 모양이 `Cross`인데 현재 약공격 도형에 없어 버튼·패시브 모두 발사되지 않는다.
- 상태: `Assets/Resources/Data/CombatSkillTable.json`과 `Document/SkillDatatable.xlsx`(AH열) 모두 반영됐다. 엑셀이 열려 있어 사용자가 닫은 뒤 사본으로 원본을 교체했다. 엑셀과 JSON의 쿨타임이 다르면 검증 도구가 실패한다(의도한 감지). 앞으로 엑셀을 고치면 JSON도 같은 값으로 맞춘다.
- 검증: `tools/CombatSkillTableValidation/Run.ps1` 18/18 통과(교체 후 원본 엑셀 기준). Unity 컴파일·재생은 하지 않음. `SKILL_WORKBOOK` 환경변수로 다른 엑셀 파일을 지정해 검증할 수도 있다.

## 2026-11-06 씬과 보스·적 에셋 이름 정리

- 씬 파일 네 개는 `TitleScene`, `LoadScene`, `MainScene`, `GiungScene`으로 변경했다. 빌드 목록 경로, `Define.EScene`, 씬 전환 코드, Play From Title, PlayMode 테스트의 씬 참조를 맞췄다. 씬 meta GUID와 enum 값·빌드 목록 순서는 보존했다. 기존 Giung 씬의 빌드 목록 GUID만 씬 meta의 실제 GUID로 맞췄다. 이전 작업 기록에서 쓰인 당시 이름은 유지했다.
- `BossMvp` → `Boss`, `EnemyMvp`와 이동 속도 변형 → `Enemy`와 해당 변형으로 변경했다. 프리팹의 루트 이름과 씬의 Boss 이름 오버라이드, 관련 폴더 이름, `Enemy.Runtime` 어셈블리명과 참조를 맞췄다. 프리팹·어셈블리·폴더 meta를 옮기고 기존 GUID를 보존했다. MainScene의 데미지 숫자 프리팹 및 기존 사용자 설정도 보존했다.
- 검증: 기존 meta·빌드 GUID·빌드 순서·enum 값 확인, 활성 에셋·빌드 설정·도구에서 이전 이름 0건, Overlay Canvas 정렬 4개 통과. Unity 설치본 Roslyn으로 런타임/에디터/PlayMode 테스트 어셈블리 총 7개 컴파일 종료 0. 기존 Player.ParryCamera CS0252 경고 1개. 실제 Unity 테스트 0개, 에디터 Play·빌드 미실행.
- 직접 확인: Unity 임포트 완료 → `Tools > Play From Title` 켜기 → Play → 타이틀의 시작 버튼 → `LoadScene` → `MainScene`. 승패 화면에서 재시작 또는 타이틀 버튼을 눌러 이동을 확인한다. Project의 `Assets/1.Prefabs`에서 Boss와 Enemy를 열어 이름과 스크립트가 정상인지 확인한다. 이상하면 Console 첫 오류, `File > Build Profiles`의 Scene List, 프리팹의 Missing Script 표시를 확인한다.

## 2026-11-06 GameManager의 데미지 숫자 표시 생성

- `MainTestScene`의 기존 독립 표시 오브젝트 설정을 `Assets/1.Prefabs/DamageNumberDisplay.prefab`으로 옮겼다. 기존 `GameManager.Awake`가 프리팹이 연결된 씬에서만 자식으로 한 번 생성한다. 씬이 내려가면 인스턴스와 숫자 풀이 정리된다. `GiungTestScene`은 프리팹 미연결 상태다.
- 표시 스크립트는 `Assets/0.Scripts/Game/UI/DamageNumberDisplay.cs`로 옮기고 기존 meta GUID를 보존했다. 클래스, Canvas 및 메뉴 이름에서 Test를 제거했다. 수동 배치용 Tools 메뉴는 제거했고 런타임 컴포넌트의 `샘플 숫자 표시 (Play 중)`는 카메라 중앙에서 동작한다.
- 프리팹은 기존 나눔고딕 폰트와 색상·크기·지속시간·최대 개수 설정을 사용한다. 카메라는 첫 표시 때 `MainCamera` 태그로 찾는다. 저장된 폰트 에셋과 스폰·회수 설정 변경은 보존했다.
- 검증: Unity 설치본 Roslyn으로 Assembly-CSharp와 Assembly-CSharp-Editor 컴파일 종료 0, 기존 Player.ParryCamera CS0252 경고 1개. 임시 .NET 풀링 검증 3/3, Overlay 정렬 검증 4개, 씬→프리팹 컴포넌트 참조·원본 스크립트 GUID·설정·다른 씬 데이터 보존 확인. Unity 테스트 0개, 에디터 Play 및 Profiler 미실행.
- 직접 확인: Unity 임포트 완료 → `Tools > Play From Title`이 켜져 있으면 해제 → `MainTestScene`에서 GameManager Inspector의 Damage Number Prefab 확인 → Play. GameManager 아래 `DamageNumberDisplay(Clone)`이 한 개 생기고 첫 타격 또는 컴포넌트의 `샘플 숫자 표시 (Play 중)`에서 숫자가 떠야 한다. 재시작 뒤에도 인스턴스는 한 개여야 한다. 문제가 있으면 Console 첫 오류, 프리팹 참조, 폰트, 활성 카메라의 `MainCamera` 태그를 확인한다.

## 2026-11-06 데미지 숫자 풀링

- `DamageNumberTest`가 자기 Canvas 안에서 숫자와 외곽선 머티리얼을 재사용한다. 처음 필요한 만큼 생성하고, 만료된 숫자는 숨겨서 반납한다. 동시 한도(씬 기본 48)에 도달하면 가장 오래된 표시를 새 타격에 재사용한다. 대여할 때 표시값·위치·색·경과 시간·그리기 순서를 갱신한다. 컴포넌트를 끄거나 씬에서 내리면 사용 중 및 대기 중 머티리얼과 Canvas를 정리한다.
- 기존 씬의 폰트·카메라·색·0.8초 지속시간·Canvas 정렬을 그대로 사용한다. 사용자 변경인 `MainTestScene.unity`와 `NanumGothic Combat SDF.asset`은 수정하지 않았다.
- 검증: 실제 컴포넌트 소스를 링크한 임시 .NET 검증에서 재사용 관련 2건의 수정 전 실패를 확인했고 수정 후 만료 재사용·초과 시 교체 순서·비활성화/재활성화 정리 3/3 통과. `tools/ValidateDamageNumberSorting.ps1` 통과(두 씬의 Overlay Canvas 4개). Unity 설치본 Roslyn으로 Assembly-CSharp 컴파일 종료 0, 기존 `Player.ParryCamera.cs` CS0252 경고 1개. Unity 테스트 0개. 실제 씬 재생과 Profiler 프레임 시간은 미확인.
- 직접 확인: Unity 임포트 완료 → `MainTestScene` Play → `DamageNumberTest` Inspector의 컴포넌트 메뉴에서 `테스트/샘플 숫자 표시 (Play 중)`를 띄우고 숫자가 사라진 뒤 반복한다. Canvas 아래 기존 `DamageNumber` 자식이 비활성화됐다가 다시 켜지고, 같은 동시 표시 수에서 개수가 계속 늘지 않아야 한다. 광역 공격과 플레이어 피격 때 색과 외곽선도 확인한다. 이상하면 Console 첫 오류와 컴포넌트의 Font·Target Camera 연결을 확인한다.

## 2026-11-06 플레이 렉 조사 — 데미지 숫자와 발밑 그림자

- `main`의 `6a2cbec`에서 조사했다. 사용자가 저장한 `MainTestScene`의 `DamageNumberTest` 오브젝트와 컴포넌트는 활성 상태이며 `NanumGothic Combat SDF`와 `Main Camera`가 연결되어 있다. 씬과 폰트 에셋의 사용자 변경을 보존했다.
- 다중 타격 때 끊긴다면 데미지 숫자를 먼저 비교한다. 타격마다 TMP 오브젝트와 외곽선용 머티리얼 인스턴스를 생성하고, 표시 중에는 매 프레임 좌표와 알파를 갱신하며 만료 시 파괴한다. 동시 48개 제한은 생성 빈도를 제한하지 않는다. 저장된 동적 폰트에는 숫자·마침표 글리프와 두 번째 아틀라스가 추가되어 있다. 첫 타격에서만 튀는 경우에는 Canvas 최초 생성과 반복 타격 비용을 분리해서 본다.
- 적이 쌓일수록 계속 느려진다면 그림자를 분리한다. `GroundShadow`는 공유 스프라이트를 쓰며 발밑 크기·위치를 처음에만 계산한다. 활성 유닛마다 `LateUpdate`의 렌더러 속성 갱신과 반투명 스프라이트 렌더링이 남는다. 같은 씬 저장에서 스포너 수 5→10, 화면 밖 스폰 여유 10→5, 회수 여유 20→10이 바뀌었으므로 비교 중에는 이 값들과 활성 적 수를 통제한다.
- 검증은 씬·코드·로컬 TMP 패키지 소스 확인까지 했다. Unity 테스트 0개, Play 재현 및 Profiler 캡처 미실행. 다음은 `MainTestScene` Play → `Window > Analysis > Profiler` → `CPU Usage > Timeline`과 `Rendering`에서 기준 프레임을 기록한다. Play 중 `DamageNumberTest` 컴포넌트만 꺼서 숫자와 타격 프레임 스파이크가 함께 사라지는지 본다. 그림자 비교는 숫자를 끈 상태에서 Play 중 `Ground Shadow` 오브젝트들만 꺼서 그림자가 사라지고 유닛은 계속 움직이는지 확인한 뒤 같은 밀도의 프레임을 비교한다. 새로 스폰된 그림자와 Console 반복 경고를 확인한다. 실제 원인은 프레임 비교 후 확정한다.
## 2026-11-06 보유 약·강 스킬의 패시브 칸

- 완료: `Document/SkillDatatable.xlsx` 약공격·강공격 시트에 `Passive_Cooldown`(AH), `Passive_Scale`(AI)을 추가했다. 데이터 칸은 비었다. 변환기와 배포 JSON도 같은 두 칸을 읽는다. 쿨타임이 0이면 자동 발사하지 않고, 배율이 비어 있으면 피해만 기존 공식의 0.2다. 탄 수·범위·타격 횟수·모양은 그대로다. MP와 입력 잠금은 쓰지 않는다. 보유 중이면 장착 여부와 상관없이 동작하고, 버튼 시전은 기존 수치를 유지한다.
- 유지함: 액티브 비용·피해·쿨타임. 넉백·슈퍼아머는 자동 시전에 넣지 않았다. 패시브 초는 정하지 않았다. 전략 스킬은 대상이 아니다.
- 검증: `tools/CombatSkillTableValidation/Run.ps1` 18/18 통과. Unity 컴파일·씬 재생은 하지 않음. 엑셀과 배포 JSON은 이번 이전부터 아이콘·회복 효과·강공격 반경이 다르다. 전체 변환을 다시 돌리면 배포 JSON의 기존 전투 수치가 엑셀의 빈 칸으로 덮인다.
- 다음 확인: 아래 사용자 절차. 자동 발사를 보려면 해당 스킬 JSON의 `PassiveCooldown`만 임시로 채운다. `Tools > Parse Combat Skill Table`은 돌리지 않는다.

## 2026-11-06 야수 좀비 보스 돌진(body-charge) 동쪽 붉은 오라 제거

- 완료: `Assets/2.Resources/Art/Beast_Zombie_Boss/Idle/animations/body-charge/east`의 0~10.png(188×188). 사용자가 블러로 번지게 지운 0·1·8·9·10번은 버리고, Git HEAD 원본 11장에서 다시 작업했다. 주먹·팔뚝·몸통 주변의 붉은 피 튀김·오라 픽셀을 지우고 주변 피부·바지 색(원래 팔레트 색만 사용)으로 채웠다. 외곽선은 유지하고 알파는 바뀌지 않았다. 몸에서 떨어진 붉은 파편 5픽셀만 투명 처리했다(9번).
- 유지함: 어깨의 둥근 붉은 상처와 무릎의 붉은 표식(디자인 요소로 판단. 북동쪽 이동 작업과 같은 기준). 이것까지 지우려면 따로 요청해야 한다. 어깨 원 하단과 일부 프레임(2·4번)에 오라 잔여 픽셀이 몇 개 남을 수 있다.
- 바꾸지 않음: meta, 다른 방향·동작, 코드, 프리팹. 파일 용량은 저장 방식 차이로 HEAD보다 줄었다. Unity 테스트 0개. 에디터 재생은 확인하지 않음.
- 백업: 사용자 블러본은 에이전트 저장소(`BeastZombieBoss_bodycharge_east_userblur_backup`)에 있다. 이 폴더 11장은 커밋 전이다.
- 다음 확인: Unity에서 폴더 0~10을 열어 주먹·팔 주변에 붉은 오라가 없고 어깨 상처만 보이는지 본다. 같은 보스의 body-charge 다른 방향(north-east 등)이 같은 상태인지는 아직 확인하지 않았다.

## 2026-11-06 BossRevise와 플레이어 성장 브랜치 병합

- 로컬 main의 시작점은 b26e7f0이며 작업 트리는 깨끗했다. BossRevise(3a7fc60)에 이어 feature/player-growth-design(6f83583)을 병합했다. 두 브랜치의 인수인계 기록을 함께 보존했다.
- BossMvp의 Capsule은 main 쪽 기존 fileID를 사용했다. MainTestScene은 BossRevise 쪽 이동 속도·HP·사거리·MP 회복 설정을 유지하고 성장 브랜치의 기본 공격 간격 0.333초·공격력 1·탄속 6 및 XP 공식·회수 설정을 반영했다. 스킬 표 변경은 main의 Document/SkillDatatable.xlsx 경로에 반영됐다. 내보내기 도구의 기존 루트 경로 참조는 후속 확인 대상이다.
- 검증은 Unity 설치본 Roslyn으로 런타임·에디터 어셈블리 6개를 컴파일했다. 오래된 Bee 응답 파일에 빠진 ZombieSlimeBoss.cs를 검증용 사본에 추가했으며 기존 CS0252 경고 1개가 남았다. .NET 9 임시 프로젝트에서 성장·스폰 계산 28/28 통과. Unity 테스트 0개, 원본 씬 Play·Android 빌드·기기 미검증. 수정 대상 프리팹/두 테스트 씬 YAML 객체 ID 중복 및 충돌 표시 0건을 확인했다.
- 다음 확인은 Unity 임포트 완료 → Tools > Play From Title 해제 → MainTestScene Play 순서다. 기본 탄환이 사거리 내 적에게 자동 발사되는지 확인한다. CombatUI의 CombatPopupController에서 테스트/경험치 13 획득을 실행하면 Lv.2 카드가 열려야 한다. 보스 HP를 절반 이하로 낮추면 2페이즈 원형 패턴이 한 번 시작되는지 확인한다. 이상 시 Console 첫 오류와 PlayerData·CombatPopupController 연결, BossMvp Capsule 및 Animator를 확인한다.
- 원본 씬 통합 확인 후 기존 스킬 자세히 보기 MP·쿨다운 작업과 성장 브랜치의 실제 스폰·처치·회수율 계측을 이어간다.

## 2026-11-05 야수 좀비 보스 북동쪽 이동 어깨 상처·팔 흉터 복원

- 완료: `Assets/2.Resources/Art/Beast_Zombie_Boss/Idle/animations/move/north-east`의 0~7.png. 붉은 오오라(주먹 주변 피 튀김)는 이미 지워진 상태를 그대로 두고, 오오라를 지우며 같이 회색으로 덮인 어깨의 붉은 상처 안쪽과 팔의 연한 분홍 흉터 줄만 10/28 커밋(`a4faa2b`)의 같은 프레임에서 픽셀 단위로 복원했다. 어깨는 x72~85, y35~47 영역, 팔 흉터는 원본이 옅은 분홍인 픽셀만 대상으로 했다. 프레임 0의 어깨도 같은 원본으로 통일했다.
- 바꾸지 않음: 주먹의 붉은 색(오오라에 가까워 지운 상태 유지), 등의 붉은 선, 무릎 표식, 다른 방향·대기·공격, meta, 프리팹, 코드. 캔버스는 128×128 그대로. Unity 테스트 0개. 에디터 재생은 확인하지 않음.
- 알아 둘 점: 이 폴더의 8장은 커밋 전이다. 저장 방식이 달라져 파일 용량이 약 4KB에서 약 8KB로 늘었다. 그림 자체는 프레임 1~7이 HEAD와 같았다.
- 다음 확인: Unity에서 해당 폴더 0~7을 열어 어깨가 붉은 상처로 보이고 주먹 주변에 붉은 튀김이 없는지 본다. 주먹 붉은 색도 살리려면 그 부분만 따로 요청한다.

## 2026-11-05 오늘 작업 커밋과 origin/main 병합

- 로컬 커밋은 기획 문서와 데이터 표를 `Document/`로 옮기고, 화염 고리를 조준과 상관없이 가로로 두며, 좀비 슬라임 보스를 추가하고, 야수 보스 이동·회전 그림을 갱신했다.
- origin/main 10커밋을 병합했다. 보스 재작업, 스킬 표, 레벨업, 콜라이더 변경이 들어왔다. PlayMode 테스트 삭제는 원격 상태를 따랐다.
- `CombatSkillTableExporter`와 검증 도구는 아직 프로젝트 루트의 `SkillDatatable.xlsx`를 읽는다. 표 파일은 `Document/SkillDatatable.xlsx`에 있다.

## 2026-11-05 야수 좀비 보스 동쪽 이동 재제작

- 완료: `Assets/2.Resources/Art/Beast_Zombie_Boss/Idle/animations/move/east`의 0~7.png를 다시 그렸다. 예전 8장은 몸통의 붉은 선이 겹쳐 픽셀을 고칠 수 없었다. 기준은 같은 폴더의 깨끗한 `rotations/east.png`다. 캔버스는 144×144, 반투명 픽셀 없음, 색은 프레임당 48~49개. 스프라이트 GUID는 그대로 두고, 잘리지 않게 각 meta의 자르기 영역만 새 몸 크기에 맞췄다.
- 바꾸지 않음: 다른 방향, 대기, 공격, 프리팹, 코드. Unity 테스트 0개. 에디터 재생은 확인하지 않음.
- 다음 확인: Unity 임포트가 끝나면 Project에서 그 폴더의 0~7을 연다. 어깨 상처는 동그란 덩어리고, 몸통을 가로지르는 붉은 긁힌 선이 없어야 한다. 4번 프레임은 앞발이 뻗고 주먹이 앞으로 나가 있다. 그림이 비거나 분홍이면 그 폴더를 우클릭해 Reimport한다.

## 다음 작업 — 스킬 자세히 보기의 MP·쿨다운

- 상태: 아직 안 함. 2026-11-05에 사용자가 나중에 하라고 남김. 이 기록만 추가했고 코드·씬·프리팹은 바꾸지 않음.
- 할 일: 스킬 자세히 보기에서 그 스킬의 MP 소모량과 쿨다운을 보여 준다.
- 현재: 일시정지 상태창의 공격·전략 슬롯은 `PlayerStatusPanel.Refresh`가 `StatusPanelUI.Cell.Show`로 이름과 아이콘만 그린다. 자세히 보기 버튼은 코드에 없다.
- 수치: 실행 데이터는 `Data.SkillData`의 `MPCost`, `CoolTime`이다. 엑셀 전투 표는 `CombatSkillRecord`의 `CostAmount`, `Cooldown`이다. 표에 없는 값은 만들지 않는다. 약공격 쿨타임은 기획상 0이다. 강공격 20001~20005와 전략 스킬은 비용·쿨이 비어 있을 수 있다.
- 유지: 상태창 슬롯은 조회용이다. 클릭으로 장착하거나 빼지 않는다. 자원 이름은 MP다.

## 2026-11-05 화염 고리 방향

- 완료: 약공격 화염 고리(10014)가 조준 방향으로 회전해, 위나 아래를 보면 가로 고리 그림이 세로로 서 있었다. `LightSkillFx`는 고리만 회전을 항등으로 두고 플레이어 위치에 그대로 둔다. 탄환·직선·부채꼴·호는 조준 방향을 유지한다. 타격은 원래처럼 시전 위치의 원형 고리다.
- 검증: PlayMode `FlameRingStaysFlatAroundThePlayerForEveryAim`를 작성했다. 배치 모드 Unity는 헤드리스 라이선스가 없어 종료 코드 198로 끝났고, 이 테스트는 실행하지 못했다. 이어서 받은 main이 `Assets/Tests/PlayMode` 스크립트를 삭제한 상태라 그 파일은 병합에서 되돌리지 않았다. 에디터 재생은 확인하지 않음.
- 노션: `팀프로젝트 - 1팀 / 작업`에 `화염 고리 세로 표시 수정`을 완료로 올렸다. https://app.notion.com/p/40f283dd24798185aa7cdedb6c3d7957 개발, 플레이어, 중간, 시작일·완료일 2026-11-05. 할당 대상은 비움.
- 다음 확인: Unity 컴파일 후 Console에 새 빨간 오류가 없어야 한다. 약공격이 화염 고리인 상태에서 위·아래·왼쪽·대각선으로 바라본 뒤 공격(마우스 왼쪽 또는 Enter)한다. 고리는 매번 플레이어 발밑에 가로로 깔려 있어야 한다. 불꽃 탄환은 바라본 쪽으로 날아가야 한다. 고리가 비거나 분홍이면 `Assets/Resources/LightSkills/lightflamering`를 우클릭해 Reimport한다. PlayMode 테스트 파일은 이번 병합에 없다.

## 2026-11-05 뷰 변경 노션 작업

- 완료: `작업` 데이터베이스에 뷰 변경 작업 7개를 추가했다. 카테고리 `뷰 변경`은 원래 있던 태그다. 상태 `시작하지 않음`, 우선순위 `낮음`, 시작일은 비움. 기획 2개는 구동현, 시스템 3개는 김기웅, 메카닉 2개는 임동진. 기존 `도트 그래픽 연출 변경 제안`은 진행 중이라 수정하지 않았다.
- 검증: 노션 조회로 7개의 상태, 우선순위, 할당, 빈 시작일을 확인했다. Unity 테스트 0개. 씬, 프리팹, 코드는 변경하지 않음.
- 다음 확인: 노션 `팀프로젝트 - 1팀 / 작업`에서 카테고리 `뷰 변경`으로 거른다. 새 7개가 시작하지 않음, 낮음이고 시작일이 비어 있으면 된다. 진행 중인 `도트 그래픽 연출 변경 제안`은 이번 추가가 아니다.

## 2026-11-05 3라운드 좀비 슬라임 보스

- 완료: 참고 그림의 시체 더미 슬라임을 위에서 비스듬히 보는 8방향 보스로 만들었다. PixelLab 오브젝트 `d31cd033-0e4a-4d81-9da0-ded7e893ed9a`. 캔버스 128×128. 회전 8장, 숨쉬기·이동 각 8방향 9프레임. 프리팹 `Assets/1.Prefabs/ZombieSlimeBoss.prefab`. 애니메이터는 `IsMoving`, `FacingX`, `FacingY`로 멈춤과 이동을 가른다. 켜지면 같은 씬의 플레이어를 찾아 다가가고, 닿으면 기존 접촉 피해를 준다. `BossEncounterController`에 Zombie Slime 칸을 추가했다. 이 칸만 연결되어 있을 때 그 보스를 켜고, 보스전 정리에서 빼지 않는다. 무리군주나 기존 보스 칸이 채워져 있으면 예전처럼 그 보스가 나온다.
- 수치: 체력 25, 접촉 피해 10, 간격 0.5초, 이동 속도 1.1. 기존 보스 프리팹에 있던 임시 값이다. 슬라임 전용 체력·패턴·보상은 정하지 않았다. 크기는 가로 약 1.5타일에 맞춘 배율 1.2다.
- 넣지 않음: 테스트 씬의 보스 참조와 등장 시각. 씬에 있는 보스는 그대로다. 6번 등장(3·6·9·12·15·18분) 중 세 번째인 9분으로 타이머를 바꾸지 않았다.
- 검증: PNG 152장 모두 128×128, Point 필터 메타 152개, 클립 16개가 컨트롤러와 연결된다. Unity 테스트 0개. 에디터 재생은 확인하지 않음.
- 다음 확인: 열린 Unity의 임포트가 끝나면 `GiungTestScene` 또는 `MainTestScene`을 연다. `ZombieSlimeBoss` 프리팹을 씬에 두고, Boss Encounter의 Boss와 Horde Lord를 비운 뒤 Zombie Slime에 그 오브젝트를 넣는다. Play 후 그 씬의 보스 등장 시각에 초록 슬라임 더미가 나와 플레이어를 따라오면 된다. 서 있으면 몸이 출렁이고, 움직이면 이동 모션이 돌아야 한다. 예전 보스가 나오면 Zombie Slime 칸이 비어 있거나 다른 보스 칸이 남아 있는 것이다. 그림이 비거나 분홍이면 `Zombie_Slime_Boss`를 우클릭해 Reimport하고 Console을 본다.

## 2026-11-05 몬스터 기획서 PPT 이어쓰기

- 시작 상태: `Zomso_Monster_기획서.pptx` 8장. 1.1 기본 좀비만 수치가 있고, 1.2·1.3은 `0 -` 자리표시자, 1.4 이후와 엘리트·보스는 없었다. 4장과 8장은 내용 없는 빈 템플릿이었다.
- 완료: 총 15장. 1.2 여성 좀비·1.3 사무 좀비_남의 수치 칸을 미정으로 바꾸고 가운데 상자 제목 "몬스터 이름"을 실제 이름으로 바꿨다. 1.4 사무 좀비_여, 1.5 연구 좀비_남, 1.6 연구 좀비_여, 1.7 공사장 좀비, 1.8 간호사 좀비를 추가했다(카드 그림은 3장 목록의 그림 사용, 특징 문구는 3장 목록과 같음). 2 엘리트, 2.1 엘리트 접두사, 3 보스, 3.1 보스 등장 순서를 추가했다. 목차와 개정 표(26.11.05)를 갱신했다. 사용한 빈 템플릿 2장은 삭제했다.
- 수치 기준: 사용자 결정에 따라 기본 좀비 외 몬스터 수치는 미정으로 두었다. 슬라이드에 적은 근거는 3장 목록의 특징 문구(공사장=방어형·높은 방어/넉백 저항, 연구_남=자폭형, 연구_여=원거리, 간호사=지원형), 시스템 기획서의 엘리트·보스 규칙(1.5×1.5 타일, 3분 주기 6마리, 타이머 정지), `EnemyDatatable.xlsx`의 엘리트 접두사 2개(강인한 체력 1.1, 빠른 이동속도 1.5)다. 접두사 값은 데이터 표 기재값이며 적용 방식은 미정이라고 적었다.
- 넣지 않음: 엑셀의 여성 좀비(체력 2·공격력 8·이동속도 0.1), 사무 좀비_남 체력 8, 야수 좀비 보스(체력 300·공격력 15·방어력 10). 기획서 문구 "모든 수치가 표준치"와 충돌하거나 확정 여부가 불분명해서다. 보스 순서·이름, 히든 보스, 엘리트 주기·종류도 미정.
- 불일치(고치지 않음): 1.2 카드 부제 "기본형 / 대량 생성"은 3장 목록의 "기본형 / 군집 추적"과 다르다. 1.3 카드 부제 "기본형 / 돌진 가속"은 3장의 "균형형 / 돌발 가속"과 다르다.
- 검증: 파워포인트로 15장 모두 PNG로 내보내 눈으로 확인했다. 원본은 작업 전 `C:\tmp\mon\src.pptx`에 복사해 두었다. Unity 테스트 0개. 씬, 프리팹, 코드는 변경하지 않음.
- 다음 확인: 파워포인트에서 파일을 열어(열려 있던 창은 닫고 다시 연다) 2장 목차, 5~11장 몬스터 카드, 12~15장 엘리트·보스를 본다. 불일치 두 곳과 미정 항목의 확정이 다음 작업이다.

## 2026-11-05 비투사체 약공격의 몬스터 처치 복구

- 시작 상태는 `feature/player-growth-design` / `44210ce`였다. 작업 전부터 수정된 `SkillDatatable.xlsx`와 미추적 엑셀 잠금 파일을 보존했다.
- 비투사체 `LightSkillFx.Play`가 `PoolManager.Pop`의 초기화 콜백 안에서 첫 피해를 줬다. 치명타로 HP가 0이 되어도 같은 풀에서 경험치 보석 대여는 예외, 몬스터 반납은 거절되어 사망한 적이 화면과 활성 목록에 남았다. 투사체 충돌은 초기화가 끝난 뒤 발생한다.
- `LightSkillFx`의 첫 타격을 활성화 후 첫 전투 `Update`로 옮겼다. 후속 타격은 기존 타격 횟수와 간격을 사용한다. 기존 풀의 초기화 제한을 유지했다.
- Unity 6000.5.0f1 격리 PlayMode에서 원형·고리·직선·부채꼴·호형이 모두 같은 풀 예외로 실패했다(53/58 통과). 수정 후 58/58 통과, 실패·생략 0. 다섯 판정 모두 적 사망, 비활성화, 활성 수 감소, 보석 1개 드롭을 검사했다. 프로젝트 응답 파일로 6개 어셈블리를 컴파일했으며 기존 CS0252 경고 1개가 남았다. 테스트 결과는 `.utmp/GrowthUnity/area-skill-red.xml`, `area-skill-green.xml`에 있다.
- 다음 확인은 원본 씬 수동 재생이다. Tools > Play From Title 해제 → MainTestScene Play → 레벨업에서 화염 강타나 화염창 획득 → 일시정지의 보유 스킬에서 약공격으로 장착 → 재개 후 적을 향해 `N_Attack` 또는 `Enter`. 치명 피해를 받은 적이 사라지고 경험치 보석 1개가 떨어져야 한다. 이상 시 Console 첫 오류, 장착 상태와 남은 MP, 적의 `Enemy` 태그와 Collider2D, `EnemyHealth`의 체력과 경험치 프리팹 참조를 확인한다.

## 2026-11-05 불꽃 탄환 시작 보유 제거와 기본 탄환 복구

- `feature/player-growth-design` / `35bf8c5`에서 MainTestScene과 GiungTestScene의 시작 장착을 해제하고 첫 보유 슬롯도 비웠다. 기존 보유 불꽃 탄환의 패시브 설정이 시작 자동탄을 발사하던 원인이었다. 불꽃 탄환은 이후 레벨업에서 획득해 직접 장착할 수 있다. 미추적 엑셀 잠금 파일은 보존했다.
- `PlayerCombat`에서 Player의 기존 `bulletPrefab`으로 기본 자동 공격을 복구했다. 사거리 안의 가장 가까운 적을 향해 풀에서 발사하며 `attackRange`, `attackCooldown`, `attackDamage`, `bulletSpeed`를 사용한다. 기본 자동 공격은 MP를 쓰지 않는다. 입력으로 시전하는 장착 약공격과 보유 패시브의 실행 경로는 별개다.
- 검증 사본에서 기본탄 회귀 2개가 구현 전 실패했고 구현 후 전체 53/53 통과했다. 기존 장착 모델 4/4 통과, 관련 6개 어셈블리 컴파일 성공, `git diff --check` 통과. 기존 CS0252 경고 1개가 남는다. 원본 씬 재생은 수동 확인 항목이다.
- Tools > Play From Title 해제 후 MainTestScene을 새로 재생한다. 일시정지에서 공격 스킬 보유 칸이 비어 있는지 확인한다. 재개 후 사거리 안의 적에게 접근하면 버튼 입력 없이 기본 탄환이 발사되어야 한다. 문제가 있으면 Console 첫 오류, Player의 Bullet Prefab, PlayerData의 Attack Range와 Attack Cooldown, 적의 Collider2D와 Enemy 태그를 확인한다.
- 다음 작업은 스킬 표에서 구현 조건이 정해진 패시브 또는 광역 액티브 후보를 확인하는 것이다.

## 2026-11-05 18분 스폰·처치 보석 풀·회수 보정 통합

- 사용자 요청: 다음 작업도 이어서 전체 진행. 명세의 단계3~5를 구현하고 자동 통합 검증을 실행했다. 실제 시작 `feature/player-growth-design` / `60fd1a4`. 직전 색상 코드/문서/검증 도구의 미커밋 변경을 보존했다. 브랜치 전환·커밋·PR 없음. 기존 씬/프리팹/기존meta·Project Settings·패키지·xlsx·JSON은 변경하지 않았다. 신규 Survival C#3개에만 새 meta를 생성했다.
- 책임: 기존 EnemySpawnDirector가 씬의 단일 진행 시간을 소유한다. BossEncounterController는 연결된 Director.TimeAdvanced에 구독하고 기존 보스 등장 경계를 전달한다. 보스 동안 Director만 정지하며 Time.timeScale은 멈추지 않는다. 처치 후 같은 시계 재개. EncounterTimerView는 기존 encounter.ElapsedTime을 통해 같은 시간을 표시한다. Director 없는 기존 단독 보스 씬은 로컬 시계를 유지한다. 보스 등장 테스트값30초·게임 승패는 변경하지 않았다.
- 설정: Enemy Spawn System의 EnemySpawnDirector > Settings에 `SurvivalBalanceSettings`가 직렬화된다. 기본값은30초36구간, 첫50회/구간증가10회, 프레임당최대8회, 일반활성130, XP180초별2/3/4/6/7/9, 보정 체크180/360/540/720/900초와목표19/35/51/70/87, 차이3ON/1OFF/2유지, 배율1.25다. 1차 플레이테스트용 조정값이며 최종 밸런스가 아니다. 현재시간/레벨/carry/카운트는 설정에 저장하지 않는다.
- 스폰: 목표는 씬 전체이며 기존 spawnerCount5로 곱하지 않는다. 구간의 반 간격부터 분산(첫 구간 첫 시도0.3초, 이후0.6초 간격). 기존 분별 minutes는 적 종류를 계속 제공하며, Director 경로의 수량/간격은 새 생존 설정이 소유한다. 단독 EnemySpawner.Configure/기존 standalone 경로는 보존했다. 상한/풀 실패도 시도를 소비하고, 프레임 한도를 넘긴 예정 시도는 frameSkipped로 폐기하며 보충 큐를 만들지 않는다. 1080초에는 새 일반 스폰이 중단되고 게임 승리/새 보스로 자동 전환하지 않는다. 마지막 예정 시도가1080초 이전이어도 해당 프레임이 종료 경계에 도착하면 생성 없이 frameSkipped 처리할 수 있다. 예정8100과 실제 성공 수를 구분한다.
- 풀/처치: SceneEnemyPool은 일반 생성 시 rawXP를 EnemyHealth.SpawnRawExperience에 고정한다. 실제 죽음만 기존 Managers.Pool로 XP 프리팹을 빌린다. 거리 반환/보스 진입 정리/씬 소유자 비활성에는 드롭하지 않는다. 원본 EnemyHealth 공개 API를 보존하고 일반 풀 초기화 오버로드/선택적 IEnemyExperienceDropper·IPooledExperiencePickup 경계를 추가했다. 보스는 기존 프리팹 보상량을 유지한 채 Encounter에서 같은 드롭 풀을 연결한다. Director/소유자 없는 레거시 비풀 경로는 기존 동작을 보존한다.
- 활성 정산: PooledObject.LeaseEnded 선택적 알림을 추가해 반환/풀Clear/외부Destroy에 SceneEnemyPool의 lease사전을 정산한다. ActiveEnemyCount 조회마다 전체 적 목록을 순회하지 않는다. 보스/XP는 일반130 상한에 포함하지 않으며 풀의 비활성 보관256과도 별개다. XP의 활성 소유권도 추적해 SceneEnemyPool 비활성/파괴 때 미회수 보석을 지급 없이 반납한다.
- 회수: ExperiencePickup은 IPoolable 초기화에서 collected/대상/보고콜백을 초기화하고, Pop 초기화에서 rawXP/색/대상을 받는다. TryCollect/실제 Trigger는 한번만 PlayerData.TryCollectExperience로 지급하며, 성공 후 원래 lease를 반납한다. 색은 보정되지 않은 rawXP 기준이다. 보스와 일반이 같은 프리팹을 재사용해도 원래 프리팹XP와 주입rawXP를 구분한다.
- 보정: PlayerData가 같은 씬의 Director를 한 번 찾아 시간 이벤트에 구독한다. SurvivalExperienceBonus가 체크포인트 히스테리시스와 소수carry를 소유하며 실제 회수 때만 floor 지급한다. 일반 AddEXP/기존 테스트XP 메뉴는 보정하지 않는다. 2XP 두 번+25%는2+3=5, 보정OFF에도carry보존, Lv100에서보정OFF. 이미 떨어진 보석의 rawXP/색은 수정하지 않는다. 처치율85%/회수율90% 가정을 런타임 제거/소멸 기능으로 구현하지 않았다.
- 관측: Director.Sampled/LastSample의 SurvivalWaveMetrics에30초마다 목표·실행시도·성공·상한스킵·상한외실패·프레임폐기·현재/최대활성·일반처치·XP드롭·raw회수·실지급·레벨·보정을 기록한다. Log Wave Metrics 기본false, Editor/Development에서만 CSV출력한다. 컴포넌트 메뉴 `관측/최근 30초 지표 출력`은 헤더와 최근샘플을 출력한다. `테스트/진행시간 30초 앞으로`는 실제Tick을 사용하므로 보스 정지·팝업정지·프레임8회제한을 우회하지 않는다. 빠른 시간점프의 frameSkipped를 정상 플레이 결과로 해석하지 않는다.
- 신규/핵심 파일: EnemyMvp.Runtime의 SurvivalBalanceSettings/SurvivalExperienceBonus/SurvivalWaveMetrics, EnemySpawnDirector/EnemySpawner/EnemyHealth/경계인터페이스, Encounter.Runtime의 BossEncounterController, Assembly-CSharp의 SceneEnemyPool/ExperiencePickup/PlayerData, Core의 PooledObject. 새 전역 매니저·이벤트 버스·풀·런타임 asmdef를 만들지 않았다. 구현 기록 `docs/superpowers/plans/2026-11-05-survival-integration.md`.
- 수치 검증: Run.ps1에서 기존17개+신규11개. 구현 전17/28(신규기능부재11실패)→최종 **28/28통과**. 목표합8100·경계29.999/30·XP179.999/180·종료1080·보정3/1/2·carry 보존·설정/상태분리를 실행했다.
- Unity 검증: 원본 대신 `.utmp/GrowthUnity` 검증 사본. 구현 전40개중33통과/7실패→구현후40/40. 독립리뷰의 보스 비활성 경계잔존·XP알림중재대여 오반납은49개중2실패로재현→수정후49/49. 추가 사망알림중재대여 후 old finally가 새 적을제거하는문제는51개중1실패→세대비교 수정후 **최종51/51통과, 실패/생략0, 종료0**. 실제 PoolManager/PooledObject/SceneEnemyPool/EnemyHealth/EnemyFollow/Spawner/Director/Encounter/PlayerData/PlayerView/UIManager/카드패널을 실행했다. 테스트원본과 사본 소스 해시를 비교한다. 결과 `.utmp/GrowthUnity/survival-final.xml`/`.log`는 생성물이다.
- 확인한 통합: 실제 물리 Trigger의 풀 보석 회수1회, 처치1회드롭/미회수XP0/재대여재획득, 반환·Clear·외부Destroy 카운트복구, 130상한,5스포너전체50회, 상한/풀 실패소비·무보충, 프레임급증8회이하, 보스중전투계속/시계정지·재개,1080일반스폰중단·TimeScale1, 생성XP경계스냅샷, 회수시보정/색불변, 보석→레벨업카드→선택→전투복원. 21,600회의0.05초Tick으로 가속한1080초시계 검사에서36샘플·예정8100·최고활성130을 확인했다. 이는 실제18분플레이나FPS검사가 아니다.
- 컴파일: RunCompile.ps1이 Combat.Runtime→EnemyMvp.Runtime→Boss.Runtime→Encounter.Runtime→Assembly-CSharp→Editor 순으로 설치본Roslyn컴파일. 새 Survival소스와 이번ref DLL을 연결해6개모두종료0. 출력 `.utmp/PlayerExperience/Compile` 격리, Library DLL덮어쓰기없음. 기존Player.ParryCamera CS0252경고1개 유지. 장착4/4통과. 전투표도구8/17통과·기존9실패(엑셀43/기준46·ID/JSON불일치)재확인, 관련xlsx/JSON은 수정하지 않았다. git diff --check 통과. 독립리뷰수명지적2건 수정후읽기재검토에서추가중요발견없음.
- 검증 한계: 사본은 Player의 전투/입력·실제Boss/HordeLord AI·GameManager승패판정·VFX·리소스/사운드·UI_Base바인딩을 경계로 대체한다. 실제MainTestScene 화면/터치/스킬·보스공격·실제씬언로드·카탈로그/번들/Android/기기는미검증. actual18분 처치율/회수율·체크포인트실측Lv·Lv100도달시각·FPS/GC는아직측정하지않았다. 구현과 가속시계 통과만으로16~17분100Lv를보장하지않는다.
- 사용자 확인1: Unity컴파일/임포트완료 → Console첫오류확인 → Tools > Play From Title해제 → MainTestScene Play → 일반적처치 → 파랑2XP DNA드롭 → 회수시만EXP증가 → 레벨업카드자동표시/선택 → 일시정지스탯우측검정Lv와보유등록확인. 선택중시간/적이멈추고선택완료후재개되어야한다. 보유스킬은기존대로직접장착한다.
- 사용자 확인2: 기존30초보스등장 → 일반적정리에는보석추가없음 → 타이머고정/보스와플레이어전투계속 → 보스처치후시계·일반스폰재개. 3분경계전에태어난적은2XP, 이후는3XP로색이구분되며늦게처치해도생성XP유지. 레벨업/일시정지는별도로게임시간정지. Director의Log Wave Metrics를켜30초지표와최대활성130이하를확인한다.
- 사용자 확인3: Enemy Spawn System의Settings에서시도예산·XP·체크포인트/배율/상한을조정한다(미정HP/DPS/보상추가없음). 정상18분에는타이머18:00/새일반생성중단/남은적과보석유지·임의승리전환없음. 오류시Console첫오류, Director의Pool Provider/Player/Camera/Spawner Template/Minutes적종류/Settings유효성, SceneEnemyPool활성, XP프리팹/IPooledExperiencePickup, PlayerData/슬라이더/팝업연결을확인한다. 색설정은DNA(EXP) Experience Colors에서변경한다.
- 다음 우선순위: 원본씬수동통합확인 → 실측130상한빈도·처치율/회수율 → 체크포인트레벨·100도달시각 → FPS/GC와기기. 기존전투표43/46불일치도해결대상이나 이번에임의데이터보충은하지않았다. 완료한성장/카드/색상/풀/시계/보정 구현을처음부터반복하지않는다.

## 2026-11-05 경험치 보석 색상 구간 추가

- 사용자 요청: 경험치량에 따라 보석 색을 구분하고, 임시 색/구간은 Codex가 정하되 이후 변경 가능하게 한다. 실제 시작 상태 `feature/player-growth-design` / `60fd1a4`, 작업 트리 깨끗함. 앞선 성장 구현은 사용자가 커밋한 상태이며 다시 만들지 않았다. 이번 커밋/PR 없음.
- 기존 ExperiencePickup에 `experienceColors` 구간 배열과 선택적 `pickupRenderer` 참조를 추가했다. 기본 구간은 0~3 미만 파랑 `#4DA6FF`, 3~4 미만 초록 `#52D681`, 4~6 미만 노랑 `#FFD45C`, 6~7 미만 보라 `#B58CFF`, 7~9 미만 주황 `#FF9E4A`, 9 이상 분홍 `#FF5E7E`. 현재 프리팹의1XP도 파랑이다. 수치/색은 임시 설정값이며 스킬 등급과 별개다.
- `RawExperience`와 `SetExperienceAmount(float)`를 추가했다. 값 주입과 동시에 색이 갱신되며 음수/NaN/무한값은 거절한다. 기존 Initialize(Transform)과 OnEnable도 현재 경험치량으로 색을 갱신한다. 기존 experienceAmount/attractionSpeed 필드·회수·끌림 동작과 IExperiencePickup 시그니처는 보존했다. 생성시각별 rawXP 주입·풀링/재사용 획득상태 초기화는 아직 다음 단계다.
- 색은 현재 경험치량 이하인 최소값 중 가장 큰 구간을 사용한다. 배열 순서는 무관하고 중복 최소값은 앞 항목 우선이다. null/잘못된 최소값은 건너뛰며 유효 구간이 없으면 tint흰색으로 원본을 표시한다. renderer는 지정 참조→자신→자식 순으로 찾는다. 기존 SpriteRenderer.color만 변경하고 공유 머티리얼·텍스처·셰이더를 만들거나 수정하지 않는다. 원본 DNA가 노랑/초록 그림이므로 화면색은 설정 tint와 원본색/명암이 섞인 결과다.
- 수정: ExperiencePickup.cs, 기존 RunUnity.ps1/검증README. 신규: `tools/PlayerExperienceValidation/Unity/ExperiencePickupColorTests.cs`, `docs/superpowers/plans/2026-11-05-experience-pickup-color.md`. 원본 씬·프리팹·기존 meta/GUID·ProjectSettings·Packages·xlsx·JSON 변경 없음.
- 검증: 색상 기능 구현 전 격리 Unity31개 중 기존15통과/신규16실패(기능 부재). 구현 후 **31/31 통과·실패/생략0·종료0**. 기본/경계/상한초과 색, Inspector 구간 변경/재정렬, 빈 배열, 잘못된 양 거절, Initialize/재활성 외형 복원, 자식 renderer/renderer 누락을 검사했다. 기존 카드/성장 UI15개 포함이며 실제 SpriteRenderer 색 속성으로 검증했다. 결과 `.utmp/GrowthUnity/color-green.xml`/`.log`는 생성물이다. 해당 사본 ExperiencePickup 원본 해시 일치를 확인한다.
- 독립 성장 **17/17 통과**. `RunCompile.ps1`으로 런타임/에디터 컴파일 각각 종료0, 기존 Player.ParryCamera.cs CS0252 경고1개 유지. `git diff --check` 통과. 전투 표8/17(기존9실패)·장착4/4는 앞선 세션 결과이며 이번 색상 작업에서는 해당 두 도구를 재실행하지 않았다.
- 직접 수정/확인: Play 종료 → Project > Assets/1.Prefabs/DNA(EXP) 프리팹 열기 → ExperiencePickup > `경험치별 색상` > Experience Colors 배열의 Minimum Experience/Color 수정 → 컴포넌트 메뉴 `외형/경험치 색상 미리보기`. Experience Amount를2/3/4/6/7/9로 바꾸고 메뉴를 다시 실행하면 구간별 tint가 달라져야 한다. Inspector를 바꾼 직후 자동 미리보기하는 기능은 없으며 메뉴 또는 재활성/Play 시작 때 적용한다. 임시 경험치 변경은 테스트 후 원래 값으로 돌린다. 색을 저장하려면 프리팹의 색상표 변경을 저장한다.
- 플레이 확인: Tools > Play From Title 해제 → MainTestScene Play → 적 처치 → 떨어진 현재1XP DNA의 색 구분 및 회수/기존 EXP 증가 확인. 모든 보석의 양은 아직 기존 프리팹 값이므로 시간 경과만으로 다른 색이 생기지는 않는다. 보석 풀링/시각별rawXP 연결 후 구간별 실제 드롭을 확인한다. 이상 시 Console 첫 오류, 경험치량·Minimum Experience·Color의 알파, Pickup Renderer, SpriteRenderer/머티리얼 색 지원·씬/프리팹 Overrides를 확인한다.
- 검증 한계: 실제 게임 화면/폰트·터치·머티리얼의 최종 렌더링·실제 보석 물리 회수/운영 카탈로그/Android/기기는 이번에 검증하지 않았다. 색 테스트는 실제 SpriteRenderer 속성 검사이며 시각 검증으로 설명하지 않는다. 다음 우선순위는 기존과 같이 3단계 처치→보석→회수다.

## 2026-11-05 레벨·XP 계산과 카드 자동 연결·상태창 레벨 표시

- 사용자 승인: 요구 XP만 차감하고 초과분 이월. 이번에는 18분 명세의 첫 두 단계(계산/기존 카드 자동 연결)와 추가 요청인 스탯 제목 오른쪽 검정색 `Lv.1` 표시를 구현했다. 시작 실제 상태는 `feature/player-growth-design` / `5d34dad`, 작업 트리 깨끗함. 브랜치 전환·커밋·PR은 하지 않았다.
- 읽은 자료: NextCodex-LevelSpawnXP, Handoff, CoreInitialization, ResourceLoading, 기존 카드 계획, Markdown 명세 전체와 루트 ImplementationSpec DOCX 텍스트. 두 명세의 핵심 규칙 차이 없음. 아래 11월4일의 “성장 런타임 미구현”은 당시 기록이며 이번 첫 두 단계에는 더 이상 해당하지 않는다.
- 계산: `PlayerExperienceSettings`가 Inspector에서 조정 가능한 정수 공식 계수를, `PlayerExperienceProgress`가 Lv1~100·잔여 XP·선택 대기를 소유한다. 새 전역 매니저나 asmdef 없음. 기본 Lv1 요구13, Lv2 요구15, 1→100 합33,033. 15XP를 얻으면 Lv2/잔여2, 30XP면 Lv3/잔여2/선택2회. 최대 레벨에서는 추가 선택 없이 남는 XP를 보존한다. 요구량0에서 EXP Slider는 기존 표시 규칙대로0이다. 최대 레벨 보상은 추가하지 않았다.
- PlayerData: AddEXP/SetEXP/EXPChanged와 기존 직렬화 필드명을 보존했다. CurrentLevel/PendingLevelUps/LevelChanged/TryCompleteLevelUp을 추가했다. 실제 요구량은 공식이 소유하므로 `maxEXP` Inspector 구값은 시작 시 요구13으로 갱신된다. `SetEXP(current, maximum)`의 maximum은 유효성 검사만 유지하는 호환 인자이며 요구량을 덮어쓰지 않는다. current는 현재 레벨 XP 지정이며 초과 시 정상 성장한다. 기존 호출 검색에서 외부 런타임 호출은 없었다. InitEXP는 Lv1/0XP/선택0으로 초기화한다. 소수 AddEXP 호출 호환은 유지하며 후속 +25% 보정은 아직 연결하지 않았다.
- 카드: CombatPopupController가 같은 씬 PlayerData에 구독하고 XP 획득 후 자동으로 기존 ShowLevelUp을 연다. 대기 선택마다 기존 약공격 추첨·보유 등록·보유 제외·미장착 정책을 사용한다. 카드 소진/가득 참 확인도1건 완료다. 같은 창에서 다음 프레임에 선택지를 교체해 같은 프레임 중복 클릭/확인과 중간 전투 재개를 막았다. 자동 선택은 일반 닫기 버튼으로 건너뛸 수 없다. 수동 미리보기는 대기 선택을 소비하지 않는다.
- 수명: 컨트롤러 비활성은 구독/코루틴/창을 정리하고 대기는 PlayerData에 남겨 재활성 때 재개한다. 사망/승패 종료는 대기를 취소하고 전투 정지를 복원하지 않는다. 비활성 중 승패 후 재활성 시에도 대기를 정리한다. 완료창 위에 일시정지창이 있는 경우 완료창을 비활성화해 기존 UIManager의 지연 스택 정리를 이용하며 나머지 창의 정지는 유지한다. 보스전/진행 시간 규칙은 변경하지 않았다.
- 표시: PlayerStatusPanel의 기존 스탯 제목 영역을 나눠 오른쪽에 검정색 `Lv.{현재레벨}`을 추가했다. 기존 EXPChanged 경로로 갱신하며 씬/프리팹을 재저장하지 않았다. 기존 meta/GUID·Project Settings·패키지·xlsx·JSON 변경 없음. 새 C# 두 파일에만 새 meta를 추가했다.
- 검증 도구: `tools/PlayerExperienceValidation/Run.ps1`, `RunUnity.ps1`, `RunCompile.ps1`, README와 검증 소스를 추가했다. 원본 Assets/Tests/asmdef는 복원하지 않았다. 구현 전 성장 검증14개 중13개 실패(레벨/요구량/선택 기능 부재), 구현 후 최종 독립17/17 통과. 장착 도구4/4 통과.
- Unity: `.utmp/GrowthUnity` 격리 프로젝트에서 Unity6000.5.0f1 PlayMode 실행. 검증 경계의 Canvas null비교/SetGuard 누락을 수정한 뒤 기능 구현 전9/9실패→구현 후9/9통과. 추가 중첩 사례12개 중1실패→수정 후12/12. 추가 수명 사례15개 중1실패(비활성 중 승리 후 대기 잔존)→순서 수정 후 **최종15/15 통과, 실패/생략0, 종료0**. 실제 PlayerView EXP Slider 이월비율, 실제 UIManager/카드 패널의 자동 열기·획득·연속 선택·중복 클릭/확인·소진/가득 참·중첩 양방향·비활성 재개·사망/승패·초기화·재부모화 팝업 Destroy·검정색 레벨 텍스트 갱신을 확인했다. 사본에 복사한 관련 실제 런타임 소스는 원본과 해시를 비교한다. 결과 `.utmp/GrowthUnity/final-lifecycle.xml`/`.log`는 생성물이다.
- 컴파일: Unity 설치본 Roslyn으로 현재 원본 소스의 Assembly-CSharp와 Editor 각각 종료0. 새 성장 소스를 포함하고 Editor는 이번 런타임 ref DLL을 참조한다. 출력은 `.utmp/PlayerExperience/Compile`로 격리하여 Library DLL을 덮어쓰지 않았다. 기존 Player.ParryCamera.cs CS0252 경고1개가 남는다. `git diff --check` 통과. 독립 리뷰에서 중요 추가 발견 없음; 이후 수명 검증을 보강했다.
- 기존 데이터 실패: CombatSkillTable 도구는 **8/17 통과·9실패**로 인수인계에 기록된 43개/기준46개 불일치가 그대로다. 이번에 엑셀·JSON을 재출력/보충하지 않았다. 실패 항목은 가득 참 획득 fixture, 설명 토큰, 약공격 수, 재생, 강공격, 빈 자원, 범위 분리, inline문자열, JSON 비교다. 이 실패를 새 성장 테스트 통과와 혼동하지 않는다.
- 한계: Unity 사본은 Player 전투/입력·GameManager 승패 판정·방어 VFX·리소스/사운드·UI_Base 바인딩을 경계로 대체했다. 실제 GameManager.LateUpdate/Player 입력 Submit·터치, 실제 씬 언로드·화면 해상도/폰트 시각 확인, 운영 카탈로그/번들, Android/기기는 미검증. 객체 Destroy 검사를 실제 씬 전환 전체 검사로 설명하지 않는다.
- 직접 확인1: Tools > Play From Title 해제 → MainTestScene Play → CombatUI의 CombatPopupController 메뉴 `테스트/경험치 13 획득`. 새 판에서 Lv2/잔여XP0으로 카드가 자동으로 열려야 한다. 카드 본문 선택 → 창 닫힘/전투 재개 → 일시정지 → 스탯 오른쪽 검정색 `Lv.2` 및 보유 스킬 등록 확인. 새 스킬은 자동 장착되지 않으며 클릭 장착 후 N_Attack/Enter로 사용한다.
- 직접 확인2: Play를 재시작하고 같은 메뉴 `테스트/경험치 30 획득 (연속 레벨업)` → Lv3/잔여XP2/요구17, 두 번 선택해야 한다. 첫 선택 뒤 전투가 재개되면 안 되고 마지막 선택 뒤 재개되어야 한다. 보유창을 채운 뒤 같은 메뉴로 가득 참 확인의 연속 처리도 확인한다. `테스트/카드 소진 안내`는 기존 수동 표시 확인용이다. 실제 보석은 현재 Inspector 경험치량이 양수인 기존 보석으로 회수→자동 표시를 확인한다(시각별 rawXP/풀링은 다음 단계).
- 오류 확인: Console 첫 오류 전체, CombatPopupController의 Status Player/두 Popup/ContentRoot/버튼과 PlayerData 연결, Player의 EXP Slider, 보석 experienceAmount 양수 여부를 확인한다. 레벨 글씨는 가로가 좁은 해상도와 Lv100까지 잘리는지 직접 확인한다. 스킬 후보 이상은 기존 엑셀/JSON 불일치와 Skill_Rarity 설정도 확인한다.
- 다음 우선순위: 실제 씬 수동 확인 후 **3단계 처치→보석→회수(생성 시 rawXP 스냅샷/기존 Managers.Pool/획득1회/재사용 초기화)**. 이어 단일 진행 시계·30초 스폰 예산·18분 일반스폰 중단·성장 보정/소수carry·관측을 진행한다. 첫 두 단계와 카드 UI/획득을 다시 만들지 않는다. 설계/실행 기록은 `docs/superpowers/plans/2026-11-05-player-experience.md`.

## 2026-11-05 Player·Boss Capsule Collider 2D 교체 호환성

- 요청: 사용자가 Player·Boss의 Box를 Capsule로 교체하기 전 호환성을 확인하고 장애 코드 수정. 실제 콜라이더 크기·방향·씬·프리팹 교체는 하지 않았다. 시작 상태 `main` / `9d740cc`, 직전 EnemyMvp·ZombieHound Trigger 및 문서 변경을 모두 보존했다.
- 확인/수정: BossController·HordeLordBoss가 요구하는 EnemyFollow가 BoxCollider2D를 강제하고 있었다. Capsule을 먼저 붙여도 EnemyFollow 추가 시 Box가 자동 추가되는 현상을 Unity에서 재현했다. EnemyFollow의 RequireComponent에서 Box만 제거하고 Rigidbody2D 요구는 유지했다. 기존 일반 적의 Box Trigger·Circle 몸체는 그대로 유지된다. 새 적을 구성할 때는 몸체용 비 Trigger Collider2D를 직접 붙여야 한다.
- 경로 조사: PlayerMovement의 일반 이동·대시·밀림과 EnemyFollow/Boss 이동은 CollisionMovement2D의 Collider2D.Cast/Distance를 사용한다. Boss 공격·Player 피격/패링·투사체·EnemyHealth도 몸체의 Box 타입을 요구하지 않는다. LightSkillFx의 Circle 참조는 스킬 이펙트 자체 것으로 이번 몸체 교체 대상이 아니다.
- 회귀 검증 소스: `Assets/Tests/EnemyPhysics/CapsuleColliderCompatibilityTests.cs`(meta 포함). 별도 최소 Unity 6000.5.0f1 프로젝트에서 원본 EnemyFollow·CollisionMovement2D를 복사하고 런타임/테스트 asmdef를 사본에만 생성해 EditMode 실행. 검증 사본의 최초 테스트 구성 오류(Editor 전용 어셈블리에 런타임 컴포넌트를 넣음)를 고친 뒤 수정 전 4/6 통과·2 실패(Box 자동 추가), 수정 후 6/6 통과·실패/생략 0. 세로/가로 Capsule 벽 차단·이탈, Trigger 장애물 통과, Capsule EnemyFollow 이동, 불필요한 Box 부재, 기존 Circle+Box Trigger 이동을 확인했다. 검증한 두 런타임 파일의 SHA-256이 원본과 일치한다. 최종 사본 C# 오류·경고 0, 종료 코드 0. 원본 테스트 asmdef/런타임 어셈블리는 변경하지 않았다.
- 결과 파일은 `.utmp/CapsuleValidation/green.xml`·`green.log`(생성물/Git 제외). 원본 프로젝트 전체 컴파일·전체 테스트·실제 Player/Boss 애니메이션/패링·씬 플레이·Android/기기는 이번에 검증하지 않았다. 최소 프로젝트 결과를 전체 게임 검증으로 해석하지 않는다.
- 사용자 교체 순서: Play 종료 → 컴파일 완료 → 기존 Box의 Size·Offset·Material·레이어 설정 기록 → 같은 Rigidbody2D가 있는 Player/Boss 루트에 Capsule Collider 2D 추가 → Is Trigger 끄기 → 크기·오프셋·방향 설정 → 몸체 Box 제거. Boss에 별도 Circle Trigger가 있으면 보존한다. 기존 Rigidbody2D도 보존한다. CollisionMovement2D와 EnemyHealth가 초기화 때 콜라이더를 캐시하므로 실행 도중 교체하지 않는다. 보스는 기본 프리팹뿐 아니라 씬 인스턴스의 Overrides도 확인한다.
- 직접 검증: Tools > Play From Title 해제 → MainTestScene Play → Player 이동·벽/보스와 접촉·대시 → 보스 등장 후 추적·돌진·할퀴기·E 패링 → 스킬 피격·처치/재스폰 확인. 정상은 몸체 통과 없이 이동하고 피해/패링/사망 처리가 유지되는 것. Capsule의 둥근 끝 때문에 Box와 모서리 접촉 위치는 달라질 수 있다. 몸체를 크게 늘리면 중심 거리 기반 보스 공격 거리 밖에서 막힐 수 있으므로 기존 Size·Offset 기준부터 조정한다. 이상 시 Console 첫 오류, 중복 비 Trigger 몸체, Capsule Is Trigger/활성 상태, Direction·Size·Offset, 레이어 충돌과 Overrides를 확인한다.
- 다음 작업: 사용자가 실제 Capsule 크기/방향으로 교체한 뒤 위 플레이 검증. 기존 데이터 표 불일치 등 구현 우선순위는 유지한다.

## 2026-11-05 Box 감지·Circle 몸체 역할 적용

- 사용자 요청에 따라 `Assets/1.Prefabs/EnemyMvp.prefab`·`ZombieHound.prefab`의 BoxCollider2D만 `m_IsTrigger: 1`로 변경했다. CircleCollider2D는 `m_IsTrigger: 0` 유지. 크기·오프셋·GUID·파일 ID·meta·씬·런타임 코드는 변경하지 않았다. 직전 조사 문서 변경을 보존했다. 시작 상태는 `main` / `9d740cc`다.
- 기존 CollisionMovement2D가 Trigger를 이동 검사에서 제외하므로 Circle이 몸체 충돌을 담당한다. EnemyContactDamage는 기존 Trigger/Collision Stay 경로와 공통 피해 간격을 유지한다. 기존 공격 코드는 Collider 타입을 한정하지 않아 Box 외에 Circle도 피격 검색 대상이 된다. Box만 피격 대상으로 강제하는 변경은 하지 않았다. `docs/EnemyMvp.md`의 역할 설명도 현재 설정으로 수정했다.
- 검증: 두 프리팹의 Box Trigger/Circle 비 Trigger·활성 상태 정적 확인, 프리팹 diff가 각 Is Trigger 한 줄뿐임을 확인. Assets/9.Scenes의 m_IsTrigger 프리팹 오버라이드 검색 0건. Unity 테스트 0개, 컴파일·에디터 임포트·Play 동작 미검증. 신규 테스트 코드 없이 설정을 직접 검사했다.
- 직접 확인: Unity 임포트 완료 → Project > Assets/1.Prefabs > 각 프리팹 더블클릭 → Box Collider 2D의 Is Trigger 켜짐·Circle은 꺼짐 확인. Tools > Play From Title 해제 → MainTestScene Play → EnemyMvp에 접근해 몸체 통과 방지·접촉 피해·공격 피격 확인. ZombieHound는 테스트 씬에 배치해 같은 순서로 확인한다. 플레이어 쪽 무적/피해 간격도 있으므로 최종 HP 변화 주기를 적의 0.5초 설정만으로 판단하지 않는다. 오류 시 Console 첫 오류·씬 인스턴스 Overrides·콜라이더 활성 상태·플레이어 피해 수신 설정을 확인한다.
- 남은 작업: 위 실제 플레이 검증. 기존 구현 우선순위·데이터 표 불일치 후속 작업은 유지한다. 아래 조사 섹션의 “둘 다 비 Trigger”는 적용 전 상태 기록이다.

## 2026-11-05 EnemyMvp·ZombieHound 이중 콜라이더 조사

- 시작 상태: `main` / `9d740cc`, 작업 트리 깨끗함. 조사만 요청받아 코드·프리팹·씬은 변경하지 않았다.
- 원래 역할은 CircleCollider2D의 공격/접촉 감지 Trigger와 BoxCollider2D의 몸체 충돌 분리다(`docs/EnemyMvp.md:19`, 최초 프리팹 커밋 `d20210e`). 그러나 `803dac7`에서 EnemyMvp 원형의 Is Trigger가 1→0으로 변경됐고, ZombieHound는 최초 추가 `a94be70`부터 두 콜라이더가 모두 비 Trigger다. 변경 이유를 역할 재설계로 확정할 근거는 없다.
- 현재 EnemyMvp는 박스 0.16×0.16·원 반지름 0.13, ZombieHound는 박스 0.32×0.32·원 반지름 0.16이며 둘 다 같은 루트, 오프셋 0, 활성 상태다. EnemyFollow는 BoxCollider2D를 RequireComponent로 요구하고 CollisionMovement2D는 모든 비 Trigger 콜라이더를 이동 검사에 사용한다. EnemyContactDamage는 Trigger/Collision Stay 모두 처리하며 공통 nextDamageTime을 사용한다. 현재는 두 모양 모두 몸체 충돌·접촉 판정에 참여하는 구조로, 문서의 역할 분리와 다르다.
- 검증: 프리팹 YAML·관련 코드·Git 이력 정적 조사. 이번 Unity 실행/컴파일/자동 테스트 0개. 다음 변경은 요청 시 현재 이동·접촉·피격 판정을 검증한 뒤 결정한다. 기존 데이터 표 불일치 등 다음 구현 우선순위는 그대로다.
- 직접 확인: Project > Assets/1.Prefabs > 각 프리팹 더블클릭 > 루트 Inspector의 Circle/Box Collider 2D 확인. 현재 정상 관찰값은 둘 다 활성·Is Trigger 해제이며 크기는 위와 같다. 씬 인스턴스가 다르면 Inspector의 Overrides와 Console 첫 오류를 확인한다. 조사 결과는 실제 Play 동작 검증을 대신하지 않는다.

## 2026-11-05 main에 보스·스킬 표 브랜치 병합

- 시작: `main` / `6d42f15`, 작업 트리 깨끗함. `git fetch origin` 후 origin/main과 일치 확인. 요청한 원격 끝점은 BossBeastZombie `d44c94c`, feature/skill-data-table `3c1c3d4`다. 원격 push와 브랜치 삭제는 하지 않는다.
- 보스 병합: `f60b46b`. main의 할퀴기·돌진 VFX를 보존하고 보스 브랜치의 공격 애니메이션 중단/종료 복구·추적 복귀·돌진 애니메이션과 패링 히트스톱/밀림/카메라 연출을 함께 반영했다. GiungTestScene의 main 최신 객체들을 보존하면서 폐기 BossParry 참조만 제거했다. main에서 삭제한 _Recovery/0.unity와 0 (1).unity는 삭제를 유지한다.
- 스킬 병합 충돌: 데이터 표 기반 TryUseLightSkill 경로와 main의 강공격 중 입력 방지를 함께 유지했다. MainTestScene의 보스 콜라이더 크기/오프셋과 스킬 브랜치의 타일링 속성을 모두 보존했다. 보스 등장 시간은 두 브랜치의 서로 다른 테스트 값(1초/60초) 대신 작업 전 main의 30초를 유지했다. 이는 게임 정식 기획 수치 확정이 아니다. 양쪽 인수인계의 고유 제목 섹션을 보존했다.
- 데이터 검증: 장착 모델 도구 4/4 통과. 스킬 표 도구는 현재 main 엑셀에서 8/17 통과·9 실패(기준 46개 대비 실제 43개 및 검증 대상 ID 누락/JSON 불일치). 동일한 병합 검증 실행 파일에 feature/skill-data-table 원본 엑셀·JSON을 입력하면 17/17 통과. 현재 main의 SkillDatatable.xlsx를 덮어쓰지 않았다. 다음 작업은 현재 엑셀의 의도한 스킬 목록과 생성 JSON/검증 기준을 맞추는 것이다. 실패를 숨기기 위한 재출력이나 데이터 보충은 하지 않았다.
- 정적 검증: 충돌 표시 및 폐기 BossParry 타입/GUID 잔존 0건. MainTestScene 233개·GiungTestScene 233개·BossMvp 13개 YAML 객체 ID 중복 0건. 코드·문서 whitespace 검사 통과(빈 YAML 값의 Unity 생성 공백은 보존; 전체 기본 diff --check에는 유입 브랜치의 해당 공백 경고가 있다).
- Unity 검증: 별도 짧은 경로의 사본에서 Unity 6000.5.0f1 C# 컴파일 성공. 원본 프로젝트는 Unity로 열거나 저장하지 않았다. 사본에만 테스트 asmdef 두 개와 PlayFromTitle 배치 가드를 넣었다. 기존 TouchGizmoOverlay.cs CS0618 및 유입 Player.ParryCamera.cs CS0252 경고는 남아 있다. PlayMode 결과는 아래 최종 검증 항목을 참조한다.
- 최종 PlayMode: 73/73 통과, 실패 0/생략 0, 약 9.36초. 첫 실행 72/73의 실패는 LightSkillCastTests가 미장착 자동 발사를 기대하던 구동작 테스트였다. feature 브랜치의 명시적 장착 정책을 확인한 뒤 이 테스트를 미장착 거절/MP 보존 → 장착 발사/표 MP 소비/입력 잠금 해제 → 장착 해제 후 거절/MP 보존 순서로 갱신했다. 런타임 동작은 테스트에 맞춰 되돌리지 않았다. 최종 사본 코드·테스트 해시 일치 확인. 자료는 `C:/tmp/sbs-merge-20261105/final.xml`·`final.log`(생성물, Git 제외)다. 데이터 도구의 9개 실패는 여전히 남아 있으며 이 PlayMode 통과와 별개다.
- 직접 확인: Unity 임포트 완료 → Console 첫 오류 확인 → Tools > Play From Title 해제 → MainTestScene Play → 일시정지에서 보유 약공격을 장착하고 복귀 → N_Attack/Enter 시 장착 스킬 발사·MP 차감 확인 → 레벨업 카드 선택 후 보유 등록 확인 → 30초 보스 등장 후 돌진/할퀴기·E 패링과 카메라 복귀 확인. 공격 없이 E만 누르면 성공 연출이 없어야 한다. 오류 시 Console 첫 오류, Player 입력 에셋·MP·장착 상태, Boss.controller 연결과 스킬 JSON/엑셀 불일치를 확인한다.
- 남은 검증: 원본 씬 실제 UI/터치·그래픽 확인, 운영 카탈로그·번들, Android 빌드/기기는 미검증이다. 브랜치 이력의 과거 테스트 수는 이번 결과로 재사용하지 않는다.
## 2026-11-04 다음 Codex용 18분 성장·스폰 명세 인수인계

- 사용자 요청은 시간이 부족하므로 다음 Codex에게 넘길 문서 작성이다. [NextCodex-LevelSpawnXP.md](NextCodex-LevelSpawnXP.md)에 원본 명세 위치, 승인된 카드/슬롯 동작, 실제 코드 연결 지점, 단계별 권장 작업, 시간/보스·다중 선택·어셈블리 주의점, 검증 기준과 다음 채팅 시작 문구를 정리했다.
- 실제 상태 확인: 브랜치 `feature/Player-Growth-Design`, HEAD `521ef10` (`레벨 업 패널에서 스킬 선택, 획득`). 시작 시 미추적 원본 Markdown 명세와 루트 DOCX를 보존했다. 다음 세션에서는 Git 상태를 다시 확인한다.
- 문서 작성 종료 시 PowerPoint 잠금 파일 `~$씬 별 UI.pptx`가 Git에서 D 상태로 관찰됐다. 이번 작업의 파일 조작 대상은 인수인계 문서 두 개뿐이며 잠금 파일 상태는 그대로 보존한다.
- 원본 `docs/VampireSurvivor_18min_LevelSpawnXP_Codex_Spec_v0.1.md`를 전부 읽고 일부 기존 코드를 조사했다. 요구 XP 총33,033·스폰 목표8,100·가정상 획득XP38,762.55는 직접 산술 검산으로 일치했다. 루트 DOCX 비교는 수행하지 않았다.
- 이번 변경은 인수인계 Markdown만이다. 18분 명세 런타임 구현·자동 테스트·컴파일·Unity 플레이는 이번 문서 작업에서 실행하지 않았다. 기존 검증17/17·4/4는 과거 구현 결과이며 새 검증으로 재사용하지 않는다. 커밋·PR 없음.
- 다음 우선순위: 사용자가 재개하면 인수인계와 원본 명세를 읽고 레벨/XP 계산→기존 카드 자동 연결부터 진행한다. 현재 별도인 보스/스폰 시간을 조사해 단일 시간 권위를 정하되 기존 보스전 진행 정지와 전투 정지를 구분한다.

## 2026-11-04 레벨업 카드 선택 획득·후보 제외·소진 안내

- 시작 상태: `feature/Player-Growth-Design`, HEAD `3c1e8e7` 위의 직전 가중 추첨 미커밋 변경과 사용자 PowerPoint 잠금 파일을 보존했다. 사용자 승인: 선택한 약공격은 일시정지 보유창에 등록하고 보유 스킬은 추첨에서 제외한다. 후보가 없으면 제목 `카드 없음`, 내용 `카드가 소진되었습니다.`로 임시 표시한다. 슬롯 가득 참은 별도 질문에 ‘가득 참 안내 후 확인으로 종료’를 선택했다.
- 완료: `LevelUpSkillReward.TryAcquire`가 표의 ID/이름/설명/아이콘 키를 기존 Data.SkillData 보유 항목으로 등록한다. `PlayerLoadout.TryAddAttackSkill`의 분류·중복·빈 슬롯 제한과 Changed 알림을 사용한다. 자동 장착은 하지 않으며, 일시정지 창에서 새 보유 약공격을 클릭해 Light 버튼에 장착할 수 있다. 표에 없는 패시브 주기/피해·애니메이션 설정은 만들지 않았고 액티브 실행은 기존 ID 기반 CombatSkillTable 경로를 사용한다.
- 완료: 팝업을 새로 열 때 현재 보유 공격 스킬 ID 집합을 추첨기의 제외 인자로 전달한다. 이후에도 소유 중인 스킬은 나오지 않는다. 후보가 1~2개면 있는 만큼 표시한다. 후보 0개는 `카드 없음 / 카드가 소진되었습니다.` 안내와 확인을 표시하며, 확인 시 기존 CompleteLevelUp으로 닫고 전투를 복원한다. 실제 수치 대체 보상은 지급하지 않는다.
- 완료: 슬롯이 가득 차면 `스킬 슬롯 가득 참 / 공격 스킬 슬롯이 가득 찼습니다.` 안내와 확인을 표시한다. 표시 중 슬롯이 차는 경우에도 기존 항목을 덮어쓰지 않고 같은 안내를 표시한다. 성공한 선택만 즉시 보유 등록 후 닫는다. 중복 선택/확인은 뷰의 가드로 한 번만 처리한다. 선택 중 이미 보유된 항목 등 등록 실패는 실패 안내 후 확인으로 종료한다.
- 검증: 신규 획득·가득 참·제외/소진 검증 3개는 구현 전 부재로 실패했고, 구현 후 통과했다. 추가 통합 검증은 실제 표에서 추첨→획득→보유 ID 제외→다음 추첨→Light 장착까지 실행했다. `tools/CombatSkillTableValidation/Run.ps1` 17/17, 기존 `tools/PlayerLoadoutValidation/Run.ps1` 4/4 통과. Unity 6000.5.0f1 설치본 Roslyn으로 신규 소스를 포함해 Assembly-CSharp와 Assembly-CSharp-Editor 각각 종료 코드 0, C# 오류·경고 0. 출력은 `.utmp/LevelUpAcquisition`으로 격리했다. `git diff --check` 통과.
- 검증 한계: 실행한 Unity 테스트 0개. 실제 에디터 임포트·카드 클릭·일시정지 표시·확인 버튼·겹친 팝업·Android/기기는 미확인이다. 씬·프리팹·기존 meta·Project Settings·xlsx·패키지를 변경하지 않았다. Assets/Tests/asmdef를 복원하지 않았다.
- 독립 리뷰: 선택 중복, 획득 실패, 정지/복원 연결, 후보 제외, 안내 확인, 기존 명시적 선택지 오버로드를 검토했으며 중요 지적 없음. 화면·입력 실행 검증을 대신하지 않는다.
- 사용자 직접 확인: MainTestScene/GiungTestScene에서 Play From Title 끄고 Play → CombatUI 선택 → CombatPopupController 컴포넌트 메뉴 `테스트/레벨업 팝업 열기` → 약공격 카드 본문 클릭. 팝업이 닫히고 전투가 재개되어야 한다. 일시정지에서 선택한 스킬이 빈 보유 칸에 나타나야 하며 클릭 장착 후 N_Attack/Enter로 해당 약공격을 사용할 수 있어야 한다. 다시 레벨업 팝업을 열면 기존 보유·선택 획득 스킬은 제외되어야 한다. 등록을 반복해 기본 6칸이 차면 가득 참 안내와 확인을 확인한다.
- 소진 안내 직접 확인: 재생 중 레벨업 창을 닫은 상태에서 같은 컴포넌트 메뉴 `테스트/카드 소진 안내` → 정확한 제목/내용 확인 → 확인 버튼으로 전투 복원. 이 메뉴는 안내 화면만 미리 보는 기능이다. 실제 후보 0개 반환은 독립 테스트로 검증했으며 현재 원본 표는 빈 슬롯보다 스킬 수가 많아 보통 소진 전 슬롯 가득 참 안내가 먼저 뜬다.
- 오류 확인: Console 첫 오류, Controller의 Status Player 또는 같은 씬 Player/PlayerData, 현재 보유 슬롯 수를 확인한다. 등록했는데 버튼이 발동하지 않으면 일시정지에서 장착 여부·MP·CombatSkillTable JSON을 확인한다. 카드 없음이 예상보다 일찍 뜨면 Skill_Rarity 양수 가중치와 후보의 보유 여부를 확인하고 표 변환 후 Play를 다시 시작한다.
- 다음 작업: 실제 Unity에서 선택→일시정지 등록→장착→발동 및 반복 열기를 확인한다. 실제 EXP/레벨업 발생 이벤트, 수치 대체 보상, 강공격 구현은 아직 별도다. 현재 보유 항목을 제거하면 다시 후보가 될 수 있으며 영구 획득 이력은 추가하지 않았다.

## 2026-11-04 약공격 레벨업 카드 가중 추첨

- 시작 상태: 실제 브랜치 `feature/Player-Growth-Design`, HEAD `3c1e8e7`. 미추적 PowerPoint 잠금 파일은 사용자 생성물이라 그대로 보존했다. 사용자 요청은 SkillDatatable.xlsx/Skill_Rarity 가중치로 스킬을 랜덤 표시이며, 이번에는 약공격만·향후 강공격도 연결하도록 확인했다.
- 완료: 기존 전투 표 변환기에 Skill_Rarity C~G열의 등급 ID·이름·실수 가중치·프레임 키·색을 추가했다. JSON은 기존 46개 스킬 필드/값을 보존하고 등급 7개를 추가했다. 가중치는 60/23/11/4/1.7/0.3/0이며 시너지(Fusion)는 추첨되지 않는다. 엑셀의 Mystic 스킬 표기는 등급표 Mythic으로 매칭한다. 엑셀 원본은 변경하지 않았다. 음수·NaN·무한/비숫자 가중치나 중복 등급은 변환 오류로 처리한다.
- 완료: `LevelUpSkillDraft.Draw`는 먼저 후보가 남은 등급을 가중 추첨하고 그 등급 안에서 스킬을 균등 추첨한다. 동일 묶음의 스킬 ID는 중복되지 않는다. 가중치 0/알 수 없는 등급은 제외하고 후보 부족 시 있는 만큼 반환한다. 기본 호출은 10000 이상 20000 미만의 약공격 최대 3장이다. 범위 인자를 받아 향후 강공격에도 재사용 가능하다.
- 완료: 새 `ShowLevelUp()` 호출은 실제 표의 약공격 이름·등급·색·AF열 툴팁(토큰을 수치로 치환)·AG열 설명을 카드에 표시한다. 고정 예시 3장은 제거했다. 이미 열린 창 재요청/일시정지 복귀는 기존 카드를 유지하며, 완료 후 다시 열 때 새로 추첨한다. 표에 없는 패시브 설명은 만들지 않았다. 아이콘 키가 빈 현재 스킬은 기존 빈 아이콘 자리를 사용한다. 보유 스킬의 재등장·획득·강화 규칙은 이번에 확정하지 않았으며 **현재 기본 무작위 카드 경로는 추첨/조회만 한다. 실제 선택·획득과 레벨업 발생 이벤트는 아직 미연결**이다. 기존 명시적 선택지/콜백 오버로드는 보존했다.
- 검증: 구현 전 신규 4개가 등급/추첨기 부재로 실패하고 기존 7개는 통과했다. 구현 후 `tools/CombatSkillTableValidation/Run.ps1` 전체 13/13 통과(실제 등급 7개·0.3 가중치·색, 등급 우선 경계, 등급 내 균등, 중복/0/미등록/강공격 제외, 후보 소진, Mystic 호환, 설명 토큰, 기존 스킬 모든 필드 대조 포함). `-Export`로 등급 데이터를 생성했고 기존 스킬 숫자 표기까지 보존해 JSON 변경은 등급 추가 51줄이다. 장착 모델 검증도 4/4 재실행 통과.
- 검증: Unity 6000.5.0f1 설치본 Roslyn 및 기존 응답 파일에 신규 소스를 포함해 Assembly-CSharp와 Assembly-CSharp-Editor 각각 종료 코드 0, C# 오류·경고 0. 출력은 `.utmp/WeightedLevelUp`에 격리했다. 독립 코드 리뷰에서 중요 지적 없음. `git diff --check` 통과. 실행한 Unity 테스트 0개. 실제 임포트·Unity 메뉴·팝업 조작·Android/기기는 미검증이다.
- 보존: 씬·프리팹·기존 meta·Project Settings·패키지·xlsx는 변경하지 않았다. 신규 추첨기 C#과 meta만 추가했다. Unity 테스트 asmdef는 복원하지 않았다.
- 사용자 직접 확인: Play 종료 → 엑셀 저장 → Unity `Tools > Parse Combat Skill Table` → Console `전투 스킬 46개와 등급 7개` 확인 → MainTestScene/GiungTestScene에서 Play From Title 끄고 Play → CombatUI의 CombatPopupController 컴포넌트 메뉴 `테스트/레벨업 팝업 열기`. 서로 다른 약공격 카드 3장이 표의 이름·등급·색·설명으로 떠야 한다. 같은 창 열기를 다시 눌러도 카드가 유지되어야 한다. `테스트/레벨업 선택 완료` 후 다시 열기를 여러 번 반복하면 새로운 추첨 결과가 표시될 수 있다(무작위이므로 같은 조합도 가능). 강공격·전략·Fusion은 나오지 않아야 하며 i로 표 설명을 확인한다.
- 오류 확인: Console 첫 오류와 JSON의 rarities 배열 확인. `레벨업에 표시할 약공격이 없습니다`이면 변환 메뉴 재실행 후 Play를 다시 시작한다. 표 수정은 실행 중 캐시를 자동 갱신하지 않으므로 Play 종료→변환→다시 Play 순서로 확인한다. 확률 수치를 몇 번의 육안 추첨 빈도로 판단하지 않는다.
- 다음 작업: 실제 팝업 표시·반복 열기 검증. 실제 레벨업 이벤트·선택 획득/보유 중복/슬롯 가득 참 정책은 별도 연결이 필요하다. 강공격 후보 확장은 이후 사용자 요청 시 같은 추첨기의 ID 범위를 사용한다.

## 2026-11-04 레벨업 패널 참고 이미지 적용

- 시작 상태: 브랜치 `feature/Player-Growth-Design`, HEAD `b55f733`. 직전 약공격 장착 관련 미커밋 변경을 모두 보존했다. 기존 레벨업 프리팹은 제목과 빈 ContentRoot만 있고 레벨업 이벤트/보상 선택은 아직 미연결이다.
- 완료: `LevelUpSelectionPanel`이 기존 레벨업 ContentRoot에 남색 배경, 청록 제목, 가로 3장 카드, common/Uncommon/Rare 테두리색, 아이콘 자리, 이름·등급, 액티브 N·패시브 구획, 설명·하단 문구·정보 버튼을 생성한다. 정보 버튼은 해당 카드에 제공된 텍스트를 상세 창에 표시하고 닫기를 제공한다. 실제 이미지 아이콘은 도안처럼 빈 자리이며 새 그림은 만들지 않았다.
- 완료: `CombatPopupController.ShowLevelUp()`에 생성 경로를 연결했다. `ShowLevelUp(IReadOnlyList<LevelUpCardData>, Action<int>)`로 실제 선택지 최대 3장과 선택 콜백을 공급할 수 있다. 선택 콜백은 한 SetChoices당 한 번 실행되며, 보상 적용과 CompleteLevelUp 호출은 성장 담당자가 소유한다. 인자 없는 호출은 사용자 도안의 불꽃 탄환/피해량 증가/화염 심장을 표시 예시로 보여 준다. 이 예시의 수치·문구는 실제 보상 정의나 전투 수치를 변경하지 않으며 예시 카드를 클릭해도 지급/완료하지 않는다.
- 보존: 기존 UIManager 등록·팝업 수명·Time.timeScale 정지/복원·CompleteLevelUp API를 사용한다. 기존 프리팹 제목만 숨기고 외부 ContentRoot 콘텐츠는 제거하지 않는다. 씬·기존 프리팹·기존 meta·Project Settings·패키지는 변경하지 않았다. 신규 C#과 그 meta만 추가했다.
- 독립 리뷰: 겹친 일시정지창에서 돌아올 때 기존 closeButton=null 레벨업 Focus가 선택을 지우는 문제를 찾아 새 카드 뷰 Focus로 연결했다. 실제 선택 데이터가 있으면 카드, 예시 화면이면 정보 버튼, 상세창이 열려 있으면 닫기 버튼으로 복귀한다. 실제 입력 검증은 남아 있다.
- 검증: Unity 6000.5.0f1 설치본 Roslyn 및 기존 응답 파일에 신규 소스를 포함해 Assembly-CSharp/Assembly-CSharp-Editor 각각 종료 코드 0, 오류·경고 0. 출력은 `.utmp/LevelUpPanel`로 격리했다. 기존 독립 검증은 장착 모델 4/4, 전투 표 7/7 재실행 통과. `git diff --check` 통과. 이번 표시 변경 전용 자동 테스트나 Assets/Tests는 추가하지 않았다. 실행한 Unity 테스트 0개.
- 검증 제한: 열린 MainTestScene Unity 창이 최소화되어 UI 캡처 도구가 `window is minimized`로 연결에 실패했다. 두 번째 에디터를 열지 않았다. 실제 임포트·플레이 화면·한글 줄바꿈·정보 버튼 조작·해상도별 배치·Android/기기는 미확인이다.
- 사용자 직접 확인: Unity 복원 → 컴파일 완료 → MainTestScene/GiungTestScene에서 Play From Title 끄고 Play → Hierarchy의 CombatUI 선택 → Inspector의 CombatPopupController 컴포넌트 메뉴 → `테스트/레벨업 팝업 열기`. 전투가 정지하고 가로 3장 카드와 상단 레벨 업 제목이 보여야 한다. 각 카드 우측 i를 누르면 제공된 설명이 뜨고 닫기로 카드 화면에 복귀해야 한다. 예시 카드 자체는 보상 선택을 완료하지 않는다. 닫기는 컴포넌트 메뉴 `테스트/레벨업 선택 완료`를 사용한다. 전투가 원래 시간 배율로 복원되어야 한다.
- 오류 확인: Console 첫 컴파일 오류, CombatPopupController의 Level Up Popup/ContentRoot 참조를 확인한다. 옛 제목만 보이면 Unity `Assets > Refresh` 후 재생을 다시 시작한다. 카드 내용이 잘리면 Game 뷰 해상도와 실제 화면을 확인한다.
- 다음 작업: 실제 Unity 화면 검증. 레벨업 발생/실제 선택지 추첨·획득·보상 효과는 별도 미구현이며 임의 확정하지 않았다. 향후 실제 성장 시스템에서 데이터와 콜백을 공급한다.

## 2026-11-04 일시정지 창 약공격 장착/해제

- 시작 상태: 실제 브랜치 `feature/Player-Growth-Design`, HEAD `b55f733`, 작업 트리 깨끗함. 사용자는 우선 약공격만 연결하고 향후 강공격은 강공격 버튼에 할당하도록 요청했다.
- 완료: 일시정지 창의 보유 약공격 셀 클릭으로 Light 버튼에 장착하고, 같은 셀을 다시 누르면 해제한다. 다른 약공격을 누르면 교체한다. 장착 셀은 녹색과 `[장착]`으로 표시한다. 빈 칸·잠금 칸·강공격은 클릭 불가다. 강공격 효과는 추가하지 않았고 기존 TryEquip의 Light/Heavy/Tactical 분류 검증을 보존했다.
- 완료: `PlayerLoadout.TryUnequip`은 보유 스킬을 제거하지 않고 버튼 참조만 비운다. 기존 `Changed`로 UI를 갱신하며 팝업을 다시 열어도 클릭 리스너를 중복 등록하지 않는다. 버튼의 `TryUseLightSkill`과 키보드/마우스는 같은 `TryCastLightSkill` 표 경로를 사용한다. 미장착 시 불꽃 탄환 10001로 대체 발사하던 처리를 제거해 입력 시전·MP 차감 없이 종료한다. 기존 공개 콜백과 직렬화 필드는 보존했다. 자동 패시브는 보유 기준 별도 실행이므로 해제 후에도 발사할 수 있다.
- 검증: 새 독립 모델 검증은 구현 전 1/4 통과(해제 API 부재로 3개 실패), 구현 후 4/4 통과. `tools/PlayerLoadoutValidation/Run.ps1`에서 실제 Loadout과 SkillData 코드로 보유 유지·교체·독립 Heavy 할당·잘못된 분류·알림·잠금·제거를 검증한다. 기존 `tools/CombatSkillTableValidation/Run.ps1`도 7/7 통과. Unity 6000.5.0f1 설치본 Roslyn과 기존 응답 파일로 Assembly-CSharp/Assembly-CSharp-Editor 컴파일 각각 종료 코드 0, 오류·경고 0. 출력은 `.utmp/PlayerLoadout`으로 격리해 Library DLL을 덮어쓰지 않았다. `git diff --check` 통과.
- 실행한 Unity 테스트 0개. 실제 에디터 임포트·UI 클릭·플레이·Android 빌드·기기는 확인하지 않았다. 씬·프리팹·meta·Project Settings·패키지는 변경하지 않았다. Assets/Tests와 테스트 asmdef도 복원하지 않았다.
- 사용자 직접 확인: Unity 컴파일 완료 → `MainTestScene` 또는 `GiungTestScene` 열기 → Play From Title 끄기 → Play → 일시정지 → 공격 스킬의 불꽃 탄환 클릭. 녹색 `[장착]` 표시 후 뒤로가기 → `N_Attack` 버튼 또는 Enter를 눌러 불꽃 탄환과 MP 5 소모 확인. 다시 일시정지 → 같은 셀 클릭해 표시 해제 → 복귀 후 같은 입력을 눌렀을 때 추가 액티브 발사·MP 차감이 없어야 한다. 자동 패시브 탄환과 구분한다. 다른 보유 약공격이 있으면 교체 후 해당 효과가 나오는지, 빈/잠금/강공격 칸이 장착되지 않는지 확인한다.
- 오류 확인: Console 첫 컴파일 오류, Player의 `lightAttackButton` 연결, PlayerData의 보유 ID/Light 장착 ID, `Assets/Resources/Data/CombatSkillTable.json` 유무를 확인한다. 표가 없으면 엑셀 저장 후 `Tools > Parse Combat Skill Table` 실행. 이번부터 버튼도 표 비용을 쓰므로 씬의 과거 SkillData.MPCost 10 대신 현재 표의 불꽃 탄환 비용 5가 적용된다.
- 다음 작업: 실제 플레이에서 위 절차 확인. 이후 강공격 구현 시 기존 Heavy 분류와 강공격 버튼/차징 기반을 연결한다. 단계별 배율·시간, 최대 차징 시간 충돌, 취소 MP 정책은 여전히 미정이며 임의 확정하지 않는다.

## 2026-11-04 전투 스킬 효과 데이터 확장

- 시작 상태: `main`, HEAD `1706cda`, 작업 트리 깨끗함. 사용자 요청은 테이블 기반 다음 구현 진행이다. 이번 단계는 기존 엑셀 변환기의 실행 데이터 보완이며 강공격·전략 실제 시전 구현은 후속이다.
- 완료: `CombatSkillRecord`에 대상, 효과 종류, 지속 시간, 상태이상 ID, 슈퍼아머, 넉백/군중제어, 등급, 아이콘·애니메이션·VFX·사운드 키, 설명을 추가했다. `Area_Typetable`의 반경·길이는 Radius·Length로 별도 보존한다. 기존 Range 및 약공격 수치와 해석은 유지한다. 빈 연출 키는 빈 문자열, None 지속 시간은 0, None 슈퍼아머는 false다.
- 완료: 실제 파서에서 inlineStr 문자열과 `/xl/...` 절대 시트 관계 경로도 지원한다. `Assets/Resources/Data/CombatSkillTable.json` 46개(약 21, 강 15, 전략 10)를 다시 생성했다. 엑셀 원본·씬·프리팹·meta·Project Settings·패키지는 변경하지 않았다. 신규 전역 매니저나 런타임 테스트 asmdef도 없다.
- 검증: 확장 전 독립 검증 6개 중 5개가 필드 누락·시트 경로 문제로 실패했고 구현 후 전체 7/7 통과했다. 현재 HEAD의 JSON과 비교해 46개 모든 기존 필드 값 동일. openpyxl 읽기 전용으로 새 효과·대상·연출·설명 값 644개를 실제 엑셀과 별도 대조해 일치했다. Unity 6000.5.0f1 설치본 Roslyn 및 기존 Unity 응답 파일을 사용해 Assembly-CSharp와 Assembly-CSharp-Editor 컴파일 각각 종료 코드 0, 오류·경고 0. 출력은 `.utmp/CombatSkillData`로 격리했고 Library DLL은 덮어쓰지 않았다. `git diff --check` 통과.
- 검증 도구: `tools/CombatSkillTableValidation/Run.ps1`(.NET SDK 9, 외부 NuGet 패키지 없음). 기존 자동 테스트 삭제 결정을 유지해 Assets 밖에서 실제 변환기 코드를 실행한다. `-Export`는 검증 통과 후 JSON을 재생성한다. 콘솔 JSON 직렬화는 System.Text.Json이므로 Unity JsonUtility·AssetDatabase·Resources 동작을 검증한 것은 아니다. 해당 csproj만 `.gitignore` 예외로 추가했다.
- 실행한 Unity 테스트 0개. Unity 메뉴 직접 호출·열린 에디터 임포트·플레이·Android 빌드·기기는 확인하지 않았다.
- 다음 구현: 기존 Player 차징 기반에 강공격 실행을 연결하고 이후 전략 효과·쿨타임·횟수·스테이지 초기화를 연결한다. 강공격 단계별 배율/시간 칸 부재, 최대 차징 0.8초 기준과 20013~20015의 0.85/0.9/0.95초 충돌, 취소 시 MP 정책, 전략 버프 중첩/재사용 규칙은 아직 미정이다. 임의로 확정하지 않는다. 기존 약공격의 키보드/마우스 표 경로와 버튼의 SkillData 경로 차이도 남아 있다.
- 사용자 직접 확인: Unity 컴파일을 기다린 뒤 엑셀 저장 → `Tools > Parse Combat Skill Table` → Console의 `전투 스킬 46개` 메시지 확인. Project에서 `Assets/Resources/Data/CombatSkillTable.json`을 열어 30001 재생 치유의 `EffectType: HealOverTime`, `TargetType: Self`, `EffectDuration: 5`, 20001 화염 망치의 `CrowdControl: Knockback`, `Radius: 2.2`, 20015의 `SuperArmor: true`를 확인한다. MainTestScene에서 Play From Title을 끄고 Play, 미장착 기본 상태에서 Enter/마우스 왼쪽을 눌러 불꽃 탄환과 MP 5 소모를 확인한다. 강공격·전략 효과는 이번 변경만으로 새로 실행되지 않는다. 실패하면 Console 첫 컴파일/변환 오류, 엑셀 저장 여부, JSON 생성 시각을 확인한다.

## 2026-11-04 재시작 컴파일 오류와 자동 테스트 삭제

- 시작 상태: `main`, HEAD `acb7aab`, 작업 트리 깨끗함. 사용자 보고는 에디터 재시작 후 다수 오류이며, 테스트용 씬 복제가 원인인지 확인 요청이었다.
- 원인: 씬을 로드하지 않는 C# 컴파일에서도 동일 오류 재현. `Player.cs`의 `partial` 누락(CS0260), `LightSkillCastTests`의 런타임 어셈블리 직접 참조 및 InputSystem 확장 메서드 using 누락으로 실패했다. 첫 오류를 고친 뒤에는 이전 약공격 버튼·Animator API와 PlayerCombat 생성자 연결 누락, Unity 6000.5에서 금지된 `GetInstanceID()` 호출 오류가 추가로 확인됐다. 확인된 컴파일 오류는 씬 복제에서 발생한 것이 아니다.
- 사용자 요청으로 `Assets/Tests` 전체와 `Assets/Tests.meta` 삭제: C# 26개, 테스트 asmdef 2개 및 관련 meta 포함. 씬·프리팹·asset에서 삭제 대상 GUID 참조 없음 확인. `Assets/0.Scripts`의 테스트용 게임 컴포넌트와 테스트 씬은 유지했다. 자동 회귀 테스트는 현재 저장소에서 제거된 상태이며, 필요하면 이 변경 전 Git 기록에서 복구해야 한다. 과거 문서의 테스트 통과 수는 당시 기록이며 현재 실행 가능 수를 뜻하지 않는다.
- 수정: Player의 partial 선언, 기존 직렬화 필드 `lightAttackButton`, `lightAttackProjectile`, `passiveProjectiles`와 버튼·Animator 콜백 연결을 이전 구현에서 복원. 기존 3인자 PlayerCombat 생성자와 투사체 바인딩 사용. 새 표 기반 `TryCastLightSkill` 경로는 유지했다. LightSkillFx의 중복 피격 식별은 `EntityId`/`GetEntityId()`로 교체했다. 씬·프리팹·Project Settings·패키지는 변경하지 않았다.
- 검증: 설치된 Unity 6000.5.0f1의 Roslyn과 Unity 생성 응답 파일을 사용해 수정 전 Assembly-CSharp/Framework.PlayModeTests 컴파일 실패 재현. 수정 후 Assembly-CSharp 및 Assembly-CSharp-Editor 각각 종료 코드 0, C# 오류·경고 0. 출력은 `.utmp/EditorRestart`로 격리하여 Library의 DLL을 덮어쓰지 않았다. `git diff --check` 통과. 실행한 Unity 테스트 0개. 두 번째 에디터를 열지 않았으며, 열린 에디터의 재임포트·Safe Mode 해제·실제 플레이·빌드·기기는 미확인이다.
- 다음 확인: Unity로 돌아가 임포트/컴파일을 기다린다. 갱신되지 않으면 `Assets > Refresh`를 실행한다. Safe Mode라면 오류가 사라진 뒤 `Exit Safe Mode`를 누른다. Console을 Clear하고 새 빨간 컴파일 오류가 없는지 확인한다. `MainTestScene` 또는 복제한 `GiungTestScene`을 열고 Play From Title을 끈 뒤 Play하여 이동과 Enter/마우스 왼쪽 약공격을 확인한다. MP가 충분하면 불꽃 탄환이 나가고 MP가 소모되며 잠금이 풀려야 한다. 오류가 다시 생기면 Console 첫 빨간 오류의 파일·행과 전체 메시지를 확인한다. 기존 버튼은 장착 SkillData의 ActiveConfigured 및 연결 프리팹·Animator 설정을 사용하는 별도 경로이므로 버튼 플레이 검증도 남아 있다.

## 2026-11-04 전투 수치 엑셀 변환

- 시작 상태: 약공격 21개의 피해·MP·범위는 `LightSkillCatalog` 배열에 적혀 있었고, `SkillDatatable.xlsx`와 따로 놀 수 있었다. 기존 `Tools/ParseExcel`은 `Data.SkillData` 필드 순서로 CSV만 읽어서 이 표에 붙일 수 없다.
- 완료: `Tools > Parse Combat Skill Table`이 프로젝트 루트의 `SkillDatatable.xlsx`에서 약·강·전략 시트와 `Area_Typetable`을 ID로 합쳐 `Assets/Resources/Data/CombatSkillTable.json`에 저장한다. 수식 ID는 저장된 값, `Reach`는 범위 시트의 길이, `Projecttile`은 Projectile이다. 약공격 시전은 이 JSON의 MP·범위·탄수·타격·기본 수치·계수를 읽는다. 그림 5종과 셰이더 색은 그대로다. 강공격·전략은 수치만 들어 있고 실행은 없다. 상태창용 `SkillData`는 바꾸지 않았다.
- 검증: 변환 결과 46줄(약 21, 강 15, 전략 10). 불꽃 탄환은 MP 5, 사거리 7, 폭 0.5, 기본 8, 계수 1.2. Unity 메뉴 재실행, 플레이, 테스트 러너는 실행하지 않음.
- 다음 확인: 엑셀을 저장한 뒤 Unity 메뉴 `Tools > Parse Combat Skill Table`을 누른다. Console에 전투 스킬 46개가 보여야 한다. 이어서 `MainTestScene`에서 Play From Title을 끄고 Play한 뒤 적 앞에서 마우스 왼쪽 또는 Enter를 누른다. MP가 5 줄고 불꽃 탄환이 날아가면 정상이다. Console에 `CombatSkillTable이 없습니다`가 보이면 메뉴를 다시 실행하고, 그림이 분홍 네모면 `LightSkills/LightSkillGlow` 셰이더가 `AllIn1Urp2dRenderer`인지 본다.

## 2026-11-04 약공격 인게임 시전

- 시작 상태: 약공격 표 수치는 있었고, 버튼 시전과 이펙트는 없었다. 약공격 잠금과 MP 차감 API만 있었다.
- 완료: 마우스 왼쪽, Enter, 게임패드 서쪽 버튼의 Attack 입력으로 장착된 약공격을 시전한다. 장착이 없으면 불꽃 탄환(10001)을 쓴다. 비용은 표의 MP, 피해는 기본 수치 + 공격력 × 계수다. 시전 중에는 기존 약공격 잠금이 이동·회피·막기·다음 약공격을 막는다. 자동 탄환은 그대로 따로 나간다.
- 완료: PixelLab 이펙트 5종을 `Assets/Resources/LightSkills`에 넣었다. 탄환 `lightfirebolt`, 원 `lightfireblast`, 직선 `lightthrust`, 부채꼴·호 `lightslash`, 고리 `lightflamering`. Point 필터. `LightSkillGlow`는 `AllIn1SpriteShader/AllIn1Urp2dRenderer`의 Glow와 HSV로 속성 색을 입힌다.
- 검증: 표 수치와 시트 프레임 수를 읽었다. Unity 플레이와 테스트 러너는 실행하지 않음.
- 다음 확인: MainTestScene에서 Play From Title을 끄고 Play한다. 적 앞에서 마우스 왼쪽 또는 Enter를 누른다. MP가 5 줄고 불꽃 탄환이 조준 방향으로 날아가 맞으면 적이 피해를 입는다. 시전 순간에 이동·회피·막기가 안 되고, 잠시 뒤 다시 되면 정상이다. UI 위 클릭은 나가지 않는다. 그림이 분홍 네모면 Console의 셰이더 오류와 `LightSkills/LightSkillGlow` 셰이더가 `AllIn1Urp2dRenderer`인지 본다. 그림이 흐리면 해당 PNG의 Filter Mode가 Point인지 본다.

## 2026-11-04 보스 피격 범위 VFX

- 시작 상태: 보스 스와이프 그림은 피격 부채꼴보다 작고, 빨간 범위 표시는 `BossPatternRangeView`에서 꺼져 있었다. 베기 판정은 선입력 때 고정된 방향의 `slashRange`·`slashAngle`이고, 돌진 판정은 도달한 순간의 플레이어 위치에 `dashEndDamageRadius` 원이다.
- 완료: `BossAttackHitVfx`가 All In 1 URP 2D 셰이더로 그 판정 모양을 그린다. 베기는 스와이프가 시작된 뒤 끝날 때까지 방향이 고정되고, 돌진 원은 타격 순간 0.2초 동안 사라진다. 부모 스케일을 나눠 월드 지름이 반지름의 두 배가 된다. 데미지 계산과 애니메이션 클립은 바꾸지 않았다. 디버그 범위 표시는 그대로 꺼 두었다.
- 연결: `BossMvp` 프리팹. 머티리얼 `Assets/1.Prefabs/Materials/BossAttackHitVfx.mat`는 `RADIALCLIPPING_ON`, `GLOW_ON`. `MainTestScene`의 보스 스케일 4, 베기 반지름 6, 각도 120은 씬 덮어쓰기라 그대로다. 그 씬의 `landingDamageRadius`, `leapDuration`은 현재 필드와 이름이 달라 돌진 반지름은 프리팹 값 2다.
- 검증: 크기 계산 테스트 `BossAttackHitVfxShapeTests`를 추가했다. Unity가 이미 열려 있어 테스트 러너는 실행하지 않음. 에디터 재생, 화면, 빌드, 기기는 확인하지 않음.
- 다음 확인: Unity 임포트가 끝난 뒤 `MainTestScene`을 재생한다. 보스가 가까이서 베면 몸 앞에 주황 부채꼴이 스와이프 동안 유지되고, 플레이어가 움직여도 방향은 그대로여야 한다. 멀리서 돌진해 닿으면 플레이어 자리에 원이 잠깐 보였다가 사라진다. 부채꼴이 안 보이면 Console에 머티리얼 오류가 있는지, 머티리얼 셰이더가 `AllIn1SpriteShader/AllIn1Urp2dRenderer`인지 본다.

## 2026-11-04 강공격 20001~20005와 전략 스킬 수치

- 시작 상태: `main`. HeavySkilltable 20001~20005는 이름·등급·불 속성만 있고, UtilitySkilltable 30001~30010은 이름·속성·자원 Count만 있었다. 20006~20015와 약공격 수치는 이미 채워져 있었다.
- 완료: 20001 화염 망치~20005 운석검에 MP, 쿨타임 0, Charge, 차징 0.5~0.8초, 사거리, 범위, 타격, 기본 수치, ATK 계수, 슈퍼아머, 넉백, 설명을 넣었다. 같은 등급의 20006~20015 위력대에 맞췄고, 기획서의 강공격 최대 차징 0.8초를 넘기지 않았다. 20006~20015는 바꾸지 않았다.
- 완료: 전략 10개에 소모 횟수 1, 쿨타임 6초, Instant, 시전 0초, 효과 종류, 기본 수치, 계수, 지속 시간, 설명을 넣었다. 기획서의 초기 기준은 사용마다 횟수 1, 스테이지당 5회, 쿨타임 6초다. 횟수 상한 칸은 표에 없어 설명에 적었다. 회복·방어막·증가량은 플레이어 최대 체력·마력 비율이다. 등급은 전략 스킬에 없다는 기존 정리를 유지해 비워 두었다.
- 완료: Area_Typetable 34~39행에 화염 망치, 화염 돌진, 대폭발, 화염 폭풍, 운석검, 불타는 갑주의 범위 수치를 추가했다. 자기 자신에게만 쓰는 전략 스킬은 Point라 반경 칸이 없어 범위 표에는 넣지 않았다.
- 수정: 바람의 가호 영문 `Blessing of Wind)`의 닫는 괄호를 뺐다.
- 그대로 빈 칸: 아이콘, 애니메이션, 이펙트 프리팹, 사운드. 상태이상 ID는 None. 차징 1·2·3단계 배율 칸은 이 표에 없다.
- 검증: 저장 파일에서 20001~20005, 30001~30010, 범위 34~39행의 아이디·비용·기본 수치·계수를 다시 읽었다. 20006 기본 수치 14와 20015, 아이디 수식은 유지됐다. Unity 테스트 0개. 전투 로딩은 확인하지 않음.
- 다음 확인: HeavySkilltable 3~7행, UtilitySkilltable 3~12행, Area_Typetable 34~39행. 엑셀을 저장 전에 열어 두었다면 닫을 때 덮어쓰지 말고 다시 연다.

## 2026-11-04 보스 경계 정체 및 할퀴기 중단 복구 수정

- 사용자 승인: 직전 발견의 추적 정지 경계와 리뷰4의 애니메이션 중단 대기를 함께 수정. 리뷰1 돌진 피해 중심은 유지,2 표시 수정은 다른 브랜치 담당,3 벽 재시도는 보류라는 사용자 판단을 유지했다. 이번 런타임 수정은 BossController에만 적용하며 씬/프리팹/공용 이동/피해/표시·패턴 간격은 바꾸지 않았다.
- 경계 수정: FollowStoppingDistance=Mathf.Max(0,closeRange-0.001f). 초기 활성화·SetTarget·할퀴기 복귀·돌진 복귀의 추적 정지점을 이 값으로 통일했다. 근접 공격 선택 조건 distance<=closeRange 자체는 유지하므로 공격 범위를 늘리지 않는다. 실제 이동의 작은 이동량 무시 경계보다 안쪽까지 접근하여4.00005에서 멈추는 현상을 방지한다.
- 중단 복구: PlaySlashAnimation의 Animator 재생/대기를 try/finally로 감싸 공격 실행 플래그와 추적 상태를 정리한다. 대기 중 Animator 비활성화/삭제, runtimeAnimatorController 제거, 현재 상태 변경, 다른 상태로 전이, normalizedTime>=1을 감지해 종료한다. 정상 종료 이벤트는 계속 우선 사용하고, 이벤트 누락 시 피해를 임의로 발생시키지 않는다. 일시정지는 정상 애니메이션 진행을 기다리며 보스 비활성화/사망 시 추적을 재활성화하지 않는다.
- 회귀 테스트: BossAttackRecoveryTests.cs 및 meta 추가(7개). 거리4.00005/3.99 접근 후 근접 선택과 기존 공격 범위 미확장, Animator 해제/컨트롤러 제거/상태 변경 복구, 종료 이벤트 제거 후 복구, 일시정지 유지/재개를 검사한다.
- 실행 결과: Unity6000.5.0f1 별도 최소 복사본 컴파일·PlayMode25/25 통과(신규7+직전 경계 재현2+돌진 시작/루프16), 실패0/생략0, exit0. 동일 테스트7개를 복사본에서만 수정 전 두 로직으로 비교 실행하여2통과/5실패(exit2): 경계4.00005와 중단4종이 의도대로 실패했다. 원본은 계속 수정본을 유지했고 비교 종료 후 복사본도 수정본으로 복구했다. 수정본/테스트 해시 일치 및 대상 diff --check 통과.
- 자료/한계: .utmp/ChargeAnimation/recovery-green.xml/log, recovery-before.xml/log 및 BossController.recovery-green.txt(Git 제외). 원본 씬 실제 조작·전체 프로젝트 회귀·Android 기기는 이번에 검사하지 않았다. 과거50개 검사 결과를 새 검증 수로 재사용하지 않는다.
- 직접 확인: Unity 컴파일 완료 → Tools > Play From Title 해제 → MainTestScene Play → 보스가 접근한 뒤 가만히 있어도 기존 패턴 간격 후 계속 공격하는지 확인. 실행 중 할퀴기 준비에서 Animator만 꺼 보면 공격 대기가 해제되고 추적이 복구되어야 한다. 정상 일시정지 후 복귀하면 해당 공격은 계속 진행되어야 한다. 임시 Animator 변경은 Play 종료로 되돌리고 씬을 저장하지 않는다.
- 오류 확인/다음: Console 첫 오류, 실제 중심 거리/closeRange와 EnemyFollow stoppingDistance, Animator 활성/현재 상태/종료 이벤트를 확인한다. 생존·활성 보스에 한해 추적이 켜져야 한다. 다음은 사용자 원본 씬에서 정지 후 패턴 반복 및 중단 복구 체감 확인이다.

## 2026-11-04 보스 추적 정지 경계에서 패턴 미선택 조사

- 사용자 판단 반영: 이전 리뷰1의 플레이어 중심 돌진 피해는 현 상태 허용,2의 범위 방향은 다른 브랜치에서 수정됨,3의 벽 차단 후 반복은 추후 벽 추가 시 검토. 4의 애니메이션 중단 복구는 수정 승인 뒤 사용자가 잠시 보류하고 본 현상 조사를 요청했다. 아직4번 런타임 수정을 시작하지 않았으며 이번에도 원본 코드·씬·프리팹은 변경하지 않았다.
- 현상: 추적하다 범위 안에 온 것으로 보이지만 공격하지 않고 멈추며 플레이어가 움직이면 다시 공격. 유력 원인은 추적 정지 거리와 근접 패턴 시작 거리를 같은 closeRange로 사용하는 경계 정체다. SelectAvailablePattern은 distance>closeRange면 None, EnemyFollow는 remaining만큼 이동하려 하지만 CollisionMovement2D는 이동 제곱값1e-8 이하를 처리하지 않는다. closeRange4에서 실제 거리4.00005가 남으면 추가 이동도 공격 선택도 되지 않는다.
- 재현: 원본 BossController를 별도 최소 Unity6000.5.0f1 검증본에 복사하고 실제 EnemyFollow/CollisionMovement2D로 정지 표적에 대해 FixedUpdate60회 호출. 거리4.00005는 그대로/selected=None/following=True여서 근접 공격 기대 검사가 실패했고, 거리3.99 비교는 selected=Pattern1로 통과했다. 최종2개 중1통과/1실패, exit2. 원본 씬에서 사용자가 목격한 순간의 거리나 nextPatternTime은 측정하지 않아 동일 원인으로 확정하지는 않는다.
- 자료/검증 제한: .utmp/ChargeAnimation/boundary-confirm.xml 및 boundary-confirm.log, .utmp/c/Assets/Tests/BossBoundaryInvestigationTests.cs(Git 제외). 최초 비교 검사는 검증용 Animator 누락으로 불필요한 오류가 있어 표적을 활성화 후 이동하는 비교로 정리하고 최종 검사를 재실행했다. 결과는 동작 검증 통과가 아니라 버그 재현이다. 실제 프레임 진행/원본 씬 플레이 재현이나 수정 후 테스트는 하지 않았다.
- 권장 수정: 공격 반경 자체를 늘리기보다 보스의 추적 정지 거리를 근접 판정 경계보다 작은 오차 여유만큼 안쪽으로 두어 경계 정체를 없앤다. 이번은 조사 요청이므로 적용하지 않았다. 사용자 확인은 MainTestScene Play → 보스 추적 후 가만히 대기 → 장시간 미공격이면 실제 중심 거리와 closeRange/nextPatternTime 확인 → 조금 보스 쪽으로 이동했을 때 공격 재개 여부 확인. 정상 패턴 간 대기1~2초와 지속적인 경계 정체를 구분한다. 오류 시 Console 첫 오류 및 Animator 종료 상태도 함께 본다.

## 2026-11-04 보스 중심 코드 정적 검토

- 사용자 요청은 검토/보고이며 런타임 코드·씬·프리팹을 수정하지 않았다. BossBeastZombie/88f436b 및 기존 다수 변경을 확인하고 보존했다. BossController, PatternSelector/RuleSelector, BossPatternRangeView, BossEncounterController, EnemyHealth, 보스 프리팹과 MainTestScene override 및 테스트 범위를 읽었다.
- 주요 발견1: BossController:344가 돌진 도착 피해 중심으로 target.position을 사용한다. 현재 보스 closeRange4/dashEndDamageRadius2에서 플레이어3거리도 도착으로 판단하고 플레이어 중심 원으로 명중시킨다. 보스 주변 타격 의도라면 중심을 body.position으로 통일하고 범위 표시도 함께 변경해야 한다. 현재 코드는 플레이어 중심 타격이며 보스 중심 반경2 판정이 아니다.
- 주요 발견2: BossController:196은 attackForward를 시작 시 고정하지만 BossPatternRangeView:76~82는 매 프레임 현재 target 방향으로 부채꼴 표시를 회전시킨다. 준비 중 플레이어가 옆으로 움직이면 보이는 범위와 실제 피해 방향이 다르다. 표시가 고정된 공격 방향을 참조해야 한다.
- 주요 발견3: 돌진에서 벽에 막히면 MoveTo 이동0으로 종료하지만 PatternLoop:165가 다음 공격 시간을 즉시 현재 시간으로 설정한다. 벽 너머 표적이 특수 거리 밖에 남으면 반복적으로 돌진을 재시작해 시작 동작/로그가 되풀이된다. 차단 종료 후 재시도 간격 또는 추적 복귀 정책을 정해야 한다. 단순 반복 시작을 피할 필요가 있으며 시간 수치는 임의 확정하지 않았다.
- 조건부 발견4: PlaySlashAnimation:214는 종료 이벤트만 기다리고 Animator 비활성화/상태 교체/이벤트 누락을 감지하지 않는다. 공격 도중 Animator만 꺼지거나 종료 이벤트가 제거되면 follow=false 상태에서 패턴 대기가 끝나지 않을 수 있다. 현재 정상 클립에 종료 이벤트가 있는 것과 중단 복구 부재는 구분한다.
- 현재 흐름: 근접은 규칙 선택기로 단발→연속 할퀴기 교대, 중거리는 추적, 원거리는 돌진. 할퀴기는 애니메이션 이벤트에서 피해, 돌진은 시작1회→지속 루프 및 도착 피해. 보스 출현은 진행 타이머/일반 스폰을 정지시키며 Time.timeScale을0으로 만들지 않는다. 보스 사망 후 AfterBoss/스폰 재개이며 곧바로 최종 승리로 연결하지 않은 기존 설계는 임의로 버그로 분류하지 않았다.
- 검증: 이번 Unity 실행/컴파일/회귀 테스트0개. 위 내용은 소스·직렬화 설정 기반 정적 검토이며 실제 씬 재현 완료로 보고하지 않는다. 과거50/16개 통과는 이번 검토 결과를 대신하지 않는다.
- 직접 확인/다음: MainTestScene에서 보스 할퀴기 준비 중 옆으로 이동해 부채꼴과 실제 타격 비교 → 보스와3거리/몸통 반경2 밖에서 돌진 도착 피해 확인 → 벽 너머로 원거리 유도해 반복 돌진 확인 → 임시 Play 상태에서 공격 도중 Animator만 해제해 추적/패턴 중단 여부 확인. 마지막 조건은 Play 종료로 원복하고 저장하지 않는다. 오류는 Console 첫 오류/패턴3 반복 로그 및 Animator 상태·이벤트·실제 prefab override를 확인한다. 수정 요청 시 위 조건을 회귀 테스트로 먼저 재현하고 판정 중심·표시 동기화부터 처리한다.

## 2026-11-04 돌진 시작/지속 루프 클립 분리

- 사용자 요청: body-charge 전체 반복으로 서 있는 준비 자세가 매번 나오는 부자연스러움 제거. BossBeastZombie의 기존 코드·씬·프리팹·패링/피격/카메라 연출 변경을 보존했다. 이번 변경은 보스 애니메이션 클립과 Boss.controller에만 적용했다.
- 완료: 원본 south 3/4/6/8/10 그림을 확인해 0~3은 준비, 4~10은 몸을 낮춘 돌진 자세로 나눴다. 기존 body-charge_{방향}.anim 8개는 0~3/12fps/비반복(약0.333초)으로 변경하고, 새 body-charge-loop_{방향}.anim 8개는 원본4~10/12fps/반복(약0.583초)으로 만들었다. 원본 PNG/meta 및 기존 클립 GUID는 유지한다.
- Animator: body-charge(기존 진입 상태) → exitTime1/전환시간0 → body-charge-loop(새8방향 BlendTree)로 연결했다. BossController.StartDashAnimation은 기존 이름으로 진입하므로 코드를 바꾸지 않았다. StopDashAnimation은 반복 중에도 기존 Idle로 종료한다. 이동/추적/피해/돌진 시간 규칙은 변경하지 않았다.
- 검증: Unity6000.5.0f1 별도 최소 검증 복사본에서 Animator PlayMode 검사16/16 통과, 실패0/생략0, exit0. 방향별8개는 시작4키/루프7키·참조 PNG번호·반복 여부·길이를 검사, 방향별8개는 실제 Animator 시작→루프 전환 후 여러 반복 동안4~10 PNG만 표시됨과 Idle 종료/새 돌진 재시작을 검사했다. 클립/meta 해시가 검증본과 같고 controller는 검증 후 새 빈값 뒤 공백만 정리했다(그 외 내용 일치). 대상 diff --check 통과.
- 자료/한계: .utmp/ChargeAnimation/split-loop.xml 및 split-loop.log, 임시 검사 .utmp/c/Assets/Tests/ChargeLoopSplitTests.cs(Git 제외). 이번16개는 Animator 자산 검사이며 실제 MainTestScene 전체 돌진·벽 충돌·모바일 기기를 새로 검사한 결과는 아니다. 과거50개 테스트를 이번 실행 수로 재사용하지 않는다.
- 최종 PlayMode: 73/73 통과, 실패 0/생략 0, 약 9.36초. 첫 실행 72/73의 실패는 LightSkillCastTests가 미장착 자동 발사를 기대하던 구동작 테스트였다. feature 브랜치의 명시적 장착 정책을 확인한 뒤 이 테스트를 미장착 거절/MP 보존 → 장착 발사/표 MP 소비/입력 잠금 해제 → 장착 해제 후 거절/MP 보존 순서로 갱신했다. 런타임 동작은 테스트에 맞춰 되돌리지 않았다. 최종 사본 코드·테스트 해시 일치 확인. 자료는 `C:/tmp/sbs-merge-20261105/final.xml`·`final.log`(생성물, Git 제외)다. 데이터 도구의 9개 실패는 여전히 남아 있으며 이 PlayMode 통과와 별개다.
- 직접 확인: Unity 임포트 완료 → Tools > Play From Title 해제 → MainTestScene Play → 보스와 거리를 벌려 긴 돌진 유도. 처음에만 몸을 낮추고 이후 돌진 자세가 반복되어 서 있는 자세가 중간에 끼지 않아야 한다. Animator 창에서 body-charge→body-charge-loop 전환을 확인하고 돌진 종료 시 Idle/일반 공격으로 복귀하는지 본다. 8방향도 반복 확인한다.
- 오류 확인/다음: Console 첫 임포트 오류, 보스의 실제 Boss.controller 연결, body-charge 상태 전이 및 body-charge-loop의 방향별 클립 참조를 확인한다. 다음은 원본 화면에서 루프 이음새 체감 확인이며 필요하면 반복 프레임 범위를 조정한다.

## 2026-11-04 패링 성공 카메라 줌인/복귀

- 사용자 요청: 패링 성공 시 Main Camera가 살짝 확대됐다 원래대로 돌아오는 연출. 현재 MainTestScene의 CinemachineBrain과 관련 PlayerCameraTests를 확인했다. 다른 작업으로 이동된 PlayerHitFeedbackTest의 Test 폴더 위치를 유지했다. 씬·프리팹·Cinemachine 설정은 이번에 변경하지 않았다.
- 완료: Player.ParryCamera.cs 부분 클래스를 추가하고 기존 HandleParrySucceeded 성공 경로에서 PlayParryCameraZoom을 호출한다. 기본 줌 비율0.05(렌즈 크기/FOV를95%로), 확대0.06초, 복귀0.18초. Inspector의 패링 성공 카메라 줌에서 조절하며 비율0으로 끈다. 실제 시간 기준으로 부드럽게 변화해 히트스톱 중에도 연출이 진행된다.
- 카메라: Camera.main에 활성 CinemachineBrain이 있으면 ActiveVirtualCamera의 CinemachineCamera.Lens를 조절하여 Brain 덮어쓰기와 경쟁하지 않는다. Brain이 없는 일반 Camera는 orthographicSize 또는 fieldOfView를 조절한다. 위치·회전·추적 대상은 유지한다. 카메라가 없거나 지원되는 활성 가상 카메라가 없으면 게임 로직을 막지 않고 줌만 생략한다.
- 수명: 재발동 전 이전 렌즈를 복원해 누적 확대를 방지하며 플레이어 비활성화/사망 때 진행 중 코루틴을 취소하고 원래 렌즈를 복구한다. 가상 카메라 전환 중에는 새 카메라를 덮어쓰지 않고 기존 렌즈를 복원한다. 가드/일반 피격/공격 없이 누른 패링에는 성공 줌을 연결하지 않았다.
- 검증: Unity6000.5.0f1 별도 검증 복사본 컴파일 및 PlayMode50/50 통과, 실패0/생략0, exit0. 신규 카메라5 + 기존 피격5/패링8/MP·회피19/팝업13. 히트스톱 중 직교 줌/정확 복구, 원근 FOV 복구, 비활성화 즉시 복원, 공격 없는 패링 미발동, 실제 Cinemachine 가상 렌즈와 Main Camera 출력 확대/복귀를 검사했다. 변경 소스/새 테스트 해시 일치, 대상 diff --check 통과.
- 자료/한계: .utmp/ParryFeedback/camera-zoom.xml 및 camera-zoom.log(Git 제외). 이번 검사는 실제 MainTestScene 씬을 로드한 전체 플레이 검사나 화면 캡처가 아니며 실제 원본 씬 조작/모바일 기기는 미검증이다.
- 직접 확인: Unity 컴파일 완료 → Tools > Play From Title 해제 → MainTestScene Play → 보스 타격에 맞춰 E키 패링. 짧은 정지 중 화면이 약간 확대됐다 곧 원래 구도로 돌아오고 플레이어 추적/정지/밀림이 유지되어야 한다. Player Inspector의 패링 성공 카메라 줌 세 값으로 체감을 조절한다.
- 오류 확인/다음: Console 첫 컴파일 오류, MainCamera 태그, Brain의 ActiveVirtualCamera가 CinemachineCamera인지, 줌 비율이0인지 확인한다. 다른 카메라 제어기가 렌즈를 변경하는지도 확인한다. 다음은 원본 씬에서 실제 줌 강도와 복귀 시간의 체감 확인이다.

## 2026-11-04 BossParry 사용 폐기 정리

- 사용자 요청: 더 이상 사용하지 않는 BossParry 관련 코드 정리. 시작 상태 BossBeastZombie/88f436b에서 사용자가 BossParry.cs와 meta를 이미 삭제한 상태였다. 삭제는 보존하고 복원하지 않았다. 기존 보스/패링·피격 연출 변경을 유지했다.
- 코드: Player의 BossParry 직렬화 필드 및 PlayerDefense 생성자 인자 제거. PlayerDefense의 BossParry 필드/생성자 의존성과 boss.TryParry 주기 조회를 제거했다. 성공 여부는 실제 TakeDamage 차단 경로에서 받은 parrySucceeded만 사용한다. LastParrySource, 기존 성공 이벤트·MP 회복·히트스톱·밀림·가드 규칙은 유지했다. 참조 없는 옛 색상 타이밍 테스트 BossParryTest.cs와 meta도 삭제했다.
- 직렬화: BossMvp.prefab의 옛 컴포넌트 블록/목록 항목, MainTestScene·GiungTestScene 및 _Recovery의 0.unity/0 (1).unity의 stripped 블록과 해당 컴포넌트 전용 override만 제거했다. Player 스크립트 블록의 폐기된 boss 필드는 dongjinScenes까지 포함해 제거했다. BossEncounterController가 실제 BossController를 참조하는 boss 필드는 유지했다. 씬을 Unity로 전체 재저장하지 않고 나머지 YAML/기존 사용자 프리팹 변경을 보존했다.
- 테스트: 옛 BossParry 전용 성공 테스트를 공격 없이 패링을 눌러도 정지/밀림이 없다는 검사로 교체했다. 실제 공격자 차단 성공 검사는 유지된다. Unity6000.5.0f1 별도 검증 복사본에서 컴파일 및 관련 PlayMode 45/45 통과(피격5·패링8·MP/회피19·팝업13), 실패0/생략0, exit0. 최종 원본 Player/PlayerDefense/수정 테스트는 검증본과 해시 일치. 대상 코드·씬 diff --check 통과. Assets의 C#/씬/프리팹에서 BossParry 이름·두 폐기 스크립트 GUID·폐기 컴포넌트 fileID 잔존 검색0건.
- 자료/한계: .utmp/BossParryCleanup/results.xml 및 validation.log, 직렬화 변경 전 사본(Git 제외). 원본 씬의 실제 재생/Android 기기는 미확인. 새 씬 로드 검사는 이번 별도 코드 테스트에 포함하지 않았다.
- 사용자 확인: Unity 컴파일 완료 → Console에 BossParry 타입 누락 오류가 없는지 확인 → Tools > Play From Title 해제 → MainTestScene Play → 공격 없이 E 입력 시 성공 연출 없음 → 보스 타격을 E로 막으면 MP 회복/히트스톱/밀림 유지 → PlayerHitFeedbackTest 부착 시 일반 피격 빨간색 연출 확인. BossMvp와 두 테스트 씬에서 폐기 BossParry의 Missing Script가 없어야 한다. 오류 시 Console 첫 오류와 실제 Player 입력 에셋을 확인한다. 다음은 사용자 실제 씬 확인이다.

## 2026-11-04 분리형 일반 피격 테스트 연출

- 사용자 요청: 패링 성공 외에도 피격 시 밀림·짧은 정지·빨간색 표시를 테스트용 별도 스크립트로 구현. 기존 변경을 유지하고 씬·프리팹에 자동 부착하거나 저장하지 않았다.
- 추가: PlayerHitFeedbackTest 컴포넌트. Player 오브젝트에 수동 Add Component하면 활성화되고 체크 해제/제거로 끌 수 있다. 기본 빨간색0.25초(scaled), 실제시간 정지0.06초, 공격자 반대 방향 거리0.25/시간0.12초. 색은 기존 alpha를 유지하고 반복 피격/컴포넌트 해제 시 원래 색으로 복구한다. 대상 SpriteRenderer는 Inspector 지정 또는 자식에서 찾는다.
- 연결: Player에 실제 비치명 피해 적용 후 Damaged(float, GameObject) 이벤트만 추가했다. 가드/패링/회피로 막힌 공격 및 무효 피해에는 이벤트가 없고, 치명타는 기존 사망 비활성화 경로를 유지한다. 컴포넌트가 없으면 기존 일반 피격 동작 그대로다. 공격자 정보가 없거나 같은 위치면 임의 밀림 방향을 만들지 않는다.
- 정지/이동 재사용: Player.ParryFeedback의 기존 충돌 이동·시간 소유권 경로에 내부 테스트 진입/취소 함수를 추가했다. 별도 전역 매니저나 두 번째 Time.timeScale 제어 루프는 만들지 않았다. 연출 순서는 패링처럼 정지 후 밀림이며 빨간색 표시는 즉시 시작한다. 실제 피격은 진행 중 반응을 새 피격 기준으로 교체하고, 테스트 컴포넌트 해제는 자신이 시작한 연출만 취소한다. 기존 패링 수치는 유지했다.
- 검증: Unity6000.5.0f1 별도 복사본에서 관련 코드 컴파일 및 PlayMode 45/45 통과, 실패0·생략0, exit0. 새 PlayerHitFeedbackTests5 + 기존 패링8/MP·회피19/전투 팝업13. 실제 피해 시 색·정지·밀림 및 복구, 미부착 시 미발동, 패링 차단 시 빨간색 미발동, 컴포넌트 해제 시 시간/색/입력잠금 복구, 연속 피격 후 원래 색 복구를 검사했다. API 경고 수정 뒤 코드도 이번 실행에 포함했고 이번 연출/수정 검색 호출의 경고는 없었다. 대상 git diff --check 및 테스트 소스 해시 일치 확인.
- 검증 자료/한계: .utmp/ParryFeedback/hit-feedback.xml 및 hit-feedback.log, .utmp/p 복사본(Git 제외). 원본 에디터 화면 재생·실제 MainTestScene 조작·Android 기기는 미실행. 테스트용 컴포넌트는 원본 씬에 아직 부착하지 않았다.
- 직접 확인: Unity 컴파일 완료 → Tools > Play From Title 해제 → MainTestScene의 Player 선택 → Add Component > PlayerHitFeedbackTest → Play → 방어 없이 보스/일반 적에게 맞는다. HP가 줄면서 빨간색, 짧은 정지 후 공격자 반대쪽 밀림, 원래 색 복구가 정상이다. 컴포넌트 체크 해제 후 같은 피격에서는 기존 HP 감소만 나와야 한다. 패링 성공 시 기존 패링 연출만 나오고 빨간색 피격 표시는 없어야 한다.
- 오류 확인/다음: Console 첫 오류, 같은 오브젝트의 Player 존재, 컴포넌트 enabled, Target Renderer 및 네 시간/거리 값, attacker/source 유무를 확인한다. 색만 안 바뀌면 실제 화면을 그리는 SpriteRenderer를 Target Renderer로 지정한다. 다음은 사용자 씬에서 연출 체감 조절이며 테스트 종료 시 컴포넌트를 제거하면 된다. 실제 피해/무적/사망 규칙은 변경하지 않았다.

## 2026-11-04 패링 연출 검색 API 경고 수정

- 사용자 지적 확인: 앞선 green.log에 Player.ParryFeedback.cs 및 CombatPopupController.cs의 FindObjectsSortMode 인자 사용에 대한 CS0618 경고가 있었다. 앞선 40/40 통과는 경고가 없다는 의미가 아니며 경고를 놓쳤다.
- 수정: 이번 연출에서 추가한 두 검색만 FindObjectsByType<GameManager>() 및 FindObjectsByType<Player>()로 변경했다. Unity6000.5 컴파일 경고의 공식 대체 호출 안내에 따른 수정이며 씬 필터·승패/팝업 정지 소유권 로직은 유지한다. 관련 없는 기존 호출은 일괄 변경하지 않았다.
- 검증: 변경 호출 정적 확인 및 대상 git diff --check 통과. 이번 컴파일/Unity 테스트 재실행0개. 앞선 40개 결과를 이번 수정의 재실행 결과로 보고하지 않는다.
- 직접 확인: Unity 컴파일 완료 → Console Clear 후 해당 파일의 CS0618이 새로 발생하는지 확인 → MainTestScene에서 패링 성공 및 일시정지/복귀 확인. 해당 두 호출 경고가 사라지고 정지/밀림/팝업 복귀가 유지되면 정상이다. 오류 시 첫 컴파일 메시지와 실제 Unity 버전을 확인한다. 다음은 원본 에디터 컴파일/실제 화면 확인이다.

## 2026-11-04 패링 성공 히트스톱 및 플레이어 밀림

- 사용자 요청: 패링 성공 시 잠깐 게임을 멈추고 플레이어를 보스 반대 방향으로 조금 밀어내는 연출. 시작 브랜치 BossBeastZombie/88f436b. 기존 BossController 복구·사용자 BossMvp.prefab·Animator·할퀴기 및 body-charge 클립 변경을 보존했다. 기존 수정 파일은 작업 전후 SHA-256 동일. 이번에 씬·프리팹·Project Settings·패키지를 수정하지 않았다.
- 완료: Player.ParryFeedback.cs 부분 클래스에서 성공 연출을 소유한다. 기본 정지 0.08초(실제 시간), 정지 해제 후 공격자 반대 방향으로 거리0.35를 0.12초 동안 이동한다. Player Inspector의 패링 성공 연출에서 세 값을 조절한다. 연출 동안 플레이 입력을 잠깐 잠그고 기존 PlayerMovement/CollisionMovement2D를 사용해 벽을 통과하거나 몸통을 밀지 않게 한다. 새 전역 매니저/싱글톤은 추가하지 않았다.
- 연결: PlayerDefense의 기존 ParrySucceeded 이벤트와 무인자 NotifyIncomingAttackBlocked API를 유지했다. LastParrySource 및 NotifyIncomingAttackBlockedFrom으로 실제 피해 공격자 또는 기존 BossParry 참조를 성공 시 전달한다. 가드·실패에는 연출이 없으며 같은 패링의 다중 Collider 접촉은 기존 성공 플래그로 한 번만 연출/MP 회복한다. 공격자 미상/동일 위치면 임의 방향을 만들지 않고 정지만 적용한다. MP 회복 중 비활성화/재활성화된 경우 새 생명주기에 연출을 시작하지 않는다.
- 정지 수명: Player 비활성화/파괴 시 소유한 정지와 연출을 정리한다. 성공 전 시간 배율을 저장해 복구하며 승패 정지는 GameManager.state로 확인해 유지한다. CombatPopupController.AcquirePause는 플레이어 히트스톱을 먼저 종료한 뒤 원래 배율을 저장하고 팝업 정지를 인수한다. 따라서 히트스톱 중 일시정지·레벨업 팝업이 겹쳐도 조기 재개하거나 팝업 종료 후 0배율에 갇히지 않는다. 팝업 중 밀림 진행도 scaled deltaTime으로 정지한다.
- 회귀: PlayerParryFeedbackTests 8개 추가. 구현 전 별도 Unity 복사본에서 8개 중3 통과/5 실패로 정지·밀림 부재 재현(exit2). 구현 후 Unity6000.5.0f1 컴파일 및 PlayMode 40/40 통과, 실패0·생략0, exit0. 새 연출8 + 기존 PlayerResourceActionTests19 + CombatPopupTests13. 실제 피해 경로 성공/중복 접촉, 일반 가드 미발동, 벽 차단, 팝업 중첩과 기존0.4배율 복구, 비활성화 정지 해제, 승패 정지 유지, 기존0.5배율 복구, 구 BossParry 경로를 검사했다.
- 검증 근거: .utmp/ParryFeedback/red.xml/red.log, green.xml/green.log 및 .utmp/p 검증 복사본(생성물 Git 제외). 변경 런타임/새 테스트와 복사본의 SHA-256 동일, 대상 git diff --check 통과. 검증 복사본에는 전체0.Scripts와 Tests 및 Joystick Pack을 복사하고 기존 로컬 패키지 캐시를 사용했다. 복사본 PlayFromTitle에만 batchmode guard가 있으며 원본 옵션을 변경하지 않았다. 전체 게임 회귀·원본 MainTestScene 실제 화면/모바일 기기는 이번에 실행하지 않았다.
- 사용자 확인: Unity 컴파일 완료 → Tools > Play From Title 해제 → MainTestScene Play → 보스의 손 내리기 타격 직전에 Player/Parry 입력 → 성공 순간 약0.08초 멈췄다가 플레이어가 보스 반대쪽으로 조금 밀리는지 확인. 실패/일반 가드에는 정지·밀림이 없어야 한다. 벽을 등지고 성공해도 통과하지 않아야 하며, 성공 직후 일시정지나 레벨업 팝업을 열었다 닫아도 정상 복귀해야 한다.
- 오류 확인/다음: Console 첫 컴파일 오류, Player Inspector의 패링 성공 연출 세 값, 입력 에셋 Player/Parry 연결, 실제 피격 호출의 source/attacker 전달을 확인한다. 밀림 방향이 없으면 공격자가 null이거나 플레이어와 동일 위치인지 확인한다. 다음은 원본 씬에서 정지 길이/거리 체감 및 실제 터치 입력 확인이다. 추가 무적·보스 넉백·보상·패링 수치 규칙은 이번 연출에 넣지 않았다.

## 2026-11-04 삭제된 보스 돌진 애니메이션 코드 복구

- 사용자 요청으로 BossController.cs에서 삭제된 body-charge 연결을 복구했다. 시작 브랜치 BossBeastZombie/88f436b. 기존 BossMvp.prefab·Animator controller·할퀴기 클립·새 body-charge 클립·인수인계 변경은 보존했다. body-charge 상태와 8방향 클립은 남아 있어 다시 만들지 않았다.
- 복구: dashAnimationRunning 필드, 돌진 시작의 StartDashAnimation 호출, 돌진 종료 및 OnDied/OnDisable의 StopDashAnimation 호출, 두 보조 메서드. 현재 피해 중심(target.position)과 돌진 이동 규칙은 유지했다.
- 검증: 복구 코드 SHA-256이 앞선 검증 복사본의 BossController.cs와 일치하고 git diff --check 통과. 이번 Unity 실행 테스트 0개이며 앞선 5/5 결과를 이번 재실행 결과로 취급하지 않는다.
- 직접 확인: Unity 컴파일 완료 → Tools > Play From Title 해제 → MainTestScene Play → 보스와 10 이상 거리로 벌려 돌진 유도. body-charge로 돌진하고 종료 후 일반 동작으로 돌아오면 정상. 오류 시 Console 첫 컴파일 오류와 missing body-charge state 경고, Boss.controller 연결을 확인한다. 다음 작업은 원본 씬 실제 재생 확인이다.

## 2026-11-04 보스 돌진 body-charge 애니메이션 연결

- 시작 상태: BossBeastZombie, HEAD 88f436b. 이전 할퀴기 선딜레이 16클립·인수인계 변경과 사용자 BossMvp.prefab 수정을 보존했다. 이번 작업 전후 사용자 프리팹 SHA-256 동일. 브랜치·씬·기존 스프라이트 PNG/meta·이전 공격 클립을 변경하지 않았다.
- 완료: 기존 body-charge 원본 8방향×11장을 사용하는 body-charge_{방향}.anim 8개와 meta 추가. 기존 보스 애니메이션과 같은 12fps, 반복 재생. Boss.controller에 body-charge 상태와 FacingX/FacingY를 쓰는 8방향 BlendTree를 추가했고 기존 상태/전이 내용은 그대로 유지했다.
- 실행 연결: BossController.DashToTrackedTarget에서 돌진 시작 시 body-charge를 한 번 재생한다. 기존 추적 방향 갱신으로 방향이 바뀌어도 재생 시간을 매 프레임 초기화하지 않는다. 돌진 완료·장애물 차단 후 종료·대상 소실에 따른 정상 루프 종료 시 Idle로 복귀하며, 사망/컴포넌트 비활성화 때도 재생 상태를 정리한다. 이동·돌진 시간·피해 수치·도착 피해 시점은 기존 규칙을 유지했다. 상태가 없는 다른 Animator를 쓰는 경우 경고 후 기존 돌진 이동은 유지한다.
- 검증: Unity 6000.5.0f1 별도 최소 검증 프로젝트에서 실제 Boss/NormalEnemy/Combat 소스와 보스 아트를 임포트·컴파일, 임시 PlayMode 검사 5/5 통과(실패0). 8방향 11개 Sprite 참조·반복 설정, 실제 돌진 이동 중 body-charge 진입/종료, 방향 변경 시 재생 진행 유지, 비활성화/재활성화, 일시정지 중 이동/애니메이션 정지 및 복귀를 확인했다. 원본 BossController·controller·새 클립/meta와 검증본 해시 일치. 새 controller ID 중복 없음 및 기존 controller 내용 보존 검사 통과.
- 검증 환경/한계: 최초 샌드박스 실행은 라이선스 IPC 연결 거부/초기화 시간 초과로 테스트에 도달하지 못했다. 승인 후 검증 복사본 에디터만 재실행해 위 5개를 완료했다. 원본 에디터를 추가로 열거나 종료하지 않았다. 결과 .utmp/ChargeAnimation/results.xml 및 validation-elevated.log, 임시 검사 코드는 .utmp/c/Assets/Tests(모두 Git 제외). 전체 프로젝트 회귀·원본 MainTestScene 화면·Android 기기는 미검증이다. 별도 임포트 중 기존 idle/north-west 일부 Sprite Rect 경고가 있었으며 이번 body-charge Sprite 참조 검사는 통과했다.
- 차이 검사: 이번 BossController/Boss.controller의 git diff --check 통과. 전체 diff에는 작업 전부터 있던 사용자 BossMvp.prefab의 빈 YAML 값 뒤 공백 3곳이 있어 이를 임의 정리하지 않았다.
- 사용자 확인 순서: Unity 임포트/컴파일 완료 → Tools > Play From Title 해제 → MainTestScene Play → 보스 출현 후 10 이상 거리를 벌려 패턴3 돌진 유도 → 돌진 중 옆으로 움직여 방향 전환 확인 → 도착 후 일반 동작/할퀴기 복귀 확인. 정상은 몸을 낮추는 body-charge 동작이 돌진 동안 반복되고 추적 방향을 따라 바뀌며, 돌진 종료 후 그 자세에 고정되지 않는 상태다. 일시정지/복귀도 확인한다.
- 오류 확인/다음: Console 첫 컴파일/임포트 오류와 missing body-charge state 경고, BossMvp Animator의 Boss.controller 연결, Animator 창의 body-charge 상태 및 각 방향 클립 Sprite 참조를 확인한다. 다음은 실제 원본 씬에서 방향별 화면·기존 할퀴기 선딜레이와의 연결 체감 확인이며 돌진 선딜레이·피해 규칙을 추가로 바꾸지는 않았다.

## 2026-11-04 보스 할퀴기 선딜레이 4프레임 추가

- 사용자 요청: 손을 올린 자세를 약 4프레임 늘려 공격을 보고 대응할 시간을 제공한다. 현재 브랜치 BossBeastZombie, HEAD 88f436b. 변경 전 작업 트리는 깨끗했고 브랜치를 전환하지 않았다.
- 완료: Beast_Zombie_Boss/Animator/Clips의 Boss_Swipe 및 Boss_SwipeLeft 각 8방향, 총 16개 클립. south 원본 그림을 확인해 3.png가 손을 올린 자세이고 4.png가 내려치는 동작임을 확인했다. 3번 자세를 기존보다 4샘플 더 유지하도록 4번 이후 키를 4/12초 뒤로 이동했다. 12fps 기준 추가 선딜레이 약 0.333초. 피해 이벤트 0.4583→0.7917초, 종료 이벤트 0.6583→0.9917초, 클립 길이 0.75→1.0833초. 스프라이트 순서·참조·GUID 및 휘두르는 구간의 속도는 보존했다.
- 검증: PowerShell 정적 검사에서 16/16 클립의 키 개수·스프라이트 참조/순서·4샘플 지연·피해/종료 이벤트·클립 길이를 HEAD와 비교해 통과. 시간 필드 외 내용 동일 확인. git diff --check 통과. Unity 실행 테스트 0개이며 실제 Animator 재생·기기 검증은 하지 않았다. 씬·프리팹·meta·런타임 코드는 수정하지 않았다.
- 참고: BossMvp에는 별도의 EnemyContactDamage가 활성화되어 있어 몸통 접촉 피해는 할퀴기 이벤트와 무관하다. 이번 요청에서 접촉 피해 규칙을 변경하지 않았다. 앞선 EnemyContactDamage 리뷰의 활성 상태 검사 누락/풀 재사용 시 타이머 미초기화도 미수정 상태다.
- 사용자 확인: Unity 임포트 완료 → Tools > Play From Title 해제 → MainTestScene Play → 보스 출현 후 몸통에 닿지 않는 할퀴기 범위에서 오른손·왼손 공격 확인. 손을 올린 자세가 약 0.33초 더 유지되고 내려치는 시점에 할퀴기 피해가 들어오면 정상. Window > Animation > Animation에서 Boss_Swipe_South 및 Boss_SwipeLeft_South를 선택해도 확인할 수 있다. 오류 시 Console 첫 임포트 오류, 실제 Animator의 Boss.controller 연결, 클립 피해 이벤트 위치를 확인한다. 준비 동작 중 HP가 감소하면 몸통 접촉과 다른 적 공격을 먼저 분리한다.
- 다음 작업: 원본 에디터에서 8방향 공격 예고/피해 동기화 및 패턴2 연속 공격의 체감 확인. 몸통 접촉 피해까지 선딜레이 동안 막을지는 별도 동작 변경이므로 이번에 임의 적용하지 않았다.
## 2026-11-04 main에 EnemyEXP·feat/PlayerSkill 병합

- 사용자 요청: `EnemyEXP`와 `feat/PlayerSkill`을 `main`에 병합. 시작 작업 트리는 깨끗한 `EnemyEXP`, HEAD `f42760d`였다. origin fetch 후 main `53c7b5d`, EnemyEXP `f42760d`, feat/PlayerSkill `3d0974b`가 로컬·원격에서 일치함을 확인했다. 기존 브랜치는 보존한다.
- 병합 순서: main에 EnemyEXP를 `f5a3783` merge commit으로 병합한 뒤 feat/PlayerSkill을 병합했다. 두 병합 모두 실제 충돌은 Handoff.md뿐이었다. main·EnemyEXP·스킬 브랜치의 고유 인수인계 섹션을 모두 보존했으며, 아래 과거 기록의 브랜치·HEAD·테스트 수는 기록 당시 값이다. 현재 작업 방향은 이 최신 항목과 각 기능 상세 문서를 기준으로 판단한다.
- 보존 확인: EnemyHealth의 DNA 드롭과 표시 전용 HP 갱신, PlayerData의 EXP API/획득 거리, 스킬 데이터·약공격/패시브·강공격 차징, main의 기존 아트·데이터 수정이 함께 반영됐다. WorldWrap은 폐기 상태이며 Restart의 정리 프레임 분리는 유지된다. MainTestScene의 보스 출현 30초 및 스킬 설정은 feat/PlayerSkill의 원래 직렬화 값이며 병합 과정에서 새 기획 수치를 정하지 않았다.
- 실행 검증: Unity 6000.5.0f1 별도 전체 프로젝트 복사본. EnemyEXP 기준 **181개 중 163 통과/18 실패**, feat/PlayerSkill 단독 기준 **249개 중 231 통과/18 실패**, 병합 결과 **250개 중 232 통과/18 실패**. 모두 생략 0, exit 2. 병합 결과의 실패 18개는 feature 단독 기준과 이름·실패 메시지가 전부 일치하고 새 실패는 없다. 강공격 차징 32/32, 약공격 13/13, 적 HP 표시 7/7, Restart 회귀 1/1 통과. 실패가 없는 전체 테스트로 보고하지 않는다.
- 현재 실패 목록: `docs/AI/HeavyChargeValidation.md`의 18개 목록과 동일하다. GiungTestScene Player 입력 에셋 참조 누락 16개, `PlayerPassiveSkillTests.OutsideRangeOrViewportDoesNotConsumeReadyShot`(탄환 0 기대/1 관측), `PlayerStatusTests.KeyboardSubmitCanEnterAdjustAndLeaveOptionsWithoutLosingSelection`(MasterVolume 대신 OptionsButton 선택). EnemyEXP 기준의 CombatPausePopup 중복 등록 실패는 병합 결과에서는 통과했다. 서로 다른 시점의 18개 실패를 같은 목록이라고 취급하지 않는다.
- 비교 자료: `.utmp/g/merge-enemy-baseline.xml`, `merge-combined-suite.xml`, `merge-skill-baseline.xml`과 대응 로그(Git 제외). 복사본 PlayFromTitle에만 batchmode guard를 넣었고 원본 씬·프리팹·Project Settings·패키지는 Unity로 자동 저장하지 않았다. 원격 push는 이 요청에서 수행하지 않는다.
- 독립 리뷰: 자동 병합 교차 부분의 DNA+HP 표시, EXP+스킬 모델, 삭제된 스크립트 참조, 씬 연결, WorldWrap 폐기·Restart 처리 보존을 확인했다. 추가 회귀나 자동 병합 결함 지적은 없었다. Unity 직렬화 파일의 원래 빈 값 뒤 공백을 보존하며 문서 충돌 해결에 공백 일괄 정리를 섞지 않았다.
- 사용자 확인 순서: main에서 Unity 임포트/컴파일 완료 → `Tools > Play From Title` 체크 시 해제 → MainTestScene Play → N_Attack 약공격 발사와 C_Attack 홀드/방향/단계 표시·해제 확인 → 적 처치 시 DNA 드롭 확인 → 사망 후 Game Over Restart 반복. 정상 결과는 불꽃 탄환·차징 표시가 동작하고, DNA가 생성되며, Restart 후 새 플레이어로 재진입하는 상태다. DNA 유도 거리/속도/EXP가 0이면 드롭 이후 자동 유도·EXP 증가를 기대하지 않는다. 오류 시 Console 첫 오류, Player의 입력 에셋·버튼 참조·MP, EnemyHealth의 픽업 프리팹 연결을 확인한다.
- 남은 검증/다음 작업: 원본 에디터의 병합 후 실제 UI 입력·터치와 Android 빌드/기기 확인. 강공격의 실제 피해·정식 스킬 실행은 이 브랜치에서도 후속이며 차징 기반을 완료된 피해 구현으로 취급하지 않는다. 기존 GiungTestScene 입력 참조·테스트 격리 실패는 별도 범위로 남긴다.

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

## 최신 작업 상태 — 2026-11-03 강공격 차징 기반 완료

- `docs/AI/HeavyChargeHandoff.md`, 사용법 `docs/AI/HeavyCharge.md`, 상세 검증 `docs/AI/HeavyChargeValidation.md`를 함께 읽는다. 아래의 ‘종료 요청: 구현 중’ 기록은 이전 세션의 역사이며 현재 상태가 아니다.
- 시작/종료 브랜치 `feat/PlayerSkill`, HEAD `526aaf6` (`강공격 작업중`), 시작 작업 트리 깨끗함. 이전 초안은 이미 커밋되어 있었다. 이번 작업은 미커밋으로 유지했으며 브랜치 전환 없음.
- PlayerView의 빠진 SetHeavyChargeIndicator를 구현했다. 재사용하는 LineRenderer 화살표가 청록/노랑/주황으로 세 단계를 표시하고 진행도에 따라 길이가 늘어난다. 해제/취소/비활성화 시 숨김, Dispose 시 자식과 Material 정리. 이 길이는 피해 범위가 아니다.
- MainTestScene에 C_Attack Button452539071과 승인 설정 MP10/단계0,.4,.8초/증분.1,.1,.5/SuperArmor=false만 13줄 추가했다. 새 런타임3·테스트2개의 Unity 생성 meta를 반영했다. 기존 GUID·다른 씬·프리팹·설정·패키지 보존.
- 리뷰 결함4개를 실패 테스트로 재현 후 수정: 정지 중 Q 해제 즉시취소, 키 해제와 동시 입력된 실제 방어/회피 우선취소, 포커스 복귀 후 누락된 PointerUp 예약 정리, 조이스틱 중립에서 마지막 실제 각도 유지. 실패한 회피는 유지하고 취소 MP는 반환하지 않는다.
- Unity6000.5.0f1 별도 전체 자산 검증본에서 C# 컴파일 성공. 최종 차징 **32/32 통과**(단위12+통합20). 전체 PlayMode **249개 중231통과/18실패/생략0**. HEAD 입력 코드를 검증본에만 적용한 기준 비교는227통과/22실패였고, 차징4개만 통과로 바뀌었으며 나머지18개는 같은 실패다. 원래 HEAD는 View 메서드가 없어 컴파일 불가이므로 기준 비교에도 완성 View를 유지했다.
- 기존18실패: GiungTestScene Player의 InputAction 참조 누락으로 CombatPopupSceneTests7/GameResultFlowTests8/풀링 씬1, 전체 실행 시 이전 씬 카메라·탄환 영향 PlayerPassiveSkillTests1, EventSystem 포커스 간섭 PlayerStatusTests1. 이름과 실제 메시지는 HeavyChargeValidation.md에 기록. 이번 차징 범위 밖의 씬/기존 테스트를 일괄 수정하지 않았다.
- 별도 전체 자산 검증본에서 MainTestScene을 실제 PlayMode 로드해 직렬화된 C_Attack의 PointerDown/Up → MP10 차감 → 3단계/1.7배 해제 → 표시 숨김을 확인했다. 씬 연결 smoke **1/1 통과**. 원본 에디터 수동 조작/실제 터치 검증은 아니다.
- 그래픽 사용 오프스크린 화살표 검증1/1 통과 및 PNG 직접 확인. 독립 리뷰4건 모두 수정·재검증. 실제 원본 에디터 수동 플레이/실제 터치·Android/실제 카탈로그·번들은 미검증. 실제 피해·스킬ID·공격 애니메이션·그로기는 승인된 후속 범위다.
- 직접 확인: Tools > Play From Title 끄기 → MainTestScene Play → C_Attack 홀드 → MP10 한 번 차감·청록→.4초 노랑→.8초 주황·이동 고정/조준 변경 → 해제 시 숨김·이동/패시브 복구. PC는 PlayerController2D를 Play 중 끄고 Q+이동 키. 방어/회피 성공·피격/사망·정지 중 해제/포커스 취소도 확인한다. 오류 시 Console 첫 새 오류와 C_Attack 참조/Configured/배열/MP/입력 소스를 확인한다. 자세한 순서는 HeavyCharge.md.
- 다음: 사용자 실제 조작 피드백 확인. 별도 작업으로 기존 씬 입력 연결/테스트 간 간섭18개를 정리하고, 정식 테이블이 준비되면 강공격 결과 이벤트를 실제 스킬 실행에 연결한다. 미정 수치·보상·피격 경직·그로기 조건은 임의 구현하지 않는다.

## 이전 세션 기록 — 2026-11-03 종료 요청: 강공격 차징 구현 중

**다음 세션은 이 절과 `docs/AI/HeavyChargeHandoff.md`부터 읽는다.** 사용자가 컴퓨터를 끄기 위해 작업을 중단하고 인수인계를 요청했다. 강공격은 아직 완료/컴파일 검증되지 않았다. 아래의 이전 완료 기록을 현재 차징 변경의 통과 결과로 쓰지 않는다.

- 실제 확인한 상태: `feat/PlayerSkill`, HEAD `3da8308` (`테스트용 불꽃 탄환`). 앞선 약공격·패시브·보스 HP 수정은 이 커밋에 포함되어 현재 작업 트리 변경 목록에는 없음. 다음 세션에서도 git 상태를 새로 확인한다.
- 미커밋 변경: Player.cs 및 Mobile/PlayerController2D.cs. 미추적: Player.HeavyCharge.cs, PlayerHeavyCharge.cs, PlayerHeavyAttackButton.cs, PlayerHeavyChargeTests.cs, PlayerHeavyChargeIntegrationTests.cs, 강공격 계획 문서. 이 인수인계 문서 변경도 추가됨. reset/clean/restore 또는 브랜치 전환으로 없애지 않는다.
- 승인 완료: 차징 중 이동 차단·조준 변경 허용. 방어/회피/피격 취소 시 MP 반환 없음. 테스트 MP10, 단계 진입0/.4/.8초, 누적 배율1.1/1.2/1.7. 이 수치는 Inspector에서 변경 가능하게 한다. 정식 데이터 테이블 연결은 준비될 때까지 미룬다.
- 구현 초안: 비용1회/스냅샷/3단계/해제 결과/취소 엔진, C_Attack 포인터 bridge, Player partial 연결, 모바일 이동·조준 분리까지 작성. 실제 강공격 피해/스킬 ID/공격 애니메이션/그로기는 이번 기반 범위가 아님.
- **현재 막힌 지점:** Player.HeavyCharge.cs가 호출하는 `PlayerView.SetHeavyChargeIndicator`가 아직 구현되지 않음. 현 소스는 컴파일 가능한 완료 상태가 아니며 실제 변경 후 컴파일은 아직 실행하지 않았다. PlayerView 표시/정리, MainTestScene의 C_Attack 참조와 승인 설정, 새 파일 meta가 미완료.
- 검증: 구현 전 새 단위 테스트12개 모두 실패(차징 타입 부재)를 확인한 것뿐. 구현 후 재컴파일/통과 테스트/통합 테스트/리뷰는 미실행. 마지막 상태 확인 시 실행 중 Unity 프로세스 없음. 컴퓨터 종료/재시작 명령을 실행한 적 없음.
- 다음 순서: 표시 메서드와 정리 구현 → C_Attack/테스트 설정 최소 연결 → meta 준비 → 별도 검증 프로젝트에 최신 파일 반영 → 차징 단위·통합 및 관련 회귀 테스트 → 코드 리뷰/수정 → 실제 확인 절차·Handoff 갱신. 상세 API/파일/주의점은 HeavyChargeHandoff.md에 기록함.

## 2026-11-03 첫 약공격 불꽃 탄환 연결

- 사용자 요청으로 이전 작업순서1(병합 전체 선행 재검증)을 생략하고2(첫 스킬 연결)를 진행했다. 시작/현재 `feat/PlayerSkill`, HEAD `ccac76f`, 시작 작업 트리 깨끗함. 임의 브랜치 전환·커밋 없음.
- 사용자 확정: 불꽃 탄환10001, MP10, 피해10, 기존 Bullet 재사용+임시 빨간 원, 기존8방향 Attack 프레임, 모바일 버튼 N_Attack. 기타 미정 스킬 수치를 채우지 않았다.
- Data.SkillData에 ActiveConfigured/MPCost/BaseDamage를 추가하고 기존 DamageMultiplier와 함께 실행 설정을 검증한다. PlayerLightSkill이 보유/약공격 장착·리소스·애니메이션·MP를 확인한 뒤 방향/피해/속도/타이밍을 캡처해 시전한다. 메타데이터만 있는 스킬은 무소모·미발동.
- PlayerView와 LightAttackAnimationState가 Animator의 실제 시작/진행/종료를 실행 ID로 연결한다. 초기 즉발 시점0에서 풀의 빨간 탄환을 한 번 발사하고 상태 종료 시 잠금을 해제한다. 약공격 중 이동/공격/방어/회피 거절, 중복/예약 입력 방지, 정지·사망·비활성화/Animator중단 정리. MP 알림 중 pause/Animator무효화는 시작 전 환불하며 수락한 설정은 스냅샷으로 유지한다.
- 기존 Player/Attack을 선택 복제해 PC 입력을 재사용했다. N_Attack은 런타임 PointerDown/Submit bridge로 같은 API를 호출한다. Touchscreen의 일반 tap 입력은 중복 처리하지 않으며 Mouse는 현재 위치 GraphicRaycaster 검사로 UI 클릭을 배제한다. InputAction 에셋·기존 N_Attack onClick은 변경하지 않았다.
- Bullet.InitDirection을 추가하되 기존 Init API와 풀링 계약 유지. 새 FlameBullet은 기존 Bullet/Collider/Rigidbody를 복제 재사용하고 빨간 Sprite로 표시한다. 원본 Bullet.prefab 해시 유지.
- 기존 Player.controller20개 객체를 모두 보존하고 LightAttack 상태/트리/Behaviour/종료 전이4개를 추가했다. Controller meta/GUID 유지. 새8방향 클립은9프레임/12fps, Unity 조회0.75초의 초기 연출값이다. 처음에는 끝 중복 키로0.8333초였음을 검사로 발견해 중복 제거/stopTime 명시 후 재검증했다. 기존 Animator 전체 Generate는 실행하지 않았다.
- MainTestScene의 기존 스탯/MP설정(공격력3, 탄속3, 최대MP100·회복1/s 등)을 유지하고 Player N_Attack Button1415531113/새 투사체 참조, 첫 빈 보유 슬롯10001/LightAttack/MP10/Base10/ActiveConfigured, lightSkillId10001만 최소 변경했다(추가9줄/교체4줄). 이 씬의 승인된 검증 설정이며 모든 PlayerData 기본값에 자동 지급하지 않는다. 전투 피해는 Base10+기본공격력×Multiplier0=10이다.
- 새 리소스 Assets/Resources/SkillProjectiles/FlameBullet.prefab·FlameBulletCircle.png, LightAttack_*.anim8개, Editor의 PlayerLightAttackSetup 및 런타임3개 클래스/테스트 추가. 원본 워크북/다른 씬/기존 프리팹/입력 에셋/패키지 수정 없음. 생성 작업은 이전 검증 복사본에서 Editor API로 수행해 확인 후 원본에 반영했다.
- TDD: 최초7개 실패 → 구현7/7 통과. MP 알림 pause/Animator변경·메타데이터 스냅샷 실패를 추가 재현해 수정. PC WasPressed는 테스트 InputSystem 갱신 프레임에서 검사하도록 보완했다. UI 첫 클릭은 유효한 화면 배치/포인터 캐시 미갱신 상태로 재현해 현재 위치 Raycast로 수정했다.
- 최종 Unity6000.5.0f1 변경 관련 검증 **73/73 통과**, 실패/생략0: 신규 PlayerLightSkillTests13 + MP19 + 탄환6 + HUD12 + 상태창9 + 팝업13 + 임시 그래픽 렌더1. 현재0.Scripts 소스를 검증본에 반영했고 원본에서 삭제된 과거 MVP파일은 검증 복사본에서만 정리했다. 이는 요청대로 전체 병합 회귀/보스 통합 검증은 아니다.
- 독립 리뷰에서 MP 재진입 경계와 클립 길이를 확인해 수정 후 재검증했다. 추가 확정 런타임 결함은 없다는 정적 리뷰 결과를 받았다. 실제 공격 자세/빨간 원을 오프스크린 카메라로 렌더해 픽셀과 이미지로 확인했다. 원본 MainTestScene 실제 플레이/터치·보스 전체·Android 빌드/기기는 미검증.
- 영속 테스트 Assets/Tests/PlayMode/PlayerLightSkillTests.cs(13), 계획 docs/superpowers/plans/2026-11-03-flame-light-skill.md, 사용법 docs/AI/FlameBullet.md. 결과 `.utmp/FlameSkill/red.xml`, `review-red.xml`, `final.xml`, `final.log`, `flame-cast.png`, 검증 위치 validation-path.txt. 렌더 헬퍼는 같은 임시 폴더에만 있으며 원본 테스트 에셋에는 넣지 않았다.
- 직접 확인: 컴파일 완료 → MainTestScene → Play(필요하면 Tools > Play From Title 끄기) → N_Attack → 빨간 직선탄/MP10 차감/Attack 모션 중 이동·연타·패링·회피 거절/종료 후 복구 확인. PC는 PlayerController2D를 끄고 월드 마우스 왼쪽 클릭 또는 UI 선택이 없는 Enter. 이동 입력/마지막 바라본 방향이며 마우스 위치 유도는 아니다. 기존 MP회복1/s는 유지하므로 차감 후 서서히 회복한다. 일시정지 상태창 첫 칸의 불꽃 탄환 보유도 확인한다.
- 오류 시 Console 첫 오류, Player Light Attack Button=N_Attack, Light Attack Projectile=FlameBullet, Loadout10001 장착/ActiveConfigured/MPCost/BaseDamage/AnimName 및 Animator LightAttack Behaviour/Idle 전이를 확인한다. 정상 자동 탄환은 별도로 계속 발사된다. 분리 검증은 Play 중 기존 Player.bulletPrefab만 잠시 비우고 저장하지 않는다.
- 다음 우선순위: 사용자 실제 플레이 확인 후 첫 스킬 연출속도/조준·입력 피드백 조정, 스킬 테이블의 승인 수치/리소스 정식 로딩과 획득 경로 연결. 보유 전체 패시브 발사, 다른 약공격, 강공격·전략·성장·MP 부족 점멸·미정 그로기는 아직 후속이다.

## 2026-11-03 CombatPopupController CS0618 경고 수정

- 현재 브랜치 `main`, HEAD `756c2ee`. 이전 작업은 `dcc8e43`에 커밋됐고 이후 보스 병합이 들어온 상태다. 시작 시 Player_GiungTest 스크립트/asmdef 및 meta 삭제, Boss.controller·MainTestScene·NanumGothic Combat SDF 사용자 변경이 있었으며 건드리지 않았다.
- CombatPopupController.ShowPause의 `FindObjectsByType<Player>(FindObjectsSortMode.None)`를 Unity6000.5의 인자 없는 `FindObjectsByType<Player>()`로 한 줄 변경했다. 같은 씬 필터와 첫 Player 선택 동작은 유지한다. 다른 파일의 검색 호출이나 사용자 변경은 일괄 수정하지 않았다.
- 검증: 이전 임시 Unity6000.5 검증본에 해당 파일을 반영해 C# 컴파일 및 CombatPopupTests13 + PlayerStatusTests9 = **22/22 통과**, 실패/생략0. 해당 파일의 CS0618/컴파일 오류 로그가 없는지 확인했다. git diff --check 통과. 현재 보스 병합·사용자 변경까지 포함한 원본 전체 프로젝트 검증은 아니다.
- 결과 `.utmp/PopupApiWarning/final.xml`, `final.log`. 새 테스트는 추가하지 않고 기존 관련 회귀를 실행했다. 커밋·브랜치 전환 없음.
- 직접 확인: Unity 재컴파일 완료 → Console에서 CombatPopupController의 해당 CS0618 경고가 새로 발생하지 않는지 확인 → MainTestScene → Play → 일시정지 상태창 열기/뒤로가기. 기존과 같이 스탯·보유 목록 표시 및 전투 정지/재개가 정상이어야 한다. 오류가 남으면 파일/줄 번호가 같은지, Console 첫 새 오류와 상세 스택을 확인한다.
- 다음 작업은 기존 우선순위를 유지한다: 실제 스킬 실행/약공격 버튼·애니메이션·타격 연결. 병합 후 전체 프로젝트 기준 검증과 실제 원본 씬/기기 검증은 별도다.

## 2026-11-03 보유/장착 모델·일시정지 상태창·음량 옵션

- 사용자 참고 이미지의 상태창 요청을 구현했다. 첫 이미지는 배치/색상, 두 번째는 영역 설명으로 사용했다. 기존 옵션 창이 없음을 확인한 뒤 사용자에게 음량 옵션 포함 승인을 받았다. `main`, HEAD `f3fd42c` 유지, 커밋/브랜치 전환 없음.
- PlayerData.Loadout과 PlayerLoadout 추가: 공격8칸 중 기본6·해금2, 전략5칸, 유물/증강 각60개, 보유 스킬만 약/강/전략 버튼에 하나씩 장착, 제거 시 장착 참조 정리. 기존 Data.SkillData를 재사용하며 새 전역 인벤토리 매니저를 만들지 않았다. 실제 스킬/유물을 자동 지급하지 않는다.
- 기존 CombatPopupController.ShowPause가 같은 씬 Player를 찾아 pause ContentRoot에 PlayerStatusPanel을 한 번 구성한다. 실제 스탯/보유 데이터 이벤트로 갱신하고 닫기/소스 교체 시 구독 정리. 남색 패널·크림 스탯·색상별 제목·8/5 슬롯·가로 목록·톱니 옵션·우하단 뒤로가기 배치를 참고 이미지에 맞췄다. 모델이 없는 스탯은 —로 표시한다.
- 기존 CloseButton/팝업 정지 소유권/스택을 유지했다. 옵션은 동일 팝업 내부 전환이며 옵션 뒤로가기는 상태창으로만 돌아온다. 옵션 진입 시 전체 음량 Slider, 복귀 시 기존 뒤로가기 버튼으로 EventSystem 선택을 옮겨 키보드/패드 탐색을 유지한다.
- SoundManager에 전체(AudioListener)·BGM·효과음 채널 음량 API를 추가했다. 기존 AudioSource와 씬 재초기화에 설정을 반영한다. 앱 재시작 영구 저장은 추가하지 않았다.
- 씬/프리팹/기존 meta/패키지/설정/엑셀 변경 없음. MainTestScene과 CombatUI.prefab SHA-256이 시작값과 동일함을 확인했다. 상태창은 런타임 생성이므로 비실행 원본 프리팹에는 기존 기본 패널이 보일 수 있다.
- 실제 테이블을 이번에 발견했다: SkillDatatable.xlsx(약11/강5/전략10), Strengthentable.xlsx. 이름·일부 등급/속성은 있으나 비용·계수·애니메이션·프리팹 대부분 비어 있다. 보유/장착/조회만 구현했으며 기본 Bullet을 보유 스킬 전체의 패시브로 바꾸거나 액티브 버튼/약공격 타격을 연결하지 않았다. Fusion10011 중복 정의와 원본 Cooldawn 컬럼 매핑에 향후 주의한다. 이전 ‘xlsx 미발견’ 기록은 과거 상태다.
- TDD: 새 테스트7개 모두 기존 코드에서 실패 확인. 첫 구현에서 새 UI helper meta GUID 길이 입력 오류로 파일이 임포트되지 않아 컴파일 실패했고 새 meta만 올바른32자리로 수정했다. 리뷰에서 옵션 선택 포커스 문제를 찾고 새 Submit/Move 테스트 실패를 확인한 뒤 수정. 실제 CombatUI 프리팹 통합 테스트도 추가했다. 독립 재리뷰에서 추가 수정 필요 결함 없음.
- 최종 검증: Unity6000.5.0f1 임시 검증본에서 **111/111 통과**, 실패/생략0. 기존60 + PlayerStatusTests9 + CombatPopupTests13 + UIPopupTests17 + 씬 비의존 사운드11 + 그래픽 렌더1. C# 컴파일·git diff --check·최종 소스/테스트 해시 일치 확인.
- 검증 제한: 초기에 사운드 씬 전환2개까지 확장한 실행은112개 중110통과/2실패. 최소 복사본의 MainTestScene 입력/스폰 참조 의존성이 누락되어 Player/EnemySpawnDirector 설정 오류가 발생했다. 원본 씬을 고치거나 실패를 숨기지 않고 이2개를 최종 비씬 회귀 집합에서 분리했다. 전체 원본 MainTestScene 실행·실제 터치/기기/Android 검증은 하지 않았다.
- 그래픽 QA: 그래픽 사용 검증 Unity의 오프스크린 카메라에서 빈 상태·보유 테스트 데이터·목록 끝 스크롤·음량 화면 PNG를 렌더해 직접 확인했다. 테스트 데이터는 검증 픽스처에만 존재한다. 첫 캡처에서 잔존 검증 씬 배경이 분홍색으로 나와 미리보기 카메라를 UI 레이어/명시적 배경으로 분리했다. 첫 목록 폭과 스탯 줄바꿈을 보완 후 재렌더했다. 콘텐츠/뷰포트 폭 비1.5, 스크롤바 크기2/3 및 마지막 항목 표시를 확인했다.
- 영속 테스트: Assets/Tests/PlayMode/PlayerStatusTests.cs(9개). 계획 docs/superpowers/plans/2026-11-03-pause-status-loadout.md. 직접 확인/API 설명 docs/AI/PlayerStatus.md. 결과 `.utmp/PlayerStatus/red.xml`, `review-red.xml`, `final.xml`, `final.log`, 미리보기 status-empty/populated/scroll-end/options.png. 검증 프로젝트 위치 validation-path.txt. 임시 렌더 헬퍼는 같은 폴더 StatusPreviewTests.cs이며 원본 런타임/테스트 에셋에는 넣지 않았다.
- 직접 확인: Unity 컴파일 완료 → MainTestScene → Play(필요하면 Tools > Play From Title 끄기) → 오른쪽 위 기존 일시정지 → 스탯/공격6빈칸+잠금2/전략5/유물·증강 가로 목록 확인 → 톱니 → 전체/BGM/효과음 조절 → 옵션 뒤로가기에서 전투 계속 정지 → 상태창 뒤로가기에서 재개 확인. 반복 열기도 확인한다.
- 보유 표시 확인: Play 중 PlayerData.Loadout.Attack Skills 첫 항목을 실제 표의 DataId10001/Name불꽃 탄환으로 설정 후 다시 열면 이름이 보인다. 자동 획득/발사 확인이 아니라 임시 표시 확인이며 저장하지 않는다. 정상 획득 코드는 Loadout.TryAddAttackSkill 등 API를 호출해야 Changed가 즉시 갱신한다. 원본 아이콘 키가 비었거나 사전 로딩되지 않았으면 이름 표시로 대체한다.
- 오류 시 Console 첫 오류, CombatUI Controller의 Pause Popup ContentRoot/CloseButton/Status Player와 PlayerData.Loadout을 확인한다. 데이터 미보유와 잠금은 오류가 아니다. 한글 폰트는 기존 Assets/UI/Fonts의 NanumGothic Combat SDF를 사용한다.
- 다음 작업: 3단계 남은 실제 스킬 실행/약공격 버튼·애니메이션·타격 연결. 첫 스킬의 MP 비용·계수·애니메이션/프리팹 등 미정 값을 확정해야 하며 임의로0비용/효과를 채우지 않는다. 실제 획득/성장/유물·증강 해금 효과와 MP 부족 점멸도 후속 범위다.

## 2026-11-03 HP 입력 버그 수정 및 MP·EXP HUD 연결

- 사용자 요청: Mobile UI/PlayerHUD의 HP·MP·EXP를 PlayerData → PlayerView 흐름으로 통일하고 이동 방향에 HP 표시가 변하는 버그 수정. 시작 브랜치 `main`, HEAD `f3fd42c`; 기존 코드·문서·씬 사용자 변경 보존, 커밋/브랜치 전환 없음.
- 원인 재현: MainTestScene의 Player_HP/MP/EXP Slider가 Interactable=true, Navigation=Automatic이었다. 기존 PlayerView에 연결한 HP Slider 선택 후 좌측 Move 이벤트를 보내면 모델 HP는 50인데 표시가 0.5→0.4로 감소함을 새 테스트로 확인했다. 선택 여부에 따라 재현되는 UI 탐색 입력 문제이며 체력/이동 수식 오류가 아니다.
- 수정: PlayerView에서 세 Slider를 transition=None, navigation=None, interactable=false로 표시 전용 설정하고 모델 값을 SetValueWithoutNotify로 반영한다. 기존 레이아웃·색상·채움 방향 및 사용자 지정 Slider 범위는 유지한다. 최대치 0·비정상 수치는 빈 표시로 처리한다.
- MP는 기존 PlayerData.MPChanged를 구독, EXP는 PlayerData에 maxEXP/CurrentEXP 및 EXPChanged·InitEXP·SetEXP(current,maximum)를 추가해 같은 흐름으로 표시한다. 초과 EXP는 모델에 유지하고 바만 100%로 제한한다. EXP 요구량 기본 0이며 획득/드랍·레벨업·보상 규칙은 추가하지 않았다.
- Player의 기존 healthSlider와 PlayerView 생성자 API를 유지했다. 선택 MP/EXP 참조와 BindResourceSliders를 추가하고 재활성화 때 RefreshResources로 갱신한다. PlayerView.Dispose는 세 데이터 이벤트를 모두 해제한다. 참조가 없는 기존 씬도 안전하게 동작한다.
- MainTestScene의 Player 컴포넌트에 mpSlider=1395659084, expSlider=1801941379 참조 두 줄만 추가했다. 기존 healthSlider=496842392 및 PlayerHUD 자식 Slider fileID를 확인했다. 시작 씬 원본에 정확히 두 줄을 삽입한 결과와 종료 파일이 일치하며 기존 Grid→Ground 사용자 변경도 유지된다. GiungTestScene·프리팹·기존 meta·설정·패키지·InputAction 에셋 수정 없음.
- 검증: 새 HUD 테스트 12/12가 기존 코드에서 실패한 뒤 수정. Unity 6000.5.0f1 임시 검증 프로젝트에서 **60/60 통과**, 실패/생략 0(HUD 12 + MP·행동 19 + 탄환 6 + Core 4 + 씬 비의존 적 풀링 19). C# 컴파일 및 git diff --check 통과. 독립 코드 리뷰에서 수정 필요 결함 없음. 소스/씬 해시가 검증 복사본과 일치한다.
- 범위: 실제 MainTestScene 실행 화면·터치·Android는 이번에도 검증하지 않았다. 씬 참조는 파일ID 정적 검사, UI 동작은 실제 uGUI 컴포넌트 기반 PlayMode 테스트로 검증했다. Player 재활성화 표시 갱신은 코드로 확인했으며 별도 씬 통합 테스트는 없다. EXP 성장 시스템 및 MP 부족 점멸은 후속 범위다.
- 테스트 원본: Assets/Tests/PlayMode/PlayerHudTests.cs. 결과 `.utmp/PlayerHUD/red.xml`, `final.xml`, `final.log`, 검증 위치 validation-path.txt. 계획 docs/superpowers/plans/2026-11-03-player-resource-hud.md.
- 직접 확인 1: Unity 컴파일 완료 → MainTestScene → Play(자동 타이틀 시작이면 Tools > Play From Title 옵션 확인) → PC 검사에서는 PlayerController2D 끄기 → 적 피해를 받지 않는 상태에서 Mobile UI/PlayerHUD/Player_HP 클릭 후 좌우 이동. HP 값과 채움이 그대로이고, 실제 피격 때만 줄어드는 것이 정상이다. HP/MP/EXP는 입력용 Slider가 아니므로 Play 중 Interactable 해제·Navigation=None이 정상이다.
- 직접 확인 2: Play 중 PlayerData에 테스트용 Max MP=10, Dash MP Cost=3, MP Recovery Per Second=0 지정 → 컴포넌트 메뉴 MP > 현재 설정으로 초기화 → MP바가 가득 참 → WASD 이동 중 Left Shift 회피 → MP=7일 때 70% 표시 확인. EXP는 아직 획득 경로가 없으므로 SetEXP(25,100)→25%, SetEXP(125,100)→100% 표시를 자동 테스트로 확인했다. 레벨업/경험치 획득이 이미 연결됐다고 설명하지 않는다.
- 직접 확인 3: Tools > Play From Title 비활성화 → Window > General > Test Runner → PlayMode → PlayerHudTests → Run Selected. HP 선택/좌우 입력, MP 소모/회복, EXP 표시/잘못된 값, 이벤트 해제 테스트 12개 통과 확인.
- 오류 시 Console, Player의 Health/MP/EXP Slider 참조(각 PlayerHUD 자식의 Slider인지), PlayerData의 최대치와 초기화 여부를 확인한다. Inspector에서 필드를 직접 바꾼 것은 데이터 API 이벤트 호출과 다르므로 MP 초기화 메뉴나 SetEXP API로 확인한다. Play 중 테스트값은 저장하지 않는다.
- 다음 우선순위: 원래 3단계 스킬 보유·버튼 장착 및 약공격 애니메이션/타격 연결. EXP 성장 규칙·실제 테이블 미확정은 유지.

## 2026-11-03 2단계 MP 및 행동 제어 기반 완료

- 사용자가 1단계 탄환 풀링 직접 확인 후 두 번째 단계 진행 승인. `main`, HEAD `f3fd42c` 유지, 커밋/브랜치 전환 없음. 이전 탄환 변경과 MainTestScene 사용자 변경을 보존했으며 씬 SHA-256 일치 확인.
- PlayerData에 현재/최대 MP, 유효 비용 차감, 상한 회복, 자연 회복, MPChanged/MPSpendRejected 알림을 추가했다. maxMP·mpRecoveryPerSecond·dashMPCost·parryMPRecovery는 Inspector 설정값이고 미정이므로 기본 0. 실제 게임 수치를 임의 확정하지 않았다. `MP/현재 설정으로 초기화` 컴포넌트 메뉴로 Play 중 설정을 적용할 수 있다.
- 플레이어 소유 PlayerActionGate와 Player.TryBeginLightAttack(cost,out actionId)/CompleteLightAttack(actionId) 계약 추가. 잠금 중 공통 CanAcceptGameplayInput=false로 이동/다른 스킬 시작/방어/회피를 거절한다. 이전 실행 ID 종료는 새 잠금을 풀지 않는다. 사망/비활성화 때 잠금과 진행 코루틴을 정리한다. 다음 단계 스킬 실행부가 시작 성공 후 ID를 애니메이션 실행에 연결해야 한다.
- PC 이동 및 PlayerController2D의 Rigidbody 이동에 같은 잠금을 적용하고 잠금 시작 즉시 모바일 속도를 정리한다. 회피는 방향·쿨타임·진행 상태 확인 후 MP를 차감한다. MP 동기 알림 중 재진입을 예약 상태로 차단하며, 비활성화로 실제 시작하지 못한 회피는 비용을 복구한다. 이는 강공격 차징 취소 환불 정책과 별개다.
- 자연 회복은 전투 시간으로 진행하며 일시정지 중 멈춘다. 패링 성공 알림을 연결해 접촉이 여러 번 와도 한 번만 설정량 회복한다. 패링/가드 모션 중 자동 발사를 중단하며, 약공격 잠금 중 기존 자동 패시브 발사는 유지한다.
- 범위 제한: 실제 약공격 버튼·애니메이션·타격·장착은 3단계. MP HUD 게이지/부족 점멸은 아직 연결하지 않았으며 데이터 이벤트만 제공한다. 가드 MP 부과 방식·강공격 환불·그로기·미정 수치도 이번에 확정하지 않았다. 씬·프리팹·InputAction 에셋·애니메이션·패키지 변경 없음. 테스트 asmdef에 Unity.InputSystem 참조만 추가했다.
- 검증: 기존 코드에서 새 17개 테스트 실패 확인. 첫 구현 16/17 통과 후 PC 키보드 테스트의 배치 포커스 문제를 분리했다. 테스트가 복제한 InputSettings에만 IgnoreFocus/게임뷰 입력을 적용하고 원래 설정을 복구하도록 보완했다. 리뷰에서 찾은 MP 알림 재진입·비활성화 취소를 추가 2개 테스트로 먼저 실패시킨 뒤 수정했다. 최종 Unity 6000.5.0f1 검증 복사본에서 **48/48 통과**, 실패/생략 0(MP·행동 19 + 탄환 6 + Core 4 + 씬 비의존 적 풀링 19). 독립 재리뷰에서 중요한 잔여 결함 없음. C# 컴파일 및 git diff --check 통과.
- 검증 환경은 이전 BulletPooling 임시 소스 프로젝트 재사용. 원본과 같은 개별 패키지 캐시지만 필요한 패키지만 참조하는 최소 구성이다. 실제 원본 씬·그래픽·전체 패키지 조합·Android 빌드/기기 검증은 별도이며 이전 기록을 현재 결과로 재사용하지 않았다.
- 재현 테스트: Assets/Tests/PlayMode/PlayerResourceActionTests.cs. 결과 `.utmp/PlayerMP/red.xml`, `review-red.xml`, `final.xml`, `final.log`; 검증 위치 validation-path.txt. 문서 계획은 docs/superpowers/plans/2026-11-03-player-mp-actions.md.
- 직접 확인 1: Unity 컴파일 완료 → Tools > Play From Title 비활성화 → Window > General > Test Runner → PlayMode → PlayerResourceActionTests → Run Selected. MP 부족 무소모, 애니메이션 종료 ID, PC/모바일 잠금·복구, 사망/비활성화, 회피 재진입, 중복 패링 회복 테스트 19개가 통과하는 것이 정상이다.
- 직접 확인 2: MainTestScene → Play(자동 Title 시작이면 Tools > Play From Title 옵션 확인) → Player 선택 → PC 검증 시 PlayerController2D 비활성화(사용자 확인 방법 반영) → PlayerData에 테스트용 Max MP=10, Dash MP Cost=3, MP Recovery Per Second=0 지정 → 컴포넌트 메뉴 MP > 현재 설정으로 초기화 → WASD 이동 중 Left Shift 회피. Current MP가 10→7→4→1로 줄고 부족한 다음 시도는 추가 회피나 MP 차감 없이 거절되는 것이 정상이다. 회피 쿨타임이 있으므로 각 시도 사이 기존 설정만큼 기다린다. 회복은 MP Recovery Per Second=2 등 명시적 테스트값으로 별도 확인하며 이 값은 확정 기획값이 아니다. Play 중 테스트 설정은 저장하지 않는다.
- 약공격 버튼은 아직 없으므로 실제 버튼으로 입력 잠금을 확인했다고 보고하지 않는다. 잠금은 이번 단계 자동 테스트로 확인했고 다음 단계에서 실제 애니메이션/버튼으로 검증해야 한다. MP 게이지가 아직 변하지 않는 것은 HUD 미연결 상태다. 오류 시 Console·실패 테스트명, PlayerData MP 설정, Player inputActions, PC/조이스틱 제어 모드와 Rigidbody 속도를 확인한다.
- 다음 우선순위: 3단계 스킬 보유/버튼 장착 모델과 약공격 실행·애니메이션 연결. 실제 SkillDatatable.xlsx 미발견 및 개별 스킬 계수 미정은 유지한다.

## 2026-11-03 플레이어 탄환 풀링 구현 완료

- 브랜치 `main`, HEAD `f3fd42c` 유지. 커밋·브랜치 전환 없음. 시작 시 MainTestScene 및 이전 설계/Handoff 변경을 보존했고, 종료 시 MainTestScene SHA-256이 시작값과 같음을 확인했다.
- PlayerCombat.FireAt을 `Managers.Pool.Pop`으로 연결하고 활성화 전 기존 `Bullet.Init(Transform, float, float)` 호출. Bullet은 IPoolable을 구현하며 적중·수명 종료·화면 이탈에 풀로 반환한다. 비풀링 Init 호출은 Destroy 호환 경로를 유지한다.
- 대여/반환 때 피해·속도·방향·남은 수명·적중/초기화 상태와 Rigidbody 속도를 정리한다. 예약 Destroy 대신 수명 카운트다운을 사용한다. 피해 콜백 중 재대여되면 대여 버전으로 새 발사를 보호한다.
- 발사 순간 방향만 저장하고 타깃 참조를 유지하지 않아 표적 이동·소멸·풀 재사용으로 비행 중 방향이나 수명을 바꾸지 않는다. 카메라 부재 시에도 수명으로 회수한다. 화면 기반 타깃 선정·MP·스킬·입력 잠금은 이번 범위에서 변경하지 않았다.
- 씬·프리팹·기존 meta·Project Settings·패키지 변경 없음. 기존 Bullet.Init API와 DeleteTime 직렬화 필드명 유지. 새 PlayMode 테스트/meta 및 구현 계획 문서 추가.
- 검증: Unity 6000.5.0f1 검증 복사본에서 기존 코드의 새 테스트 6/6 실패 확인 후 수정. 첫 수정 검증 9/10에서 비활성 객체 SendMessage 테스트 호출 문제를 수정했다. 최종 탄환 6 + CoreInitialization 4 + 씬 의존 테스트를 제외한 PooledEnemySpawning 19 = **29/29 통과**, 실패/생략 0. C# 컴파일 성공, git diff --check 통과. 독립 정적 리뷰에서 수정 필요 결함 없음.
- 검증 환경: 원본 Unity를 다시 열지 않고 짧은 Temp 경로에 코드·테스트·Joystick Pack·설정 복사본 사용. 라이선스 초기화 지연 후 검증 프로세스만 권한 재실행했다. 복사본에만 PlayFromTitle 배치 guard와 필요한 기존 로컬 캐시 패키지 참조를 적용했다. 원본 패키지 구성 그대로의 전체 프로젝트/씬/그래픽 검증은 아님.
- 결과: `.utmp/BulletPooling/red.xml`, `final.xml`, `final.log`; 검증 위치는 같은 폴더 validation-path.txt. 임시 결과는 참고이며 재현 가능한 테스트 원본은 Assets/Tests/PlayMode/PlayerBulletPoolingTests.cs에 있다.
- 남은 검증: 실제 MainTestScene 물리 충돌·화면 표시, Android 빌드·기기·성능. Rigidbody 속도 초기화 및 피해 콜백 내부 즉시 재대여는 정적 검토했지만 별도 직접 테스트로 계측하지 않았다.
- 직접 확인: Unity 컴파일 완료 → Console 오류 확인 → MainTestScene → Play(자동 Title 시작이면 Tools > Play From Title 옵션 확인) → 적이 범위 안에 있을 때 발사 → Hierarchy `@Managers/@Pools/@BulletPool` 아래 탄환 선택 → 적중/화면 이탈 시 같은 인스턴스가 비활성화되고 다음 발사에 재활성화되는지 확인. 반복 재사용 후에도 적 체력이 줄고 탄환이 회수되는 것이 정상이다. 동시 비행 수가 늘면 필요한 새 인스턴스가 추가되는 것은 정상이다.
- 오류 시 Console 메시지, Player.bulletPrefab, Bullet·PooledObject 컴포넌트, 카메라 MainCamera 태그를 확인한다. PooledObject는 첫 풀 생성 시 자동 부착되므로 프리팹 수동 수정은 불필요하다.
- 다음 우선순위: 승인된 스킬 설계의 MP·행동 제어 구현 범위를 구체화한다. 화면 내 대상 선정 등 탄환 기획 정합성은 별도 작업이며 이번 풀링 완료와 혼동하지 않는다.

## 2026-11-03 스킬 후속 합의 및 설계 초안

- 최신 결정은 `docs/superpowers/specs/2026-11-03-player-skills-design.md`에 정리했다. 아래 초기 검토의 유도탄 설명과 이동 약공격 제안은 이 합의로 대체된다.
- 탄환은 발사 때 화면 내 가까운 일반/엘리트/보스를 다시 선택하되 발사 순간 위치로 직진하며 비행 중 유도·재타겟하지 않는다.
- 약공격 애니메이션 중 이동·공격·전략·방어·회피 입력을 모두 차단하고 종료 후 해제한다.
- 차징 배율은 누적 증가, 시간·배율은 조정 가능한 테스트값, MP는 차징 시작 때 차감. 전략 쿨타임 6초 기준. 보유 스킬은 패시브/액티브를 가지며 슬롯 8개 중 6개 기본 활성, 2개 해금 가능. 버튼당 스킬 하나 장착. 보스 그로기 미정.
- 다음 작업: 설계 초안 기준으로 탄환 정합성·풀링부터 구체적 구현 계획을 작성한다. 자동 패시브의 약공격 중 발사 여부, 장착 제약·개별 효과·취소 환불 등은 확정으로 취급하지 않는다.
- 런타임 코드·씬·프리팹 변경 없음. 사용자 MainTestScene과 Office 잠금 파일 보존. Unity 실행·컴파일·테스트 0개. 문서 변경에 git diff --check 수행.

## 2026-11-03 스킬 기획 구현 방향 검토

- `Zomso_Skill_기획서.docx`와 루트 종합 기획서를 읽고 Player/PlayerCombat/Bullet/PlayerDefense/PlayerData 및 기존 SkillData·리소스·풀·팝업 코드를 비교함. 구현은 요청되지 않아 런타임 코드와 에셋은 변경하지 않음.
- 제안: 기존 Player 구성에 플레이어 소유 스킬 실행·우선순위 중재를 추가하고, MP는 PlayerData, 스테이지별 전략 횟수 초기화 신호는 게임 진행, 로딩과 풀링은 기존 Core를 사용. 데이터 정의와 실행 중 상태를 분리.
- 차이: 현재 Bullet은 범위 내 직선탄·Instantiate/Destroy이며, 화면 내 유도탄·차징/방어 중 자동 발사 중단과 다름. PlayerData는 HP 중심이고 MP 실행 규칙이 없음. 기존 SkillData는 예제 스키마이며 기획 컬럼을 충분히 표현하지 못함. 추적 파일 검색에서 SkillDatatable.xlsx는 발견하지 못함.
- 구현 전 결정할 내용: 비행 중 탄환 재타겟 여부/일반 적과 보스의 타깃 범위, 차징 3단계 임계시간과 배율 누적 방식, MP 차감 시점 및 취소별 환불, 전략 쿨타임 본문 6초와 QA 6~10초 차이, 다중 보유 스킬과 버튼 연결, 보스 그로기 임계값. MP 명칭 통일하며 문서 잔존 구표기는 새 코드에 반영하지 않음.
- 권장 순서: MP·상태 우선순위 → 자동 탄환 정합성·풀링 → 약공격 → 강공격 → 전략 효과·스테이지 초기화 → 데이터 확장·성장 연결·연출. 검증 순서는 MP 부족/이동 약공격/차징 캔슬/전략 제한/일시정지 및 재시작/풀 재대여.
- 이번 검토의 Unity 실행·컴파일·테스트는 0개. 사용자 MainTestScene 변경 보존. 다음은 브리핑의 미정 사항을 확정한 뒤 설계·구현 범위를 정하는 것.

## 2026-11-03 플레이어 Bullet 풀링 확인

- 현재 상태: `main`, HEAD `f3fd42c`. 시작 시 사용자 변경 `Assets/9.Scenes/MainTestScene.unity`가 있었으며 수정하지 않음.
- 확인 결과: 플레이어 Bullet은 풀링되지 않음. `PlayerCombat.FireAt`이 `Object.Instantiate`로 생성하고, `Bullet`은 충돌·화면 이탈·타깃 소멸·수명 종료에 `Destroy`를 사용함. 공통 `PoolManager`와 풀링 예제는 있지만 실제 플레이어 발사 경로에 연결되지 않음.
- MainTestScene의 bulletPrefab GUID는 `Assets/1.Prefabs/Bullet.prefab`와 일치하고 해당 프리팹은 실제 `Bullet` 스크립트를 참조함.
- 검증: 소스 및 씬/프리팹 참조 정적 확인. Unity 실행·컴파일·테스트 0개. 런타임 인스턴스 재사용은 계측하지 않음. 코드·씬·프리팹 변경 없음.
- 직접 확인: MainTestScene에서 Play → 적이 공격 범위에 있는 동안 발사 확인 → Hierarchy에서 Bullet(Clone)이 생성 후 사라지는지 확인. 발사가 없으면 Player의 bulletPrefab, 적 Enemy 태그·Collider2D·공격 범위 및 Console 오류를 확인.
- 다음 작업: 풀링 적용은 아직 요청되지 않았음. 적용 시 생성/반환 경로 전환과 함께 isHit 등 재대여 상태 초기화 및 예약 Destroy 제거를 검토해야 함. 기존 실제 데이터·Android 검증 제한은 유지.

## 2026-11-03 보스 남쪽 Swipe 잘림 수정

- 시작 상태: `BossId`, HEAD `964ff51`. 기존 변경인 UniversalRenderPipelineGlobalSettings.asset, EditorBuildSettings.asset, ShaderGraphSettings.asset는 보존.
- 재현: Beast_Zombie_Boss/Idle/animations/swipe/south의 4~7.png에서 원본의 불투명 픽셀이 Sprite Rect 밖에 있었음. 각각 526/384/224/125픽셀이 영역 밖으로 잘림. Boss_Swipe_South.anim은 해당 Sprite ID를 직접 참조함.
- 완료: 해당 `.png.meta` 4개의 Sprite Rect만 상하좌우 대칭 확장하고 1픽셀 여유 확보. Center 정렬과 원본 좌표상 중심, GUID, spriteID, internalID를 보존. 코드·씬·프리팹·애니메이션 클립·원본 PNG·Collider는 변경하지 않음.
- 새 Rect(x,y,w,h): 4=(29,26,107,110), 5=(28,25,106,110), 6=(28,23,105,114), 7=(29,22,103,116).
- 검증: 원본 PNG 알파와 Rect 비교에서 수정 전 4개 모두 잘림 재현, 수정 후 영역 밖 불투명 픽셀 0. 중심 좌표 동일, Rect 외 메타 내용 동일 확인. git diff --check 통과. Unity 컴파일/테스트 실행 0개이며 실제 재임포트·화면 재생은 미확인.
- 사용자 확인: Unity 자동 임포트 후 Project → Assets/2.Resources/Art/Beast_Zombie_Boss/Idle/animations/swipe/south → 4~7.png → Sprite Editor에서 손끝과 이펙트 전체가 사각형 안에 들어오는지 확인. 보스 선택 → Animation 창 → Boss_Swipe_South의 4~7프레임을 이동해 잘림 없이 중심이 유지되는지 확인. 이전 영역이면 PNG 4개만 Reimport하고 Console 임포트 오류 확인.
- 다음 작업: 실제 Unity 재생 확인. 다른 방향·프레임은 이번 요청 범위 밖으로 유지.

## 2026-11-03 캐릭터 Sprite Rect 추가 검사

- 캐릭터/보스 원본 Sprite PNG 1,410개 검사(배경 CityPeaceful, Items, Tiles 제외), 읽기 오류 0. 영역 밖 알파 픽셀 검사에서 Beast_Zombie_Boss에 추가 57개 발견. 앞서 수정한 south 4~7은 잘림 0 유지.
- 애니메이션 클립이 참조하는 문제 이미지 33개: swipe/north 0~7, north-west 0·3~7, south 0~3, south-east 0·2~7, south-west 0~7.
- 현재 클립이 직접 참조하지 않는 문제 이미지 24개: swipe/east 0~7, west 0~7, north-east 4~7, swipe-both/east 0·2, west 2·4. 대체 FrameSources를 쓰는 경우 원본 결함과 실제 재생 결함을 구분해야 함.
- 다른 캐릭터는 이번 Rect 검사에서 영역 밖 픽셀 없음. 별도로 PNG 캔버스 가장자리에 그림이 닿는 후보 65개(Beast 보스 6, Magical_Girl_Zombie 1, Pretty_Magical_Girl 43, Zombie 7, Zombie_Construction 8)가 있으나 원본 잘림 여부는 미확정. Tight 메시·압축·실제 화면 가림은 이 검사 범위 밖.
- 이번 요청은 추가 확인이므로 에셋 추가 수정 없음. 다음 작업은 사용자 수정 요청 시 위 목록의 Rect 확장. Unity 테스트 실행 0개, 실제 화면 재생 미확인.
- 직접 확인: 보스 선택 → Animation 창 → Boss_Swipe_North / NorthWest / South / SouthEast / SouthWest에서 해당 프레임 확인. Sprite Editor에서 손·이펙트가 Rect 밖이면 같은 문제. 불일치 시 실제 연결 Sprite와 Console 임포트 오류 확인.

## 2026-11-03 추가 발견 57개 Sprite Rect 수정 완료

- 사용자 요청에 따라 위 추가 검사에서 발견한 Beast_Zombie_Boss 이미지 57개의 `.png.meta`를 수정. 현재 클립 참조 33개와 미참조 원본 24개 모두 포함. 앞서 수정한 south 4~7은 그대로 유지하여 총 수정 메타 61개.
- 주 Sprite Rect를 상하좌우 대칭 확장하여 원본의 모든 알파 픽셀과 최소 1픽셀 여유 포함. 중심 좌표·Center 정렬·GUID·spriteID·internalID·기타 메타 설정 보존. 여러 Sprite가 나뉜 원본 12개는 가장 큰 본체 Sprite만 확장하고 나머지 작은 이펙트 Sprite의 Rect/ID는 유지.
- 검증: 57개 수정 전 잘림 재현 보고서 기반으로 수정 후 캐릭터/보스 PNG 1,410개 재검사 → Sprite Rect 밖 알파 픽셀을 가진 이미지 0개, 읽기 오류 0개. 57개 모두 중심 동일·Rect 이외 메타 내용 동일 확인. 코드·씬·프리팹·클립·Project Settings 등 231개 파일의 작업 전후 해시 동일. git diff --check 통과.
- Unity 컴파일/테스트 실행 0개. 실제 에디터 재임포트·애니메이션 재생은 미확인. 캔버스 가장자리 접촉 후보 65개는 원본 이미지 자체의 별도 조사 대상으로 유지하며 이번 수정으로 해결됐다고 판단하지 않음.
- 사용자 확인: Unity 자동 임포트 완료 → 보스 선택 → Window/Animation/Animation → Boss_Swipe의 각 방향 재생. 손끝·공격 이펙트가 잘리지 않고 보스 중심이 유지되는 것이 정상. 변화가 없으면 해당 PNG만 Reimport하고 Sprite Editor의 Rect 및 Console 임포트 오류 확인.
- 다음 작업: 실제 재생에서 수정 결과 확인. 이전 절의 '추가 수정 없음'은 당시 검사 시점 기록이며 이번 절에서 수정 완료함.

## 2026-11-03 남동쪽 걷기 첫 프레임 참조 수정

- 사용자 스크린샷은 swipe가 아닌 move/south-east/0.png의 Sprite 목록이었음. 0_0은 작은 조각(10x8), 0_1은 본체(83x94)인데 BossWalk_SouthEast.anim 첫 프레임이 0_0을 참조하는 문제 확인.
- 수정: 해당 클립의 첫 Sprite 키와 pptrCurveMapping만 본체 0_1(fileID 8745284039982918138)로 변경. GUID, 나머지 프레임·타이밍·이벤트·코드·씬·프리팹·원본 meta 유지.
- 검증: 본체 Rect에 원본의 전체 알파 픽셀이 포함됨 확인. 키와 바인딩의 본체 참조 2곳 일치, git diff --check 통과. Unity 테스트 0개, 실제 재생 미확인.
- 이전 1,410개 검사는 한 PNG의 모든 Sprite Rect 합집합과 원본을 비교한 것이므로, 작은 조각을 애니메이션이 잘못 참조하는 문제까지 검증한 것은 아님.
- 직접 확인: Unity 임포트 후 보스 선택 → Animation → BossWalk_SouthEast → 0프레임에서 보스 본체가 보여야 함. 여전히 조각만 보이면 클립 Reimport 후 첫 Sprite 키가 0_1인지와 Console 오류 확인.

## 2026-11-03 Enemy/Boss 플레이어 접촉 밀림 방지

- 시작 상태: BossId, HEAD 964ff51. 이전 Sprite Rect 메타 61개, BossWalk_SouthEast.anim, Handoff 및 기존 설정 3개 변경을 보존.
- 원인: EnemyFollow.Awake가 일반 적/보스 Rigidbody2D를 Dynamic으로 설정하여 플레이어 충돌의 물리 반발에 밀림. FixedUpdate에서 속도를 0으로 만들어도 다음 물리 스텝의 위치 보정까지 막지는 못했으며 보스 공격 시 Follow 비활성 상태에서도 밀림.
- 수정: 이동/물리 책임을 가진 EnemyFollow에서 bodyType을 Kinematic으로 설정하고 useFullKinematicContacts=true로 접촉 콜백 유지. EnemyContactDamage, BossController, 씬/프리팹/Collider/공격 수치는 변경하지 않음.
- 추가: Assets/Tests/EnemyPhysics에 독립 PlayMode 테스트 어셈블리와 접촉 데미지 수신용 테스트 컴포넌트. 기존 런타임 어셈블리 이동 없음.
- 재현/검증: Unity 6000.5.0f1의 별도 검증 프로젝트에서 실제 EnemyFollow/EnemyContactDamage/IContactDamageReceiver 소스 사용. 수정 전 10개 중 5개 실패(플레이어 Dynamic/Kinematic 각각 Follow 켜짐/꺼짐 밀림 4개, 재사용 후 외부 힘 1개), 수정 후 10/10 통과·생략 0. solid/trigger 접촉 데미지 및 쿨다운 4개, 추적과 정지 거리 1개, Follow 비활성 중 MovePosition 이동 및 재사용 후 외부 힘 저항 1개 포함.
- 초기 검증 6개 중 일부는 테스트 씬 비동기 정리 문제로 실패했으며 UnityTearDown으로 수정한 뒤 위 10개를 다시 실행함. 이를 게임 결함으로 집계하지 않음.
- 검증 경로: .utmp/EnemyPhysics/reproduced.xml, after.xml 및 해당 로그. 원본 씬/프리팹/클립/기존 메타/Project Settings 작업 전후 해시 동일. git diff --check 통과.
- 제한: 전체 프로젝트 컴파일/전체 회귀 테스트/실제 MainTestScene 보스 패턴 재생은 미실행. 테스트는 실제 BossController 전체 대신 Follow 비활성 중 Rigidbody2D.MovePosition 경로를 검증함.
- 물리 동작: Kinematic은 접촉 힘으로 밀리지 않으며 코드로 이동함. 적끼리 및 Kinematic/Static 상대와의 접촉은 콜백은 발생하지만 서로 물리적으로 밀어내거나 분리하지 않음. 실제 군집/벽 및 Kinematic 플레이어 겹침 여부는 씬 확인 필요.
- 직접 확인: Unity 컴파일 완료 → MainTestScene Play → 일반 적/보스의 몸통에 플레이어를 계속 이동. 적/보스가 플레이어 때문에 밀려나지 않고 추적 및 접촉 HP 감소가 유지되어야 함. 보스 공격 대기·공격 중에도 확인. 오류 시 Console 컴파일 오류, 실행 중 Enemy/Boss Rigidbody2D의 Kinematic 및 Full Kinematic Contacts, Collider 활성/Is Trigger, 플레이어 Rigidbody 유형을 확인.
- 다음 작업: 실제 씬에서 밀림 방지·접촉 데미지·공격·돌진·군집 동작 확인. 에디터에서 새로 Play해야 Awake 설정이 적용됨.

## 2026-11-03 밀림 방지 + 몸통 관통 방지 재수정

- 사용자 의도 확정: 플레이어가 Enemy/Boss를 밀 수 없으며 서로의 solid 몸통을 통과해서도 안 됨. 이전 Kinematic 단독 변경은 이 의도를 충족하지 못하므로 이번 수정으로 보완.
- 시작 상태: BossId/964ff51. 기존 코드·이미지 메타 변경 외에 사용자 MainTestScene.unity 및 AddressableAssetSettings.asset 변경이 존재했으며 저장/덮어쓰기하지 않음.
- 공통 CollisionMovement2D(Combat.Runtime)를 추가: 실제 활성 non-trigger Collider2D 형상을 이동 거리 전체에 Cast하여 가장 가까운 solid 앞에서 이동 제한. 접촉면 방향으로 잔여 이동을 다시 검사하여 벽면 이동/모서리 차단 지원. Trigger는 이동 차단에서 제외하고 기존 접촉 데미지 콜백 유지. Collider/레이어 충돌 설정과 IgnoreCollision을 반영. 매 액터 이동 위치를 즉시 반영해 다른 액터의 다음 검사가 갱신된 위치를 보도록 함.
- EnemyFollow: Kinematic+Full Kinematic Contacts 유지(외력에 밀리지 않음), 추적에 위 이동 검사 적용. 공개 MoveTo를 보스 패턴에서도 사용 가능하도록 추가.
- BossController: 돌진의 직접 MovePosition을 Follow.MoveTo로 교체. solid에 막혀 이동이 불가능하면 돌진 루프를 끝내어 장애물 뒤에서 무한 대기하지 않도록 처리. 공격 수치/이벤트/일반 패턴 규칙 변경 없음.
- PlayerMovement: 키보드 Tick과 DashRoutine의 직접 Transform 이동을 같은 검사 경로로 교체. Rigidbody 없는 기존 호출에만 기존 Transform 이동 fallback 유지.
- PlayerController2D: 조이스틱의 직접 속도 이동을 같은 검사 경로로 교체. PlayerMvpMovement도 직접 MovePosition을 같은 검사로 교체. 기존 입력/속도/대시 시간·MP 규칙 유지.
- 재현: 이전 코드에 몸통 차단 검증을 추가한 14개 테스트 중 5개 실패(적→플레이어 관통, 적끼리 관통, 대시 관통, Kinematic 플레이어 관통 2개). 이후 실제 프레임 진행을 검증하는 UnityTest와 벽면/Trigger 검사를 추가.
- 최종 검증: Unity 6000.5.0f1 별도 PlayMode 프로젝트에서 18/18 통과, 생략 0. 실제 EnemyFollow, EnemyContactDamage, CollisionMovement2D, PlayerMovement, PlayerData 소스 사용. Dynamic/Kinematic 플레이어의 밀림·차단, 실제 PlayerMovement 키보드/대시, 적끼리 차단, Follow 비활성 중 보스 MoveTo 차단, 추적 정지 거리, 재사용/외력 저항, solid/trigger 접촉 데미지와 쿨다운, 큰 Trigger 반경 무시, 벽면 이동 및 후퇴 검증.
- 결과: .utmp/EnemyPhysics/blocking-final.xml 및 blocking-final.log. 초기 접촉면 방향 보완 후, UnityTest 프레임 시간과 Cast 접촉 여유를 반영해 최종 재실행. 이전 10개 통과 기록을 재사용한 결과가 아님.
- 한계: 실제 MainTestScene 전투/보스 전체 패턴/조이스틱 입력/프로토타입 InputAction의 화면 재생은 미실행. 별도 프로젝트에서는 공통 이동과 실제 PlayerMovement 경로를 검증했고, 모바일/프로토타입/전체 BossController 시나리오까지 통과했다고 주장하지 않음. 원본 에디터에서 변경된 Runtime/Assembly-CSharp 어셈블리 생성 시간은 확인함.
- 직접 확인: Unity 컴파일 완료 후 Play 재시작 → MainTestScene에서 키보드와 조이스틱으로 적/보스 몸통을 계속 밀기 → 대시로 몸통을 넘으려 하기 → 보스 공격 대기 및 돌진 중 반복. 플레이어는 몸통 앞에서 멈추고 적/보스는 밀리지 않아야 함. 붙은 뒤 반대 방향 이동과 옆 이동은 가능하고 접촉 HP 감소/공격은 유지되는 것이 정상. 적끼리도 관통하지 않아야 함.
- 오류 확인: Console 컴파일 오류, 양쪽의 활성 solid Collider 존재(Is Trigger=false), 충돌 레이어 및 IgnoreCollision, 실행 중 Enemy Rigidbody Kinematic+Full Kinematic Contacts, 이동 스크립트가 최신인지 확인. Collider가 Trigger뿐인 대상은 의도적으로 통과 가능.
- 다음 작업: 사용자 실제 씬에서 모든 입력 경로·군집·벽·보스 공격/돌진 확인. 이번 작업에서 씬/프리팹/Project Settings/패키지를 수정하지 않음.

## 2026-11-03 보유 스킬 패시브 자동 발사 연결

- 시작 상태: feat/PlayerSkill, HEAD ccac76f. 이전 불꽃 탄환 약공격 관련 코드/씬/컨트롤러/리소스/테스트/문서 변경과 열린 테이블의 임시 잠금 파일을 보존. 커밋·브랜치 변경·테이블 수정 없음.
- 사용자 승인: 불꽃 탄환10001 패시브 피해6/간격3초/탄속3/사거리6. 빨간 원 프리팹 재사용. 약공격 MP10/피해10 유지.
- 완료: PlayerCombat의 기본탄 자동 발사를 보유 공격 스킬 순회로 교체. 장착 버튼과 무관하게 각 보유 스킬의 독립 주기로 발사. 빈 보유/미작성 패시브/잘못된 수치/누락 리소스는 발사하지 않음. 스킬 제거·교체 및 Player 비활성화에 실행 상태 반영. 첫 유효 표적에는 즉시 발사하며 지연 프레임의 누적 연사는 하지 않음.
- 조준: 사거리 안이면서 MainCamera 화면 안의 Enemy 태그 대상 중 가장 가까운 위치를 매 발사 시점에 선택. 일반 적과 BossMvp의 Enemy 태그 확인. 이미 발사한 탄환은 기존 Bullet.InitDirection의 직선 비행 유지. 기존 풀을 사용하며 새 매니저/로딩 체계는 추가하지 않음.
- 설정: Data.SkillData에 PassiveConfigured/PassivePrefabKey/PassiveDamage/PassiveInterval/PassiveSpeed/PassiveRange 추가. Player.Passive Projectiles 직접 바인딩 또는 기존 ResourceManager의 준비된 캐시를 사용. MainTestScene에는10001의 승인 설정과 FlameBullet 바인딩만 추가(이 작업의 씬 추가9줄). 기존 기본 공격 수치와 Player.bulletPrefab 참조는 보존하되 기본탄 중복 발사는 제거. bulletPrefab을 비워도 이제 패시브를 끄지 않음.
- 시간/입력: MP 무소모. 일시정지·방어 모션 중 남은 패시브 주기 정지. 약공격 입력 잠금 중에는 기존대로 패시브 허용. 비활성화 후 재활성화는 첫 유효 표적부터 새 주기로 시작.
- 재현/검증: 새13개 테스트가 기존 구현에서13개 실패(빈 보유 상태 기본탄 생성/패시브 정의 부재). 구현 후 관련85/85 통과. 실제 Player 바인딩/빨간 프리팹 물리 충돌 피해6/비활성화/방어와 약공격 잠금/씬 설정 테스트 추가 후 Unity6000.5.0f1 별도 검증 프로젝트에서 최종89/89 통과, 실패0·생략0. 패시브17, 기존 풀6/약공격13/MP행동19/HUD12/상태창9/팝업13. 이번 실행 결과이며 이전73개 기록 재사용 아님.
- 검증 파일: .utmp/PassiveSkill/red.xml, final.xml, final.log. 변경 코드·테스트·MainTestScene은 테스트 복사본과 해시 동일. 전체0.Scripts의 차이는 검증 복사본 PlayFromTitle 배치 guard뿐. git diff --check 통과. 기존 테스트의 CS0618 경고가 남아 있으며 이번 패시브 코드/새 테스트에는 구 FindObjectsSortMode 호출 없음. 독립 코드 리뷰에서 중요한 결함 없음, 지적된 통합 검증 공백을 위 테스트로 보완.
- 한계: 사용자 요청으로 생략한 병합 전체 선행 검증은 수행하지 않음. 실제 원본 MainTestScene 전투/보스 전체 패턴·전체 카탈로그·Android 빌드/기기 미확인. 다른 스킬의 수치·효과·획득/성장·강공격·전략·미정 그로기는 확정하지 않음.
- 직접 확인: Unity 컴파일 완료 → Tools > Play From Title 끄기 → MainTestScene Play → 버튼 없이 화면 안의 적에게 사거리6까지 접근. 빨간 원이 첫 발사 후3초마다 직진하며 MP가 줄지 않는지 확인. 적이 움직여도 기존 탄환은 꺾이지 않고 다음 탄환이 가까운 적을 다시 조준해야 함. N_Attack 약공격은 MP10/피해10·입력 잠금을 유지. 패링/가드 및 일시정지 중 주기가 멈추고 복귀 후 이어지는지 확인. PC 이동은 PlayerController2D 비활성화.
- 오류 확인: Console 첫 오류, Player.Passive Projectiles의 Key=FlameBullet/Prefab=FlameBullet, PlayerData.Loadout의10001 보유와 PassiveConfigured·수치·키 일치, Enemy 태그/활성 Collider/MainCamera 태그·화면·거리를 확인. 패시브만 잠시 끄려면 Play 중10001의 PassiveConfigured를 해제하고 임시 설정은 저장하지 않음.
- 문서: docs/AI/PlayerPassiveSkills.md 및 docs/superpowers/plans/2026-11-03-owned-passive-skills.md. 다음은 실제 플레이 피드백 반영 및 정식 스킬 테이블/리소스·획득 경로 연결, 이후 강공격 차징 구현. 해당 단계의 미작성 수치는 별도 사용자 결정이 필요함.

## 2026-11-03 보스 HP 바 좌우 입력 버그 수정

- 시작 상태: feat/PlayerSkill/ccac76f. 앞선 약공격·패시브 관련 코드/씬/리소스/테스트/문서 및 열린 테이블 잠금 파일을 보존.
- 원인: MainTestScene의 Boss_HP Slider(1137732343553467738)가 Interactable=true, Navigation=Automatic으로 설정되어 있음. 보스는 PlayerView가 아닌 공용 EnemyHealth의 healthSlider 경로를 사용하여 앞선 플레이어 HUD 표시 전용 처리가 적용되지 않았음. 실제 HP와 무관하게 선택된 Slider가 좌우/포인터 입력으로 편집됨.
- 재현: 실제 EnemyHealth+Slider+EventSystem 테스트에서 HP50 상태의 왼쪽 이동만으로 표시 비율0.5→0.4, 숨김 후 표시된 바도0.7→0.6, 포인터/드래그로0.5→1이 됨. 수정 전 새7개 중6개 실패·HP바 없는 적1개 통과.
- 수정: EnemyHealth 초기화/Start/피격/풀 재등장 시 HP 바를 표시 전용으로 설정(Interactable=false, Navigation=None, Transition=None). 비활성 색상 전환을 제거하여 색상 유지. 체력 비율은 Slider의 min/max 범위로 매핑하고 SetValueWithoutNotify로 표시하여 조작 이벤트를 발생시키지 않음. 체력/피해/보스 패턴·스폰 수치 변경 없음.
- 검증: Unity6000.5.0f1 별도 검증 프로젝트에서55/55 통과, 실패0·생략0. EnemyHealthHudTests7 + PlayerHudTests12 + PlayerPassiveSkillTests17 + PlayerBulletPoolingTests6 + PlayerLightSkillTests13. 좌우/포인터 차단, 실제 피격·사용자 지정 범위·이벤트 무발생, 풀 재등장, 숨김/표시, UI 없는 적 검증. 결과 .utmp/BossHealthHud/red.xml 및 final.xml/final.log. 새 결과이며 앞선89개 기록 재사용 아님.
- 보존 확인: MainTestScene/BossMvp.prefab/EnemyMvp.prefab 작업 전후 해시 동일. 변경 소스·테스트는 검증 복사본과 해시 동일. git diff --check 통과. 씬·프리팹·Project Settings 변경 없음. 기존 다른 테스트의 CS0618 경고는 별개로 남아 있음.
- 직접 확인: Unity 컴파일 완료 → Tools > Play From Title 끄기 → MainTestScene Play 재시작 → 보스 출현 후 Boss_HP를 클릭/드래그한 다음 왼쪽·오른쪽 이동. 공격하지 않을 때 바가 입력만으로 줄거나 늘지 않고 실제 보스 피격에만 줄어야 함. 일시정지/복귀 후에도 반복. PC 이동 확인은 PlayerController2D 비활성화.
- 오류 확인: Console 첫 컴파일 오류, 실행 중 보스 EnemyHealth.healthSlider가 Boss_HP Slider를 참조하는지, Boss_HP의 Interactable=false/Navigation=None인지 확인. 기존 Play를 종료하고 재시작하여 새 초기화 적용. 소스만으로 표시 전용을 보장하므로 씬에서 수동 참조 재연결이나 HP값 조정은 필요 없음.
- 한계/다음: 실제 원본 MainTestScene 보스 전투 화면과 Android 기기는 미확인. 위 사용자 재현 절차 확인 후 기존 패시브 피드백/정식 데이터·획득 연결 및 강공격 후속으로 복귀.## 2026-11-05 몬스터 기획서 PPT 이어쓰기

- 시작 상태: `Zomso_Monster_기획서.pptx` 8장. 1.1 기본 좀비만 수치가 있고, 1.2·1.3은 `0 -` 자리표시자, 1.4 이후와 엘리트·보스는 없었다. 4장과 8장은 내용 없는 빈 템플릿이었다.
- 완료: 총 15장. 1.2 여성 좀비·1.3 사무 좀비_남의 수치 칸을 미정으로 바꾸고 가운데 상자 제목 "몬스터 이름"을 실제 이름으로 바꿨다. 1.4 사무 좀비_여, 1.5 연구 좀비_남, 1.6 연구 좀비_여, 1.7 공사장 좀비, 1.8 간호사 좀비를 추가했다(카드 그림은 3장 목록의 그림 사용, 특징 문구는 3장 목록과 같음). 2 엘리트, 2.1 엘리트 접두사, 3 보스, 3.1 보스 등장 순서를 추가했다. 목차와 개정 표(26.11.05)를 갱신했다. 사용한 빈 템플릿 2장은 삭제했다.
- 수치 기준: 사용자 결정에 따라 기본 좀비 외 몬스터 수치는 미정으로 두었다. 슬라이드에 적은 근거는 3장 목록의 특징 문구(공사장=방어형·높은 방어/넉백 저항, 연구_남=자폭형, 연구_여=원거리, 간호사=지원형), 시스템 기획서의 엘리트·보스 규칙(1.5×1.5 타일, 3분 주기 6마리, 타이머 정지), `EnemyDatatable.xlsx`의 엘리트 접두사 2개(강인한 체력 1.1, 빠른 이동속도 1.5)다. 접두사 값은 데이터 표 기재값이며 적용 방식은 미정이라고 적었다.
- 넣지 않음: 엑셀의 여성 좀비(체력 2·공격력 8·이동속도 0.1), 사무 좀비_남 체력 8, 야수 좀비 보스(체력 300·공격력 15·방어력 10). 기획서 문구 "모든 수치가 표준치"와 충돌하거나 확정 여부가 불분명해서다. 보스 순서·이름, 히든 보스, 엘리트 주기·종류도 미정.
- 불일치(고치지 않음): 1.2 카드 부제 "기본형 / 대량 생성"은 3장 목록의 "기본형 / 군집 추적"과 다르다. 1.3 카드 부제 "기본형 / 돌진 가속"은 3장의 "균형형 / 돌발 가속"과 다르다.
- 검증: 파워포인트로 15장 모두 PNG로 내보내 눈으로 확인했다. 원본은 작업 전 `C:\tmp\mon\src.pptx`에 복사해 두었다. Unity 테스트 0개. 씬, 프리팹, 코드는 변경하지 않음.
- 다음 확인: 파워포인트에서 파일을 열어(열려 있던 창은 닫고 다시 연다) 2장 목차, 5~11장 몬스터 카드, 12~15장 엘리트·보스를 본다. 불일치 두 곳과 미정 항목의 확정이 다음 작업이다.

## 2026-11-05 적 기획서 새로 작성

- 원인: `Zomso_Enemy_기획서.docx`는 처음부터 시스템 기획서 본문을 복사한 파일이었다. 파일이 깨진 것이 아니라 내용이 잘못된 문서였다. 이전 내용은 git 이력에 남아 있다.
- 완료: 패링 기획서와 같은 양식(제목, 개정 표 Date/Author/Description, INDEX, 제목 번호, 머리글·바닥글, 표 모양)으로 `적 시스템` 문서를 새로 썼다. 개정 표에는 26.11.05 행 하나만 있다. 1 문서 개요, 2 적 분류와 공통 규칙, 3 일반 몬스터, 4 엘리트 몬스터, 5 보스 몬스터, 6 등장(스폰) 규칙, 7 데이터 표, 8 구현 현황과 기획 차이, 9 미정 사항, 10 테스트 케이스(TC-01~12). 표 16개와 QA 표. 14쪽.
- 근거: 시스템 기획서, `Zomso_Monster_기획서.pptx`, `EnemyDatatable.xlsx`, 10월 20일 팀회의록, `Assets/0.Scripts/Game/Enemy/**`, `Game/Encounter/BossEncounterController.cs`, `Game/Spawning/SceneEnemyPool.cs`. 확정된 값은 기본 좀비 HP 3·공격력 10, 크기 비율, 거리 구간, 동시 130마리 스폰 제한, 보스 3·6·9·12·15·18분과 타이머 정지, 체력바 규칙뿐이다. 나머지 몬스터 수치, 엘리트 주기·보상, 히든 보스 조건, 스테이지와 18분 타임라인의 관계, 적 종족(인간·외계인 대 좀비), 바닥 경고, 그로기는 미정으로 적었다.
- 충돌한 자료(어느 쪽도 고르지 않음): 몬스터 PPT와 엑셀의 여성 좀비(HP·공격력·속도), 사무 좀비 남 HP 8과 특징 설명, PPT 목록의 TYPE 글자 순서, 보스 엑셀의 크기 1·이동 속도 0(자리표시), 보스 공격력 15와 구현 할퀴기 피해 10.
- 코드와 기획 차이(8장 표): 보스 시작 때 일반 몬스터를 모두 거두는 구현과 보스 사망 때 정리한다는 기획, 보스 등장 기본 10초 1회와 3분 6회, 돌진 발동 거리 10(기획 뷰포트 가로 절반 8타일보다 멀다), 접촉 피해 간격 0.5초, 엘리트·그로기·붉은 안광·바닥 경고·잡기는 코드에 없음. 코드는 바꾸지 않았다.
- 검증: 저장 파일을 워드로 다시 열어 제목 번호(1~10, 하위 번호), INDEX, 표 16개, 쪽수 14를 확인했고 PDF로 내보내 페이지를 눈으로 봤다. Unity 테스트 0개. 씬, 프리팹, 코드, 엑셀은 변경하지 않음.
- 남은 일: 패링 기획서 TC-02의 `스태미나 차감` ST 구표기는 이번에 고치지 않았다. 노션은 `기획서 작성` 페이지의 개정 이력 댓글 스레드에 2026.11.05 세부 기획서 답글을 달았다. `세부 기획서 작성`·`점수 구현 기획` 페이지 본문은 고치지 않았다.
- 다음 확인: 워드에서 `Zomso_Enemy_기획서.docx`를 연다. 제목이 `적 시스템`이고, 개정 표에 26.11.05 구동현 한 줄, 2쪽 INDEX가 1~10장, 4쪽부터 본문이 보여야 한다. 시스템 기획서 내용이 보이면 예전 창이므로 닫고(저장 안 함) 파일을 다시 연다. 목차 쪽수가 어긋나 보이면 INDEX를 우클릭해 필드 업데이트 → 전체 업데이트를 한다.

## 2026-11-04 기획서 정합

- 완료: 보스전 중에는 인게임 타이머가 멈춘다는 기준으로 점수식을 적었다. 플레이 타임은 그 타이머이고, 보스전 시간은 넣지 않는다. 18분에 6번째 보스가 나오면 타이머는 1080초에서 멈추므로, 보스전을 오래 해도 클리어 보너스는 음수가 되지 않는다. 18분까지 진행한 클리어의 시간 보너스는 0이다.
- 완료: 시스템 기획서의 끊긴 문장, 오타, 거리 경계, 사망 시 부활 순서, 히든 보스·퀘스트·바닥 경고·골드·스킬 장착·몬스터 개별 수치의 미정 표기, 엘리트 보상 후보 표기. (적 기획서에 시스템 기획서 본문이 복사돼 있었다는 것은 이후 확인됐고, 2026-11-05에 새로 작성했다. 아래 새 항목 참고.) 스킬 기획서는 자동 탄환 대상, 강공격 차징 시간(표의 0.45~0.95초), 전략 쿨타임 6초, 차징 배율 미정, 동작 우선순위를 맞췄다. 빌드 기획서의 융합 예시는 태양 검 10007 = 화염 강타 10003 + 플레어 탄환 10006, 제목과 최대 MP, Uncommon. 패링 기획서는 가드 경감, 10프레임 탭, 2초 가드 해제. 스킬 개요 11장의 0.8/1/1.2 배율은 미정으로 바꿨다. 몬스터 문서에서 여성 좀비는 TYPE C, 사무 좀비 남은 TYPE E. 강공격·전략 표의 헤더 Cooldawn은 Cooldown.
- 넣지 않음: 플레이어 HP/MP, 차징 배율, 히든 보스 조건, 퀘스트 보상, 골드 가격, 20001~20005 차징 시간, 기본 좀비 외 몬스터 수치. 융합 표의 수식은 원래 위 칸을 가리키고 있어 값을 덮어쓰지 않았다.
- 검증: 저장 파일에서 해당 문장을 다시 읽었다. 2026-11-05에 시스템 기획서가 워드에서 열리지 않아, 히든 보스 문장의 닫는 태그가 하나 더 있던 것을 지우고 워드로 다시 열었다. Unity 테스트 0개. 씬, 프리팹, 코드는 변경하지 않음.
- 이력: 2026-11-05 개정 표는 목차 번호와 제목 참조로 다시 썼다. 예: 1.2.1 최종 점수 산정 방식. 제목을 누르면 해당 목차로 이동한다.
- 다음 확인: 워드에서 시스템 기획서의 개정 표 마지막 내용 행이 26.11.05인지, 점수식 다음 문장과 빌드 기획서의 태양 검 10007을 본다. 예전에 열어 둔 창이면 닫고 파일을 다시 연다.

## 2026-11-05 도트 연출 기획서 문체 정리

- 완료: 프로젝트 루트 `Zomso_도트연출_기획서.docx`의 본문을 자연스러운 서술형 문장으로 전부 다시 썼다. 변경 이력 표(작성자·설명), `가칭` 삭제, `들개` 삭제는 사용자가 직접 고친 것이라 그대로 두고, 들개를 문서 어디에도 쓰지 않도록 맞췄다. 장 제목은 2 현재 구현 기준, 3.1 지금 제작 방식의 한계, 3.2 코드로 도트를 만드는 방식, 3.3 참고한 게임 화면, 4.1 도트 제작 기준, 4.2 종류별 제작 기준, 4.3 제작 순서, 5 현재 프로젝트 적용 범위, 6 미정 사항, 7 첫 제작 대상, 8 확인 항목으로 바꿨다. 작업 확인용 문구(파일 위치, 양식 설명)는 본문에서 빼고 8장을 도트 제작 확인 항목으로 바꿨다. 내용상 결정(포켓몬 시트·코드 미사용, 3D 맵 미도입, 수치·색·프레임 수·바닥 경고·상태 표시 위치 미정)은 그대로다.
- 검증: 저장 파일의 전체 문단과 표 3개를 읽었다. 워드 쪽수 8, 바닥글 `페이지 1 / 8`, 목차는 새 제목으로 갱신. Unity 테스트 0개. 씬, 프리팹, 코드, 스프라이트, 엑셀은 변경하지 않음.
- 다음 확인: 워드에서 파일을 열어 읽히는 흐름을 본다. 이전 창이 열려 있으면 닫고 루트의 파일을 다시 연다. 상태 표시 위치, 약·강공격 이펙트를 별도 스프라이트로 둘지, 캔버스 크기는 계속 미정이다.

## 2026-11-04 스킬 개요 PPT

- 완료: `docs/스킬_개요.pptx` 12장. 레어도 테두리 7장 뒤에, 약공격 21·강공격 15는 해당 등급 테두리 안에 문양을 넣은 모습, 전략 10개는 등급이 없어 문양만 넣었다. 이 합성 그림은 PPT에만 있고 프로젝트 아이콘 파일로 추가하지 않았다.
- 검증: 슬라이드 7장과 표 헤더를 읽었다. PowerPoint에서 화면 재생은 확인하지 않음.

## 2026-11-04 스킬 아이콘

- 시작 상태: 스킬 표의 아이콘 경로는 비어 있었다. 상태창은 `SkillData.IconLabel`이 있고 미리 로드된 주소가 있을 때만 그림을 그린다.
- 완료: 스킬별 아이콘 46장을 다시 넣었다. `Assets/2.Resources/Art/SkillIcons/SkillIcon_10001.png`~`SkillIcon_30010.png`. 32×32, 투명 배경, 동그란 배지와 레어도 테두리는 그리지 않았다. 레어도 테두리는 등급당 1장, 총 7장이다. `SkillFrame_{Common,Uncommon,Rare,Epic,Legendary,Mythic,Fusion}.png`. 64×64, 속은 투명, Point 필터. `Skill_Rarity`의 Rarity_Frame_Res와 Rarity_Color_Hex에 경로와 색이 있다. 약공격·강공격·전략 표의 Skill_Icon은 `SkillIcons/SkillIcon_{아이디}`다.
- 검증: 46장 연락표를 눈으로 확인했다. 표의 아이콘 경로 46칸과 약공격 ID 수식을 다시 읽었다. Unity 임포트와 상태창 표시는 확인하지 않음. Addressables 그룹은 비어 있어 아이콘 주소를 넣지 않았다.
- 다음 확인: Unity가 임포트를 끝내면 Project의 `Art/SkillIcons`에서 `SkillIcon_10001` 불꽃 탄환과 테두리 7장이 같이 보이는지 본다. 상태창에 그림이 없고 이름만 보이면 아이콘 주소가 아직 사전 로딩되지 않은 것이다. 엑셀 Skill_Icon이 비어 있으면 파일을 다시 연다.

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

## 2026-11-05 Beast Boss 애니메이션 연결 조사

- 요청은 연결되지 않은 애니메이션 확인이며 런타임/클립/컨트롤러/씬은 수정하지 않았다. 실제 상태 BossRevise / 5d34dad. 시작 때 수정된 AllIn1SpriteShaderLit.shader·LitTransparent.shader를 보존했다.
- BossMvp의 Animator는 Beast_Zombie_Boss/Animator/Boss.controller를 참조한다. idle·move·swipe·swipe-left·body-charge 클립은 컨트롤러에 참조가 있다. 할퀴기 16상태는 BossController의 Animator.Play 직접 호출로 진입하며 화살표 부재만으로 미연결로 판단하지 않는다. 돌진은 body-charge → body-charge-loop 전환과 코드의 BossIdle 복귀가 있다.
- 미사용 원본 이미지: Idle/animations/run 64장, sprint 64장, swipe-both 64장은 Beast Boss 아래 .anim에서 GUID 참조 0개. 각 동작 전용 상태도 현재 Boss.controller에 없다. BossIdle.anim은 컨트롤러 참조가 없는 옛 단일 이미지 클립이며 실제 BossIdle 상태는 방향별 BlendTree를 사용한다.
- swipe 일부 방향은 Animator/FrameSources의 대체 이미지를 사용하므로 원본 이미지 참조 0을 공격 연결 누락으로 해석하지 않는다. idle 일부 원본 프레임도 대체 GUID를 사용하며 이번에는 화면 품질을 판정하지 않았다.
- 검증은 에셋 텍스트·GUID·코드 호출 대조만 수행. 실행 테스트 0개, Unity 재생/컴파일 미실행. 다음은 사용자가 run/sprint/swipe-both를 어떤 패턴에 사용할지 정한 뒤 필요한 연결 작업. 자동으로 패턴을 추가하지 않았다.
- 직접 확인: Project → Beast_Zombie_Boss/Animator/Boss.controller → Animator에서 BossIdle/BossWalk의 BlendTree, 할퀴기 16상태와 body-charge 두 상태 확인. Idle/animations의 run/sprint/swipe-both는 이미지가 있어도 현재 전용 재생 상태가 없는 것이 조사 결과다. 플레이 검증 시 Tools > Play From Title 해제 → MainTestScene에서 보스 추적/할퀴기/돌진 확인. 이상 시 Console 첫 오류, BossMvp 및 씬 인스턴스 Animator Controller와 Overrides 확인.

## 2026-11-05 Beast Boss 미사용 동작 클립·상태 추가

- 사용자 요청에 따라 run/sprint/swipe-both의 8방향 원본을 사용하는 .anim 24개와 meta를 추가했다. 기존 BossWalk의 12fps 형식을 기준으로 8프레임, 길이 약 0.667초. run/sprint는 반복, swipe-both는 비반복이다. 기존 클립·meta GUID는 유지했다.
- Boss.controller에 BossRun/BossSprint/BossSwipeBoth와 FacingX/FacingY 방향 BlendTree 3개를 추가했다. BossSwipeBoth는 종료 시 기존 BossIdle로 복귀한다. 기존 기본 상태·파라미터·전이·런타임 패턴/피해/이동 속도는 변경하지 않았다. 새 상태는 재생 가능하도록 등록한 단계이며 기존 BossController가 자동 호출하지 않는다. 양손 공격 피해 이벤트는 임의로 추가하지 않았다.
- 정적 검증: 새 24클립의 컨트롤러 참조, 원본 192프레임 GUID/fileID 일치, 각 클립 9키(끝 유지 키 포함), 컨트롤러 YAML 객체 ID 중복 없음 확인. Unity 컴파일/임포트/실행 테스트 0개. 씬·BossMvp 프리팹 사용자 변경 및 셰이더 사용자 변경을 보존했다.
- 직접 확인: Unity 임포트 완료 → Console 첫 오류 확인 → Project에서 Beast_Zombie_Boss/Animator/Boss.controller 열기 → BossRun/BossSprint/BossSwipeBoth 더블클릭 → Inspector BlendTree Preview에서 FacingX/FacingY를 (1,0), (0,1), (-1,0), (0,-1) 및 대각선으로 바꿔 방향별 클립 확인. Run/Sprint는 반복, SwipeBoth는 비반복이어야 한다. 새 동작은 기존 플레이 패턴에서 자동 발동하지 않는다. Missing이면 Console 첫 오류와 클립 Sprite 참조 확인. 다음 작업은 각 동작의 실제 사용 패턴을 정한 뒤 코드 진입·이탈/공격 판정을 연결하는 것이다.

## 2026-11-05 Idle West/NorthWest Missing Sprite 修正

- 사용자 지적대로 BossIdle_West의 0·1프레임과 BossIdle_NorthWest의 2프레임은 프로젝트에 존재하지 않는 GUID를 참조했다. 이전 조사에서 대체 이미지 참조로 추정한 설명을 정정한다. PNG는 존재하며 클립 연결이 깨진 상태였다.
- 해당 3개 참조를 현재 idle/west 및 idle/north-west PNG meta의 GUID와 실제 Sprite fileID로 교체했다. 커브와 pptrCurveMapping 양쪽을 수정했다. 이미지·meta·클립 GUID·재생 타이밍·씬/프리팹 사용자 변경은 보존했다.
- 정적 검증: 두 클립 8/8 프레임 키와 바인딩 매핑이 원본 Sprite GUID/fileID와 일치. Unity 실행 테스트 0개, 임포트·화면 미검증.
- 직접 확인: Unity 임포트 완료 → 보스 선택 → Window > Animation > Animation → BossIdle_West와 BossIdle_NorthWest 선택 → 4개 Sprite 키에 Missing이 없고 Preview 재생 중 사라지는 프레임이 없어야 한다. 남으면 Console 첫 오류, 해당 클립 Reimport, 현재 선택 객체의 Animator Controller를 확인한다.

## 2026-11-05 West/NorthWest Sprite 미생성 원인 수정 및 실제 에디터 확인

- 사용자 재보고로 재조사. 앞선 GUID 수정만으로는 불충분했다. 실제 PNG 8장(west/north-west 각 4장)은 96×95인데 meta의 Sprite rect가 96×96이었다. 열려 있는 Unity에서 강제 임포트 시 rect가 이미지 밖이라 Sprite가 생성되지 않는 경고와 LoadAllAssetsAtPath Sprite 부재를 재현했다.
- 해당 8개 meta의 Sprite rect height만 96 → 95로 수정했다. 이미지·GUID·Sprite internalID는 유지했다. 임시 Editor 도구로 원본을 강제 재임포트하고 실제 Sprite 객체를 두 Idle 클립의 키에 할당해 해당 클립만 저장했다. 도구와 자동 생성 meta는 검증 후 제거했다. 씬·프리팹·셰이더 사용자 변경은 보존했다.
- 실제 검증: 실행 중 Unity 6000.5.0f1에서 임시 도구 컴파일/실행 완료, 두 클립 8/8 키가 null이 아닌 imported Sprite임을 AnimationUtility.GetObjectReferenceCurve로 확인. 정적 PNG 경계 검사 8/8 통과. 실행 PlayMode 테스트 0개, 화면 재생 미검증. 결과 .utmp/BossIdleWestRepair/result.txt는 생성물이며 영속 근거는 이 기록과 meta/클립 변경이다.
- 직접 확인: Project에서 BossIdle_West 선택 → Animation 창에서 Preview → 4개 키에 Sprite가 있고 보스가 사라지는 구간이 없어야 한다. BossIdle_NorthWest도 동일. 안 보이면 Console 첫 오류와 Preview 대상 SpriteRenderer/Animator를 확인한다. 이전 Sprite 영역 오류 로그는 수정 전 재현 기록이다.

## 2026-11-05 그림자 크기·발밑 위치 고정

- 사용자 요청에 따라 GroundShadow의 크기/발밑 위치 계산을 최초 유효 Sprite 배치 때 한 번만 수행하도록 변경했다. 이후 애니메이션 Sprite 변경 때 재계산하지 않고 부모 자식 Transform으로 이동을 따라간다. 최초 크기는 기존 WidthRatio/HeightRatio/FootOverlap 규칙을 유지하므로 캐릭터별 시작 크기를 보존한다. 같은 몸에 Attach 재호출·풀 재활성화에도 고정을 유지한다. 몸 Renderer가 달라지면 다시 초기화한다. 캐릭터 자체 Transform 스케일 변경은 그림자에도 적용된다.
- 회귀 소스: GroundShadowTests.AttackSpriteDoesNotResizeOrMoveShadow 추가. 공격용 큰 Sprite 교체 후 크기·발밑 오프셋 유지, 캐릭터 이동 추종, Attach 재호출 후 크기 유지를 확인한다.
- 실제 검증: 열려 있는 Unity 6000.5.0f1의 임시 Editor 검증 도구로 수정 전 FAIL(attack resized shadow) 재현 → 수정 후 PASS(크기/오프셋 유지·이동 추종·재연결 유지). 원본 프로젝트 C# 재컴파일 완료 후 도구 실행. NUnit 러너 실행은 0개이며 위 결과는 Editor 회귀 검증 1건이다. 임시 도구와 meta는 제거했다. 기존 패키지 HDRP-Editor-ref.asmref 참조 경고는 남아 있다. 결과 .utmp/GroundShadowFixed/result.txt는 생성물이다.
- 사용자 씬·프리팹·애니메이션·meta·셰이더 변경은 보존했다. 이번에는 GroundShadow 런타임·회귀 테스트·이 문서만 영속 변경. 실제 보스 애니메이션 화면/PlayMode/기기 검증은 미실행.
- 직접 확인: 컴파일 완료 후 기존 Play를 종료하고 다시 시작 → Tools > Play From Title 해제 → MainTestScene에서 보스 추적·할퀴기·돌진 확인. 그림자는 처음 크기를 유지하며 발밑을 따라가고 공격 때 넓어지지 않아야 한다. 플레이어/일반 적도 동일. 이상 시 Console 첫 오류, Ground Shadow 중복 여부, 부모 Transform 스케일, 최초 스프라이트 상태를 확인한다. 시작 크기 자체의 미세 조정은 별도다.

## 2026-11-05 보스 공격 범위 VFX 피격 반짝임 제외

- 사용자 승인에 따라 Combat.Runtime에 HitFlashExcluded 표식 컴포넌트를 추가하고 BossAttackHitVfx가 생성하는 할퀴기/돌진 SpriteRenderer 오브젝트에 붙였다. EnemyHealth는 색 수집 때 기존 GroundShadow와 이 표식을 제외한다. 따라서 Flash/RestoreColors/재스폰 색 복원은 몸 렌더러만 처리하고 VFX의 Range Color/알파를 덮어쓰지 않는다. 기존 어셈블리 참조 방향과 씬·프리팹은 변경하지 않았다.
- 회귀 소스 HitFlashExclusionTests 추가: 몸의 흰색 반짝임·원래 색 복원 유지, 제외 VFX의 RGBA가 반짝임·복원·재스폰 동안 유지되는지 확인한다.
- 수정 전 열린 에디터의 임시 검증 도구에서 FAIL: VFX color overwritten by flash를 재현했다. 중단 후 사용자가 검증은 직접 하겠다고 했으므로 수정 후 Unity 실행·테스트 검증은 수행하지 않았다. 임시 Editor 검증 도구 및 meta는 제거했고 영속 회귀 테스트 소스는 유지했다. 코드 diff 공백 검사만 수행했다.
- 직접 확인: 기존 Play 종료 → 컴파일 완료 → Tools > Play From Title 해제 → MainTestScene Play → 보스 할퀴기 중 플레이어 공격으로 보스를 피격. 몸만 잠시 흰색이고 부채꼴은 설정한 Range Color와 투명도를 유지해야 한다. 돌진 충돌 원도 유지되는지 확인한다. 다음 공격과 재스폰 후에도 확인. 이상 시 Console 첫 오류, 생성된 Boss Slash/Dash Hit VFX의 HitFlashExcluded 컴포넌트, Range Material 연결 확인. Range Color Inspector 값 자체 변경이나 Glow 과다 노출은 이번 수정과 별개다.

## 2026-11-05 Beast Boss 2페이즈 진입 로그

- 사용자 요청의 Boss 페이지를 현재 Beast Boss의 페이즈로 해석해 BossController.CurrentPhase(초기 1)를 추가했다. 생존 중 현재 HP가 전체 최대 HP의 50% 이하이면 2로 전환하고 요청 문구 `2페이지 돌입!`을 한 번 출력한다. 공격 패턴·속도·애니메이션·피해·UI는 변경하지 않았다.
- EnemyHealth에 읽기 전용 MaxHealth를 노출해 진입 시점의 남은 HP가 아니라 실제 최대 HP를 기준으로 판정한다. SpawnGeneration 변경 시 1페이즈로 초기화하므로 풀 재스폰에 대응하며 단순 disable/enable만으로 로그를 반복하지 않는다. 사망 타격으로 즉사한 경우에는 페이즈 로그를 출력하지 않는다. HordeLordBoss의 기존 별도 페이즈 로직은 변경하지 않았다.
- 검증: diff 공백 검사만 수행. 사용자 요청대로 Unity 실행/컴파일/자동 테스트는 수행하지 않았으며 새 테스트 결과 0개. 기존 변경 모두 보존.
- 직접 확인: 컴파일 완료 → Tools > Play From Title 해제 → MainTestScene Play → Beast Boss 체력을 줄인다. 최대 HP의 50%를 초과할 때 로그가 없어야 하며 정확히 절반 또는 그 이하가 되는 첫 프레임에 Console `2페이지 돌입!` 한 번. 추가 피격에도 반복되지 않아야 한다. 실제 풀 재스폰 시 다시 1페이즈부터 시작. 오류 시 Console 첫 오류, BossController 활성/EnemyHealth MaxHealth와 CurrentHealth 확인. 2페이즈 패턴은 아직 추가하지 않았다.

## 2026-11-05 2페이즈 패턴 1·2 사이 원형 공격 추가

- 최종 사용자 정정: 패턴 1 대체가 아니라 패턴 1 → 조건부 추가 원형 공격 → 패턴 2(오른손/왼손) → 패턴 1이다. 보스 중심·반경 8·애니메이션 없음·기본 대기 0.8초(Inspector 변경 가능), 기존 돌진 종료 반경 3은 유지하도록 승인받았다.
- BossController에 phaseTwoAreaDistance(4,14), phaseTwoAreaRadius(8), phaseTwoAreaDelay(0.8)를 추가했다. 패턴 1 종료 후 기존 pattern1ToPattern2Delay를 기다린 다음 선택 시점에 거리를 한 번 확인한다. 2페이즈이고 거리 >4 && <=14이면 BossPattern.PhaseTwoArea를 1회 삽입한다. 조건 불충족이면 건너뛰고 기존 일반/돌진 거리 선택으로 돌아간다. 추가 공격 자체는 nextNormalPattern을 바꾸지 않으므로 이후 근거리에서 패턴 2를 이어간다. 원거리 돌진도 기존 일반 패턴 순서를 보존한다.
- 원형 공격 동안 EnemyFollow를 잠시 중지하고 게임 시간 0.8초 후 현재 Rigidbody2D 보스 위치에서 판정한다. 애니메이션·애니메이션 이벤트를 새로 호출하지 않는다. 기존 ShowDashImpact를 반경 8로 호출해 타격 시점에만 원을 표시한다. 피해 수신자별 1회 적용을 기존 돌진과 공통 ApplyCircleDamage로 분리했다. 피해량은 현재 damage(사용자가 추가한 2페이즈 배수 포함)를 사용한다. 일시정지 시 대기 시간도 멈춘다. 사망/비활성화/타깃 소멸 시 늦은 피해를 막고 추적 복원은 기존 생존 가드를 따른다.
- 선택 거리 4~14와 실제 피해 반경 8은 서로 다르므로, 선택 시점에 8~14에 있던 플레이어는 공격 시 반경 밖이면 피해를 받지 않는다. 이는 승인 수치를 그대로 적용한 결과다. 기존 BossPatternRangeView/Gizmo는 기본 패턴 디버그 표시를 유지하며 새 원형 공격의 사전 경고는 추가하지 않았다.
- 사용자 변경인 Multiple2·damage/follow.moveSpeed 배수·기존 Inspector 수치·씬/프리팹/애니메이션 변경은 보존했다. 이번 변경은 BossController/BossPattern/인수인계뿐이다. 사용자 요청대로 Unity 컴파일/플레이/자동 테스트는 실행하지 않았다. diff 공백 검사 통과, 새 실행 테스트 0개.
- 직접 확인: Play 종료 → 컴파일 완료 → Tools > Play From Title 해제 → MainTestScene Play → Boss HP 50% 이하로 2페이즈 진입 → 패턴 1 한 번 할퀴기 후 다음 선택 시점에 플레이어 거리를 4 초과~14로 유지. 보스가 멈춰 0.8초 후 보스 중심에 반경 8 원 표시/피해, 근거리 복귀 후 패턴 2의 두 번 할퀴기, 다음은 패턴 1이어야 한다. Inspector의 2페이즈 추가 원형 공격/Phase Two Area Delay로 시간 변경 확인. 1페이즈·거리4이하·14초과에는 추가 공격이 없어야 하고 기존 돌진 반경은 그대로여야 한다. 이상 시 Console 첫 오류, CurrentPhase/다음 패턴 시점의 거리, BossController의 새 거리·반경·대기값, Range Material과 BossAttackHitVfx 연결을 확인한다.

## 2026-11-05 BossPatternRangeView 2페이즈 범위 연계·Scene 조절

- 사용자 요청에 따라 BossPatternRangeView에 추가 원형 공격 전용 Mesh/Renderer와 최소·최대 발동 거리 선을 연결했다. PhaseTwoArea의 대기 구간 동안 보스 중심에 실제 PhaseTwoAreaRadius의 원을 표시한다. 반경 변경 시 메시를 갱신한다. 거리 선은 2페이즈부터 표시하며 추가 공격 중 이전 할퀴기 잔류 범위는 숨긴다. showInGameRanges와 컴포넌트 비활성화 시 표시를 숨기고 새 메시도 파괴 시 정리한다.
- BossController의 원형 반경·발동 거리 읽기 전용 프로퍼티를 추가했다. 값의 소유권은 BossController에 유지하고 씬·프리팹은 자동 저장하지 않았다.
- Scene 비재생 미리보기: BossPatternRangeView의 Show Scene Area Range가 켜져 있고 보스를 선택하면 Gizmos로 피해 반경 주황색, 발동 최소/최대 거리 청록색 원을 표시한다. 새 Assets/Editor/BossPatternRangeViewEditor는 Inspector는 기본 표시를 유지하고 OnSceneGUI의 슬라이더 핸들로 보스 오른쪽(반경), 왼쪽(최소 거리), 위쪽(최대 거리)을 조절한다. SerializedObject/Property로 BossController 실제 설정을 수정해 Undo/Redo 및 프리팹 Overrides를 지원한다. EditMode용 임시 GameObject/Material/Mesh 생성은 하지 않는다.
- 검증은 diff 공백 검사만 통과. 사용자 요청대로 Unity 실행/컴파일/자동 테스트 미실행, 실행 테스트 0개. UI 핸들·Undo·실제 게임 화면 확인은 사용자에게 남긴다. 기존 사용자 변경 모두 보존.
- 직접 확인: Play 종료·컴파일 완료 → MainTestScene의 BossMvp 선택(아직 없으면 BossMvp 프리팹 모드) → Scene 뷰 Gizmos 켜기 → BossPatternRangeView의 Show Scene Area Range 켜기 → 주황/청록 원과 3개 라벨 핸들 확인. 오른쪽 주황 핸들을 드래그하면 BossController > 2페이즈 추가 원형 공격 > Phase Two Area Radius와 원 크기가 같이 바뀌어야 한다. 왼쪽/위쪽 청록 핸들로 거리 X/Y 변경 후 Ctrl+Z 복원 확인. 대기 시간은 BossController의 Phase Two Area Delay에서 변경한다. Play 중에는 HP 절반 이하 → 패턴 1 이후 거리 조건 충족 시 추가 원형 공격 대기 구간의 원 표시를 확인한다. Play 중 값은 종료 시 돌아가므로 영속 조절은 EditMode에서 저장한다. 안 보이면 Console 첫 오류, Gizmos/컴포넌트 토글/선택 여부, Show In Game Ranges, RangeView의 부착 여부를 확인한다.

## 2026-11-05 2페이즈 원형 공격을 독립 조건부 특수패턴으로 변경

- 사용자 정정에 따라 패턴 1·2 사이 삽입 예약(areaAttackPending)을 제거했다. SelectAvailablePattern에서 거리 기반 특수패턴으로 직접 선택한다. 우선순위는 돌진 거리 조건 → 2페이즈 원형 거리 조건(기본 >4 && <=14) → 근거리 일반 패턴 → 추적이다. Inspector에서 거리 조건이 겹치면 기존 돌진을 우선한다.
- 원형 특수패턴은 nextNormalPattern을 변경하지 않아 기존 패턴 1/2 교대 순서를 보존한다. 진행 중인 공격은 중단하지 않고 기존 nextPatternTime 대기가 끝난 다음 선택한다. 특수패턴 종료 후 별도 쿨타임을 임의로 추가하지 않았으므로 거리 조건이 계속 맞으면 원형 공격을 연속 선택할 수 있다(매번 기존 0.8초 대기). 반경8·보스중심·애니메이션 없음·Inspector 대기 조정 및 BossPatternRangeView/Scene 핸들 연계는 유지했다.
- 변경 파일은 BossController와 이 기록이다. 씬/프리팹 등 기존 변경을 보존했다. diff 공백 검사 통과. 사용자 요청대로 Unity 실행·컴파일·자동 테스트 미실행, 새 실행 테스트 0개.
- 직접 확인: Play 재시작 → MainTestScene → 보스 2페이즈 → 거리 4 초과~14에서 패턴 1/2 차례와 무관하게 원형 특수패턴 선택 확인. 보스 중심에서 설정 대기 후 반경8 피해/범위 표시가 있어야 한다. 근거리 복귀 시 보류 중이던 일반 패턴을 이어가고 원거리에서는 기존 돌진이 나와야 한다. 거리 유지 시 연속 원형 공격은 현재 의도된 조건 선택 결과다. 안 나오면 Console 첫 오류, CurrentPhase, Inspector의 Phase Two Area Distance와 Special Pattern Distance 중복, 기존 일반 패턴 대기시간을 확인한다.

## 2026-11-05 원형 특수패턴 후 일반 패턴 강제 진행·내부 쿨타임

- 사용자 요청에 따라 원형 공격 종료 후 nextNormalPattern을 패턴1로 지정하고 areaFollowupPending 동안 추적/근거리 패턴1→기존 간격→패턴2를 먼저 완료하도록 변경했다. 이 구간에는 돌진·원형 특수패턴이 끼어들지 않는다. 패턴2 종료 후 기존 패턴2→1 간격을 거쳐 특수패턴 거리 선택을 다시 허용한다. 기존 돌진 종료 처리 자체는 변경하지 않았다.
- 사용자 지정 내부 쿨타임 3초를 phaseTwoAreaCooldown(Inspector 변경 가능)으로 추가했다. 원형 공격 종료부터 Time.time으로 계산한다. 일반 패턴1·2 완료와 쿨타임 만료를 모두 만족해야 원형 공격을 재사용한다. 일시정지 중에는 쿨타임도 멈춘다. SpawnGeneration 변경 때 재사용 시간과 후속 일반 패턴 가드를 초기화한다.
- 반경8/대기0.8/거리 조건/Scene 핸들·RangeView는 그대로 유지한다. 씬·프리팹·사용자 배수 변경은 보존했다. 이번 변경은 BossController/인수인계만이다. diff 공백 검사 통과. 사용자 요청대로 Unity 실행·컴파일·자동 테스트 미실행, 실행 테스트0개.
- 직접 확인: Play 재시작 → MainTestScene → 2페이즈·원형 공격 거리 조건 충족 → 원형 공격 후 보스가 추적하여 근거리 패턴1 한 번·패턴2 두 번을 수행하고 나서야 특수패턴을 다시 선택하는지 확인한다. Inspector > 2페이즈 원형 특수패턴 > Phase Two Area Cooldown 기본3초, 늘려 재사용 제한 확인. 거리를 계속 벌리면 근거리 일반 패턴 완료까지 추적하며 특수패턴이 반복되지 않아야 한다. 오류 시 Console 첫 오류, 가까운 거리 도달/장애물 여부, 패턴 간격/쿨타임, 새 필드에 0 등의 씬 인스턴스 값이 저장되어 있는지 확인한다.

## 2026-11-05 원형 특수패턴 표시 수명 정리

- 사용자 최종 지시는 잠깐 타격 표시가 아니라 패턴 시작 때 범위를 보여주고 패턴 종료 때 즉시 숨기는 것이다. BossPatternRangeView의 areaRenderer.enabled = ActivePattern == PhaseTwoArea를 유지해 대기 시작부터 피해 적용/종료까지 보스 중심의 실제 원형 반경을 표시한다.
- PerformPhaseTwoAreaAttack에서 ShowDashImpact 호출을 제거해 종료 후 0.2초 추가로 남는 원형 VFX를 없앴다. 기존 돌진 타격 VFX는 그대로 유지한다. 새로 추가했던 게임 중 발동 최소/최대 거리 선은 제거해 원형 특수패턴에는 실제 피해 원만 표시한다. Scene 뷰 Gizmos/핸들의 최소·최대 거리 및 반경 편집은 유지한다. 기존 일반 근접/돌진 디버그 거리 선은 변경하지 않았다.
- 쿨타임3초·피해반경8·대기0.8·원형 후 일반1/2 진행은 유지. 사용자 씬/프리팹 등 보존. diff 공백 검사만 수행했고 사용자 요청대로 Unity 실행/컴파일/자동 테스트 미실행(실행 테스트0개).
- 직접 확인: Play 재시작 → MainTestScene → 2페이즈 원형 특수패턴 발동. 패턴 시작과 동시에 보스 중심 원이 나타나고 0.8초 후 피해 적용/종료 시 사라져야 한다. 종료 뒤 발광 원이 더 남지 않아야 한다. 표시가 없으면 BossPatternRangeView 활성과 Show In Game Ranges, Console 첫 오류를 확인한다. Scene의 선택 Gizmos는 편집용이며 게임 표시 수명과 별개다.

## 2026-11-05 원형 특수패턴 인게임 표시를 BossAttackHitVfx로 이전

- 사용자 요청에 따라 원형 특수패턴의 인게임 표시 책임을 BossPatternRangeView의 디버그 MeshRenderer에서 BossAttackHitVfx의 전용 Boss Area Hit VFX SpriteRenderer로 옮겼다. 기존 CreateCircleSprite/CreateRenderer/Place를 재사용해 Range Material/Range Color/Glow/Radial 360도 설정을 할퀴기와 통일했다. 전용 렌더러도 HitFlashExcluded 표식을 받으며 같은 Sprite/Texture를 공유한다.
- BossAttackHitVfx.ShowArea/HideArea를 추가했다. BossController가 원형 공격 시작 때 ShowArea를 호출하고 finally에서 HideArea를 호출한다. Hide/OnDisable에도 포함해 중단·사망·비활성화 시 숨긴다. 반경은 공격 시작 때 한 번 저장해 표시와 실제 타격 판정에 같은 값을 사용한다. 보스 자식으로 움직임을 따라간다. 종료 후 지연 페이드나 타격 잔류 원은 추가하지 않았다.
- BossPatternRangeView에서 원형 공격의 인게임 메시 생성/표시/정리를 제거해 중복 표시를 없앴다. 기존 할퀴기·돌진 디버그 표시 및 원형 공격의 Scene Gizmos/편집 핸들은 유지했다. Show In Game Ranges가 꺼져 있어도 새 원형 VFX는 표시된다. 반경/거리/대기/쿨타임은 기존처럼 BossController에서 설정한다. 기존 머티리얼/씬/프리팹/meta를 수정하지 않았다.
- diff 공백 검사 통과. 사용자 요청대로 Unity 실행·컴파일·자동 테스트는 수행하지 않았다(실행 테스트0개). 기존 사용자 변경 모두 보존했다.
- 직접 확인: Play 종료 → 컴파일 완료 → MainTestScene Play → 2페이즈 원형 특수패턴 발동. BossAttackHitVfx의 Range Color로 보스 중심 원이 시작부터 0.8초 동안 표시되고 피해 적용/종료 시 즉시 사라져야 한다. Show In Game Ranges를 꺼도 동일해야 한다. 범위 중 보스를 타격해도 색이 흰색으로 덮이지 않아야 한다. 원형 후 일반1·2 및 쿨타임3초는 유지. 안 보이면 Console 첫 오류, BossAttackHitVfx 활성/Range Material/Range Color 알파, 생성된 Boss Area Hit VFX 렌더러와 카메라 레이어를 확인한다. Scene Gizmos/핸들의 편집 기능은 별도로 확인한다.

## 2026-11-05 2페이즈 원형 후 돌진 차단 수정

- 사용자 재보고 원인: SelectAvailablePattern의 areaFollowupPending 분기가 돌진 거리 조건보다 앞에 있어 원형 공격 후 일반1/2를 마칠 때까지 원거리에서도 None을 반환했다. 조건 순서로 원인을 확인했으며 Unity 실행 재현은 사용자 요청에 따라 수행하지 않았다.
- 돌진 거리 검사를 최우선으로 이동했다. 원형 후 일반 패턴 진행 중에도 원거리면 돌진하여 접근할 수 있고, 돌진은 nextNormalPattern/areaFollowupPending을 변경하지 않아 가까워진 뒤 남은 일반1/2를 이어간다. 원형 재사용은 기존 일반1/2 완료·내부 쿨타임3초 조건을 유지한다. 이전 기록의 돌진도 후속 일반 패턴에 끼어들지 않는다는 동작은 이번 수정으로 변경됐다.
- 회귀 소스 BossAreaFollowupTests 추가: 일반1/2 각각 대기 중 원거리 돌진 허용·일반 순서 보존·중거리 추가 원형 차단·근거리 일반 패턴 선택 확인. 테스트 실행0개, 컴파일/플레이 미실행. diff 공백 검사만 수행했다. 기존 씬/프리팹/사용자 변경 보존.
- 직접 확인: Play 재시작 → MainTestScene → 2페이즈에서 원형 공격 사용 후 플레이어를 Special Pattern Distance 이상으로 이동. 돌진 시작 로그와 접근이 나와야 하고 가까워지면 일반1/2를 이어가야 한다. 원형은 연속 발동하지 않아야 한다. 안 되면 Console 첫 오류, 기존 패턴 종료/대기 여부, 플레이어와 보스 실제 중심 거리 및 Inspector Special Pattern Distance, 이동 장애물을 확인한다.

## 2026-11-05 BossPatternRangeView를 Scene 전용으로 정리

- 사용자 요청에 따라 BossPatternRangeView의 모든 인게임 표시 코드/설정(showInGameRanges, 선 두께·색·알파·지속시간, Awake/LateUpdate, GameObject·Mesh·Material·Renderer 생성·갱신·해제)을 삭제했다. Scene의 선택 Gizmos와 기존 범위 편집 핸들에 필요한 showSceneAreaRange/프로퍼티만 유지했다.
- Gizmos 그리기는 UNITY_EDITOR 내부에서 SceneView 카메라일 때만 허용해 Game 뷰의 Gizmos를 켜도 이 스크립트 표시가 나오지 않도록 했다. 컴포넌트를 끄면 Scene Gizmos/편집 핸들도 숨긴다. 실제 할퀴기·돌진·원형 공격 표시는 기존 BossAttackHitVfx가 그대로 담당한다.
- 씬/프리팹과 GUID/meta는 자동 저장·수정하지 않았다. 기존 YAML에 남은 삭제 필드 값은 Unity가 다음 정상 저장 시 정리하며 더 이상 코드에서 사용하지 않는다. 기존 사용자 변경 보존. diff 공백 검사 통과 및 인게임 생성/갱신 키워드 잔존 없음 확인. 사용자 요청대로 Unity 실행/컴파일/자동 테스트 미실행(실행 테스트0개).
- 직접 확인: Play 종료·컴파일 완료 → MainTestScene 보스 선택 → Scene Gizmos/Show Scene Area Range 켜기 → 주황 반경/청록 발동 거리 원·편집 핸들 확인. Play 재시작 후 Boss Pattern Range Debug 자식이 생성되지 않고 Game 뷰에 이 스크립트의 상시 거리 선·붉은 디버그 면이 없어야 한다. 실제 공격 때 BAHV 표시는 유지되어야 한다. 오류 시 Console 첫 오류, Scene 선택/토글 여부, BAHV 활성/Range Material/알파 확인. BossController의 별도 기존 편집용 Gizmos는 이번 요청 범위에서 변경하지 않았다.

## 2026-11-05 원형 특수패턴 Sprint 테스트 애니메이션·이벤트 피해

- 사용자 요청에 따라 기존 BossSprint 방향 BlendTree를 원형 공격 테스트 모션으로 연결했다. 기존 원형 준비 시간 phaseTwoAreaDelay(기본0.8초) 후 Base Layer.BossSprint를 한 번 재생한다. 범위는 준비 시작부터 모션 종료까지 BAHV로 표시한다. 방향은 Sprint 재생 중 고정한다. 일반1/2 후속·원형 쿨타임3초·피해반경8·2페이즈 배수는 유지한다.
- BossSprint_8방향 .anim을 비반복으로 바꾸고 마지막 원본 프레임 시각0.5833333초에 AnimationEvent_ApplyAreaDamage, 클립 끝0.6666667초에 AnimationEvent_AreaAnimationFinished를 추가했다. 피해 시각은 테스트 초기값이며 Animation 창 이벤트로 조절 가능하다. 기존 0.8초는 준비시간으로 남아 피해는 기본 총 약1.383초 이후 발생한다. Animator 속도 변경 시 이벤트 실제 시간도 달라진다.
- 타이머 직후 직접 피해 경로를 제거했다. 원형 공격 실행 중/생존/활성/현재 패턴 가드와 areaHitApplied로 이벤트 중복 피해를 막는다. 이벤트가 없거나 모션이 중단되면 임의로 피해를 보충하지 않는다. 종료 이벤트·상태 이탈·normalizedTime>=1에 따라 종료하고 StopAreaAnimation에서 Idle 복귀, finally에서 BAHV 숨김/추적 복원한다. 사망/OnDisable에서도 모션을 정리한다. Sprint는 제자리 재생이며 실제 돌진 이동은 추가하지 않았다.
- BossController와 Sprint 클립8개 및 인수인계만 변경. .anim meta GUID/Animator Controller/씬/프리팹 사용자 변경 보존. 정적 검사8/8: 각 클립 피해 이벤트1개·종료 이벤트1개·비반복 설정 확인, 코드 공백 검사 통과. 사용자 요청대로 Unity 컴파일·재생·테스트 실행 미수행(실행 테스트0개).
- 직접 확인: Play 종료 → 임포트/컴파일 완료 → MainTestScene → 2페이즈 원형 공격. 범위 표시/0.8초 준비 → Sprint 단발 → 마지막 프레임에서 반경8 내 피해1회 → 종료 시 Idle/범위 숨김/일반1·2 진행 확인. Animation 창 BossSprint_각방향의 이벤트 마커에서 피해 시각을 옮길 수 있다. 한 방향만 바꾸면 방향별 시각이 달라지므로 필요 시8방향 동일하게 조절한다. 오류 시 Console 첫 오류/AnimationEvent 수신 메시지, Animator BossSprint 상태와 Controller 연결, 각 클립 이벤트 이름/Loop Time, 플레이어가 실제 반경 내에 있는지 확인한다.

## 2026-11-05 Sprint 테스트 연결 제거

- 사용자 요청으로 직전 Sprint 시험 연결만 제거했다. 원형 패턴은 다시 BAHV 범위 표시 → phaseTwoAreaDelay(기본0.8초) 대기 → 보스 중심 피해1회 → 범위 숨김/추적 복원으로 동작한다. 원형 전용 애니메이션 상태 검사·재생·종료·피해 이벤트 메서드와 임시 플래그를 제거하고 기존 방향 갱신을 복원했다.
- BossSprint 8클립의 시험 피해/종료 이벤트를 제거하고 기존 반복 설정으로 복구했다. Sprint 클립/컨트롤러 상태 자체는 유지했다. 범위8·쿨타임3·후속 일반1/2·원거리 돌진 허용·Scene 전용 RangeView는 유지했다.
- diff 공백 검사 수행. 사용자 요청대로 Unity 실행/컴파일/자동 테스트 미실행(실행 테스트0개). 씬/프리팹/meta 및 기존 사용자 변경 보존.
- 직접 확인: Play 종료·컴파일 완료 → MainTestScene → 2페이즈 원형 공격. Sprint 없이 범위가 나타나고 기본0.8초 후 피해/범위 종료여야 한다. 문제 시 Console 첫 오류, Phase Two Area Delay와 BAHV 연결 확인.

## 2026-11-05 원형 패턴을 swipe-both 애니메이션으로 시험 연결

- 사용자 요청으로 원형 패턴에 Base Layer.BossSwipeBoth(기존 8방향 BlendTree)를 연결했다. 기존 phaseTwoAreaDelay 기본0.8초 준비 후 양손 할퀴기를 한 번 재생하고, BAHV 원형 범위는 준비 시작부터 모션 종료까지 유지한다. Sprint는 연결하지 않았다.
- 기존 PlaySlashAnimation에 선택적 useArea 경로를 추가해 일반 할퀴기의 상태 종료/이탈 감지와 추적 중지/복원을 재사용한다. 원형에서는 부채꼴 VFX를 켜지 않고 외부 원형 표시를 유지한다. 공격 중 방향 BlendTree 입력 갱신은 멈춰 방향을 고정한다. 일반 공격은 기존 방식이다.
- BossSwipeBoth_8방향 비반복 클립에 AnimationEvent_ApplyAreaDamage(마지막 원본 프레임0.5833333초)와 AnimationEvent_AttackAnimationFinished(클립 종료0.6666667초)를 추가했다. 피해는 이벤트에서만 발생하고 활성/현재패턴/중복가드로 한 번만 적용한다. 반경은 패턴 시작 때 저장한 값 사용. 기본 준비시간 포함 피해까지 약1.383초(Animator 속도1 기준)이며 이벤트 마커로 시각 조정 가능하다. 일반 할퀴기 이벤트는 원형 패턴에서 피해를 적용하지 않는다.
- 쿨타임3초·원형 후 일반1/2·원거리 돌진 우선·Scene 편집 기능은 유지. 씬/프리팹/meta/AnimatorController 사용자 변경은 보존. 정적 이벤트8클립 연결 및 diff 공백 검사 수행. 사용자 요청대로 Unity 컴파일·재생·테스트 실행 미수행(실행 테스트0개).
- 직접 확인: Play 재시작 → MainTestScene → 2페이즈 원형 발동 → 0.8초 준비 후 swipe-both 단발/범위 유지 → 마지막 원본 프레임 피해1회 → 종료 후 범위 숨김/Idle/일반1·2 진행. Animation 창 BossSwipeBoth_각방향의 ApplyAreaDamage 이벤트를 옮겨 타격 시점 조정 가능. 오류 시 Console 첫 오류/이벤트 수신 오류, Animator BossSwipeBoth 상태·8클립 이벤트·비반복 설정, 실제 반경8 안인지 확인한다.

## 2026-11-05 원형 피해 순간 빨간 ParryFlash 확산 테스트

- 사용자 요청으로 Assets/Resources/PlayerDefenseVfx/ParryFlash_03.png를 Assets/0.Scripts/Game/Enemy/Boss/Test/ParryFlash_03_Red.png로 복제했다. 원본 픽셀/알파는 변경하지 않고 복제본 전체 이미지를 Single Sprite/Point로 임포트하여 BAHV의 빨간 tint로 표시한다. PNG 자체를 다시 그리지 않았으며 Project 미리보기는 원본색이다. 신규 GUID 사용, 원본 meta/GUID 보존.
- BossAttackHitVfx에 areaBurstSprite/areaBurstColor/areaBurstDuration(기본0.2초), 전용 Boss Area Burst Test VFX 렌더러와 ShowAreaBurst를 추가했다. 보스 프리팹의 기존 BAHV에 복제본 참조와 빨강/지속시간만 최소 추가했다. 피해 이벤트 AnimationEvent_ApplyAreaDamage에서 실제 피해 반경과 같은 activeAreaRadius로 재생한다. 처음 지름10%에서 cubic ease-out으로 목표 지름(반경*2)까지 확장하며 알파를0으로 줄이고 숨긴다. 이미지 전체 Sprite bounds 기준 크기이며 원본 PNG의 투명 여백은 유지된다. 대기용 범위와 별도라 패턴 정상 종료 뒤에도 짧은 확산을 마치며, 전체 Hide/비활성화 때는 정리한다.
- 신규 효과는 Unlit Sprite 머티리얼을 사용해 기본색 빨강을 유지한다(URP2D Unlit 우선, Sprites/Default 대체). 몸보다 위 sortingOrder, HitFlashExcluded, 기존 Sprite/Texture 재사용. 생성한 전용 Material은 파괴 시 해제한다. Time.deltaTime이므로 일시정지와 함께 정지한다. 피해/쿨타임/기존 Sprite 애니메이션은 변경하지 않았다.
- 정적 diff 공백 검사 수행. Unity 임포트/컴파일/화면·테스트 실행은 사용자 요청대로 수행하지 않았다(실행 테스트0개). 사용자 기존 프리팹 변경은 유지하면서 새 필드만 추가, 씬 저장 없음. 실제 프로젝트의 원본·복제본 PNG bytes 동일함을 복제 방식으로 보장한다.
- 직접 확인: Play 종료 → 임포트/컴파일 완료 → BossMvp의 BAHV > 테스트용 원형 타격 확산에서 Area Burst Sprite/Color(빨강)/Duration0.2 확인 → MainTestScene → 2페이즈 원형 공격. swipe-both 피해 순간 보스 중심에서 작은 빨간 링이 빠르게 반경8 크기까지 퍼졌다 사라져야 한다. Duration으로 속도 조절 가능. 원본 패링 그림은 변경되지 않아야 한다. 안 보이면 Console 첫 오류, Boss 인스턴스 BAHV 새 Sprite 참조(씬 Overrides 포함), Color 알파/머티리얼 Shader 및 카메라 레이어 확인. Project 이미지 색은 tint 전 원본색임에 유의한다.

## 2026-11-05 2페이즈 패턴2 중간 대기 제거

- 사용자 정정: 패턴2의 첫 할퀴기 → 두 번째 할퀴기 사이 딜레이를 2페이즈에서 없앤다. 첫 PlaySlashAnimation 종료 후 CurrentPhase>=2이면 doubleSlashInterval 대기를 건너뛰고 왼손 두 번째 할퀴기를 바로 시작하도록 조건만 변경했다. 1페이즈의 기존 Inspector 간격과 패턴1/2 사이 대기시간은 유지했다. 애니메이션 자체 재생시간/후딜 프레임은 잘라내지 않는다.
- 변경은 BossController 한 조건과 이 기록뿐이며 기존 사용자 변경 보존. diff 공백 검사 통과. Unity 컴파일/재생/자동 테스트는 요청대로 미실행(실행 테스트0개).
- 직접 확인: Play 재시작 → MainTestScene → 1페이즈 패턴2는 기존 두 타 사이 간격 → HP50% 이하 2페이즈 패턴2는 첫 모션 종료 직후 두 번째 모션 시작 확인. 차이가 없으면 Console 첫 오류, 2페이즈 진입 로그, doubleSlashInterval과 애니메이션 자체 종료 이벤트/후딜 프레임을 확인한다.

## 2026-11-05 2페이즈 패턴2 두 번째 할퀴기 반경1.5배

- 사용자 지정대로 2페이즈 패턴2의 왼손(두 번째) 할퀴기에만 Slash Range의1.5배를 적용했다. 공격 시작 때 activeSlashRange에 범위를 저장하고 BAHV 표시·OverlapCircleAll 후보 조회·중심 거리 필터에 같은 값을 사용한다. Inspector slashRange 자체는 수정하지 않으며 다음 공격 시작 때 다시 기본값으로 초기화한다. 각도·피해·첫 타/1페이즈/원형 공격은 유지했다.
- 변경은 BossController와 인수인계뿐이며 기존 사용자 변경 보존. diff 공백 검사 통과. Unity 실행/컴파일/자동 테스트는 요청대로 미실행(실행 테스트0개).
- 직접 확인: Play 재시작 → MainTestScene → 2페이즈 패턴2. 첫 할퀴기는 원래 반경, 종료 직후 두 번째만1.5배 표시/피해 판정이어야 한다(기본값6이면9). 다음 패턴1은 원래 반경으로 돌아와야 한다. 1페이즈 패턴2는 양쪽 같은 기본 범위. 이상 시 Console 첫 오류, CurrentPhase/패턴2 여부와 Slash Range/BAHV 색·머티리얼을 확인한다. Scene의 기본 할퀴기 Gizmo는 기본 slashRange 표시를 유지한다.

## 2026-11-05 2페이즈 진입 직후 원형 패턴1회

- 사용자 요청에 따라 HP50% 이하 최초 2페이즈 진입 때 현재 패턴 코루틴을 중단하고 이전 돌진/할퀴기 플래그·BAHV 표시를 정리한 뒤 원형 패턴을 즉시 선택한다. phaseEntryAreaPending은 일반 패턴 대기/거리/원형 쿨타임/후속 일반 가드보다 우선해 한 번 소비한다. 타깃 없음/비활성·전투 일시정지 때는 대기하며 정상 조건 복귀 후 한 번 실행한다. SpawnGeneration 변경 시 진입 예약을 초기화한다.
- 즉시 발동은 패턴 시작을 뜻하며 기존0.8초 준비·swipe-both·피해 애니메이션 이벤트는 그대로다. 완료 뒤 일반1/2·쿨타임3초가 그대로 적용되고 이후 원형은 기존 거리 조건으로 선택한다. 사용자가 추가한 2페이즈 배수와 pattern2ToPattern1Delay=1.5 변경을 보존했다.
- BossController/인수인계만 수정. diff 공백 검사 통과. 사용자 요청대로 Unity 컴파일/플레이/자동 테스트 미실행(실행 테스트0개). 씬/프리팹/기존 사용자 변경 보존.
- 직접 확인: Play 재시작 → MainTestScene → 보스 HP를 절반 이하로 낮춘다. 2페이지 돌입 로그와 함께 현재 공격/대기를 중단하고 원형 범위가 표시되어야 한다. 거리 조건 밖에서도 진입1회는 발동하되 실제 반경 밖이면 피해는 없어야 한다. 기존 준비 후 swipe-both 타격 이벤트/빨간 확산 → 일반1/2 확인. 추가 피격마다 강제 원형이 반복되지 않아야 한다. 오류 시 Console 첫 오류, 체력/CurrentPhase, 타깃·일시정지 상태, BAHV/Animator 참조 확인.
