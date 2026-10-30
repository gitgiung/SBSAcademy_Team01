using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

public class ResourceLoadingTests
{
    private object resources;
    private PreparationCoroutineHost host;
    private ResourceLocationMap locator;
    private TestAssetProvider provider;
    private readonly List<Object> assets = new List<Object>();
    private Action<UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle, Exception> previousErrorHandler;

    [UnitySetUp]
    public IEnumerator Setup()
    {
        var initialization = Addressables.InitializeAsync(false);
        yield return initialization;
        Addressables.Release(initialization);
        resources = Activator.CreateInstance(Type.GetType("ResourceManager, Assembly-CSharp", true));
        host = new GameObject("@ResourceLoadingTest").AddComponent<PreparationCoroutineHost>();
        locator = new ResourceLocationMap("ResourceLoadingTests");
        provider = new TestAssetProvider();
        Addressables.ResourceManager.ResourceProviders.Add(provider);
        Addressables.AddResourceLocator(locator);
        previousErrorHandler = UnityEngine.ResourceManagement.ResourceManager.ExceptionHandler;
        UnityEngine.ResourceManagement.ResourceManager.ExceptionHandler = (operation, error) => { };
    }

    [UnityTearDown]
    public IEnumerator Cleanup()
    {
        provider.CompleteAll();
        yield return null;
        Clear();
        yield return null;
        Object.Destroy(host.gameObject);
        Addressables.RemoveResourceLocator(locator);
        Addressables.ResourceManager.ResourceProviders.Remove(provider);
        UnityEngine.ResourceManagement.ResourceManager.ExceptionHandler = previousErrorHandler;
        foreach (Object asset in assets)
            if (asset != null) Object.Destroy(asset);
        assets.Clear();
    }

    private TextAsset AddText(string key, string label)
    {
        var asset = new TextAsset("fixture:" + key);
        AddAsset(asset, key, label);
        return asset;
    }

    private void AddAsset(Object asset, string key, string label)
    {
        assets.Add(asset);
        string id = provider.ProviderId + ":" + assets.Count;
        provider.Assets[id] = asset;
        locator.Add(label, new ResourceLocationBase(key, id, provider.ProviderId, asset.GetType()));
    }

    private IEnumerator Preload(string label, Action<bool, string> completed)
        => (IEnumerator)resources.GetType().GetMethod("PreloadAsync").Invoke(resources, new object[] { label, completed });

    private void LoadAll(string label, Action<string, int, int> progress, Action<bool, string> completed = null, Type type = null)
    {
        int parameterCount = completed == null ? 2 : 3;
        MethodInfo method = resources.GetType().GetMethods().FirstOrDefault(info =>
            info.Name == "LoadAllAsync" && info.GetParameters().Length == parameterCount);
        Assert.That(method, Is.Not.Null, "LoadAllAsync must expose explicit success/failure completion.");
        object[] arguments = completed == null
            ? new object[] { label, progress } : new object[] { label, progress, completed };
        method.MakeGenericMethod(type ?? typeof(Object)).Invoke(resources, arguments);
    }

    private T Load<T>(string key) where T : Object
        => (T)resources.GetType().GetMethod("Load").MakeGenericMethod(typeof(T)).Invoke(resources, new object[] { key });

    private void Clear() => resources.GetType().GetMethod("Clear").Invoke(resources, null);

    private IEnumerator WaitUntil(Func<bool> condition)
    {
        for (int frame = 0; frame < 30 && !condition(); frame++) yield return null;
        Assert.That(condition(), Is.True, "Resource request must finish within the test's frame budget.");
    }

    [UnityTest]
    public IEnumerator LegacyConcurrentRequestsBothNotifyAndCacheOneAsset()
    {
        TextAsset asset = AddText("shared", "shared-label");
        provider.Delay = true;
        int first = 0, second = 0;
        LoadAll("shared-label", (key, count, total) => first++);
        LoadAll("shared-label", (key, count, total) => second++);
        yield return WaitUntil(() => provider.PendingCount != 0);
        yield return null;
        provider.CompleteAll();
        yield return WaitUntil(() => first == 1 && second == 1);
        Assert.That(Load<TextAsset>("shared"), Is.SameAs(asset));
        Assert.That(provider.ProvidedCount, Is.EqualTo(1));
        Clear();
        yield return null;
        Assert.That(provider.ReleasedCount, Is.EqualTo(1));
    }

