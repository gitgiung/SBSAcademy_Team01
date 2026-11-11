using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 본선 Soul Pipeline 조립: Humanoid FBX 설정, Animator, Prefab, SoulArena3D 씬.
/// MainScene은 덮어쓰지 않는다. 합류는 Handoff의 단계적 경로를 따른다.
/// </summary>
public static class SoulPipelineBuilder
{
    private const string HumanoidFolder = "Assets/2.Resources/Art/SoulPipeline/Humanoid";
    private const string GeneratedFolder = "Assets/2.Resources/Art/SoulPipeline/Generated";
    private const string PrefabFolder = "Assets/1.Prefabs/SoulPipeline";
    private const string ScenePath = "Assets/9.Scenes/SoulArena3D.unity";
    private const string MannequinPath = HumanoidFolder + "/KinemIQ_Mannequin.fbx";

    [MenuItem("Tools/Soul Pipeline/0. One-Click Setup (1+2+3)")]
    public static void OneClickSetup()
    {
        ConfigureHumanoidFbx();
        BuildAnimatorsAndPrefabs();
        BuildArenaScene(askToSave: true);
        EnsureBuildSettingsEntry();
        Debug.Log("[SoulPipeline] One-Click Setup 완료. Play From Title 끄고 Play.");
    }

    /// <summary>배치/검증용. 저장 확인 창 없이 Prefab·씬을 조립한다.</summary>
    public static void OneClickSetupBatch()
    {
        ConfigureHumanoidFbx();
        BuildAnimatorsAndPrefabs();
        BuildArenaScene(askToSave: false);
        EnsureBuildSettingsEntry();
        string handPath = DescribeBossHandBinding();
        Debug.Log("[SoulPipeline] Batch One-Click 완료. " + handPath);
        if (Application.isBatchMode)
            EditorApplication.Exit(string.IsNullOrEmpty(handPath) ? 1 : 0);
    }

