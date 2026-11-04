using UnityEngine;

/// <summary>Shows the boss slash arc and dash impact circle on the actual hit shape.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(BossController))]
public sealed class BossAttackHitVfx : MonoBehaviour
{
    private const int CircleResolution = 256;
    private static readonly int RadialStartId = Shader.PropertyToID("_RadialStartAngle");
    private static readonly int RadialClipId = Shader.PropertyToID("_RadialClip");
    private static readonly int RadialClip2Id = Shader.PropertyToID("_RadialClip2");
    private static readonly int AlphaId = Shader.PropertyToID("_Alpha");
    private static readonly int GlowColorId = Shader.PropertyToID("_GlowColor");

    [SerializeField] private Material rangeMaterial;
    [SerializeField] private Color rangeColor = new Color(1f, 0.35f, 0.08f, 0.4f);
    [SerializeField, Min(0f)] private float dashDisplayDuration = 0.2f;

    private SpriteRenderer bossRenderer;
    private SpriteRenderer slashRenderer;
    private SpriteRenderer dashRenderer;
    private MaterialPropertyBlock propertyBlock;
    private Texture2D circleTexture;
    private Sprite circleSprite;
    private float dashTimeRemaining;
    private Vector2 dashCenter;
    private float dashRadius;

    private void Awake()
    {
        bossRenderer = GetComponent<SpriteRenderer>();
        propertyBlock = new MaterialPropertyBlock();
        circleSprite = CreateCircleSprite();
        slashRenderer = CreateRenderer("Boss Slash Hit VFX", circleSprite);
        dashRenderer = CreateRenderer("Boss Dash Hit VFX", circleSprite);
        if (rangeMaterial == null)
            Debug.LogError("보스 피격 범위 VFX 머티리얼이 없습니다.", this);
        HideSlash();
        HideDash();
    }

    public void ShowSlash(Vector2 forward, float radius, float angle)
    {
        if (slashRenderer == null) return;
        slashRenderer.enabled = true;
        slashRenderer.transform.localPosition = Vector3.zero;
        Place(slashRenderer, slashRenderer.transform.position, forward, radius, angle, rangeColor.a);
    }

    public void HideSlash()
    {
        if (slashRenderer != null) slashRenderer.enabled = false;
    }

    public void Hide()
    {
        HideSlash();
        HideDash();
    }

    public void ShowDashImpact(Vector2 center, float radius)
    {
        if (dashRenderer == null) return;
        dashCenter = center;
        dashRadius = radius;
        dashTimeRemaining = dashDisplayDuration;
        dashRenderer.enabled = dashDisplayDuration > 0f;
        Place(dashRenderer, center, Vector2.right, radius, 360f, rangeColor.a);
    }

    private void LateUpdate()
    {
        if (dashRenderer == null || !dashRenderer.enabled) return;

        dashTimeRemaining -= Time.deltaTime;
        if (dashTimeRemaining <= 0f)
        {
            HideDash();
            return;
        }

        float duration = Mathf.Max(0.0001f, dashDisplayDuration);
        float alpha = rangeColor.a * Mathf.Clamp01(dashTimeRemaining / duration);
        Place(dashRenderer, dashCenter, Vector2.right, dashRadius, 360f, alpha);
    }

    private void OnDisable()
    {
        HideSlash();
        HideDash();
    }

    private void OnDestroy()
    {
        if (!Application.isPlaying) return;
        if (circleSprite != null) Destroy(circleSprite);
        if (circleTexture != null) Destroy(circleTexture);
    }

    private void OnValidate()
    {
        dashDisplayDuration = Mathf.Max(0f, dashDisplayDuration);
        rangeColor.a = Mathf.Clamp01(rangeColor.a);
    }

    private SpriteRenderer CreateRenderer(string objectName, Sprite sprite)
    {
        var visual = new GameObject(objectName);
        visual.transform.SetParent(transform, false);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = Vector3.one;

        SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sharedMaterial = rangeMaterial;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        if (bossRenderer != null)
        {
            renderer.sortingLayerID = bossRenderer.sortingLayerID;
            renderer.sortingOrder = bossRenderer.sortingOrder - 1;
        }
        else
        {
            renderer.sortingOrder = -1;
        }

        return renderer;
    }

    private void Place(SpriteRenderer renderer, Vector2 worldPosition, Vector2 forward, float radius, float angle, float alpha)
    {
        Transform visual = renderer.transform;
        visual.position = worldPosition;
        if (forward.sqrMagnitude <= Mathf.Epsilon) forward = Vector2.right;
        visual.rotation = Quaternion.FromToRotation(Vector3.right, forward.normalized);
        visual.localScale = BossAttackHitVfxShape.LocalScale(radius, transform.lossyScale);

        Color color = rangeColor;
        color.a = Mathf.Clamp01(alpha);
        renderer.color = color;

        renderer.GetPropertyBlock(propertyBlock);
        float clip = BossAttackHitVfxShape.RadialClip(angle);
        propertyBlock.SetFloat(RadialStartId, BossAttackHitVfxShape.RadialStartAngle);
        propertyBlock.SetFloat(RadialClipId, clip);
        propertyBlock.SetFloat(RadialClip2Id, clip);
        propertyBlock.SetFloat(AlphaId, 1f);
        propertyBlock.SetColor(GlowColorId, new Color(rangeColor.r, rangeColor.g, rangeColor.b, 1f));
        renderer.SetPropertyBlock(propertyBlock);
    }

    private void HideDash()
    {
        dashTimeRemaining = 0f;
        if (dashRenderer != null) dashRenderer.enabled = false;
    }

    private Sprite CreateCircleSprite()
    {
        circleTexture = new Texture2D(CircleResolution, CircleResolution, TextureFormat.RGBA32, false)
        {
            name = "Boss Attack Hit Circle",
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        float radius = CircleResolution * 0.5f;
        var pixels = new Color32[CircleResolution * CircleResolution];
        for (int y = 0; y < CircleResolution; y++)
        {
            for (int x = 0; x < CircleResolution; x++)
            {
                float dx = x + 0.5f - radius;
                float dy = y + 0.5f - radius;
                bool inside = dx * dx + dy * dy <= radius * radius;
                pixels[y * CircleResolution + x] = inside
                    ? new Color32(255, 255, 255, 255)
                    : new Color32(0, 0, 0, 0);
            }
        }

        circleTexture.SetPixels32(pixels);
        circleTexture.Apply(false, false);
        return Sprite.Create(
            circleTexture,
            new Rect(0f, 0f, CircleResolution, CircleResolution),
            new Vector2(0.5f, 0.5f),
            CircleResolution,
            0,
            SpriteMeshType.FullRect);
    }
}
