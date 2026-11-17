# 리소스 로딩 사용 방법

## 2026-11-17 다운로드 전용 기반 API

`ResourceManager.GetDownloadSizeAsync(string label, Action<long, string> completed)`와 `DownloadDependenciesAsync(string label, Action<long, long> progress, Action<bool, string> completed)`를 추가했다. 기존 메모리 로딩 API는 그대로다.

사용 순서는 **카탈로그 준비 → 추가 용량 조회 → 사용자 다운로드 동의 → 다운로드 → 필요한 에셋 PreloadAsync → 사용**이다. 이번 구현은 두 다운로드 API까지이며, 카탈로그 선택·동의 UI·게임 진입 연결은 아직 하지 않았다. 현재 LoadScene의 로컬 플레이는 그대로다.

```csharp
long requiredBytes = -1;
string sizeError = null;
yield return Managers.Resource.GetDownloadSizeAsync(label, (bytes, error) =>
{
    requiredBytes = bytes;
    sizeError = error;
});
if (sizeError != null)
    yield break;

// requiredBytes가 양수면 이 위치에서 UI 동의를 받아야 한다.
// UI 결정 없이 자동 다운로드하도록 그대로 복사하지 않는다.
// 동의를 받았거나 requiredBytes == 0일 때 아래를 호출한다.
bool downloaded = false;
yield return Managers.Resource.DownloadDependenciesAsync(label,
    (receivedBytes, totalBytes) => { /* 바이트 진행 표시 */ },
    (success, error) => { downloaded = success; });
if (!downloaded)
    yield break;

// 다운로드는 디스크 캐시만 준비한다. 사용할 에셋은 별도로 메모리에 로딩한다.
yield return Managers.Resource.PreloadAsync(label, (success, error) => { /* 진입 여부 판단 */ });
```

- 용량 조회 성공은 추가 바이트 수(캐시가 있으면 0), 실패는 -1과 오류다. 빈 라벨과 대상 없는 라벨을 0바이트 성공으로 처리하지 않는다.
- 다운로드 진행은 받은/전체 바이트다. 초기 대상 확인 중에는 0/0일 수 있다. 성공 여부는 진행 수치가 아니라 완료 콜백으로 판단한다.
- 같은 라벨·같은 종류의 진행 중 요청은 작업을 공유하며, 각 코루틴 대기자는 자신의 진행·완료를 받는다. 조회가 끝난 뒤 새 조회는 캐시 상태를 다시 계산한다. 실패 결과를 보관하지 않는다.
- 코루틴 대기자가 사라져도 이미 시작한 작업의 완료 콜백에서 핸들을 정리한다. 파괴된 대기자에게 사용자 완료 콜백을 보장하지 않는다.
- `ResourceManager.Clear`는 다운로드 요청도 실패로 종료하고 이전 세대 핸들을 정리한다. 네트워크 전송 자체가 즉시 중단된다고 보장하지 않으며, 늦은 완료가 새 요청을 제거하지 못하게 한다. 이미 내려받은 디스크 캐시는 삭제하지 않는다.
- 기존 `Managers.Clear`는 씬 이동용이며 Resource 캐시를 보존한다. 다운로드 전체 취소 API로 대신 사용하지 않는다.
- 새 구현은 `ResourceManager.Downloads.cs` partial에 있고 새 전역 매니저나 asmdef 이동은 없다. API는 이미 등록된 카탈로그/라벨을 대상으로 하며 원격 서버나 카탈로그를 자동 생성하지 않는다.

검증 실행과 제한은 `tools/RemoteContentValidation/README.md`를 참조한다. Android 출시용 원격 패치 전체가 완료된 상태는 아니다.

이번 단위의 실제 검증: 변경 전 새 API 부재로 PlayMode 9/9 실패 → 구현·독립 리뷰 보완 후 **14/14 통과, 실패·생략 0**. Windows Editor에서 실제 HTTP 번들·바이트 진행·캐시·전송 중 Clear와 새 요청·이탈 후 작업 핸들 반환을 확인했다. 원본 C# 7개 어셈블리 컴파일도 변경 전·후 exit 0이다. 원본 전체 씬/PlayMode 테스트, Android APK/AAB·기기·운영 CDN·실제 앱 재실행은 이번 결과에 포함하지 않는다.

