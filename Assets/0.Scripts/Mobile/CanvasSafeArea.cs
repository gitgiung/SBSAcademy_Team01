using UnityEngine;
using UnityEngine.UI;

/// <summary>기존 HUD 계층과 위치를 보존하며 화면 안전 영역에 배치합니다.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Canvas), typeof(CanvasScaler))]
[DefaultExecutionOrder(-1000)]
public sealed class CanvasSafeArea : MonoBehaviour
{
    [SerializeField] private Vector2 referenceResolution = new Vector2(1920f, 1080f);
    private struct Placement
    {
        public RectTransform rect;
        public Vector2 anchorMin;
        public Vector2 anchorMax;
    }

    private Placement[] placements;
    private CanvasScaler scaler;
    private CanvasScaler.ScaleMode originalScaleMode;
    private CanvasScaler.ScreenMatchMode originalMatchMode;
    private Vector2 originalResolution;
    private Rect previousSafeArea;
    private Vector2 previousScreenSize;
    private bool applied;

    private void OnEnable()
    {
        scaler = GetComponent<CanvasScaler>();
        originalScaleMode = scaler.uiScaleMode;
        originalMatchMode = scaler.screenMatchMode;
        originalResolution = scaler.referenceResolution;
        placements = new Placement[transform.childCount];
        for (int i = 0; i < placements.Length; i++)
        {
            var child = transform.GetChild(i) as RectTransform;
            // 결과 화면 등의 배경은 전체 화면을 유지합니다.
            if (child == null || (child.anchorMin == Vector2.zero && child.anchorMax == Vector2.one
                && child.offsetMin == Vector2.zero && child.offsetMax == Vector2.zero)) continue;
            placements[i] = new Placement { rect = child, anchorMin = child.anchorMin, anchorMax = child.anchorMax };
        }
        applied = false;
        Update();
    }

    private void Update()
    {
        var size = new Vector2(Screen.width, Screen.height);
        Rect safeArea = Screen.safeArea;
        if (!applied || safeArea != previousSafeArea || size != previousScreenSize) Apply(safeArea, size);
    }

    private void Apply(Rect safeArea, Vector2 screenSize)
    {
        if (screenSize.x <= 0f || screenSize.y <= 0f || safeArea.width <= 0f || safeArea.height <= 0f) return;
        Vector2 min = new Vector2(Mathf.Clamp01(safeArea.xMin / screenSize.x), Mathf.Clamp01(safeArea.yMin / screenSize.y));
        Vector2 max = new Vector2(Mathf.Clamp01(safeArea.xMax / screenSize.x), Mathf.Clamp01(safeArea.yMax / screenSize.y));
        Vector2 span = max - min;
        if (span.x <= 0f || span.y <= 0f) return;
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        // 중앙 기준 고정 오프셋도 안전 영역 안에서 기준 해상도만큼 공간을 확보합니다.
        scaler.referenceResolution = new Vector2(referenceResolution.x / span.x, referenceResolution.y / span.y);
        foreach (Placement placement in placements)
        {
            if (placement.rect == null) continue;
            Vector2 position = placement.rect.anchoredPosition;
            Vector2 size = placement.rect.sizeDelta;
            placement.rect.anchorMin = min + Vector2.Scale(placement.anchorMin, span);
            placement.rect.anchorMax = min + Vector2.Scale(placement.anchorMax, span);
            placement.rect.anchoredPosition = position;
            placement.rect.sizeDelta = size;
        }
        previousSafeArea = safeArea;
        previousScreenSize = screenSize;
        applied = true;
    }

    private void OnDisable()
    {
        if (placements == null || scaler == null) return;
        foreach (Placement placement in placements)
        {
            if (placement.rect == null) continue;
            placement.rect.anchorMin = placement.anchorMin;
            placement.rect.anchorMax = placement.anchorMax;
        }
        scaler.uiScaleMode = originalScaleMode;
        scaler.screenMatchMode = originalMatchMode;
        scaler.referenceResolution = originalResolution;
        applied = false;
    }
}
