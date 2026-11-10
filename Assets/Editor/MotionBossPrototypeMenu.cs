using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>모션 보스 샌드박스 씬을 만들거나 연다. MainScene은 건드리지 않는다.</summary>
public static class MotionBossPrototypeMenu
{
    private const string ScenePath = "Assets/9.Scenes/MotionBossPrototype.unity";

    [MenuItem("Tools/Motion Boss Prototype/Open Sandbox Scene")]
    public static void OpenSandboxScene()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        var existing = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
        if (existing == null)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("MotionBossPrototype");
            root.AddComponent<MotionBossPrototypeBootstrap>();
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.Refresh();
            Debug.Log($"[MotionBossPrototype] 샌드박스 씬 생성: {ScenePath}");
        }

        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        EnsureBootstrapPresent();
    }

    private static void EnsureBootstrapPresent()
    {
        var bootstrap = Object.FindFirstObjectByType<MotionBossPrototypeBootstrap>();
        if (bootstrap != null) return;

        var root = new GameObject("MotionBossPrototype");
        root.AddComponent<MotionBossPrototypeBootstrap>();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("[MotionBossPrototype] Bootstrap을 씬에 추가했다. 저장 후 Play.");
    }
}
