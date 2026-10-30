# Core 초기화와 게임 준비

## 시작 경로

`TitleTestScene → LoadTestScene → Managers.PrepareGame → MainTestScene`

`Managers.Init()`은 풀과 사운드를 한 번 초기화한다. `Initialized`는 공통 서비스 준비만 의미하며, 게임 데이터의 준비 여부는 `GameDataReady`로 확인한다.

| PreparationState | 의미 | 로딩 씬 진입 판단 |
|---|---|---|
| NotStarted | 미준비 또는 준비 후 의존성 무효화 | 대기/다시 준비 |
| Preparing | 요청한 리소스와 데이터 준비 중 | 대기 |
| TestReady | 스킬 데이터 없이 테스트 가능한 상태 | 진입 허용 |
| Ready | 요청한 리소스와 SkillData 준비 완료 | 진입 허용 |
| Failed | 요청한 리소스 또는 데이터 준비 실패 | 진입 중단 |

`Ready`는 현재 등록된 SkillData에 대한 준비 결과다. 아직 작성되지 않은 유물·증강·퀘스트 데이터 전체의 완성을 뜻하지 않는다.

## 현재 기본 설정

LoadScene의 `preloadLabel`은 빈 문자열, `requireSkillData`는 false다. 기존 씬 파일을 수정하지 않고도 이 기본값이 적용되며, 데이터가 없는 현재 테스트 씬은 TestReady로 시작한다. 데이터 누락을 숨기기 위한 임시 JSON이나 가짜 게임 데이터는 추가하지 않았다.

실제 데이터를 연결할 때는:

1. 스킬 JSON을 Addressables에 주소 `SkillData`로 등록하고 준비용 라벨을 지정한다.
2. LoadTestScene의 LoadScene 컴포넌트에서 `preloadLabel`에 그 라벨을 지정한다.
3. 실제 플레이에 스킬 데이터가 필수라면 `requireSkillData`를 true로 설정한다.

JSON 예시 구조는 `{ "skills": [{ "DataId": 101, "Name": "..." }] }`다. 실제 값과 추가 필드는 게임 기획과 `Data.SkillData`에 맞춰 작성한다. 목록 비어 있음, null 항목, 중복 ID, JSON 오류는 실패로 처리한다.

## 호출과 실패

코루틴에서 `yield return Managers.PrepareGame(label, requireSkillData);`를 호출하고 `PreparationState`를 확인한다. 실패 원인은 `PreparationError`로 확인한다. LoadScene은 실패를 Console에 기록한 뒤 MainTestScene으로 이동하지 않는다.

준비 작업은 Managers가 소유한다. 같은 설정의 동시 요청은 같은 작업을 기다리며, 서로 다른 설정의 동시 요청은 거절한다. 대기하던 객체가 사라져도 공유 준비는 끝까지 진행한다. 실패 후 다시 호출하면 재시도한다.

리소스 캐시가 정리되거나 Data.Init이 실패하면 GameDataReady가 false로 돌아간다. Managers 파괴 시 풀과 준비 경로의 진행 중/캐시 핸들을 해제한다. 사운드 루트도 Managers 자식으로 함께 파괴된다.

## 다음 단계

- 리소스 로딩 API 통합은 2026-10-28 2단계에서 완료했다. 호출 계약과 검증은 `docs/AI/ResourceLoading.md`를 참조한다.
- 팝업 스택, 씬별 정리 계약 및 실제 게임 진행 연결.
- 실제 게임 데이터와 Android 기기 검증.

## 2026-10-28 검증

- Unity 6000.5.0f1에서 C# 컴파일 성공, 전체 PlayMode 테스트 21/21 통과.
- 수정 전 사운드 테스트 0/4, Core 수정 후 4/4 통과.
- 준비 API 추가 전 새 테스트 9개 실패, 구현 후 준비 테스트 포함 13/13 통과.
- 로딩 씬 연결 전 새 테스트 2개 실패, 연결 후 17/17 통과.
- 독립 리뷰에서 확인한 호출자 파괴 및 Ready 무효화 문제를 테스트 3개로 재현하고 수정 후 전체 21/21 통과.
- 실제 LoadTestScene에서 MainTestScene 진입, 테스트 데이터 정상 로딩, 실패 시 전환 차단, 진행 중 캐시 정리, 반복 초기화와 핸들 해제 검증.
- 결과: `.utmp/CoreInitialization/final-playmode.xml`, `.utmp/CoreInitialization/final-playmode.log` (Git 제외).
- 사용자 수정 GiungTestScene 및 meta의 SHA-256은 작업 전과 동일. 테스트가 다시 저장한 ProjectSettings.asset은 복원했다.
- 실제 운영용 전체 데이터, Android 빌드 및 기기는 검증하지 않았다.
