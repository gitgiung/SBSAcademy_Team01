# GiungTestScene 승패·재시작 연결 계획

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans.

**Goal:** 사용자 승인에 따라 테스트 씬의 승패 결과와 재시작·타이틀 복귀를 연결하고 추후 MainTestScene 흐름 복원 지점을 표시한다.
**Architecture:** 기존 GameManager에 실제 Player 사망 구독과 상태 변경 알림을 추가한다. 기존 CombatPopupController가 결과 알림에 자기 창과 정지 소유권을 정리한다. 재시작 기본은 LoadTestScene이며 GiungTestScene만 임시 직렬화 설정을 사용한다.
**Tech Stack:** Unity 6000.5.0f1, 기존 Assembly-CSharp, uGUI, PlayMode reflection 테스트.

## Global Constraints

- 기존 공개 API·GUID·씬 객체·스폰 설정 보존. 새 싱글톤 없음.
- 보상·레벨·6회 보스 최종 승리 판정은 구현하지 않음. 기존 명시적 최종 승리/테스트 신호 유지.
- TODO 주석으로 Giung 전용 설정을 추후 LoadTestScene으로 변경해 MainTestScene 진입을 복원하도록 명시.
- 같은 프로젝트에 Unity 중복 실행 금지. 별도 임시 복사본에서 검증.

## Review Focus

- 레벨업/일시정지 중 결과: 창이 결과 UI를 가리지 않고 timeScale 0 유지.
- 동시 사망/승리: 기존 패배 우선, 중복 결과 없음.
- 실제 Player 사망: 비활성화 전에 1회 알림; 단순 비활성화와 구분.
- 재시작과 타이틀: UI·풀·사운드 정리, 시간 복원, 리소스 보존.
- 실패한 전환: 시간 배율과 재시도 가드 복원, 기존 결과 유지.

### Task 1 — 결과 연결

- [x] GameResultFlowTests에 실제 씬 사망·결과 중 팝업 정리·동시 판정·반복 결과 테스트를 작성하고 실패 확인.
- [x] Player.Died와 GameManager.StateChanged를 기존 담당자에 추가하고 CombatPopupController 구독/해제를 연결.
- [x] 실제 결과 패널·버튼 포커스와 정지 유지 검증.

### Task 2 — 임시 재시작과 정상 경로 보존

- [x] 실제 씬 재시작/타이틀/실패한 전환의 회귀 테스트 작성 및 실패 확인.
- [x] Define.EScene 끝에 GiungTestScene 추가, GameManager 재시작 기본 LoadTestScene 유지, 씬에는 Giung 값과 실제 Player 참조만 추가.
- [x] Build Profiles Scene List 끝에 Giung 씬을 추가. 기존 타이틀/로딩/메인 순서 보존.
- [x] TODO 주석과 문서에 복원 위치를 기록하고 전체 PlayMode 실행.

### Task 3 — 리뷰와 인수인계

- [x] 독립 코드 리뷰, diff·소스 일치 확인, 문서 갱신과 직접 확인 절차 제공.
- [x] 커밋·푸시는 요청하지 않았으므로 하지 않음.

## 시작 기록

2026-10-30: feature/poolmanager, HEAD 1d7b95e, 작업 트리 깨끗함. 사용자가 임시 Giung 재시작 및 향후 Main 흐름 복원 주석을 명시 승인함.

완료: 수정 전6개 중4개 실패, 수정 후 전체111/111, 보강 후 최종113/113. 정상 Load→Main 복원과 사망1회 검증. 독립리뷰 중요결함 없음. TODO와 Inspector 복원 절차 문서화. 커밋/푸시 없음.
