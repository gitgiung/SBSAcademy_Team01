using UnityEngine;

/// <summary>보호막이 남아 있는 동안 캐릭터 위에 남은 보호막 체력을 표시합니다.</summary>
[DisallowMultipleComponent]
public sealed class PlayerShieldBar : MonoBehaviour
{
    private PlayerData source;
    private SpriteRenderer bodyRenderer;
    private GameObject root;
    private Transform fill;
    private float width = 0.9f;

    public void Initialize(PlayerData data, SpriteRenderer body)
    {
        source = data;
        bodyRenderer = body;
        Build();
        if (source != null) source.ShieldChanged += Refresh;
        Refresh(source != null ? source.CurrentShield : 0f, source != null ? source.MaxShield : 0f);
    }

    private void Build()
    {
        if (root != null) return;
        Sprite pixel = Pixel();
        float y = 1.15f;
        if (bodyRenderer != null && bodyRenderer.sprite != null)
            y = bodyRenderer.sprite.bounds.max.y + 0.22f;
        root = new GameObject("ShieldHp");
        root.transform.SetParent(transform, false);
        root.transform.localPosition = new Vector3(0f, y, 0f);
        CreateBar(root.transform, "Back", pixel, new Color(0.05f, 0.08f, 0.14f, 0.9f), width, 0.12f, 0);
        fill = CreateBar(root.transform, "Fill", pixel, new Color(0.45f, 0.82f, 1f, 1f), width, 0.08f, 1).transform;
        root.SetActive(false);
    }

    private SpriteRenderer CreateBar(Transform parent, string name, Sprite sprite, Color color, float barWidth, float height, int order)
    {
        var bar = new GameObject(name);
        bar.transform.SetParent(parent, false);
        var renderer = bar.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingLayerID = bodyRenderer != null ? bodyRenderer.sortingLayerID : 0;
        renderer.sortingOrder = (bodyRenderer != null ? bodyRenderer.sortingOrder : 0) + 20 + order;
        bar.AddComponent<HitFlashExcluded>();
        bar.transform.localScale = new Vector3(barWidth, height, 1f);
        return renderer;
    }

    private void Refresh(float current, float maximum)
    {
        if (root == null || fill == null) return;
        float ratio = maximum > 0f ? Mathf.Clamp01(current / maximum) : 0f;
        bool visible = ratio > 0f;
        root.SetActive(visible);
        if (!visible) return;
        fill.localScale = new Vector3(width * ratio, 0.08f, 1f);
        fill.localPosition = new Vector3((ratio - 1f) * width * 0.5f, 0f, 0f);
    }

    private void OnDestroy()
    {
        if (source != null) source.ShieldChanged -= Refresh;
        if (root != null) Destroy(root);
    }

    private static Sprite pixel;
    private static Sprite Pixel()
    {
        if (pixel != null) return pixel;
        var texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        pixel = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
        pixel.name = "ShieldBarPixel";
        return pixel;
    }
}
