using UnityEngine;

/// <summary>
/// 표의 전략 스킬 EffectType을 PlayerData에 적용합니다.
/// 쿨타임·횟수·버튼 입력은 소유하지 않습니다.
/// </summary>
public static class TacticalSkillEffect
{
    public static bool IsSupported(string effectType)
    {
        if (string.IsNullOrEmpty(effectType)) return false;
        return string.Equals(effectType, "Heal", System.StringComparison.OrdinalIgnoreCase)
            || string.Equals(effectType, "HealOverTime", System.StringComparison.OrdinalIgnoreCase)
            || string.Equals(effectType, "Recover", System.StringComparison.OrdinalIgnoreCase)
            || string.Equals(effectType, "Shield", System.StringComparison.OrdinalIgnoreCase)
            || string.Equals(effectType, "Invulnerable", System.StringComparison.OrdinalIgnoreCase);
    }

    public static bool TryApply(PlayerData data, CombatSkillRecord record)
    {
        if (data == null || record == null || data.IsDead) return false;
        string type = record.EffectType ?? "";
        if (string.Equals(type, "Heal", System.StringComparison.OrdinalIgnoreCase))
            return ApplyHeal(data, record);
        if (string.Equals(type, "HealOverTime", System.StringComparison.OrdinalIgnoreCase))
            return ApplyHealOverTime(data, record);
        if (string.Equals(type, "Recover", System.StringComparison.OrdinalIgnoreCase))
            return ApplyRecover(data, record);
        if (string.Equals(type, "Shield", System.StringComparison.OrdinalIgnoreCase))
            return ApplyShield(data, record);
        if (string.Equals(type, "Invulnerable", System.StringComparison.OrdinalIgnoreCase))
            return ApplyInvulnerable(data, record);
        return false;
    }

    private static bool ApplyHeal(PlayerData data, CombatSkillRecord record)
    {
        float amount = ScaledAmount(data, record);
        if (!(amount > 0f) || float.IsNaN(amount)) return false;
        data.RestoreHealth(amount);
        return true;
    }

    private static bool ApplyHealOverTime(PlayerData data, CombatSkillRecord record)
    {
        float perTick = ScaledAmount(data, record);
        int hits = Mathf.Max(1, record.Hits);
        float delay = record.HitDelay > 0f ? record.HitDelay : 1f;
        if (!(perTick > 0f) || float.IsNaN(perTick) || float.IsNaN(delay)) return false;
        data.BeginHealOverTime(perTick, delay, hits);
        return true;
    }

    private static bool ApplyRecover(PlayerData data, CombatSkillRecord record)
    {
        float amount = data.maxMP * Mathf.Max(0f, record.Factor);
        if (!(amount > 0f) || float.IsNaN(amount)) return false;
        data.RestoreMP(amount);
        return true;
    }

    private static bool ApplyShield(PlayerData data, CombatSkillRecord record)
    {
        float amount = data.maxHealth * Mathf.Max(0f, record.Factor);
        float duration = record.EffectDuration;
        if (!(amount > 0f) || !(duration > 0f) || float.IsNaN(amount) || float.IsNaN(duration)) return false;
        data.GrantShield(amount, duration);
        return true;
    }

    private static bool ApplyInvulnerable(PlayerData data, CombatSkillRecord record)
    {
        float duration = record.EffectDuration;
        if (!(duration > 0f) || float.IsNaN(duration)) return false;
        data.GrantInvulnerable(duration);
        return true;
    }

    private static float ScaledAmount(PlayerData data, CombatSkillRecord record)
    {
        float factor = Mathf.Max(0f, record.Factor);
        if (string.Equals(record.ScalingStat, "MP", System.StringComparison.OrdinalIgnoreCase))
            return data.maxMP * factor;
        return data.maxHealth * factor;
    }
}
