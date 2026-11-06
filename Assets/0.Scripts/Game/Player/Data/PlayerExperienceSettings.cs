using System;
using UnityEngine;

/// <summary>1차 플레이테스트용 XP 공식 계수. 런타임 성장 상태는 저장하지 않습니다.</summary>
[Serializable]
public sealed class PlayerExperienceSettings
{
    public const int MaxLevel = 100;
    [Min(0)] public int baseNumerator = 108000;
    [Min(0)] public int linearNumerator = 19800;
    [Min(0)] public int quadraticNumerator = 675;

    public int RequiredXP(int level)
    {
        if (level < 1 || level >= MaxLevel) return 0;
        long numerator = Math.Max(0, baseNumerator) + (long)Math.Max(0, linearNumerator) * level
            + (long)Math.Max(0, quadraticNumerator) * level * level;
        return (int)Math.Min(int.MaxValue, Math.Max(1, (numerator + 5000) / 10000));
    }
}
