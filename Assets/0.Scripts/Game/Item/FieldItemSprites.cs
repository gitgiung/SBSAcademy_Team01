using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 필드 아이템과 폭탄 폭발 그림을 Resources/FieldItems에서 불러와 공유합니다.
/// 그림은 tools/FieldItemArt/bake.mjs로 굽습니다. 불러오지 못하면 코드로 만든 그림으로 대신합니다.
/// </summary>
public static class FieldItemSprites
{
    /// <summary>Resources 아래 폴더 이름입니다. 굽기 스크립트의 출력 폴더와 같아야 합니다.</summary>
    public const string ResourceFolder = "FieldItems";
    public const string BlastRingName = "BlastRing";
    /// <summary>폭발 프레임 이름의 앞부분입니다. 뒤에 두 자리 번호가 붙습니다.</summary>
    public const string BlastFramePrefix = "BombBlast_";
    public const float BlastFrameDuration = 1f / 30f;
    private const int RingResolution = 64;

    private static readonly Sprite[] items = new Sprite[4];
    private static Sprite blastRing;
    private static Sprite[] blastFrames;

    /// <summary>종류별 그림 파일 이름입니다. 종류가 없으면 null입니다.</summary>
    public static string ResourceName(FieldItemKind kind)
    {
        switch (kind)
        {
            case FieldItemKind.HealPack: return "HealPack";
            case FieldItemKind.Magnet: return "Magnet";
            case FieldItemKind.Bomb: return "Bomb";
            default: return null;
        }
    }

    /// <summary>종류별 그림입니다. 종류가 없으면 null입니다.</summary>
    public static Sprite Get(FieldItemKind kind)
    {
        int index = (int)kind;
        string name = ResourceName(kind);
        if (name == null || index <= 0 || index >= items.Length) return null;
        // 도메인 리로드를 끈 에디터에서 이전 Play의 텍스처가 파괴됐으면 다시 준비합니다.
        if (items[index] != null) return items[index];

        Sprite loaded = Resources.Load<Sprite>(ResourceFolder + "/" + name);
        items[index] = loaded != null ? loaded : CreateFallback(kind);
        return items[index];
    }

    /// <summary>화면 전체로 퍼지는 파동의 흰 고리입니다. 색과 크기는 렌더러에서 정합니다.</summary>
    public static Sprite BlastRing()
    {
        if (blastRing != null) return blastRing;
        Sprite loaded = Resources.Load<Sprite>(ResourceFolder + "/" + BlastRingName);
        blastRing = loaded != null ? loaded : CreateFallbackRing();
        return blastRing;
    }

    /// <summary>폭탄 폭발 프레임을 이름순으로 돌려줍니다. 없으면 빈 배열이라 파동 고리만 재생됩니다.</summary>
    public static Sprite[] BlastFrames()
    {
        if (blastFrames != null && blastFrames.Length > 0 && blastFrames[0] != null) return blastFrames;

        var frames = new List<Sprite>();
        Sprite[] loaded = Resources.LoadAll<Sprite>(ResourceFolder);
        if (loaded != null)
            foreach (Sprite sprite in loaded)
                if (sprite != null && sprite.name != null
                    && sprite.name.StartsWith(BlastFramePrefix, System.StringComparison.Ordinal)) frames.Add(sprite);
        frames.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        blastFrames = frames.ToArray();
        return blastFrames;
    }

    private static Sprite CreateFallback(FieldItemKind kind)
    {
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
        Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size, 0,
            SpriteMeshType.FullRect);
        sprite.name = texture.name;
        return sprite;
    }

    private static Sprite CreateFallbackRing()
    {
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
        Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, RingResolution, RingResolution),
            new Vector2(0.5f, 0.5f), RingResolution, 0, SpriteMeshType.FullRect);
        sprite.name = texture.name;
        return sprite;
    }
}
