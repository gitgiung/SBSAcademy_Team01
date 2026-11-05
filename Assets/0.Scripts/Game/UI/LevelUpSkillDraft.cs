using System;
using System.Collections.Generic;
using System.Globalization;

/// <summary>등급을 가중 추첨한 뒤 등급 안에서 균등 추첨합니다. 한 카드 묶음 안의 ID 중복은 허용하지 않습니다.</summary>
public static class LevelUpSkillDraft
{
    private sealed class Bucket
    {
        internal double Weight;
        internal readonly List<CombatSkillRecord> Skills = new List<CombatSkillRecord>();
    }

    public static List<CombatSkillRecord> Draw(CombatSkillTableFile table, int count,
        int minId, int maxId, Func<double> nextDouble, ISet<int> excludedIds = null)
    {
        if (table == null) throw new ArgumentNullException(nameof(table));
        if (nextDouble == null) throw new ArgumentNullException(nameof(nextDouble));
        var result = new List<CombatSkillRecord>();
        var byRarity = new Dictionary<string, Bucket>(StringComparer.OrdinalIgnoreCase);
        var buckets = new List<Bucket>();
        if (table.rarities != null)
            foreach (var rarity in table.rarities)
            {
                if (rarity == null || rarity.Weight <= 0f || float.IsNaN(rarity.Weight) || float.IsInfinity(rarity.Weight)) continue;
                string id = SkillRarityRecord.NormalizeId(rarity.Id);
                if (id.Length == 0 || byRarity.ContainsKey(id)) continue;
                var bucket = new Bucket { Weight = rarity.Weight };
                byRarity.Add(id, bucket);
                buckets.Add(bucket);
            }
        var seen = new HashSet<int>();
        if (table.skills != null)
            foreach (var skill in table.skills)
                if (skill != null && skill.Id >= minId && skill.Id < maxId
                    && (excludedIds == null || !excludedIds.Contains(skill.Id))
                    && byRarity.TryGetValue(SkillRarityRecord.NormalizeId(skill.Rarity), out var bucket) && seen.Add(skill.Id))
                    bucket.Skills.Add(skill);

        while (result.Count < count)
        {
            double total = 0;
            foreach (var bucket in buckets) if (bucket.Skills.Count > 0) total += bucket.Weight;
            if (total <= 0) break;
            double roll = Unit(nextDouble) * total;
            Bucket selected = null;
            foreach (var bucket in buckets)
            {
                if (bucket.Skills.Count == 0) continue;
                selected = bucket;
                if (roll < bucket.Weight) break;
                roll -= bucket.Weight;
            }
            int index = (int)(Unit(nextDouble) * selected.Skills.Count);
            result.Add(selected.Skills[index]);
            selected.Skills.RemoveAt(index);
        }
        return result;
    }

    private static double Unit(Func<double> nextDouble)
    {
        double value = nextDouble();
        if (double.IsNaN(value) || value < 0 || value >= 1)
            throw new ArgumentOutOfRangeException(nameof(nextDouble), "Random value must be in [0, 1).");
        return value;
    }

    public static string FormatDescription(CombatSkillRecord skill)
    {
        string value = string.IsNullOrEmpty(skill.SimpleDescription) ? skill.Description ?? "" : skill.SimpleDescription;
        return value.Replace("{Projectile_Count}", Number(skill.Projectiles))
            .Replace("{Base_Value}", Number(skill.BaseValue)).Replace("{Scaling_Factor}", Number(skill.Factor))
            .Replace("{Cast_Time}", Number(skill.CastTime)).Replace("{Hit_Count}", Number(skill.Hits))
            .Replace("{Hit_Delay}", Number(skill.HitDelay)).Replace("{Effect_Duration}", Number(skill.EffectDuration));
    }

    private static string Number(float value) => value.ToString("0.###", CultureInfo.InvariantCulture);
}
