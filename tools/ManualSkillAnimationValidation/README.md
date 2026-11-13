# 수동 공격 표시 회귀 검증

`Probe.cs`는 Unity Editor의 `ManualSkillAnimationProbe.Run` 진입점이다. 실제 PlayerView, PlayerDefenseVfx, PlayerLightSkill, PlayerActionGate, LightAttackAnimationState와 Player Animator/클립을 사용한다. 주변 Player/Data는 `Boundary.cs`로 대체하므로 전체 입력·피해·MP 통합 검증은 아니다. 행동 잠금은 실제 PlayerActionGate를 실행한다.

재실행 준비:

1. 원본과 다른 짧은 경로의 Unity 6000.5.0f1 검증 프로젝트를 준비한다. 원본 프로젝트를 두 번째 에디터로 열지 않는다.
2. 검증 프로젝트의 Assets에 Boundary.cs와 원본 `Assets/0.Scripts/Game/Player/`의 PlayerView.cs, PlayerDefenseVfx.cs, PlayerLightSkill.cs, PlayerActionGate.cs, LightAttackAnimationState.cs를 복사한다. LightAttackAnimationState.cs.meta도 복사해 Controller 참조를 보존한다. PlayerLightSkill의 사용하지 않는 레거시 경로도 컴파일되므로 Bullet 및 Managers.Pool API/의존성은 검증 프로젝트에 별도로 필요하다. 이번 실행은 기존 기본 탄환/풀링 검증 복사본을 재사용했다. 원본 Player/Data를 Boundary와 중복 포함하지 않는다.
3. 원본 `Assets/Resources/PlayerAnimator/`를 검증 프로젝트의 같은 상대 경로로 복사한다. 원본 자산을 수정하거나 저장하지 않는다.
4. Probe.cs를 검증 프로젝트 Assets/Editor에 복사한다. 검증 프로젝트에 Unity UI와 Animation 내장 모듈을 활성화한다.
5. 검증 프로젝트에서 `-batchmode -nographics -executeMethod ManualSkillAnimationProbe.Run`으로 실행한다. 결과는 검증 프로젝트 루트의 manual-animation-result.txt이며, 성공 시 PASS 41/41 및 종료 코드0이다.

차징 중 Guard 주기 재시작/여러 주기 반복과 일반 Guard의 단발 유지도 검증한다. 차징 반복은 PlayerView의 차징 표시 갱신에서 수행하므로 검증에서도 매 프레임 SetHeavyChargeAnimation을 호출한다.

Guard 유지, 공격 진입/복귀/재시작, 취소, 리셋, 방어 VFX 비활성, 대시 보존, 실제 가드와 차징 표시의 독립 해제 및0.25초 타격/0.74초 잠금/모션 종료 해제, 중복 발사·오래된 ID 방어, 일시정지·모션 중단·Animator 비활성화·큰 프레임 간격·사망·Controller 제거를 검사한다. 스프라이트 원화/화면 구성, 실제 스킬 발사 코루틴과 원본 MainScene Play는 별도 확인한다.
