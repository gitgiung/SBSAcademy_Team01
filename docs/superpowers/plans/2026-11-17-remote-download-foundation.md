# Remote Download Foundation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [x]`) syntax for tracking.

**Goal:** 기존 ResourceManager에 디스크 다운로드 용량 조회·진행·실패·Clear 계약을 추가하고 실제 HTTP 번들로 검증한다.

**Architecture:** ResourceManager를 partial 파일로 확장한다. 다운로드와 메모리 에셋 로딩은 다른 작업이며, 같은 라벨/작업 종류의 요청은 공유한다. 기존 게임 진입·로컬 에셋·씬 설정을 유지하며 이번 단위를 전체 원격 패치 완료로 취급하지 않는다.

**Tech Stack:** Unity 6000.5.0f1, Addressables 2.9.1, Unity Test Framework 1.7.0, PowerShell, 로컬 HTTP 테스트 서버.

**Spec:** `docs/superpowers/specs/2026-11-17-android-remote-content-design.md` (사용자 설계 검토 완료). 이 계획은 첫 원격 시범에 필요한 기반 API 단위다. 앱 버전 선택·카탈로그 배포·동의 UI·실제 스킬 공급 경로 이전·Android 패키지 용량 기준선은 후속 단위다.

## Global Constraints

- 새 전역 매니저·싱글톤·이벤트 프레임워크를 만들지 않는다. 기존 공개 API를 보존한다.
- 사용자 전용 브랜치와 현재 체크아웃에서 작업한다. 시작 시 사용자 삭제인 GiungScene/dongjinScenes 및 meta를 보존한다. 브랜치 전환·복원·커밋하지 않는다.
- 씬·프리팹·Project Settings·패키지·게임 수치·MP·에셋 위치는 변경하지 않는다. GiungScene은 제외한다.
- 같은 프로젝트 두 번째 에디터를 실행하지 않는다. `.utmp/remote-download`의 격리 프로젝트에서 검증하며 실제 원본 런타임 파일을 복사해 컴파일한다.
- 테스트용 HTTP는 loopback에서만 사용한다. 출시 원격 호스팅 URL이나 유료 계정을 만들지 않는다.

## Review Focus

- 없는 라벨이 0바이트 성공으로 처리되는 경우: 위치 조회 결과가 없으면 실패한다(Task 1/2).
- Clear 뒤 늦은 완료가 새 요청을 지우는 경우: 요청 인스턴스와 세대를 확인하고 기존 핸들을 정확히 한 번 반환한다(Task 2).
- 동일 라벨 동시 요청 또는 콜백 예외: 작업을 공유하되 각 요청 결과를 전달하고 다른 요청을 막지 않는다(Task 2).
- 다운로드를 메모리 로딩으로 오해하는 경우: 다운로드 후 Load<T>는 여전히 null이고 별도 PreloadAsync 후만 에셋을 읽을 수 있다(Task 3).
- 앱 재실행·캐시 삭제: 실제 HTTP 번들의 첫 다운로드 후 다시 조회하면 0바이트, 캐시를 삭제하면 다시 다운로드가 필요하다(Task 3).

## Task 1: 다운로드 계약 회귀 테스트

**Files:** Create `Assets/Tests/PlayMode/RemoteContent/ResourceDownloadTests.cs`, 해당 테스트 asmdef/meta, `tools/RemoteContentValidation/RunUnity.ps1`와 검증 경계 파일.

**Interfaces:** 테스트는 Assembly-CSharp 경계를 보존하기 위해 reflection으로 ResourceManager를 생성하고 공개 메서드를 호출한다.

- [x] `GetDownloadSizeAsync(string label, Action<long,string> completed)`가 없는 코드에서 missing method 단언 실패를 확인한다. 성공은 바이트>=0/error=null, 실패는 -1/error 유효다.
- [x] `DownloadDependenciesAsync(string label, Action<long,long> progress, Action<bool,string> completed)`가 없는 코드에서 missing method 단언 실패를 확인한다. 빈 라벨·없는 라벨 실패와 캐시에 들어가지 않는 동작을 검사한다.
- [x] 격리 Unity PlayMode 실행으로 실패 원인이 새 API 부재인지 확인한다. 테스트 로그·XML은 `.utmp/remote-download`에 둔다.

## Task 2: 기존 ResourceManager 확장

**Files:** Modify `Assets/0.Scripts/Framework/Managers/Core/ResourceManager.cs`, Create `ResourceManager.Downloads.cs`와 meta.

**Interfaces:** Task 1의 정확한 두 공개 시그니처를 구현한다. 기존 Load/PreloadAsync/LoadAllAsync/Clear 호출 계약은 유지한다.

- [x] partial 선언과 다운로드 전용 요청 레지스트리를 추가한다. 라벨 trim, 빈/누락 위치 실패, size/download 작업별 공유를 구현한다.
- [x] `Addressables.GetDownloadSizeAsync`와 `Addressables.DownloadDependenciesAsync`를 사용한다. 다운로드 핸들에는 `autoReleaseHandle=false`를 사용하고 완료/실패/Clear에서 소유권을 반환한다.
- [x] byte 진행은 GetDownloadStatus를 사용한다. 콜백 예외를 격리하고 종료 상태·Clear 세대를 보장한다. 코루틴 대기자가 없어져도 Addressables 완료 콜백에서 핸들 정리가 끝난다.
- [x] 진행 중 Clear·동시 요청·재시도 회귀 테스트를 추가하고 실행한다. 기존 리소스 캐시는 다운로드 요청으로 채우지 않는다.

