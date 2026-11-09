using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>전투 팝업의 화면 수명을 씬의 진행 담당자에게 전달합니다.</summary>
public sealed class UI_CombatPopup : UI_Popup, IPointerClickHandler
{
    [SerializeField] private Button closeButton;
    [SerializeField] private RectTransform contentRoot;
    private CombatPopupController owner;

    public RectTransform ContentRoot => contentRoot;
    public Button CloseButton => closeButton;

    public void Bind(CombatPopupController controller)
    {
        owner = controller;
    }

    public override void ClosePopupUI()
    {
        if (owner != null && !owner.CanClosePopupUI(this)) return;
        base.ClosePopupUI();
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

    /// <summary>회색 바깥 패널을 누르면 뒤로가기와 같은 닫기입니다. 내용 버튼은 자식이 먼저 받습니다.</summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData == null || eventData.button != PointerEventData.InputButton.Left) return;
        if (Managers.UI == null || !Managers.UI.IsTopPopup(this)) return;
        ClosePopupUI();
    }

    private void Update()
    {
        if (Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame) return;
        if (Managers.UI == null || !Managers.UI.IsTopPopup(this)) return;
        PlayerStatusPanel status = GetComponentInChildren<PlayerStatusPanel>(true);
        if (status != null && status.OptionsVisible)
        {
            status.HideOptions();
            return;
        }
        ClosePopupUI();
    }
}
