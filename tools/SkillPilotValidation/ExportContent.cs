using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;

public static class SkillPilotExport
{
    public static void Export()
    {
        string copy = Environment.GetEnvironmentVariable("SBSA_PILOT_BUILD_COPY");
        if (string.IsNullOrEmpty(copy) || Path.GetFullPath(copy) != Directory.GetParent(Application.dataPath).FullName)
            throw new InvalidOperationException("콘텐츠 빌드는 명시한 검증 복사본에서만 수행합니다.");
        string output = Environment.GetEnvironmentVariable("SBSA_PILOT_CONTENT_OUTPUT");
        string origin = Environment.GetEnvironmentVariable("SBSA_PILOT_ORIGIN") ?? "http://127.0.0.1:18763";
        if (string.IsNullOrWhiteSpace(output)) throw new InvalidOperationException("콘텐츠 출력 경로가 없습니다.");
        string platform = EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android ? "Android" : "Windows";
        string server = Path.Combine(output, platform);
        Directory.CreateDirectory(server);
        var settings = AddressableAssetSettings.Create("Assets/SkillPilotBuild/Content", "Settings", true, true);
        AddressableAssetSettingsDefaultObject.Settings = settings;
        settings.BuildRemoteCatalog = true;
        settings.DisableCatalogUpdateOnStartup = true;
        settings.OverridePlayerVersion = "skill-pilot-v1";
        settings.profileSettings.SetValue(settings.activeProfileId, "Remote.BuildPath", server);
        string remoteUrl = origin.TrimEnd('/') + "/content/" + platform;
        settings.profileSettings.SetValue(settings.activeProfileId, "Remote.LoadPath", remoteUrl);
        settings.RemoteCatalogBuildPath.SetVariableByName(settings, "Remote.BuildPath");
        settings.RemoteCatalogLoadPath.SetVariableByName(settings, "Remote.LoadPath");
        var group = settings.CreateGroup("Skill Pilot", false, false, false, null, typeof(BundledAssetGroupSchema), typeof(ContentUpdateGroupSchema));
        var schema = group.GetSchema<BundledAssetGroupSchema>();
        schema.BuildPath.SetVariableByName(settings, "Remote.BuildPath");
        schema.LoadPath.SetVariableByName(settings, "Remote.LoadPath");
        schema.BundleMode = BundledAssetGroupSchema.BundlePackingMode.PackSeparately;
        schema.UseAssetBundleCache = true;
        schema.UseAssetBundleCrc = true;
        foreach (string address in new[] { SkillPilotContent.TableAddress, SkillPilotContent.TextureAddress, SkillPilotContent.FramesAddress, SkillPilotContent.IconAddress })
        {
            string guid = AssetDatabase.AssetPathToGUID(SkillPilotContent.AssetPath(address));
            if (string.IsNullOrEmpty(guid)) throw new InvalidOperationException("에셋 GUID가 없습니다: " + address);
            var entry = settings.CreateOrMoveEntry(guid, group);
            entry.address = address;
            entry.SetLabel("skill-pilot", true, true);
        }
        AssetDatabase.SaveAssets();
        bool layout = ProjectConfigData.GenerateBuildLayout;
        try
        {
            ProjectConfigData.GenerateBuildLayout = true;
            AddressableAssetSettings.BuildPlayerContent(out var result);
            if (!string.IsNullOrEmpty(result.Error)) throw new InvalidOperationException(result.Error);
        }
        finally { ProjectConfigData.GenerateBuildLayout = layout; }
        var catalog = Directory.GetFiles(server, "catalog*.bin").Single();
        string digest;
        using (var sha = SHA256.Create()) digest = BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(catalog))).Replace("-", "").ToLowerInvariant();
        var release = new RemoteContentRelease
        {
            formatVersion = 1, platform = platform, appCompatibility = SkillPilotContent.Profile,
            contentVersion = "skill-pilot-v1", contentProfile = SkillPilotContent.Profile,
            requiredLabel = "skill-pilot", catalogUrl = remoteUrl + "/" + Path.GetFileName(catalog), catalogSha256 = digest
        };
        File.WriteAllText(Path.Combine(server, "release.json"), JsonUtility.ToJson(release, true));
        string state = UnityEditor.AddressableAssets.Build.ContentUpdateScript.GetContentStateDataPath(false, settings);
        if (!File.Exists(state)) throw new InvalidOperationException("콘텐츠 업데이트 기준 파일이 없습니다: " + state);
        File.Copy(state, Path.Combine(server, "addressables_content_state.bin"), true);
        string layoutRoot = Path.Combine(copy, "Library", "com.unity.addressables");
        if (Directory.Exists(layoutRoot))
        {
            var latest = Directory.GetFiles(layoutRoot, "buildlayout*.json", SearchOption.AllDirectories).OrderByDescending(File.GetLastWriteTimeUtc).FirstOrDefault();
            if (latest != null) File.Copy(latest, Path.Combine(server, "content-buildlayout.json"), true);
        }
        // 앱에는 원격 기본 카탈로그를 넣지 않는다. 검증된 카탈로그만 수동 등록한다.
        var bootstrap = AddressableAssetSettings.Create("Assets/SkillPilotBuild/Bootstrap", "Settings", true, true);
        bootstrap.BuildRemoteCatalog = false;
        bootstrap.DisableCatalogUpdateOnStartup = true;
        AddressableAssetSettingsDefaultObject.Settings = bootstrap;
        AddressableAssetSettings.BuildPlayerContent(out var bootstrapResult);
        if (!string.IsNullOrEmpty(bootstrapResult.Error)) throw new InvalidOperationException(bootstrapResult.Error);
        var config = RemoteContentSettings.Load();
        if (config == null)
        {
            config = ScriptableObject.CreateInstance<RemoteContentSettings>();
            AssetDatabase.CreateAsset(config, "Assets/Resources/RemoteContentSettings.asset");
        }
        var serialized = new SerializedObject(config);
        serialized.FindProperty("useRemoteContent").boolValue = true;
        serialized.FindProperty("releaseUrl").stringValue = remoteUrl + "/release.json";
        serialized.FindProperty("appCompatibility").stringValue = SkillPilotContent.Profile;
        serialized.FindProperty("allowDevelopmentHttp").boolValue = true;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.SaveAssets();
        // 개발 APK가 ADB reverse로 받은 로컬 서버를 사용할 수 있게 복사본에서만 허용한다.
        PlayerSettings.insecureHttpOption = InsecureHttpOption.DevelopmentOnly;
    }
}
