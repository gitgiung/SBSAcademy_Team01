# 일시정지 창 약공격 장착 연결

**Goal:** 보유 약공격 클릭으로 약공격 버튼에 장착/해제하고, 미장착 시 입력과 MP 소모가 발생하지 않게 한다.

**Architecture:** PlayerLoadout이 장착 ID와 해제를 소유한다. PlayerStatusPanel은 보유 약공격 클릭을 장착/해제로 전달하고 Changed로 표시를 갱신한다. Player의 버튼/키보드 시전은 동일한 CombatSkillTable 경로를 사용한다.

**범위:** 사용자 확인에 따라 약공격만 실행 연결한다. 기존 TryEquip의 약/강/전략 분류 제한을 보존해 향후 강공격은 Heavy 버튼에만 연결한다. 자동 패시브, 강공격 효과와 차징 규칙, 씬/프리팹/직렬화 필드는 변경하지 않는다.

**방식 판단:** 기존 PlayerLoadout과 상태창을 확장한다. 별도 장착 상태나 새 매니저는 중복 상태를 만들므로 추가하지 않는다. 버튼의 구형 SkillData 실행 대신 이미 구현된 표 기반 약공격을 공통 사용한다.

- [x] Assets 밖 독립 검증으로 장착/교체/해제, 보유 유지, 분류 제한, 알림, 잠금 슬롯을 확인하고 해제 기능 부재로 실패를 확인한다.
- [x] PlayerLoadout.TryUnequip(PlayerSkillButton)를 추가한다.
- [x] PlayerStatusPanel의 약공격 셀만 클릭 가능하게 하고 장착 표시/안내를 갱신한다. 강공격은 클릭 불가로 유지한다.
- [x] Player.TryUseLightSkill을 TryCastLightSkill로 연결하고 기본 10001 대체 발사를 제거한다. 미장착은 비용 차감 이전에 거절한다.
- [x] 독립 검증 4/4, 기존 변환기 검증 7/7, Unity Roslyn 런타임/에디터 컴파일 종료 코드 0, diff 검사를 실행했다. 플레이/UI는 미실행이다.
- [x] Handoff에 실제 결과와 직접 확인 순서를 기록했다.

**Review Focus:** 빈/잠금 셀 클릭, 팝업 반복 열기 시 리스너 중복, 장착 교체 후 이전 스킬 잔류, 미장착 MP 소모, 강공격의 약공격 버튼 오할당.
