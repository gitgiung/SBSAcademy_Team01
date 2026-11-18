using UnityEditor;
using UnityEngine;
public static class SkillPilotBuildLocal
{
    public static void Build()
    {
        var config = RemoteContentSettings.Load();
        if (config != null)
        {
            var serialized = new SerializedObject(config);
            serialized.FindProperty("useRemoteContent").boolValue = false;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssetIfDirty(config);
        }
        SkillPilotBuildPlayer.Build();
    }
    public static void BuildRemote()
    {
        var config = RemoteContentSettings.Load();
        var serialized = new SerializedObject(config);
        serialized.FindProperty("useRemoteContent").boolValue = true;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.SaveAssetIfDirty(config);
        SkillPilotBuildPlayer.Build();
    }
}
