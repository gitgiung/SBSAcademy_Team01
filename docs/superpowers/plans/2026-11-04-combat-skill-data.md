# 전투 스킬 효과 데이터 연결 계획

**Goal:** 기존 엑셀 변환기에 실행에 필요한 효과·대상·연출 필드와 범위 반경·길이를 추가한다.

**Architecture:** `CombatSkillRecord`의 기존 필드와 약공격 계산을 유지하고 필드만 추가한다. `CombatSkillWorkbook.Read`가 기존 세 시트와 범위 시트를 계속 ID로 합친다. 새 전역 시스템과 패키지는 만들지 않는다.

**Tech Stack:** Unity 6000.5.0f1, C#, OOXML/ZipArchive. 검증은 Assets 밖의 .NET 9 콘솔에서 실제 파서를 실행한다.

## 범위와 결정

- 사용자의 다음 구현 승인에 따라 기존 변환기 확장부터 진행한다.
- 엑셀 원본, 씬, 프리팹, GUID, 차징 수치 및 피해 공식을 변경하지 않는다.
- `Assets/Tests` 삭제 결정은 유지한다. 독립 검증 도구는 Unity 프로젝트 컴파일에 참여하지 않는다.
- 신규 문자열은 빈 문자열, None 숫자는 0, None 슈퍼아머는 false로 변환한다.
- Range의 기존 의미를 보존하고 Radius·Length를 별도로 추가한다.
- 강공격 차징 단계 배율·시간 충돌, 전략 중첩 정책과 실제 시전 연결은 후속 작업이다.

## 작업과 확인

- [x] 실제 엑셀 회복 효과·넉백·슈퍼아머·범위 및 기존 약공격 수치를 읽는 독립 검증 작성. 확장 전 6개 중 5개 실패 확인.
- [x] CombatSkillRecord 및 변환기 확장. 인라인 문자열과 절대 워크북 관계 경로도 지원한다.
- [x] 검증 전체 7/7 통과, 실제 46개 JSON 재생성, HEAD JSON의 기존 필드 전수 비교. 추가 필드 644개를 엑셀과 독립 대조.
- [x] Unity 설치본 Roslyn으로 런타임·에디터 어셈블리 각각 컴파일 종료 코드 0. 에디터/Library DLL은 덮어쓰지 않았다.
- [x] 독립 리뷰의 csproj Git 무시 문제를 해당 파일만 예외 처리하고 diff 확인 및 Handoff 갱신.

## 검토 초점

전략 EffectType과 EffectDuration 유실, boolean 1/0/None 해석, Circle의 사거리와 반경 혼동, Excel 재저장 시 inlineStr 유실, 기존 약공격 값 변경을 검증한다. Unity 메뉴 호출·플레이·빌드·기기는 별도 확인으로 남긴다.
