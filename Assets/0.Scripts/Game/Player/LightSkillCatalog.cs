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
    /// <summary>불씨 산탄은 시간 차를 둔 연사가 아니라 한 번에 흩뿌립니다.</summary>
    public bool ScatterVolley => Id == 10010;
    /// <summary>화염 연사는 호 모양 베기가 아니라 정면 직선 연사입니다.</summary>
    public bool StraightRapidFire => Id == 10013;
    /// <summary>화염구·작열 구체는 맞으면 터집니다. 표에 반경이 없어 가로 폭의 두 배를 임시 반경으로 씁니다.</summary>
    public bool ExplodesOnHit => Id == 10004 || Id == 10015;
    /// <summary>작열 구체는 원 대신 불줄기가 사방으로 갈라집니다. 가닥 수는 6~8입니다.</summary>
    public bool ForksOnHit => Id == 10015;
    /// <summary>절멸의 불은 전방 직선을 가르는 균열에 불이 차오르는 그림(pixelfx sundering-fire)을 씁니다. 대지 파편은 없습니다.</summary>
    public bool SunderingFire => Id == 10017;
    /// <summary>화염 고리는 바닥 고리 대신 플레이어 둘레를 도는 불 회오리 그림(pixelfx flame-whirl)을 씁니다. 피해 판정은 기존 고리 그대로입니다.</summary>
    public bool FlameWhirl => Id == 10014;
    /// <summary>태양 탄막은 플레이어 둘레에 태양 문양을 그리고, 긴 가시 끝에서 적을 쫓는 흰 불꽃 미사일을 쏩니다(pixelfx sun-crest·sun-missile). 발수·간격·피해는 표 그대로입니다.</summary>
    public bool SunBarrage => Id == 10016;
    public float SplashRadius => Mathf.Max(0f, Width) * 2f;

    public LightSkillCast Presented()
    {
        if (!StraightRapidFire) return this;
        return new LightSkillCast(Id, Name, Element, MpCost, LightSkillShape.Projectile,
            Range, 0f, Width > 0.01f ? Width : 0.35f, Mathf.Max(1, Hits), 1, HitDelay, BaseValue, Factor);
    }

    public LightSkillCast WithStatUpgrades(int additionalProjectiles, float rangeMultiplier)
    {
        if (Shape == LightSkillShape.Projectile)
            return new LightSkillCast(Id, Name, Element, MpCost, Shape, Range, Angle, Width,
                Projectiles + additionalProjectiles, Hits, HitDelay, BaseValue, Factor);
        return new LightSkillCast(Id, Name, Element, MpCost, Shape, Range * rangeMultiplier, Angle,
            Width * rangeMultiplier, Projectiles, Hits, HitDelay, BaseValue, Factor);
    }

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
    public static bool TryCreateHeavy(CombatSkillRecord record, out LightSkillCast skill)
    {
        skill = default;
        // 화염 돌진은 전용 이동이 필요하며 십자 범위는 공통 판정에서 지원하지 않습니다.
        return record != null && record.Id >= 20000 && record.Id < 30000 && record.Id != 20002
            && record.CastType == "Charge" && record.CostType == "MP"
            && record.CostAmount >= 0f && !float.IsInfinity(record.CostAmount)
            && record.CastTime > 0f && !float.IsInfinity(record.CastTime)
            && LightSkillCast.TryFrom(record, out skill);
    }

    public static bool TryGet(int id, out LightSkillCast skill)
    {
        skill = default;
        if (id < 10000 || id >= 20000) return false;
        return CombatSkillTable.TryGet(id, out CombatSkillRecord record) && LightSkillCast.TryFrom(record, out skill);
    }

    public static int Count => CombatSkillTable.CountInRange(10000, 20000);
}