    [UnityTest]
    public IEnumerator MixedCoroutineAndLegacyRequestsShareTheSameLoad()
    {
        AddText("shared", "mixed-label");
        provider.Delay = true;
        bool coroutineDone = false;
        int legacyDone = 0;
        host.StartCoroutine(Preload("mixed-label", (ok, error) => { Assert.That(ok, Is.True); coroutineDone = true; }));
        LoadAll("mixed-label", (key, count, total) => legacyDone++);
        yield return WaitUntil(() => provider.PendingCount != 0);
        yield return null;
        provider.CompleteAll();
        yield return WaitUntil(() => coroutineDone && legacyDone == 1);
        Clear();
        yield return null;
        Assert.That(provider.ProvidedCount, Is.EqualTo(1));
        Assert.That(provider.ReleasedCount, Is.EqualTo(1));
    }

    [UnityTest]
    public IEnumerator ClearDuringOverlappingCoroutinesReleasesEveryAcquisition()
    {
        AddText("shared", "clear-label");
        provider.Delay = true;
        int finished = 0;
        Action<bool, string> completed = (ok, error) => { Assert.That(ok, Is.False); finished++; };
        host.StartCoroutine(Preload("clear-label", completed));
        host.StartCoroutine(Preload("clear-label", completed));
        yield return WaitUntil(() => provider.PendingCount != 0);
        yield return null;
        Clear();
        provider.CompleteAll();
        yield return WaitUntil(() => finished == 2);
        yield return null;
        Assert.That(Load<TextAsset>("shared"), Is.Null);
        Assert.That(provider.ReleasedCount, Is.EqualTo(1), "No shared asset reference may remain after Clear.");
    }

    [UnityTest]
    public IEnumerator LegacyEmptyLabelStillNotifiesZeroProgress()
    {
        int notifications = 0;
        // Legacy API reports failure to the Console; its progress callback must still finish.
        // Unity serializes Regex.ToString() and reconstructs it without RegexOptions.
        LogAssert.Expect(LogType.Error, new Regex("^Resource load failed:"));
        LoadAll("does-not-exist", (key, count, total) =>
        {
            Assert.That(count, Is.EqualTo(0));
            Assert.That(total, Is.EqualTo(0));
            notifications++;
        });
        yield return WaitUntil(() => notifications == 1);
    }

    [UnityTest]
    public IEnumerator EmptyLabelReportsFailureExactlyOnce()
    {
        int completed = 0;
        LoadAll("does-not-exist", null, (ok, error) =>
        {
            Assert.That(ok, Is.False);
            Assert.That(error, Is.Not.Null.And.Not.Empty);
            completed++;
        });
        yield return WaitUntil(() => completed == 1);
        yield return null;
        Assert.That(completed, Is.EqualTo(1));
    }

    [UnityTest]
    public IEnumerator FailedLoadCanRetryAndOnlySuccessfulResultIsCached()
    {
        TextAsset asset = AddText("retry", "retry-label");
        provider.Fail = true;
        bool? success = null;
        yield return Preload("retry-label", (ok, error) => success = ok);
        Assert.That(success, Is.False);
        Assert.That(Load<TextAsset>("retry"), Is.Null);
        provider.Fail = false;
        yield return Preload("retry-label", (ok, error) => success = ok);
        Assert.That(success, Is.True);
        Assert.That(Load<TextAsset>("retry"), Is.SameAs(asset));
        Assert.That(provider.ProvidedCount, Is.EqualTo(2));
    }

