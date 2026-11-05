# 플레이어 장착 모델 검증

저장소 루트에서 PowerShell로 `& tools/PlayerLoadoutValidation/Run.ps1`을 실행한다. .NET SDK 9가 필요하며 외부 NuGet 패키지는 없다. 출력과 중간 파일은 `.utmp/PlayerLoadout`에 격리한다.

실제 `PlayerLoadout.cs`와 `Data.Contents.cs`를 컴파일해 장착 교체, 해제 후 보유 유지, 분류 제한, Changed 알림, 잠금 슬롯, 장착 스킬 제거를 검증한다. Unity 속성과 Sprite/Mathf만 최소 경계로 대체한다. UI 클릭·입력·MP·코루틴·씬 직렬화는 이 도구의 검증 범위가 아니다.

플레이 확인: MainTestScene에서 Play From Title을 끄고 재생 → 일시정지 → 보유 불꽃 탄환 클릭. 녹색 `[장착]` 표시를 확인하고 복귀해 N_Attack 또는 Enter를 누르면 표 기준 MP 5를 소모하고 발사한다. 다시 일시정지에서 같은 스킬을 눌러 해제 후 복귀하면 두 입력 모두 추가 시전과 MP 차감을 하지 않는다. 자동 패시브 발사는 보유에 따른 별도 동작이다.
