# 작업 인수인계

## 2026-11-04 전투 스킬 효과 데이터 확장

- 시작 상태: `main`, HEAD `1706cda`, 작업 트리 깨끗함. 사용자 요청은 테이블 기반 다음 구현 진행이다. 이번 단계는 기존 엑셀 변환기의 실행 데이터 보완이며 강공격·전략 실제 시전 구현은 후속이다.
- 완료: `CombatSkillRecord`에 대상, 효과 종류, 지속 시간, 상태이상 ID, 슈퍼아머, 넉백/군중제어, 등급, 아이콘·애니메이션·VFX·사운드 키, 설명을 추가했다. `Area_Typetable`의 반경·길이는 Radius·Length로 별도 보존한다. 기존 Range 및 약공격 수치와 해석은 유지한다. 빈 연출 키는 빈 문자열, None 지속 시간은 0, None 슈퍼아머는 false다.
- 완료: 실제 파서에서 inlineStr 문자열과 `/xl/...` 절대 시트 관계 경로도 지원한다. `Assets/Resources/Data/CombatSkillTable.json` 46개(약 21, 강 15, 전략 10)를 다시 생성했다. 엑셀 원본·씬·프리팹·meta·Project Settings·패키지는 변경하지 않았다. 신규 전역 매니저나 런타임 테스트 asmdef도 없다.
- 검증: 확장 전 독립 검증 6개 중 5개가 필드 누락·시트 경로 문제로 실패했고 구현 후 전체 7/7 통과했다. 현재 HEAD의 JSON과 비교해 46개 모든 기존 필드 값 동일. openpyxl 읽기 전용으로 새 효과·대상·연출·설명 값 644개를 실제 엑셀과 별도 대조해 일치했다. Unity 6000.5.0f1 설치본 Roslyn 및 기존 Unity 응답 파일을 사용해 Assembly-CSharp와 Assembly-CSharp-Editor 컴파일 각각 종료 코드 0, 오류·경고 0. 출력은 `.utmp/CombatSkillData`로 격리했고 Library DLL은 덮어쓰지 않았다. `git diff --check` 통과.
- 검증 도구: `tools/CombatSkillTableValidation/Run.ps1`(.NET SDK 9, 외부 NuGet 패키지 없음). 기존 자동 테스트 삭제 결정을 유지해 Assets 밖에서 실제 변환기 코드를 실행한다. `-Export`는 검증 통과 후 JSON을 재생성한다. 콘솔 JSON 직렬화는 System.Text.Json이므로 Unity JsonUtility·AssetDatabase·Resources 동작을 검증한 것은 아니다. 해당 csproj만 `.gitignore` 예외로 추가했다.
- 실행한 Unity 테스트 0개. Unity 메뉴 직접 호출·열린 에디터 임포트·플레이·Android 빌드·기기는 확인하지 않았다.
- 다음 구현: 기존 Player 차징 기반에 강공격 실행을 연결하고 이후 전략 효과·쿨타임·횟수·스테이지 초기화를 연결한다. 강공격 단계별 배율/시간 칸 부재, 최대 차징 0.8초 기준과 20013~20015의 0.85/0.9/0.95초 충돌, 취소 시 MP 정책, 전략 버프 중첩/재사용 규칙은 아직 미정이다. 임의로 확정하지 않는다. 기존 약공격의 키보드/마우스 표 경로와 버튼의 SkillData 경로 차이도 남아 있다.
- 사용자 직접 확인: Unity 컴파일을 기다린 뒤 엑셀 저장 → `Tools > Parse Combat Skill Table` → Console의 `전투 스킬 46개` 메시지 확인. Project에서 `Assets/Resources/Data/CombatSkillTable.json`을 열어 30001 재생 치유의 `EffectType: HealOverTime`, `TargetType: Self`, `EffectDuration: 5`, 20001 화염 망치의 `CrowdControl: Knockback`, `Radius: 2.2`, 20015의 `SuperArmor: true`를 확인한다. MainTestScene에서 Play From Title을 끄고 Play, 미장착 기본 상태에서 Enter/마우스 왼쪽을 눌러 불꽃 탄환과 MP 5 소모를 확인한다. 강공격·전략 효과는 이번 변경만으로 새로 실행되지 않는다. 실패하면 Console 첫 컴파일/변환 오류, 엑셀 저장 여부, JSON 생성 시각을 확인한다.

