using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class BossMvpSceneBuilder
{
    private const string ScenePath = "Assets/9.Scenes/EnemyMvpTestScene.unity";
    private const string PrefabPath = "Assets/1.Prefabs/BossMvp.prefab";

    [MenuItem("Tools/Enemy MVP/Add Boss To Test Scene")]
    public static void AddBossToTestScene()
    {
        EnsureBossInTestScene();
    }

    public static bool EnsureBossInTestScene()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return false;

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null) prefab = CreatePrefab();
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        EnemySpawner spawner = null;
        BossController controller = null;
        foreach (var root in scene.GetRootGameObjects())
        {
            if (root.TryGetComponent<EnemySpawner>(out var foundSpawner)) spawner = foundSpawner;
            if (root.TryGetComponent<BossController>(out var foundBoss)) controller = foundBoss;
        }
        if (spawner == null) throw new System.InvalidOperationException("Enemy MVP scene has no spawner.");
        var spawnerSettings = new SerializedObject(spawner);
        var target = (Transform)spawnerSettings.FindProperty("player").objectReferenceValue;
        if (target == null) throw new System.InvalidOperationException("Enemy MVP scene has no player target.");
        if (controller == null)
        {
            var boss = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            boss.transform.position = new Vector3(0, 4, 0);
            controller = boss.GetComponent<BossController>();
        }
        var settings = new SerializedObject(controller);
        settings.FindProperty("target").objectReferenceValue = target;
        settings.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("BOSS_MVP_SCENE_READY: " + ScenePath);
        return true;
    }

    private static GameObject CreatePrefab()
    {
        var normal = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/1.Prefabs/EnemyMvp.prefab");
        if (normal == null) throw new System.InvalidOperationException("EnemyMvp prefab is required.");
        var boss = new GameObject("BossMvp");
        try
        {
            var sourceVisual = normal.GetComponent<SpriteRenderer>();
            var visual = boss.AddComponent<SpriteRenderer>();
            visual.sprite = sourceVisual.sprite;
            visual.sharedMaterial = sourceVisual.sharedMaterial;
            visual.color = new Color(0.7f, 0.25f, 1f);
            boss.transform.localScale = Vector3.one * 1.2f;
            var follow = boss.AddComponent<EnemyFollow>();
            var health = boss.AddComponent<EnemyHealth>();
            var selector = boss.AddComponent<RandomBossPatternSelector>();
            var controller = boss.AddComponent<BossController>();
            var body = boss.GetComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Dynamic;
            body.gravityScale = 0;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            var hitbox = boss.AddComponent<CircleCollider2D>();
            hitbox.isTrigger = true;
            hitbox.radius = 0.5f;
            var healthSettings = new SerializedObject(health);
            healthSettings.FindProperty("maxHealth").intValue = 10;
            healthSettings.ApplyModifiedPropertiesWithoutUndo();
            var moveSettings = new SerializedObject(follow);
            moveSettings.FindProperty("moveSpeed").floatValue = 1.5f;
            moveSettings.FindProperty("stoppingDistance").floatValue = 2f;
            moveSettings.ApplyModifiedPropertiesWithoutUndo();
            var settings = new SerializedObject(controller);
            settings.FindProperty("patternSelector").objectReferenceValue = selector;
            settings.ApplyModifiedPropertiesWithoutUndo();
            return PrefabUtility.SaveAsPrefabAsset(boss, PrefabPath);
        }
        finally { Object.DestroyImmediate(boss); }
    }
}
