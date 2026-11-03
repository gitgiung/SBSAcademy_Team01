# 첫 약공격 불꽃 탄환

## 직접 확인

1. Unity 컴파일 완료 후 Console의 새 오류를 확인한다. MainTestScene을 직접 열려면 Tools > Play From Title 옵션을 끄고 Play한다.
2. 첫 보유 슬롯에는 승인된 검증 스킬 불꽃 탄환10001이 들어 있고 약공격 버튼에 장착되어 있다. 일시정지 상태창의 공격 스킬 첫 칸에서도 이름을 확인할 수 있다. 다른 씬/PlayerData 기본값에 자동 지급하는 코드는 없다.
3. 모바일 UI의 **N_Attack**을 누른다. PC에서는 이동 후 월드 영역에서 마우스 왼쪽 버튼을 누르거나, UI에 선택이 없는 상태에서 기존 Attack 액션의 Enter를 사용한다. 게임패드는 기존 Attack 버튼을 따른다. UI를 클릭하는 것은 월드 공격 입력으로 중복 처리하지 않는다.
4. 현재 이동 입력 방향으로 빨간 원이 직선 발사된다. 중립이면 마지막으로 바라본 방향이다. 마우스 위치 조준이나 유도탄은 아니다. 사격 순간 MP10을 한 번 차감하고 피해10을 적용한다.
5. 8방향 Attack 모션 동안 이동·약공격·방어·회피 입력이 거절되고 종료 후 조작이 복구된다. 잠금 중 눌렀던 공격/방어/회피를 종료 후 예약 실행하지 않는다. 이동은 해제 후 현재 입력으로 재개한다. 별도 약공격 쿨타임은 없다. 현재 클립은9프레임/12fps, Unity 조회 길이0.75초의 초기 연출값이며 종료는 Animator 상태 종료를 따른다.
6. MP가10보다 작으면 시전·차감이 없어야 한다. 현재 씬의 최대MP100·자연회복1/s는 기존 설정을 유지했으므로 시전 후 서서히 회복되는 것이 정상이다. MP 부족 점멸은 아직 후속이다.
7. 애니메이션 중 일시정지/뒤로가기로 복귀하면 남은 동작을 이어서 끝낸다. 사망·Player 비활성화·Animator 중단 때는 잠금이 남지 않아야 한다. 시작 전 리소스/애니메이션이 무효하면 무소모·미발동이고, 비용 알림 중 시작이 취소돼도 비용을 복구한다.
8. PC 이동을 확인할 때 PlayerController2D를 비활성화해 조이스틱과 겹치지 않게 한다. 후속 보유 패시브 연결로 자동 탄환도 빨간 원을 사용한다. 약공격만 관찰하려면 Play 중 첫 보유 스킬의 PassiveConfigured를 잠시 끈다. Player.bulletPrefab을 비우는 방법은 더 이상 자동 발사를 끄지 않는다. 임시 설정은 저장하지 않는다. 패시브 수치와 확인 순서는 PlayerPassiveSkills.md를 따른다.

오류가 나면 Console 첫 오류, Player의 Light Attack Button=N_Attack/Button 참조, Light Attack Projectile=FlameBullet, PlayerData.Loadout의10001/Light장착, ActiveConfigured/MPCost/BaseDamage/AnimName을 확인한다. Animator에는 LightAttack 상태와 LightAttackAnimationState Behaviour, Idle 복귀 전이가 있어야 한다. 기존 Animator 전체를 삭제/재생성할 필요는 없다.

자동 테스트는 Tools > Play From Title 비활성화 → Window > General > Test Runner → PlayMode → PlayerLightSkillTests → Run Selected. 실제 시전·종료·부족/중복·N_Attack/PC입력·정지·사망·Animator중단·MP알림 재진입·스냅샷·UI 첫 클릭·8방향/씬 연결을 검증한다.

## 구조

- Data.SkillData의 ActiveConfigured/MPCost/BaseDamage를 추가하고 기존 DamageMultiplier를 사용한다. 현재 피해는 BaseDamage + PlayerData.attackDamage × DamageMultiplier다. 불꽃 탄환은 BaseDamage10/Multiplier0이므로 기본 자동 공격력과 별개로10이다. 메타데이터만 있는 스킬은 실행하지 않는다.
- PlayerLightSkill은 플레이어 한 명의 현재 약공격 실행 ID와 방향·피해·속도·타이밍 스냅샷을 소유한다. 기존 PlayerActionGate/TryBeginLightAttack으로 MP와 잠금을 처리한다. 새 전역 스킬 매니저는 없다.
- PlayerView가 기존 Animator에 LightAttack을 재생한다. LightAttackAnimationState가 실제 상태의 시작/진행/종료를 같은 실행 ID로 전달한다. 과거 종료 알림은 새 실행을 해제하지 않는다.
- N_Attack에 런타임 PlayerLightAttackButton을 연결해 PointerDown/Submit을 공통 TryUseLightSkill API로 전달한다. 기존 Button.onClick을 시전 예약으로 사용하지 않는다. PC는 기존 Player/Attack을 선택 복제하며 InputAction 에셋을 바꾸지 않는다.
- Bullet.InitDirection을 추가하고 기존 Init(Transform,float,float)은 유지한다. 새 FlameBullet 프리팹은 기존 Bullet·CircleCollider2D·Rigidbody2D를 재사용하고 Mesh 대신 빨간 Sprite를 표시한다. 수명·적중·화면 이탈 반환은 기존 풀 계약이다. 원본 Bullet.prefab은 변경하지 않았다.

## 리소스 및 후속

- Assets/Resources/PlayerAnimator의 기존 Controller에 LightAttack 상태/방향 트리/Behaviour/Idle 전이만 추가했다. 기존20개 객체와 Controller meta/GUID를 보존했다. LightAttack_*.anim8개를 새로 추가했다.
- Assets/Resources/SkillProjectiles/FlameBullet.prefab과 FlameBulletCircle.png는 승인된 임시 빨간 원 리소스다. 기존 자동 탄환 프리팹과 구분한다.
- Tools > Player Skill > Build Flame Bullet Resources는 기존 상태를 보존하며 이 새 리소스를 준비하는 Editor 도구다. 관계없는 같은 이름 상태는 덮어쓰지 않는다. 명시적 기존 Player Animation Generate 작업에도 약공격 구성을 포함하도록 연결했다.
- 현재 Player.lightAttackProjectile은 첫 검증 스킬용 직접 참조다. 여러 스킬별 프리팹/캐스팅 사운드/아이콘/효과를 실제 테이블·리소스 로딩에 연결하는 것은 후속이다.
- SkillDatatable.xlsx 원본은 수정하지 않았다. 승인한 MP10·피해10은 현재 MainTestScene 검증 설정에 반영했다. 보유 패시브 발사는 후속 작업에서 연결했다(PlayerPassiveSkills.md). 일반 획득·성장·다른 약공격·강공격·전략·그로기는 후속이다.

사용자가 요청한 병합 전체 선행 검증은 생략하고 변경 관련 컴파일/회귀를 수행했다. 실제 원본 씬 플레이·보스 전체 통합·Android 빌드/기기는 별도 확인 대상이다.
