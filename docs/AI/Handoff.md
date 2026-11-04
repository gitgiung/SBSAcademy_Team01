# 작업 인수인계

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