    [UnityTest]
    public IEnumerator CachedRequestStillReportsItsOwnProgressAndCompletion()
    {
        AddText("cached", "cached-label");
        yield return Preload("cached-label", (ok, error) => Assert.That(ok, Is.True));
        int progress = 0;
        bool done = false;
        LoadAll("cached-label", (key, count, total) =>
        {
            Assert.That(key, Is.EqualTo("cached"));
            Assert.That(count, Is.EqualTo(1));
            Assert.That(total, Is.EqualTo(1));
            progress++;
        }, (ok, error) => { Assert.That(ok, Is.True); done = true; });
        yield return WaitUntil(() => done);
        Assert.That(progress, Is.EqualTo(1));
        Assert.That(provider.ProvidedCount, Is.EqualTo(1));
    }

    [UnityTest]
    public IEnumerator CachedAddressCollisionFailsInsteadOfReturningAnotherAsset()
    {
        AddText("collision", "text-label");
        var other = new GameObject("fixture-prefab");
        other.SetActive(false);
        AddAsset(other, "collision", "object-label");
        yield return Preload("text-label", (ok, error) => Assert.That(ok, Is.True));
        bool? success = null;
        yield return Preload("object-label", (ok, error) => success = ok);
        Assert.That(success, Is.False);
        Assert.That(Load<GameObject>("collision"), Is.Null);
    }

    [UnityTest]
    public IEnumerator ThrowingProgressCallbackDoesNotBlockOtherConsumers()
    {
        AddText("shared", "callback-label");
        provider.Delay = true;
        int finished = 0;
        LogAssert.Expect(LogType.Exception, new Regex("InvalidOperationException: Fixture callback failure"));
        LoadAll("callback-label", (key, count, total) => throw new InvalidOperationException("Fixture callback failure"),
            (ok, error) => { Assert.That(ok, Is.True); finished++; });
        LoadAll("callback-label", null, (ok, error) => { Assert.That(ok, Is.True); finished++; });
        yield return WaitUntil(() => provider.PendingCount != 0);
        yield return null;
        provider.CompleteAll();
        yield return WaitUntil(() => finished == 2);
    }

    [UnityTest]
    public IEnumerator ProgressCanClearAndStartANewRequestWithoutLosingIt()
    {
        AddText("old", "old-label");
        TextAsset fresh = AddText("fresh", "fresh-label");
        bool oldFailed = false, newDone = false;
        LoadAll("old-label", (key, count, total) =>
        {
            Clear();
            LoadAll("fresh-label", null, (ok, error) => { Assert.That(ok, Is.True); newDone = true; });
        }, (ok, error) => oldFailed = !ok);
        yield return WaitUntil(() => newDone);
        Assert.That(oldFailed, Is.True);
        Assert.That(Load<TextAsset>("old"), Is.Null);
        Assert.That(Load<TextAsset>("fresh"), Is.SameAs(fresh));
    }

    private void AddDependencyVersions()
    {
        string prefix = provider.ProviderId;
        foreach (string id in new[] { "main", "old-dependency", "new-dependency" })
        {
            var asset = new TextAsset(id);
            assets.Add(asset);
            provider.Assets[prefix + id] = asset;
        }
        var oldDependency = new ResourceLocationBase("dependency", prefix + "old-dependency", prefix, typeof(TextAsset));
        var newDependency = new ResourceLocationBase("dependency", prefix + "new-dependency", prefix, typeof(TextAsset));
        locator.Add("old-version", new ResourceLocationBase("versioned", prefix + "main", prefix, typeof(TextAsset), oldDependency));
        locator.Add("new-version", new ResourceLocationBase("versioned", prefix + "main", prefix, typeof(TextAsset), newDependency));
    }

    [UnityTest]
    public IEnumerator ChangedDependenciesConflictWithTheCachedLocation()
    {
        AddDependencyVersions();
        yield return Preload("old-version", (ok, error) => Assert.That(ok, Is.True));
        bool? success = null;
        yield return Preload("new-version", (ok, error) => success = ok);
        Assert.That(success, Is.False, "A changed bundle dependency is not the same resource location.");
    }

