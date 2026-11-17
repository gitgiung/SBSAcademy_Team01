# Remote Startup Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 원격 설정을 명시적으로 켠 경우에만 기존 LoadScene에서 호환 릴리스·검증된 카탈로그·다운로드 동의·진행·재시도를 거치게 한다.

**Architecture:** Managers partial이 영속 준비와 현재 실행의 릴리스를 소유한다. ResourceManager partial이 HTTP 메타데이터·카탈로그 및 기존 다운로드 API를 담당한다. UI_LoadScene은 상태를 읽고 동의/재시도만 요청한다. 새 전역 매니저나 씬 파일 변경은 없다.

**Tech Stack:** Unity 6000.5.0f1, Addressables 2.9.1, UGUI/TMP, UnityWebRequest, SHA-256, 기존 격리 HTTP 테스트 도구.

**Spec:** `docs/superpowers/specs/2026-11-17-android-remote-content-design.md`. 사용자 설계 승인 및 이전 기반 API 확인 후 다음 단계 진행 요청에 따른 구현이다.

## Global Constraints

- 기존 사용자 변경(두 씬과 meta 삭제 포함), 브랜치, 에셋 GUID·직렬화 참조를 보존한다. 씬·프리팹·패키지·Project Settings는 변경하지 않는다. 씬 검증은 MainScene만 대상으로 한다.
- 설정 없음/Use Remote Content 꺼짐은 기존 로컬 경로다. 설정을 켰는데 실패하면 TestReady로 우회하지 않는다.
- 기존 API와 전투 수치·MP는 유지한다. 실제 게임 표·스킬 에셋의 원격 이전, 운영 CDN, APK/AAB, 영속 정상 릴리스/오프라인·롤백 정책은 다음 단위다.
- 원격 manifest·catalog는 HTTPS를 기본으로 한다. 개발 HTTP는 Editor/Development Build와 설정의 명시 허용이 모두 필요하다.
- UI 문구는 기술 검증용 최소 상태 안내이며 게임 기획 팝업 내용을 확정하지 않는다.
- 현재 체크아웃에서 직접 구현하고 Unity 실행은 별도 `.utmp/remote-download/project`에서 한다. 커밋·배포하지 않는다.

## Review Focus

- 잘못된 플랫폼/앱 호환/schema/URL/해시/라벨: 카탈로그나 콘텐츠 다운로드 전에 실패(Task 1).
- 카탈로그 변조 및 메타데이터 실패: SHA-256 확인, 제한 크기·timeout·재시도, 정상인 척 진입하지 않음(Task 2).
- Clear 도중 카탈로그 완료: 늦게 등록된 locator 제거, 작업 핸들 반환, 새 작업을 지우지 않음(Task 2).
- 동의 전 다운로드 및 실패 후 무한 자동 재시도: 명시적인 버튼 요청까지 대기, 캐시 0바이트만 동의 생략(Task 3).
- 씬 호출자 이탈/동시 호출/같은 프로세스의 새 버전: Managers 소유 공유 준비, 옵션·릴리스 고정, 설정 변경은 거부(Task 3).

## Task 1: 릴리스·설정 계약

**Files:** Create `Assets/0.Scripts/Framework/Managers/Core/RemoteContentRelease.cs`, `RemoteContentSettings.cs`, `Assets/Tests/PlayMode/RemoteContent/RemoteStartupTests.cs`.

**Interfaces:** `RemoteContentRelease.TryParse(json, platform, compatibility, allowHttp, out release, out error)`; 필드는 formatVersion=1, platform, appCompatibility, contentVersion, catalogUrl, catalogSha256(64 hex), requiredLabel. 설정은 UseRemoteContent/ReleaseUrl/AppCompatibility/AllowDevelopmentHttp 읽기 전용 속성이다.

- [x] reflection 회귀 테스트부터 추가하고 API 부재 실패를 확인한다.
- [x] JsonUtility 파싱·필수값·정확한 호환·HTTPS/개발 HTTP 검증을 구현한다. 설정 asset은 기본 꺼짐이며 자동 생성하지 않는다.

## Task 2: HTTP와 카탈로그 수명

**Files:** Create `ResourceManager.RemoteContent.cs`; modify `ResourceManager.Clear`; extend `tools/RemoteContentValidation/{BuildFixture.cs,Serve.mjs,RunUnity.ps1}`.

**Interfaces:** `FetchRemoteReleaseAsync(url, platform, compatibility, allowHttp, Action<RemoteContentRelease,string>)`, `LoadRemoteCatalogAsync(release, Action<bool,string>)` (호출 전 release 검증 필수).

- [x] 실제 HTTP manifest와 카탈로그 fixture를 만들고, 파싱·404·해시 불일치·성공·같은 실행 내 카탈로그 변경 거부·Clear 회귀를 작성한다.
- [x] manifest 64KiB/catalog 16MiB 제한, 요청 timeout 15초, redirect 금지, SHA-256 확인 후 내용 hash 파일명으로 캐시한다. 메타데이터만 HTTP로 읽고, 검증된 로컬 카탈로그를 Addressables에 등록한다. 원격 URL을 Unity에서 다시 자동 조회하게 하지 않는다.
- [x] HTTP 요청을 Clear에서 abort/dispose한다. 진행 중 카탈로그는 취소 표시 후 실제 완료 콜백에서 locator/handle을 정리하며 그동안 재시도는 정리 완료를 기다린다.
- [x] 현재 실행에 하나의 카탈로그 identity를 고정한다. 디스크 캐시는 자동 전체 삭제하지 않는다.

## Task 3: 영속 준비·LoadScene·UI 연결

