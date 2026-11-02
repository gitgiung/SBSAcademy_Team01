using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>Builds the player's directional animation assets from the supplied sprite frames.</summary>
public static class PlayerAnimationSetup
{
    private const string ArtRoot = "Assets/2.Resources/Art/Pretty_Magical_Girl/Animations";
    private const string OutputRoot = "Assets/Resources/PlayerAnimator";
    private static readonly string[] Directions =
    {
        "east", "north-east", "north", "north-west", "west", "south-west", "south", "south-east"
    };

    static PlayerAnimationSetup()
    {
        EditorApplication.delayCall += EnsureControllerHasParry;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    [MenuItem("Tools/Player Animation/Generate Controller")]
    public static void GenerateController()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("Stop Play mode before regenerating the Player Animator Controller.");
            return;
        }

        EnsureFolder("Assets/Resources");
        EnsureFolder(OutputRoot);
        var idleClips = new List<AnimationClip>();
        var moveClips = new List<AnimationClip>();
        var chargeClips = new List<AnimationClip>();
        var parryClips = new List<AnimationClip>();
        var guardClips = new List<AnimationClip>();
        var positions = new Vector2[Directions.Length];

        for (int i = 0; i < Directions.Length; i++)
        {
            string direction = Directions[i];
            float angle = i * 45f * Mathf.Deg2Rad;
            positions[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            idleClips.Add(CreateClip("Idle_" + ToPascal(direction), "Idle", direction, true, 6f));
            moveClips.Add(CreateClip("Move_" + ToPascal(direction), "Dash", direction, true, 12f));
            chargeClips.Add(CreateClip("Charge_" + ToPascal(direction), "Charge", direction, false, 12f));
            parryClips.Add(CreateClip("Parry_" + ToPascal(direction), "Parry", direction, false, 24f));
            guardClips.Add(CreateClip("Guard_" + ToPascal(direction), "Guard", direction, false, 12f));
        }

        string controllerPath = OutputRoot + "/Player.controller";
        AssetDatabase.DeleteAsset(controllerPath);
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        controller.AddParameter("FacingX", AnimatorControllerParameterType.Float);
        controller.AddParameter("FacingY", AnimatorControllerParameterType.Float);
        controller.AddParameter("IsMoving", AnimatorControllerParameterType.Bool);
        controller.AddParameter("IsDashing", AnimatorControllerParameterType.Bool);
        controller.AddParameter("ChargeAnimationSpeed", AnimatorControllerParameterType.Float);
        controller.AddParameter("Parry", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("IsGuarding", AnimatorControllerParameterType.Bool);

        BlendTree idleTree = CreateDirectionalTree("Player Idle Directions", idleClips, positions);
        BlendTree moveTree = CreateDirectionalTree("Player Move Directions", moveClips, positions);
        AnimatorStateMachine machine = controller.layers[0].stateMachine;
        AnimatorState idle = machine.AddState("Idle", new Vector3(250, 80));
        idle.motion = idleTree;
        AnimatorState move = machine.AddState("Move", new Vector3(500, 80));
        move.motion = moveTree;
        AnimatorState charge = machine.AddState("Charge", new Vector3(500, 250));
        charge.motion = CreateDirectionalTree("Player Charge Directions", chargeClips, positions);
        charge.speedParameterActive = true;
        charge.speedParameter = "ChargeAnimationSpeed";
        AnimatorState parry = machine.AddState("Parry", new Vector3(250, 250));
        parry.motion = CreateDirectionalTree("Player Parry Directions", parryClips, positions);
        AnimatorState guard = machine.AddState("Guard", new Vector3(250, 420));
        guard.motion = CreateDirectionalTree("Player Guard Directions", guardClips, positions);
        machine.defaultState = idle;

        AddTransition(idle, move, "IsMoving", true);
        AddTransition(move, idle, "IsMoving", false);
        AnimatorStateTransition toCharge = machine.AddAnyStateTransition(charge);
        toCharge.hasExitTime = false;
        toCharge.duration = 0f;
        toCharge.canTransitionToSelf = false;
        toCharge.AddCondition(AnimatorConditionMode.If, 0f, "IsDashing");
        AnimatorStateTransition toParry = machine.AddAnyStateTransition(parry);
        toParry.hasExitTime = false;
        toParry.duration = 0f;
        toParry.canTransitionToSelf = false;
        toParry.AddCondition(AnimatorConditionMode.If, 0f, "Parry");
        AnimatorStateTransition fromParry = parry.AddTransition(idle);
        fromParry.hasExitTime = true;
        fromParry.exitTime = 1f;
        fromParry.duration = 0.02f;
        AnimatorStateTransition toGuard = machine.AddAnyStateTransition(guard);
        toGuard.hasExitTime = false;
        toGuard.duration = 0.02f;
        toGuard.canTransitionToSelf = false;
        toGuard.AddCondition(AnimatorConditionMode.If, 0f, "IsGuarding");
        AnimatorStateTransition fromGuard = guard.AddTransition(idle);
        fromGuard.hasExitTime = false;
        fromGuard.duration = 0.02f;
        fromGuard.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsGuarding");
        AnimatorStateTransition fromCharge = charge.AddTransition(idle);
        fromCharge.hasExitTime = false;
        fromCharge.duration = 0.04f;
        fromCharge.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsDashing");

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        Debug.Log($"Player animation controller generated: {controllerPath}. Idle, Dash movement, Charge dash, Parry, and Guard clips are configured in eight directions.");
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredEditMode)
            EditorApplication.delayCall += EnsureControllerHasParry;
    }

