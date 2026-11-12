# 모바일 조작 설정 구현 계획

> 실행 방식: 현재 사용자 변경을 보존하며 이 세션에서 순차 구현한다. 테스트 접근을 위해 런타임 asmdef를 이동하지 않는다.

**Goal:** 전략 버튼 통합·쿨타임 표시·저장되는 좌우 변경 설정을 제공한다.
**Architecture:** Player는 쿨타임 상태, 기존 SprintCooldownUI는 표시, 씬 내부 MobileControlLayout/CanvasSafeArea는 배치, PlayerStatusPanel은 설정 입력을 담당한다.
**Tech Stack:** Unity 6000.5.0f1, uGUI, TMP, PlayerPrefs.
**Spec:** `docs/superpowers/specs/2026-11-12-mobile-controls-design.md`

## 제약과 검토 중점

- 시작 시 모든 수정·생성 파일을 보존한다. 원본 Unity를 두 번째로 열지 않는다.
- 전략 실패 시 쿨타임이 시작되지 않으며 버튼 다운/업 중복 시전이 없어야 한다.
- 설정 전환은 Time.timeScale=0에서도 적용되고 동일 값 반복/회전 후 inset이 누적되지 않아야 한다.
- 저장값 복원·Canvas/Layout 재활성화·조이스틱 입력 초기화·문자 비반전·기존 대시 동작을 확인한다.
- 씬/프리팹 GUID·참조·버튼 순서와 안전 영역 범위를 검사한다.

## 작업

1. [x] MobileReadinessTests에 실제 쿨타임/표시와 배치 전환 회귀를 추가하고 수정 전 실패를 확인한다.
2. [x] Player 쿨타임 조회와 기존 SprintCooldownUI의 전략 표시 연결을 구현한다.
3. [x] CanvasSafeArea의 원본 기준 반전 API와 MobileControlLayout, 기존 설정 UI 연결·저장을 구현한다.
4. [x] MainScene 전략 버튼을 첫 자식으로 옮기고 5개 묶음/새 씬 컴포넌트의 참조를 최소 변경한다. 정적 배치 검사도 새 요구에 맞춘다.
5. [x] 격리 PlayMode, 관련 대시 회귀, 직접 Android/Editor 컴파일, diff·meta·참조 검토 및 Handoff/사용자 절차를 갱신한다.

## 실행 결과와 판단

- 기본 신규 회귀 전 8/12 → 구현 후12/12. 확장 과정의 headless TMP 그래픽 장치 오류는 그래픽 포함 격리 실행으로 해결했다.
- 재연결 덮개 중복, 비활성 버튼의 Awake 이전 바인딩, CanvasSafeArea 단독 재활성화 문제를 추가 재현(13/16)하고 수정해 최종16/16 통과했다. 독립 리뷰의 두 지적도 수정 후 재검토했다.
- 대시 모델8/8, 실제 씬 정적 배치30/30. 원본 캐시의 Android 조건 직접 게임 어셈블리 컴파일 및 NUnit 누출 부재 검사 성공. Editor 조건은 격리 Unity에서 실제 컴파일·PlayMode 수행했다.
- 사용자 작업 트리가 이미 수정 중이어서 현재 체크아웃에서 진행했다. 브랜치 전환·commit·push·원본 Unity 재시작/씬 자동 저장은 하지 않았다.
- 원본 MainScene 렌더링 및 새 Android 전체 빌드·기기 조작감은 남아 있다. 실제 APK 성공으로 확대하지 않는다.
