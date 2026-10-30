using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>전투 팝업의 화면 수명을 씬의 진행 담당자에게 전달합니다.</summary>
public sealed class UI_CombatPopup : UI_Popup
{
    [SerializeField] private Button closeButton;
    [SerializeField] private RectTransform contentRoot;
    private CombatPopupController owner;

    public RectTransform ContentRoot => contentRoot;

    public void Bind(CombatPopupController controller)
    {
        owner = controller;
    }

    public override bool Init()
    {
        if (!base.Init()) return false;
        if (closeButton != null)
            closeButton.onClick.AddListener(ClosePopupUI);
        return true;
    }

    private void OnEnable()
    {
        if (owner != null) owner.AcquirePause(this);
        Focus();
    }

    public void Focus()
    {
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(closeButton != null ? closeButton.gameObject : null);
    }

    private void OnDisable()
    {
        if (owner != null) owner.ReleasePause(this);
    }

    private void OnDestroy()
    {
        if (closeButton != null)
            closeButton.onClick.RemoveListener(ClosePopupUI);
    }
}
