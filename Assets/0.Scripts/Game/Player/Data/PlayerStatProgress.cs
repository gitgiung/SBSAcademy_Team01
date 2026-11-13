using System;
using System.Collections.Generic;
using UnityEngine;

public enum PlayerStat
{
    HP, HP_Regen, ATK, Move_Speed, DEF, MP_Regen, Cooldown, Elm_All_ATK,
    Projectile_Count, Projectile_Size, Skill_Range, XP_Gain, Pickup_Radius
}

[Serializable]
public sealed class PlayerStatCard
{
    public PlayerStat Stat;
    public string Name;
    public float[] Values;
    public float Limit;
    public float Weight;

    public float ValueAt(int level) => Limit > 0f ? Math.Min(Values[level], Limit) : Values[level];

    public string FormatValue(int level)
    {
        float value = ValueAt(level);
        switch (Stat)
        {
            case PlayerStat.ATK:
            case PlayerStat.XP_Gain: return $"×{value:0.##}";
            case PlayerStat.HP_Regen: return $"{value:P1}/초";
            case PlayerStat.MP_Regen: return $"{value:0.##}/초";
            case PlayerStat.Move_Speed:
            case PlayerStat.Cooldown:
            case PlayerStat.Elm_All_ATK:
            case PlayerStat.Projectile_Size:
            case PlayerStat.Skill_Range:
            case PlayerStat.Pickup_Radius: return $"{value:P0}";
            default: return $"{value:0.##}";
        }
    }
}

[Serializable]
public sealed class PlayerStatTable
{
    public PlayerStatCard[] Cards;
    private static PlayerStatCard[] loadedCards;

    public static IReadOnlyList<PlayerStatCard> LoadCards()
    {
        if (loadedCards != null) return loadedCards;
        TextAsset asset = Resources.Load<TextAsset>("Data/PlayerStatTable");
        if (asset != null)
        {
            try { loadedCards = JsonUtility.FromJson<PlayerStatTable>(asset.text)?.Cards; }
            catch (ArgumentException error) { Debug.LogError($"플레이어 스탯 표를 읽을 수 없습니다: {error.Message}"); }
        }
        if (loadedCards == null)
        {
            Debug.LogError("플레이어 스탯 카드 데이터가 없습니다. Tools > Parse Player Stat Table을 확인하세요.");
            loadedCards = Array.Empty<PlayerStatCard>();
        }
        return loadedCards;
    }
}

/// <summary>한 판의 스탯 카드 선택 상태를 보관합니다.</summary>
public sealed class PlayerStatProgress
{
    private readonly Dictionary<PlayerStat, (int level, float value)> upgrades = new();

    public int Level(PlayerStat stat) => upgrades.TryGetValue(stat, out var entry) ? entry.level : 0;
    public float Value(PlayerStat stat, float baseline = 0f) =>
        upgrades.TryGetValue(stat, out var entry) ? entry.value : baseline;

    public bool Remove(PlayerStat stat) => upgrades.Remove(stat);

    private bool CanUpgrade(PlayerStatCard card)
    {
        int level = Level(card.Stat);
        return card.Values != null && level + 1 < card.Values.Length && card.ValueAt(level + 1) > card.ValueAt(level);
    }

    public bool TryUpgrade(PlayerStatCard card, int nextLevel)
    {
        if (nextLevel != Level(card.Stat) + 1 || !CanUpgrade(card)) return false;
        upgrades[card.Stat] = (nextLevel, card.ValueAt(nextLevel));
        return true;
    }

    public List<PlayerStatCard> Draw(IReadOnlyList<PlayerStatCard> cards, int count, Func<double> nextDouble)
    {
        var pool = new List<PlayerStatCard>();
        double total = 0;
        foreach (PlayerStatCard card in cards)
        {
            if (card == null || !(card.Weight > 0f) || float.IsInfinity(card.Weight) || !CanUpgrade(card)) continue;
            pool.Add(card);
            total += card.Weight;
        }

        var result = new List<PlayerStatCard>(count);
        while (result.Count < count && pool.Count > 0)
        {
            double roll = nextDouble() * total;
            int index = 0;
            while (index < pool.Count - 1 && roll >= pool[index].Weight) roll -= pool[index++].Weight;
            PlayerStatCard card = pool[index];
            result.Add(card);
            total -= card.Weight;
            pool.RemoveAt(index);
        }
        return result;
    }
}
