# 플레이어 중앙 고정 카메라

작성: 2026-10-30. 상태: 사용자 진행 승인 후 구현 완료. 실행 검증 결과는 Handoff.md 참조.

## 목적과 합의

- 기존 CameraPlayer의 Update 직접 추적 구현을 폐기하고 Cinemachine 기반으로 전환한다.
- 사용자는 플레이어 화면 중앙 고정을 선택했다. 추적 지연과 데드 존 없이 XY 위치를 추적한다.
- 맵 경계 제한과 화면 흔들림은 사용자가 명시한 **추가 구현 범위**다. 이번에는 구현하지 않으며 다음 카메라 작업에서 다시 확인한다.

## 접근 비교

- 직접 LateUpdate 추적: 기본 중앙 고정에는 충분하지만 향후 경계 제한과 화면 흔들림을 직접 관리해야 한다.
- Cinemachine 3 + Position Composer: Inspector에서 추적 설정을 관리하고 후속 연출을 확장할 수 있다. 사용자 제안과 중앙 고정 선택에 맞춰 이 방식을 제안한다.

## 이번 구현안

- MainTestScene을 대상으로 Cinemachine 3의 Unity 호환 정식 패키지를 추가한다. 실제 설치 가능 버전은 구현 시 확인한다.
- Main Camera에 CinemachineBrain, 별도 CinemachineCamera에 Position Composer를 구성한다. 현재 PlayerTarget 참조를 추적 대상으로 사용한다.
- viewport 화면 중심은 (0.5, 0.5), Cinemachine 3의 Composition Screen Position 설정값은 (0, 0)이다. XY damping과 dead zone은 0, lookahead는 끈다. 카메라 회전은 고정하고 플레이어 회전은 반영하지 않는다.
- 기존 직교 투영, Orthographic Size 10, 깊이 간격 10을 유지한다. 플레이어 Update 이동 이후 추적 결과가 반영되도록 갱신 시점을 설정한다.
- 기존 CameraPlayer 연결을 제거한다. 저장소 전체 참조를 확인해 다른 씬에 Missing Script를 남기지 않는 방식으로 구 구현을 폐기한다. 관련 없는 씬의 게임 설정은 바꾸지 않는다.
- 새 전역 매니저를 만들지 않는다. 씬과 패키지는 이 전환에 필요한 항목만 변경한다.

## 후속 구현 범위

1. 맵 경계 제한: 실제 맵 경계가 정해진 뒤 Confiner 2D 적용 검토. 맵 크기나 경계 형태를 이번에 확정하지 않는다. 경계에서는 중앙 고정과 충돌할 수 있으므로 우선 동작을 후속 설계에서 결정한다.
2. 화면 흔들림: Cinemachine Impulse 등 검토. 발생 조건, 강도, 지속 시간, 설정 옵션은 미정으로 남긴다.

## 검증과 직접 확인 절차

- 컴파일과 관련 자동 검증 결과를 인수인계에 기록한다. PlayerCameraTests는 실제 MainTestScene을 로드해 viewport 중심, 거리, 회전, 직교 크기 및 씬 재진입을 확인한다.
- Unity에서 Tools → Play From Title을 확인하고, MainTestScene을 실행한다. 이동과 대시 중 플레이어가 Game 화면 중앙에 유지되고 카메라 각도와 확대율이 유지되어야 한다.
- 정지·재개와 씬 재진입 때 추적 중복, 화면 점프, Missing Script가 없는지 확인한다.
- 오류가 있으면 Console 첫 빨간 오류, Main Camera의 Brain, CinemachineCamera의 Tracking Target과 Position Composer 설정을 확인한다.
- Android 빌드·실기기 확인은 별도이며 실행하지 않은 검증을 완료로 보고하지 않는다.
