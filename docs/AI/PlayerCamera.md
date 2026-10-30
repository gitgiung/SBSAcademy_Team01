# 플레이어 추적 카메라

2026-10-30, Unity 6000.5.0f1. 사용자 설계 승인 후 적용.

## 구성

- MainTestScene → Main Camera → Cinemachine Brain: Update Method / Blend Update Method 모두 Late Update.
- MainTestScene → Player Follow Camera → Cinemachine Camera: Tracking Target은 Player Target, 직교 크기 10. Rotation Control 없음.
- Position Composer: Camera Distance 10, Damping (0,0,0), Screen Position (0,0). Cinemachine 3의 (0,0)은 실제 viewport (0.5,0.5) 중앙이다. Dead Zone / Lookahead 꺼짐, Target Offset 0.
- 기존 CameraPlayer 스크립트와 컴포넌트 제거. 별도 런타임 카메라 스크립트나 매니저를 만들지 않았다.
- manifest: Cinemachine 3.1.7. Unity가 해결한 lock의 전이 의존성: Splines 2.8.4, Settings Manager 2.1.1.

## 후속 범위 — 사용자 명시 요청

1. 맵 경계 제한: 경계가 확정되면 Confiner 2D 검토. 경계에서 중앙 고정과 충돌할 때의 동작도 그때 결정한다.
2. 화면 흔들림: Impulse 등 검토. 발생 조건·강도·시간은 미정.

이번 구현에 위 두 기능은 포함하지 않았다.

## 직접 확인 순서

1. Unity의 패키지 임포트와 컴파일 완료를 기다린다. MainTestScene을 다시 열어 파일 변경을 반영한다.
2. Tools → Play From Title이 켜져 있으면 직접 Main 씬 테스트 동안 끄고, 확인 후 원래 설정으로 돌린다.
3. Play 후 기존 이동·대시 입력을 사용한다. 플레이어가 항상 Game 화면 중앙이며 카메라가 회전하거나 확대율이 바뀌지 않아야 한다.
4. 정지 후 재개, 씬 재진입도 확인한다. Hierarchy에 Main Camera와 Player Follow Camera가 각각 하나 있어야 한다.
5. Window → General → Test Runner → PlayMode에서 PlayerCameraTests를 실행하면 2개가 통과해야 한다.
6. 오류 시 Console 첫 빨간 오류의 상세 내용, Brain의 갱신 방식, Tracking Target 연결, Position Composer 설정을 확인한다.

## 실행 검증

- 별도 짧은 복사본에서 C# 컴파일 오류 없음. 패키지 편집기 샘플/HDRP 참조 경고와 기존 AudioListener 없음 경고는 존재한다. 이를 경고 없는 실행이라고 보고하지 않는다.
- PlayerCameraTests: 변경 전 Brain 누락으로 2개 실패, 변경 후 2개 통과. 실제 씬을 로드해 이동·목표 회전·큰 위치 변경·정지/재개·재진입의 viewport 중앙, 거리10, 고정회전, 직교크기10을 검증했다.
- 전체 PlayMode: 100 통과 / 16 실패 / 생략 0. 카메라 2개 포함. 아래 실패는 모두 `Player requires an Input Action Asset reference.`이며 이전 ExitAssertion baseline-suite에도 같은 테스트 실패가 있다. GiungTestScene 설정은 이번 작업에서 수정하지 않았다.
- 독립 정적 리뷰 중요 지적 없음. 원본/검증 복사본 씬과 새 테스트 SHA-256 일치, git diff --check 통과.
- 사용자 실제 입력과 화면 시각 확인, Android 빌드·실기기·성능은 미검증이다.
- 임시 로그/XML: .utmp/PlayerCamera/. 이 폴더는 Git에 추가하지 않는다.

### 이번 전체 실행 실패 목록

- CombatPopupSceneTests.BossFightKeepsTimerPausedButCombatResumesAfterBack
- CombatPopupSceneTests.BringingAnOpenPausePopupToFrontRestoresButtonFocus
- CombatPopupSceneTests.KoreanLabelsRenderAndLevelContentRemainsEmpty
- CombatPopupSceneTests.PausedPlayerDoesNotStartAutoAttackWhenCooldownReady
- CombatPopupSceneTests.PopupRaycastBlocksWorldAndBackWorksWhilePaused
- CombatPopupSceneTests.SceneButtonsFreezeCombatAndResumeOnlyAfterBothPopupsClose
- CombatPopupSceneTests.SceneUnloadReleasesPauseAndSceneOwnedPopupInstances
- GameResultFlowTests.ActualDeathNotificationIsSentOnce
- GameResultFlowTests.ActualPlayerDeathReachesDefeat
- GameResultFlowTests.DeathWinsWhenBothSignalsArriveInSameFrame
- GameResultFlowTests.InvalidRestartPreservesResultAndAllowsRetry
- GameResultFlowTests.RestartReturnsToGiungWithFreshStateAndOwnedManagers
- GameResultFlowTests.RestoringLoadingRouteReachesMainWithPreparationReady
- GameResultFlowTests.ReturnToTitleCleansPopupsPoolAndSound
- GameResultFlowTests.VictoryClosesBothPopupsAndFocusesResult
- PooledEnemySpawningTests.GiungSceneUsesPoolAndBossPauseReturnsNormalEnemies
