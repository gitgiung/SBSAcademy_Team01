# 강공격 차징 검증 기록 (2026-11-03)

## 환경과 기준

Unity 6000.5.0f1. 원본 Assets/Packages/ProjectSettings 전체를 별도 `.utmp/v`에 복사해 실행했다. 원본 Unity 프로젝트를 추가 에디터로 열지 않았다. 검증본의 PlayFromTitle에만 batch guard를 두었으며 원본 설정·패키지·씬을 자동 저장하지 않았다.

시작 브랜치 `feat/PlayerSkill`, HEAD `526aaf6`, 작업 트리 깨끗함. HEAD에는 이전 차징 초안이 커밋되어 있었다. 원본 작업물의 커밋·브랜치 전환은 하지 않았다.

## 실패 재현과 수정

- 원래 초안은 `PlayerView.SetHeavyChargeIndicator` 부재로 CS1061 컴파일 실패. 표시 구현 후 컴파일 성공.
- 차징 테스트 32개 중 28통과/4실패를 수정 전 재현했다.
- `KeyboardReleaseDuringPauseCancelsImmediately`: 정지 중 Q 해제를 Update 조기 반환으로 놓침. 정지 중에도 소유한 키 해제를 취소로 처리한다.
- `DefenseOnKeyboardReleaseFrameCancelsInsteadOfReleasing`: 같은 프레임 방어 시작 전에 결과를 발행함. 실제 방어·회피 시작을 키 해제보다 먼저 판정한다.
- `NewTouchAfterFocusLossCanStartWithoutMissingOldPointerUp`: 포커스 취소 후 bridge의 이전 포인터 예약이 남음. 소유 실행이 달라진 예약을 새 down에서 정리한다.
- `NeutralMobileJoystickRetainsLastUnquantizedAim`: 중립에서 View의 45도 방향으로 덮어씀. 실행 중에는 마지막 실제 조준 방향을 유지한다.
- 수정 후 차징 단위12 + 통합20 = **32/32 통과**, 실패/생략0.
- 전체 실행에서 새 패시브 테스트가 이전 씬의 카메라·탄환을 검사하는 격리 문제를 확인했다. 기존 카메라를 테스트 동안 비활성화/복원하고 자체 프리팹의 탄환만 검사하도록 보완했다.
- 그래픽 사용 검증 Unity에서 세 단계 화살표를 RenderTexture에 렌더하고 청록/노랑/주황 픽셀을 검사했다. **1/1 통과**, PNG도 직접 확인했다. 표시 테스트 헬퍼는 임시 검증본에만 두었다.

- 별도 전체 자산 검증본에서 MainTestScene을 실제 PlayMode 로드해 직렬화된 C_Attack의 PointerDown/Up → MP10 차감 → 3단계/1.7배 해제 → 표시 숨김을 확인했다. 씬 연결 smoke **1/1 통과**. 원본 에디터 수동 조작/실제 터치 검증은 아니다.

## 전체 회귀 비교

기준 비교는 HEAD의 Player.cs/Player.HeavyCharge.cs/PlayerHeavyAttackButton.cs를 **검증 복사본에만** 적용했다. HEAD가 원래 컴파일 불가이므로 완성한 View 메서드와 같은 현재 테스트 픽스처를 유지했다. 따라서 수정 전 원본 전체가 통과했다는 주장이 아니다.

기준 전체: **249개 중227통과/22실패/생략0**. 차징 수정 대상4개와 아래 기존 실패18개가 재현되었다. 최종 전체는 **249개 중231통과/18실패/생략0**이며 기준 비교의 차징4개만 통과로 바뀌었다. 아래18개의 테스트 이름과 실패 메시지는 입력 수정 전후 동일했다.

## 기존 실패 목록

입력 에셋 참조가 없는 GiungTestScene Player의 SetUp/씬 실행 오류(16개):

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

전체 테스트에서 이전 씬의 카메라/탄환 및 EventSystem이 남는 영향으로 재현한 기존 테스트 실패(2개):

- PlayerPassiveSkillTests.OutsideRangeOrViewportDoesNotConsumeReadyShot — 탄환0 기대, 실제1.
- PlayerStatusTests.KeyboardSubmitCanEnterAdjustAndLeaveOptionsWithoutLosingSelection — MasterVolume 포커스 기대, 실제 OptionsButton.

차징 범위를 벗어나는 기존 씬 연결과 기존 픽스처 문제는 이번 작업에서 일괄 수정하지 않았다. GiungTestScene, 다른 프리팹, ProjectSettings, 패키지는 원본에서 변경하지 않았다.

## 리뷰와 판단

독립 읽기 전용 리뷰에서 Critical0, Important3, Minor1을 보고받았다. 마지막 중립 조준 문제는 결과 방향 보존 계약에 영향을 주므로 수정 대상으로 올려 위4개 모두 RED→GREEN으로 검증했다. 보류한 리뷰 지적은 없다. 실제 강공격 피해/스킬ID/정식 테이블/공격 애니메이션/사운드·햅틱/그로기·피격 경직은 승인된 후속 범위를 유지한다. 기존 공통 피해 수치의 NaN/극소값 처리도 별도 공통 시스템 검토 대상으로 남긴다.

## 증거와 제한

영속 회귀는 Assets/Tests/PlayMode의 PlayerHeavyChargeTests.cs, PlayerHeavyChargeIntegrationTests.cs다. 임시 결과는 `.utmp/HeavyCharge`의 compile-red.log, heavy-boundary-red.xml/log, heavy-green.xml/log, baseline-full.xml/log, final-full.xml/log, preview.xml/log, scene-smoke.xml/log, charge-stages.png에 보관한다. 이 폴더는 Git 제외 생성물이며 이 문서가 영속 요약이다.

실제 원본 에디터에서 수동 조작, 실제 터치 장치, Android 빌드·기기, 실제 카탈로그/번들은 검증하지 않았다. 조준 화살표 길이는 피해 범위가 아니다. 사용자가 직접 확인할 절차는 HeavyCharge.md를 따른다.