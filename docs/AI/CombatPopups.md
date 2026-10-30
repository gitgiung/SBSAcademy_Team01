# GiungTestScene 일시정지·레벨업 팝업

## 직접 확인

1. Unity 컴파일이 끝나면 Console의 새 빨간 오류가 없는지 확인한다. `Tools → Play From Title`이 켜져 있다면 직접 씬 확인 중 잠시 끈다.
2. `Assets/9.Scenes/GiungTestScene.unity`를 열고 Play를 누른다. 새 `Combat UI` 프리팹이 있고 화면 오른쪽 위에 **일시정지** 버튼이 보여야 한다. 기존 스포너 수치와 보스 등장 시간은 그대로다.
3. 일시정지를 누른다. 배경을 가리는 **일시정지** 팝업과 **뒤로가기** 버튼이 나타난다. 플레이어 이동·새 공격·몬스터 이동·스폰·진행 시간이 멈춰야 한다. 뒤로가기를 누르면 팝업이 닫히고 전투가 재개된다. 여러 번 반복해도 창과 정지 상태가 누적되지 않아야 한다.
4. 레벨업 수동 확인: Play 중 Hierarchy에서 **Combat UI**를 선택한다. Inspector의 **Combat Popup Controller** 컴포넌트 메뉴(⋮ 또는 제목 우클릭)에서 **테스트 → 레벨업 팝업 열기**를 누른다. **레벨 업** 제목과 빈 콘텐츠 영역이 보이고 전투가 멈춰야 한다. 같은 메뉴의 **테스트 → 레벨업 선택 완료**를 누르면 닫히고 재개된다. 현재 실제 레벨 시스템·선택지·효과는 구현하지 않았다.
5. 겹침 확인: 레벨업을 연 뒤 컴포넌트 메뉴의 **테스트 → 일시정지 팝업 열기**를 누른다. 뒤로가기를 누르면 레벨업 창이 남고 전투는 계속 정지한다. **레벨업 선택 완료**를 누른 뒤에 재개돼야 한다. 게임 화면의 테스트 버튼이나 가짜 보상은 추가하지 않았다.
6. 보스전에서도 일시정지·뒤로가기를 확인한다. 팝업을 닫으면 보스와의 전투는 재개되지만, 보스전 동안 진행 타이머와 일반 몬스터 스폰은 기존대로 정지해 있어야 한다.
7. 종료 후 Play From Title 설정을 원래대로 돌린다. 자동 확인은 `Window → General → Test Runner → PlayMode`의 `CombatPopupTests`, `CombatPopupSceneTests`를 선택하고 **Run Selected**: 신규 20개, 이후 **Run All**: 총 113개 통과가 정상이다.

오류가 있으면 Console **첫 빨간 오류와 상세 스택**, 실패 테스트 이름을 확인한다. UI가 없으면 씬의 Combat UI 프리팹과 Controller의 Pause Button·Pause Popup·Level Up Popup·Game Manager 참조를 확인한다. 실행 후 두 팝업은 기존 UIManager의 `@UI_Root` 아래로 이동하는 것이 정상이다. 한글이 네모로 나오면 `Assets/UI/Fonts`의 TTF·TMP SDF·meta가 함께 있는지 확인한다.

## 향후 레벨업 이벤트 연결

씬에 연결된 `CombatPopupController` 참조를 레벨 시스템의 연결 컴포넌트에 전달한다. 이벤트의 구독은 해당 컴포넌트 OnEnable, 해제는 OnDisable에서 한다. 새 전역 이벤트 버스는 필요 없다.

- 레벨업 발생: `ShowLevelUp()` 호출. 반환된 팝업과 `LevelUpContentRoot`에 실제 성장 선택 UI를 제공한다.
- 실제 선택과 효과 적용 완료: `CompleteLevelUp()` 호출. 자동으로 보상을 적용하거나 레벨을 올리지 않는다.
- 팝업이 이미 열려 있으면 같은 인스턴스를 앞으로 올린다. 여러 레벨업의 대기 선택 수·큐 정책은 미래의 레벨 시스템이 소유한다.
- 최상단만 닫는 기존 UIManager 계약을 따른다. 일시정지 창이 위에 있으면 먼저 뒤로가기로 닫은 후 레벨업 선택을 완료한다. 아래 창에 대한 완료 요청은 닫기나 정지 해제를 일으키지 않는다.
- 레벨업 팝업에는 성장 선택을 건너뛰는 버튼을 넣지 않았다. 현재 닫기는 외부 완료 API 또는 Editor 수동 완료 메뉴로 확인한다.

## 구조와 정리

- `CombatUI.prefab`: 직렬화된 HUD·두 팝업·버튼·빈 콘텐츠 영역. 기존 EventSystem/InputSystem UI를 사용한다.
- `UIManager.RegisterPopupUI(name, popup)`: 씬의 기존 인스턴스를 등록한다. 같은 등록은 유지하고 이름 충돌·별칭 중복은 거절한다. Clear의 소유 정리에 포함된다. `CacheAllPopups`는 ResourceManager에 로드된 프리팹 타입만 준비하므로 씬 전용 팝업에 Addressables 등록은 필요 없다.
- `UI_CombatPopup`: 기존 UI_Popup을 사용한다. 활성화·비활성화로 정지 소유권을 전달하고 뒤로가기와 포커스를 연결한다.
- `CombatPopupController`: 씬 로컬 컴포넌트다. 첫 팝업 전 시간 배율을 저장하고 모든 팝업을 닫은 뒤 복원한다. 이미 0이었던 시간은 0으로 남고 GameManager가 승리·패배 상태면 재개하지 않는다. OnDisable은 열린 창을 숨기고 OnDestroy는 소유 UI를 파괴한다. 새 싱글톤은 없다.
- 실제 GiungTestScene의 `Player.cs`는 Update에서 timeScale 0일 때 입력·자동공격을 처리하지 않는다. 진행 중 패링·가드 루프도 정지 중 상태를 넘기지 않는다. `PlayerMeleeAttack` MVP 구현은 이 씬의 실제 연결이 아니므로 변경하지 않았다.
- 씬 수정은 새 프리팹 인스턴스와 기존 GameManager 참조만 추가했다. 기존 씬 데이터·GUID·보스·스폰 수치·Project Settings·패키지는 보존했다.

## 글꼴과 검증 범위

한글은 [Google Fonts의 NanumGothic 원본](https://github.com/google/fonts/tree/main/ofl/nanumgothic)을 수정 없이 포함하고 `Assets/UI/Fonts/OFL.txt` 라이선스를 함께 보존했다. TMP SDF는 이 원본을 참조하는 dynamic atlas다.

UI 프리팹을 별도 카메라로 1920×1080 렌더해 제목·뒤로가기·빈 레벨업 영역의 배치를 확인했다. 배치 Game View 화면 캡처는 시간 초과로 완료되지 않았으며 실제 게임 전체 화면의 시각 확인 완료로 보고하지 않는다. 실제 씬 버튼·Raycast·UI 포커스·시간/스폰/공격·보스전·씬 해제는 PlayMode에서 별도로 검증했다. Android 빌드·터치 실기기·노치 safe area·미래 레벨 시스템은 미검증이다.

2026-10-30 Unity 6000.5.0f1 컴파일 및 전체 PlayMode **105/105 통과**, 실패·생략 0, 종료 코드 0. 기존 85개와 신규 관리 13개·실제 씬 7개를 포함한다. 초기 미구현 11개 실패, 실제 Player 정지 중 자동공격과 버튼 포커스 실패, 활성 창 재전면 포커스 실패를 확인하고 수정했다.
