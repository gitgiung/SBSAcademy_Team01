# Beast Zombie charge and narrow N/S gait

- `charge-source.png`: built-in imagegen 11×8 시트. N/NE/E/SE/S/SW/W/NW 방향. 0~3 준비, 4~10 낮춘 돌진 루프. 수정된 보스 체형·회색 팔·국소 어깨 상처, 골반 아래 좁고 평행한 보폭. 옆으로 벌리는 다리/팔 및 붉은 오라 제외.
- `gait-source.png`: 기존 Move/Run 북·남 8×4 시트 편집. 행은 Move North, Move South, Run North, Run South. 발끝과 무릎이 진행 방향을 보고, 골반 아래의 좁은 평행 경로에서 앞뒤 교대하도록 요청했다. 상체/색/캐릭터는 기존 디자인 기준.
- `bake.mjs`: 두 생성 원본을 분리해 body-charge 88장 + Move/Run N/S 32장 적용. nearest 110px + 여백 11px, 132×132 full Rect. alpha>=200만 유지. GUID·내부 ID·PPU·이벤트·클립 시간은 유지한다.
- `gait-frames/`: Move/Run 제작 도구에서 사용하는 N/S 보정본. 이 파일들을 우선 사용하므로 각 도구 재실행 시 팔자걸음이 되돌아오지 않는다.
- `charge-preview.gif`: 위 N/NE/E/SE, 아래 S/SW/W/NW. 준비 0~3 후 4~10을 두 번 표시. GIF 반복 때 준비부터 다시 시작하지만 게임에서는 기존 준비/루프 상태 분리를 유지한다.
- `gait-preview.gif`: Move N, Move S, Run N, Run S. 비교 편의를 위한 공통 약 12fps. 실제 Walk 마지막 프레임 유지 시간과는 다를 수 있으며 기존 Move 전체 GIF에는 별도 타이밍이 유지된다.

재생성: `node tools/BeastChargeArt/bake.mjs`, 이어 `node tools/BeastRunArt/bake.mjs`, `node tools/BeastMoveArt/bake.mjs`로 각 전체 GIF 갱신. sharp 설치는 기존 `.utmp/skill-icons-runtime` 사용.

실제 MainScene 재생/전환/발 위치 검증 필요. 전체 시트 생성 방식이므로 다리 이외 픽셀도 원본과 동일하지 않다.
