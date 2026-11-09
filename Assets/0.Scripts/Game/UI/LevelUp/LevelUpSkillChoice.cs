using System;

/// <summary>스킬 표 한 줄을 불변 초이스로 감쌉니다. 보유 등록은 하지 않습니다.</summary>
public sealed class LevelUpSkillChoice : ILevelUpReward
{
    public CombatSkillRecord Skill { get; }
    public int Id => Skill.Id;
    public string Name => Skill.Name ?? string.Empty;
    public string Rarity => SkillRarityRecord.NormalizeId(Skill.Rarity);
    public LevelUpRewardKind Kind => LevelUpRewardKind.Skill;

    public LevelUpSkillChoice(CombatSkillRecord skill)
    {
        Skill = skill ?? throw new ArgumentNullException(nameof(skill));
    }
}