**Files:** Modify `Managers.cs` partial; Create `Managers.RemoteContent.cs`; Modify `LoadScene.cs`, `UI_LoadScene.cs`; Create `Assets/0.Scripts/Framework/Editor/RemoteContentSettingsMenu.cs`.

**Interfaces:** Managers `RemoteStatus`, `PrepareRemoteContent()`, `PrepareRemoteContent(RemoteContentSettings)`, `ApproveRemoteDownload()`, `RetryRemoteContent()`; 상태 Disabled/CheckingRelease/LoadingCatalog/CheckingSize/AwaitingConsent/Downloading/Ready/Failed.

- [x] 설정 꺼짐·잘못된 호환 실패·동의 전 캐시 미생성·동의 후 Ready·재시도·동시 호출·호출자 이탈·설정 변경 거부·Clear 후 재준비 회귀를 작성한다.
- [x] Managers가 코루틴/상태/릴리스 snapshot을 소유하고 ResourceManager의 API를 순서대로 사용한다. Ready는 원격 필수 번들의 디스크 준비 의미이며 GameDataReady와 별개다.
- [x] LoadScene은 원격 Ready/Disabled 전에는 기존 PrepareGame/씬 전환을 호출하지 않는다. 실패 중 UI 재시도를 기다린다. UI는 로컬 폰트를 이용한 기술 검증용 상태/용량·바이트/동의·재시도만 런타임 생성한다. 로컬 모드에는 생성하지 않는다.
- [x] Tools > Remote Content > Settings 메뉴에서 필요할 때만 꺼진 설정 asset을 만들고 선택한다. URL·기본값을 임의로 운영 주소에 연결하지 않는다.

## Task 4: 검증과 인수인계

- [x] 새 테스트가 없는 구현에서 실패하는 것을 확인한 뒤 실제 원본 코드를 격리 Unity에서 컴파일·PlayMode 테스트한다. 기존 다운로드 14개도 함께 실행한다.
- [x] 기존 전체 원본 C# 7개 어셈블리 컴파일을 실행한다. 실제 Android 빌드나 MainScene Play로 확대 해석하지 않는다.
- [x] 독립 코드 리뷰 후 필요한 수정·회귀를 검증한다. 문서와 Handoff에 테스트 수·실제 범위·직접 메뉴/실행/정상/오류 확인 절차를 남긴다.

## Execution ledger

- Ruling: 이미 승인된 설계의 다음 구현 단위를 현재 세션에서 직접 진행한다. 현재 전용 브랜치와 사용자 변경을 보존한다.
- Ruling: 영속 롤백·오프라인 허용 정책은 미정이다. 이번에는 manifest 연결 실패 시 차단/재시도하며, 이전 정상으로 자동 폴백한다고 주장하지 않는다. 다운로드 캐시 재사용과 원격 콘텐츠를 실제 게임에 사용하는 것은 별도다.
- Ruling: 카탈로그 메타데이터도 직접 받고 hash 확인 후 로컬 경로로 등록한다. 네트워크 timeout·검증을 기존 Addressables 전역 설정 변경 없이 보장하기 위해서다. 요청별 크기 제한은 기술적 자원 상한이며 게임 기획 수치가 아니다.
- 사전 인터페이스 점검: Task 1 DTO→Task 2 HTTP/카탈로그→Task 3 Managers→LoadScene/UI의 단방향 의존. 기존 ResourceManager 다운로드와 메모리 준비 계약은 그대로 소비한다.

## 완료 기록

- Task 1: complete — 기존 다운로드 14개 통과 상태에서 릴리스/카탈로그 계약 부재로 새 10개 실패 확인 후 구현.
- Task 2: complete — 실제 HTTP 릴리스와 바이너리 카탈로그, SHA-256 확인, 수신 단계 크기 상한, timeout·redirect 차단, 세대 및 locator/handle 수명 구현.
- Task 3: complete — Managers 준비 부재로 새 6개 실패, UI/LoadScene 미연결로 새 2개 실패를 확인한 뒤 연결. Disabled, 동의, 재시도, 공유/이탈, 설정 고정, Clear 재준비를 검증했다.
- Task 4: complete — 최종 40/40 PlayMode 통과, 실패·생략 0. 원본 전체 C# 7개 어셈블리 컴파일 exit 0. 독립 리뷰의 수신 버퍼 상한 지적을 LimitedDownloadHandler로 반영하고 추가 Clear/릴리스/UI 회귀를 확인했다.
- 검증 보정: Unity 2.9는 카탈로그 LocatorId를 위치 PrimaryKey로 덮어쓴다. fixture 이름을 LocatorId로 가정한 검사 대신 실제 키를 찾는 locator 수와 이전 등록 handle 반환을 검사하도록 수정했다. 실제 등록이 아직 Settled=false일 때 Clear하도록 명시적으로 확인한다.
- Evidence: .utmp/remote-download/startup-red.xml, preparation-red.xml, ui-red.xml, startup-final.xml (40/40), startup-final-compile.log. 생성물은 Git 제외이며 영속 테스트/도구/문서가 재현 근거다.
- 상태: 작업 중 이전 단계 커밋 de76099가 반영돼 현재 HEAD가 바뀌었음을 재확인했다. 에이전트는 커밋/체크아웃/복구하지 않았다. 씬/프리팹/Packages/ProjectSettings diff 없음.
- 한계: 주변 서비스는 격리 경계이고 버튼 onClick 연결을 검사했다. 원본 전체 PlayMode·씬·실제 포인터/시각 배치·Android APK/AAB/번들/기기·실제 앱 재실행·운영 CDN은 미검증이다. 게임 스킬 공급 이전과 영속 정상 릴리스/오프라인 정책은 후속이다.
