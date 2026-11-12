using UnityEngine;

/// <summary>Shows boss slash, dash impact and phase-two area attack shapes.</summary>
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
    [Header("공격 범위 깜빡임")]
    [Tooltip("범위가 완전히 투명한 상태에서 최대 밝기에 도달하는 시간(초).")]
    [SerializeField, Min(0.01f)] private float rangeFadeInDuration = 0.07f;
    [Tooltip("최대 밝기에서 다시 투명해지는 시간(초).")]
    [SerializeField, Min(0.01f)] private float rangeFadeOutDuration = 0.23f;
    [Tooltip("돌진 착지 범위 깜빡임의 전체 시간(초). 0이면 표시하지 않습니다.")]
    [SerializeField, Min(0f)] private float dashDisplayDuration = 0.2f;
    [Header("원형 패턴")]
    [SerializeField] private Sprite areaBurstSprite;
    [SerializeField] private Color areaBurstColor = Color.red;
    [SerializeField, Min(0.01f)] private float areaBurstDuration = 0.2f;
    [Header("등장 연출 전용 VFX (피해 범위와 무관)")]
    [SerializeField, Min(1f)] private float entranceBurstMinimumSize = 100f;
    [Tooltip("등장 폭발의 확산·페이드 시간(초). 0이면 기존 Area Burst Duration을 사용합니다.")]
    [SerializeField, Min(0f)] private float entranceAreaBurstDuration;

    private SpriteRenderer bossRenderer;
    private SpriteRenderer slashRenderer;
    private SpriteRenderer dashRenderer;
    private SpriteRenderer areaRenderer;
    private SpriteRenderer areaChargeRenderer;
    private float areaChargeRadius;
    private SpriteRenderer burstRenderer;
    private Material burstMaterial;
    private float burstTimeRemaining;
    private float activeBurstDuration;
    private float burstRadius;
    private MaterialPropertyBlock propertyBlock;
    private Texture2D circleTexture;
    private Sprite circleSprite;
    private float slashPulseStart;
    private float dashPulseStart;

    private void Awake()
    {
        bossRenderer = GetComponent<SpriteRenderer>();
        propertyBlock = new MaterialPropertyBlock();
        circleSprite = CreateCircleSprite();
        slashRenderer = CreateRenderer("Boss Slash Hit VFX", circleSprite);
        dashRenderer = CreateRenderer("Boss Dash Hit VFX", circleSprite);
        areaRenderer = CreateRenderer("Boss Area Hit VFX", circleSprite);
        areaRenderer.sortingOrder = bossRenderer != null ? bossRenderer.sortingOrder - 2 : -2;
        areaChargeRenderer = CreateRenderer("Boss Area Charge VFX", circleSprite);
        areaChargeRenderer.sortingOrder = areaRenderer.sortingOrder + 1;
        burstRenderer = CreateRenderer("Boss Area Burst Test VFX", areaBurstSprite);
        burstRenderer.sortingOrder = bossRenderer != null ? bossRenderer.sortingOrder - 1 : -1;
        Shader burstShader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
        if (burstShader == null) burstShader = Shader.Find("Sprites/Default");
        if (burstShader != null)
        {
            burstMaterial = new Material(burstShader) { name = "Boss Area Burst Test Material" };
            burstRenderer.sharedMaterial = burstMaterial;
        }
        if (rangeMaterial == null)
            Debug.LogError("보스 피격 범위 VFX 머티리얼이 없습니다.", this);
        Hide();
    }

    public void ShowSlash(Vector2 forward, float radius, float angle)
    {
        if (slashRenderer == null) return;
        slashRenderer.enabled = true;
        slashRenderer.transform.localPosition = Vector3.zero;
        slashPulseStart = Time.time;
        Place(slashRenderer, slashRenderer.transform.position, forward, radius, angle, 0f);
    }

    public void HideSlash()
    {
        if (slashRenderer != null) slashRenderer.enabled = false;
    }

    public void Hide()
    {
        HideSlash();
        HideDash();
        HideArea();
        HideBurst();
    }

    public void ShowArea(float radius)
    {
        if (areaRenderer == null) return;
        areaRenderer.transform.localPosition = Vector3.zero;
        Place(areaRenderer, transform.position, Vector2.right, radius, 360f, rangeColor.a);
        areaRenderer.enabled = true;
        areaChargeRadius = radius;
        SetAreaChargeProgress(0f);
    }

    public void SetAreaChargeProgress(float progress)
    {
        if (areaChargeRenderer == null) return;
        float fill = Mathf.Clamp01(progress);
        areaChargeRenderer.enabled = fill > 0f;
        Place(areaChargeRenderer, transform.position, Vector2.right, areaChargeRadius * fill, 360f, rangeColor.a);
    }

    public void HideArea()
    {
        if (areaRenderer != null) areaRenderer.enabled = false;
        if (areaChargeRenderer != null) areaChargeRenderer.enabled = false;
    }

    public void ShowAreaBurst(float radius)
    {
        BeginBurst(radius, areaBurstDuration);
    }

    private void BeginBurst(float radius, float duration)
    {
        if (!isActiveAndEnabled || burstRenderer == null || areaBurstSprite == null || burstMaterial == null) return;
        burstRadius = Mathf.Max(0f, radius);
        activeBurstDuration = Mathf.Max(0.01f, duration);
        burstTimeRemaining = activeBurstDuration;
        burstRenderer.enabled = true;
        UpdateBurst();
    }

    public void ShowEntranceBurst(float attackRadius)
    {
        float size = Mathf.Max(attackRadius, entranceBurstMinimumSize);
        Camera camera = Camera.main;
        if (camera != null)
        {
            float depth = Vector3.Dot(transform.position - camera.transform.position, camera.transform.forward);
            if (depth > 0f)
                for (int x = 0; x <= 1; x++)
                    for (int y = 0; y <= 1; y++)
                    {
                        Vector3 corner = camera.ViewportToWorldPoint(new Vector3(x, y, depth));
                        // 멀리 있는 플레이어 화면에도 확산 중인 원이 닿도록 충분한 여유를 줍니다.
                        size = BossAttackHitVfxShape.EntranceBurstRadius(size, transform.position, corner);
                    }
        }
        BeginBurst(size, entranceAreaBurstDuration > 0f ? entranceAreaBurstDuration : areaBurstDuration);
    }

    private void UpdateBurst()
    {
        if (burstRenderer == null || !burstRenderer.enabled) return;
        if (burstTimeRemaining <= 0f)
        {
            HideBurst();
            return;
        }
        float t = 1f - Mathf.Clamp01(burstTimeRemaining / activeBurstDuration);
        float expansion = Mathf.Lerp(0.1f, 1f, 1f - Mathf.Pow(1f - t, 3f));
        float spriteDiameter = Mathf.Max(areaBurstSprite.bounds.size.x, areaBurstSprite.bounds.size.y);
        burstRenderer.transform.localPosition = Vector3.zero;
        burstRenderer.transform.rotation = Quaternion.identity;
        burstRenderer.transform.localScale = BossAttackHitVfxShape.LocalScale(
            burstRadius * expansion / Mathf.Max(0.0001f, spriteDiameter), transform.lossyScale);
        Color color = areaBurstColor;
        color.a *= 1f - t;
        burstRenderer.color = color;
    }

    private void HideBurst()
    {
        burstTimeRemaining = 0f;
        if (burstRenderer != null) burstRenderer.enabled = false;
    }

    public void ShowDashImpact(Vector2 center, float radius)
    {
        if (dashRenderer == null) return;
        dashPulseStart = Time.time;
        dashRenderer.enabled = dashDisplayDuration > 0f;
        Place(dashRenderer, center, Vector2.right, radius, 360f, 0f);
    }

    private float RangePulseDuration => rangeFadeInDuration + rangeFadeOutDuration;

    private void LateUpdate()
    {
        burstTimeRemaining -= Time.deltaTime;
        UpdateBurst();
        UpdateRangePulse(slashRenderer, slashPulseStart, RangePulseDuration, rangeColor);
        UpdateRangePulse(dashRenderer, dashPulseStart, dashDisplayDuration, rangeColor);
    }

    private float GetPulseOpacity(float startTime, float duration)
    {
        if (duration <= 0f) return 0f;
        float progress = Mathf.Clamp01((Time.time - startTime) / duration);
        float peak = rangeFadeInDuration / RangePulseDuration;
        return Mathf.Min(progress / peak, (1f - progress) / (1f - peak));
    }

    private void UpdateRangePulse(SpriteRenderer renderer, float startTime, float duration, Color color)
    {
        if (renderer == null || !renderer.enabled) return;
        if (Time.time - startTime >= duration)
        {
            renderer.enabled = false;
            return;
        }

        color.a *= GetPulseOpacity(startTime, duration);
        renderer.color = color;
        renderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetColor(GlowColorId, color);
        renderer.SetPropertyBlock(propertyBlock);
    }

    private void OnDisable()
    {
        Hide();
    }

    private void OnDestroy()
    {
        if (!Application.isPlaying) return;
        if (circleSprite != null) Destroy(circleSprite);
        if (circleTexture != null) Destroy(circleTexture);
        if (burstMaterial != null) Destroy(burstMaterial);
    }

    private void OnValidate()
    {
        dashDisplayDuration = Mathf.Max(0f, dashDisplayDuration);
        rangeFadeInDuration = Mathf.Max(0.01f, rangeFadeInDuration);
        rangeFadeOutDuration = Mathf.Max(0.01f, rangeFadeOutDuration);
        rangeColor.a = Mathf.Clamp01(rangeColor.a);
        areaBurstDuration = Mathf.Max(0.01f, areaBurstDuration);
        entranceAreaBurstDuration = Mathf.Max(0f, entranceAreaBurstDuration);
    }

    private SpriteRenderer CreateRenderer(string objectName, Sprite sprite)
    {
        var visual = new GameObject(objectName);
        visual.transform.SetParent(transform, false);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = Vector3.one;

        SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
        visual.AddComponent<HitFlashExcluded>();
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
