# 수동 공격 표시 회귀 검증

`Probe.cs`는 Unity Editor의 `ManualSkillAnimationProbe.Run` 진입점이다. 실제 PlayerView, PlayerDefenseVfx, LightAttackAnimationState와 Player Animator/클립을 사용한다. 주변 Player/Data는 `Boundary.cs`로 대체하므로 전체 입력·피해·MP·행동 잠금 통합 검증은 아니다.

재실행 준비:

1. 원본과 다른 짧은 경로의 Unity 6000.5.0f1 검증 프로젝트를 준비한다. 원본 프로젝트를 두 번째 에디터로 열지 않는다.
2. 검증 프로젝트의 Assets에 Boundary.cs와 원본 `Assets/0.Scripts/Game/Player/`의 PlayerView.cs, PlayerDefenseVfx.cs, LightAttackAnimationState.cs를 복사한다. LightAttackAnimationState.cs.meta도 복사해 Controller 참조를 보존한다.
3. 원본 `Assets/Resources/PlayerAnimator/`를 검증 프로젝트의 같은 상대 경로로 복사한다. 원본 자산을 수정하거나 저장하지 않는다.
4. Probe.cs를 검증 프로젝트 Assets/Editor에 복사한다. 검증 프로젝트에 Unity UI와 Animation 내장 모듈을 활성화한다.
5. 검증 프로젝트에서 `-batchmode -nographics -executeMethod ManualSkillAnimationProbe.Run`으로 실행한다. 결과는 검증 프로젝트 루트의 manual-animation-result.txt이며, 성공 시 PASS 15/15 및 종료 코드0이다.

Guard 유지, 공격 즉시 진입/복귀/재시작, 취소, 리셋, 방어 VFX 비활성, 대시 보존, 실제 가드와 차징 표시의 독립 해제를 검사한다. 스프라이트 원화/화면 구성 및 원본 MainScene Play는 별도 확인한다.
