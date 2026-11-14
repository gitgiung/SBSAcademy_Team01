# 수동 공격 애니메이션 연결

사용자 확정 동작: 약공격 시 Attack, 강공격 차징 중 Guard, 강공격 해제 시 Attack. 기존 Animator에서 공격 클립이 연결된 상태 이름은 LightAttack이다. Charge는 대시용으로 유지한다.

PlayerView의 기존 IsGuarding Animator 파라미터에 실제 가드와 차징 시각 상태를 합성한다. 차징은 PlayerDefense나 방어 VFX를 호출하지 않는다. 기존 PlayLightAttack으로 시전 자세만 재생하고 발사 코루틴의 시점·MP·피해·잠금 종료는 변경하지 않는다. 자동 발동도 변경하지 않는다. 씬/프리팹/Animator 자산 수정은 필요 없다.

- [x] 실제 PlayerView 및 Animator를 복사본에 연결한 회귀 검증에서 차징 표시 API 누락 실패 확인.
- [x] PlayerView에 방어 효과와 분리한 차징 표시 추가. 시작/자동 취소/명시 취소/해제/리셋에서 갱신.
- [x] 수동 약공격과 강공격 해제에 기존 공격 모션 재생 연결.
- [x] Guard 유지·취소·공격 전환·반복 공격·리셋·대시 회귀 및 현재 게임 소스 컴파일 확인.
- [x] 결과와 실제 MainScene 미검증 한계를 Handoff에 기록.

