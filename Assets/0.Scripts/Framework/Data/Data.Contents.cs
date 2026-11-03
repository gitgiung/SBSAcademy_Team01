using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Define;


namespace Data
{
    #region SkillData Serialization Example
    [Serializable]
    public class SkillData
    {
        public int DataId;
        public string Name;
        public string ClassName;
        public string Description;
        public int ProjectileId;
        public string PrefabLabel;
        public string IconLabel;
        public string AnimName;
        public float CoolTime;
        public float DamageMultiplier;
        public float Duration;
        public float AnimImpactDuration;
        public string CastingSound;
        public float SkillRange;
        public float ScaleMultiplier;
        public int TargetCount;
        public List<int> EffectIds = new List<int>();
        public int NextLevelId;
        public int AoEId;
        public EEffectSize EffectSize;
        // 메타데이터만 작성된 스킬은 비용0/피해0의 실행 스킬로 취급하지 않습니다.
        public bool ActiveConfigured;
        public float MPCost;
        public float BaseDamage;
        // 패시브와 액티브의 피해/주기는 서로 독립적인 설정입니다.
        public bool PassiveConfigured;
        public string PassivePrefabKey;
        public float PassiveDamage;
        public float PassiveInterval;
        public float PassiveSpeed;
        public float PassiveRange;
    }

    [Serializable]
    public class SkillDataLoader : ILoader<int, SkillData>
    {
        public List<SkillData> skills = new List<SkillData>();

        public Dictionary<int, SkillData> MakeDict()
        {
            Dictionary<int, SkillData> dict = new Dictionary<int, SkillData>();
            foreach (SkillData skill in skills)
                dict.Add(skill.DataId, skill);
            return dict;
        }
    }
    #endregion
}
