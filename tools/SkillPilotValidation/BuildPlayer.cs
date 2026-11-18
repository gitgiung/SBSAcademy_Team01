using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class SkillPilotBuildPlayer
{
    [Serializable] private sealed class Summary
    {
        public string result;
        public long bytes;
        public long apkBytes;
        public double seconds;
        public string[] packedSourceAssets;
    }

    public static void Build()
    {
        string expectedCopy = Environment.GetEnvironmentVariable("SBSA_PILOT_BUILD_COPY");
        string actualCopy = Directory.GetParent(Application.dataPath).FullName;
        if (string.IsNullOrEmpty(expectedCopy) || Path.GetFullPath(expectedCopy) != actualCopy)
            throw new InvalidOperationException("원본이 아닌 명시적인 검증 복사본에서만 실행합니다.");
        string output = Environment.GetEnvironmentVariable("SBSA_PILOT_OUTPUT");
        if (string.IsNullOrEmpty(output)) throw new InvalidOperationException("APK 출력 경로가 없습니다.");
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        EditorUserBuildSettings.buildAppBundle = false;
        PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android, "com.sbsacademy.team01.skillpilot");
        PlayerSettings.Android.useCustomKeystore = false;
        // Gradle의 기존 APK를 패치하면 대체된 ZIP 데이터가 남아 파일 크기 비교를 왜곡할 수 있다.
        // 네이티브 컴파일 캐시는 유지하고 이 검증 복사본의 Gradle APK 출력만 다시 패키징한다.
        string gradleRoot = Path.GetFullPath(Path.Combine(actualCopy, "Library/Bee/Android/Prj/IL2CPP/Gradle"));
        if (Directory.Exists(gradleRoot))
            foreach (string apk in Directory.GetFiles(gradleRoot, "*.apk", SearchOption.AllDirectories))
            {
                if (!Path.GetFullPath(apk).StartsWith(gradleRoot + Path.DirectorySeparatorChar))
                    throw new InvalidOperationException("Gradle APK 정리 경로 오류");
                File.Delete(apk);
            }
        var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = scenes, locationPathName = output, target = BuildTarget.Android,
            options = BuildOptions.Development | BuildOptions.CompressWithLz4HC
        });
        var summary = new Summary
        {
            result = report.summary.result.ToString(), bytes = (long)report.summary.totalSize,
            apkBytes = File.Exists(output) ? new FileInfo(output).Length : 0,
            seconds = report.summary.totalTime.TotalSeconds,
            packedSourceAssets = report.packedAssets.SelectMany(p => p.contents)
                .Select(p => p.sourceAssetPath).Where(p => !string.IsNullOrEmpty(p)).Distinct().OrderBy(p => p).ToArray()
        };
        File.WriteAllText(output + ".report.json", JsonUtility.ToJson(summary, true));
        if (report.summary.result != BuildResult.Succeeded) throw new InvalidOperationException("Android APK 빌드 실패: " + report.summary.result);
    }
}
