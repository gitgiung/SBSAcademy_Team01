using System;
using System.Collections.Generic;

/// <summary>
/// 스탯 종류·등급 → 가산값 순수 조회 테이블.
/// 호출자가 전달한 값만 보관합니다.
/// </summary>
public sealed class LevelUpStatBonusTable
{
    private static readonly string[] StatRarityOrder =
    {
        "Uncommon", "Rare", "Epic", "Legendary", "Mythic"
    };

    private readonly Dictionary<(LevelUpStatKind kind, string rarity), float> amounts =
        new Dictionary<(LevelUpStatKind, string), float>();

    public LevelUpStatBonusTable(IEnumerable<(LevelUpStatKind kind, string rarity, float amount)> entries)
    {
        foreach (var (kind, rarity, amount) in entries)
            amounts.Add((kind, SkillRarityRecord.NormalizeId(rarity)), amount);
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
