using System;

/// <summary>등급별 스탯 가산 초이스. 불변 값 객체이며 적용은 하지 않습니다.</summary>
public sealed class LevelUpStatChoice : ILevelUpReward
{
    public const int IdBase = 91000;

    public LevelUpStatKind StatKind { get; }
    public float Amount { get; }
    public int Id { get; }
    public string Name { get; }
    public string Rarity { get; }
    public LevelUpRewardKind Kind => LevelUpRewardKind.Stat;

    public LevelUpStatChoice(LevelUpStatKind statKind, string rarity, float amount, string displayName)
    {
        if (string.IsNullOrWhiteSpace(rarity)) throw new ArgumentException("Rarity is required.", nameof(rarity));
        if (!(amount > 0f) || float.IsNaN(amount) || float.IsInfinity(amount))
            throw new ArgumentOutOfRangeException(nameof(amount));
        StatKind = statKind;
        Rarity = SkillRarityRecord.NormalizeId(rarity);
        Amount = amount;
        Name = string.IsNullOrWhiteSpace(displayName) ? DefaultName(statKind) : displayName.Trim();
        Id = ComposeId(statKind, Rarity);
    }

    public static int ComposeId(LevelUpStatKind statKind, string rarity)
    {
        string id = SkillRarityRecord.NormalizeId(rarity);
        int rarityCode = id.ToUpperInvariant() switch
        {
            "UNCOMMON" => 1,
            "RARE" => 2,
            "EPIC" => 3,
            "LEGENDARY" => 4,
            "MYTHIC" => 5,
            _ => 0
        };
        return IdBase + (int)statKind * 100 + rarityCode;
    }

    public static string DefaultName(LevelUpStatKind statKind) => statKind switch
    {
        LevelUpStatKind.Damage => "피해량 증가",
        _ => statKind.ToString()
    };
}
