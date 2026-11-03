# 작업 인수인계

## 2026-11-03 스프라이트 필터 설정

- 시작 상태: `main`, HEAD `b3b0de6`, 작업 트리 깨끗함. 기존 Handoff.md는 없었음.
- 완료: `Assets/2.Resources` 하위 TextureImporter의 Sprite(`textureType: 8`) 1,471개를 확인하고 Bilinear였던 908개의 `.meta`를 Point(`filterMode: 0`)로 변경. 기존 Point 563개 유지.
- 검증: 전체 Sprite 1,471개에서 Point가 아닌 항목 0개. Git diff에서 필터 값 외 변경 0줄. `git diff --check` 통과. GUID와 다른 임포트 설정은 유지.
- 실행한 Unity 테스트: 0개. 설정 파일 검사만 수행했으며 Unity 재임포트, 화면 표시, 빌드와 기기는 검증하지 않음.
- 다음 확인: Unity로 돌아가 자동 임포트 완료 후 Project → Assets/2.Resources에서 스프라이트 선택 → Inspector → Filter Mode가 `Point (no filter)`인지 확인. 이전 값이 보이면 해당 에셋 우클릭 → Reimport 후 Console의 임포트 오류 확인.
- 이번 작업에서 씬, 프리팹, 코드, Project Settings, 패키지는 변경하지 않음.

## 2026-11-03 보스 남쪽 Swipe 잘림 수정

- 시작 상태: `BossId`, HEAD `964ff51`. 기존 변경인 UniversalRenderPipelineGlobalSettings.asset, EditorBuildSettings.asset, ShaderGraphSettings.asset는 보존.
- 재현: Beast_Zombie_Boss/Idle/animations/swipe/south의 4~7.png에서 원본의 불투명 픽셀이 Sprite Rect 밖에 있었음. 각각 526/384/224/125픽셀이 영역 밖으로 잘림. Boss_Swipe_South.anim은 해당 Sprite ID를 직접 참조함.
- 완료: 해당 `.png.meta` 4개의 Sprite Rect만 상하좌우 대칭 확장하고 1픽셀 여유 확보. Center 정렬과 원본 좌표상 중심, GUID, spriteID, internalID를 보존. 코드·씬·프리팹·애니메이션 클립·원본 PNG·Collider는 변경하지 않음.
- 새 Rect(x,y,w,h): 4=(29,26,107,110), 5=(28,25,106,110), 6=(28,23,105,114), 7=(29,22,103,116).
- 검증: 원본 PNG 알파와 Rect 비교에서 수정 전 4개 모두 잘림 재현, 수정 후 영역 밖 불투명 픽셀 0. 중심 좌표 동일, Rect 외 메타 내용 동일 확인. git diff --check 통과. Unity 컴파일/테스트 실행 0개이며 실제 재임포트·화면 재생은 미확인.
- 사용자 확인: Unity 자동 임포트 후 Project → Assets/2.Resources/Art/Beast_Zombie_Boss/Idle/animations/swipe/south → 4~7.png → Sprite Editor에서 손끝과 이펙트 전체가 사각형 안에 들어오는지 확인. 보스 선택 → Animation 창 → Boss_Swipe_South의 4~7프레임을 이동해 잘림 없이 중심이 유지되는지 확인. 이전 영역이면 PNG 4개만 Reimport하고 Console 임포트 오류 확인.
- 다음 작업: 실제 Unity 재생 확인. 다른 방향·프레임은 이번 요청 범위 밖으로 유지.

## 2026-11-03 캐릭터 Sprite Rect 추가 검사