## 진입과 로딩

게임 시작은 기존처럼 `Managers.PrepareGame`을 사용한다. 이 코루틴이 호출하는 Resource.PreloadAsync와 기존 LoadAllAsync는 같은 처리 경로를 사용한다.

별도 라벨을 로딩할 때는 성공 여부를 받는 overload를 사용한다.

```csharp
Managers.Resource.LoadAllAsync<UnityEngine.Object>(
    "사용할_라벨",
    (key, count, total) => { /* 진행 표시: 처리된 개수 / 전체 개수 */ },
    (success, error) =>
    {
        if (!success)
        {
            Debug.LogError(error);
            return;
        }
        // 이 요청의 모든 에셋이 준비된 뒤 실행할 작업
    });
```

기존 `(label, progress)` 호출은 유지된다. 이 형태의 실패는 Console에 기록한다. 진행 수는 성공 수가 아니라 처리된 수이므로, 게임 진행 여부는 새 overload의 success 또는 PreloadAsync의 완료 결과로 판단한다. 빈 라벨이나 결과는 진행 `(null, 0, 0)`을 알린 뒤 실패로 완료한다.

## 캐시와 수명

- 같은 주소·파일·공급자·타입·의존 리소스의 동시 요청은 하나의 에셋 로딩을 공유한다. 각 요청의 진행 및 완료 알림은 따로 유지한다.
- 실패한 결과는 캐시하지 않으며 재호출로 재시도한다. 캐시를 사용한 요청도 진행과 완료 알림을 받는다.
- 주소가 같지만 실제 위치·타입·의존 리소스가 다르면 실패한다. Sprite와 Texture의 우선 선택은 같은 원본과 의존성을 가리킬 때만 적용한다.
- 라벨 조회 핸들은 각 배치가 취득한 참조를 해제한다. 에셋 핸들은 진행 중 요청에서 성공한 캐시로 소유권을 이전하고 Clear에서 해제한다.
- Clear는 진행 중 요청도 실패로 종료한다. 늦게 완료된 이전 작업은 캐시를 다시 채우지 못한다. 정리 콜백에서 새로 시작한 요청은 유지한다.
- 한 호출자의 콜백 예외는 Console에 기록하며 다른 요청의 알림을 막지 않는다.

## 2026-10-28 검증

- Unity 6000.5.0f1 C# 컴파일 및 PlayMode **36/36 통과**, 생략 0. 이전 단계 21개 포함.
- 수정 전 31개 중 8개 실패: 중복 키 예외, 동시 Clear 시 해제 누락, 캐시 충돌, 빈 결과/완료 API 처리 등을 재현했다.
- 독립 리뷰 후 의존 리소스 변경과 무관한 Sprite/Texture 충돌을 테스트 3개로 재현하고 수정했다. 일반 타입 필터 및 같은 원본 Sprite 우선 선택도 검증했다.
- 결과와 로그: `.utmp/ResourceLoading/final.xml`, `final.log`.
- 원본 에디터가 열려 있어 짧은 임시 경로의 검증 복사본을 사용했다. 위치는 `.utmp/ResourceLoading/validation-path.txt`에 있다.
- 검증 복사본의 PlayFromTitle에만 배치 실행 guard를 추가했다. 사용자의 전역 자동 타이틀 옵션과 원본 편집기 스크립트는 변경하지 않았다.
- 사용자 GiungTestScene, meta, ProjectSettings.asset의 SHA-256은 이 단계 시작 전과 같다.

실제 운영 카탈로그·번들 및 Android 빌드/기기는 검증하지 않았다. 독립 리뷰의 추가 경계 테스트 제안 중 별칭 주소의 참조 수, Clear 직후 같은 주소 재요청, 완료 콜백 예외, 조회 핸들별 참조 수 직접 계측은 후속 검증으로 남긴다. 현재 테스트는 실제 Addressables와 메모리 공급자로 수행했으며 해당 추가 조건까지 검증했다고 주장하지 않는다.
