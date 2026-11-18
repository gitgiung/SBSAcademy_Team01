using UnityEditor;
using UnityEngine;

public static class RemoteContentSettingsMenu
{
    [MenuItem("Tools/Remote Content/Settings")]
    private static void SelectSettings()
    {
        const string path = "Assets/Resources/RemoteContentSettings.asset";
        RemoteContentSettings settings = AssetDatabase.LoadAssetAtPath<RemoteContentSettings>(path);
        if (settings == null)
        {
            if (AssetDatabase.LoadMainAssetAtPath(path) != null)
            {
                Debug.LogError("같은 경로의 기존 asset을 보존했습니다. RemoteContentSettings 경로를 확인하세요.");
                return;
            }
            if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
            settings = ScriptableObject.CreateInstance<RemoteContentSettings>();
            AssetDatabase.CreateAsset(settings, path);
            Undo.RegisterCreatedObjectUndo(settings, "Create Remote Content Settings");
            AssetDatabase.SaveAssetIfDirty(settings);
        }
        Selection.activeObject = settings;
        EditorGUIUtility.PingObject(settings);
    }
}
