# Cursor 작업 기록

Cursor만 갱신한다. Codex는 읽고, 수정하지 않는다.

## 2026-11-11 비스트 보스 Idle 좌·우 배 실루엣

- 목표: Idle east/west에서 배가 앞으로 부풀어 보이던 실루엣을 집어넣는다.
- 담당 파일: `Assets/2.Resources/Art/Beast_Zombie_Boss/Idle/animations/idle/east|west/0~3.png` (8장). meta·애니 클립·컨트롤러 미수정.
- 진행 상태: PNG 수정 저장 완료. 인계 가능.
- 변경 요약: 팔 tip이 끝나는 행의 몸통 앞선을 기준으로 복부 12행을 최대 부풀림 1px·허리 쪽 테이퍼로 잘라 내고 검정 아웃라인을 다시 찍음. 스크립트·백업은 `.utmp/fix_belly.py`, `.utmp/belly_backup`(Git 제외).
- 검증: 픽셀 전후 수치·비교 PNG만 확인. Unity 임포트/Play 미실행.
- 다음 요청: 사용자 육안 확인. 더 집어넣거나 대각 방향도 필요하면 알려 달라.

## 2026-11-11 협업 설정과 걷기 모션 현황

- 목표: Cursor(구현)와 Codex(디자인 주도, 코드 검토·검증)의 협업 규칙을 기존 지침에 추가하고, 작업 트리에 있는 정면 뼈대 걷기 조절의 현재 상태를 기록한다.
- 담당 파일:
  - 이번 세션에서 수정: `AGENTS.md`
  - 이번 세션에서 생성: `docs/AI/cursor.md`, `docs/AI/codex.md`, `docs/AI/design.md`
  - 구현 파일은 수정하지 않음. 아래 두 파일은 이전 구현이 작업 트리에 저장된 상태이며, 인계 가능(Codex 검토 대기).
    - `Assets/0.Scripts/Game/SoulPipeline/SoulPlayerBoneRig.cs`
    - `tools/SoulPipelineValidation/Unity/SoulPipelinePlayProbe.cs`
- 진행 상태: 협업 설정 완료. 브랜치 `feature/soul-pipeline-3d`, HEAD `d1779b8`. 걷기 구현은 커밋되지 않은 변경으로 디스크에 저장되어 있다. Unity Editor 프로세스는 이 기록 시점에 없었다.
- 변경 요약:
  - 협업 규칙과 디자인 주도 역할을 `AGENTS.md`에 추가했다. 기존 작업 지침 본문은 유지했다. `AGENTS.override.md`와 `.cursor/rules`는 없어서 새로 만들지 않았고, Codex도 읽는 `AGENTS.md`에만 반영했다.
  - 작업 트리 diff 기준 `SoulPlayerBoneRig`의 정면 걷기 값은 Inspector `Walk` 항목으로 노출되어 있다. 기본값은 주기 0.7초, 엉덩이 sway 1.5px / bob 1.25px / tilt 1.5°, 발 들기 3px, 팔 bob 1.25px / spread 7.5° / bias 2.5°, 머리 tilt 1.5°, 머리카락 지연 3.5°이다.
  - `SoulPipelinePlayProbe`는 걷기 발·머리카락·엉덩이 하한과 몸통 중심 늘어남 `<4` 검사를 추가한 상태다.
- 검증:
  - 코드 검토: 이번 세션에서 두 소스의 작업 트리 diff를 읽었다. 구현 적합성에 대한 Codex 검토는 아직 없다.
  - 컴파일 확인: 이번 세션에서 Unity 컴파일을 새로 실행하지 않았다. 두 소스의 SHA-256이 검증 복사본 `C:\sv\soul`과 같고, 그 복사본의 배치 로그 `C:\sv\soul-play-probe.log`(2026-11-11 22:12, 프로젝트 경로 `C:\sv\soul`)에는 C# 컴파일 오류 문자열이 없으며 프로브가 끝까지 실행되었다.
  - 플레이 검증: 같은 실행의 `C:\sv\soul-probe.txt`는 전체 PASS다. 걷기 항목은 발 들기 3.0px, 머리카락 2.2px, 엉덩이 1.4px, 몸통 중심 늘어남 x3.79이다. 이번 세션에서 Play를 다시 실행하지 않았다. 이번 진폭의 스크린샷·플레이 영상·에디터 육안·기기는 확인하지 않았다.
  - 파일 존재 확인: 이번 세션에 `tools/SoulPipelineValidation/CheckFiles.ps1`을 실행해 37/37 PASS, meta GUID 검사 PASS.
- 다음 요청: Codex가 `docs/AI/design.md`에 정면 걷기 피드백을 남기면, 그 요구사항을 Inspector 수치와 코드에 반영한다. 소스를 다시 수정하기 전에 이 파일의 담당·인계 상태를 갱신한다.
