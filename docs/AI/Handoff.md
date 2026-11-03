# 작업 인수인계

## 2026-11-03 좀비 하운드 견종 교체

- 시작 상태: `main`, HEAD `b71b791`. 작업 트리의 `Zombie_Hound` 그림은 빠져 있었고, 프리팹 `ZombieHound`만 수정돼 있었다. 직전 들개는 짧은 주둥이의 소형 개였고, 몬스터 기획서의 들개·박쥐 수치는 여전히 없다.
- 완료: 잉글리시 폭스하운드 골격의 좀비 들개로 교체했다. PixelLab 캐릭터 `30e3b20a-c9f2-4ed9-aca7-cda990aae8c0`. 늘어진 귀, 긴 주둥이, 구분되는 앞·뒷다리. 8방향 회전, idle 8프레임, run 8프레임. 캔버스 96×96. 클립·컨트롤러·스프라이트 GUID는 유지하고 PNG와 스프라이트 rect, 프리팹 `oldSize`만 바꿨다. 체력 3, 접촉 피해 10, 이동 속도 3. 박스 콜라이더 0.32, 원 반지름 0.16은 그대로다. 스폰 타임라인에는 넣지 않음.
- 검증: 남·동·북·서·남동 회전과 남·북 idle, 남·동·서·북동 run을 눈으로 확인. PNG 136장 모두 96×96, rect 96인 메타 136개, Point 필터와 GUID 유지. Unity 테스트 0개. Unity가 이미 열려 있어 에디터 재생은 확인하지 않음.
- 다음 확인: 열린 Unity의 임포트가 끝나면 `ZombieHound` 프리팹을 테스트 씬에 두거나 `Enemy Spawn System`의 Enemy Prefabs에 넣는다. 플레이어를 따라 달리면 run, 붙으면 idle. 그림이 비거나 예전 소형 개면 `Zombie_Hound` Reimport. 몸이 접촉 판정보다 커 보이면 콜라이더를 맞춘다.
- 속도·무리 스폰 수치는 기획 미정이라 확정하지 않음.

## 2026-11-03 좀비 들개

- 시작 상태: `main`, HEAD `13e4abe`. 몬스터 기획서에는 기본 좀비 HP 3·공격력 10만 확정돼 있고, 들개·박쥐 대응 수치는 없다. `EnemyDatatable.xlsx`는 엑셀이 열어 둔 상태라 읽거나 수정하지 않음.
- 완료: 8방향 좀비 들개. 앞다리와 뒷다리가 겹쳐 한 쌍처럼 보이던 64px 그림을 버리고, 개 골격으로 앞·뒷다리가 구분되는 92px 그림으로 교체했다. 회전, idle, run 각 8프레임. 클립·컨트롤러·프리팹 GUID는 유지하고 PNG와 스프라이트 rect만 바꿨다. 체력 3, 접촉 피해 10, 이동 속도 3. 스폰 타임라인에는 넣지 않음.
- 검증: 남·동 회전과 남쪽 idle, 남·동 run에서 앞다리와 뒷다리가 따로 보이는지 확인. PNG 136장 모두 92×92, rect 92인 메타 136개. Unity 테스트 0개. 에디터 재생은 확인하지 않음.
- 다음 확인: Unity 임포트 후 `ZombieHound` 프리팹을 테스트 씬에 두거나 `Enemy Spawn System`의 Enemy Prefabs에 넣는다. 플레이어를 따라 달리면 run, 붙으면 idle. 그림이 비면 `Zombie_Hound` Reimport.
- 속도·무리 스폰 수치는 기획 미정이라 확정하지 않음.

## 2026-11-03 Beast Zombie Boss idle 모션

- 시작 상태: `main`, HEAD `7cd15af`. Beast Zombie Boss의 8방향 `BossIdle_*` 클립은 회전 스프라이트 1장을 두 번 붙잡은 정지 클립이었다.
- 완료: PixelLab 캐릭터 `45afeb63-7461-402b-9282-8d0bbcc7bdfe`에 `breathing-idle` 템플릿으로 8방향 4프레임을 생성했다. 그룹 `c0647bc4-943d-4c82-890a-78321270802c`. 프레임은 `Idle/animations/idle/{방향}/0.png`~`3.png` 32장. 기존 `BossIdle_South`부터 `BossIdle_NorthWest`까지 8개 클립이 이 프레임을 12샘플, 1초 루프로 재생한다. 컨트롤러 `Boss Idle Directions`와 클립 GUID는 유지했다.
- 검증: 남·북·동 프레임을 눈으로 확인했고, 같은 보스 자세에서 가슴이 늘었다 줄었다. PNG 32장 모두 96×96, Point 필터 메타 32개, 방향 클립 8개가 각 4프레임을 가리킨다. Unity 테스트 0개. Unity가 꺼져 있어 에디터 재생, 화면, 빌드, 기기는 확인하지 않음.
- 다음 확인: Unity로 프로젝트를 연 뒤 임포트가 끝나면 `Boss.controller`의 BossIdle 상태를 재생하거나, 보스전이 있는 씬에서 보스가 멈춘 채 숨 쉬는 모션이 도는지 본다. 스프라이트가 비면 `Idle/animations/idle` 우클릭 → Reimport 후 Console을 확인한다.
- 씬, 프리팹, 코드, Project Settings, 패키지는 변경하지 않음.

## 2026-11-03 스프라이트 필터 설정

- 시작 상태: `main`, HEAD `b3b0de6`, 작업 트리 깨끗함. 기존 Handoff.md는 없었음.
- 완료: `Assets/2.Resources` 하위 TextureImporter의 Sprite(`textureType: 8`) 1,471개를 확인하고 Bilinear였던 908개의 `.meta`를 Point(`filterMode: 0`)로 변경. 기존 Point 563개 유지.
- 검증: 전체 Sprite 1,471개에서 Point가 아닌 항목 0개. Git diff에서 필터 값 외 변경 0줄. `git diff --check` 통과. GUID와 다른 임포트 설정은 유지.
- 실행한 Unity 테스트: 0개. 설정 파일 검사만 수행했으며 Unity 재임포트, 화면 표시, 빌드와 기기는 검증하지 않음.
- 다음 확인: Unity로 돌아가 자동 임포트 완료 후 Project → Assets/2.Resources에서 스프라이트 선택 → Inspector → Filter Mode가 `Point (no filter)`인지 확인. 이전 값이 보이면 해당 에셋 우클릭 → Reimport 후 Console의 임포트 오류 확인.
- 이번 작업에서 씬, 프리팹, 코드, Project Settings, 패키지는 변경하지 않음.
