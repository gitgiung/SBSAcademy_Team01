# 부활/게임 오버 프로토타입

- MainScene의 기존 GameManager가 RunEndPopup을 생성한다. 씬 YAML/기존 참조는 보존한다.
- 사망 → ReviveOffer에서 전투 정지 → 예: HP 100%·무소모 부활 / 광고 후 부활: 광고 없음 안내 → 확인: 같은 부활 / 아니요: Defeat 결과.
- 부활권 지급·경제·광고 횟수·광고 SDK·부활 무적·유물 자동 부활은 구현하지 않았다. 사용자 승인된 프로토타입 HP/무소모만 적용했다.
- 결과의 처치 수는 EnemySpawnDirector가 실제 기록한 **일반 몬스터** 누계다. 생존 시간은 보스 전투를 포함하며 팝업 정지는 제외한다. 점수/골드는 미정 표시이며 보상을 지급하지 않는다.
- 광고 제공자가 정해지면 RunEndPopup.ShowAdNotice 경로를 SDK 요청으로 교체하고, 보상 성공에서만 GameManager의 부활을 호출해야 한다. 현재 Confirm은 실제 광고 성공을 의미하지 않는 명시적인 프로토타입 버튼이다.
- 검증: `powershell -ExecutionPolicy Bypass -File tools/RevivePopupValidation/Run.ps1 -Graphics`. 짧은 임시 별도 프로젝트에서 실제 Player/GameManager/uGUI를 PlayMode로 실행한다. 원본 에디터에는 접근하지 않는다. 컴파일 검사는 `tools/AndroidReadinessValidation/RunCompile.ps1`.
- 테스트는 반복 사망/부활, 중복 요청 차단, HP/입력 복구, MP/전략 잔여 횟수 유지, 거절 결과, 광고 안내 확인, 이미지 로딩과 3개 화면 캡처를 확인한다. 전체 MainScene/Android 검증을 대신하지 않는다.

## 이미지 제작 (2026-11-17 톤 수정)

사용자 피드백: 이전 하트·리본·핑크가 지나치게 공주풍이었다. 은빛 날개·후광·청록 불씨의 도트풍 천사로 교체했다. 팝업은 남색 바탕, 청록 주요 버튼, 회청색 보조 버튼, 밝은 회색 글자로 변경했다. 게임 동작은 동일하다.

Built-in image_gen 사용. 최종 파일: `Assets/Resources/UI/ReviveAngel.png` (1774×887 RGBA, 실제 투명 알파 유지). 기존 Sprite GUID/참조를 유지했다.

이전 은빛 날개 버전 프롬프트:

> Use case: stylized-concept. Asset: transparent resurrection angel emblem for a dark 2D pixel-art magical-girl zombie-survival action game. Restrained combat UI symbol, not princess jewelry. A compact pair of silver-white feather wings flanking a tiny cool cyan soul flame, a single thin muted silver halo above. Strong readable silhouette, crisp deliberate pixel-art edges and limited slate-blue / silver / desaturated cyan palette, subtle navy outlines. Symmetric wings angled slightly upward, simple layered feathers. No hearts, no gemstones, no bows, no ribbons, no pink, no purple, no gold, no ornate jewelry, no scattered sparkles, no person, no text. Flat sprite-style shading, no bloom or haze. Centered wide icon on genuinely transparent background, generous clear margins. The icon should feel like a restrained resurrection ability in a combat HUD, retain a gentle angelic quality without cute princess ornament.
## 2026-11-17 중앙 불씨 → 플레이어 지팡이

최종 에셋은 동일한 `Assets/Resources/UI/ReviveAngel.png` (1774×887, 투명 PNG)이다. Built-in image_gen 편집을 사용했다. 기존 천사를 편집 대상으로, `Assets/2.Resources/Art/Pretty_Magical_Girl/Idle/rotations/south.png`의 확대본을 지팡이 참고로 제공했다. 금빛 네 갈래 별과 연보라색 손잡이를 중앙에 세웠다. 날개/후광의 디자인과 배치 유지, UI 코드/meta/GUID 변경 없음.

지팡이 교체 단계 프롬프트:

> Use case: precise-object-edit. Image 1 is the EDIT TARGET, the existing transparent pixel-art silver angel wings and halo resurrection icon. Image 2 is a REFERENCE ONLY showing the actual player's wand held at the lower left: a very simple slender lavender/pale-purple shaft with a small golden yellow four-point star at its tip. Edit ONLY the central cyan flame of Image 1: remove that flame and replace it with that same simple player wand, upright, gold star at top and lavender handle downward. Fit the wand entirely in the existing center gap below the halo, approximately the flame's original footprint; do not obscure or move either wing or the halo. Keep wing silhouettes, individual feathers, silver-blue colors, halo size and position, overall layout, canvas and transparent background unchanged. Match existing crisp pixel-art style. The reference's character, clothing, pink bows, ribbons and face must NOT be inserted. No added ornaments, no heart, no extra glow, no new sparkles, no text. A small plain star-tipped lavender wand ONLY instead of the flame. Preserve actual transparent alpha.
## 2026-11-17 상위 티어 지팡이 비주얼

사용자 요청으로 같은 별 지팡이가 진화한 느낌을 추가했다. 금빛 네 갈래 별을 크게 면 분할하고 은빛 각진 테두리, 짙은 보라색 그립, 금속 고리와 은빛 끝장식을 더했다. 날개/후광의 디자인·배치 유지. 부활 아이콘만 변경했으며 실제 플레이어 무기 스프라이트/등급/게임 수치는 바꾸지 않았다. Built-in image_gen 편집, 최종 파일은 `Assets/Resources/UI/ReviveAngel.png` (1774×887 RGBA).

최종 편집 프롬프트:

> Use case: precise-object-edit. Edit this transparent pixel-art angel resurrection emblem for the game. Upgrade ONLY the central star wand so it feels like an evolved higher-tier version of the same weapon. Preserve the large silver-blue feather wings and silver halo design, positions and scale, transparent canvas and overall composition. Central wand: retain recognizable golden four-point star head and violet shaft lineage, but transform the plain little star into a larger beautifully faceted pale-gold four-point radiant star with a tiny icy-white core and a restrained angular silver setting. Add two compact swept silver prongs supporting the star, refined gold collars on the dark violet grip, a tapered silver pommel. Make the central weapon about 1.5x wider and a little longer than the original, clearly readable as a rare upgraded wand between the wings, with the star just below the existing halo. Sharp deliberate pixel-art clusters and metallic highlights, excellent small-icon readability, limited gold/silver/violet colors. Keep restraint: no hearts, bows, ribbons, pink decorations, princess jewelry, floating sparkles, flames or diffuse glow. Do not add extra surrounding wings or another halo; no text, no background. Maintain existing wings and halo unchanged in appearance. True transparent alpha. The result should unmistakably be an upgraded version of this same wand, not a different weapon.