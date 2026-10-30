# 리소스 로딩 사용 방법

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