- 캐릭터/보스 원본 Sprite PNG 1,410개 검사(배경 CityPeaceful, Items, Tiles 제외), 읽기 오류 0. 영역 밖 알파 픽셀 검사에서 Beast_Zombie_Boss에 추가 57개 발견. 앞서 수정한 south 4~7은 잘림 0 유지.
- 애니메이션 클립이 참조하는 문제 이미지 33개: swipe/north 0~7, north-west 0·3~7, south 0~3, south-east 0·2~7, south-west 0~7.
- 현재 클립이 직접 참조하지 않는 문제 이미지 24개: swipe/east 0~7, west 0~7, north-east 4~7, swipe-both/east 0·2, west 2·4. 대체 FrameSources를 쓰는 경우 원본 결함과 실제 재생 결함을 구분해야 함.
- 다른 캐릭터는 이번 Rect 검사에서 영역 밖 픽셀 없음. 별도로 PNG 캔버스 가장자리에 그림이 닿는 후보 65개(Beast 보스 6, Magical_Girl_Zombie 1, Pretty_Magical_Girl 43, Zombie 7, Zombie_Construction 8)가 있으나 원본 잘림 여부는 미확정. Tight 메시·압축·실제 화면 가림은 이 검사 범위 밖.
- 이번 요청은 추가 확인이므로 에셋 추가 수정 없음. 다음 작업은 사용자 수정 요청 시 위 목록의 Rect 확장. Unity 테스트 실행 0개, 실제 화면 재생 미확인.
- 직접 확인: 보스 선택 → Animation 창 → Boss_Swipe_North / NorthWest / South / SouthEast / SouthWest에서 해당 프레임 확인. Sprite Editor에서 손·이펙트가 Rect 밖이면 같은 문제. 불일치 시 실제 연결 Sprite와 Console 임포트 오류 확인.

## 2026-11-03 추가 발견 57개 Sprite Rect 수정 완료

- 사용자 요청에 따라 위 추가 검사에서 발견한 Beast_Zombie_Boss 이미지 57개의 `.png.meta`를 수정. 현재 클립 참조 33개와 미참조 원본 24개 모두 포함. 앞서 수정한 south 4~7은 그대로 유지하여 총 수정 메타 61개.
- 주 Sprite Rect를 상하좌우 대칭 확장하여 원본의 모든 알파 픽셀과 최소 1픽셀 여유 포함. 중심 좌표·Center 정렬·GUID·spriteID·internalID·기타 메타 설정 보존. 여러 Sprite가 나뉜 원본 12개는 가장 큰 본체 Sprite만 확장하고 나머지 작은 이펙트 Sprite의 Rect/ID는 유지.
- 검증: 57개 수정 전 잘림 재현 보고서 기반으로 수정 후 캐릭터/보스 PNG 1,410개 재검사 → Sprite Rect 밖 알파 픽셀을 가진 이미지 0개, 읽기 오류 0개. 57개 모두 중심 동일·Rect 이외 메타 내용 동일 확인. 코드·씬·프리팹·클립·Project Settings 등 231개 파일의 작업 전후 해시 동일. git diff --check 통과.
- Unity 컴파일/테스트 실행 0개. 실제 에디터 재임포트·애니메이션 재생은 미확인. 캔버스 가장자리 접촉 후보 65개는 원본 이미지 자체의 별도 조사 대상으로 유지하며 이번 수정으로 해결됐다고 판단하지 않음.
- 사용자 확인: Unity 자동 임포트 완료 → 보스 선택 → Window/Animation/Animation → Boss_Swipe의 각 방향 재생. 손끝·공격 이펙트가 잘리지 않고 보스 중심이 유지되는 것이 정상. 변화가 없으면 해당 PNG만 Reimport하고 Sprite Editor의 Rect 및 Console 임포트 오류 확인.
- 다음 작업: 실제 재생에서 수정 결과 확인. 이전 절의 '추가 수정 없음'은 당시 검사 시점 기록이며 이번 절에서 수정 완료함.

## 2026-11-03 남동쪽 걷기 첫 프레임 참조 수정

- 사용자 스크린샷은 swipe가 아닌 move/south-east/0.png의 Sprite 목록이었음. 0_0은 작은 조각(10x8), 0_1은 본체(83x94)인데 BossWalk_SouthEast.anim 첫 프레임이 0_0을 참조하는 문제 확인.
- 수정: 해당 클립의 첫 Sprite 키와 pptrCurveMapping만 본체 0_1(fileID 8745284039982918138)로 변경. GUID, 나머지 프레임·타이밍·이벤트·코드·씬·프리팹·원본 meta 유지.
- 검증: 본체 Rect에 원본의 전체 알파 픽셀이 포함됨 확인. 키와 바인딩의 본체 참조 2곳 일치, git diff --check 통과. Unity 테스트 0개, 실제 재생 미확인.
- 이전 1,410개 검사는 한 PNG의 모든 Sprite Rect 합집합과 원본을 비교한 것이므로, 작은 조각을 애니메이션이 잘못 참조하는 문제까지 검증한 것은 아님.
- 직접 확인: Unity 임포트 후 보스 선택 → Animation → BossWalk_SouthEast → 0프레임에서 보스 본체가 보여야 함. 여전히 조각만 보이면 클립 Reimport 후 첫 Sprite 키가 0_1인지와 Console 오류 확인.