## 2026-11-04 재시작 컴파일 오류와 자동 테스트 삭제

- 시작 상태: `main`, HEAD `acb7aab`, 작업 트리 깨끗함. 사용자 보고는 에디터 재시작 후 다수 오류이며, 테스트용 씬 복제가 원인인지 확인 요청이었다.
- 원인: 씬을 로드하지 않는 C# 컴파일에서도 동일 오류 재현. `Player.cs`의 `partial` 누락(CS0260), `LightSkillCastTests`의 런타임 어셈블리 직접 참조 및 InputSystem 확장 메서드 using 누락으로 실패했다. 첫 오류를 고친 뒤에는 이전 약공격 버튼·Animator API와 PlayerCombat 생성자 연결 누락, Unity 6000.5에서 금지된 `GetInstanceID()` 호출 오류가 추가로 확인됐다. 확인된 컴파일 오류는 씬 복제에서 발생한 것이 아니다.
- 사용자 요청으로 `Assets/Tests` 전체와 `Assets/Tests.meta` 삭제: C# 26개, 테스트 asmdef 2개 및 관련 meta 포함. 씬·프리팹·asset에서 삭제 대상 GUID 참조 없음 확인. `Assets/0.Scripts`의 테스트용 게임 컴포넌트와 테스트 씬은 유지했다. 자동 회귀 테스트는 현재 저장소에서 제거된 상태이며, 필요하면 이 변경 전 Git 기록에서 복구해야 한다. 과거 문서의 테스트 통과 수는 당시 기록이며 현재 실행 가능 수를 뜻하지 않는다.
- 수정: Player의 partial 선언, 기존 직렬화 필드 `lightAttackButton`, `lightAttackProjectile`, `passiveProjectiles`와 버튼·Animator 콜백 연결을 이전 구현에서 복원. 기존 3인자 PlayerCombat 생성자와 투사체 바인딩 사용. 새 표 기반 `TryCastLightSkill` 경로는 유지했다. LightSkillFx의 중복 피격 식별은 `EntityId`/`GetEntityId()`로 교체했다. 씬·프리팹·Project Settings·패키지는 변경하지 않았다.
- 검증: 설치된 Unity 6000.5.0f1의 Roslyn과 Unity 생성 응답 파일을 사용해 수정 전 Assembly-CSharp/Framework.PlayModeTests 컴파일 실패 재현. 수정 후 Assembly-CSharp 및 Assembly-CSharp-Editor 각각 종료 코드 0, C# 오류·경고 0. 출력은 `.utmp/EditorRestart`로 격리하여 Library의 DLL을 덮어쓰지 않았다. `git diff --check` 통과. 실행한 Unity 테스트 0개. 두 번째 에디터를 열지 않았으며, 열린 에디터의 재임포트·Safe Mode 해제·실제 플레이·빌드·기기는 미확인이다.
- 다음 확인: Unity로 돌아가 임포트/컴파일을 기다린다. 갱신되지 않으면 `Assets > Refresh`를 실행한다. Safe Mode라면 오류가 사라진 뒤 `Exit Safe Mode`를 누른다. Console을 Clear하고 새 빨간 컴파일 오류가 없는지 확인한다. `MainTestScene` 또는 복제한 `GiungTestScene`을 열고 Play From Title을 끈 뒤 Play하여 이동과 Enter/마우스 왼쪽 약공격을 확인한다. MP가 충분하면 불꽃 탄환이 나가고 MP가 소모되며 잠금이 풀려야 한다. 오류가 다시 생기면 Console 첫 빨간 오류의 파일·행과 전체 메시지를 확인한다. 기존 버튼은 장착 SkillData의 ActiveConfigured 및 연결 프리팹·Animator 설정을 사용하는 별도 경로이므로 버튼 플레이 검증도 남아 있다.

## 2026-11-04 전투 수치 엑셀 변환

