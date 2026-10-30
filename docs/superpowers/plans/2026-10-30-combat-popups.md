# GiungTestScene Combat Popups Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** GiungTestScene에서 일시정지·뒤로가기와 향후 레벨업 이벤트 연결용 팝업을 제공한다.
**Architecture:** 기존 UIManager에 씬 팝업을 등록한다. 씬 로컬 CombatPopupController가 정지 소유권을 관리하고 UI_CombatPopup은 화면 수명과 닫기를 전달한다.
**Tech Stack:** Unity 6000.5.0f1, uGUI, TMP, 기존 InputSystem UI.
**Spec:** docs/superpowers/specs/2026-10-30-combat-popups-design.md

## Global Constraints

- 새 전역 매니저·이벤트 버스·가짜 보상·레벨 시스템 없음. MP 표기 유지.
- 기존 씬 객체와 스폰 설정·GUID 보존. 새 UI 프리팹 연결만 추가.
- 사용자 에디터와 별도 임시 복사본에서 Unity 검증.

## Review Focus

- 중복 등록과 이미 열린 팝업 재요청: 단일 인스턴스·단일 정지 소유권.
- 팝업이 겹친 경우: 최상단만 닫고 남은 팝업은 정지 유지.
- 외부 비활성화·Clear·씬 종료: timeScale 누수와 살아남은 UI 없음.
- 승패 또는 기존 외부 정지: 뒤로가기로 임의 재개하지 않음.
- 한글·Raycast·화면 비율·paused 입력: 실제 프리팹과 씬 통합 검증.

### Task 1: 관리 API와 전투 팝업 수명

**Files:** UIManager.cs, Game/UI/CombatPopupController.cs, Framework/UI/Popup/UI_CombatPopup.cs, Tests/PlayMode/CombatPopupTests.cs.
**Interfaces:** RegisterPopupUI(string, UI_Popup), ShowPause(), ShowLevelUp(), CompleteLevelUp(), LevelUpContentRoot.

- [x] 실패 테스트: 등록된 팝업 재사용, 중복·겹침, 정지 복원, 비활성화·Clear, 승패 보존을 실제 API로 검증한다.
- [x] Unity PlayMode에서 구현 전 실패를 확인한다.
- [x] 등록과 로컬 소유권·버튼 수명·수동 Inspector 명령을 구현한다.
- [x] 해당 테스트와 기존 회귀를 실행한다.

### Task 2: 실제 화면과 씬 연결

**Files:** Assets/1.Prefabs/CombatUI.prefab, Assets/UI/Fonts/, GiungTestScene.unity.
**Interfaces:** Task 1의 직렬화 필드와 기존 EventSystem을 사용한다.

- [x] 임시 검증 복사본의 Unity Editor에서 UI 프리팹·한글 글꼴 자산을 생성한다.
- [x] 원본 씬에는 프리팹 인스턴스와 GameManager 참조만 추가한다.
- [x] 실제 씬을 로드해 버튼·배경 입력 차단·팝업 순서·타이머/스폰 정지와 재개를 테스트한다.
- [x] UI 렌더를 확인하고 기존 씬 데이터 보존을 대조한다.

### Task 3: 회귀와 인수인계

- [x] 전체 PlayMode 실행과 diff 검사, 별도 코드 리뷰를 수행한다.
- [x] Handoff와 직접 확인/향후 이벤트 연결 설명을 갱신한다.
- [x] 변경 파일·검증 결과·미검증 범위를 보고한다. 커밋·푸시는 요청이 없어 수행하지 않는다.

## 진행 기록

- 시작: feature/poolmanager, HEAD 25671d6, 작업 트리 깨끗함.
- 사용자는 위 기능 범위를 승인하고 진행을 요청했다. 같은 방향의 재확인 없이 이 세션에서 직접 구현한다.

- 완료: 기존85 + 관리13 + 씬7 = 전체105/105 통과. 종료0. 씬원문보존·소스해시일치·별도리뷰·프리팹렌더 확인.
- Ruling: 실제 씬은 Player.cs를 사용하므로 미연결 MVP 대신 실제 Player의 정지 중 준비된 자동공격과 입력을 가드했다. 0→0.5 공격 타이머 실패로 재현했다.
- Ruling: 배치 Game View 캡처가 시간 초과되어 전용 카메라 프리팹 렌더로 배치를 확인했다. 실제 전체 화면 시각 확인으로 확대해 보고하지 않는다.
