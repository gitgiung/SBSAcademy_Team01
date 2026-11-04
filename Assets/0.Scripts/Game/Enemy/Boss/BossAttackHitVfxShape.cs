using UnityEngine;

/// <summary>Maps a boss hit radius and angle onto the attack-range sprite.</summary>
public static class BossAttackHitVfxShape
{
    public const float RadialStartAngle = 180f;

    public static float Diameter(float radius)
    {
        return Mathf.Max(0f, radius) * 2f;
    }

    public static float RadialClip(float angle)
    {
        float clamped = Mathf.Clamp(angle, 1f, 360f);
        return RadialStartAngle - clamped * 0.5f;
    }

    public static Vector3 LocalScale(float radius, Vector3 parentLossyScale)
    {
        float diameter = Diameter(radius);
        return new Vector3(
            ScaleAxis(diameter, parentLossyScale.x),
            ScaleAxis(diameter, parentLossyScale.y),
            ScaleAxis(1f, parentLossyScale.z));
    }

    private static float ScaleAxis(float size, float parentAxis)
    {
        return Mathf.Abs(parentAxis) > Mathf.Epsilon ? size / parentAxis : size;
    }
}
