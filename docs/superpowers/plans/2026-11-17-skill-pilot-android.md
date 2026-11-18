# Skill Pilot and Android APK Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans. 승인된 원격 배포 설계의 다음 단위를 현재 세션에서 직접 진행한다.

**Goal:** 실제 전투 표와 수살(10025)의 Texture/Frames/아이콘을 GUID 보존해 Resources 밖으로 이전하고, 원격 준비 후 실제 소비 경로를 연결하며 Android APK를 빌드한다.

**Architecture:** ResourceManager의 기존 캐시·타입별 로딩을 재사용한다. Managers가 skill-pilot-v1 프로필 준비와 데이터/연출 검증을 완료해야 Ready로 만든다. 로컬 Editor는 이전한 원본을 AssetDatabase에서 읽고, 로컬 APK는 빌드 시 로컬 참조 asset을 포함한다. 원격 APK에는 로컬 참조 asset을 포함하지 않는다. 새 전역 매니저·씬은 만들지 않는다.

**Tech Stack:** Unity 6000.5.0f1 / Addressables 2.9.1 / ARM64 IL2CPP Android / 기존 격리 HTTP 검사 도구.

**Spec:** `docs/superpowers/specs/2026-11-17-android-remote-content-design.md`.

## Global Constraints

- 사용자 변경인 MainScene, NanumGothic Combat SDF, 원격 설정·meta, AddressableAssetGroupSortSettings·meta의 시작 SHA-256을 기록하고 보존한다. 체크아웃/커밋/씬 재저장하지 않는다. GiungScene 제외.
- 기준 브랜치/HEAD는 실제 조회한 `addressable-resources-loading` / `9d80a66`. 수치·MP·그림 내용과 기존 공개 조회 API는 유지한다.
- 실제 이전 대상은 CombatSkillTable.json, WaterArrow.png, Fx/Frames/WaterArrow.json, Skill_10025.png 네 파일과 meta다.
- 기존 formatVersion=1 릴리스에는 선택적 contentProfile을 추가하며 미지의 프로필은 거부한다. 이전 fixture는 빈 프로필로 유지한다. 새 프로필은 skill-pilot-v1이며 앱 호환 계열도 분리한다.
- 사용자 지시대로 기기 없이 APK 빌드까지만 한다. 설치·터치·기기 네트워크 검증은 하지 않는다. 유료 호스팅·운영 배포 없음.
- 원본 에디터와 같은 프로젝트 두 번째 실행 금지. 짧은 임시 전체 복사본에서 Android 빌드하고 SDK·패키지는 원본에서 변경하지 않는다.

## Review Focus

- Texture2D/Sprite 동일 주소 선택 충돌: 네 에셋에 명시적 주소/타입으로 로딩한다.
- 다운로드는 성공했으나 표/그림 누락·중복 ID·잘못된 프레임: 게임 진입 전에 검증 실패로 차단한다.
- 기존 정적 캐시와 파생 Sprite가 이전 릴리스를 유지하는 경우: 검증 후 게시하고 Resource.Clear 시 파생 객체를 먼저 정리한다.
- 원격 에셋 누락이 Editor/로컬 fallback으로 감춰지는 경우: 원격 프로필에서는 이전한 키의 로컬 대체를 금지한다.
- 원격 APK에 로컬 참조·Resources 복제본이 섞이는 경우: BuildReport/번들 layout에서 실제 포함 여부를 확인한다.

## Task 1: 기준선과 로컬/원격 경로 계약

- [x] 원본 사용자 파일 hash, 네 대상 bytes/GUID와 참조를 기록한다. 이전 전 전체 복사본 Android baseline APK를 빌드한다.
- [x] AssetDatabase 기반 Editor fallback 및 빌드 전용 로컬 holder 계약을 테스트 먼저 작성한다.
- [x] 네 파일과 meta를 Resources 밖 `Assets/RemoteContent/SkillPilot`로 옮기며 바이트/GUID를 보존한다. 표/아이콘/연출 소비자와 관련 내보내기/검사 도구 경로를 갱신한다.

## Task 2: 실제 프로필 준비

- [x] 릴리스 contentProfile 검증과 타입별 메모리 로딩, 실제 표 게시, WaterArrow 프레임 준비를 회귀 테스트로 먼저 고정한다.
- [x] Managers 원격 준비에서 skill-pilot-v1만 추가 준비한다. 기존 전송 fixture와 로컬 모드를 유지한다. 실패는 Ready가 되지 않으며 이전한 키의 원격 fallback을 허용하지 않는다.
- [x] Clear에 프로필/정적 표/파생 Sprite 수명을 연결한다. 실패 후 재시도에서 잘못된 파생 캐시가 남지 않게 한다.

## Task 3: 실제 번들·빌드 도구

- [x] 기존 격리 HTTP 검사에 네 실제 에셋으로 만든 카탈로그/번들을 추가하고 실제 수치·아이콘·Texture2D·프레임 내용을 검사한다.
- [x] Windows 격리 fixture와 짧은 전체 복사본의 표준 Addressables Android 콘텐츠/카탈로그/릴리스 JSON을 빌드한다. Android content state/Build Layout을 보존한다. 운영 Windows 업데이트 릴리스는 범위에서 제외한다.
- [x] 로컬 APK에는 로컬 holder를 포함하고 원격 APK에는 제외한다. 원격 APK는 전용 프로필 설정을 복사본에서만 만든다. 운영 주소 미정이므로 개발 검증용 localhost(ADB reverse용) 주소를 명확히 구분한다.
- [x] Android APK 빌드 결과·파일 크기·실제 에셋 포함·중복 여부를 baseline과 비교한다. 실제 기기 성공으로 확대 해석하지 않는다.

## Task 4: 최종 검증과 인수인계

- [x] 기존 원격 40개와 새 실제 프로필 테스트, 관련 데이터 검사, 전체 C# 컴파일을 실행하고 기존 실패를 구분한다.
- [x] 독립 리뷰 지적을 검증·수정한다. 사용자 파일 hash를 비교한다.
- [x] Handoff와 도구 README에 메뉴·명령·정상 결과·오류 확인·APK 위치·남은 기기 검증을 기록한다.

## Execution ledger

- 사용자 기기 답변: 기기 없이 APK 빌드까지만 진행. ADB 목록도 빈 상태였다.
- Ruling: 설치 크기 절감은 네 파일의 실제 포함 제거를 우선 검증한다. 전체 게임 용량 목표나 최종 절감 수치를 미리 확정하지 않는다.
- Ruling: 원격 전용 파일을 Resources에 복제해 로컬 모드를 유지하지 않는다. Editor 원본 조회와 로컬 빌드 holder를 구분한다. 원격 APK에서 holder가 있으면 빌드를 거절한다.

- Final: 격리 HTTP PlayMode 48/48, 원본 C# 7개 exit 0. 표 검사 기존 실패 1개 유지(30/31). Android baseline/local/remote APK Succeeded. 최종 동일 코드 local 76,437,686 / remote 76,433,550바이트. 상세는 docs/AI/SkillPilotBuildValidation.md.
