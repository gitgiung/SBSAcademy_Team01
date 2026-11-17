# Beast Zombie Run art

## 현재: v3 원본 캐릭터 외형 보정

- 사용자가 v2 모션은 좋지만 캐릭터가 달라졌다고 확인. `Idle/rotations`의 south/south-east/east/north/north-east를 외형 기준으로, v2 방향별 시트를 동작 기준으로 built-in imagegen 편집했다. 두꺼운 상체/팔, 짧은 다리, 괴물 얼굴, 가슴·등의 붉은 무늬, 어깨·무릎 상처와 짙은 찢어진 바지를 복원하는 방향이다. 생성 편집이므로 관절 위치가 픽셀 단위로 동일하다는 의미는 아니다.
- 현재 `bake.mjs` → `bake-v3.mjs`. `*-identity-v3.png` 6개가 원본이며 SW는 SE 반전으로 기존과 같은 동작 관계를 유지한다. West는 이전 그대로. 정확한 요청문은 `prompts-v3.md`.
- 56 PNG만 변경, 현재 작업 시작 대비 64 meta/West 8 PNG 동일. 8프레임 순서·12fps·클립·골반 정렬·캔버스와 재생 길이 유지. `contact-v3.png`, `preview.gif`, `preview-slow.gif` 갱신.
- 정적 검사 64/64 통과 및 출력 시트 육안 확인. Unity Play/GIF 재생 관찰 미실행. 처음 SW PNG 쓰기 오류는 짧은 재시도 추가 후 전체 재출력으로 해결했다.
- 아래 v2는 이전 기록이며 재생성 진입점은 위 v3가 우선한다.

## 2026-11-17 방향별 피드백 보정

- 현재 재생성은 `node tools/BeastRunArt/bake.mjs` → `bake-v2.mjs`. 아래 기존 통합 시트와 gait override 경로는 구버전이다. `bake-legacy.mjs`는 기록용이며 실행하지 않는다.
- built-in imagegen으로 방향별 4×2 시트를 생성했다. 정확한 프롬프트는 `prompts-v2.md`, 원본은 `*-source-v2.png`다. 초반 통합 시트 2개는 품질/배열 문제로 적용하지 않았다.
- East의 기울기를 줄이는 자세, North/South의 가슴~골반 높이 팔 교대, 대각선의 고정 방향 중간 자세를 요청했다. SW 생성본이 오른쪽을 향하여 해당 시트를 SE에 쓰고 좌우 반전하여 SW에 쓴다. SE 별도 생성본은 미사용. 나머지는 해당 방향 시트를 쓴다.
- 방향 내 고정 배율, 골반 중심 x66, 기본 접지 y116, 체공 프레임 3/7은 2px 상승. 전체 132×132, binary alpha. 기존 PNG 56장만 교체하고 West 8장·모든 meta·클립/12fps/반복 길이는 유지했다.
- `preview.gif`/`preview-slow.gif`: 위 N/NE/E/SE, 아래 S/SW/W/NW. `contact-v2.png`: 같은 순서의 8행, 각 행 0~7프레임.
- `node tools/BeastRunArt/validate-v2.mjs`: 64/64 규격·고유 그림·알파·여백·클립 참조·12fps/반복 검사 통과. 작업 전 스냅샷이 있으면 meta/West 불변도 검사한다. 이 검사는 모션 품질의 자동 판정이 아니다.
- 출력 시트 육안 확인. Unity 컴파일/Play 및 GIF 재생 관찰 미실행. 실제 MainScene의 Run/Idle 전환과 반복 체감은 남은 검증이다.

Run 전용 8방향 × 8프레임. 기존 sprint는 방향 회전/옆으로 다리 벌리기 때문에 재사용하지 않는다.

- `run-source.png`: built-in imagegen 생성 원본. 참조는 기존 보스 sprint 전체 시트이며 캐릭터 디자인만 참고했다.
- 생성 지시: N, NE, E, SE, S, SW, W, NW 행 순서, 8열의 달리기 접지/압축/통과/체공 반주기 반복. 행마다 방향 고정, 허리~가슴 아래의 굽힌 팔과 반대 다리가 앞뒤로 움직임, 전방 기울기, 팔 머리 위 들기/체조/좌우 벌리기 금지. 회색 근육질 좀비·붉은 어깨 상처·찢어진 검은 바지·맨발·투명 배경 유지.
- `bake.mjs`: 원본을 동일 그리드로 분리, nearest 110px + 11px 투명 여백 → 132×132. 알파 200 미만 제거. run PNG 및 Sprite Rect만 변경하며 GUID/내부 ID는 유지한다.
- `preview.gif`: 12fps, 위 행 N/NE/E/SE, 아래 행 S/SW/W/NW.

재생성: 저장소 루트에서 `npm install --prefix .utmp/skill-icons-runtime --no-audit --no-fund sharp@0.35.5`, `node tools/BeastRunArt/bake.mjs`.

Unity 실제 MainScene 재생과 기존 모션 전환의 시각 검증은 별도로 필요하다. Sprint 복사 재적용 스크립트(.utmp/beast-run-fix)는 구버전이므로 실행하지 않는다.
