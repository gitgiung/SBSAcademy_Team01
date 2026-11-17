using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.ResourceManagement.Util;

public static class RemoteDownloadFixture
{
    public static void Build()
    {
        string output = Path.GetFullPath(Path.Combine(Application.dataPath, "../../fixture"));
        Directory.CreateDirectory(output);
        Directory.CreateDirectory("Assets/Fixture");
        var random = new System.Random(731);
        var chars = new char[256 * 1024];
        for (int i = 0; i < chars.Length; i++) chars[i] = (char)random.Next(33, 127);
        File.WriteAllText("Assets/Fixture/payload.txt", "SBSA remote fixture\n" + new string(chars));
        AssetDatabase.ImportAsset("Assets/Fixture/payload.txt", ImportAssetOptions.ForceSynchronousImport);
        var build = new AssetBundleBuild { assetBundleName = "remote-download-fixture", assetNames = new[] { "Assets/Fixture/payload.txt" } };
        var manifest = BuildPipeline.BuildAssetBundles(output, new[] { build }, BuildAssetBundleOptions.ChunkBasedCompression,
            EditorUserBuildSettings.activeBuildTarget);
        if (manifest == null) throw new InvalidOperationException("검증 번들 빌드 실패");
        File.WriteAllText(Path.Combine(output, "hash.txt"), manifest.GetAssetBundleHash(build.assetBundleName).ToString());
        // Addressables 초기화의 로컬 빈 카탈로그를 만든다. 원본 프로젝트 설정은 사용하거나 변경하지 않는다.
        var settings = UnityEditor.AddressableAssets.Settings.AddressableAssetSettings.Create("Assets/AddressableAssetsData", "AddressableAssetSettings", true, true);
        UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.Settings = settings;
        settings.BuildRemoteCatalog = false;
        UnityEditor.AddressableAssets.Settings.AddressableAssetSettings.BuildPlayerContent(out var result);
        if (!string.IsNullOrEmpty(result.Error)) throw new InvalidOperationException(result.Error);
        var bundleName = build.assetBundleName;
        var bundlePath = Path.Combine(output, bundleName);
        var entries = new List<ContentCatalogDataEntry>
        {
            new ContentCatalogDataEntry(typeof(IAssetBundleResource), "http://127.0.0.1:18763/" + bundleName,
                typeof(AssetBundleProvider).FullName, new object[] { "remote-startup-bundle" }, null,
                new AssetBundleRequestOptions { BundleName = bundleName,
                    Hash = manifest.GetAssetBundleHash(bundleName).ToString(), BundleSize = new FileInfo(bundlePath).Length,
                    Timeout = 5, RetryCount = 0 }),
            new ContentCatalogDataEntry(typeof(TextAsset), "assets/fixture/payload.txt", typeof(BundledAssetProvider).FullName,
                new object[] { "RemoteStartupPayload", "remote-startup" }, new object[] { "remote-startup-bundle" })
        };
        var catalog = new ContentCatalogData(entries, "sbsa-startup-fixture");
        catalog.ResourceProviderData.Add(ObjectInitializationData.CreateSerializedInitializationData(typeof(AssetBundleProvider)));
        catalog.ResourceProviderData.Add(ObjectInitializationData.CreateSerializedInitializationData(typeof(BundledAssetProvider)));
        catalog.InstanceProviderData = ObjectInitializationData.CreateSerializedInitializationData(typeof(InstanceProvider));
        catalog.SceneProviderData = ObjectInitializationData.CreateSerializedInitializationData(typeof(SceneProvider));
        var catalogBytes = catalog.SerializeToByteArray();
        File.WriteAllBytes(Path.Combine(output, "catalog.bin"), catalogBytes);
        string digest;
        using (var sha = SHA256.Create()) digest = BitConverter.ToString(sha.ComputeHash(catalogBytes)).Replace("-", "").ToLowerInvariant();
        File.WriteAllText(Path.Combine(output, "catalog.sha256"), digest);
        File.WriteAllText(Path.Combine(output, "release.json"), "{\"formatVersion\":1,\"platform\":\"Windows\",\"appCompatibility\":\"1\",\"contentVersion\":\"pilot-v1\",\"catalogUrl\":\"http://127.0.0.1:18763/catalog.bin\",\"catalogSha256\":\""
            + digest + "\",\"requiredLabel\":\"remote-startup\"}");
        BuildSkillPilot(Path.Combine(output, "pilot"));
    }

    private static void BuildSkillPilot(string output)
    {
        Directory.CreateDirectory(output);
        string[] addresses = { SkillPilotContent.TableAddress, SkillPilotContent.TextureAddress, SkillPilotContent.FramesAddress, SkillPilotContent.IconAddress };
        string[] paths = Array.ConvertAll(addresses, SkillPilotContent.AssetPath);
        Type[] types = { typeof(TextAsset), typeof(Texture2D), typeof(TextAsset), typeof(Sprite) };
        var build = new AssetBundleBuild { assetBundleName = "skill-pilot-fixture", assetNames = paths };
        var manifest = BuildPipeline.BuildAssetBundles(output, new[] { build }, BuildAssetBundleOptions.ChunkBasedCompression, EditorUserBuildSettings.activeBuildTarget);
        if (manifest == null) throw new InvalidOperationException("실제 스킬 번들 빌드 실패");
        var entries = new List<ContentCatalogDataEntry>
        {
            new ContentCatalogDataEntry(typeof(IAssetBundleResource), "http://127.0.0.1:18763/pilot/" + build.assetBundleName,
                typeof(AssetBundleProvider).FullName, new object[] { "skill-pilot-bundle" }, null,
                new AssetBundleRequestOptions { BundleName = build.assetBundleName, Hash = manifest.GetAssetBundleHash(build.assetBundleName).ToString(),
                    BundleSize = new FileInfo(Path.Combine(output, build.assetBundleName)).Length, Timeout = 5, RetryCount = 0 })
        };
        for (int i = 0; i < paths.Length; i++)
            entries.Add(new ContentCatalogDataEntry(types[i], paths[i].ToLowerInvariant(), typeof(BundledAssetProvider).FullName,
                new object[] { addresses[i], "skill-pilot" }, new object[] { "skill-pilot-bundle" }));
        var catalog = new ContentCatalogData(entries, "skill-pilot-fixture");
        catalog.ResourceProviderData.Add(ObjectInitializationData.CreateSerializedInitializationData(typeof(AssetBundleProvider)));
        catalog.ResourceProviderData.Add(ObjectInitializationData.CreateSerializedInitializationData(typeof(BundledAssetProvider)));
        var bytes = catalog.SerializeToByteArray();
        File.WriteAllBytes(Path.Combine(output, "catalog.bin"), bytes);
        string hash;
        using (var sha = SHA256.Create()) hash = BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
        File.WriteAllText(Path.Combine(output, "release.json"), "{\"formatVersion\":1,\"platform\":\"Windows\",\"appCompatibility\":\"skill-pilot-v1\",\"contentVersion\":\"skill-pilot-v1\",\"contentProfile\":\"skill-pilot-v1\",\"catalogUrl\":\"http://127.0.0.1:18763/pilot/catalog.bin\",\"catalogSha256\":\"" + hash + "\",\"requiredLabel\":\"skill-pilot\"}");
        File.WriteAllText(Path.Combine(output, "hash.txt"), manifest.GetAssetBundleHash(build.assetBundleName).ToString());
    }
}