    [UnityTest]
    public IEnumerator ChangedDependenciesConflictWithAPendingLocation()
    {
        AddDependencyVersions();
        provider.Delay = true;
        bool? first = null, second = null;
        host.StartCoroutine(Preload("old-version", (ok, error) => first = ok));
        host.StartCoroutine(Preload("new-version", (ok, error) => second = ok));
        for (int frame = 0; frame < 30 && (!first.HasValue || !second.HasValue); frame++)
        {
            provider.CompleteAll();
            yield return null;
        }
        Assert.That(first, Is.True);
        Assert.That(second, Is.False);
    }

    [UnityTest]
    public IEnumerator SpriteAndItsOriginalTexturePreferTheSprite()
    {
        var texture = new Texture2D(2, 2);
        var sprite = Sprite.Create(texture, new Rect(0, 0, 2, 2), Vector2.zero);
        assets.Add(texture);
        assets.Add(sprite);
        string id = provider.ProviderId + ":sprite-source";
        provider.Assets[id] = sprite;
        locator.Add("sprite-source", new ResourceLocationBase("image", id, provider.ProviderId, typeof(Texture2D)));
        locator.Add("sprite-source", new ResourceLocationBase("image", id, provider.ProviderId, typeof(Sprite)));
        yield return Preload("sprite-source", (ok, error) => Assert.That(ok, Is.True));
        Assert.That(Load<Sprite>("image"), Is.SameAs(sprite));
        Assert.That(provider.ProvidedCount, Is.EqualTo(1));
    }

    [UnityTest]
    public IEnumerator UnrelatedSpriteAndTextureWithOneAddressFail()
    {
        var texture = new Texture2D(2, 2);
        var sprite = Sprite.Create(texture, new Rect(0, 0, 2, 2), Vector2.zero);
        AddAsset(texture, "image", "unrelated-images");
        AddAsset(sprite, "image", "unrelated-images");
        bool? success = null;
        yield return Preload("unrelated-images", (ok, error) => success = ok);
        Assert.That(success, Is.False);
        Assert.That(Load<Sprite>("image"), Is.Null);
        Assert.That(provider.ProvidedCount, Is.EqualTo(0));
    }

    [UnityTest]
    public IEnumerator GenericLoadFiltersOutOtherAssetTypes()
    {
        TextAsset text = AddText("text-only", "typed-label");
        var other = new GameObject("fixture-other");
        other.SetActive(false);
        AddAsset(other, "other-type", "typed-label");
        bool done = false;
        LoadAll("typed-label", (key, count, total) =>
        {
            Assert.That(key, Is.EqualTo("text-only"));
            Assert.That(total, Is.EqualTo(1));
        }, (ok, error) => { Assert.That(ok, Is.True); done = true; }, typeof(TextAsset));
        yield return WaitUntil(() => done);
        Assert.That(Load<TextAsset>("text-only"), Is.SameAs(text));
        Assert.That(Load<GameObject>("other-type"), Is.Null);
    }

    private sealed class TestAssetProvider : ResourceProviderBase
    {
        public readonly Dictionary<string, Object> Assets = new Dictionary<string, Object>();
        private readonly List<ProvideHandle> pending = new List<ProvideHandle>();
        public int PendingCount => pending.Count;
        public int ProvidedCount;
        public int ReleasedCount;
        public bool Delay;
        public bool Fail;

        public TestAssetProvider() { m_ProviderId = "ResourceTest-" + Guid.NewGuid().ToString("N"); }
        public override void Provide(ProvideHandle handle)
        {
            ProvidedCount++;
            if (Delay) pending.Add(handle);
            else Complete(handle);
        }
        public void CompleteAll()
        {
            ProvideHandle[] handles = pending.ToArray();
            pending.Clear();
            foreach (ProvideHandle handle in handles) Complete(handle);
        }
        private void Complete(ProvideHandle handle)
        {
            handle.Complete<Object>(Fail ? null : Assets[handle.Location.InternalId], !Fail,
                Fail ? new InvalidOperationException("Fixture load failure") : null);
        }
        public override void Release(IResourceLocation location, object asset) { ReleasedCount++; }
    }
}
