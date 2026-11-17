using UnityEngine;

/// <summary>로컬 APK 빌드에만 포함하는 참조 묶음. 원격 APK에는 포함하지 않는다.</summary>
public sealed class SkillPilotLocalAssets : ScriptableObject
{
    [SerializeField] private TextAsset table;
    [SerializeField] private Texture2D texture;
    [SerializeField] private TextAsset frames;
    [SerializeField] private Sprite icon;
    public T Load<T>(string address) where T : Object
    {
        switch (address)
        {
            case SkillPilotContent.TableAddress: return table as T;
            case SkillPilotContent.TextureAddress: return texture as T;
            case SkillPilotContent.FramesAddress: return frames as T;
            case SkillPilotContent.IconAddress: return icon as T;
            default: return null;
        }
    }
}

public static class SkillPilotContent
{
    public const string Profile = "skill-pilot-v1";
    public const string Root = "Assets/RemoteContent/SkillPilot/";
    public const string TableAddress = "SkillPilot/CombatSkillTable";
    public const string TextureAddress = "SkillPilot/WaterArrowTexture";
    public const string FramesAddress = "SkillPilot/WaterArrowFrames";
    public const string IconAddress = "SkillPilot/Icon10025";
    public const string LocalHolderKey = "__GeneratedSkillPilotLocal";
    public static string Address(string key)
    {
        switch (key)
        {
            case "Data/CombatSkillTable": return TableAddress;
            case "LightSkills/Fx/WaterArrow": return TextureAddress;
            case "LightSkills/Fx/Frames/WaterArrow": return FramesAddress;
            case "SkillIcons/SkillIcon_10025":
            case "SkillIcons/Skill_10025": return IconAddress;
            default: return null;
        }
    }
    public static string AssetPath(string address)
    {
        switch (address)
        {
            case TableAddress: return Root + "CombatSkillTable.json";
            case TextureAddress: return Root + "WaterArrow.png";
            case FramesAddress: return Root + "Frames/WaterArrow.json";
            case IconAddress: return Root + "Skill_10025.png";
            default: return null;
        }
    }
}
