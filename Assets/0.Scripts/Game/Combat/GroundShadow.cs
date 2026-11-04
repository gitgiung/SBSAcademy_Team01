using UnityEngine;

/// <summary>몸 스프라이트 발밑에 붙는 타원 그림자입니다.</summary>
[DisallowMultipleComponent]
public sealed class GroundShadow : MonoBehaviour
{
    public const float WidthRatio = 0.78f;
    public const float HeightRatio = 0.32f;
    public const float FootOverlap = 0.5f;
    public const int SortingOrderOffset = -1;
    private const int TextureWidth = 48;
    private const int TextureHeight = 16;
    private const string ChildName = "Ground Shadow";
    private const string ResourcePath = "Shadows/GroundShadow";

    private static Sprite sharedSprite;

    [SerializeField] private SpriteRenderer body;
    private SpriteRenderer shadowRenderer;

    public static bool IsShadow(SpriteRenderer renderer)
    {
        return renderer != null && renderer.GetComponent<GroundShadow>() != null;
    }

    public static GroundShadow Attach(SpriteRenderer bodyRenderer)
    {
        if (bodyRenderer == null) return null;

        Transform existing = bodyRenderer.transform.Find(ChildName);
        GroundShadow shadow = existing != null ? existing.GetComponent<GroundShadow>() : null;
        if (shadow == null)
        {
            var child = new GameObject(ChildName);
            child.transform.SetParent(bodyRenderer.transform, false);
            shadow = child.AddComponent<GroundShadow>();
        }

        shadow.body = bodyRenderer;
        shadow.EnsureRenderer();
        shadow.Place();
        return shadow;
    }

    public static void GetFootShadow(Bounds bodyBounds, out Vector2 worldSize, out Vector3 worldCenter)
    {
        float width = Mathf.Max(0.01f, bodyBounds.size.x * WidthRatio);
        float height = width * HeightRatio;
        worldSize = new Vector2(width, height);
        worldCenter = new Vector3(
            bodyBounds.center.x,
            bodyBounds.min.y + height * FootOverlap,
            bodyBounds.center.z);
    }

    private void OnEnable() => Place();

    private void LateUpdate() => Place();

    private void EnsureRenderer()
    {
        if (shadowRenderer == null) shadowRenderer = GetComponent<SpriteRenderer>();
        if (shadowRenderer == null) shadowRenderer = gameObject.AddComponent<SpriteRenderer>();
        if (sharedSprite == null) sharedSprite = LoadOrCreateSprite();

        shadowRenderer.sprite = sharedSprite;
        shadowRenderer.color = Color.white;
        shadowRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        shadowRenderer.receiveShadows = false;
    }

    private void Place()
    {
        if (shadowRenderer == null) EnsureRenderer();
        if (body == null || shadowRenderer == null || body.sprite == null || sharedSprite == null)
        {
            if (shadowRenderer != null) shadowRenderer.enabled = false;
            return;
        }

        Bounds bounds = body.bounds;
        if (bounds.size.x <= 0.0001f || bounds.size.y <= 0.0001f)
        {
            shadowRenderer.enabled = false;
            return;
        }

        GetFootShadow(bounds, out Vector2 size, out Vector3 foot);
        foot.z = body.transform.position.z;
        Vector3 parentScale = transform.parent != null ? transform.parent.lossyScale : Vector3.one;
        float scaleX = Mathf.Abs(parentScale.x) > 0.0001f ? Mathf.Abs(parentScale.x) : 1f;
        float scaleY = Mathf.Abs(parentScale.y) > 0.0001f ? Mathf.Abs(parentScale.y) : 1f;
        Vector2 spriteSize = sharedSprite.bounds.size;
        transform.localScale = new Vector3(
            size.x / (Mathf.Max(0.0001f, spriteSize.x) * scaleX),
            size.y / (Mathf.Max(0.0001f, spriteSize.y) * scaleY),
            1f);
        transform.position = foot;

        shadowRenderer.sortingLayerID = body.sortingLayerID;
        // 플레이어는 순서 1, 몬스터는 0입니다. 그림자는 그 아래인 -2에 두어 몸보다 뒤에 깔리게 합니다.
        shadowRenderer.sortingOrder = Mathf.Min(body.sortingOrder + SortingOrderOffset, -2);
        shadowRenderer.flipX = false;
        shadowRenderer.flipY = false;
        shadowRenderer.enabled = body.enabled && body.gameObject.activeInHierarchy;
    }

    private static Sprite LoadOrCreateSprite()
    {
        Sprite loaded = Resources.Load<Sprite>(ResourcePath);
        return loaded != null ? loaded : CreateSprite();
    }

    private static Sprite CreateSprite()
    {
        var texture = new Texture2D(TextureWidth, TextureHeight, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.SetPixels32(BuildPixels());
        texture.Apply(false, true);
        return Sprite.Create(
            texture,
            new Rect(0f, 0f, TextureWidth, TextureHeight),
            new Vector2(0.5f, 0.5f),
            TextureWidth);
    }

    private static Color32[] BuildPixels()
    {
        var pixels = new Color32[TextureWidth * TextureHeight];
        float centerX = (TextureWidth - 1) * 0.5f;
        float centerY = (TextureHeight - 1) * 0.5f;
        float radiusX = TextureWidth * 0.5f;
        float radiusY = TextureHeight * 0.5f;
        for (int y = 0; y < TextureHeight; y++)
        {
            for (int x = 0; x < TextureWidth; x++)
            {
                float nx = (x - centerX) / radiusX;
                float ny = (y - centerY) / radiusY;
                float distance = Mathf.Sqrt(nx * nx + ny * ny);
                byte alpha = distance <= 0.42f ? (byte)170
                    : distance <= 0.68f ? (byte)120
                    : distance <= 0.9f ? (byte)64
                    : (byte)0;
                pixels[y * TextureWidth + x] = new Color32(0, 0, 0, alpha);
            }
        }

        return pixels;
    }
}