    [MenuItem("Tools/Soul Pipeline/1. Configure Humanoid FBX")]
    public static void ConfigureHumanoidFbx()
    {
        EnsureFolders();
        if (!File.Exists(MannequinPath))
        {
            EditorUtility.DisplayDialog("Soul Pipeline", "KinemIQ_Mannequin.fbx가 없다.\n" + HumanoidFolder, "OK");
            return;
        }

        ConfigureModel(MannequinPath, createAvatar: true, sourceAvatar: null);
        Avatar avatar = LoadSubAsset<Avatar>(MannequinPath);

        string[] clips =
        {
            "Idle_relaxed.fbx",
            "Walk_forwards.fbx",
            "Run_jog_forwards.fbx",
            "Sword_slash.fbx",
            "Strike_zombie_scratch.fbx",
            "Idle_with_a_sword.fbx"
        };

        foreach (string clip in clips)
        {
            string path = $"{HumanoidFolder}/{clip}";
            if (!File.Exists(path)) continue;
            ConfigureModel(path, createAvatar: false, sourceAvatar: avatar);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[SoulPipeline] Humanoid FBX Rig 설정 완료.");
    }

    [MenuItem("Tools/Soul Pipeline/2. Build Animators And Prefabs")]
    public static void BuildAnimatorsAndPrefabs()
    {
        EnsureFolders();
        ConfigureHumanoidFbx();

        AnimationClip idle = LoadFirstClip($"{HumanoidFolder}/Idle_relaxed.fbx");
        AnimationClip walk = LoadFirstClip($"{HumanoidFolder}/Walk_forwards.fbx");
        AnimationClip slash = LoadFirstClip($"{HumanoidFolder}/Sword_slash.fbx");
        AnimationClip scratch = LoadFirstClip($"{HumanoidFolder}/Strike_zombie_scratch.fbx");

        if (idle == null || walk == null || slash == null)
        {
            EditorUtility.DisplayDialog("Soul Pipeline", "클립 로드 실패. 1번 Configure 후 Unity 임포트가 끝났는지 확인.", "OK");
            return;
        }

        var playerController = BuildController($"{GeneratedFolder}/SoulPlayer.controller", idle, walk, slash: null, scratch: null);
        var bossController = BuildController($"{GeneratedFolder}/SoulBoss.controller", idle, walk, slash, scratch);

        GameObject playerPrefab = BuildHumanoidPrefab("SoulPlayerHumanoid", playerController, isBoss: false, Color.cyan);
        GameObject bossPrefab = BuildHumanoidPrefab("SoulBossHumanoid", bossController, isBoss: true, new Color(0.85f, 0.25f, 0.2f));

        PrefabUtility.SaveAsPrefabAsset(playerPrefab, $"{PrefabFolder}/SoulPlayerHumanoid.prefab");
        PrefabUtility.SaveAsPrefabAsset(bossPrefab, $"{PrefabFolder}/SoulBossHumanoid.prefab");

        // Resources 경로에도 복사해 Bootstrap이 런타임 로드할 수 있게 한다.
        if (!AssetDatabase.IsValidFolder("Assets/Resources/SoulPipeline"))
        {
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                AssetDatabase.CreateFolder("Assets", "Resources");
            AssetDatabase.CreateFolder("Assets/Resources", "SoulPipeline");
        }
        PrefabUtility.SaveAsPrefabAsset(playerPrefab, "Assets/Resources/SoulPipeline/SoulPlayerHumanoid.prefab");
        PrefabUtility.SaveAsPrefabAsset(bossPrefab, "Assets/Resources/SoulPipeline/SoulBossHumanoid.prefab");

        Object.DestroyImmediate(playerPrefab);
        Object.DestroyImmediate(bossPrefab);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[SoulPipeline] Animator/Prefab 생성 완료.");
    }

    [MenuItem("Tools/Soul Pipeline/3. Build Arena Scene (SoulArena3D)")]
    public static void BuildArenaSceneMenu() => BuildArenaScene(askToSave: true);

    public static void BuildArenaScene(bool askToSave)
    {
        EnsureFolders();
        if (!File.Exists($"{PrefabFolder}/SoulPlayerHumanoid.prefab") ||
            !File.Exists($"{PrefabFolder}/SoulBossHumanoid.prefab"))
            BuildAnimatorsAndPrefabs();

        if (askToSave && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var lightGo = new GameObject("Directional Light");
        var light = lightGo.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.1f;
        lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "FlatGround3D";
        ground.transform.position = Vector3.zero;
        ground.transform.localScale = SoulRenderSpec.FlatGroundScale / 10f; // Plane default 10x10
        var groundRenderer = ground.GetComponent<MeshRenderer>();
        if (groundRenderer != null)
        {
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Sprites/Default"));
            mat.color = new Color(0.23f, 0.25f, 0.28f, 1f);
            groundRenderer.sharedMaterial = mat;
            AssetDatabase.CreateAsset(mat, $"{GeneratedFolder}/FlatGround.mat");
        }

        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        camGo.AddComponent<AudioListener>();
        var rig = camGo.AddComponent<SoulCameraRig>();
        if (camGo.GetComponent<SoulCameraShake>() == null)
            camGo.AddComponent<SoulCameraShake>();

        var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabFolder}/SoulPlayerHumanoid.prefab");
        var bossPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabFolder}/SoulBossHumanoid.prefab");
        var player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
        player.transform.position = new Vector3(-3f, 0f, 0f);
        var boss = (GameObject)PrefabUtility.InstantiatePrefab(bossPrefab);
        boss.transform.position = new Vector3(4.5f, 0f, 0f);

        var bootstrap = new GameObject("SoulArenaBootstrap");
        bootstrap.AddComponent<SoulArenaBootstrap>();

        rig.Bind(player.transform, cam);
        var bossAi = boss.GetComponent<SoulBossApproachSlash>();
        var hitbox = boss.GetComponentInChildren<SoulWeaponHitbox3D>(true);
        bossAi?.Bind(player.transform, hitbox);

        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.Refresh();
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Debug.Log($"[SoulPipeline] 씬 저장: {ScenePath}. Play로 확인. MainScene 미변경.");
    }

    [MenuItem("Tools/Soul Pipeline/Open SoulArena3D")]
    public static void OpenArena()
    {
        EnsureSceneAssetExists();
        EnsureBuildSettingsEntry();
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
    }

    [MenuItem("Tools/Soul Pipeline/Ensure Build Settings Entry")]
    public static void EnsureBuildSettingsEntryMenu() => EnsureBuildSettingsEntry();

    private static void EnsureSceneAssetExists()
    {
        if (File.Exists(ScenePath)) return;
        // Prefab이 없어도 Bootstrap+폴백으로 Play 가능한 최소 씬을 만든다.
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var bootstrap = new GameObject("SoulArenaBootstrap");
        bootstrap.AddComponent<SoulArenaBootstrap>();
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.Refresh();
    }

