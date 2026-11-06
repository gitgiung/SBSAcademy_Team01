using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>조회 전용 테스트 숫자. 끄거나 삭제하면 숫자와 구독을 모두 정리합니다.</summary>
[DisallowMultipleComponent]
public sealed class DamageNumberTest : MonoBehaviour
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
    private Canvas canvas;
    private RectTransform canvasRect;
    private bool warnedMissingCamera;

    private sealed class Number
    {
        internal TextMeshProUGUI text;
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

    private void Show(float damage, Vector3 position, Color color)
    {
        if (!isActiveAndEnabled || damage <= 0f || float.IsNaN(damage) || float.IsInfinity(damage)) return;
        if (targetCamera == null) targetCamera = Camera.main;
        if (targetCamera == null)
        {
            if (!warnedMissingCamera) Debug.LogWarning("DamageNumberTest: Target Camera 또는 MainCamera 태그를 확인하세요.", this);
            warnedMissingCamera = true;
            return;
        }
        if (canvas == null) BuildCanvas();
        if (font == null)
        {
            Debug.LogWarning("DamageNumberTest: Font에 TMP Font Asset을 지정하세요.", this);
            return;
        }
        while (numbers.Count >= Mathf.Max(1, maxNumbers)) RemoveAt(0);
        var label = new GameObject("DamageNumber", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
        label.transform.SetParent(canvasRect, false);
        label.font = font;
        label.text = damage.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture);
        label.fontSize = Mathf.Max(10f, fontSize);
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
        label.color = color;
        label.outlineColor = Color.black;
        label.outlineWidth = .15f;
        label.rectTransform.sizeDelta = new Vector2(240f, 80f);
        var number = new Number { text = label, worldPosition = position + worldOffset, color = color };
        numbers.Add(number);
        Position(number);
    }

    private void BuildCanvas()
    {
        font = font != null ? font : TMP_Settings.defaultFontAsset;
        if (font == null) font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        var root = new GameObject("DamageNumbers (Test)", typeof(RectTransform), typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler));
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
        if (targetCamera == null) targetCamera = Camera.main;
        for (int i = numbers.Count - 1; i >= 0; i--)
        {
            Number number = numbers[i];
            number.age += Time.deltaTime;
            if (number.age >= Mathf.Max(.1f, lifetime)) { RemoveAt(i); continue; }
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

    private void RemoveAt(int index)
    {
        if (numbers[index].text != null)
        {
            GameObject label = numbers[index].text.gameObject;
            Material material = numbers[index].text.fontMaterial;
            label.SetActive(false);
            Destroy(label);
            // Outline 설정으로 생성된 인스턴스 재질도 정리합니다.
            Destroy(material);
        }
        numbers.RemoveAt(index);
    }

    [ContextMenu("테스트/샘플 숫자 표시 (Play 중)")]
    private void Preview()
    {
        if (Application.isPlaying) Show(123f, transform.position, enemyColor);
    }

    private void OnDisable()
    {
        EnemyHealth.DamageApplied -= OnEnemyDamage;
        Player.DamageApplied -= OnPlayerDamage;
        while (numbers.Count > 0) RemoveAt(numbers.Count - 1);
        if (canvas != null) { canvas.gameObject.SetActive(false); Destroy(canvas.gameObject); }
        canvas = null;
        canvasRect = null;
    }
}