## 2026-11-03 Enemy/Boss 플레이어 접촉 밀림 방지

- 시작 상태: BossId, HEAD 964ff51. 이전 Sprite Rect 메타 61개, BossWalk_SouthEast.anim, Handoff 및 기존 설정 3개 변경을 보존.
- 원인: EnemyFollow.Awake가 일반 적/보스 Rigidbody2D를 Dynamic으로 설정하여 플레이어 충돌의 물리 반발에 밀림. FixedUpdate에서 속도를 0으로 만들어도 다음 물리 스텝의 위치 보정까지 막지는 못했으며 보스 공격 시 Follow 비활성 상태에서도 밀림.
- 수정: 이동/물리 책임을 가진 EnemyFollow에서 bodyType을 Kinematic으로 설정하고 useFullKinematicContacts=true로 접촉 콜백 유지. EnemyContactDamage, BossController, 씬/프리팹/Collider/공격 수치는 변경하지 않음.
- 추가: Assets/Tests/EnemyPhysics에 독립 PlayMode 테스트 어셈블리와 접촉 데미지 수신용 테스트 컴포넌트. 기존 런타임 어셈블리 이동 없음.
- 재현/검증: Unity 6000.5.0f1의 별도 검증 프로젝트에서 실제 EnemyFollow/EnemyContactDamage/IContactDamageReceiver 소스 사용. 수정 전 10개 중 5개 실패(플레이어 Dynamic/Kinematic 각각 Follow 켜짐/꺼짐 밀림 4개, 재사용 후 외부 힘 1개), 수정 후 10/10 통과·생략 0. solid/trigger 접촉 데미지 및 쿨다운 4개, 추적과 정지 거리 1개, Follow 비활성 중 MovePosition 이동 및 재사용 후 외부 힘 저항 1개 포함.
- 초기 검증 6개 중 일부는 테스트 씬 비동기 정리 문제로 실패했으며 UnityTearDown으로 수정한 뒤 위 10개를 다시 실행함. 이를 게임 결함으로 집계하지 않음.
- 검증 경로: .utmp/EnemyPhysics/reproduced.xml, after.xml 및 해당 로그. 원본 씬/프리팹/클립/기존 메타/Project Settings 작업 전후 해시 동일. git diff --check 통과.
- 제한: 전체 프로젝트 컴파일/전체 회귀 테스트/실제 MainTestScene 보스 패턴 재생은 미실행. 테스트는 실제 BossController 전체 대신 Follow 비활성 중 Rigidbody2D.MovePosition 경로를 검증함.
- 물리 동작: Kinematic은 접촉 힘으로 밀리지 않으며 코드로 이동함. 적끼리 및 Kinematic/Static 상대와의 접촉은 콜백은 발생하지만 서로 물리적으로 밀어내거나 분리하지 않음. 실제 군집/벽 및 Kinematic 플레이어 겹침 여부는 씬 확인 필요.
- 직접 확인: Unity 컴파일 완료 → MainTestScene Play → 일반 적/보스의 몸통에 플레이어를 계속 이동. 적/보스가 플레이어 때문에 밀려나지 않고 추적 및 접촉 HP 감소가 유지되어야 함. 보스 공격 대기·공격 중에도 확인. 오류 시 Console 컴파일 오류, 실행 중 Enemy/Boss Rigidbody2D의 Kinematic 및 Full Kinematic Contacts, Collider 활성/Is Trigger, 플레이어 Rigidbody 유형을 확인.
- 다음 작업: 실제 씬에서 밀림 방지·접촉 데미지·공격·돌진·군집 동작 확인. 에디터에서 새로 Play해야 Awake 설정이 적용됨.

## 2026-11-03 밀림 방지 + 몸통 관통 방지 재수정

