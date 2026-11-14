using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>눌렀을 때만 평가해 잠금 중 입력이 버튼 해제 시 예약 실행되지 않게 합니다.</summary>
public sealed class PlayerLightAttackButton : MonoBehaviour, IPointerDownHandler, ISubmitHandler
{
    private Player owner;
    public void Bind(Player player) { owner = player; }
    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left && owner != null) owner.TryUseLightSkill();
    }
    public void OnSubmit(BaseEventData eventData) { if (owner != null) owner.TryUseLightSkill(); }
}