- 시작 상태: 약공격 21개의 피해·MP·범위는 `LightSkillCatalog` 배열에 적혀 있었고, `SkillDatatable.xlsx`와 따로 놀 수 있었다. 기존 `Tools/ParseExcel`은 `Data.SkillData` 필드 순서로 CSV만 읽어서 이 표에 붙일 수 없다.
- 완료: `Tools > Parse Combat Skill Table`이 프로젝트 루트의 `SkillDatatable.xlsx`에서 약·강·전략 시트와 `Area_Typetable`을 ID로 합쳐 `Assets/Resources/Data/CombatSkillTable.json`에 저장한다. 수식 ID는 저장된 값, `Reach`는 범위 시트의 길이, `Projecttile`은 Projectile이다. 약공격 시전은 이 JSON의 MP·범위·탄수·타격·기본 수치·계수를 읽는다. 그림 5종과 셰이더 색은 그대로다. 강공격·전략은 수치만 들어 있고 실행은 없다. 상태창용 `SkillData`는 바꾸지 않았다.
- 검증: 변환 결과 46줄(약 21, 강 15, 전략 10). 불꽃 탄환은 MP 5, 사거리 7, 폭 0.5, 기본 8, 계수 1.2. Unity 메뉴 재실행, 플레이, 테스트 러너는 실행하지 않음.
- 다음 확인: 엑셀을 저장한 뒤 Unity 메뉴 `Tools > Parse Combat Skill Table`을 누른다. Console에 전투 스킬 46개가 보여야 한다. 이어서 `MainTestScene`에서 Play From Title을 끄고 Play한 뒤 적 앞에서 마우스 왼쪽 또는 Enter를 누른다. MP가 5 줄고 불꽃 탄환이 날아가면 정상이다. Console에 `CombatSkillTable이 없습니다`가 보이면 메뉴를 다시 실행하고, 그림이 분홍 네모면 `LightSkills/LightSkillGlow` 셰이더가 `AllIn1Urp2dRenderer`인지 본다.

## 2026-11-04 약공격 인게임 시전

- 시작 상태: 약공격 표 수치는 있었고, 버튼 시전과 이펙트는 없었다. 약공격 잠금과 MP 차감 API만 있었다.
- 완료: 마우스 왼쪽, Enter, 게임패드 서쪽 버튼의 Attack 입력으로 장착된 약공격을 시전한다. 장착이 없으면 불꽃 탄환(10001)을 쓴다. 비용은 표의 MP, 피해는 기본 수치 + 공격력 × 계수다. 시전 중에는 기존 약공격 잠금이 이동·회피·막기·다음 약공격을 막는다. 자동 탄환은 그대로 따로 나간다.
- 완료: PixelLab 이펙트 5종을 `Assets/Resources/LightSkills`에 넣었다. 탄환 `lightfirebolt`, 원 `lightfireblast`, 직선 `lightthrust`, 부채꼴·호 `lightslash`, 고리 `lightflamering`. Point 필터. `LightSkillGlow`는 `AllIn1SpriteShader/AllIn1Urp2dRenderer`의 Glow와 HSV로 속성 색을 입힌다.
- 검증: 표 수치와 시트 프레임 수를 읽었다. Unity 플레이와 테스트 러너는 실행하지 않음.
- 다음 확인: MainTestScene에서 Play From Title을 끄고 Play한다. 적 앞에서 마우스 왼쪽 또는 Enter를 누른다. MP가 5 줄고 불꽃 탄환이 조준 방향으로 날아가 맞으면 적이 피해를 입는다. 시전 순간에 이동·회피·막기가 안 되고, 잠시 뒤 다시 되면 정상이다. UI 위 클릭은 나가지 않는다. 그림이 분홍 네모면 Console의 셰이더 오류와 `LightSkills/LightSkillGlow` 셰이더가 `AllIn1Urp2dRenderer`인지 본다. 그림이 흐리면 해당 PNG의 Filter Mode가 Point인지 본다.

## 2026-11-04 보스 피격 범위 VFX

