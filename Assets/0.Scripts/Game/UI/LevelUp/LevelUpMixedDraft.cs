using System;
using System.Collections.Generic;

/// <summary>
/// 스킬·스탯 혼합 추첨 순수 함수. Unity·팝업·보유 상태를 읽거나 쓰지 않습니다.
/// 스킬 다리는 기존 <see cref="LevelUpSkillDraft.Draw"/>를 재사용합니다.
/// </summary>
public static class LevelUpMixedDraft
{
    public const double DefaultSkillShare = 0.75;
    public const double DefaultStatShare = 0.25;
    public const int DefaultCount = 3;

    public static List<ILevelUpReward> DrawChoices(
        CombatSkillTableFile table,
        LevelUpStatBonusTable bonuses,
        int count,
        int skillMinId,
        int skillMaxId,
        Func<double> nextDouble,
        ISet<int> excludedSkillIds = null,
        double skillShare = DefaultSkillShare,
        double statShare = DefaultStatShare,
        LevelUpStatKind statKind = LevelUpStatKind.Damage)
    {
        if (table == null) throw new ArgumentNullException(nameof(table));
        if (bonuses == null) throw new ArgumentNullException(nameof(bonuses));
        if (nextDouble == null) throw new ArgumentNullException(nameof(nextDouble));
        if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
        if (skillShare < 0 || statShare < 0 || (skillShare <= 0 && statShare <= 0))
            throw new ArgumentOutOfRangeException(nameof(skillShare), "Skill/stat shares must allow a positive total.");

        var result = new List<ILevelUpReward>(count);
        var excluded = excludedSkillIds == null ? new HashSet<int>() : new HashSet<int>(excludedSkillIds);
        var usedStatRarities = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        while (result.Count < count)
        {
            bool skillOk = skillShare > 0 && HasSkillCandidate(table, skillMinId, skillMaxId, excluded);
            bool statOk = statShare > 0 && HasStatCandidate(bonuses, statKind, usedStatRarities, table);
            if (!skillOk && !statOk) break;

            bool pickSkill;
            if (skillOk && statOk)
            {
                double total = skillShare + statShare;
                pickSkill = Unit(nextDouble) * total < skillShare;
            }
            else pickSkill = skillOk;

            if (pickSkill)
            {
                List<CombatSkillRecord> drawn = LevelUpSkillDraft.Draw(
                    table, 1, skillMinId, skillMaxId, nextDouble, excluded);
                if (drawn.Count == 0)
                {
                    if (!statOk) break;
                    if (!TryAddStat(result, bonuses, statKind, usedStatRarities, table, nextDouble)) break;
                    continue;
                }
                CombatSkillRecord skill = drawn[0];
                excluded.Add(skill.Id);
                result.Add(new LevelUpSkillChoice(skill));
            }
            else if (!TryAddStat(result, bonuses, statKind, usedStatRarities, table, nextDouble))
            {
                if (!skillOk) break;
                List<CombatSkillRecord> drawn = LevelUpSkillDraft.Draw(
                    table, 1, skillMinId, skillMaxId, nextDouble, excluded);
                if (drawn.Count == 0) break;
                excluded.Add(drawn[0].Id);
                result.Add(new LevelUpSkillChoice(drawn[0]));
            }
        }
        return result;
    }

    private static bool TryAddStat(
        List<ILevelUpReward> result,
        LevelUpStatBonusTable bonuses,
        LevelUpStatKind statKind,
        HashSet<string> usedStatRarities,
        CombatSkillTableFile table,
        Func<double> nextDouble)
    {
        LevelUpStatChoice choice = DrawStat(bonuses, statKind, usedStatRarities, table, nextDouble);
        if (choice == null) return false;
        usedStatRarities.Add(choice.Rarity);
        result.Add(choice);
        return true;
    }

    private static LevelUpStatChoice DrawStat(
        LevelUpStatBonusTable bonuses,
        LevelUpStatKind statKind,
        HashSet<string> usedStatRarities,
        CombatSkillTableFile table,
        Func<double> nextDouble)
    {
        var buckets = new List<(string rarity, float amount, double weight)>();
        double total = 0;
        if (table.rarities != null)
            foreach (SkillRarityRecord rarity in table.rarities)
            {
                if (rarity == null || rarity.Weight <= 0f || float.IsNaN(rarity.Weight) || float.IsInfinity(rarity.Weight))
                    continue;
                string id = SkillRarityRecord.NormalizeId(rarity.Id);
                if (id.Length == 0 || usedStatRarities.Contains(id)) continue;
                if (!bonuses.TryGetAmount(statKind, id, out float amount)) continue;
                buckets.Add((id, amount, rarity.Weight));
                total += rarity.Weight;
            }
        if (total <= 0 || buckets.Count == 0) return null;
        double roll = Unit(nextDouble) * total;
        for (int i = 0; i < buckets.Count; i++)
        {
            if (roll < buckets[i].weight || i == buckets.Count - 1)
                return new LevelUpStatChoice(statKind, buckets[i].rarity, buckets[i].amount,
                    LevelUpStatChoice.DefaultName(statKind));
            roll -= buckets[i].weight;
        }
        return null;
    }

    private static bool HasSkillCandidate(
        CombatSkillTableFile table, int minId, int maxId, ISet<int> excluded)
    {
        if (table.skills == null || table.rarities == null) return false;
        var weighted = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (SkillRarityRecord rarity in table.rarities)
        {
            if (rarity == null || rarity.Weight <= 0f || float.IsNaN(rarity.Weight) || float.IsInfinity(rarity.Weight))
                continue;
            string id = SkillRarityRecord.NormalizeId(rarity.Id);
            if (id.Length > 0) weighted.Add(id);
        }
        foreach (CombatSkillRecord skill in table.skills)
        {
            if (skill == null || skill.Id < minId || skill.Id >= maxId) continue;
            if (excluded != null && excluded.Contains(skill.Id)) continue;
            if (weighted.Contains(SkillRarityRecord.NormalizeId(skill.Rarity))) return true;
        }
        return false;
    }

    private static bool HasStatCandidate(
        LevelUpStatBonusTable bonuses,
        LevelUpStatKind statKind,
        HashSet<string> usedStatRarities,
        CombatSkillTableFile table)
    {
        if (table.rarities == null) return false;
        foreach (SkillRarityRecord rarity in table.rarities)
        {
            if (rarity == null || rarity.Weight <= 0f || float.IsNaN(rarity.Weight) || float.IsInfinity(rarity.Weight))
                continue;
            string id = SkillRarityRecord.NormalizeId(rarity.Id);
            if (id.Length == 0 || usedStatRarities.Contains(id)) continue;
            if (bonuses.TryGetAmount(statKind, id, out _)) return true;
        }
        return false;
    }

    private static double Unit(Func<double> nextDouble)
    {
        double value = nextDouble();
        if (double.IsNaN(value) || value < 0 || value >= 1)
            throw new ArgumentOutOfRangeException(nameof(nextDouble), "Random value must be in [0, 1).");
        return value;
    }
}
