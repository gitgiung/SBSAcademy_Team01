# Android 호환 수정 실행 계획

**Goal:** 에디터 전용 참조로 인한 플레이어 컴파일 위험, 터치 방어 누락, 입력 백엔드 및 MainScene 화면 대응을 수정한다.
**Architecture:** 기존 Player/PlayerDefense·UIManager·EventSystem을 보완한다. 전역 매니저나 풀을 새로 만들지 않는다.
**Tech Stack:** Unity 6000.5.0f1, Input System 1.19.0, uGUI, Android IL2CPP/ARM64.
**Spec:** `docs/superpowers/specs/2026-11-11-android-readiness-design.md`

## 제약 및 검토 중점

- 사용자 변경·GUID·직렬화 참조·공개 API 보존. 현재 작업 디렉터리에서 진행하며 커밋/브랜치 전환은 하지 않는다.
- 단일 터치 소유권, 입력 실패 후 예약 실행 금지, 포커스 손실·팝업·비활성화 후 가드 고착 방지.
- EventSystem이 이미 있는 경우 중복 생성 금지. 기존 플레이어 수치와 보스 시계 정책 유지.
- UI는 1920×1080에서 기존 크기를 보존하고 컷아웃·해상도 변경 후 안전 영역 갱신.
- 성능 후보는 실제 MainScene 경로 확인으로 재분류하고 기기 미측정 한계를 기록한다.

## 작업

1. [x] 플레이어 조건의 직접 컴파일 시도: 수정 전에도 통과. 확정 실패 판단을 철회하고 using 예방 보호 후 동일 컴파일 통과.
2. [x] MobileReadinessTests를 먼저 작성·실행 → Guard 버튼 바인딩 및 터치 누름/해제/취소를 기존 방어 흐름에 연결 → 회귀 통과.
3. [x] EventSystem fallback 회귀 → BaseScene에서 New 모듈 사용 및 Active Input Handling=New → 컴파일/회귀 통과.
4. [x] 안전 영역 반복 변경 회귀 → MainScene의 CanvasScaler와 안전 영역 컴포넌트 최소 연결 → 자동 검증 통과. 실제 MainScene/기기 화면 확인은 남음.
5. [x] 직접 C# 컴파일 7개·격리 PlayMode 8/8·diff 검사 및 Handoff 갱신. 사용자의 종료 요청으로 실제 Android 빌드 시작은 보류.

## 종료 상태

사용자가 현재 변경의 커밋·push를 명시적으로 요청했다. 원본 씬 화면, 실제 APK/AAB/Gradle/IL2CPP/기기 성능 검증은 다음 세션에서 수행한다. 해당 한계를 Handoff에 기록했으며 Android 전체 검증 완료로 취급하지 않는다.

별도 승인 단계 없이 사용자가 요청한 수정 작업을 이어간다. 검증 환경 문제는 코드 실패와 구분하고 가능한 검증을 계속한다.
