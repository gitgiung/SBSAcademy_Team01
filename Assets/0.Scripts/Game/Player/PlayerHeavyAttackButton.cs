using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>하나의 터치가 시작한 차징 ID만 해제합니다.</summary>
public sealed class PlayerHeavyAttackButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, ICancelHandler
{
    private Player owner;
    private int pointerId;
    private int chargeId;
    public void Bind(Player player) { owner = player; }
    public void OnPointerDown(PointerEventData eventData)
    {
        if (chargeId != 0 || eventData.button != PointerEventData.InputButton.Left || owner == null) return;
        if (!owner.TryBeginHeavyCharge()) return;
        pointerId = eventData.pointerId; chargeId = owner.HeavyChargeId;
    }
    public void OnPointerUp(PointerEventData eventData)
    {
        if (chargeId == 0 || eventData.pointerId != pointerId || eventData.button != PointerEventData.InputButton.Left) return;
        int id = chargeId; chargeId = 0;
        if (owner != null) owner.TryReleaseHeavyCharge(id);
    }
    public void OnCancel(BaseEventData eventData) { CancelOwnedCharge(); }
    private void OnDisable() { CancelOwnedCharge(); }
    private void CancelOwnedCharge()
    {
        int id = chargeId; chargeId = 0;
        if (owner != null && id != 0) owner.CancelHeavyCharge(id);
    }
}