    private static void EnsureControllerHasParry()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(OutputRoot + "/Player.controller");
        if (controller != null && controller.parameters.Any(parameter => parameter.name == "IsGuarding")) return;
        GenerateController();
    }

    private static BlendTree CreateDirectionalTree(string name, IReadOnlyList<AnimationClip> clips, IReadOnlyList<Vector2> positions)
    {
        var tree = new BlendTree
        {
            name = name,
            blendType = BlendTreeType.FreeformDirectional2D,
            blendParameter = "FacingX",
            blendParameterY = "FacingY",
            useAutomaticThresholds = false
        };
        for (int i = 0; i < clips.Count; i++) tree.AddChild(clips[i], positions[i]);
        AssetDatabase.AddObjectToAsset(tree, OutputRoot + "/Player.controller");
        return tree;
    }

    private static AnimationClip CreateClip(string clipName, string sourceFolder, string direction, bool loop, float frameRate)
    {
        string directory = ArtRoot + "/" + sourceFolder;
        string[] framePaths = Directory.GetFiles(directory, direction + "_*.png")
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase).ToArray();
        if (framePaths.Length == 0) throw new InvalidOperationException($"No frames found for {sourceFolder}/{direction}.");

        var keys = new ObjectReferenceKeyframe[framePaths.Length];
        for (int i = 0; i < framePaths.Length; i++)
        {
            string assetPath = framePaths[i].Replace('\\', '/');
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (sprite == null) throw new InvalidOperationException($"Could not load Sprite at {assetPath}.");
            keys[i] = new ObjectReferenceKeyframe { time = i / frameRate, value = sprite };
        }

        var clip = new AnimationClip { name = clipName, frameRate = frameRate };
        AnimationUtility.SetObjectReferenceCurve(clip, EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite"), keys);
        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        string clipPath = OutputRoot + "/" + clipName + ".anim";
        AssetDatabase.DeleteAsset(clipPath);
        AssetDatabase.CreateAsset(clip, clipPath);
        return clip;
    }

    private static void AddTransition(AnimatorState source, AnimatorState destination, string parameter, bool value)
    {
        AnimatorStateTransition transition = source.AddTransition(destination);
        transition.hasExitTime = false;
        transition.duration = 0.05f;
        transition.AddCondition(value ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0f, parameter);
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
        string name = Path.GetFileName(path);
        if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, name);
    }

    private static string ToPascal(string direction) => string.Concat(direction.Split('-').Select(part => char.ToUpperInvariant(part[0]) + part.Substring(1)));
}