- 사용자 의도 확정: 플레이어가 Enemy/Boss를 밀 수 없으며 서로의 solid 몸통을 통과해서도 안 됨. 이전 Kinematic 단독 변경은 이 의도를 충족하지 못하므로 이번 수정으로 보완.
- 시작 상태: BossId/964ff51. 기존 코드·이미지 메타 변경 외에 사용자 MainTestScene.unity 및 AddressableAssetSettings.asset 변경이 존재했으며 저장/덮어쓰기하지 않음.
- 공통 CollisionMovement2D(Combat.Runtime)를 추가: 실제 활성 non-trigger Collider2D 형상을 이동 거리 전체에 Cast하여 가장 가까운 solid 앞에서 이동 제한. 접촉면 방향으로 잔여 이동을 다시 검사하여 벽면 이동/모서리 차단 지원. Trigger는 이동 차단에서 제외하고 기존 접촉 데미지 콜백 유지. Collider/레이어 충돌 설정과 IgnoreCollision을 반영. 매 액터 이동 위치를 즉시 반영해 다른 액터의 다음 검사가 갱신된 위치를 보도록 함.
- EnemyFollow: Kinematic+Full Kinematic Contacts 유지(외력에 밀리지 않음), 추적에 위 이동 검사 적용. 공개 MoveTo를 보스 패턴에서도 사용 가능하도록 추가.
- BossController: 돌진의 직접 MovePosition을 Follow.MoveTo로 교체. solid에 막혀 이동이 불가능하면 돌진 루프를 끝내어 장애물 뒤에서 무한 대기하지 않도록 처리. 공격 수치/이벤트/일반 패턴 규칙 변경 없음.
- PlayerMovement: 키보드 Tick과 DashRoutine의 직접 Transform 이동을 같은 검사 경로로 교체. Rigidbody 없는 기존 호출에만 기존 Transform 이동 fallback 유지.
- PlayerController2D: 조이스틱의 직접 속도 이동을 같은 검사 경로로 교체. PlayerMvpMovement도 직접 MovePosition을 같은 검사로 교체. 기존 입력/속도/대시 시간·MP 규칙 유지.
- 재현: 이전 코드에 몸통 차단 검증을 추가한 14개 테스트 중 5개 실패(적→플레이어 관통, 적끼리 관통, 대시 관통, Kinematic 플레이어 관통 2개). 이후 실제 프레임 진행을 검증하는 UnityTest와 벽면/Trigger 검사를 추가.
- 최종 검증: Unity 6000.5.0f1 별도 PlayMode 프로젝트에서 18/18 통과, 생략 0. 실제 EnemyFollow, EnemyContactDamage, CollisionMovement2D, PlayerMovement, PlayerData 소스 사용. Dynamic/Kinematic 플레이어의 밀림·차단, 실제 PlayerMovement 키보드/대시, 적끼리 차단, Follow 비활성 중 보스 MoveTo 차단, 추적 정지 거리, 재사용/외력 저항, solid/trigger 접촉 데미지와 쿨다운, 큰 Trigger 반경 무시, 벽면 이동 및 후퇴 검증.
- 결과: .utmp/EnemyPhysics/blocking-final.xml 및 blocking-final.log. 초기 접촉면 방향 보완 후, UnityTest 프레임 시간과 Cast 접촉 여유를 반영해 최종 재실행. 이전 10개 통과 기록을 재사용한 결과가 아님.
- 한계: 실제 MainTestScene 전투/보스 전체 패턴/조이스틱 입력/프로토타입 InputAction의 화면 재생은 미실행. 별도 프로젝트에서는 공통 이동과 실제 PlayerMovement 경로를 검증했고, 모바일/프로토타입/전체 BossController 시나리오까지 통과했다고 주장하지 않음. 원본 에디터에서 변경된 Runtime/Assembly-CSharp 어셈블리 생성 시간은 확인함.
- 직접 확인: Unity 컴파일 완료 후 Play 재시작 → MainTestScene에서 키보드와 조이스틱으로 적/보스 몸통을 계속 밀기 → 대시로 몸통을 넘으려 하기 → 보스 공격 대기 및 돌진 중 반복. 플레이어는 몸통 앞에서 멈추고 적/보스는 밀리지 않아야 함. 붙은 뒤 반대 방향 이동과 옆 이동은 가능하고 접촉 HP 감소/공격은 유지되는 것이 정상. 적끼리도 관통하지 않아야 함.
- 오류 확인: Console 컴파일 오류, 양쪽의 활성 solid Collider 존재(Is Trigger=false), 충돌 레이어 및 IgnoreCollision, 실행 중 Enemy Rigidbody Kinematic+Full Kinematic Contacts, 이동 스크립트가 최신인지 확인. Collider가 Trigger뿐인 대상은 의도적으로 통과 가능.
- 다음 작업: 사용자 실제 씬에서 모든 입력 경로·군집·벽·보스 공격/돌진 확인. 이번 작업에서 씬/프리팹/Project Settings/패키지를 수정하지 않음.
