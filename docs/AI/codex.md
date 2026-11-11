# Codex 작업 기록

Codex만 갱신한다. 이 초기 스냅샷은 기록 파일이 없어 Cursor가 사용자 요청으로 작성했다. 이후 수정은 Codex가 한다.

## 2026-11-11 Cursor 프로젝트 규칙 연결

- 목표는 Cursor와 Codex가 동일한 프로젝트 지침과 인계 기록을 읽게 하는 것이다.
- 담당 파일은 `.cursor/rules/agent-collaboration.mdc`와 이 기록이다.
- Cursor가 새 채팅에서 공통 `AGENTS.md`와 협업 기록을 읽도록 상시 적용 규칙을 추가했다. Codex는 프로젝트 루트의 `AGENTS.md`를 따른다.
- 설정 검증에서 `alwaysApply: true`, 참조 문서 존재, Git 추적 가능 여부와 `git diff --check`를 확인했다. Cursor 채팅에서의 로드는 사용자 확인 대기다.
- 이번 설정 작업의 코드 검토, Unity 컴파일, 플레이 검증은 미실시다.
- 다음 작업은 정면 걷기의 비교 자료를 확보해 원본 대비 동작의 가독성과 소매·머리 형태를 평가하고, 결과를 `design.md`에 남기는 것이다.

## 2026-11-11 초기 스냅샷 (Codex 검토 전)

- 목표: 정면 뼈대 걷기 모션의 디자인 방향과 코드 검토. 이 시점에는 Codex 세션이 검토를 수행하지 않았다.
- 담당 파일:
  - 검토 대상(미수정, 인계 가능): `Assets/0.Scripts/Game/SoulPipeline/SoulPlayerBoneRig.cs`
  - 검토 대상(미수정, 인계 가능): `tools/SoulPipelineValidation/Unity/SoulPipelinePlayProbe.cs`
  - 이후 Codex가 유지: `docs/AI/design.md`
- 진행 상태: 초기 기록만 있음. 디자인 평가 전. 브랜치 `feature/soul-pipeline-3d`, HEAD `d1779b8`. 위 두 소스의 변경은 커밋 전이며 Cursor 쪽에 저장되어 있다.
- 변경 요약: 작업 트리 diff에 정면 걷기 진폭의 Inspector 노출과 Play 프로브의 걷기 하한·몸통 늘어남 검사가 있다. 수치는 `docs/AI/cursor.md`와 `docs/AI/design.md`에 적어 두었다. Codex가 만든 변경은 없다.
- 검증:
  - 코드 검토: Codex 미실시.
  - 컴파일 확인: Codex 미실시.
  - 플레이 검증: Codex 미실시. 이번 진폭의 스크린샷·플레이 영상을 보지 못했다. 외형과 모션 품질은 미확인이다.
- 다음 요청: 확보된 화면을 본 뒤, 정면 걷기가 원본 프레임 대비 읽히는지와 소매·머리 찌그러짐 여부를 `docs/AI/design.md`에 남긴다. 수치 변경이 필요하면 Cursor용 구현 요구사항을 적고, 소스 수정은 담당 인계 후에 시작한다.
