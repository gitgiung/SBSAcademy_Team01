using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class SoulVerifyLaunch
{
    const string ProbeResultPath = @"C:\sv\soul-probe.txt";
    const string CaptureResultPath = @"C:\sv\soul-capture.txt";
    const string StartedKey = "SoulProbeStarted";

    static readonly bool capture;
    static readonly string resultPath;

    static SoulVerifyLaunch()
    {
        string[] args = Environment.GetCommandLineArgs();
        bool probe = Array.IndexOf(args, "-soulProbe") >= 0;
        capture = Array.IndexOf(args, "-soulCapture") >= 0;
        if (!probe && !capture)
            return;

        resultPath = capture ? CaptureResultPath : ProbeResultPath;
        EditorApplication.delayCall += Begin;
        EditorApplication.update += Watch;
    }

    static void Begin()
    {
        if (SessionState.GetBool(StartedKey, false))
            return;
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        SessionState.SetBool(StartedKey, true);
        if (File.Exists(resultPath))
            File.Delete(resultPath);

        EditorSceneManager.playModeStartScene = null;
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var go = new GameObject("SoulProbe");
        if (capture)
            go.AddComponent<SoulPipelineCaptureProbe>();
        else
            go.AddComponent<SoulPipelinePlayProbe>();
        EditorApplication.EnterPlaymode();
    }

    static void Watch()
    {
        if (!File.Exists(resultPath))
            return;

        string text = File.ReadAllText(resultPath);
        EditorApplication.Exit(text.StartsWith("PASS") ? 0 : 1);
    }
}
