# 플레이어 레벨·경험치와 카드 자동 연결

**목표:** 승인한 초과 XP 이월 방식으로 Lv1~100 성장을 적용하고, 기존 카드 선택을 레벨마다 자동 실행한다. 스탯 제목 오른쪽에 검정색 `Lv.1`을 표시한다.

**명세:** `docs/VampireSurvivor_18min_LevelSpawnXP_Codex_Spec_v0.1.md`, `docs/AI/NextCodex-LevelSpawnXP.md`, 사용자 승인 대화.

**구조:** PlayerData가 순수 C# PlayerExperienceProgress를 소유한다. 직렬화 가능한 PlayerExperienceSettings에는 공식 계수만 두고 현재 레벨·XP·선택 대기는 런타임에만 보관한다. CombatPopupController가 기존 카드와 전투 정지를 연결한다. 전역 매니저·새 asmdef·씬 변경은 필요하지 않다.

**방식 비교:** PlayerData에 모든 계산을 넣는 방식보다 순수 계산 분리를 선택했다. 수치 검증과 후속 성장 보정 연결을 독립적으로 확인할 수 있다. UI와 계산을 한 컴포넌트로 합치는 방식은 책임 중복 때문에 제외한다.

## 작업과 검증

- [x] 독립 검증에 기존 PlayerData 실제 소스를 연결하고, 요구량/이월/다중 레벨/상한/선택 대기/초기화/잘못된 입력 테스트 실패를 확인한다.
- [x] PlayerExperienceSettings.RequiredXP(int), PlayerExperienceProgress.Add(double), CompleteLevelUp(), Reset()을 구현한다. 정수 공식 기본값과 합계33,033을 검증한다.
- [x] PlayerData의 AddEXP/SetEXP/EXPChanged 시그니처를 보존한다. maxEXP는 현재 레벨 요구량의 호환 표시 필드로 계산한다. SetEXP는 현재 레벨 XP를 지정하며 요구량 초과 시 정상 성장 처리한다. maximum 인자는 유효성 검사는 유지하되 공식 요구량을 대체하지 않는다.
- [x] 레벨 변경 통지와 대기 선택을 연결한다. 카드 선택/소진/가득 참 완료마다 한 건만 차감하며 다음 카드를 같은 열린 팝업에서 갱신한다. 일시정지 중첩·사망·비활성/재활성·씬 종료·수동 미리보기를 검토한다.
- [x] PlayerStatusPanel 제목 영역을 나누어 검정색 레벨을 표시하고 기존 EXPChanged 갱신 경로를 사용한다.
- [x] 독립 검증, 기존 장착/전투 표 검증, Unity 설치본 컴파일을 실행한다. 가능한 Unity 수명 검증을 실행하고 실제 화면 검증과 구분한다.
- [x] Handoff에 실제 결과와 사용자 확인 절차를 기록한다. 커밋/PR은 하지 않는다.

## 판단 기록

- 사용자가 승인한 현재 브랜치/작업 경로를 유지한다. 시작 상태 feature/player-growth-design / 5d34dad, 작업 트리 깨끗함.
- 최대 레벨에서 남는 XP는 보존하고 추가 선택은 만들지 않는다. 별도 보상은 없다. 요구량0에 대한 기존 Slider 표시를 유지한다.
- 비활성화는 구독과 창만 정리하고 미완료 선택은 PlayerData에 보존한다. 사망/승패 종료에는 대기 선택을 취소한다.
- 보석 풀링·시간·스폰·성장 보정은 이번 두 단계 이후 작업이다.
