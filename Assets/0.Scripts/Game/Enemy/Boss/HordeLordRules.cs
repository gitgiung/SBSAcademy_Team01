using UnityEngine;

/// <summary>
/// 소환형 무리군주의 거리와 페이즈 판단. 마리 수·거리는 패턴을 보기 위한 임시 값이며 기획 확정 수치가 아니다.
/// 구체 피해는 현재 발톱 보스 시험 피해 10과 같다.
/// </summary>
public static class HordeLordRules
{
    public const float PhaseTwoHealthFraction = 0.5f;
    public const int PhaseOneSummonCount = 3;
    public const int PhaseTwoSummonCount = 5;
    public const int PhaseOneOrbCount = 1;
    public const int PhaseTwoOrbCount = 3;
    public const int PhaseOneAliveCap = 6;
    public const int PhaseTwoAliveCap = 10;
    public const float OrbSpreadDegrees = 36f;
    public const float SummonArcDegrees = 100f;

    public enum Stance
    {
        Hold,
        Advance,
        Retreat
    }

    public static bool IsPhaseTwo(float currentHealth, float maxHealth)
    {
        if (maxHealth <= 0f || float.IsNaN(maxHealth) || float.IsNaN(currentHealth))
            return false;
        return currentHealth / maxHealth <= PhaseTwoHealthFraction;
    }

    public static int SummonCount(bool phaseTwo)
    {
        return phaseTwo ? PhaseTwoSummonCount : PhaseOneSummonCount;
    }

    public static int OrbCount(bool phaseTwo)
    {
        return phaseTwo ? PhaseTwoOrbCount : PhaseOneOrbCount;
    }

    public static int AliveCap(bool phaseTwo)
    {
        return phaseTwo ? PhaseTwoAliveCap : PhaseOneAliveCap;
    }

    public static Stance ChooseStance(float distance, float innerDistance, float outerDistance)
    {
        if (distance < innerDistance)
            return Stance.Retreat;
        if (distance > outerDistance)
            return Stance.Advance;
        return Stance.Hold;
    }

    public static Vector2 SummonOffset(int index, int count, Vector2 forward, float radius)
    {
        Vector2 direction = forward.sqrMagnitude <= Mathf.Epsilon ? Vector2.down : forward.normalized;
        if (count <= 1)
            return direction * radius;

        float start = -SummonArcDegrees * 0.5f;
        float step = SummonArcDegrees / (count - 1);
        return Rotate(direction, start + step * index) * radius;
    }

    public static Vector2 OrbDirection(int index, int count, Vector2 forward)
    {
        Vector2 direction = forward.sqrMagnitude <= Mathf.Epsilon ? Vector2.down : forward.normalized;
        if (count <= 1)
            return direction;

        float start = -OrbSpreadDegrees * 0.5f;
        float step = OrbSpreadDegrees / (count - 1);
        return Rotate(direction, start + step * index);
    }

    private static Vector2 Rotate(Vector2 direction, float degrees)
    {
        float radians = degrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(radians);
        float sin = Mathf.Sin(radians);
        return new Vector2(direction.x * cos - direction.y * sin, direction.x * sin + direction.y * cos);
    }
}
