# Beast Zombie Swipe art

Swipe 8방향 × 8프레임 전용 공격 그림. swipe-left/swipe-both는 이번 변경 대상 아님.

- `swipe-source.png`: 기존 Swipe 8×8 시트를 참조해 built-in imagegen으로 편집한 원본.
- 프롬프트: 동일 보스 디자인과 고정 방향 N/NE/E/SE/S/SW/W/NW. 손바닥 펴 보이기/가위 손동작 대신 굽힌 발톱. 0 준비, 1~2 팔 당기기, 3 준비 유지, 4 펼치기 시작, 5 할퀴기 타격, 6 후속 동작, 7 회수. 팔/손의 붉은 오라·선·궤적 제거, 어깨의 국소 피부 상처 유지. 텍스트/배경/VFX 없음.
- `bake.mjs`: 격자 분리 → nearest 144×144 + 16px 여백 = 176×176; binary alpha. 팔이 뻗는 공간을 확보하면서 본체 크기를 유지하기 위한 캔버스. PNG 및 full Sprite Rect만 교체, GUID/내부 ID 유지. 실제로 동/서/북동 클립이 참조하는 FrameSources 사본 24장도 동기화한다. 사본 meta는 변경하지 않는다.
- `preview.gif`: 위 N/NE/E/SE, 아래 S/SW/W/NW. 3번 417ms 준비 유지, 5번 타격 프레임. 기존 클립 약 1.083초 표시. 편의를 위해 GIF는 반복하지만 게임 클립은 비반복이다.

저장소 루트에서 `node tools/BeastSwipeArt/bake.mjs`. sharp 설치: `npm install --prefix .utmp/skill-icons-runtime --no-audit --no-fund sharp@0.35.5`.

게임 피해 이벤트 0.791666673초 및 모든 클립/이동/공격 수치는 변경하지 않는다. Unity MainScene 실제 재생과 피해 시점 체감은 별도 확인 필요.

## 북쪽 6번 조각 보정
생성 시트에서 앞 프레임의 손끝이 북쪽 6번 왼쪽으로 넘어왔다. imagegen 정리 결과의 왼쪽 32×176 영역만 추출한 north-6-left-cleanup.png를 적용한다. 본체는 그대로 보존하며 bake 재실행 시에도 같은 보정이 적용된다. preview.gif도 갱신했다.
