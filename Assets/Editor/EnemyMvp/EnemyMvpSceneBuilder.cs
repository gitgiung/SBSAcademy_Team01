using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;

public static class EnemyMvpSceneBuilder
{
    private const string ScenePath = "Assets/9.Scenes/EnemyMvpTestScene.unity";
    private const string ResourceFolder = "Assets/2.Resources/EnemyMvp";

    [MenuItem("Tools/Enemy MVP/Upgrade Enemy Body Collision")]
    public static void UpgradeEnemyBodyCollision()
    {
        const string prefabPath = "Assets/1.Prefabs/EnemyMvp.prefab";
        var root = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            ConfigureEnemyBody(root);
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
        AssetDatabase.SaveAssets();
        Debug.Log("ENEMY_MVP_COLLISION_UPGRADED");
    }

    private static void ConfigureEnemyBody(GameObject enemy)
    {
        var body = enemy.GetComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Dynamic;
        body.gravityScale = 0;
        body.constraints = RigidbodyConstraints2D.FreezeRotation;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        var solid = enemy.GetComponent<BoxCollider2D>();
        if (solid == null) solid = enemy.AddComponent<BoxCollider2D>();
        solid.isTrigger = false;
        solid.size = Vector2.one;
        solid.offset = Vector2.zero;
    }

