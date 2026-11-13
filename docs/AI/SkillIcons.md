# 스킬 카드 아이콘

- 원본은 `Document/스킬_개요.pptx`의 6장(약공격 10001~10021), 7장(강공격 20001~20015), 8장(전략 30001~30010)이다. 각 그림 옆의 이름/ID와 게임 데이터를 대응시켜 확인했다.
- 실제 표시 파일은 `Assets/Resources/SkillIcons/Skill_<ID>.png`다. PPT의 테두리를 포함한 64×64 PNG와 bytes가 동일하다. 기존 `Assets/2.Resources/Art/SkillIcons/Skill_<ID>.png`와 meta를 함께 이동해 GUID와 기존 참조를 보존했다. 별도의 `SkillIcon_<ID>.png` 및 등급 테두리 파일은 원래 위치에 유지한다.
- 카드 선택 화면(`CombatPopupController`)과 보유 목록(`PlayerStatusPanel`)은 `SkillCardIcons.Get`을 공유한다. 명시된 Icon/IconLabel의 기존 Managers.Resource 캐시 → 해당 키의 Resources → 스킬 ID에 대응하는 PPT 그림 순서로 찾는다. Icon이 비어 있는 현재 JSON도 기본 그림을 표시하므로 재변환이나 Addressables 사전 로딩이 필요하지 않다. 개별 아이콘이 없으면 null로 남겨 기존 숨김 동작을 사용한다.
- Resources 로딩은 Sprite Single 임포트와 `SkillIcons/Skill_<ID>` 이름을 전제로 한다. PNG/meta 변경 시 GUID를 유지한다. 스탯 카드는 PPT에 대응 그림이 없어 이번 연결 대상이 아니다.
- 자동 검증: 저장소 루트에서 `tools/SkillIconValidation/Run.ps1`. C# 실제 조회 도우미를 Unity 경계 대역으로 검사하고, 46개 PNG의 PPT 원본 일치·슬라이드 이름/ID 대응·Sprite 설정을 확인한다. 이 검증은 실제 Unity 임포트/렌더링을 대체하지 않는다. `SkillCardIconTests`는 실제 Resources와 두 UI 연결을 검사하는 Unity 테스트 소스다.
- 화면 확인: Play 종료 → 임포트/컴파일 완료 → MainScene Play → CombatUI의 CombatPopupController ⋮ → `테스트/원하는 카드 고르기` → 화염 부채/용암 분출/재생 치유 카드 열기 → PPT와 같은 그림 확인 → 획득 후 일시정지의 보유 목록에서 같은 그림 확인. 안 보이면 Console 첫 오류, Resources 폴더의 해당 PNG 임포트(Sprite Single), 테스트 스킬 ID 및 컴파일 완료 여부를 확인한다.