    public static void EnsureBuildSettingsEntry()
    {
        EnsureSceneAssetExists();
        var scenes = EditorBuildSettings.scenes.ToList();
        if (scenes.Any(s => s.path == ScenePath)) return;
        scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
        Debug.Log($"[SoulPipeline] Build Settings에 추가: {ScenePath}");
    }

    private static void EnsureFolders()
    {
        Directory.CreateDirectory(HumanoidFolder.Replace("Assets/", Application.dataPath + "/"));
        Directory.CreateDirectory(GeneratedFolder.Replace("Assets/", Application.dataPath + "/"));
        Directory.CreateDirectory(PrefabFolder.Replace("Assets/", Application.dataPath + "/"));
        if (!AssetDatabase.IsValidFolder("Assets/2.Resources/Art/SoulPipeline"))
            AssetDatabase.CreateFolder("Assets/2.Resources/Art", "SoulPipeline");
        if (!AssetDatabase.IsValidFolder(HumanoidFolder))
            AssetDatabase.CreateFolder("Assets/2.Resources/Art/SoulPipeline", "Humanoid");
        if (!AssetDatabase.IsValidFolder(GeneratedFolder))
            AssetDatabase.CreateFolder("Assets/2.Resources/Art/SoulPipeline", "Generated");
        if (!AssetDatabase.IsValidFolder("Assets/1.Prefabs/SoulPipeline"))
            AssetDatabase.CreateFolder("Assets/1.Prefabs", "SoulPipeline");
    }

    private static void ConfigureModel(string path, bool createAvatar, Avatar sourceAvatar)
    {
        var importer = AssetImporter.GetAtPath(path) as ModelImporter;
        if (importer == null) return;
        importer.animationType = ModelImporterAnimationType.Human;
        importer.avatarSetup = createAvatar
            ? ModelImporterAvatarSetup.CreateFromThisModel
            : ModelImporterAvatarSetup.CopyFromOther;
        if (!createAvatar && sourceAvatar != null)
            importer.sourceAvatar = sourceAvatar;
        importer.SaveAndReimport();
    }

    private static T LoadSubAsset<T>(string path) where T : Object
    {
        return AssetDatabase.LoadAllAssetsAtPath(path).OfType<T>().FirstOrDefault();
    }

    private static AnimationClip LoadFirstClip(string path)
    {
        return AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>()
            .FirstOrDefault(c => !c.name.StartsWith("__preview__"));
    }

    private static AnimatorController BuildController(
        string path,
        AnimationClip idle,
        AnimationClip walk,
        AnimationClip slash,
        AnimationClip scratch)
    {
        if (File.Exists(path)) AssetDatabase.DeleteAsset(path);
        var controller = AnimatorController.CreateAnimatorControllerAtPath(path);
        AnimatorStateMachine sm = controller.layers[0].stateMachine;

        AnimatorState idleState = sm.AddState("Idle");
        idleState.motion = idle;
        sm.defaultState = idleState;

        AnimatorState walkState = sm.AddState("Walk");
        walkState.motion = walk;

        if (slash != null)
        {
            AnimatorState slashState = sm.AddState("Slash");
            slashState.motion = slash;
        }

        if (scratch != null)
        {
            AnimatorState scratchState = sm.AddState("Scratch");
            scratchState.motion = scratch;
        }

        EditorUtility.SetDirty(controller);
        return controller;
    }

