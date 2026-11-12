using UnityEngine;

/// <summary>일반 몬스터 처치 시 떨어지는 필드 아이템 종류입니다.</summary>
public enum FieldItemKind
{
    None = 0,
    HealPack = 1,
    Magnet = 2,
    Bomb = 3
}

/// <summary>
/// 필드 아이템 드랍 확률과 효과 수치입니다. 기획 확정 전 임시값이며
/// SceneEnemyPool Inspector의 Field Items에서 조정합니다.
/// </summary>
[System.Serializable]
public sealed class FieldItemDropSettings
{
    [Header("드랍 확률 (임시값, 일반 몬스터 처치 1회당 0~1)")]
    [Range(0f, 1f)] public float healPackChance = 0.007f;
    [Range(0f, 1f)] public float magnetChance = 0.004f;
    [Range(0f, 1f)] public float bombChance = 0.003f;
    [Tooltip("필드에 동시에 놓일 수 있는 아이템 수입니다. 가득 차면 새로 떨어지지 않습니다.")]
    [Min(0)] public int maxFieldItems = 12;

    [Header("효과 (임시값)")]
    [Tooltip("힐팩이 회복하는 최대 체력 대비 비율입니다.")]
    [Range(0f, 1f)] public float healPackHealthRatio = 0.3f;
    [Tooltip("자석을 먹었을 때 경험치가 끌려오는 최소 속도입니다.")]
    [Min(0f)] public float magnetAttractionSpeed = 18f;
    [Tooltip("폭탄이 카메라 화면 밖으로 이 거리까지의 일반 몬스터를 처치합니다.")]
    [Min(0f)] public float bombViewMargin = 0.5f;
    [Tooltip("플레이어 중심에서 이 거리 안으로 들어오면 아이템을 획득합니다.")]
    [Min(0f)] public float collectDistance = 0.9f;

    [Header("외형 (비우면 코드로 만든 픽셀 그림 사용)")]
    public Sprite healPackSprite;
    public Sprite magnetSprite;
    public Sprite bombSprite;
    [Min(0.01f)] public float itemScale = 0.9f;

    /// <summary>0~1 난수 한 번으로 드랍할 종류를 정합니다. 힐팩 → 자석 → 폭탄 순으로 구간을 차지합니다.</summary>
    public FieldItemKind Roll(float roll)
    {
        if (float.IsNaN(roll) || roll < 0f) return FieldItemKind.None;
        float threshold = Chance(healPackChance);
        if (roll < threshold) return FieldItemKind.HealPack;
        threshold += Chance(magnetChance);
        if (roll < threshold) return FieldItemKind.Magnet;
        threshold += Chance(bombChance);
        return roll < threshold ? FieldItemKind.Bomb : FieldItemKind.None;
    }

    /// <summary>잘못된 확률은 0으로, 범위를 벗어난 확률은 0~1로 맞춥니다.</summary>
    public static float Chance(float value)
    {
        if (float.IsNaN(value) || value <= 0f) return 0f;
        return value >= 1f ? 1f : value;
    }

    public float HealAmount(float maxHealth)
    {
        if (float.IsNaN(maxHealth) || float.IsInfinity(maxHealth) || maxHealth <= 0f) return 0f;
        return maxHealth * Chance(healPackHealthRatio);
    }

    public Sprite SpriteFor(FieldItemKind kind)
    {
        switch (kind)
        {
            case FieldItemKind.HealPack: return healPackSprite;
            case FieldItemKind.Magnet: return magnetSprite;
            case FieldItemKind.Bomb: return bombSprite;
            default: return null;
        }
    }

    /// <summary>중심에서 가로·세로 절반 길이 안에 있는지 판정합니다. 경계는 포함합니다.</summary>
    public static bool IsInsideView(Vector2 point, Vector2 center, float halfWidth, float halfHeight)
    {
        if (float.IsNaN(halfWidth) || float.IsNaN(halfHeight) || halfWidth < 0f || halfHeight < 0f) return false;
        float x = point.x - center.x;
        float y = point.y - center.y;
        return x >= -halfWidth && x <= halfWidth && y >= -halfHeight && y <= halfHeight;
    }
}
