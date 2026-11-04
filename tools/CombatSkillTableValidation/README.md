# 전투 스킬 변환 검증

.NET SDK 9가 필요합니다. 외부 NuGet 패키지를 사용하지 않습니다.

```powershell
./tools/CombatSkillTableValidation/Run.ps1
./tools/CombatSkillTableValidation/Run.ps1 -Export
```

실제 `CombatSkillWorkbook.Read`를 컴파일하여 루트 `SkillDatatable.xlsx`를 읽습니다. 기존 46개 수치 보존, 회복 효과·대상·지속 시간, 슈퍼아머·넉백, 빈 연출 키, 독립 반경·길이, 인라인 문자열·절대 관계 경로를 검사합니다. `-Export`는 전체 검증 통과 후 실제 변환기 Export를 호출하여 `Assets/Resources/Data/CombatSkillTable.json`을 갱신합니다. 엑셀을 바꿔 기존 JSON과 수치가 달라지면 전수 비교가 실패하므로, 정상적인 데이터 변경은 Unity 메뉴로 먼저 변환한 뒤 검증하세요.

`UnityBoundary.cs`는 콘솔 실행에 필요한 최소 API 경계입니다. JSON 직렬화는 System.Text.Json이며, Unity의 JsonUtility·AssetDatabase·Resources 및 에디터 메뉴 동작을 검증하지 않습니다. 출력과 임시 엑셀은 Git 무시 경로 `.utmp/CombatSkillData`에 생성합니다. Assets 안에 테스트 어셈블리를 추가하지 않습니다.
