using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Guard를 시작한 손가락만 가드를 유지·해제합니다.</summary>
public sealed class PlayerGuardButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, ICancelHandler
{
    private Player owner;
    private Button button;
    private int pointerId;
    public bool IsHeld { get; private set; }

    public void Bind(Player player)
    {
        Release();
        owner = player;
        button = GetComponent<Button>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (IsHeld || !isActiveAndEnabled || owner == null ||
            eventData.button != PointerEventData.InputButton.Left || (button != null && !button.IsInteractable())) return;
        pointerId = eventData.pointerId;
        IsHeld = true;
        // 패링에 실패한 누름은 이후 가드나 다음 입력으로 예약하지 않습니다.
        if (!owner.TryParry()) Release();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (IsHeld && eventData.pointerId == pointerId && eventData.button == PointerEventData.InputButton.Left) Release();
    }

    public void Release() => IsHeld = false;
    public void OnCancel(BaseEventData eventData) => Release();
    private void OnDisable() => Release();
}
