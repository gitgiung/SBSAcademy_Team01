/// <summary>
/// 선택한 전투 정의를 보유 목록에 등록하거나, 이미 가진 스킬이면 스택을 올립니다.
/// 버튼 장착과 미정인 패시브 수치는 추가하지 않습니다.
/// </summary>
public static class LevelUpSkillReward
{
    public static bool TryAcquire(PlayerLoadout loadout, CombatSkillRecord skill)
    {
        if (loadout == null || skill == null || skill.Id < 10000 || skill.Id >= 30000) return false;
        return loadout.TryAddOrUpgradeAttackSkill(new Data.SkillData
        {
            DataId = skill.Id,
            Name = skill.Name,
            Description = LevelUpSkillDraft.FormatDescription(skill),
            IconLabel = skill.Icon
        });
    }
}