    private static GameObject BuildHumanoidPrefab(string name, RuntimeAnimatorController controller, bool isBoss, Color tint)
    {
        var mannequin = AssetDatabase.LoadAssetAtPath<GameObject>(MannequinPath);
        var root = Object.Instantiate(mannequin);
        root.name = name;
        root.transform.position = Vector3.zero;
        root.transform.localScale = Vector3.one * SoulRenderSpec.CharacterWorldScale;

        var animator = root.GetComponentInChildren<Animator>();
        if (animator == null) animator = root.AddComponent<Animator>();
        animator.runtimeAnimatorController = controller;
        animator.applyRootMotion = false;

        foreach (var renderer in root.GetComponentsInChildren<Renderer>())
        {
            foreach (var mat in renderer.sharedMaterials)
            {
                if (mat == null) continue;
                // 인스턴스 틴트는 Prefab 저장 시 공유 머티리얼을 더럽히지 않도록 복제
            }
        }

        // 단순 틴트용 자식 마커(시인성)
        var marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        marker.name = "TeamTintMarker";
        marker.transform.SetParent(root.transform, false);
        marker.transform.localPosition = new Vector3(0f, 2.05f, 0f);
        marker.transform.localScale = Vector3.one * 0.18f;
        Object.DestroyImmediate(marker.GetComponent<Collider>());
        var markerRenderer = marker.GetComponent<MeshRenderer>();
        var markerMat = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default"));
        markerMat.color = tint;
        markerRenderer.sharedMaterial = markerMat;

        if (root.GetComponent<CharacterController>() == null)
        {
            var cc = root.AddComponent<CharacterController>();
            cc.height = isBoss ? 2f : 1.8f;
            cc.radius = 0.35f;
            cc.center = new Vector3(0f, isBoss ? 1f : 0.9f, 0f);
        }

        if (root.GetComponent<SoulGroundGravity>() == null)
            root.AddComponent<SoulGroundGravity>();
        if (root.GetComponent<SoulArenaBounds>() == null)
            root.AddComponent<SoulArenaBounds>();

        if (isBoss)
        {
            if (root.GetComponent<SoulArenaDummyHealth>() == null)
                root.AddComponent<SoulArenaDummyHealth>();
            if (root.GetComponent<SoulBossApproachSlash>() == null)
                root.AddComponent<SoulBossApproachSlash>();
            if (root.GetComponent<SoulBossWindupFlash>() == null)
                root.AddComponent<SoulBossWindupFlash>();

            Transform hand = FindHandBone(animator);
            if (hand == null)
            {
                var handGo = new GameObject("RightHandFallback");
                handGo.transform.SetParent(root.transform, false);
                handGo.transform.localPosition = new Vector3(0.35f, 1.2f, 0.45f);
                hand = handGo.transform;
            }

            var hitbox = hand.gameObject.GetComponent<SoulWeaponHitbox3D>();
            if (hitbox == null) hitbox = hand.gameObject.AddComponent<SoulWeaponHitbox3D>();
            var sphere = hand.GetComponent<SphereCollider>();
            if (sphere == null) sphere = hand.gameObject.AddComponent<SphereCollider>();
            sphere.isTrigger = true;
            sphere.radius = 0.28f;
            sphere.enabled = false;
            hitbox.Configure(18f, root);
            if (hand.gameObject.GetComponent<SoulWeaponArmedPulse>() == null)
                hand.gameObject.AddComponent<SoulWeaponArmedPulse>();
        }
        else
        {
            if (root.GetComponent<SoulPipelineAvatar>() == null)
                root.AddComponent<SoulPipelineAvatar>();
        }

        return root;
    }

    private static Transform FindHandBone(Animator animator)
    {
        if (animator == null) return null;
        Transform hand = animator.GetBoneTransform(HumanBodyBones.RightHand);
        if (hand != null) return hand;
        hand = animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
        if (hand != null) return hand;

        // Avatar 매핑 전이거나 본 이름이 다른 FBX용 이름 탐색.
        string[] names =
        {
            "RightHand", "Hand_R", "hand_r", "mixamorig:RightHand",
            "Right_Hand", "R_Hand", "Wrist_R", "RightLowerArm", "mixamorig:RightForeArm"
        };
        Transform[] all = animator.GetComponentsInChildren<Transform>(true);
        for (int n = 0; n < names.Length; n++)
        {
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] != null && all[i].name == names[n])
                    return all[i];
            }
        }

        for (int i = 0; i < all.Length; i++)
        {
            Transform t = all[i];
            if (t == null) continue;
            string lower = t.name.ToLowerInvariant();
            if (lower.Contains("righthand") || lower.Contains("hand_r") || lower.Contains("right_hand"))
                return t;
        }

        return null;
    }

    private static string DescribeBossHandBinding()
    {
        string prefabPath = $"{PrefabFolder}/SoulBossHumanoid.prefab";
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null) return null;
        var hitbox = prefab.GetComponentInChildren<SoulWeaponHitbox3D>(true);
        if (hitbox == null) return null;
        bool fallback = hitbox.transform.name.Contains("Fallback");
        return $"손히트={hitbox.transform.name} fallback={fallback}";
    }
}
