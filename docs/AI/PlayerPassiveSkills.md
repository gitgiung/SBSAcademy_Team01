# 보유 스킬 패시브 자동 발사

## 직접 확인

1. Unity 컴파일 완료 후 Console을 확인한다. Tools > Play From Title을 끄고 MainTestScene을 직접 Play한다.
2. 버튼을 누르지 않고 화면 안의 적에게 접근한다. 사거리6 안에 유효한 적이 있으면 첫 빨간 원을 발사하고, 이후 전투 시간3초 간격으로 발사한다. 피해6, 탄속3이며 MP를 소모하지 않는다. 대상이 없으면 발사하지 않는다.
3. 적 둘 사이를 이동한다. 다음 탄환은 그 발사 순간 가장 가까운 적을 조준한다. 이미 발사한 탄환은 적이 움직이거나 죽어도 꺾이지 않는다. 화면 밖의 적은 새 발사의 표적으로 삼지 않는다.
4. N_Attack을 눌러 약공격을 사용한다. 약공격 MP10/피해10은 유지되며 패시브의3초 주기와 별개다. 약공격 애니메이션 동안 입력은 차단되고 패시브는 계속 동작한다.
5. 패링/가드 모션 중 패시브 발사와 남은 주기가 정지한다. 모션 종료 후 남은 주기를 이어간다. 일시정지 팝업에서도 주기와 탄환 이동이 정지하고 뒤로가기로 재개된다.
6. 일시정지 상태창의 보유 공격 스킬 첫 칸에 불꽃 탄환이 표시되는지 확인한다. 버튼 장착과 무관하게 보유한 공격 스킬이 패시브 대상이다. 현재 획득/제거 UI는 없으며 이후 획득 담당이 Loadout.TryAddAttackSkill/RemoveSkill을 호출하면 발사 목록에 반영된다.
7. PC 이동 테스트는 PlayerController2D를 비활성화한다. 이번 변경 후 Player.bulletPrefab은 기존 참조 보존용이며 자동 발사를 제어하지 않는다. 패시브를 잠시 끄려면 Play 중 PlayerData > Loadout > Attack Skills > 첫 항목의 PassiveConfigured를 끈다. 테스트 후 저장하지 않는다.

오류 시 Console 첫 오류, Player > Passive Projectiles의 Key=FlameBullet와 Prefab=FlameBullet, PlayerData의10001 보유 및 PassiveConfigured/PassivePrefabKey/PassiveDamage6/PassiveInterval3/PassiveSpeed3/PassiveRange6을 확인한다. 적의 활성 Collider2D, Enemy 태그, MainCamera 태그·화면·거리도 확인한다. 설정하지 않은 다른 스킬은 자동으로 임시탄을 발사하지 않는다.

## 구조와 확장

- 기존 PlayerCombat이 최대8개 보유 공격 슬롯의 타이머를 관리한다. 정의 데이터에 타이머를 기록하지 않고 플레이어끼리 실행 상태를 공유하지 않는다. 제거된 슬롯은 다음 Tick에서 중단하고 교체된 정의는 새 주기로 시작한다. Player 비활성화 시 실행 상태를 초기화한다.
- Data.SkillData에 PassiveConfigured/PassivePrefabKey/PassiveDamage/PassiveInterval/PassiveSpeed/PassiveRange를 추가했다. ActiveConfigured/MPCost/BaseDamage 등 기존 액티브 필드와 독립적이다. 유효하지 않은 수치, 주기0, 사거리0, 누락 프리팹이나 Bullet 컴포넌트는 실행하지 않는다.
- Player의 Passive Projectiles는 씬/프리팹에 준비된 직접 참조를 Key로 바인딩한다. 없으면 기존 Managers.Resource.Load<GameObject>(key)의 준비된 캐시를 조회한다. 발사 중 새 Addressables 로딩이나 Resources.Load를 시작하지 않는다. 정식 데이터/리소스가 생기면 기존 PrepareGame/Preload 계약에 연결한다.
- MainTestScene의 불꽃 탄환10001만 사용자 승인 수치로 설정했다. 기본 자동탄 경로를 보유 패시브로 교체하여 중복 기본탄을 발사하지 않는다. PlayerData의 기존 기본 공격 수치와 Player.bulletPrefab 직렬화 참조는 보존한다.
- 기존 Managers.Pool/Bullet.InitDirection을 그대로 사용한다. 탄환 비행·수명·적중 반환 규칙은 변경하지 않았다. 다른 스킬의 실제 효과·획득/성장·강공격·전략·미정 그로기는 후속이다.

## 자동 확인

Tools > Play From Title 비활성화 → Window > General > Test Runner → PlayMode → PlayerPassiveSkillTests → Run Selected. 독립 주기·제거·장착 불필요·미설정/잘못된 값·무소모·새 표적/직진·화면 밖·정지·비활성화 및 실제 Player/빨간 프리팹/물리 피해를 확인한다. 별도 검증 프로젝트 결과와 한계는 Handoff 최신 항목을 따른다.
