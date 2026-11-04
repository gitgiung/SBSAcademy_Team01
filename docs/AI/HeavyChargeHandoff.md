# 강공격 차징 작업 인수인계

작성일: 2026-11-03. 이전 세션의 미완성 초안에서 이어 **승인된 차징 기반 구현을 완료**했다. 실제 강공격 피해/정식 스킬 실행을 완료했다는 의미는 아니다.

## 현재 상태

- 브랜치 `feat/PlayerSkill`, HEAD `526aaf6` (`강공격 작업중`). 시작 트리는 깨끗했으며 이전 차징 초안이 이 커밋에 포함되어 있었다. 현재 마무리 변경은 미커밋이다. 브랜치 전환/커밋 없음. 다음 세션에서도 Git 상태를 새로 확인한다.
- 빠졌던 `PlayerView.SetHeavyChargeIndicator` 구현, MainTestScene C_Attack 연결과 승인 수치 설정, Unity가 생성한 런타임3/테스트2 meta 반영까지 완료했다.
- 피격·방어·회피·비활성화·포커스·일시정지 연결, 모바일 raw 조준/이동 차단, 약공격/패시브 차단·복귀를 테스트했다.
- 리뷰에서 발견한 정지 Q해제, 동시 방어/키해제 순서, 포커스 취소 후 포인터 예약, 중립 조준 각도 보존 문제4개를 RED→GREEN으로 수정했다. 새로운 조준 화살표는 재사용·숨김·Material 해제를 구현했다.

## 확정된 요구와 범위

- 테이블보다 차징 기반을 먼저 만든다. 새 스킬 ID·피해량·정식 테스트 테이블을 만들지 않는다.
- 차징 중 이동 차단, 현재 조이스틱/이동 키로 방향만 변경한다. 중립은 마지막 실제 조준 각도를 유지한다. 최대 단계에서도 홀드하고 놓을 때 결과를 확정한다.
- MP는 시작 순간 한 번 차감한다. 방어/회피/피격 취소에 반환하지 않는다. 동기 MP 알림 중 비활성화/사망/정지되어 실제 시작하지 못한 비용만 복구한다.
- 승인된 테스트 수치: MP10, 단계 진입0/.4/.8초, 누적 배율1.1/1.2/1.7. StageBonuses는 증분.1/.1/.5다. Inspector에서 수정 가능하며 최종 밸런스가 아니다.
- 차징 중 약공격과 자동 패시브의 다음 발사를 차단한다. 이미 날아간 탄환은 유지한다. 실제 시작한 방어/회피는 차징을 취소하고 실패한 회피는 유지한다. 같은 PC 프레임에서는 실제 방어/회피 시작을 키 해제보다 먼저 처리한다.
- SuperArmor=false에서 실제 HP 감소 시 취소한다. true면 피해는 받되 유지하고, 사망·비활성화는 항상 정리한다. 별도 피격 경직 시스템은 구현하지 않았다.
- 팝업 일시정지 중 시간/표시 유지, 정지 중 해제나 포커스 상실/앱 일시중단은 무환불 취소한다. 복귀 후 공격을 예약하지 않는다.
- 결과는 `HeavyChargeReleased`와 실행 ID/단계/배율/방향/시간 스냅샷이다. 실제 피해·공격 애니메이션·사운드/햅틱·그로기는 후속이다.
- 모바일 C_Attack 사용. PC는 기존 Player/HeavyAttack이 있으면 복제하고 없으면 임시 Q 입력을 생성한다. Configured가 꺼진 씬에서는 PC 차징 입력을 생성하지 않는다. 원본 InputAction 에셋은 변경하지 않았다.

## 주요 파일과 API