    [MenuItem("Tools/Enemy MVP/Upgrade Test Scene With Combat")]
    public static void UpgradeTestSceneWithCombat()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        const string prefabPath = "Assets/1.Prefabs/EnemyMvp.prefab";
        var prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            if (prefabRoot.GetComponent<EnemyHealth>() == null) prefabRoot.AddComponent<EnemyHealth>();
            var hitbox = prefabRoot.GetComponent<CircleCollider2D>();
            if (hitbox == null) hitbox = prefabRoot.AddComponent<CircleCollider2D>();
            hitbox.isTrigger = true;
            hitbox.radius = 0.5f;
            ConfigureEnemyBody(prefabRoot);
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }

        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        EnemySpawner spawner = null;
        Camera camera = null;
        foreach (var root in scene.GetRootGameObjects())
        {
            if (root.TryGetComponent<EnemySpawner>(out var foundSpawner)) spawner = foundSpawner;
            if (root.TryGetComponent<Camera>(out var foundCamera)) camera = foundCamera;
        }
        if (spawner == null || camera == null)
            throw new System.InvalidOperationException("Enemy MVP scene requires its spawner and camera.");
        var spawnerSettings = new SerializedObject(spawner);
        var target = (Transform)spawnerSettings.FindProperty("player").objectReferenceValue;
        if (target == null) throw new System.InvalidOperationException("Enemy MVP spawner has no player.");
        var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/InputSystem_Actions.inputactions");
        bool wasActive = target.gameObject.activeSelf;
        target.gameObject.SetActive(false);
        target.name = "Player Target - WASD and Left Click";
        var movement = target.GetComponent<PlayerMvpMovement>();
        if (movement == null) movement = target.gameObject.AddComponent<PlayerMvpMovement>();
        var movementSettings = new SerializedObject(movement);
        movementSettings.FindProperty("inputActions").objectReferenceValue = actions;
        movementSettings.ApplyModifiedPropertiesWithoutUndo();
        var body = target.GetComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0;
        body.constraints = RigidbodyConstraints2D.FreezeRotation;
        var attack = target.GetComponent<PlayerMeleeAttack>();
        if (attack == null) attack = target.gameObject.AddComponent<PlayerMeleeAttack>();
        var attackSettings = new SerializedObject(attack);
        attackSettings.FindProperty("inputActions").objectReferenceValue = actions;
        attackSettings.FindProperty("aimCamera").objectReferenceValue = camera;
        var arcTransform = target.Find("Attack Arc");
        var arcObject = arcTransform != null ? arcTransform.gameObject : new GameObject("Attack Arc");
        arcObject.transform.SetParent(target, false);
        var arc = arcObject.GetComponent<LineRenderer>();
        if (arc == null) arc = arcObject.AddComponent<LineRenderer>();
        arc.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(ResourceFolder + "/Marker.mat");
        arc.startColor = arc.endColor = new Color(1, 0.85f, 0.15f);
        arc.startWidth = arc.endWidth = 0.06f;
        arc.sortingOrder = 2;
        arc.positionCount = 0;
        arc.enabled = false;
        attackSettings.FindProperty("attackIndicator").objectReferenceValue = arc;
        attackSettings.ApplyModifiedPropertiesWithoutUndo();
        target.gameObject.SetActive(wasActive);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("ENEMY_MVP_COMBAT_UPGRADED: " + ScenePath);
    }

    [MenuItem("Tools/Enemy MVP/Create Test Scene")]
    public static void CreateTestScene()
    {
        if (File.Exists(ScenePath))
        {
            Debug.Log("Enemy MVP test scene already exists: " + ScenePath);
            return;
        }

        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        Directory.CreateDirectory(ResourceFolder);
        Directory.CreateDirectory("Assets/1.Prefabs");
        AssetDatabase.Refresh();

        string texturePath = ResourceFolder + "/Square.png";
        var texture = new Texture2D(16, 16);
        var pixels = new Color[256];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
        texture.SetPixels(pixels);
        texture.Apply();
        File.WriteAllBytes(texturePath, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(texturePath);
        var importer = (TextureImporter)AssetImporter.GetAtPath(texturePath);
        importer.textureType = TextureImporterType.Sprite;
        importer.spritePixelsPerUnit = 16;
        importer.filterMode = FilterMode.Point;
        importer.SaveAndReimport();
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(texturePath);

        var material = new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default"));
        AssetDatabase.CreateAsset(material, ResourceFolder + "/Marker.mat");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0, 0, -10);
        var camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 6;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.06f, 0.08f, 0.12f);

        var player = Marker("Player Target - move in Scene view during Play", Vector3.zero,
            new Color(0.2f, 0.8f, 1f), sprite, material);
        player.GetComponent<SpriteRenderer>().sortingOrder = 1;
        var left = new GameObject("Spawn Point Left");
        left.transform.position = new Vector3(-6, 3, 0);
        var right = new GameObject("Spawn Point Right");
        right.transform.position = new Vector3(6, -3, 0);

        var enemy = Marker("EnemyMvp", Vector3.zero, new Color(1f, 0.3f, 0.25f), sprite, material);
        enemy.transform.localScale = Vector3.one * 0.6f;
        enemy.AddComponent<EnemyFollow>();
        ConfigureEnemyBody(enemy);
        var prefab = PrefabUtility.SaveAsPrefabAsset(enemy, "Assets/1.Prefabs/EnemyMvp.prefab");
        Object.DestroyImmediate(enemy);

        var host = new GameObject("Enemy Spawner");
        host.SetActive(false);
        var spawner = host.AddComponent<EnemySpawner>();
        var settings = new SerializedObject(spawner);
        settings.FindProperty("enemyPrefab").objectReferenceValue = prefab;
        settings.FindProperty("player").objectReferenceValue = player.transform;
        var points = settings.FindProperty("spawnPoints");
        points.arraySize = 2;
        points.GetArrayElementAtIndex(0).objectReferenceValue = left.transform;
        points.GetArrayElementAtIndex(1).objectReferenceValue = right.transform;
        settings.ApplyModifiedPropertiesWithoutUndo();
        host.SetActive(true);

        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log("ENEMY_MVP_SCENE_CREATED: " + ScenePath);
    }

    private static GameObject Marker(string name, Vector3 position, Color color, Sprite sprite, Material material)
    {
        var marker = new GameObject(name);
        marker.transform.position = position;
        var renderer = marker.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sharedMaterial = material;
        return marker;
    }
}
