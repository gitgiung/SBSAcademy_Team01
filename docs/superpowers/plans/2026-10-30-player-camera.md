# 플레이어 중앙 고정 카메라 구현 계획

> 실행: superpowers:executing-plans로 현재 세션에서 직접 수행한다. 사용자의 진행 승인을 적용한다.

**Goal:** MainTestScene의 구 Update 추적을 Cinemachine 중앙 고정으로 교체한다.
**Architecture:** Main Camera의 Brain이 별도 CinemachineCamera와 Position Composer의 결과를 적용한다. 런타임 사용자 스크립트는 추가하지 않는다.
**Tech Stack:** Unity 6000.5.0f1, Cinemachine 3.1.7, 기존 PlayMode 테스트.
**Spec:** ../specs/2026-10-30-player-camera-design.md

## 제약과 검토 초점

- 사용자 씬 재저장 없이 카메라 관련 YAML만 수정한다. GiungTestScene은 변경하지 않는다.
- 중앙 고정·회전 독립·직교 크기 10·깊이 간격 10. 맵 경계/화면 흔들림은 후속이다.
- 시작 위치, 큰 이동, 목표 회전, 정지/재개, 씬 재진입을 실제 Main 씬 PlayMode 테스트로 확인한다.

## 작업

- [x] MainTestScene을 로드해 Brain 존재와 화면 중심 추적을 검증하는 PlayerCameraTests.cs를 추가하고 구 구현에서 실패 확인.
- [x] Packages/manifest.json에 공식 3.1.7 추가. Unity가 생성한 packages-lock.json 확인.
- [x] MainTestScene의 구 CameraPlayer 컴포넌트를 Brain으로 교체하고 CinemachineCamera/Position Composer를 추가. 기존 목표 Transform을 연결한다. LateUpdate, damping/dead zone/lookahead 0을 적용한다.
- [x] 참조가 이 씬뿐임을 확인한 CameraPlayer.cs와 meta 제거.
- [x] 짧은 별도 검증 복사본에서 컴파일과 관련 PlayMode 테스트 수행. 실패는 코드와 환경 문제로 구분한다.
- [x] diff와 참조 검토, 인수인계·직접 확인 순서 갱신. 임의 커밋·브랜치 전환은 하지 않는다.

검증 결과: 관련 2/2 통과, 전체 100 통과/16 기존 실패/0 생략. 실패 상세는 docs/AI/PlayerCamera.md 참조.
