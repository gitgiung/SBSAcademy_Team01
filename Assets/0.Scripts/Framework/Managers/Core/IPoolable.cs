/// <summary>프리팹의 루트 및 자식 MonoBehaviour가 선택적으로 구현합니다.</summary>
public interface IPoolable
{
    // 비활성 상태에서 호출됩니다. 최초 Awake보다 먼저 호출될 수 있습니다.
    // 이전 상태만 초기화하고, 이번 발사의 데미지 등은 Pop의 initialize에서 설정합니다.
    void OnPoolRent();

    // 비활성화 직전 호출됩니다. 직접 소유한 Tween/예약 작업/이벤트 등을 정리합니다.
    void OnPoolReturn();
}
