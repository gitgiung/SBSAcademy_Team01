using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>전투 피해량을 표시하고 씬 안에서 숫자 오브젝트를 재사용합니다.</summary>
[DisallowMultipleComponent]
public sealed class DamageNumberDisplay : MonoBehaviour
{
    [SerializeField] private TMP_FontAsset font;
    [SerializeField] private Camera targetCamera;
    [SerializeField] private Color enemyColor = new Color(1f, .85f, .2f);
    [SerializeField] private Color playerColor = new Color(1f, .25f, .25f);
    [SerializeField, Min(10f)] private float fontSize = 36f;
    [SerializeField, Min(.1f)] private float lifetime = .8f;
    [SerializeField, Min(0f)] private float riseDistance = 70f;
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, .7f, 0f);
    [SerializeField, Range(1, 100)] private int maxNumbers = 48;

    private readonly List<Number> numbers = new List<Number>();
    private readonly Stack<Number> availableNumbers = new Stack<Number>();
    private Canvas canvas;
    private RectTransform canvasRect;
    private bool warnedMissingCamera;
    private bool suppressed;

    private sealed class Number
    {
        internal TextMeshProUGUI text;
        internal Material outlineMaterial;
        internal Vector3 worldPosition;
        internal Color color;
        internal float age;
    }

    private void OnEnable()
    {
        EnemyHealth.DamageApplied += OnEnemyDamage;
        Player.DamageApplied += OnPlayerDamage;
    }

    private void OnEnemyDamage(EnemyHealth enemy, float damage)
    {
        if (enemy != null && enemy.gameObject.scene == gameObject.scene)
            Show(damage, enemy.transform.position, enemyColor);
    }

    private void OnPlayerDamage(Player player, float damage)
    {
        if (player != null && player.gameObject.scene == gameObject.scene)
            Show(damage, player.transform.position, playerColor);
    }

    public void SuppressAndClear()
    {
        suppressed = true;
        while (numbers.Count > 0) ReturnNumberAt(0);
    }

    private void Show(float damage, Vector3 position, Color color)
    {
        if (suppressed || !isActiveAndEnabled || damage <= 0f || float.IsNaN(damage) || float.IsInfinity(damage)) return;
        if (targetCamera == null) targetCamera = Camera.main;
        if (targetCamera == null)
        {
            if (!warnedMissingCamera) Debug.LogWarning("DamageNumberDisplay: Target Camera 또는 MainCamera 태그를 확인하세요.", this);
            warnedMissingCamera = true;
            return;
        }
        if (canvas == null) BuildCanvas();
        if (font == null)
        {
            Debug.LogWarning("DamageNumberDisplay: Font에 TMP Font Asset을 지정하세요.", this);
            return;
        }
        while (numbers.Count >= Mathf.Max(1, maxNumbers)) ReturnNumberAt(0);
        Number number = availableNumbers.Count > 0 ? availableNumbers.Pop() : CreateNumber();
        number.worldPosition = position + worldOffset;
        number.color = color;
        number.age = 0f;

        TextMeshProUGUI label = number.text;
        label.text = damage.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture);
        label.fontSize = UiFontScale.Size(Mathf.Max(10f, fontSize));
        label.transform.SetAsLastSibling();
        Position(number);
        label.gameObject.SetActive(true);
        numbers.Add(number);
    }

    private Number CreateNumber()
    {
        var label = new GameObject("DamageNumber", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
        label.transform.SetParent(canvasRect, false);
        label.font = font;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
        label.outlineColor = Color.black;
        label.outlineWidth = .15f;
        label.rectTransform.sizeDelta = new Vector2(240f, 80f);
        return new Number { text = label, outlineMaterial = label.fontMaterial };
    }

    private void BuildCanvas()
    {
        font = font != null ? font : TMP_Settings.defaultFontAsset;
        if (font == null) font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        var root = new GameObject("DamageNumbers", typeof(RectTransform), typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler));
        root.transform.SetParent(transform, false);
        canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        // 게임오버·승리·일시정지 등 기존 Overlay UI(Order 0)보다 뒤에 표시합니다.
        canvas.sortingOrder = -1;
        canvasRect = (RectTransform)root.transform;
        var scaler = root.GetComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = .5f;
        Canvas.ForceUpdateCanvases();
    }

    private void LateUpdate()
    {
        if (numbers.Count == 0) return;
        if (targetCamera == null) targetCamera = Camera.main;
        for (int i = numbers.Count - 1; i >= 0; i--)
        {
            Number number = numbers[i];
            number.age += Time.deltaTime;
            if (number.age >= Mathf.Max(.1f, lifetime)) { ReturnNumberAt(i); continue; }
            Position(number);
        }
    }

    private void Position(Number number)
    {
        if (targetCamera == null) { number.text.enabled = false; return; }
        Vector3 screen = targetCamera.WorldToScreenPoint(number.worldPosition);
        number.text.enabled = screen.z > 0f;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen, null, out Vector2 local);
        float progress = Mathf.Clamp01(number.age / Mathf.Max(.1f, lifetime));
        number.text.rectTransform.anchoredPosition = local + Vector2.up * (Mathf.Max(0f, riseDistance) * progress);
        Color color = number.color;
        color.a *= 1f - progress;
        number.text.color = color;
    }

    private void ReturnNumberAt(int index)
    {
        Number number = numbers[index];
        numbers.RemoveAt(index);
        number.text.gameObject.SetActive(false);
        availableNumbers.Push(number);
    }

    [ContextMenu("샘플 숫자 표시 (Play 중)")]
    private void Preview()
    {
        if (!Application.isPlaying) return;
        if (targetCamera == null) targetCamera = Camera.main;
        Vector3 position = targetCamera != null
            ? targetCamera.ViewportToWorldPoint(new Vector3(.5f, .5f, targetCamera.nearClipPlane + 1f)) - worldOffset
            : transform.position;
        Show(123f, position, enemyColor);
    }

    private void OnDisable()
    {
        EnemyHealth.DamageApplied -= OnEnemyDamage;
        Player.DamageApplied -= OnPlayerDamage;
        foreach (Number number in numbers) Destroy(number.outlineMaterial);
        foreach (Number number in availableNumbers) Destroy(number.outlineMaterial);
        numbers.Clear();
        availableNumbers.Clear();
        if (canvas != null) { canvas.gameObject.SetActive(false); Destroy(canvas.gameObject); }
        canvas = null;
        canvasRect = null;
    }
}
