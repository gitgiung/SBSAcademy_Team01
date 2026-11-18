using UnityEditor;
using UnityEngine;

internal static class BossAttackRangeGizmos
{
    [DrawGizmo(GizmoType.Selected | GizmoType.NonSelected)]
    private static void Draw(BossController boss, GizmoType gizmoType)
    {
        Vector3 center = boss.transform.position;
        Vector3 target = boss.Target != null ? boss.Target.position : center;
        Vector2 forward = target - center;
        if (forward.sqrMagnitude <= Mathf.Epsilon) forward = Vector2.right;
        Vector3 arcStart = Quaternion.AngleAxis(-boss.SlashAngle * .5f, Vector3.forward) * forward.normalized;

        using (new Handles.DrawingScope(new Color(1f, 0f, 0f, .22f)))
        {
            Handles.DrawSolidArc(center, Vector3.forward, arcStart, boss.SlashAngle, boss.SlashRange);
            Handles.DrawSolidDisc(target, Vector3.forward, boss.DashEndDamageRadius);
        }

        using (new Handles.DrawingScope(new Color(1f, 0f, 0f, .85f)))
        {
            Handles.DrawWireDisc(center, Vector3.forward, boss.CloseRange);
            Handles.DrawWireDisc(center, Vector3.forward, boss.SpecialPatternDistance);
            Handles.DrawWireDisc(target, Vector3.forward, boss.DashEndDamageRadius);
            Handles.DrawWireArc(center, Vector3.forward, arcStart, boss.SlashAngle, boss.SlashRange);
            Handles.DrawLine(center, center + arcStart * boss.SlashRange);
            Vector3 arcEnd = Quaternion.AngleAxis(boss.SlashAngle, Vector3.forward) * arcStart;
            Handles.DrawLine(center, center + arcEnd * boss.SlashRange);
        }
    }
}
