# Beast Zombie Move art

## 현재: 원본 직접 추출로 선명도 개선

- `bake.mjs`는 `bake-hires.mjs`를 실행한다. 기존 132px 출력이나 축소된 gait-frames를 확대하지 않고 원본 시트에서 직접 220px 칸으로 추출한다. N/S는 `BeastChargeArt/gait-source.png`의 첫 두 행을 직접 읽어 좁은 보폭을 유지한다. 나머지는 기존 move-source다. 새 그림 생성/모션 변경 없음.
- PNG 264×264, 22px 여백, PPU200, Sprite Rect264. 이전 132×132/PPU100과 월드 캔버스 크기(1.32)가 동일하다. Point/mipmap off를 유지하고 Default/Standalone/Android 압축을 None으로 변경했다. GUID/internalID/피벗/클립 그대로.
- 원본의 세부 묘사 한계는 남는다. 비압축 텍스처와 픽셀 수 증가로 메모리 사용량은 증가한다. 실제 Android 메모리/시각 확인은 미실행.
- `node tools/BeastMoveArt/validate-hires.mjs`: 64/64 통과. 이번 작업 전 백업이 있으면 meta의 Rect/PPU/압축 외 불변도 확인한다. `clarity-comparison.png` 위=이전, 아래=개선, S/E/N/SE 순. 실제 PNG의 같은 표시 크기 비교이며 Unity 렌더는 아니다.
- `BeastChargeArt/bake.mjs`는 과거 132px Move N/S도 쓰므로 실행했다면 반드시 현재 Move bake를 마지막에 다시 실행한다. `bake-legacy.mjs`는 과거 기록용이다.

아래 내용은 이전 132px 파이프라인 기록이다.

Move 8방향 × 8프레임. 기존 팔의 양손 내밀기/옆으로 벌리기를 줄인 걷기 그림.

- `move-source.png`: 기존 move 시트를 참조해 built-in imagegen으로 편집한 원본.
- 프롬프트 요지: 원래 회색 근육질 좀비·붉은 어깨 상처·바지·맨발·픽셀 스타일 유지. 작은 보행 팔 스윙, 팔꿈치 살짝 굽힘, 손은 골반/허리 근처, 반대 다리와 교대. 양팔 동시에 앞으로 내밀기·펀치·옆 벌리기·머리 위 들기 금지. 행 방향 고정 N/NE/E/SE/S/SW/W/NW, 8열 걷기 루프, 투명 배경.
- `bake.mjs`: 동일 격자 분리 → nearest 110×110 + 11px 여백 = 132×132. 알파 200 미만 제거. PNG 및 full Sprite Rect만 교체, GUID/내부 ID 유지.
- `preview.gif`: 위 N/NE/E/SE, 아래 S/SW/W/NW. 기본 83ms, 마지막 167ms로 BossWalk_East의 약 0.75초 루프를 표현.

저장소 루트에서 `node tools/BeastMoveArt/bake.mjs`. sharp 설치는 `npm install --prefix .utmp/skill-icons-runtime --no-audit --no-fund sharp@0.35.5`.

Unity 실제 재생 및 Idle/Run 전환의 시각 확인은 별도 필요하다. 이전 이미지와 픽셀 단위로 팔만 합성한 것은 아니며, 걷기 시트를 다시 생성한 결과다.