- 시작 상태: 보스 스와이프 그림은 피격 부채꼴보다 작고, 빨간 범위 표시는 `BossPatternRangeView`에서 꺼져 있었다. 베기 판정은 선입력 때 고정된 방향의 `slashRange`·`slashAngle`이고, 돌진 판정은 도달한 순간의 플레이어 위치에 `dashEndDamageRadius` 원이다.
- 완료: `BossAttackHitVfx`가 All In 1 URP 2D 셰이더로 그 판정 모양을 그린다. 베기는 스와이프가 시작된 뒤 끝날 때까지 방향이 고정되고, 돌진 원은 타격 순간 0.2초 동안 사라진다. 부모 스케일을 나눠 월드 지름이 반지름의 두 배가 된다. 데미지 계산과 애니메이션 클립은 바꾸지 않았다. 디버그 범위 표시는 그대로 꺼 두었다.
- 연결: `BossMvp` 프리팹. 머티리얼 `Assets/1.Prefabs/Materials/BossAttackHitVfx.mat`는 `RADIALCLIPPING_ON`, `GLOW_ON`. `MainTestScene`의 보스 스케일 4, 베기 반지름 6, 각도 120은 씬 덮어쓰기라 그대로다. 그 씬의 `landingDamageRadius`, `leapDuration`은 현재 필드와 이름이 달라 돌진 반지름은 프리팹 값 2다.
- 검증: 크기 계산 테스트 `BossAttackHitVfxShapeTests`를 추가했다. Unity가 이미 열려 있어 테스트 러너는 실행하지 않음. 에디터 재생, 화면, 빌드, 기기는 확인하지 않음.
- 다음 확인: Unity 임포트가 끝난 뒤 `MainTestScene`을 재생한다. 보스가 가까이서 베면 몸 앞에 주황 부채꼴이 스와이프 동안 유지되고, 플레이어가 움직여도 방향은 그대로여야 한다. 멀리서 돌진해 닿으면 플레이어 자리에 원이 잠깐 보였다가 사라진다. 부채꼴이 안 보이면 Console에 머티리얼 오류가 있는지, 머티리얼 셰이더가 `AllIn1SpriteShader/AllIn1Urp2dRenderer`인지 본다.

## 2026-11-04 강공격 20001~20005와 전략 스킬 수치

- 시작 상태: `main`. HeavySkilltable 20001~20005는 이름·등급·불 속성만 있고, UtilitySkilltable 30001~30010은 이름·속성·자원 Count만 있었다. 20006~20015와 약공격 수치는 이미 채워져 있었다.
- 완료: 20001 화염 망치~20005 운석검에 MP, 쿨타임 0, Charge, 차징 0.5~0.8초, 사거리, 범위, 타격, 기본 수치, ATK 계수, 슈퍼아머, 넉백, 설명을 넣었다. 같은 등급의 20006~20015 위력대에 맞췄고, 기획서의 강공격 최대 차징 0.8초를 넘기지 않았다. 20006~20015는 바꾸지 않았다.
- 완료: 전략 10개에 소모 횟수 1, 쿨타임 6초, Instant, 시전 0초, 효과 종류, 기본 수치, 계수, 지속 시간, 설명을 넣었다. 기획서의 초기 기준은 사용마다 횟수 1, 스테이지당 5회, 쿨타임 6초다. 횟수 상한 칸은 표에 없어 설명에 적었다. 회복·방어막·증가량은 플레이어 최대 체력·마력 비율이다. 등급은 전략 스킬에 없다는 기존 정리를 유지해 비워 두었다.
- 완료: Area_Typetable 34~39행에 화염 망치, 화염 돌진, 대폭발, 화염 폭풍, 운석검, 불타는 갑주의 범위 수치를 추가했다. 자기 자신에게만 쓰는 전략 스킬은 Point라 반경 칸이 없어 범위 표에는 넣지 않았다.
- 수정: 바람의 가호 영문 `Blessing of Wind)`의 닫는 괄호를 뺐다.
- 그대로 빈 칸: 아이콘, 애니메이션, 이펙트 프리팹, 사운드. 상태이상 ID는 None. 차징 1·2·3단계 배율 칸은 이 표에 없다.
- 검증: 저장 파일에서 20001~20005, 30001~30010, 범위 34~39행의 아이디·비용·기본 수치·계수를 다시 읽었다. 20006 기본 수치 14와 20015, 아이디 수식은 유지됐다. Unity 테스트 0개. 전투 로딩은 확인하지 않음.
- 다음 확인: HeavySkilltable 3~7행, UtilitySkilltable 3~12행, Area_Typetable 34~39행. 엑셀을 저장 전에 열어 두었다면 닫을 때 덮어쓰지 말고 다시 연다.

