using UnityEngine;

public enum LightSkillShape
{
    Projectile,
    Circle,
    Line,
    Cone,
    Arc,
    Ring
}

/// <summary>엑셀에서 읽은 약공격 한 줄의 시전 값입니다. 그림과 셰이더는 여기에 없습니다.</summary>
public readonly struct LightSkillCast
{
    public readonly int Id;
    public readonly string Name;
    public readonly string Element;
    public readonly float MpCost;
    public readonly LightSkillShape Shape;
    public readonly float Range;
    public readonly float Angle;
    public readonly float Width;
    public readonly int Projectiles;
    public readonly int Hits;
    public readonly float HitDelay;
    public readonly float BaseValue;
    public readonly float Factor;

    public LightSkillCast(int id, string name, string element, float mpCost, LightSkillShape shape,
        float range, float angle, float width, int projectiles, int hits, float hitDelay, float baseValue, float factor)
    {
        Id = id;
        Name = name;
        Element = element;
        MpCost = mpCost;
        Shape = shape;
        Range = range;
        Angle = angle;
        Width = width;
        Projectiles = projectiles;
        Hits = hits;
        HitDelay = hitDelay;
        BaseValue = baseValue;
        Factor = factor;
    }

    public float Damage(float attack) => BaseValue + attack * Factor;

    public float ShotSpacing => Projectiles > 1 ? (HitDelay > 0f && HitDelay < 1f ? HitDelay : 0.12f) : 0f;
    public float RepeatSpacing => Hits > 1 ? (HitDelay > 0f && HitDelay < 1f ? HitDelay : 0.2f) : 0f;

    public static bool TryFrom(CombatSkillRecord record, out LightSkillCast skill)
    {
        skill = default;
        if (record == null || !TryShape(record.AreaType, out LightSkillShape shape)) return false;
        skill = new LightSkillCast(record.Id, record.Name, record.Element, record.CostAmount, shape,
            record.Range, record.Angle, record.Width, Mathf.Max(1, record.Projectiles), Mathf.Max(1, record.Hits),
            record.HitDelay, record.BaseValue, record.Factor);
        return true;
    }

    public static bool TryShape(string areaType, out LightSkillShape shape)
    {
        switch (areaType)
        {
            case "Projectile": shape = LightSkillShape.Projectile; return true;
            case "Circle": shape = LightSkillShape.Circle; return true;
            case "Line": shape = LightSkillShape.Line; return true;
            case "Cone": shape = LightSkillShape.Cone; return true;
            case "Arc": shape = LightSkillShape.Arc; return true;
            case "Ring": shape = LightSkillShape.Ring; return true;
            default: shape = default; return false;
        }
    }
}

public static class LightSkillCatalog
{
    public static bool TryGet(int id, out LightSkillCast skill)
    {
        skill = default;
        if (id < 10000 || id >= 20000) return false;
        return CombatSkillTable.TryGet(id, out CombatSkillRecord record) && LightSkillCast.TryFrom(record, out skill);
    }

    public static int Count => CombatSkillTable.CountInRange(10000, 20000);
}
