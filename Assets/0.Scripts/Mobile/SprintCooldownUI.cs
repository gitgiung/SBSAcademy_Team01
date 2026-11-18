using UnityEngine;

/// <summary>기존 Sprint 및 전략 버튼의 원형/초 표시를 실제 플레이어 쿨타임에 연결합니다.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(UnityEngine.UI.Button))]
public sealed class SprintCooldownUI : MonoBehaviour
{
    [SerializeField] private Player player;
    [Tooltip("쿨타임 중에만 켤 별도 Cooldown Image입니다. Sprint 버튼 자신의 Image를 쓰면 버튼을 유지하기 위해 숨기지 않습니다.")]
    [SerializeField] private UnityEngine.UI.Image cooldownImage;
    [Tooltip("남은 초를 표시할 TMP 텍스트입니다. 비우면 Cooldown Image 아래에 자동 생성합니다.")]
    [SerializeField] private TMPro.TMP_Text cooldownText;

    private UnityEngine.UI.Button sprintButton;
    private GameObject generatedText;
    private GameObject generatedOverlay;
    private bool tactical;

    public void BindTactical(Player source)
    {
        Initialize();
        if (tactical && player == source) return;
        player = source;
        if (tactical) { Refresh(); return; }
        tactical = true;
        // 전략 입력은 기존 PointerDown이 소유합니다. 클릭 해제 때 다시 시전하지 않습니다.
        sprintButton.onClick.RemoveListener(OnSprintClicked);
        generatedOverlay = new GameObject("Tactical Cooldown", typeof(RectTransform), typeof(UnityEngine.UI.Image));
        generatedOverlay.transform.SetParent(transform, false);
        cooldownImage = generatedOverlay.GetComponent<UnityEngine.UI.Image>();
        cooldownImage.sprite = GetComponent<UnityEngine.UI.Image>()?.sprite;
        cooldownImage.color = new Color(0f, 0f, 0f, .65f);
        Stretch(cooldownImage.rectTransform);
        ConfigureImage();
        // 전략 버튼의 원본 아이콘은 항상 온전히 표시합니다.
        var icon = GetComponent<UnityEngine.UI.Image>();
        if (icon != null) { icon.type = UnityEngine.UI.Image.Type.Simple; icon.fillAmount = 1f; }
        if (cooldownText != null) cooldownText.transform.SetAsLastSibling();
        Refresh();
    }

    private void Awake() => Initialize();

    private void Initialize()
    {
        if (sprintButton != null) return;
        sprintButton = GetComponent<UnityEngine.UI.Button>();
        if (cooldownImage == null) cooldownImage = GetComponent<UnityEngine.UI.Image>();
        ConfigureImage();
        ConfigureText();
    }

    private void ConfigureText()
    {
        if (cooldownText == null)
        {
            generatedText = new GameObject("Cooldown Seconds", typeof(RectTransform));
            generatedText.transform.SetParent(cooldownImage != null ? cooldownImage.transform : transform, false);
            var text = generatedText.AddComponent<TMPro.TextMeshProUGUI>();
            Stretch(text.rectTransform);
            text.fontSize = UiFontScale.Size(36f);
            text.gameObject.AddComponent<UiFontScaledMarker>();
            text.fontStyle = TMPro.FontStyles.Bold;
            text.alignment = TMPro.TextAlignmentOptions.Center;
            text.color = Color.white;
            cooldownText = text;
        }
        cooldownText.raycastTarget = false;
    }

    private void ConfigureImage()
    {
        if (cooldownImage == null) return;
        cooldownImage.type = UnityEngine.UI.Image.Type.Filled;
        cooldownImage.fillMethod = UnityEngine.UI.Image.FillMethod.Radial360;
        cooldownImage.fillOrigin = (int)UnityEngine.UI.Image.Origin360.Top;
        cooldownImage.fillClockwise = true;
        // 자식 덮개 Image가 버튼 클릭을 가로채지 않게 합니다. 버튼 자체 Image는 유지합니다.
        if (cooldownImage.gameObject != gameObject) cooldownImage.raycastTarget = false;
    }

    private void OnEnable()
    {
        if (!tactical) sprintButton.onClick.AddListener(OnSprintClicked);
        Refresh();
    }

    private void OnSprintClicked()
    {
        ResolvePlayer();
        if (player != null) player.TryUseDash();
        Refresh();
    }

    private void LateUpdate() => Refresh();

    private void Refresh()
    {
        ResolvePlayer();
        float remaining = 0f;
        float fill = 1f;
        if (player != null)
        {
            remaining = tactical ? player.TacticalCooldownRemaining : player.DashCooldownRemaining;
            fill = tactical ? 1f - player.TacticalCooldownProgress : player.DashCooldownProgress;
        }

        if (cooldownImage != null) cooldownImage.fillAmount = fill;
        ShowCooldown(remaining);
    }

    private void ShowCooldown(float remaining)
    {
        bool visible = remaining > 0f;
        SetSeparateObjectActive(cooldownImage, visible);
        if (cooldownText == null) return;

        cooldownText.text = visible ? Mathf.CeilToInt(remaining).ToString() : string.Empty;
        cooldownText.enabled = visible;
        SetSeparateObjectActive(cooldownText, visible);
    }

    private void SetSeparateObjectActive(Component display, bool visible)
    {
        // 버튼이나 그 상위를 비활성화하면 클릭과 쿨타임 갱신도 멈춥니다.
        if (display != null && !transform.IsChildOf(display.transform) && display.gameObject.activeSelf != visible)
            display.gameObject.SetActive(visible);
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }


    private void ResolvePlayer()
    {
        if (player != null) return;
        foreach (Player candidate in FindObjectsByType<Player>())
            if (candidate.gameObject.scene == gameObject.scene)
            {
                player = candidate;
                break;
            }
    }

    private void OnDisable()
    {
        if (sprintButton != null) sprintButton.onClick.RemoveListener(OnSprintClicked);
        ShowCooldown(0f);
    }

    private void OnDestroy()
    {
        if (generatedText != null) Destroy(generatedText);
        if (generatedOverlay != null) Destroy(generatedOverlay);
    }
}
