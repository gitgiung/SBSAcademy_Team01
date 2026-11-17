using System;
using System.IO;
using UnityEditor;
using UnityEngine;

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
    }
}
