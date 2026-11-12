using UnityEngine;

/// <summary>필드 아이템과 폭탄 파동의 코드 생성 스프라이트를 한 번만 만들어 공유합니다.</summary>
public static class FieldItemSprites
{
    private const int RingResolution = 64;

    private static readonly Sprite[] items = new Sprite[4];
    private static Sprite blastRing;

    /// <summary>종류별 임시 픽셀 그림입니다. 종류가 없거나 그림이 잘못됐으면 null입니다.</summary>
    public static Sprite Get(FieldItemKind kind)
    {
        int index = (int)kind;
        if (index <= 0 || index >= items.Length) return null;
        // 도메인 리로드를 끈 에디터에서 이전 Play의 텍스처가 파괴됐으면 다시 만듭니다.
        if (items[index] != null) return items[index];

        Color32[] pixels = FieldItemPixelArt.BuildPixels(kind);
        if (pixels == null) return null;
        int size = FieldItemPixelArt.Size;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = "FieldItem " + kind,
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };
        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        items[index] = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size, 0,
            SpriteMeshType.FullRect);
        items[index].name = texture.name;
        return items[index];
    }

    /// <summary>지름 1 유닛의 흰 고리입니다. 색과 크기는 렌더러에서 정합니다.</summary>
    public static Sprite BlastRing()
    {
        if (blastRing != null) return blastRing;

        var pixels = new Color32[RingResolution * RingResolution];
        float center = (RingResolution - 1) * 0.5f;
        float radius = RingResolution * 0.5f;
        for (int y = 0; y < RingResolution; y++)
        {
            for (int x = 0; x < RingResolution; x++)
            {
                float dx = (x - center) / radius;
                float dy = (y - center) / radius;
                float distance = Mathf.Sqrt(dx * dx + dy * dy);
                float alpha = 0f;
                if (distance <= 1f)
                {
                    // 안쪽은 옅게 채우고 가장자리 근처에 밝은 띠를 둡니다.
                    float band = Mathf.Clamp01(1f - Mathf.Abs(distance - 0.86f) / 0.14f);
                    alpha = Mathf.Max(0.16f * Mathf.Clamp01((1f - distance) / 0.1f), band);
                }
                pixels[y * RingResolution + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255f));
            }
        }

        var texture = new Texture2D(RingResolution, RingResolution, TextureFormat.RGBA32, false)
        {
            name = "FieldItem BlastRing",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        blastRing = Sprite.Create(texture, new Rect(0f, 0f, RingResolution, RingResolution), new Vector2(0.5f, 0.5f),
            RingResolution, 0, SpriteMeshType.FullRect);
        blastRing.name = texture.name;
        return blastRing;
    }
}
