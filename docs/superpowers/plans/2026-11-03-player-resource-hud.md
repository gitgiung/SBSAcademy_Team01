# Player Resource HUD Implementation Plan

> **For agentic workers:** Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** HP·MP·EXP를 PlayerData → PlayerView 이벤트 흐름으로 표시하고 좌우 입력에 HUD Slider 값이 바뀌는 문제를 수정한다.

**Architecture:** 기존 PlayerView 생성자와 Player.healthSlider를 보존한다. Player에 MP/EXP Slider 참조를 추가하고 PlayerView.BindResourceSliders(Slider,Slider)로 연결한다. 모든 HUD Slider는 표시 전용으로 설정하고 모델 변경은 SetValueWithoutNotify로 표시한다.

**Tech Stack:** Unity 6000.5.0f1, 기존 uGUI Slider/EventSystem, NUnit PlayMode.

**Spec:** 사용자 Mobile UI/PlayerHUD의 HP·MP·EXP MVC 연결 요청 및 좌우 입력 HP바 오동작 제보.

## Global Constraints

- 사용자 기존 변경을 보존한다. MainTestScene의 Player 컴포넌트에 MP/EXP 참조 2줄만 추가한다. 씬 전체 재저장·다른 Slider·다른 씬·프리팹 수정은 하지 않는다.
- EXP 요구량·레벨업·보상은 미정이다. PlayerData에는 maxEXP/CurrentEXP, InitEXP(), bool SetEXP(float current,float maximum), EXPChanged(float,float)만 추가한다. 초과 EXP는 모델에 유지하고 표시만 100%로 제한한다.
- 기존 MP API와 PlayerView 생성자를 유지한다. 새 전역 UI 매니저·런타임 asmdef를 만들지 않는다.
- Slider 레이아웃·색상·채움 방향은 유지한다. transition=None으로 비활성 색상 변화를 막고 interactable=false/navigation=None으로 입력을 막는다.

## Review Focus

- HUD 선택 후 좌우 Move 이벤트로 값이 변하지 않아야 한다.
- 값 표시가 onValueChanged를 다시 발행해 게임 로직으로 역전파되지 않아야 한다.
- 최대치 0/잘못된 수치는 빈 표시가 되며 NaN이 UI에 전달되지 않아야 한다.
- PlayerView.Dispose 후 세 데이터 이벤트 모두 구독 해제되어야 한다.
- MP/EXP 참조가 없는 기존 씬도 HP만 정상 표시해야 한다.

## Task 1: 재현 및 표시 경계

**Files:** Assets/Tests/PlayMode/PlayerHudTests.cs 및 meta, Assets/0.Scripts/Game/Player/PlayerView.cs.

- [x] 기존 PlayerView에 연결한 Slider를 선택한 뒤 좌우 AxisEventData로 HP 표시만 변하는 현상을 실패 테스트로 확인한다.
- [x] 표시용 Slider 설정과 공통 비율 표시를 구현한다. HP도 SetValueWithoutNotify를 사용하며 기존 RefreshHealth() 유지.

## Task 2: MP 및 EXP 연결

**Files:** Assets/0.Scripts/Game/Player/Data/PlayerData.cs, PlayerView.cs, Player.cs, Assets/9.Scenes/MainTestScene.unity.

**Interfaces:** PlayerData.CurrentEXP, InitEXP(), bool SetEXP(float current,float maximum), EXPChanged(float current,float maximum); PlayerView.BindResourceSliders(Slider mp,Slider exp), RefreshMP(), RefreshEXP(), RefreshResources().

- [x] MP 소모/회복 및 EXP 설정으로 바가 갱신되는 테스트, 잘못된 EXP 거절, 표시 범위·구독 해제·null 참조 테스트를 먼저 작성한다.
- [x] EXP 상태·알림을 추가하고 MP/EXP 이벤트를 PlayerView에 구독/해제한다. 초기 연결 및 Player 재활성화 시 모델에서 새로 표시한다.
- [x] MainTestScene의 Player.mpSlider는 1395659084, expSlider는 1801941379로 연결한다. 기존 healthSlider=496842392 유지. 파일ID가 실제 PlayerHUD의 Slider인지 정적 검증한다.

## Task 3: 검증과 전달

- [x] 이전 검증 프로젝트의 소스·씬 복사본에서 새 테스트 실패/수정 후 통과 및 이전 48개 회귀 실행.
- [x] 독립 코드 리뷰 및 diff --check. 씬 변경이 시작 파일에 참조 2줄을 넣은 것과 바이트 단위로 일치하는지 확인.
- [x] Handoff에 실제 검증 결과, 사용자 직접 확인과 실제 화면/기기 미검증 범위를 기록한다.
