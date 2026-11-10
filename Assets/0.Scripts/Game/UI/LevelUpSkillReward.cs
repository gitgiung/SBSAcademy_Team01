/// <summary>
/// 선택한 전투 정의를 보유 목록에 등록하거나, 이미 가진 공격 스킬이면 스택을 올립니다.
/// 전략 스킬은 중복 없이 등록하며, 미장착이면 Tactical 버튼에 자동 장착합니다.
/// 미정인 패시브 수치는 추가하지 않습니다.
/// </summary>
public static class LevelUpSkillReward
{
    public static bool TryAcquire(PlayerLoadout loadout, CombatSkillRecord skill)
    {
        if (loadout == null || skill == null) return false;
        var data = new Data.SkillData
        {
            DataId = skill.Id,
            Name = skill.Name,
            Description = LevelUpSkillDraft.FormatDescription(skill),
            IconLabel = skill.Icon
        };
        if (skill.Id >= 10000 && skill.Id < 30000)
            return loadout.TryAddOrUpgradeAttackSkill(data);
        if (skill.Id < 30000 || skill.Id >= 40000) return false;
        if (!loadout.TryAddTacticalSkill(data)) return false;
        if (loadout.GetEquippedSkillId(PlayerSkillButton.Tactical) == 0)
            loadout.TryEquip(PlayerSkillButton.Tactical, skill.Id);
        return true;
    }
}
