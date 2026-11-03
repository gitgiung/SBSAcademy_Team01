# Pause Status and Player Loadout Implementation Plan

> **For agentic workers:** Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 참고 이미지 기반 일시정지 상태창에서 실제 스탯·보유 스킬·유물·증강을 조회하고 음량을 조절한다.

**Architecture:** PlayerData가 플레이어 한 명의 직렬화 가능한 PlayerLoadout을 소유한다. 기존 Data.SkillData를 재사용하고 공격8칸(기본6), 전략5칸, 유물/증강 각60개 및 버튼당 하나의 스킬 ID 장착을 관리한다. CombatPopupController가 기존 pausePopup의 빈 ContentRoot에 상태창을 생성하고 기존 정지/뒤로가기 계약을 유지한다. 음량은 기존 SoundManager에 연결하며 옵션은 동일 팝업 내부 패널로 표시한다.

**Tech Stack:** Unity 6000.5.0f1, 기존 uGUI/TMP, 기존 UIManager/CombatPopupController/SoundManager, NUnit PlayMode.

## Global Constraints

- 첫 사진의 남색 패널·크림 스탯·분홍/살구/초록/하늘 제목·8/5 슬롯·가로 스크롤·우하단 뒤로가기 배치를 따른다. 두 번째 사진은 영역 설명이다.
- 플레이어가 보유하지 않은 실제/가짜 스킬을 자동 지급하지 않는다. 기본 활성6+잠금2가 보인다. 스탯 중 모델이 없는 항목은 —로 표시한다.
- SkillDatatable.xlsx가 현재 존재하며 11약/5강/10전략 스킬 이름·등급·속성을 확인했다. 비용·효과·애니메이션·프리팹 대부분 미작성이다. 실행 계수·첫 스킬·장비를 임의 확정하지 않는다. 이번 구현은 보유/장착 및 조회 UI이고 개별 공격 실행은 후속이다.
- 버튼 장착은 약/강/전략 각각 보유한 같은 분류의 스킬 하나만 허용한다. 상태창의 슬롯은 조회용이며 클릭으로 몰래 장착/제거하지 않는다.
- 사용자 승인으로 옵션에 전체/BGM/효과음 음량을 넣는다. 기존 SoundManager 소스와 AudioListener를 사용하고 새 전역 매니저는 만들지 않는다. 앱 재시작 영구 저장 정책은 추가하지 않는다.
- 기존 씬·프리팹·GUID·팝업 스택/겹침·레벨업 콘텐츠를 보존한다. 런타임 생성 상태창은 기존 pause ContentRoot만 사용한다. 기존 뒤로가기 Button 참조/리스너는 유지한다.

## Review Focus

- 6칸 초과 획득은 해금 전 거절되고 8칸을 넘지 않는다.
- 보유하지 않은 ID·다른 분류 장착은 거절하고 제거 시 장착 참조를 정리한다.
- 옵션 복귀는 전투를 재개하지 않는다. 기존 뒤로가기만 팝업 수명 계약으로 닫는다.
- 재열기/소스 교체/파괴에서 데이터 이벤트 구독·UI 리스너가 중복되지 않는다.
- 소유 아이템 이름·미정 스탯·빈 슬롯을 가짜 획득/수치로 채우지 않는다.

## Task 1: 보유 및 장착 상태

**Files:** PlayerData.cs, 새 Assets/0.Scripts/Game/Player/PlayerLoadout.cs 및 meta.

**Interfaces:** PlayerData.Loadout. PlayerLoadout.GetAttackSkill(int),GetTacticalSkill(int),TryAddAttackSkill(Data.SkillData),TryAddTacticalSkill(Data.SkillData),UnlockAttackSlot(),TryEquip(PlayerSkillButton,int),RemoveSkill(int),GetEquippedSkillId(PlayerSkillButton),Changed. 기존 Data.SkillData에 별도 실행 스키마를 중복 생성하지 않는다. PlayerOwnedItem은 유물/증강의 보유 표시 데이터만 소유한다.

- [x] 빈 슬롯·6/8해금·전략5칸·중복/범위·장착·제거 테스트를 먼저 실패시킨다.
- [x] 타입/ID 기반 보유·장착/해금 모델을 구현하고 Changed로 UI에 알린다.

## Task 2: 상태창 및 옵션

**Files:** 새 Assets/0.Scripts/Game/UI/PlayerStatusPanel.cs, StatusPanelUI.cs 및 meta. Modify CombatPopupController.cs, UI_CombatPopup.cs, SoundManager.cs.

- [x] 상태창 8/5 슬롯·잠금·실제 데이터 갱신·아이템60 스크롤·옵션 중 timeScale 유지·음량 반영·반복 열기 테스트 작성 후 실패 확인.
- [x] 기존 pause ContentRoot/Panel/BackButton을 사용해 이미지 배치로 생성한다. 기존 한글 TMP 폰트를 사용한다. 새 테스트 픽스처만 샘플 보유 데이터를 주입한다.
- [x] 현재 씬의 Player를 명시 참조 또는 씬 범위 검색으로 연결한다. 소스가 없으면 빈 목록·— 스탯만 표시한다.
- [x] SoundManager에 전체/채널 음량 API 추가, 재초기화/씬 정리 후 현재 채널 설정을 보존한다. 옵션에서 상태창으로 복귀해도 같은 팝업을 유지한다.

## Task 3: 검증

- [x] 최소 소스 검증 프로젝트에서 새 테스트와 기존60개·팝업·사운드 회귀 실행.
- [x] 그래픽 사용 가능한 검증본에서 빈 상태/테스트 보유/옵션 화면을 PNG로 렌더해 직접 검토한다. 실제 운영 씬/기기와 구분한다.
- [x] 독립 리뷰·diff check·씬/프리팹 해시 보존·Handoff 갱신. 커밋/브랜치 전환 없음.
