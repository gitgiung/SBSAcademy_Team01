using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class SoulPipelineValidateMenu
{
    [MenuItem("Tools/Soul Pipeline/Validate Required Files")]
    public static void Validate()
    {
        string[] required =
        {
            "Assets/9.Scenes/SoulArena3D.unity",
            "Assets/0.Scripts/Game/SoulPipeline/SoulArenaBootstrap.cs",
            "Assets/0.Scripts/Game/SoulPipeline/SoulBossApproachSlash.cs",
            "Assets/0.Scripts/Game/SoulPipeline/SoulWeaponHitbox3D.cs",
            "Assets/0.Scripts/Game/SoulPipeline/SoulPipelineAvatar.cs",
            "Assets/0.Scripts/Game/SoulPipeline/SoulArenaWaveDirector.cs",
            "Assets/0.Scripts/Game/SoulPipeline/SoulArenaBossTelegraphHud.cs",
            "Assets/0.Scripts/Game/SoulPipeline/SoulArenaTrashMob.cs",
            "Assets/0.Scripts/Game/SoulPipeline/SoulArenaFallbackFactory.cs",
            "Assets/2.Resources/Art/SoulPipeline/Humanoid/KinemIQ_Mannequin.fbx",
            "Assets/Editor/Settings/UniversalRenderer.asset",
            "docs/AI/SoulPipeline-Progress.md"
        };

        string[] missing = required.Where(p => !File.Exists(p)).ToArray();
        if (missing.Length == 0)
            EditorUtility.DisplayDialog("Soul Pipeline", $"OK {required.Length}/{required.Length}", "OK");
        else
            EditorUtility.DisplayDialog("Soul Pipeline", "Missing:\n" + string.Join("\n", missing), "OK");
    }
}
