# 전투 스킬 변환 검증

.NET SDK 9가 필요합니다. 외부 NuGet 패키지를 사용하지 않습니다.

```powershell
./tools/CombatSkillTableValidation/Run.ps1
./tools/CombatSkillTableValidation/Run.ps1 -Export
```

실제 `CombatSkillWorkbook.Read`를 컴파일하여 루트 `SkillDatatable.xlsx`를 읽습니다. 기존 46개 수치 보존, 회복 효과·대상·지속 시간, 슈퍼아머·넉백, 빈 연출 키, 독립 반경·길이, 인라인 문자열·절대 관계 경로를 검사합니다. `-Export`는 전체 검증 통과 후 실제 변환기 Export를 호출하여 `Assets/Resources/Data/CombatSkillTable.json`을 갱신합니다. 엑셀을 바꿔 기존 JSON과 수치가 달라지면 전수 비교가 실패하므로, 정상적인 데이터 변경은 Unity 메뉴로 먼저 변환한 뒤 검증하세요.

레벨업 추첨 검증도 포함합니다. 실제 Skill_Rarity 7개와 소수 가중치·색, 등급 우선 추첨 경계, 등급 안의 균등 선택, 같은 묶음 ID 중복 방지, 가중치 0·미등록 등급·범위 밖 강공격 제외, 후보 소진, Mystic/Mythic 호환, 툴팁 토큰 치환을 검사합니다. 실제 카드 표시·입력·획득 효과는 검증 범위가 아닙니다.

보유 등록은 실제 PlayerLoadout/SkillData와 LevelUpSkillReward로 검증합니다. 선택 획득·중복 거부·슬롯 가득 참·분류 제한·자동 장착하지 않음, 추첨→획득→다음 후보 제외→수동 Light 장착을 검사합니다. Unity 카드 클릭·일시정지 표시·안내/확인 버튼·시간 복원은 별도 플레이 확인이 필요합니다.

`UnityBoundary.cs`는 콘솔 실행에 필요한 최소 API 경계입니다. JSON 직렬화는 System.Text.Json이며, Unity의 JsonUtility·AssetDatabase·Resources 및 에디터 메뉴 동작을 검증하지 않습니다. 출력과 임시 엑셀은 Git 무시 경로 `.utmp/CombatSkillData`에 생성합니다. Assets 안에 테스트 어셈블리를 추가하지 않습니다.