- `PlayerHeavyCharge.cs`: 직렬화 설정, readonly HeavyChargeRelease, 플레이어별 차징 엔진. 설정 배열을 시작 전 복제하고 비용1회/실행ID/최대3단계/중복·오래된 해제를 처리한다.
- `Player.HeavyCharge.cs`: Player partial 필드/상태/API/입력·포커스 정리. TryBeginHeavyCharge(), TryReleaseHeavyCharge(id), CancelHeavyCharge(id), HeavyChargeReleased, LastHeavyCharge를 사용한다.
- `Player.cs`: 기존 행동·피격·수명 연결. CanMove와 CanAcceptGameplayInput을 구분하고 실제 방어/회피를 키 해제보다 먼저 처리한다.
- `PlayerHeavyAttackButton.cs`: pointerId와 실행ID를 검증한다. 이전 소유 실행이 취소된 경우 새 down을 허용하고 이전 다른 포인터의 늦은 up은 무시한다.
- `PlayerView.cs`: 임시 청록/노랑/주황 LineRenderer 조준 화살표. 길이는 진행 표시이며 공격 범위가 아니다.
- `Mobile/PlayerController2D.cs`: AimDirection은 raw joystick, MovementDirection은 실제 이동 입력이다. 차징 중 물리는 멈추고 조준은 허용한다.
- MainTestScene Player: heavyAttackButton=Button452539071(C_Attack), 승인 설정13줄만 추가. 기존 GUID·나머지 직렬화 참조 유지.

## 실제 검증

- Unity6000.5.0f1 별도 검증본에서 C# 컴파일 성공.
- 차징 단위12 + 통합20 = **32/32 통과**, 실패/생략0.
- 전체 PlayMode **249개 중231통과/18실패/생략0**. 수정 전 HEAD 입력 코드(완성 View는 컴파일을 위해 유지) 비교는227통과/22실패였다. 차징 회귀4개만 통과로 바뀌고 기존18개는 동일하다.
- 기존 실패는 GiungTestScene InputAction 참조 누락16개, 전체 실행의 기존 카메라·탄환/포커스 간섭2개다. 상세 테스트명/메시지와 비교 조건은 `HeavyChargeValidation.md`에 기록했다. 전체 프로젝트가 전부 통과했다고 보고하지 않는다.
- 별도 전체 자산 검증본에서 MainTestScene을 실제 PlayMode 로드해 직렬화된 C_Attack의 PointerDown/Up → MP10 차감 → 3단계/1.7배 해제 → 표시 숨김을 확인했다. 씬 연결 smoke **1/1 통과**. 원본 에디터 수동 조작/실제 터치 검증은 아니다.
- 그래픽 사용 검증 Unity의 세 단계 화살표 렌더1/1 통과, PNG 직접 확인. 독립 리뷰4건 모두 실패 재현·수정·재검증했으며 보류 지적 없음.
- 임시 검증본은 `.utmp/v`, 증거는 `.utmp/HeavyCharge`다. Git 제외 생성물이며 이 문서와 검증 문서가 영속 요약이다. 기존 사용자 Unity 프로젝트를 두 번째 에디터로 열지 않았다. 검증본 PlayFromTitle에만 batch guard를 두었다.
- 원본 에디터 수동 조작·실제 터치·Android 빌드/기기·실제 카탈로그/번들은 미검증이다.

## 직접 확인과 다음 작업

`HeavyCharge.md`의 순서로 Tools > Play From Title 끄기 → MainTestScene Play → C_Attack 홀드 → MP10·이동정지/조준·세 단계 표시 → 해제/방어/회피/피격/정지/포커스 취소를 확인한다. PC는 Play 중 PlayerController2D를 끄고 Q+이동 키로 확인한다. 오류 시 Console 첫 새 오류와 C_Attack 참조/Configured/배열/MP/입력 소스를 확인한다.

다음은 사용자 실제 플레이 피드백 확인, 별도 범위로 기존 씬 입력/테스트 간섭18개 정리, 정식 테이블 준비 후 결과 이벤트를 실제 강공격 실행에 연결하는 것이다. 이미 합의한 기반을 처음부터 반복하거나 미정 수치·효과를 임의로 구현하지 않는다.
