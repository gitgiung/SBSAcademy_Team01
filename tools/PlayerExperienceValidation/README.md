# 플레이어 성장 검증

`./tools/PlayerExperienceValidation/Run.ps1`은 .NET 9에서 실제 PlayerData·PlayerExperienceProgress·PlayerExperienceSettings 소스로 레벨·XP·선택 대기 계약을 검사한다. UnityEngine 경계만 대체하며 Unity 수명과 화면을 검증하지 않는다.

`./tools/PlayerExperienceValidation/RunUnity.ps1 -ResultName final`은 Unity 6000.5.0f1을 사용해 `.utmp/GrowthUnity` 별도 프로젝트에서 PlayMode 검사를 실행한다. 원본 프로젝트를 Unity로 열거나 씬·프리팹을 저장하지 않는다. Unity 설치 위치는 스크립트의 unityExe로 지정한다. 생성물은 Git 제외다.

`./tools/PlayerExperienceValidation/RunCompile.ps1`은 기존 Unity 컴파일 응답 파일을 이용해 런타임/에디터 소스를 설치본 Roslyn으로 컴파일한다. 새 소스를 포함하고 에디터는 이번 런타임 참조 DLL을 사용한다. 출력은 `.utmp/PlayerExperience/Compile`로 격리한다. 원본 에디터의 임포트/실행 검증을 대신하지 않는다.

Unity 사본에는 실제 PlayerData·PlayerView·카드 추첨/획득·상태 패널·카드 패널·CombatPopupController·UI_CombatPopup·UI_Popup·UIManager를 복사한다. 테스트 입력 표만 고정하며 Unity/ugui/TMP 이벤트·코루틴·팝업 수명은 실제 실행한다.

`Unity/Boundary.cs`가 대체하는 것은 Player 전투/입력, PlayerDefenseVfx, GameManager 승패 판정, Resource/Sound, UI_Base 바인딩이다. 실제 씬 연결, Player 입력 Submit/터치, GameManager.LateUpdate, 실제 씬 언로드, 폰트/해상도 시각 검증, Addressables, Android/기기는 별도다. 객체 Destroy 검사는 씬 전환 전체 검증을 대신하지 않는다.

새 테스트/asmdef는 원본 Assets에 추가하지 않는다. 설정 에셋·씬·프리팹·Project Settings·패키지도 원본에서는 변경하지 않는다.
