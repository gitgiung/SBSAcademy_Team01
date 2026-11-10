using System;
using System.Collections.Generic;

/// <summary>레벨업 공격 스킬의 후보와 획득을 처리합니다.</summary>
public static class LevelUpSkillReward
{
    public static List<CombatSkillRecord> DrawChoices(CombatSkillTableFile table, PlayerLoadout loadout,
        int count, Func<double> nextDouble)
    {
        var excluded = new HashSet<int>();
        bool hasFreeSlot = loadout.HasFreeAttackSlot;
        if (table.skills != null)
            foreach (CombatSkillRecord skill in table.skills)
            {
                if (skill == null) continue;
                int stack = loadout.GetAttackSkillStack(skill.Id);
                if (stack >= PlayerLoadout.MaxAttackSkillStack || (!hasFreeSlot && stack == 0)
                    || (skill.Id >= 20000 && !LightSkillCatalog.TryCreateHeavy(skill, out _)))
                    excluded.Add(skill.Id);
            }
        return LevelUpSkillDraft.Draw(table, count, 10000, 30000, nextDouble, excluded);
    }

    public static bool TryAcquire(PlayerLoadout loadout, CombatSkillRecord skill)
    {
        if (loadout == null || skill == null || skill.Id < 10000 || skill.Id >= 30000) return false;
        if (skill.Id >= 20000 && !LightSkillCatalog.TryCreateHeavy(skill, out _)) return false;
        return loadout.TryAddOrUpgradeAttackSkill(new Data.SkillData
        {
            DataId = skill.Id,
            Name = skill.Name,
            Description = LevelUpSkillDraft.FormatDescription(skill),
            IconLabel = skill.Icon
        });
    }
}