## 2026-11-04 스킬 개요 PPT

- 완료: `docs/스킬_개요.pptx` 12장. 레어도 테두리 7장 뒤에, 약공격 21·강공격 15는 해당 등급 테두리 안에 문양을 넣은 모습, 전략 10개는 등급이 없어 문양만 넣었다. 이 합성 그림은 PPT에만 있고 프로젝트 아이콘 파일로 추가하지 않았다.
- 검증: 슬라이드 7장과 표 헤더를 읽었다. PowerPoint에서 화면 재생은 확인하지 않음.

## 2026-11-04 스킬 아이콘

- 시작 상태: 스킬 표의 아이콘 경로는 비어 있었다. 상태창은 `SkillData.IconLabel`이 있고 미리 로드된 주소가 있을 때만 그림을 그린다.
- 완료: 스킬별 아이콘 46장은 삭제했다. 레어도 테두리는 등급당 1장, 총 7장이다. `Assets/2.Resources/Art/SkillIcons/SkillFrame_{Common,Uncommon,Rare,Epic,Legendary,Mythic,Fusion}.png`. 64×64, 속은 투명, Point 필터. `Skill_Rarity`의 Rarity_Frame_Res와 Rarity_Color_Hex에 경로와 색을 넣었다. 스킬 표의 Skill_Icon 경로는 비웠다.
- 검증: 46장 연락표를 눈으로 확인했다. Unity 임포트와 상태창 표시는 확인하지 않음. Addressables 그룹은 비어 있어 아이콘 주소를 넣지 않았다.
- 다음 확인: Unity가 임포트를 끝내면 Project의 `Art/SkillIcons`에서 그림이 보이는지 본다. 상태창에 그림이 없고 이름만 보이면 아이콘 주소가 아직 사전 로딩되지 않은 것이다.

## 2026-11-04 Area_Typetable

- 시작 상태: `Area_Typetable` 시트만 있고 칸은 비어 있었다. `AreaType`의 관련 수치 목차는 반경, 각도, 너비, 길이 네 가지다.
- 완료: 그 네 목차와 스킬 아이디·이름·범위 모양을 넣고, 범위가 이미 있는 약공격 21개와 강공격 10개(20006~20015)를 기입했다. 모양에 쓰는 칸만 숫자를 넣었다. 투사체·직선은 길이와 너비, 원·기둥은 반경, 부채꼴·호는 반경과 각도, 고리는 반경과 너비, 십자는 길이와 너비다. 불꽃 탄환의 사거리 칸은 단어 Reach라 길이는 같은 기본 탄환과 같이 7로 두었다. 강공격 20001~20005와 전략 스킬은 범위 모양이 없어 넣지 않았다.
- 검증: 저장 파일에서 헤더와 31행의 아이디·모양·해당 수치를 다시 읽었다. Unity 테스트 0개.
- 다음 확인: 엑셀 `Area_Typetable` 1행에 반경·각도·너비·길이가 있고, 3행 불꽃 탄환부터 33행 태양 붕괴까지 이어지면 된다.

## 2026-11-04 약공격 10002 이후 수치

- 시작 상태: `main`. 열린 `SkillDatatable.xlsx`에서 10002~10007과 10018~10021은 이름·등급·속성만 있고 전투 수치가 없었다.
- 완료: 그 10행에 MP, 쿨타임 0, Instant, 사거리, 범위, 투사체, 타격, 기본 수치, ATK 계수, 설명을 채웠다. 10001 불꽃 탄환과 10008~10017, 강공격 표는 수치를 바꾸지 않았다. 아이콘·애니메이션·프리팹·사운드는 그대로 비어 있다.
- 검증: 저장 후 엑셀에서 10행의 ID·MP·기본 수치·계수를 다시 읽었다. Unity 테스트 0개. 전투 로딩은 확인하지 않음.
- 다음 확인: LightSkilltable 4~9행과 20~23행. 강공격 20001~20005는 여전히 수치가 없다.

## 2026-11-03 불속성 스킬 20개 추가

