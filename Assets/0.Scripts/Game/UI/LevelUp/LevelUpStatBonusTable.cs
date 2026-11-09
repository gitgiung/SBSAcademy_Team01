using System;
using System.Collections.Generic;

/// <summary>
/// 스탯 종류·등급 → 가산값 순수 조회 테이블.
/// temporary: 기획 확정 전 테스트용 상수이며 Strengthentable 연동 전입니다.
/// </summary>
public sealed class LevelUpStatBonusTable
{
    private static readonly string[] StatRarityOrder =
    {
        "Uncommon", "Rare", "Epic", "Legendary", "Mythic"
    };

    private readonly Dictionary<(LevelUpStatKind kind, string rarity), float> amounts =
        new Dictionary<(LevelUpStatKind, string), float>();

    private LevelUpStatBonusTable() { }

    /// <summary>temporary 피해량 가산. Common/Fusion은 넣지 않습니다.</summary>
    public static LevelUpStatBonusTable CreateTemporaryDamageDefaults()
    {
        var table = new LevelUpStatBonusTable();
        table.amounts[(LevelUpStatKind.Damage, "Uncommon")] = 0.05f;
        table.amounts[(LevelUpStatKind.Damage, "Rare")] = 0.08f;
        table.amounts[(LevelUpStatKind.Damage, "Epic")] = 0.12f;
        table.amounts[(LevelUpStatKind.Damage, "Legendary")] = 0.18f;
        table.amounts[(LevelUpStatKind.Damage, "Mythic")] = 0.25f;
        return table;
    }

    public static IReadOnlyList<string> EligibleRarities => StatRarityOrder;

    public bool TryGetAmount(LevelUpStatKind kind, string rarity, out float amount)
    {
        string id = SkillRarityRecord.NormalizeId(rarity);
        if (id.Length == 0)
        {
            amount = 0f;
            return false;
        }
        return amounts.TryGetValue((kind, id), out amount);
    }

    public bool HasAny(LevelUpStatKind kind)
    {
        foreach (var pair in amounts)
            if (pair.Key.kind == kind) return true;
        return false;
    }

    public IEnumerable<(string rarity, float amount)> Entries(LevelUpStatKind kind)
    {
        foreach (string rarity in StatRarityOrder)
            if (amounts.TryGetValue((kind, rarity), out float amount))
                yield return (rarity, amount);
    }
}
