using UnityEngine;

/// <summary>필드 아이템의 16×16 임시 픽셀 그림입니다. 정식 스프라이트가 지정되면 쓰이지 않습니다.</summary>
public static class FieldItemPixelArt
{
    public const int Size = 16;

    private static readonly string[] HealPackRows =
    {
        "................",
        ".....kkkkkk.....",
        "....kwwwwwwk....",
        "....kwkkkkwk....",
        "..kkkkkkkkkkkk..",
        ".kwwwwwwwwwwwwk.",
        ".kwwwwwrrwwwwwk.",
        ".kwwwwwrrwwwwwk.",
        ".kwwwrrrrrrwwwk.",
        ".kwwwrrrrrrwwwk.",
        ".kwwwwwrrwwwwwk.",
        ".kwwwwwrrwwwwwk.",
        ".kssssssssssssk.",
        "..kkkkkkkkkkkk..",
        "................",
        "................"
    };

    private static readonly string[] MagnetRows =
    {
        "................",
        "..kkkk....kkkk..",
        ".kwwwwk..kwwwwk.",
        ".kwwwwk..kwwwwk.",
        ".kkkkkk..kkkkkk.",
        ".krrrrk..krrrrk.",
        ".krrrrk..krrrrk.",
        ".krrrrk..krrrrk.",
        ".krrrrk..krrrrk.",
        ".krrrrkkkkrrrrk.",
        ".krrrrrrrrrrrrk.",
        ".kdrrrrrrrrrrdk.",
        "..kddrrrrrrddk..",
        "...kkddddddkk...",
        ".....kkkkkk.....",
        "................"
    };

    // 바닥이 어두운 아스팔트라 몸통이 묻히지 않도록 폭탄만 밝은 외곽선을 씁니다.
    private static readonly string[] BombRows =
    {
        "..........y.....",
        ".........yoy....",
        ".........b......",
        "........b.......",
        ".....ssssss.....",
        "....sggggggs....",
        "...sgghggggns...",
        "..sgghhgggggns..",
        "..sghhggggggns..",
        "..sgggggggggns..",
        "..sgggggggggns..",
        "..sggggggggnns..",
        "...sggggggnns...",
        "....snnnnnns....",
        ".....ssssss.....",
        "................"
    };

    /// <summary>위에서 아래 순서의 행입니다. 종류가 없으면 null입니다.</summary>
    public static string[] Rows(FieldItemKind kind)
    {
        switch (kind)
        {
            case FieldItemKind.HealPack: return HealPackRows;
            case FieldItemKind.Magnet: return MagnetRows;
            case FieldItemKind.Bomb: return BombRows;
            default: return null;
        }
    }

    public static bool TryColor(char symbol, out Color32 color)
    {
        switch (symbol)
        {
            case '.': color = new Color32(0, 0, 0, 0); return true;
            case 'k': color = new Color32(26, 20, 35, 255); return true;
            case 'w': color = new Color32(244, 247, 251, 255); return true;
            case 's': color = new Color32(195, 203, 219, 255); return true;
            case 'r': color = new Color32(229, 72, 77, 255); return true;
            case 'd': color = new Color32(168, 36, 58, 255); return true;
            case 'g': color = new Color32(61, 66, 87, 255); return true;
            case 'h': color = new Color32(154, 163, 191, 255); return true;
            case 'n': color = new Color32(38, 42, 59, 255); return true;
            case 'b': color = new Color32(185, 121, 58, 255); return true;
            case 'y': color = new Color32(255, 224, 102, 255); return true;
            case 'o': color = new Color32(255, 138, 42, 255); return true;
            default: color = new Color32(0, 0, 0, 0); return false;
        }
    }

    /// <summary>텍스처에 바로 넣을 수 있도록 아래 행부터 채운 픽셀입니다. 그림이 잘못됐으면 null입니다.</summary>
    public static Color32[] BuildPixels(FieldItemKind kind)
    {
        string[] rows = Rows(kind);
        if (rows == null || rows.Length != Size) return null;
        var pixels = new Color32[Size * Size];
        for (int row = 0; row < Size; row++)
        {
            string line = rows[row];
            if (line == null || line.Length != Size) return null;
            int textureRow = Size - 1 - row;
            for (int column = 0; column < Size; column++)
            {
                if (!TryColor(line[column], out Color32 color)) return null;
                pixels[textureRow * Size + column] = color;
            }
        }
        return pixels;
    }
}
