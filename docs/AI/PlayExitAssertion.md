# Play 종료 Assertion 조사 — 2026-10-30

현재 확인 대상은 MainTestScene만이다. 아래는 사용자가 범위를 지정하기 전에 실행한 전체 테스트의 실패 기록이며 별도 씬 수정 요청이 아니다.

전체 실행: 114개 중 95 통과 / 19 실패 / 생략 0. 수정 전 비교는 같은 실패 19개와 새 종료 회귀 테스트 실패 1개였다.

GiungTestScene의 Player.inputActions가 fileID 0인 기존 설정으로 16개가 실패했다. Main 로드 관련 3개는 검증 복사본의 Variable Joystick 프리팹 누락이었다. Joystick Pack 동기화 후 Main 실제 Play에서 5마리 반납·재사용·종료를 확인했으며 전체 테스트는 다시 실행하지 않았다.

## 실패 목록

- BossFightKeepsTimerPausedButCombatResumesAfterBack: SetUp : Unhandled log message: '[Error] Player requires an Input Action Asset reference.'. Use UnityEngine.TestTools.LogAssert.Expect
- BringingAnOpenPausePopupToFrontRestoresButtonFocus: SetUp : Unhandled log message: '[Error] Player requires an Input Action Asset reference.'. Use UnityEngine.TestTools.LogAssert.Expect
- KoreanLabelsRenderAndLevelContentRemainsEmpty: SetUp : Unhandled log message: '[Error] Player requires an Input Action Asset reference.'. Use UnityEngine.TestTools.LogAssert.Expect
- PausedPlayerDoesNotStartAutoAttackWhenCooldownReady: SetUp : Unhandled log message: '[Error] Player requires an Input Action Asset reference.'. Use UnityEngine.TestTools.LogAssert.Expect
- PopupRaycastBlocksWorldAndBackWorksWhilePaused: SetUp : Unhandled log message: '[Error] Player requires an Input Action Asset reference.'. Use UnityEngine.TestTools.LogAssert.Expect
- SceneButtonsFreezeCombatAndResumeOnlyAfterBothPopupsClose: SetUp : Unhandled log message: '[Error] Player requires an Input Action Asset reference.'. Use UnityEngine.TestTools.LogAssert.Expect
- SceneUnloadReleasesPauseAndSceneOwnedPopupInstances: SetUp : Unhandled log message: '[Error] Player requires an Input Action Asset reference.'. Use UnityEngine.TestTools.LogAssert.Expect
- ActualDeathNotificationIsSentOnce: SetUp : Unhandled log message: '[Error] Player requires an Input Action Asset reference.'. Use UnityEngine.TestTools.LogAssert.Expect
- ActualPlayerDeathReachesDefeat: SetUp : Unhandled log message: '[Error] Player requires an Input Action Asset reference.'. Use UnityEngine.TestTools.LogAssert.Expect
- DeathWinsWhenBothSignalsArriveInSameFrame: SetUp : Unhandled log message: '[Error] Player requires an Input Action Asset reference.'. Use UnityEngine.TestTools.LogAssert.Expect
- InvalidRestartPreservesResultAndAllowsRetry: SetUp : Unhandled log message: '[Error] Player requires an Input Action Asset reference.'. Use UnityEngine.TestTools.LogAssert.Expect
- RestartReturnsToGiungWithFreshStateAndOwnedManagers: SetUp : Unhandled log message: '[Error] Player requires an Input Action Asset reference.'. Use UnityEngine.TestTools.LogAssert.Expect
- RestoringLoadingRouteReachesMainWithPreparationReady: SetUp : Unhandled log message: '[Error] Player requires an Input Action Asset reference.'. Use UnityEngine.TestTools.LogAssert.Expect
- ReturnToTitleCleansPopupsPoolAndSound: SetUp : Unhandled log message: '[Error] Player requires an Input Action Asset reference.'. Use UnityEngine.TestTools.LogAssert.Expect
- VictoryClosesBothPopupsAndFocusesResult: SetUp : Unhandled log message: '[Error] Player requires an Input Action Asset reference.'. Use UnityEngine.TestTools.LogAssert.Expect
- EmptyLoadingSceneReachesMainTestSceneWithCoreReady: Unhandled log message: '[Error] Problem detected while opening the Scene file: 'Assets/9.Scenes/MainTestScene.unity'. Check the following logs for more details.'. Use UnityEngine.TestTools.LogAssert.Expect
- GiungSceneUsesPoolAndBossPauseReturnsNormalEnemies: Unhandled log message: '[Error] Player requires an Input Action Asset reference.'. Use UnityEngine.TestTools.LogAssert.Expect
- AsyncTransitionRunsSceneHookAndRetainsPreparedState: Unhandled log message: '[Error] Problem detected while opening the Scene file: 'Assets/9.Scenes/MainTestScene.unity'. Check the following logs for more details.'. Use UnityEngine.TestTools.LogAssert.Expect TearDown : Unhandled log message: '[Exception] NullReferenceException: Object reference not set to an instance of an object'. Use UnityEngine.TestTools.LogAssert.Expect
- SyncTransitionRunsSceneHookAndClearsSound: Unhandled log message: '[Error] Problem detected while opening the Scene file: 'Assets/9.Scenes/MainTestScene.unity'. Check the following logs for more details.'. Use UnityEngine.TestTools.LogAssert.Expect

## 변경과 검증

- 최종 관련 PlayMode 4/4 통과: 사망 후 재사용, 소유자 파괴, 일반 비활성화 반납, 종료 후 재부모화 방지. 다른 게임 씬은 열지 않았다.
- SceneEnemyPool.OnApplicationQuit에서 종료 상태와 소유 목록을 정리해 OnDisable의 SetParent를 방지한다.
- 기존 일반 비활성화·파괴 시 반납 계약은 유지한다.
- QuittingAfterReuseDoesNotReparentSceneEnemiesOnDisable은 수정 전 실패, 수정 후 통과했다.
- 별도 최소 재현에서 Managers 선생성 후 실제 Play 종료 시 동일 Assertion 발생, 수정 후 사라짐.
- 실제 MainTestScene: 5마리 생성, 치명 피해로 사망/반납, 자동 스폰으로 동일 객체 5마리 재사용, 실제 Play 종료 시 Assertion 없음.
- Main을 직접 여는 수정 전 실행에서도 이번에는 Assertion이 없었다. 생성·종료 순서에 의존하는 오류로 Main에서 매번 재현된다고 단정하지 않는다.
- 검증 복사본의 에디터 검색 인덱스 예외는 수정 전후 별도로 존재했다. 게임 오류가 모두 없었다고 주장하지 않는다.
- 원본 씬·프리팹·입력 에셋·설정은 수정하지 않았다.
