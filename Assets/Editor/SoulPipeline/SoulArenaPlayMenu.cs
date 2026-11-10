using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>SoulArena3D를 Build Settings에 넣고 바로 연다.</summary>
public static class SoulArenaPlayMenu
{
    private const string ScenePath = "Assets/9.Scenes/SoulArena3D.unity";

    [MenuItem("Tools/Soul Pipeline/Play Setup (Build Settings + Open)")]
    public static void PlaySetup()
    {
        SoulPipelineBuilder.EnsureBuildSettingsEntry();
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Debug.Log("[SoulPipeline] SoulArena3D 열림. Play From Title 끄고 Play.");
    }
}
