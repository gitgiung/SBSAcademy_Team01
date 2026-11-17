using UnityEditor;
using UnityEngine;

/// <summary>Edits the controller's real attack settings through Scene handles.</summary>
[CustomEditor(typeof(BossPatternRangeView))]
public sealed class BossPatternRangeViewEditor : Editor
{
    private void OnSceneGUI()
    {
        var view = (BossPatternRangeView)target;
        if (view == null || !view.enabled || !view.ShowSceneAreaRange) return;
        var boss = view.GetComponent<BossController>();
        if (boss == null) return;

        var settings = new SerializedObject(boss);
        settings.Update();
        SerializedProperty radius = settings.FindProperty("phaseTwoAreaRadius");
        if (radius == null) return;

        using (new Handles.DrawingScope(new Color(1f, 0.55f, 0f)))
        using (var change = new EditorGUI.ChangeCheckScope())
        {
            Vector3 center = boss.transform.position;
            Vector3 point = center + Vector3.right * radius.floatValue;
            float size = HandleUtility.GetHandleSize(point) * 0.08f;
            Handles.Label(point + Vector3.up * size * 2f, $"원형 피해 반경: {radius.floatValue:0.##}");
            Vector3 moved = Handles.Slider(point, Vector3.right, size, Handles.DotHandleCap, 0.1f);
            if (change.changed)
            {
                radius.floatValue = Mathf.Max(0.01f, moved.x - center.x);
                settings.ApplyModifiedProperties();
            }
        }
    }
}
