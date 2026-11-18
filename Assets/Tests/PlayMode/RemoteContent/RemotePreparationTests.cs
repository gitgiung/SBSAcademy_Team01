using System;
using System.Collections;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class RemotePreparationTests
{
    private ScriptableObject settings;
    private ScriptableObject otherSettings;
    private GameObject sceneObject;
    private GameObject canvasObject;
    private static Type Runtime(string name) => Type.GetType(name + ", Assembly-CSharp", true);
    private static readonly BindingFlags Members = BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private static string Origin => Environment.GetEnvironmentVariable("SBSA_REMOTE_ORIGIN");
    private static object Status => Runtime("Managers").GetProperty("RemoteStatus")?.GetValue(null);
    private static string State => Status?.GetType().GetProperty("State").GetValue(Status).ToString();

    [SetUp]
    public void Setup()
    {
        if (string.IsNullOrEmpty(Origin)) Assert.Ignore("RunUnity.ps1의 격리 환경에서 실행하세요.");
        settings = CreateSettings("/release.json", true);
        string fixture = Environment.GetEnvironmentVariable("SBSA_REMOTE_FIXTURE");
        Caching.ClearCachedVersion("remote-download-fixture", Hash128.Parse(File.ReadAllText(Path.Combine(fixture, "hash.txt")).Trim()));
    }

    private ScriptableObject CreateSettings(string route, bool enabled)
    {
        var config = ScriptableObject.CreateInstance(Runtime("RemoteContentSettings"));
        Set(config, "useRemoteContent", enabled);
        Set(config, "releaseUrl", Origin + route);
        Set(config, "allowDevelopmentHttp", true);
        return config;
    }
    private static void Set(object instance, string field, object value) => instance.GetType().GetField(field, Members).SetValue(instance, value);
    private static IEnumerator Prepare(ScriptableObject config)
    {
        MethodInfo method = Runtime("Managers").GetMethod("PrepareRemoteContent", new[] { Runtime("RemoteContentSettings") });
        Assert.That(method, Is.Not.Null, "영속 Managers가 원격 준비를 소유해야 합니다.");
        return Flatten((IEnumerator)method.Invoke(null, new object[] { config }));
    }
    internal static IEnumerator Flatten(IEnumerator routine)
    {
        // 수동 MoveNext 테스트에서도 Unity의 중첩 IEnumerator 실행 규칙을 따른다.
        var stack = new System.Collections.Generic.Stack<IEnumerator>();
        stack.Push(routine);
        try
        {
            while (stack.Count > 0)
            {
                IEnumerator current = stack.Peek();
                if (!current.MoveNext()) { (stack.Pop() as IDisposable)?.Dispose(); continue; }
                if (current.Current is IEnumerator nested) { stack.Push(nested); continue; }
                yield return current.Current;
            }
        }
        finally { while (stack.Count > 0) (stack.Pop() as IDisposable)?.Dispose(); }
    }
    private static bool Action(string method)
    {
        MethodInfo action = Runtime("Managers").GetMethod(method, Type.EmptyTypes);
        Assert.That(action, Is.Not.Null);
        return (bool)action.Invoke(null, null);
    }
    private static IEnumerator Until(string expected)
    {
        float deadline = Time.realtimeSinceStartup + 15f;
        while (State != expected && State != "Failed" && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.That(State, Is.EqualTo(expected), Status?.GetType().GetProperty("Error").GetValue(Status) as string);
    }

    [TearDown]
    public void Cleanup()
    {
        if (sceneObject != null) UnityEngine.Object.DestroyImmediate(sceneObject);
        if (canvasObject != null) UnityEngine.Object.DestroyImmediate(canvasObject);
        var go = GameObject.Find("@Managers");
        if (go != null) UnityEngine.Object.DestroyImmediate(go);
        if (settings != null) UnityEngine.Object.DestroyImmediate(settings);
        if (otherSettings != null) UnityEngine.Object.DestroyImmediate(otherSettings);
        var fixture = Environment.GetEnvironmentVariable("SBSA_REMOTE_FIXTURE");
        if (!string.IsNullOrEmpty(fixture)) Caching.ClearCachedVersion("remote-download-fixture", Hash128.Parse(File.ReadAllText(Path.Combine(fixture, "hash.txt")).Trim()));
    }

    [UnityTest]
    public IEnumerator DisabledSettingsPreserveLocalPreparation()
    {
        Set(settings, "useRemoteContent", false);
        yield return Prepare(settings);
        Assert.That(State, Is.EqualTo("Disabled"));
    }

    [UnityTest]
    public IEnumerator DownloadRequiresConsentAndReadyIsSeparateFromGameDataReady()
    {
        IEnumerator wait = Prepare(settings);
        wait.MoveNext();
        yield return Until("AwaitingConsent");
        string fixture = Environment.GetEnvironmentVariable("SBSA_REMOTE_FIXTURE");
        Assert.That(Caching.IsVersionCached("remote-download-fixture", Hash128.Parse(File.ReadAllText(Path.Combine(fixture, "hash.txt")).Trim())), Is.False,
            "동의 전에 콘텐츠 번들을 내려받으면 안 됩니다.");
        Assert.That((bool)Runtime("Managers").GetProperty("GameDataReady").GetValue(null), Is.False);
        Assert.That(Action("ApproveRemoteDownload"), Is.True);
        yield return wait;
        Assert.That(State, Is.EqualTo("Ready"));
        Assert.That((bool)Runtime("Managers").GetProperty("GameDataReady").GetValue(null), Is.False);
        Assert.That(Action("ApproveRemoteDownload"), Is.False);
        // 같은 세션의 두 번째 진입은 동의를 다시 묻거나 새 릴리스를 선택하지 않는다.
        yield return Prepare(settings);
        Assert.That(State, Is.EqualTo("Ready"));
    }

    [UnityTest]
    public IEnumerator AbandonedAndConcurrentWaitersDoNotStopOwnedPreparation()
    {
        IEnumerator first = Prepare(settings);
        first.MoveNext();
        (first as IDisposable)?.Dispose();
        IEnumerator second = Prepare(settings);
        second.MoveNext();
        yield return Until("AwaitingConsent");
        Action("ApproveRemoteDownload");
        yield return second;
        Assert.That(State, Is.EqualTo("Ready"));
    }

    [UnityTest]
    public IEnumerator DifferentSettingsCannotReplaceRunningRelease()
    {
        IEnumerator first = Prepare(settings);
        first.MoveNext();
        yield return Until("AwaitingConsent");
        otherSettings = CreateSettings("/different.json", true);
        IEnumerator second = Prepare(otherSettings);
        Assert.Throws<InvalidOperationException>(() => second.MoveNext());
        Assert.That(State, Is.EqualTo("AwaitingConsent"));
        Action("ApproveRemoteDownload");
        yield return first;
        Assert.That(State, Is.EqualTo("Ready"));
    }

    [UnityTest]
    public IEnumerator FailureWaitsForExplicitRetryAndCanRecover()
    {
        Set(settings, "releaseUrl", Origin + "/flaky-release.json");
        yield return Prepare(settings);
        Assert.That(State, Is.EqualTo("Failed"));
        yield return null;
        Assert.That(State, Is.EqualTo("Failed"));
        Assert.That(Action("RetryRemoteContent"), Is.True);
        yield return Until("AwaitingConsent");
        Action("ApproveRemoteDownload");
        yield return Until("Ready");
    }

    [UnityTest]
    public IEnumerator IncompatibleReleaseBlocksBeforeCatalogAndConsent()
    {
        Set(settings, "appCompatibility", "unsupported-client");
        yield return Prepare(settings);
        Assert.That(State, Is.EqualTo("Failed"));
        Assert.That(Action("ApproveRemoteDownload"), Is.False);
    }

    [UnityTest]
    public IEnumerator LoadSceneDoesNotPrepareLocalGameBeforeRemoteConsent()
    {
        IEnumerator wait = Prepare(settings);
        wait.MoveNext();
        yield return Until("AwaitingConsent");
        sceneObject = new GameObject("Remote startup scene test");
        sceneObject.AddComponent(Runtime("LoadScene"));
        yield return null;
        yield return null;
        Assert.That(Runtime("Managers").GetProperty("PreparationState").GetValue(null).ToString(), Is.EqualTo("NotStarted"));
        UnityEngine.Object.DestroyImmediate(sceneObject);
        Action("ApproveRemoteDownload");
        yield return wait;
    }

    [UnityTest]
    public IEnumerator LoadingUiShowsConsentAndButtonApprovesOwnedDownload()
    {
        IEnumerator wait = Prepare(settings);
        wait.MoveNext();
        yield return Until("AwaitingConsent");
        canvasObject = new GameObject("Remote UI test", typeof(RectTransform), typeof(Canvas));
        var spinner = new GameObject("Spinner", typeof(RectTransform));
        spinner.transform.SetParent(canvasObject.transform, false);
        Component ui = canvasObject.AddComponent(Runtime("UI_LoadScene"));
        ui.GetType().GetMethod("Init").Invoke(ui, null);
        yield return null;
        Transform panel = canvasObject.transform.Find("RemoteContentStatus");
        Assert.That(panel, Is.Not.Null);
        Assert.That(panel.gameObject.activeSelf, Is.True);
        var button = panel.Find("ActionButton").GetComponent<UnityEngine.UI.Button>();
        Assert.That(button.interactable, Is.True);
        button.onClick.Invoke();
        yield return wait;
        yield return null;
        Assert.That(State, Is.EqualTo("Ready"));
        Assert.That(panel.gameObject.activeSelf, Is.False);
    }

    [UnityTest]
    public IEnumerator ResourceClearRepreparesSameReleaseWithoutRedownloadingValidCache()
    {
        IEnumerator first = Prepare(settings);
        first.MoveNext();
        yield return Until("AwaitingConsent");
        Action("ApproveRemoteDownload");
        yield return first;
        object resource = Runtime("Managers").GetProperty("Resource").GetValue(null);
        resource.GetType().GetMethod("Clear").Invoke(resource, null);
        yield return Prepare(settings);
        Assert.That(State, Is.EqualTo("Ready"));
        Assert.That((long)Status.GetType().GetProperty("RequiredBytes").GetValue(Status), Is.Zero);
    }

    [UnityTest]
    public IEnumerator ResourceClearWhileAwaitingConsentFailsAndExplicitRetryRestoresPrompt()
    {
        IEnumerator wait = Prepare(settings);
        wait.MoveNext();
        yield return Until("AwaitingConsent");
        object resource = Runtime("Managers").GetProperty("Resource").GetValue(null);
        resource.GetType().GetMethod("Clear").Invoke(resource, null);
        yield return wait;
        Assert.That(State, Is.EqualTo("Failed"));
        Assert.That(Action("RetryRemoteContent"), Is.True);
        yield return Until("AwaitingConsent");
        Action("ApproveRemoteDownload");
        yield return Until("Ready");
    }

    [UnityTest]
    public IEnumerator LoadingUiRetryButtonRestartsFailedPreparation()
    {
        Set(settings, "releaseUrl", Origin + "/ui-flaky-release.json");
        yield return Prepare(settings);
        Assert.That(State, Is.EqualTo("Failed"));
        canvasObject = new GameObject("Remote UI retry test", typeof(RectTransform), typeof(Canvas));
        new GameObject("Spinner", typeof(RectTransform)).transform.SetParent(canvasObject.transform, false);
        Component ui = canvasObject.AddComponent(Runtime("UI_LoadScene"));
        ui.GetType().GetMethod("Init").Invoke(ui, null);
        yield return null;
        var button = canvasObject.transform.Find("RemoteContentStatus/ActionButton").GetComponent<UnityEngine.UI.Button>();
        Assert.That(button.GetComponentInChildren<TMPro.TMP_Text>().text, Is.EqualTo("다시 시도"));
        button.onClick.Invoke();
        yield return Until("AwaitingConsent");
        button.onClick.Invoke();
        yield return Until("Ready");
    }
}
