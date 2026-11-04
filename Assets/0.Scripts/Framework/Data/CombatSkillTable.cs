using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>SkillDatatable.xlsx에서 변환한 전투 수치입니다. 연출 시트는 포함하지 않습니다.</summary>
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
}

[Serializable]
public sealed class CombatSkillTableFile
{
    public List<CombatSkillRecord> skills = new List<CombatSkillRecord>();
}

public static class CombatSkillTable
{
    private const string ResourcePath = "Data/CombatSkillTable";
    private static Dictionary<int, CombatSkillRecord> skills;
    private static bool missingLogged;

    public static bool TryGet(int id, out CombatSkillRecord record)
    {
        EnsureLoaded();
        if (skills != null && skills.TryGetValue(id, out record)) return true;
        record = null;
        return false;
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
    }
}
