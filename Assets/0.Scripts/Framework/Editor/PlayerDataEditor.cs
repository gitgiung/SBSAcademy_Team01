using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PlayerData))]
[CanEditMultipleObjects]
internal sealed class PlayerDataEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        if (!Application.isPlaying || serializedObject.isEditingMultipleObjects) return;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("플레이 중 스탯", EditorStyles.boldLabel);
        var data = (PlayerData)target;
        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.FloatField("현재 이동속도", data.MoveSpeed);
            EditorGUILayout.FloatField("현재 적용 방어력", data.Defense);
        }
    }

    public override bool RequiresConstantRepaint() => Application.isPlaying;
}
