using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 눌렀을 때만 평가해 잠금 중 입력이 버튼 해제 시 예약 실행되지 않게 합니다.
/// 스테이지 남은 횟수를 아이콘 위 `xN`으로 표시합니다.
/// </summary>
public sealed class PlayerTacticalSkillButton : MonoBehaviour, IPointerDownHandler, ISubmitHandler
{
    private Player owner;
    private TMP_Text usesLabel;
    private int lastShownUses = int.MinValue;

    public void Bind(Player player)
    {
        if (owner == player)
        {
            RefreshUsesLabel(true);
            return;
        }
        if (owner != null) owner.TacticalUsesChanged -= HandleUsesChanged;
        owner = player;
        if (owner != null) owner.TacticalUsesChanged += HandleUsesChanged;
        EnsureUsesLabel();
        RefreshUsesLabel(true);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left && CanPress())
            owner.TryUseTacticalSkill();
    }

    public void OnSubmit(BaseEventData eventData)
    {
        if (CanPress()) owner.TryUseTacticalSkill();
    }

    private bool CanPress() => owner != null && owner.GetEquippedTacticalUsesRemaining() > 0 &&
        (!TryGetComponent(out Button button) || button.IsInteractable());

    private void OnEnable()
    {
        if (owner != null) owner.TacticalUsesChanged += HandleUsesChanged;
        RefreshUsesLabel(true);
    }

    private void OnDisable()
    {
        if (owner != null) owner.TacticalUsesChanged -= HandleUsesChanged;
    }

    private void OnDestroy()
    {
        if (owner != null) owner.TacticalUsesChanged -= HandleUsesChanged;
    }

    private void LateUpdate()
    {
        // 장착 변경은 별도 이벤트가 없어 표시만 가볍게 맞춥니다.
        RefreshUsesLabel(false);
    }

    private void HandleUsesChanged() => RefreshUsesLabel(true);

    private void EnsureUsesLabel()
    {
        if (usesLabel != null) return;
        Transform existing = transform.Find("TacticalUses");
        if (existing != null) usesLabel = existing.GetComponent<TMP_Text>();
        if (usesLabel != null) return;

        var go = new GameObject("TacticalUses", typeof(RectTransform));
        go.transform.SetParent(transform, false);
        var rect = (RectTransform)go.transform;
        rect.anchorMin = new Vector2(0.55f, 0f);
        rect.anchorMax = new Vector2(1.05f, 0.45f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        var text = go.AddComponent<TextMeshProUGUI>();
        text.fontSize = UiFontScale.Size(28f);
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.BottomRight;
        text.color = Color.white;
        text.raycastTarget = false;
        text.enableAutoSizing = true;
        text.fontSizeMin = UiFontScale.Size(16f);
        text.fontSizeMax = UiFontScale.Size(28f);
        if (text.GetComponent<UiFontScaledMarker>() == null)
            text.gameObject.AddComponent<UiFontScaledMarker>();
        Outline outline = go.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.85f);
        outline.effectDistance = new Vector2(1.5f, -1.5f);
        usesLabel = text;
    }

    private void RefreshUsesLabel(bool force)
    {
        EnsureUsesLabel();
        if (usesLabel == null) return;
        int uses = owner != null ? owner.GetEquippedTacticalUsesRemaining() : 0;
        if (!force && uses == lastShownUses) return;
        lastShownUses = uses;
        usesLabel.text = "x" + uses;
        usesLabel.enabled = true;
        if (TryGetComponent(out Button button))
            button.interactable = uses > 0;
    }
}
