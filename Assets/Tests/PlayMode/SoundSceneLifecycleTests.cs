using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

public class SoundSceneLifecycleTests
{
    private static Type Runtime(string name) => Type.GetType(name + ", Assembly-CSharp", true);
    private static object Service(string name) => Runtime("Managers").GetProperty(name).GetValue(null);
    private static object Kind(string name) => Enum.Parse(Runtime("Define+ESound"), name);
    private static object SceneKind(string name) => Enum.Parse(Runtime("Define+EScene"), name);
    private readonly List<Object> owned = new List<Object>();
    private static object Call(object target, string name, params object[] args)
    {
        Type type = target as Type ?? target.GetType();
        return type.GetMethods().Single(m => m.Name == name && m.GetParameters().Length == args.Length
            && m.GetParameters().Select((p, i) => args[i] == null || p.ParameterType.IsInstanceOfType(args[i])).All(v => v))
            .Invoke(target is Type ? null : target, args);
    }
    private AudioClip Clip()
    {
        var clip = AudioClip.Create("lifecycle clip", 44100, 1, 44100, false);
        owned.Add(clip);
        return clip;
    }
    private static AudioSource Source(string name) => GameObject.Find("@SoundRoot").transform.Find(name).GetComponent<AudioSource>();
    private static IDictionary Resources => (IDictionary)Service("Resource").GetType()
        .GetField("_resources", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(Service("Resource"));

    [UnityTearDown]
    public IEnumerator Cleanup()
    {
        Object.Destroy(GameObject.Find("@Managers"));
        Object.Destroy(GameObject.Find("@SoundRoot"));
        foreach (var item in owned) if (item != null) Object.Destroy(item);
        owned.Clear();
        foreach (var probe in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include))
            if (probe != null && probe.GetType().Name == "SceneCleanupProbe") Object.Destroy(probe.gameObject);
        yield return null;
    }

    [Test]
    public void MissingKeyDoesNotPoisonRetryAfterResourceBecomesAvailable()
    {
        object sound = Service("Sound");
        LogAssert.Expect(LogType.Warning, "Sound clip not found: lifecycle-missing.");
        Call(sound, "Play", Kind("Bgm"), "lifecycle-missing", 1f);
        var clip = Clip();
        Resources["lifecycle-missing"] = clip;
        Call(sound, "Play", Kind("Bgm"), "lifecycle-missing", 1f);
        Assert.That(Source("Bgm").clip, Is.SameAs(clip));
    }

    [Test]
    public void BgmAppliesPitchForDirectAndKeyedPlay()
    {
        object sound = Service("Sound");
        var clip = Clip();
        Call(sound, "Play", Kind("Bgm"), clip, 0.7f);
        Assert.That(Source("Bgm").pitch, Is.EqualTo(0.7f).Within(0.001f));
        Resources["lifecycle-pitch"] = clip;
        Call(sound, "Play", Kind("Bgm"), "lifecycle-pitch", 1.2f);
        Assert.That(Source("Bgm").pitch, Is.EqualTo(1.2f).Within(0.001f));
    }

    [Test]
    public void NullClipPreservesCurrentBgm()
    {
        object sound = Service("Sound");
        var clip = Clip();
        Call(sound, "Play", Kind("Bgm"), clip, 1f);
        LogAssert.Expect(LogType.Warning, "Cannot play a null sound clip.");
        sound.GetType().GetMethod("Play", new[] { Kind("Bgm").GetType(), typeof(AudioClip), typeof(float) })
            .Invoke(sound, new object[] { Kind("Bgm"), null, 1f });
        Assert.That(Source("Bgm").clip, Is.SameAs(clip));
    }

    [Test]
    public void SoundClearBeforeInitDoesNotCreateObjectsOrThrow()
    {
        object sound = Activator.CreateInstance(Runtime("SoundManager"));
        Assert.DoesNotThrow(() => Call(sound, "Clear"));
    }

    [Test]
    public void ClearReleasesSourceClipAndResetsPitchButRetainsResource()
    {
        var clip = Clip();
        Resources["lifecycle-retained"] = clip;
        Call(Service("Sound"), "Play", Kind("Bgm"), clip, 0.5f);
        Call(Runtime("Managers"), "Clear");
        Assert.That(Source("Bgm").clip, Is.Null);
        Assert.That(Source("Bgm").pitch, Is.EqualTo(1f));
        Assert.That(Resources["lifecycle-retained"], Is.SameAs(clip));
    }

    [Test]
    public void MaxSoundKindIsRejectedWithoutIndexException()
    {
        LogAssert.Expect(LogType.Warning, "Invalid sound type: Max.");
        Assert.DoesNotThrow(() => Call(Service("Sound"), "Stop", Kind("Max")));
    }

