using UnityEngine;

/// <summary>명시한 아이콘을 우선 사용하고, 없으면 스킬 ID에 대응하는 PPT 원본 그림을 표시합니다.</summary>
public static class SkillCardIcons
{
    public static Sprite Get(int skillId, string iconKey = null)
    {
        if (!string.IsNullOrEmpty(iconKey))
        {
            Sprite custom = Managers.Initialized ? Managers.Resource.Load<Sprite>(iconKey) : null;
            if (custom == null) custom = Resources.Load<Sprite>(iconKey);
            if (custom != null) return custom;
        }
        return skillId > 0 ? Resources.Load<Sprite>("SkillIcons/Skill_" + skillId) : null;
    }
}
