# Android 입력·빌드 호환 수정

사용자 요청: 앞선 Android 점검에서 확인한 문제를 해결한다. 기존 구조·전투 수치·씬 GUID를 보존하며 현재 체크아웃에서 수정한다. 기존 Handoff 변경은 보존한다.

## 접근

- 플러그인의 에디터 전용 using을 UNITY_EDITOR로 제한한다. 에디터 기능 전체 삭제나 패키지 교체는 불필요하다.
- Player의 기존 TryParry와 PlayerDefense를 재사용한다. Guard 버튼에 소유 포인터의 누름/해제·취소를 연결하며 키보드/게임패드 입력도 유지한다. 실패한 누름을 나중에 예약하지 않고, 다른 손가락의 해제는 무시한다. 포커스 손실·일시정지·비활성화 때 터치 가드가 고착되지 않아야 한다.
- 프로젝트 입력을 New로 통일하고 BaseScene의 EventSystem 생성 경로도 InputSystemUIInputModule로 맞춘다. 기존 씬 UI 입력 모듈은 이미 New를 사용한다.
- MainScene의 MobileUI/HUD Canvas는 기존 기본 화면 1920×1080 기준 Scale With Screen Size로 맞춘다. CanvasSafeArea가 직계 자식의 기존 앵커를 안전 영역에 매핑하고 안전 영역 크기까지 고려해 최소 기준 크기를 확보한다. 계층·직렬화 참조를 유지하며 전체 화면에 stretch된 배경은 제외한다.
- 성능은 실제 경로부터 판별한다. MainScene이 참조하는 Enemy Spawn System은 SceneEnemyPool/Managers.Pool을 사용하므로 기존 단독 스폰 fallback을 무작정 교체하지 않는다. 프로파일 없이 프레임 개선을 주장하지 않는다.

## 검증

플레이어 전처리 조건에서 플러그인 C# 컴파일을 먼저 시도한다. 초기 직접 컴파일은 수정 전에도 통과했으므로 기존 using을 확정 빌드 실패로 보고하지 않는다. UNITY_EDITOR 보호는 예방 정리다. 회귀 테스트는 터치 패링→가드→해제, 다른 손가락 해제, 잠금 중 입력, 포커스 손실, EventSystem 자동 생성, 안전 영역 및 화면 크기 변경을 다룬다. 원본 Unity는 다시 열거나 저장하지 않고 짧은 임시 프로젝트에서 Unity 검증을 수행한다. 원본 전체 Android 콘텐츠 빌드·기기 설치 결과와 격리 검증 결과를 구분한다.
