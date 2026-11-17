using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>로컬 패키지만 이전 에셋의 직접 참조를 넣는다. 원격 APK에는 복제본을 넣지 않는다.</summary>
public sealed class SkillPilotBuildProcessor : IPreprocessBuildWithReport, IPostprocessBuildWithReport
{
    public const string HolderPath = "Assets/Resources/__GeneratedSkillPilotLocal.asset";
    public int callbackOrder => 0;
    private static bool generated;
    public void OnPreprocessBuild(BuildReport report)
    {
        RemoveGenerated();
        var settings = RemoteContentSettings.Load();
        if (settings != null && settings.UseRemoteContent && settings.AppCompatibility == SkillPilotContent.Profile) return;
        var local = ScriptableObject.CreateInstance<SkillPilotLocalAssets>();
        var serialized = new SerializedObject(local);
        Assign(serialized, "table", SkillPilotContent.TableAddress, typeof(TextAsset));
        Assign(serialized, "texture", SkillPilotContent.TextureAddress, typeof(Texture2D));
        Assign(serialized, "frames", SkillPilotContent.FramesAddress, typeof(TextAsset));
        Assign(serialized, "icon", SkillPilotContent.IconAddress, typeof(Sprite));
        serialized.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.CreateAsset(local, HolderPath);
        generated = true;
        AssetDatabase.SaveAssetIfDirty(local);
    }
    private static void Assign(SerializedObject serialized, string field, string address, System.Type type)
    {
        Object asset = AssetDatabase.LoadAssetAtPath(SkillPilotContent.AssetPath(address), type);
        if (asset == null) throw new BuildFailedException("필수 로컬 스킬 에셋이 없습니다: " + address);
        serialized.FindProperty(field).objectReferenceValue = asset;
    }
    private static void RemoveGenerated()
    {
        Object existing = AssetDatabase.LoadMainAssetAtPath(HolderPath);
        if (existing == null) return;
        if (!(existing is SkillPilotLocalAssets)) throw new BuildFailedException("기존 파일을 보존했습니다: " + HolderPath);
        AssetDatabase.DeleteAsset(HolderPath);
    }
    public void OnPostprocessBuild(BuildReport report)
    {
        if (generated) RemoveGenerated();
        generated = false;
    }
}
