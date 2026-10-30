# UI 팝업 관리

## 호출 계약

- `ShowPopupUI<T>(name)`은 이름별 캐시 인스턴스를 재사용한다. 이미 열려 있으면 열린 목록에서 한 번만 유지하고 최상단으로 옮긴다. 다른 타입으로 같은 이름을 요청하면 실패로 처리한다.
- 열린 팝업은 아래에서 위로 Canvas sortingOrder 10부터 순서대로 배치한다. 닫기·재열기마다 다시 계산하므로 최초 Init 순서나 누적 호출 횟수에 의존하지 않는다.
- `ClosePopupUI(popup)`은 기존처럼 최상단만 닫는다. 인자 없는 호출은 현재 최상단을 닫는다. 닫힌 인스턴스는 캐시에 남는다.
- `CachePopupUI(type)`은 생성한 팝업을 `@UI_Root` 아래 비활성 상태로 준비한다. 열린 스택에 넣지 않으며 이미 열린 캐시를 숨기지 않는다. `CacheAllPopups()`도 기존 열린 팝업을 닫지 않는다.
- 외부에서 파괴되거나 직접 비활성화한 팝업은 다음 관리 API 호출 때 열린 목록에서 제외한다. 파괴된 캐시는 다시 요청할 때 재생성한다.
- `CloseAllPopupUI()`는 모두 숨기고 캐시를 유지한다. `Clear()`는 캐시 소유 팝업을 파괴하고 참조를 비운다. Unity의 Destroy는 프레임 끝에 완료된다. SceneUI는 기존처럼 참조만 비운다.
- 리소스 생성 실패 시 null을 반환하며 캐시·열린 목록·SceneUI를 실패한 객체로 덮어쓰지 않는다. 먼저 해당 주소의 Addressables 로딩 완료 여부를 확인한다.

기존 공개 API를 유지한다. 팝업의 게임 내용·보상·선택지·전투 정지 및 Time.timeScale은 이 관리자가 결정하지 않는다.

## Unity에서 직접 확인

현재 GiungTestScene의 실제 전투 팝업과 일시정지 버튼은 [CombatPopups.md](CombatPopups.md)에서 확인한다. 아래는 검증용 기본 팝업을 런타임에 만드는 PlayMode 테스트로 일반 관리 API를 확인하는 절차다. UI 관리 단계 자체에서는 씬·프리팹을 추가하지 않았고, 실제 전투 UI는 후속 작업에서 연결했다.

1. Unity 컴파일이 끝날 때까지 기다린다. Console에 새 빨간 오류가 없어야 한다.
2. `Tools → Play From Title`이 켜져 있으면 테스트 중 잠시 끈다. 확인 후 원래 설정으로 돌린다.
3. `Window → General → Test Runner → PlayMode`에서 `UIPopupTests`를 선택하고 `Run Selected`를 누른다. **17개 모두 통과**가 정상이다.
4. `RepeatedShowKeepsOneEntryAndOneCloseHidesIt`: 같은 팝업 두 번 열기 후 열린 수 1, 한 번 닫으면 0과 비활성 상태여야 한다.
5. `ShowingAnOpenPopupBringsItAboveOtherPopups`, `ReopeningCachedPopupRefreshesCanvasOrder`: A→B→A 요청 시 A의 Canvas sortingOrder가 B보다 높고 A를 닫으면 B가 남아야 한다.
6. 캐시·외부 파괴·비활성화·CloseAll·Clear·누락 프리팹 테스트까지 모두 녹색인지 확인한다. 이어 `Run All`로 초기화·리소스·몬스터 풀링 회귀도 확인한다. 전투 팝업 후속 작업을 포함한 현재 총 **113개 통과**가 정상이다.
7. 시작 흐름은 `Assets/9.Scenes/TitleTestScene.unity`를 열고 Play를 눌러 `LoadTestScene → MainTestScene` 진입을 확인한다.

오류가 나면 실패한 테스트 이름과 상세 결과, Console의 첫 빨간 오류와 스택을 확인한다. 실제 팝업 연결 후 표시가 안 되면 요청한 이름의 리소스 로딩, `@UI_Root` 아래 팝업의 활성 상태, Canvas sortingOrder, 최상단 닫기 호출 대상을 확인한다.

## 2026-10-30 실행 검증

Unity 6000.5.0f1 컴파일 및 전체 PlayMode 72/72 통과, 실패·생략 0. 신규 UI 17개와 기존 55개를 포함한다. 수정 전 UI 14개 중 13개 실패를 재현하고, 추가 캐시 테스트 3개도 수정 전 실패를 확인했다. 기존 몬스터 씬 테스트의 1초 고정값은 현재 씬의 5초 간격을 읽도록 보정했다. 씬과 몬스터 런타임은 수정하지 않았다.

실제 파생 게임 팝업·화면·입력·해상도별 시각 검증과 Android 빌드·기기는 미검증이다. 다른 파생 타입으로 같은 이름을 요청하는 분기에는 방어 처리가 있으나 해당 타입 충돌의 실행 테스트는 아직 없다.
