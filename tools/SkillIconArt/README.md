# 전투 스킬 아이콘

`bake.mjs`는 전투 표의 76개 스킬을 각각 명시한 문양으로 그린다. 외부 이미지나 AI 생성 이미지가 아닌 수정 가능한 SVG 도형·그라데이션을 코드에서 만들고 PNG로 렌더링한다. 불꽃·물결·얼음·바람·암석·전기와 전략 역할을 구분하며, 등급 테두리와 강공격 상단 마름모를 함께 표시한다. 수치나 전투 동작은 바꾸지 않는다.

## 다시 만들기

프로젝트 루트에서 실행한다. Unity 임포트가 끝난 상태에서 실행하는 것이 좋다.

```powershell
npm install --prefix .utmp/skill-icons-runtime --no-audit --no-fund sharp@0.35.5
node tools/SkillIconArt/bake.mjs
node tools/SkillIconArt/bake.mjs --check
& tools/SkillIconValidation/Run.ps1
```

- `Assets/Resources/SkillIcons/Skill_<ID>.png`: 256×256 카드·장착 UI 기본 아이콘.
- `Assets/2.Resources/Art/SkillIcons/SkillIcon_<ID>.png`: 256×256 투명 배경 문양. 기존 명시 참조도 같은 디자인을 사용한다.
- `.meta`는 수정하지 않는다. 기존 GUID·Sprite Single·Point·PPU를 유지한다.
- `--check`는 에셋을 수정하지 않고 현재 소스의 출력과 152개 PNG를 바이트 단위로 비교한다. PNG 결과는 sharp/libvips 버전에 영향을 받을 수 있다.
- 전체 미리보기와 ID·이름·문양 목록은 `.utmp/skill-icons/`에 생성된다. 이 폴더는 Git에 추가하지 않는다.
- 이전 `tools/WaterSkillIcons/bake.mjs`는 구형 32px 물 문양을 다시 쓰므로 이 세트에는 실행하지 않는다. 물 스킬도 이 도구에서 관리한다.
- 테스트는 실제 `SkillCardIcons.cs`를 최소 Unity 대역과 함께 컴파일하여 로딩 우선순위·76개 fallback 경로를 확인한다. 실제 Unity 임포트·Play 테스트를 대신하지 않는다.

MainScene에서 `Tools > Play From Title` 해제 → Play → `CombatUI`의 `CombatPopupController` 컴포넌트 메뉴 `테스트/원하는 카드 고르기`를 사용한다. 카드 선택 화면과 장착 후 상태창에서 같은 그림이 보여야 한다. 그림이 누락되면 Console 첫 오류와 해당 PNG의 Sprite 임포트 설정을 확인한다.
