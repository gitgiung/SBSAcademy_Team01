# Player Bullet Pooling Implementation Plan

> **For agentic workers:** Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 플레이어의 실제 Bullet 발사·회수를 기존 풀에 연결하고 재사용 상태를 검증한다.

**Architecture:** PlayerCombat은 Managers.Pool.Pop의 활성화 전 콜백에서 기존 Bullet.Init을 호출한다. Bullet은 IPoolable을 구현하여 대여별 상태를 초기화하고 충돌·수명·화면 이탈 시 PooledObject.ReturnToPool을 사용한다. 직선 이동과 기존 공개 Init/직렬화 필드는 유지한다.

**Tech Stack:** Unity 6000.5.0f1, 기존 PoolManager, NUnit PlayMode tests.

**Spec:** docs/superpowers/specs/2026-11-03-player-skills-design.md. 이번 승인은 1번 탄환 풀링이며 MP·스킬·화면 기반 타깃 선정은 제외한다.

## Global Constraints

- 사용자 MainTestScene 및 Office 잠금 파일 보존. 씬·프리팹·기존 meta·패키지 변경 없음.
- 새 전역 매니저·런타임 asmdef를 만들지 않는다. 기존 Bullet.Init(Transform, float, float)와 DeleteTime 필드 유지.
- 원본 프로젝트를 두 번째 Unity로 열지 않는다. 임시 짧은 경로에 코드·테스트·패키지·설정 검증 복사본을 만들고 PlayFromTitle 배치 guard는 복사본에만 둔다.
- 이미 발사된 직선탄은 표적 참조를 따라가지 않는다. 표적 소멸로 수명을 끝내던 로직은 풀 재사용된 적 참조의 영향도 피하도록 제거한다.

## Review Focus

- 초기화가 Awake보다 먼저 호출될 때 첫 발사 정보 유지.
- 적중 후 동일 인스턴스를 재대여해도 다시 한 번 정상 피해 적용.
- 이전 대여의 수명이나 예약 Destroy가 다음 대여를 제거하지 않음.
- 중복 반환 및 반환 직후 늦은 충돌로 중복 피해 없음.
- 카메라 부재·표적 소멸 중에도 수명 반환이 정상 동작.

## Task 1: 발사와 Bullet 수명 연결

**Files:** Modify Assets/0.Scripts/Game/Player/PlayerCombat.cs, Assets/0.Scripts/Game/Player/Bullet.cs. Create Assets/Tests/PlayMode/PlayerBulletPoolingTests.cs 및 meta.

**Interfaces:** Bullet.Init(Transform target, float damage, float speed) 유지. Bullet.OnPoolRent()/OnPoolReturn()는 IPoolable 계약. PoolManager.Pop(GameObject, Vector3, Quaternion, Transform, Action<GameObject>) 및 PooledObject.ReturnToPool(uint) 재사용.

- [x] 테스트 먼저 작성: 실제 PlayerCombat 발사 시 공유 풀 활성 수 1, 충돌 2회에도 피해 1회 및 비활성 수 1, 재대여 동일 객체가 다시 피해 적용, 수명 만료 후 객체 생존·비활성화, 재대여 후 이전 수명 영향 없음, 화면 밖 반환, 표적 소멸 후 직진, 카메라 없는 수명 반환.
- [x] 검증 복사본 Unity -runTests -testPlatform PlayMode -testFilter PlayerBulletPoolingTests로 기존 코드 실패 확인. 컴파일·라이선스 문제는 기능 실패로 세지 않는다.
- [x] Pop 초기화 콜백으로 발사 전환. Bullet은 수명 카운트다운, 대여/반환 상태 초기화 및 단일 반환 경로 구현. 피해 콜백 재진입에 대비해 충돌 당시 대여 버전으로 반환한다.
- [x] 같은 테스트를 다시 실행해 통과 확인하고 CoreInitializationTests/PooledEnemySpawningTests 회귀를 실행한다. 씬 자산이 없는 최소 복사본에서 전체 씬 테스트 통과를 주장하지 않는다.
- [x] git diff --check, 사용자 씬 해시 보존 확인, Handoff 갱신. 커밋·브랜치 전환은 수행하지 않는다.

## 직접 확인

MainTestScene → Play → 자동 발사 → Hierarchy의 @Managers/@Pools 아래 Bullet 풀에서 적중/이탈 후 비활성 객체가 남고 다음 발사에 다시 활성화되는지 확인한다. 오류 시 Console, Player.bulletPrefab 참조, Bullet/PooledObject 컴포넌트와 카메라 MainCamera 태그를 확인한다.
