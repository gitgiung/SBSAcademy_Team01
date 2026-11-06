using UnityEngine;

/// <summary>Sprint 버튼과 시계방향 Image를 실제 플레이어 대시 쿨타임에 연결합니다.</summary>
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

    private void Awake()
    {
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
            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = text.rectTransform.offsetMax = Vector2.zero;
            text.fontSize = 36f;
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
        sprintButton.onClick.AddListener(OnSprintClicked);
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
        float remaining = player != null ? player.DashCooldownRemaining : 0f;
        bool visible = remaining > 0f;
        if (cooldownImage != null)
        {
            cooldownImage.fillAmount = player != null ? player.DashCooldownProgress : 1f;
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
    }
}
