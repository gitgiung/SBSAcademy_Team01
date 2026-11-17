# SwipeBoth — 16프레임 / 24fps, 효과 없는 교대 할퀴기

최종 기준: MVC 울버린처럼 실제 좌우 팔이 엇갈리는 액션. 교차 이펙트·X표시·흰 궤적·잔상·붉은 오라는 넣지 않는다.

## 제작

built-in imagegen으로 `smooth-sources/<direction>.png` 방향별 4×4 시트를 제작했다. 기존 방향별 8장을 참조하되 복제가 아닌 16개 연속 자세를 요청했다. 동일 좀비/색/근육/상처/바지/맨발, 고정 방향/규모/발 기준, 충분한 투명 여백.

프롬프트 동작: 0~1 낮춘 준비, 2~5 오른팔 감기, 6~8 첫 rake, 9 체중과 반대팔 전환, 10~12 왼팔 감기, 13~14 반대 rake, 15 guard로 이어지는 follow-through. 중간 팔꿈치/손목 위치를 채우고 손이 순간 이동하거나 가슴에 붙어 멈추지 않도록 요청했다.

SW 생성안이 정면에 가까워 최종 SW는 SE 시트 좌우 반전으로 방향을 맞췄다. 다른 7방향은 각 시트를 사용한다. 이전 8프레임/효과 포함 소스는 현재 bake에서 읽지 않는다. 원작 동작의 완전 재현이라고 주장하지 않는다.

## 출력

- `bake.mjs`: alpha>=200, nearest 리사이즈. 방향 내 배율 고정, 발 중심 x88/바닥 y148 정렬, 176×176. 안전 여백을 검사한 뒤 출력.
- 기존0~7 meta 보존. 새8~15만 GUID/internalID 추가. 재실행 시 신규 파일 GUID도 유지.
- 8클립 24fps, 16 Sprite + 마지막 유지 키. 총길이0.6666667초, 14번 키와 피해 이벤트0.5833333초, 종료 이벤트/비반복/피해 횟수 유지.
- `preview.gif`: 약24fps(42ms GIF 근사). `preview-slow.gif`: 점검용100ms. 위 N/NE/E/SE, 아래 S/SW/W/NW. GIF만 반복, 게임은 비반복.

재생성: `node tools/BeastSwipeBothArt/bake.mjs`. sharp는 `.utmp/skill-icons-runtime` 설치 사용. Unity EditMode의 `SwipeBothHasSixteenFramesWithoutChangingDamageTiming` 검사 추가. 실제 MainScene 재생/전환은 별도 검증 필요.

## East 수정 — 2026-11-17

기존 East는 앞팔이 고정되고 뒤팔만 머리 위에 올라가며 14번에서 팔이 아래로 튀었다. 출력 16장 contact sheet로 재현 확인 후 built-in imagegen으로 East 시트만 교체했다. `east-contact.png`는 전체 자세 확인, `east-preview.gif`/`east-slow.gif`는 East 단독 미리보기다.

프롬프트: 기존 East/West 출력 시트를 캐릭터 참조로 사용. 오른쪽을 바라보는 동일 좀비, 4×4/16연속 자세, 고정 발/체격, 앞팔을 뒤로 당겨 전방 대각선 아래로 긁고 회수한 뒤 반대팔 공격. 0 준비, 1~2 감기, 3~6 첫 공격, 7 회수, 8 반대팔 준비, 9~12 두 번째 공격, 13~15 복귀. 두 팔/손 연결 유지, 머리 위 응원·고정 앞팔·갑작스러운 손 이동·효과·잔상 금지. 투명 배경. 생성 시트에서 전방으로 뻗은 손→아래쪽 손→회수 자세를 확인했다. Unity 실제 재생은 미검증.

단일 방향 재생성: `node tools/BeastSwipeBothArt/bake.mjs east`. 다른 방향 PNG/클립을 쓰지 않고 전체 미리보기만 함께 갱신한다. 이번 교체에서 런타임 에셋은 East PNG16장만 변경됨을 SHA256으로 확인했고 meta/클립/다른 방향은 모두 동일했다. 128프레임/8클립 기존 정적 검사를 재실행해 통과했다.
