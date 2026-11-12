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
    private bool initialized;

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
        cooldownImage.rectTransform.anchorMin = Vector2.zero;
        cooldownImage.rectTransform.anchorMax = Vector2.one;
        cooldownImage.rectTransform.offsetMin = cooldownImage.rectTransform.offsetMax = Vector2.zero;
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
        if (initialized) return;
        sprintButton = GetComponent<UnityEngine.UI.Button>();
        if (cooldownImage == null) cooldownImage = GetComponent<UnityEngine.UI.Image>();
        ConfigureImage();
        ConfigureText();
        initialized = true;
    }

    private void ConfigureText()
    {
        if (cooldownText == null)
        {
            generatedText = new GameObject("Cooldown Seconds", typeof(RectTransform));
            generatedText.transform.SetParent(cooldownImage != null ? cooldownImage.transform : transform, false);
            var text = generatedText.AddComponent<TMPro.TextMeshProUGUI>();
            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = text.rectTransform.offsetMax = Vector2.zero;
            text.fontSize = UiFontScale.Size(36f);
            if (text.GetComponent<UiFontScaledMarker>() == null)
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
        if (sprintButton == null) sprintButton = GetComponent<UnityEngine.UI.Button>();
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
        float remaining = player != null ? (tactical ? player.TacticalCooldownRemaining : player.DashCooldownRemaining) : 0f;
        bool visible = remaining > 0f;
        if (cooldownImage != null)
        {
            cooldownImage.fillAmount = player != null
                ? (tactical ? 1f - player.TacticalCooldownProgress : player.DashCooldownProgress) : 1f;
        }
        SetCooldownVisible(visible);
        if (cooldownText != null)
        {
            cooldownText.text = visible ? Mathf.CeilToInt(remaining).ToString() : string.Empty;
            // 버튼과 같은 오브젝트에 연결해도 버튼 자체는 비활성화하지 않습니다.
            cooldownText.enabled = visible;
            if (!transform.IsChildOf(cooldownText.transform) && cooldownText.gameObject.activeSelf != visible)
                cooldownText.gameObject.SetActive(visible);
        }
    }

    private void SetCooldownVisible(bool visible)
    {
        if (cooldownImage == null) return;
        // 버튼 자신이나 그 상위를 끄면 다음 클릭과 표시 갱신도 끊어지므로 별도 표시만 토글합니다.
        if (transform.IsChildOf(cooldownImage.transform)) return;
        if (cooldownImage.gameObject.activeSelf != visible)
            cooldownImage.gameObject.SetActive(visible);
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
        SetCooldownVisible(false);
        if (cooldownText != null)
        {
            cooldownText.text = string.Empty;
            cooldownText.enabled = false;
            if (!transform.IsChildOf(cooldownText.transform)) cooldownText.gameObject.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (generatedText != null) Destroy(generatedText);
        if (generatedOverlay != null) Destroy(generatedOverlay);
    }
}
