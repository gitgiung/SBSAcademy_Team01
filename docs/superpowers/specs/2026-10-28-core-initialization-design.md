# Core 초기화와 로딩 흐름

## 목적과 승인된 기준

기존 Managers 구조에서 Core 준비와 게임 데이터 준비를 구분한다. 사용자는 2026-10-28에 빈 테스트 씬 진입을 허용하는 방향을 선택했다. 준비 실패를 성공으로 표시하거나 실패한 상태에서 게임 씬으로 이동하지 않는다.

## 접근 방식

- 기존 Managers와 LoadScene을 보완한다. 현재 호출부와 씬을 유지할 수 있어 선택했다.
- 별도 부트스트랩 객체를 도입하는 대안은 초기화 책임이 독립되지만 현재 범위에 비해 구조 변경이 크므로 채택하지 않는다.

## 동작

- Initialized는 Core 초기화 완료만 뜻하며 외부에서 변경할 수 없다. 풀과 사운드를 한 번 준비한다.
- PreparationState는 NotStarted, Preparing, TestReady, Ready, Failed로 구분한다. GameDataReady는 Ready일 때만 true다.
- PrepareGame 코루틴은 Core 준비 → 요청된 Addressables 라벨 로딩 → SkillData 초기화 순서로 동작한다.
- LoadScene의 선택적 preloadLabel은 기본 빈 문자열, requireSkillData는 기본 false다. 빈 설정은 테스트 모드로 진입하고 TestReady를 표시한다. 데이터를 만들어 넣거나 Addressables 설정을 임의로 채우지 않는다.
- 명시적으로 요청한 라벨이 없거나 그 리소스 로딩이 실패하면 Failed로 끝낸다. SkillData가 있으면 검증해 사용하며, 없고 requireSkillData가 false이면 TestReady다. 필수 데이터 누락 및 잘못된 JSON/중복 ID는 실패다.
- 반복 Core 초기화는 중복 객체를 만들지 않는다. 같은 준비 요청을 동시에 호출하면 진행 중 작업을 기다린다. 다른 설정의 동시 요청은 명시적 오류로 거절한다.
- 비동기 준비 작업은 영속 Managers가 소유한다. 기다리던 로딩 씬이 파괴되어도 공유 작업은 완료되어 이후 요청이 영구 대기하지 않는다. Managers 자체가 파괴되면 진행 중 핸들과 캐시를 해제한다.
- 준비 후 리소스 캐시를 정리하거나 데이터 재초기화가 실패하면 Ready를 무효화하고 NotStarted를 반환한다.
- 사운드 루트는 Managers의 자식으로 소유하며 기존 루트도 필요한 AudioSource를 연결한다.
- 준비 실패 시 원인을 기록하고 씬 전환을 중단한다. 성공 시 현재 빌드 목록의 MainTestScene으로 이동한다.
- 새 리소스 준비 코루틴은 조회 및 에셋 핸들의 소유권을 관리하고 실패한 에셋은 캐시하지 않는다. 기존 LoadAllAsync API의 전체 개편은 다음 단계다.

## 제약과 검증

- Unity 6000.5.0f1, 기존 Addressables 2.9.1 및 Newtonsoft.Json 사용. 패키지 추가 없음.
- HP/MP 게임 규칙 및 미정인 기획은 변경하지 않는다.
- 기존 씬, 프리팹 및 사용자의 GiungTestScene 변경은 수정하지 않는다.
- 풀, UI의 기존 기능과 전역 접근 API는 유지한다.
- Core 재초기화, 사운드 사용, 빈 테스트 준비, 필수 데이터 누락, 데이터 검증 실패, 잘못된 라벨, 재시도 및 씬 전환 전 실패 차단을 Unity 테스트로 검증한다.
- 데이터가 없으므로 실제 게임 데이터 전체의 Ready 및 Android 기기 검증은 테스트용 fixture와 별도로 구분해 보고한다.