- 시작 상태: `main`, HEAD `99a471d`. `SkillDatatable.xlsx`는 엑셀에서 열려 있었고, 약공격 불꽃 탄환만 전투 수치가 있었다. 약공격 불 스킬 7개, 강공격 불 스킬 5개.
- 완료: 열린 통합 문서에 저장했다. LightSkilltable 10008~10017에 불속성 약공격 10개, HeavySkilltable 20006~20015에 불속성 강공격 10개를 이름·등급·MP·쿨타임·시전·사거리·범위·타격·기본 수치·계수·설명까지 넣었다. 약공격은 쿨타임 0, Instant. 강공격은 쿨타임 0, Charge, 차징 0.45~0.95초. 아이콘·애니메이션·프리팹·사운드는 비워 두었다. 기존 불꽃 탄환~태양 검, 화염 망치~운석검의 빈 수치는 바꾸지 않았다.
- 배치: 약공격 새 행은 태양 검 다음이다. 얼음 탄환·바람 화살·암석포·전기 화살은 21~23행이 아니라 20~23행으로 내려갔고, ID 수식이 10018~10021을 붙였다. 이름 없는 10008행의 Mystic 칸은 열풍 탄이 대신한다. FusionSkilltable은 그대로 태양 검·화염 강타·플레어 탄환을 가리킨다.
- 검증: 엑셀에서 새 20행의 ID·이름·MP·기본 수치·계수를 다시 읽었다. Unity 테스트 0개. 게임 로딩과 전투는 확인하지 않음.
- 다음 확인: 열린 엑셀의 LightSkilltable 10~19행, HeavySkilltable 8~17행을 본다. 저장 후 다시 열어도 같은 이름이면 반영된 것이다. 이 표는 아직 전투 실행에 연결되어 있지 않다.

## 2026-11-03 body-charge 주먹 이펙트

- 시작 상태: `main`, HEAD `99a471d`. `body-charge/east` 0~7.png는 사용자가 주먹의 붉은 이펙트를 지운 상태였다.
- 완료: 8방향 `Idle/animations/body-charge`에서 주먹 밖으로 뻗은 붉은 선을 제거했다. 동쪽 0~7은 사용자 수정이라 그대로 두었고, 동쪽 8~10과 나머지 7방향 0~10을 처리했다. 북서 7~10, 남동 6은 이펙트가 없어 픽셀을 바꾸지 않았다. 어깨·등·가슴·허리·무릎의 빨간 장식은 남겼다. 이펙트가 몸을 덮던 자리는 주변 피부색으로 메웠다. 캔버스 188×188, RGBA 88장 유지.
- 남서 재처리: 첫 통과는 남서의 어두운 붉은 선을 남겼다. `south-west/0.png`~`10.png` 11장을 다시 지웠다. 빈 공간으로 뻗은 붉은 픽셀은 프레임 0의 2개를 빼고 0이다. 어깨 원, 무릎, 팔·가슴의 붉은 상처 선은 남겼다.
- 검증: 남서 0·4·8 프레임을 확대해 주먹 바깥 별 모양 선이 줄었는지 확인. Unity 테스트 0개. 에디터 재생은 확인하지 않음.
- 다음 확인: Unity에서 `body-charge/south-west`를 우클릭해 Reimport한 뒤 0~10 프레임을 본다. 주먹 밖으로 뻗은 붉은 선이 없고 어깨·무릎 장식은 남아 있으면 정상이다. 예전 그림이면 Console 임포트 오류를 확인한다.
- 씬, 프리팹, 코드, 메타는 변경하지 않음.

## 2026-11-03 Aseprite 소스 빌드

- 게임 저장소 파일은 이 작업으로 바꾸지 않음. 실행 파일은 `C:\Users\User\source\aseprite\build\bin\aseprite.exe`.
- 완료: 태그 `v1.3.18.6`, Skia `m124-08a5439a6b` Windows Release x64, Visual Studio 2022의 `vcvars64`와 Ninja로 1562개 타깃 링크. `aseprite -b --version` 결과는 `Aseprite 1.3.18.6-dev`.
- 다음 확인: 그 exe를 더블클릭해 창이 열리는지 본다. PATH에는 넣지 않음.

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
