using System;
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

    /// <summary>특수 강화 한 번의 배율입니다. 플레이테스트 초기값이며 기획 확정 수치가 아닙니다.</summary>
    public const float UpgradeStep = 1.25f;

    /// <summary>이 모양이 고를 수 있는 특수 강화입니다. 레벨과 상관없이 같은 목록에서 균등하게 뽑습니다.</summary>
    public static SkillUpgradeKind[] UpgradeChoices(LightSkillShape shape)
    {
        if (shape == LightSkillShape.Projectile)
            return new[] { SkillUpgradeKind.Projectiles, SkillUpgradeKind.Range };
        if (shape == LightSkillShape.Cone || shape == LightSkillShape.Arc)
            return new[] { SkillUpgradeKind.Range, SkillUpgradeKind.Angle };
        return new[] { SkillUpgradeKind.Range, SkillUpgradeKind.Hits };
    }

    /// <summary>nextDouble은 [0, 1)입니다. 레벨은 받지 않습니다.</summary>
    public SkillUpgradeKind RollUpgrade(Func<double> nextDouble)
    {
        SkillUpgradeKind[] choices = UpgradeChoices(Shape);
        double roll = nextDouble != null ? nextDouble() : 0;
        if (roll < 0) roll = 0;
        if (roll >= 1) roll = 0.999999;
        int index = (int)(roll * choices.Length);
        if (index >= choices.Length) index = choices.Length - 1;
        return choices[index];
    }

    /// <summary>이 모양의 후보가 아니면 값을 바꾸지 않습니다. 같은 종류를 여러 번 고르면 같은 폭으로 겹칩니다.</summary>
    public LightSkillCast WithUpgrade(SkillUpgradeKind kind)
    {
        if (!Offers(kind)) return this;
        switch (kind)
        {
            case SkillUpgradeKind.Projectiles:
                return Copy(Range, Angle, Width, Projectiles + 1, Hits);
            case SkillUpgradeKind.Range:
                float width = Shape == LightSkillShape.Projectile ? Width : Width * UpgradeStep;
                return Copy(Range * UpgradeStep, Angle, width, Projectiles, Hits);
            case SkillUpgradeKind.Hits:
                return Copy(Range, Angle, Width, Projectiles, Hits + 1);
            case SkillUpgradeKind.Angle:
                return Copy(Range, Angle * UpgradeStep, Width, Projectiles, Hits);
            default:
                return this;
        }
    }

    public LightSkillCast WithUpgrades(SkillUpgradeKind[] upgrades)
    {
        LightSkillCast cast = this;
        if (upgrades == null) return cast;
        for (int i = 0; i < upgrades.Length; i++) cast = cast.WithUpgrade(upgrades[i]);
        return cast;
    }

    /// <summary>레벨업 카드의 다음 한 줄입니다. 3레벨까지는 피해, 4레벨부터는 next 한 종류만 적습니다.</summary>
    public string DescribeNext(int currentLevel, SkillUpgradeKind next)
    {
        int nextLevel = currentLevel + 1;
        if (nextLevel < 4)
        {
            float damage = PlayerLoadout.DamageMultiplierForStack(nextLevel);
            if (damage > PlayerLoadout.DamageMultiplierForStack(currentLevel) + 0.001f)
                return $"강화 {currentLevel}→{nextLevel} (피해 ×{damage:0.#})";
            return $"강화 {currentLevel}→{nextLevel}";
        }
        LightSkillCast after = WithUpgrade(next);
        string detail = "";
        if (after.Projectiles != Projectiles) detail = $"투사체 {Projectiles}→{after.Projectiles}";
        else if (after.Hits != Hits) detail = $"타격 {Hits}→{after.Hits}";
        else if (after.Angle > Angle + 0.001f) detail = $"각도 ×{UpgradeStep:0.##}";
        else if (after.Range > Range * 1.001f)
        {
            string label = Shape == LightSkillShape.Projectile ? "사거리"
                : Shape == LightSkillShape.Line ? "길이·두께" : "범위";
            detail = $"{label} ×{UpgradeStep:0.##}";
        }
        return detail.Length == 0 ? $"강화 {currentLevel}→{nextLevel}" : $"강화 {currentLevel}→{nextLevel} ({detail})";
    }

    private bool Offers(SkillUpgradeKind kind)
    {
        if (kind == SkillUpgradeKind.None) return false;
        SkillUpgradeKind[] choices = UpgradeChoices(Shape);
        for (int i = 0; i < choices.Length; i++) if (choices[i] == kind) return true;
        return false;
    }

    private LightSkillCast Copy(float range, float angle, float width, int projectiles, int hits)
        => new LightSkillCast(Id, Name, Element, MpCost, Shape, range, angle, width,
            Mathf.Max(1, projectiles), Mathf.Max(1, hits), HitDelay, BaseValue, Factor);

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
