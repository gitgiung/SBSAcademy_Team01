using UnityEngine;

/// <summary>Scene 뷰에서만 보스 범위를 표시합니다. 인게임 표시는 BossAttackHitVfx가 담당합니다.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(BossController))]
public sealed class BossPatternRangeView : MonoBehaviour
{
    [Header("Scene 뷰 2페이즈 원형 공격")]
    [SerializeField] private bool showSceneAreaRange = true;
    public bool ShowSceneAreaRange => showSceneAreaRange;

    private const int CircleSegmentCount = 64;

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!enabled || !showSceneAreaRange || Camera.current == null ||
            Camera.current.cameraType != CameraType.SceneView) return;
        BossController controller = GetComponent<BossController>();
        if (controller == null) return;
        Color previous = Gizmos.color;
        Vector3 center = controller.transform.position;
        DrawSceneCircle(center, controller.PhaseTwoAreaRadius, new Color(1f, 0.55f, 0f));
        DrawSceneCircle(center, controller.PhaseTwoAreaDistance.x, Color.cyan);
        DrawSceneCircle(center, controller.PhaseTwoAreaDistance.y, Color.cyan);
        Gizmos.color = previous;
    }

    private static void DrawSceneCircle(Vector3 center, float radius, Color color)
    {
        Gizmos.color = color;
        Vector3 previous = center + Vector3.right * radius;
        for (int i = 1; i <= CircleSegmentCount; i++)
        {
            float angle = i * Mathf.PI * 2f / CircleSegmentCount;
            Vector3 next = center + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius;
            Gizmos.DrawLine(previous, next);
            previous = next;
        }
    }
#endif
}
