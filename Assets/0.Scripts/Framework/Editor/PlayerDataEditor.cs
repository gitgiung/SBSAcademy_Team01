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
        using (new EditorGUI.DisabledScope(true))
            EditorGUILayout.FloatField("현재 이동속도", ((PlayerData)target).MoveSpeed);
    }

    public override bool RequiresConstantRepaint() => Application.isPlaying;
}