## Task 3: 실제 HTTP 번들 검증

**Files:** Create `tools/RemoteContentValidation/BuildFixture.cs`, `Serve.mjs`, `README.md`; Task 1 테스트에 HTTP 통합 시나리오를 추가한다.

**Interfaces:** 검증 Editor 코드가 격리 프로젝트에서 실제 AssetBundle을 만든다. localhost 서버가 번들을 제공한다. 테스트는 표준 AssetBundleProvider/BundledAssetProvider와 IResourceLocation을 사용해 실제 Unity 캐시를 확인한다.

- [x] 다운로드 전 용량 >0, 다운로드 후 조회 0, ResourceManager.Load는 null, PreloadAsync 후 TextAsset 내용 일치를 확인한다.
- [x] 같은 라벨 동시 요청·요청자 이탈·실패 후 다시 요청·Clear 중 완료·Clear 직후 새 요청을 검사한다.
- [x] HTTP 실패와 캐시 삭제 후 용량 복원을 검사한다. 서버는 테스트 종료 때 정리한다. 실제 Android 앱/기기 테스트를 했다고 보고하지 않는다.
- [x] 새 파일을 포함해 기존 런타임 전체 C#을 기존 컴파일 참조로 격리 출력한다. 실행한 범위와 실패·환경 제한을 기록한다.

## Task 4: 사용 계약·인수인계·검토

**Files:** Modify `docs/AI/ResourceLoading.md`, `docs/AI/Handoff.md`; 새 검증 도구 README.

- [x] API 사용 순서(용량 조회→사용자 동의→다운로드→Preload)를 문서화한다. 이번에는 UI나 카탈로그 선택이 연결되지 않았음을 명시한다.
- [x] 실제 테스트 수·컴파일·환경 한계를 기록한다. 시작 사용자 삭제와 런타임 외 참조 변경이 없는지 확인한다.
- [x] 독립 코드 리뷰 후 필요한 수정과 검증을 수행한다. 기존 계획/설계 문서는 승인 방향과 다음 단위를 알 수 있게 갱신한다.

## Execution ledger

- Ruling: 사용자가 승인 설계의 다음 단계 진행을 지시했으므로 이 기반 단위는 현재 세션에서 직접 구현한다. 이미 작업 중인 전용 브랜치와 사용자 삭제를 보존하기 위해 체크아웃/새 worktree를 만들지 않는다. 격리는 Unity 실행 복사본에 적용한다.
- Ruling: 전체 출시 설계에는 여러 릴리스 단위가 있으므로 이번에는 다운로드 기반만 완료한다. 후속에 호환 릴리스/카탈로그/동의 UI와 게임 공급 경로를 연결한다. 운영 미완성을 숨기지 않는다.
- 사전 인터페이스 확인: Task 2의 두 IEnumerator API를 Task 1/3 테스트가 소비한다. 주소별 메모리 로딩과 라벨별 디스크 다운로드 레지스트리는 공유하지 않는다. Clear에서만 기존 세대·수명을 연결한다.

## 완료 기록

- Task 1: complete — 변경 전 실제 Unity PlayMode 9/9 실패, 모두 새 API 부재 단언. 격리 fixture 환경의 초기 공급자 등록 문제는 검증 경계에서 수정했다.
- Task 2: complete — 두 공개 API, 진행 중 요청 공유, Clear와 핸들 소유권 구현. 기존 ResourceManager.cs는 partial 선언과 ClearDownloads 호출만 추가했다.
- Task 3: complete — 실제 loopback HTTP/표준 공급자/실제 Unity 번들 캐시. 최종 PlayMode 14/14 통과, 실패·생략 0. 원본 코드 7개 어셈블리 컴파일 변경 전·후 exit 0. Android Player 응답 파일이 없어 Android 컴파일/빌드로 확대 해석하지 않는다.
- Task 4: complete — 사용 계약·도구 README·Handoff 갱신. 독립 리뷰의 전송 중 취소 누락을 보완해 실제 0 < received < total에서 취소와 새 요청 완료를 확인했다. 예상 404 이외 예외를 숨기지 않도록 변경했고 대기자 이탈 후 소유권 플래그·실제 작업 핸들 무효화도 확인했다.
- Evidence: .utmp/remote-download/red.xml (0/9), final.xml (14/14), baseline-compile.log 및 final-compile.log. 로그/XML은 임시 증거이며 영속 계약·검증 절차는 코드/테스트/README에 보존했다.
- 제한: 원본 전체 PlayMode/씬 테스트, Android APK/AAB/기기, 실제 앱 프로세스 재실행, 운영 CDN, 호환 릴리스/카탈로그·동의 UI·게임 에셋 이전은 미검증 또는 후속 구현이다.
- Ruling: 커밋·병합·배포는 요청 범위가 아니므로 현재 브랜치의 변경으로 남긴다. 초기 사용자 씬 삭제 4개를 보존했다. 새 운영 계정·요금·출시 수치를 확정하지 않았다.
