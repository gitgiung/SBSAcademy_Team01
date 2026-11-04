# 일시정지 상태창과 보유 데이터

## 직접 확인

1. Unity 컴파일 완료 후 Console을 확인한다. MainTestScene에서 직접 확인하려면 Tools > Play From Title 옵션을 끄고 Play한다.
2. 오른쪽 위 기존 일시정지 버튼을 누른다. 기존 CombatPausePopup 내부가 참고 이미지의 상태창으로 구성된다. 왼쪽 스탯, 공격 8칸(기본6·잠금2), 유물·증강 가로 목록, 전략5칸, 오른쪽 위 설정과 우하단 뒤로가기가 보인다.
3. 이동·공격·스폰이 멈추는지 확인한다. 뒤로가기를 누르면 기존 팝업 스택 규칙으로 닫히고 전투가 재개된다. 다른 전투 팝업이 아래에 있으면 계속 정지하는 것이 정상이다.
4. 톱니 버튼을 눌러 음량 설정으로 들어간다. 전체·배경 음악·효과음 슬라이더는 즉시 적용된다. 여기의 뒤로가기는 상태창으로 돌아갈 뿐 전투를 재개하지 않는다. 키보드/패드는 진입 시 전체 음량, 복귀 시 상태창 뒤로가기 버튼이 선택된다.
5. 상태창을 여러 번 열어도 UI와 리스너가 중복되지 않아야 한다. 기존 창을 외부에서 비활성화하거나 UI.Clear로 정리해도 이전 전투 정지 해제 계약을 따른다.
6. 실제 보유 데이터가 없으면 빈 칸으로 표시된다. 확인용 스킬을 자동 지급하지 않는다. Play 중 PlayerData의 Loadout > Attack Skills 첫 항목에 실제 표의 DataId=10001, Name=불꽃 탄환을 넣고 창을 다시 열면 해당 이름이 표시된다. 이 임시 설정은 Play 종료 후 저장하지 않는다. 실행/획득을 확인하는 절차는 아니다.
7. 유물·증강을 8개 넘게 획득하면 가로 드래그나 스크롤바로 뒤 항목을 확인한다. 각 목록은 실제 보유 데이터만 채우며 최대60개까지 지원한다.
8. 자동 검증은 Tools > Play From Title 비활성화 → Window > General > Test Runner → PlayMode → PlayerStatusTests → Run Selected.

오류가 있으면 Console 첫 오류와 테스트 이름을 확인한다. 창이 예전 모양이면 CombatUI의 CombatPopupController에 연결된 Pause Popup의 ContentRoot/CloseButton 참조를 확인한다. 데이터가 없으면 Controller의 Status Player를 같은 씬 Player로 지정하거나 해당 PlayerData.Loadout을 확인한다. 아이콘 주소가 아직 없거나 사전 로딩되지 않았으면 이름을 표시한다. 한글 글꼴은 기존 Assets/UI/Fonts의 NanumGothic Combat SDF를 사용한다.

## 데이터와 책임

- PlayerData.Loadout은 한 플레이어의 보유/장착 상태다. 새 전역 인벤토리 매니저는 없다.
- PlayerLoadout.TryAddAttackSkill(Data.SkillData): ID 10000~29999 범위의 보유 공격 스킬을 빈 활성 슬롯에 추가한다. 중복·잠금·초과 획득은 거절한다.
- UnlockAttackSlot(): 활성6→7→8 한 칸씩 해금한다. 유물·증강 효과 발동부가 호출할 API이며 조건이나 효과를 자동 생성하지 않는다.
- TryAddTacticalSkill(Data.SkillData): ID30000~39999의 전략 스킬을 최대5개까지 보유한다.
- TryEquip(PlayerSkillButton.Light/Heavy/Tactical, id): 보유한 같은 분류의 스킬 하나만 해당 버튼에 연결한다. 약10000대·강20000대·전략30000대는 실제 표의 분류다. RemoveSkill은 해당 장착 참조도 정리한다.
- TryAddRelic/ TryAddAugment(PlayerOwnedItem): 실제 획득한 유물/증강 이름·아이콘·ID를 각각 최대60개 목록에 추가한다. 고유 효과·합성·보상은 실행하지 않는다.
- Changed로 열린 상태창을 갱신한다. 닫힌 동안에는 구독을 해제하고 다시 열 때 현재 상태를 읽는다. HP/MP/EXP 이벤트는 실제 스탯 표시를 갱신한다.
- CombatPopupController는 기존 전투 정지를 소유한다. PlayerStatusPanel은 조회 UI와 내부 옵션 전환만 소유한다. 기존 CloseButton을 재사용하며 새로운 정지 관리자·팝업 스택을 만들지 않는다.
- SoundManager.MasterVolume은 AudioListener 전체 음량, SetVolume(Bgm/Effect)은 기존 채널 AudioSource 음량이다. 현재 Managers 수명/씬 재초기화 동안 채널 값을 유지한다. 앱 재시작 영구 저장은 추가하지 않았다.

## 미정 사항과 범위

스탯은 현재 모델의 HP/MP, 피해량, 이동속도, MP 회복량만 표시한다. 방어력·체력 재생·속성 피해 보정·쿨타임 보정·투사체 보정·방어막·획득 보정 등 모델이 없는 항목은 —로 남긴다. 그림 속 예시 숫자를 게임 수치로 사용하지 않는다.

이번 작업은 3단계 중 보유/장착 상태와 일시정지 조회·음량 옵션이다. 개별 패시브 발사와 액티브 버튼·약공격 애니메이션/타격은 아직 연결하지 않았다. 현재 기본 Bullet은 이전 경로로 발사된다. 상태창 슬롯은 조회용이며 클릭으로 임의 장착/제거하지 않는다.

SkillDatatable.xlsx의 약공격 10001~10021과 강공격 20001~20015 전투 수치가 채워져 있다. 2026-11-04에 강공격 20001~20005와 전략 30001~30010의 비용·효과 수치를 채웠다. 전략은 횟수 1, 쿨타임 6초, 스테이지당 5회이고 등급 칸은 비어 있다. 스킬 문양 46장은 `Assets/2.Resources/Art/SkillIcons/Skill_{아이디}.png`다. 약공격 10001~10021, 강공격 20001~20015, 전략 30001~30010. 약·강공격 파일에는 등급 테두리가 같이 들어 있고, 전략은 문양만이다. 레어도 테두리 7장과 `Skill_Rarity`의 테두리 경로·색은 그대로다. 스킬 표의 Skill_Icon, 애니메이션, 프리팹, 사운드는 비어 있다. 약·강·전략의 전투 수치는 `Tools > Parse Combat Skill Table`이 `Assets/Resources/Data/CombatSkillTable.json`으로 만들고, 약공격 시전만 그 수치를 읽는다. 상태창용 `Data.SkillData` 목록과는 별도다. 강공격 시트의 Cooldawn 오타는 헤더가 아니라 K열을 읽어 쿨타임으로 쓴다. FusionSkilltable은 LightSkilltable C9 태양 검(10007), C5 화염 강타, C8 플레어 탄환을 참조한다.

씬/프리팹을 재저장하지 않으며 빈 Pause ContentRoot에 런타임 UI를 한 번 구성한다. 따라서 비실행 상태의 원본 CombatUI 프리팹에는 예전 기본 패널이 보일 수 있다. 실제 표시와 검증 미리보기는 Play 상태 기준이다.
