using UnityEditor;
using UnityEngine;

/// <summary>Edits the controller's real attack settings through Scene handles.</summary>
[CustomEditor(typeof(BossPatternRangeView))]
public sealed class BossPatternRangeViewEditor : Editor
{
    private void OnSceneGUI()
    {
        var view = (BossPatternRangeView)target;
        if (!view.enabled || !view.ShowSceneAreaRange) return;
        var boss = view.GetComponent<BossController>();
        if (boss == null) return;

        var settings = new SerializedObject(boss);
        settings.Update();
        SerializedProperty radius = settings.FindProperty("phaseTwoAreaRadius");
        SerializedProperty distance = settings.FindProperty("phaseTwoAreaDistance");
        Vector3 center = boss.transform.position;
        Color oldColor = Handles.color;
        try
        {
            EditorGUI.BeginChangeCheck();
            Handles.color = new Color(1f, 0.55f, 0f);
            float newRadius = DrawRadiusHandle(center, Vector3.right, radius.floatValue, "원형 피해 반경");
            Handles.color = Color.cyan;
            Vector2 range = distance.vector2Value;
            float min = DrawRadiusHandle(center, Vector3.left, range.x, "발동 최소 거리 (초과)");
            float max = DrawRadiusHandle(center, Vector3.up, range.y, "발동 최대 거리 (이하)");
            if (EditorGUI.EndChangeCheck())
            {
                radius.floatValue = Mathf.Max(0.01f, newRadius);
                min = Mathf.Max(0f, min);
                distance.vector2Value = new Vector2(min, Mathf.Max(min, max));
                settings.ApplyModifiedProperties();
            }
        }
        finally { Handles.color = oldColor; }
    }

    private static float DrawRadiusHandle(Vector3 center, Vector3 direction, float radius, string label)
    {
        Vector3 point = center + direction * radius;
        float size = HandleUtility.GetHandleSize(point) * 0.08f;
        Handles.Label(point + Vector3.up * size * 2f, label + ": " + radius.ToString("0.##"));
        Vector3 moved = Handles.Slider(point, direction, size, Handles.DotHandleCap, 0.1f);
        return Vector3.Dot(moved - center, direction);
    }
}