    [UnityTest]
    public IEnumerator DestroyedAudioSourceIsRebuiltOnNextUse()
    {
        object sound = Service("Sound");
        Object.Destroy(Source("Bgm"));
        yield return null;
        var clip = Clip();
        Assert.DoesNotThrow(() => Call(sound, "Play", Kind("Bgm"), clip, 1f));
        Assert.That(Source("Bgm").clip, Is.SameAs(clip));
    }

    [UnityTest]
    public IEnumerator RebuiltSoundRootRemainsOwnedByManagers()
    {
        object sound = Service("Sound");
        Object.Destroy(GameObject.Find("@SoundRoot"));
        yield return null;
        Call(sound, "Play", Kind("Bgm"), Clip(), 1f);
        var root = GameObject.Find("@SoundRoot");
        Assert.That(root.transform.parent, Is.EqualTo(GameObject.Find("@Managers").transform));
        Object.Destroy(GameObject.Find("@Managers"));
        yield return null;
        Assert.That(root == null, Is.True);
    }

    [Test]
    public void ManagersClearCallsActiveSceneHookOnceEvenIfHookReenters()
    {
        Service("Scene");
        var go = new GameObject("scene cleanup probe");
        owned.Add(go);
        var probe = go.AddComponent(Runtime("SceneCleanupProbe"));
        probe.GetType().GetProperty("Reenter").SetValue(probe, true);
        Call(Runtime("Managers"), "Clear");
        Assert.That(probe.GetType().GetProperty("ClearCount").GetValue(probe), Is.EqualTo(1));
    }

    [UnityTest]
    public IEnumerator AsyncTransitionRunsSceneHookAndRetainsPreparedState()
    {
        object scene = Service("Scene");
        yield return (IEnumerator)Runtime("Managers").GetMethod("PrepareGame").Invoke(null, new object[] { "", false });
        var go = new GameObject("async cleanup probe");
        owned.Add(go);
        var probe = go.AddComponent(Runtime("SceneCleanupProbe"));
        var method = scene.GetType().GetMethod("LoadSceneAsync");
        Assert.That(method, Is.Not.Null);
        var operation = (AsyncOperation)method.Invoke(scene, new[] { SceneKind("MainTestScene") });
        Assert.That(probe.GetType().GetProperty("ClearCount").GetValue(probe), Is.EqualTo(1));
        yield return operation;
        Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("MainTestScene"));
        Assert.That(Runtime("Managers").GetProperty("PreparationState").GetValue(null).ToString(), Is.EqualTo("TestReady"));
    }

    [Test]
    public void InvalidSceneDoesNotClearCurrentSound()
    {
        var clip = Clip();
        Call(Service("Sound"), "Play", Kind("Bgm"), clip, 1f);
        Assert.Throws<TargetInvocationException>(() => Call(Service("Scene"), "LoadScene", SceneKind("Unknown")));
        Assert.That(Source("Bgm").clip, Is.SameAs(clip));
    }

    [UnityTest]
    public IEnumerator SyncTransitionRunsSceneHookAndClearsSound()
    {
        object scene = Service("Scene");
        var go = new GameObject("sync cleanup probe");
        owned.Add(go);
        go.AddComponent(Runtime("SceneCleanupProbe"));
        var counter = Runtime("SceneCleanupProbe").GetProperty("TotalClearCount");
        counter.SetValue(null, 0);
        var clip = Clip();
        Resources["sync-retained"] = clip;
        Call(Service("Sound"), "Play", Kind("Bgm"), clip, 1f);
        Call(scene, "LoadScene", SceneKind("MainTestScene"));
        yield return null;
        Assert.That(counter.GetValue(null), Is.EqualTo(1));
        Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("MainTestScene"));
        Assert.That(Source("Bgm").clip, Is.Null);
        Assert.That(Resources["sync-retained"], Is.SameAs(clip));
    }

    [Test]
    public void ThrowingSceneHookStillClearsSoundAndReleasesReentryGuard()
    {
        Service("Scene");
        var go = new GameObject("throwing cleanup probe");
        owned.Add(go);
        var probe = go.AddComponent(Runtime("SceneCleanupProbe"));
        var throwFlag = probe.GetType().GetProperty("ThrowOnClear");
        throwFlag.SetValue(probe, true);
        Call(Service("Sound"), "Play", Kind("Bgm"), Clip(), 1f);
        var error = Assert.Throws<TargetInvocationException>(() => Call(Runtime("Managers"), "Clear"));
        Assert.That(error.InnerException, Is.TypeOf<InvalidOperationException>());
        Assert.That(Source("Bgm").clip, Is.Null);
        throwFlag.SetValue(probe, false);
        Call(Runtime("Managers"), "Clear");
        Assert.That(probe.GetType().GetProperty("ClearCount").GetValue(probe), Is.EqualTo(2));
    }
}
