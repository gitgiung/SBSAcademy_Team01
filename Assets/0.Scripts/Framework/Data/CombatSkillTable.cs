using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>SkillDatatable.xlsx의 전투 정의입니다. 실행 중 상태는 보관하지 않습니다.</summary>
[Serializable]
public sealed class CombatSkillRecord
{
    public int Id;
    public string Name;
    public string Element;
    public string CostType;
    public float CostAmount;
    public float Cooldown;
    public string CastType;
    public float CastTime;
    public string AreaType;
    public float Range;
    public float Angle;
    public float Width;
    public int Projectiles;
    public int Hits;
    public float HitDelay;
    public float BaseValue;
    public string ScalingStat;
    public float Factor;
    public string Rarity;
    public string Icon;
    public string TargetType;
    public string AnimationName;
    public string EffectPrefab;
    public string SoundCue;
    public string EffectType;
    public string StatusEffectId;
    public float EffectDuration;
    public bool SuperArmor;
    public string CrowdControl;
    // 사거리와 범위 크기를 구분합니다. 기존 Range 값과 해석은 유지합니다.
    public float Radius;
    public float Length;
    public string Description;
    public string SimpleDescription;
    public string Explanation;
    // 보유 중 자동 시전. 0이면 주기가 아직 없어 발사하지 않고, 배율 0은 피해 0.2를 뜻합니다.
    public float PassiveCooldown;
    public float PassiveScale;

    public const float DefaultPassiveScale = 0.2f;

    public string GetRarityId()
    {
        string rarity = SkillRarityRecord.NormalizeId(Rarity);
        return rarity.Length == 0 && Id >= 30000 && Id < 40000 ? "Common" : rarity;
    }

    public bool TryOwnedPassive(out float cooldown, out float scale)
    {
        cooldown = 0f;
        scale = DefaultPassiveScale;
        if (Id < 10000 || Id >= 30000) return false;
        if (!(PassiveCooldown > 0f) || float.IsNaN(PassiveCooldown) || float.IsInfinity(PassiveCooldown)) return false;
        if (PassiveScale > 0f && !float.IsNaN(PassiveScale) && !float.IsInfinity(PassiveScale)) scale = PassiveScale;
        cooldown = PassiveCooldown;
        return true;
    }
}

[Serializable]
public sealed class SkillRarityRecord
{
    public string Id;
    public string Name;
    public float Weight;
    public string Frame;
    public string ColorHex;

    public static string NormalizeId(string id)
    {
        string value = (id ?? "").Trim();
        return string.Equals(value, "Mystic", StringComparison.OrdinalIgnoreCase) ? "Mythic" : value;
    }
}

[Serializable]
public sealed class CombatSkillTableFile
{
    public List<CombatSkillRecord> skills = new List<CombatSkillRecord>();
    public List<SkillRarityRecord> rarities = new List<SkillRarityRecord>();
}

public static class CombatSkillTable
{
    private const string ResourcePath = "Data/CombatSkillTable";
    private static Dictionary<int, CombatSkillRecord> skills;
    private static List<SkillRarityRecord> rarities;
    private static bool missingLogged;

    public static bool TryGet(int id, out CombatSkillRecord record)
    {
        EnsureLoaded();
        if (skills != null && skills.TryGetValue(id, out record)) return true;
        record = null;
        return false;
    }

    public static bool TryGetRarity(string id, out SkillRarityRecord rarity)
    {
        EnsureLoaded();
        rarity = rarities?.Find(value => value != null
            && string.Equals(SkillRarityRecord.NormalizeId(value.Id), id, StringComparison.OrdinalIgnoreCase));
        return rarity != null;
    }

    public static int Count
    {
        get
        {
            EnsureLoaded();
            return skills != null ? skills.Count : 0;
        }
    }

    public static int CountInRange(int minInclusive, int maxExclusive)
    {
        EnsureLoaded();
        if (skills == null) return 0;
        int count = 0;
        foreach (int id in skills.Keys)
            if (id >= minInclusive && id < maxExclusive) count++;
        return count;
    }

    public static CombatSkillTableFile CreateSnapshot()
    {
        EnsureLoaded();
        var result = new CombatSkillTableFile();
        if (skills != null) result.skills.AddRange(skills.Values);
        if (rarities != null) result.rarities.AddRange(rarities);
        return result;
    }

    private static void EnsureLoaded()
    {
        if (skills != null) return;
        TextAsset asset = Resources.Load<TextAsset>(ResourcePath);
        if (asset == null)
        {
            skills = new Dictionary<int, CombatSkillRecord>();
            if (!missingLogged)
            {
                missingLogged = true;
                Debug.LogError("CombatSkillTable이 없습니다. Unity에서 Tools > Parse Combat Skill Table을 실행하세요.");
            }
            return;
        }

        CombatSkillTableFile file = JsonUtility.FromJson<CombatSkillTableFile>(asset.text);
        var loaded = new Dictionary<int, CombatSkillRecord>();
        if (file?.skills != null)
        {
            foreach (CombatSkillRecord skill in file.skills)
            {
                if (skill == null || skill.Id <= 0) continue;
                loaded[skill.Id] = skill;
            }
        }
        skills = loaded;
        rarities = file?.rarities ?? new List<SkillRarityRecord>();
    }
}
