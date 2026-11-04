using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>기존 Animator GUID/상태를 보존하며 약공격 테스트 리소스만 추가합니다.</summary>
public static class PlayerLightAttackSetup
{
    private const string ControllerPath = "Assets/Resources/PlayerAnimator/Player.controller";
    private const string ProjectileFolder = "Assets/Resources/SkillProjectiles";
    private static readonly string[] Directions = { "east", "north-east", "north", "north-west", "west", "south-west", "south", "south-east" };
    [MenuItem("Tools/Player Skill/Build Flame Bullet Resources")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play before editing skill assets.");
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null) throw new InvalidOperationException("Existing Player Animator Controller is required.");
        EnsureController(controller);
        BuildProjectile();
        AssetDatabase.SaveAssets();
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ProjectileFolder + "/FlameBullet.prefab");
        AssetDatabase.TryGetGUIDAndLocalFileIdentifier(prefab, out string guid, out long fileId);
        File.WriteAllText(Path.Combine(Application.dataPath, "../FlameBuild.json"),
            "{\"guid\":\"" + guid + "\",\"fileId\":" + fileId + "}");
        Debug.Log("Flame Bullet resources ready; existing controller and Bullet prefab GUIDs are preserved.");
    }

    public static void EnsureController(AnimatorController controller)
    {
        var machine = controller.layers[0].stateMachine;
        var existing = machine.states.FirstOrDefault(s => s.state.name == "LightAttack").state;
        if (existing != null)
        {
            if (!existing.behaviours.OfType<LightAttackAnimationState>().Any())
                throw new InvalidOperationException("An unrelated LightAttack state already exists; it was not overwritten.");
            foreach (string direction in Directions) BuildClip(direction);
            return;
        }
        var idle = machine.states.FirstOrDefault(s => s.state.name == "Idle").state;
        if (idle == null) throw new InvalidOperationException("Existing Idle state is required.");
        var tree = new BlendTree { name = "Light Attack Directions", blendType = BlendTreeType.FreeformDirectional2D,
            blendParameter = "FacingX", blendParameterY = "FacingY", useAutomaticThresholds = false };
        AssetDatabase.AddObjectToAsset(tree, controller);
        for (int i = 0; i < Directions.Length; i++)
        {
            var clip = BuildClip(Directions[i]);
            float angle = i * Mathf.PI / 4;
            tree.AddChild(clip, new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)));
        }
        var state = machine.AddState("LightAttack", new Vector3(750, 420));
        state.motion = tree;
        state.AddStateMachineBehaviour<LightAttackAnimationState>();
        var finish = state.AddTransition(idle);
        finish.hasExitTime = true; finish.exitTime = 1; finish.duration = 0;
        EditorUtility.SetDirty(controller);
    }

    private static AnimationClip BuildClip(string direction)
    {
        string title = string.Concat(direction.Split('-').Select(part => char.ToUpperInvariant(part[0]) + part.Substring(1)));
        string path = "Assets/Resources/PlayerAnimator/LightAttack_" + title + ".anim";
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip == null) { clip = new AnimationClip(); AssetDatabase.CreateAsset(clip, path); }
        clip.name = "LightAttack_" + title;
        clip.frameRate = 12;
        clip.wrapMode = WrapMode.Once;
        string folder = "Assets/2.Resources/Art/Pretty_Magical_Girl/Animations/Attack";
        string[] frames = Directory.GetFiles(folder, direction + "_*.png").OrderBy(p => p, StringComparer.Ordinal).ToArray();
        if (frames.Length == 0) throw new InvalidOperationException("Attack frames missing: " + direction);
        var keys = new ObjectReferenceKeyframe[frames.Length];
        for (int i = 0; i < frames.Length; i++)
        {
            var sprite = AssetDatabase.LoadAllAssetsAtPath(frames[i].Replace('\\', '/')).OfType<Sprite>().FirstOrDefault();
            if (sprite == null) throw new InvalidOperationException("Sprite import required: " + frames[i]);
            keys[i] = new ObjectReferenceKeyframe { time = i / clip.frameRate, value = sprite };
        }
        AnimationUtility.SetObjectReferenceCurve(clip, new EditorCurveBinding { path = "", type = typeof(SpriteRenderer), propertyName = "m_Sprite" }, keys);
        var settings = AnimationUtility.GetAnimationClipSettings(clip); settings.loopTime = false;
        settings.startTime = 0; settings.stopTime = frames.Length / clip.frameRate;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        EditorUtility.SetDirty(clip);
        return clip;
    }

    private static void BuildProjectile()
    {
        if (!AssetDatabase.IsValidFolder(ProjectileFolder)) AssetDatabase.CreateFolder("Assets/Resources", "SkillProjectiles");
        string imagePath = ProjectileFolder + "/FlameBulletCircle.png";
        if (!File.Exists(imagePath))
        {
            var texture = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            for (int y = 0; y < 32; y++) for (int x = 0; x < 32; x++)
            {
                float dx = x - 15.5f, dy = y - 15.5f;
                texture.SetPixel(x, y, new Color(1, 1, 1, dx * dx + dy * dy <= 15.5f * 15.5f ? 1 : 0));
            }
            texture.Apply(); File.WriteAllBytes(imagePath, texture.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(imagePath);
            var importer = (TextureImporter)AssetImporter.GetAtPath(imagePath);
            importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 32; importer.filterMode = FilterMode.Point; importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed; importer.SaveAndReimport();
        }
        string prefabPath = ProjectileFolder + "/FlameBullet.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null) return;
        var root = PrefabUtility.LoadPrefabContents("Assets/1.Prefabs/Bullet.prefab");
        try
        {
            root.name = "FlameBullet"; root.transform.position = Vector3.zero;
            if (root.GetComponent<MeshRenderer>() != null) UnityEngine.Object.DestroyImmediate(root.GetComponent<MeshRenderer>());
            if (root.GetComponent<MeshFilter>() != null) UnityEngine.Object.DestroyImmediate(root.GetComponent<MeshFilter>());
            var renderer = root.AddComponent<SpriteRenderer>(); renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(imagePath);
            renderer.color = Color.red; renderer.sortingOrder = 10;
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }
}
