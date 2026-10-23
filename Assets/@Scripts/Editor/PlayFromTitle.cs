using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class PlayFromTitle
{
    const string MenuPath = "Tools/Play From Title";
    const string PrefKey = "RSG.PlayFromTitle";
    const string TitleScenePath = "Assets/@Scenes/TitleScene.unity";

    static PlayFromTitle()
    {
        EditorApplication.delayCall += Apply;
    }

    [MenuItem(MenuPath)]
    static void Toggle()
    {
        EditorPrefs.SetBool(PrefKey, !EditorPrefs.GetBool(PrefKey, false));
        Apply();
    }

    [MenuItem(MenuPath, true)]
    static bool ToggleValidate()
    {
        Menu.SetChecked(MenuPath, EditorPrefs.GetBool(PrefKey, false));
        return true;
    }

    static void Apply()
    {
        if (!EditorPrefs.GetBool(PrefKey, false))
        {
            EditorSceneManager.playModeStartScene = null;
            return;
        }

        SceneAsset title = AssetDatabase.LoadAssetAtPath<SceneAsset>(TitleScenePath);
        if (title == null)
        {
            Debug.LogError($"Title scene not found: {TitleScenePath}");
            EditorSceneManager.playModeStartScene = null;
            return;
        }

        EditorSceneManager.playModeStartScene = title;
    }
}