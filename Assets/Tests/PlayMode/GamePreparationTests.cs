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
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;

public class GamePreparationTests
{
    private static Type ManagersType => Type.GetType("Managers, Assembly-CSharp", true);
    private ResourceLocationMap locator;
    private MemoryAssetProvider provider;
    private readonly List<TextAsset> assets = new List<TextAsset>();

    private static object Read(string property)
    {
        PropertyInfo info = ManagersType.GetProperty(property);
        Assert.That(info, Is.Not.Null, property + " must expose preparation status.");
        return info.GetValue(null);
    }

    private static IEnumerator Prepare(string label = "", bool required = false)
    {
        MethodInfo method = ManagersType.GetMethod("PrepareGame");
        Assert.That(method, Is.Not.Null, "Managers must prepare resources and data before scene entry.");
        return (IEnumerator)method.Invoke(null, new object[] { label, required });
    }

    private void AddSkillData(string json)
    {
        TextAsset asset = new TextAsset(json);
        assets.Add(asset);
        string id = "core-test-skill-" + assets.Count;
        provider.Assets[id] = asset;
        locator.Add("core-test-data", new ResourceLocationBase("SkillData", id, provider.ProviderId, typeof(TextAsset)));
    }

    [UnitySetUp]
    public IEnumerator Setup()
    {
        var initialization = Addressables.InitializeAsync(false);
        yield return initialization;
        Assert.That(initialization.Status, Is.EqualTo(AsyncOperationStatus.Succeeded));
        Addressables.Release(initialization);
        locator = new ResourceLocationMap("CorePreparationTests");
        provider = new MemoryAssetProvider();
        Addressables.ResourceManager.ResourceProviders.Add(provider);
        Addressables.AddResourceLocator(locator);
    }

    [UnityTearDown]
    public IEnumerator Cleanup()
    {
        provider?.CompletePending();
        foreach (GameObject root in UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (root.name == "@Managers" || root.name == "@PreparationTest")
                UnityEngine.Object.Destroy(root);
        yield return null;
        Addressables.RemoveResourceLocator(locator);
        Addressables.ResourceManager.ResourceProviders.Remove(provider);
        foreach (TextAsset asset in assets)
            UnityEngine.Object.Destroy(asset);
        assets.Clear();
    }

    [UnityTest]
    public IEnumerator EmptyOptionalStartupAllowsTestReadyWithoutClaimingGameDataReady()
    {
        yield return Prepare();
        Assert.That(Read("Initialized"), Is.True);
        Assert.That(Read("PreparationState").ToString(), Is.EqualTo("TestReady"));
        Assert.That(Read("GameDataReady"), Is.False);
        Assert.That(Read("PreparationError"), Is.Null);
    }

    [UnityTest]
    public IEnumerator MissingRequiredDataFailsAndCanRetryAsAnEmptyTest()
    {
        yield return Prepare(required: true);
        Assert.That(Read("PreparationState").ToString(), Is.EqualTo("Failed"));
        Assert.That(Read("PreparationError").ToString(), Does.Contain("SkillData"));
        Assert.That(Read("GameDataReady"), Is.False);
        yield return Prepare();
        Assert.That(Read("PreparationState").ToString(), Is.EqualTo("TestReady"));
        Assert.That(Read("PreparationError"), Is.Null);
    }

    [UnityTest]
    public IEnumerator MissingRequestedLabelFailsInsteadOfHangingOrPassing()
    {
        yield return Prepare("core-test-label-that-does-not-exist");
        Assert.That(Read("PreparationState").ToString(), Is.EqualTo("Failed"));
        Assert.That(Read("PreparationError"), Is.Not.Null.And.Not.Empty);
        Assert.That(Read("GameDataReady"), Is.False);
    }

    [UnityTest]
    public IEnumerator LoadedValidSkillDataPublishesReadyAndUsableDictionary()
    {
        AddSkillData("{\"skills\":[{\"DataId\":101,\"Name\":\"Fixture skill\"}]}");
        yield return Prepare("core-test-data", true);
        Assert.That(Read("PreparationError"), Is.Null);
        Assert.That(Read("PreparationState").ToString(), Is.EqualTo("Ready"));
        Assert.That(Read("GameDataReady"), Is.True);
        object data = Read("Data");
        IDictionary skills = (IDictionary)data.GetType().GetProperty("SkillDic").GetValue(data);
        Assert.That(skills.Contains(101), Is.True);
        Assert.That(skills.Count, Is.EqualTo(1));
    }

    [UnityTest]
    public IEnumerator MalformedSkillDataFailsEvenWhenDataIsOptional()
    {
        AddSkillData("{this is invalid json");
        yield return Prepare("core-test-data");
        Assert.That(Read("PreparationState").ToString(), Is.EqualTo("Failed"));
        Assert.That(Read("GameDataReady"), Is.False);
        Assert.That(Read("PreparationError").ToString(), Does.Contain("SkillData"));
    }

    [UnityTest]
    public IEnumerator DuplicateSkillIdsFailWithoutPublishingPartialData()
    {
        AddSkillData("{\"skills\":[{\"DataId\":101},{\"DataId\":101}]}");
        yield return Prepare("core-test-data", true);
        Assert.That(Read("PreparationState").ToString(), Is.EqualTo("Failed"));
        object data = Read("Data");
        IDictionary skills = (IDictionary)data.GetType().GetProperty("SkillDic").GetValue(data);
        Assert.That(skills.Count, Is.EqualTo(0));
    }

    [UnityTest]
    public IEnumerator EmptySkillListDoesNotClaimGameReadiness()
    {
        AddSkillData("{\"skills\":[]}");
        yield return Prepare("core-test-data", true);
        Assert.That(Read("PreparationState").ToString(), Is.EqualTo("Failed"));
        Assert.That(Read("GameDataReady"), Is.False);
    }

    [UnityTest]
    public IEnumerator ConcurrentMatchingPreparationWaitsForOneLoad()
    {
        AddSkillData("{\"skills\":[{\"DataId\":101}]}");
        IEnumerator first = Prepare("core-test-data", true);
        Assert.That(first.MoveNext(), Is.True);
        IEnumerator second = Prepare("core-test-data", true);
        Assert.That(second.MoveNext(), Is.True);
        Assert.That(Read("PreparationState").ToString(), Is.EqualTo("Preparing"));
        yield return first.Current;
        yield return first;
        yield return second;
        Assert.That(Read("GameDataReady"), Is.True);
        Assert.That(provider.ProvidedCount, Is.EqualTo(1));
    }

    [UnityTest]
    public IEnumerator ResourceHandlesAreReleasedWhenManagersAreDestroyed()
    {
        AddSkillData("{\"skills\":[{\"DataId\":101}]}");
        yield return Prepare("core-test-data", true);
        Assert.That(provider.ReleasedCount, Is.EqualTo(0));
        UnityEngine.Object.Destroy(GameObject.Find("@Managers"));
        yield return null;
        Assert.That(provider.ReleasedCount, Is.EqualTo(1));
        Assert.That(Read("GameDataReady"), Is.False);
    }

    [UnityTest]
    public IEnumerator FailedAssetIsNotCachedOrReportedReady()
    {
        AddSkillData("{\"skills\":[{\"DataId\":101}]}");
        provider.FailLoad = true;
        var previousHandler = UnityEngine.ResourceManagement.ResourceManager.ExceptionHandler;
        // The preparation result is the error boundary being tested. Avoid duplicating
        // the provider's intentional failure in Unity's unexpected-log assertion.
        UnityEngine.ResourceManagement.ResourceManager.ExceptionHandler = (operation, error) => { };
        try { yield return Prepare("core-test-data", true); }
        finally { UnityEngine.ResourceManagement.ResourceManager.ExceptionHandler = previousHandler; }
        Assert.That(Read("PreparationState").ToString(), Is.EqualTo("Failed"));
        Assert.That(Read("PreparationError").ToString(), Does.Contain("SkillData"));
        object resources = Read("Resource");
        object cached = resources.GetType().GetMethod("Load").MakeGenericMethod(typeof(TextAsset))
            .Invoke(resources, new object[] { "SkillData" });
        Assert.That(cached, Is.Null);
    }

    [UnityTest]
    public IEnumerator ClearingResourcesDuringPreloadCannotPublishStaleSuccess()
    {
        AddSkillData("{\"skills\":[{\"DataId\":101}]}");
        provider.DelayLoad = true;
        var host = new GameObject("@PreparationTest").AddComponent<PreparationCoroutineHost>();
        host.StartCoroutine(Prepare("core-test-data", true));
        for (int frame = 0; frame < 20 && !provider.HasPending; frame++)
            yield return null;
        Assert.That(provider.HasPending, Is.True);
        object resources = Read("Resource");
        resources.GetType().GetMethod("Clear").Invoke(resources, null);
        provider.CompletePending();
        for (int frame = 0; frame < 20 && Read("PreparationState").ToString() == "Preparing"; frame++)
            yield return null;
        Assert.That(Read("PreparationState").ToString(), Is.EqualTo("Failed"));
        Assert.That(Read("GameDataReady"), Is.False);
        Assert.That(provider.ProvidedCount, Is.EqualTo(1));
    }

    [UnityTest]
    public IEnumerator DestroyedCallerCannotStrandSharedPreparation()
    {
        AddSkillData("{\"skills\":[{\"DataId\":101}]}");
        provider.DelayLoad = true;
        var host = new GameObject("@PreparationTest").AddComponent<PreparationCoroutineHost>();
        host.StartCoroutine(Prepare("core-test-data", true));
        for (int frame = 0; frame < 20 && !provider.HasPending; frame++)
            yield return null;
        Assert.That(provider.HasPending, Is.True);
        UnityEngine.Object.Destroy(host.gameObject);
        yield return null;
        provider.CompletePending();
        for (int frame = 0; frame < 20 && Read("PreparationState").ToString() == "Preparing"; frame++)
            yield return null;
        Assert.That(Read("PreparationState").ToString(), Is.EqualTo("Ready"));
        yield return Prepare("core-test-data", true);
        Assert.That(provider.ProvidedCount, Is.EqualTo(1));
    }

    [UnityTest]
    public IEnumerator ClearingReadyResourcesInvalidatesGameReadiness()
    {
        AddSkillData("{\"skills\":[{\"DataId\":101}]}");
        yield return Prepare("core-test-data", true);
        Assert.That(Read("GameDataReady"), Is.True);
        object resources = Read("Resource");
        resources.GetType().GetMethod("Clear").Invoke(resources, null);
        Assert.That(Read("GameDataReady"), Is.False);
        Assert.That(Read("PreparationState").ToString(), Is.EqualTo("NotStarted"));
    }

    [UnityTest]
    public IEnumerator FailedDataReinitializationInvalidatesGameReadiness()
    {
        AddSkillData("{\"skills\":[{\"DataId\":101}]}");
        yield return Prepare("core-test-data", true);
        UnityEngine.Object.Destroy(assets[0]);
        yield return null;
        object data = Read("Data");
        Assert.Throws<TargetInvocationException>(() => data.GetType().GetMethod("Init").Invoke(data, null));
        Assert.That(data.GetType().GetProperty("IsInitialized").GetValue(data), Is.False);
        Assert.That(Read("GameDataReady"), Is.False);
    }

    [UnityTest]
    public IEnumerator LoadingScenePreparationInitializesCoreForAnEmptyTest()
    {
        Component loader = CreateInactiveLoader();
        MethodInfo prepare = loader.GetType().GetMethod("PrepareGame", BindingFlags.NonPublic | BindingFlags.Instance);
        yield return (IEnumerator)prepare.Invoke(loader, null);
        Assert.That(Read("Initialized"), Is.True);
        Assert.That(Read("PreparationState").ToString(), Is.EqualTo("TestReady"));
    }

    [UnityTest]
    public IEnumerator LoadingSceneFailureKeepsTheCurrentScene()
    {
        Component loader = CreateInactiveLoader();
        FieldInfo required = loader.GetType().GetField("requireSkillData", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.That(required, Is.Not.Null, "LoadScene needs an explicit required-data policy.");
        required.SetValue(loader, true);
        string previousScene = SceneManager.GetActiveScene().path;
        LogAssert.Expect(LogType.Error, new Regex("Game preparation failed:.*SkillData"));
        MethodInfo start = loader.GetType().GetMethod("Start", BindingFlags.NonPublic | BindingFlags.Instance);
        yield return (IEnumerator)start.Invoke(loader, null);
        Assert.That(Read("PreparationState").ToString(), Is.EqualTo("Failed"));
        Assert.That(SceneManager.GetActiveScene().path, Is.EqualTo(previousScene));
    }

    private static Component CreateInactiveLoader()
    {
        GameObject host = new GameObject("@PreparationTest");
        host.SetActive(false);
        return host.AddComponent(Type.GetType("LoadScene, Assembly-CSharp", true));
    }

    private sealed class MemoryAssetProvider : ResourceProviderBase
    {
        public readonly Dictionary<string, TextAsset> Assets = new Dictionary<string, TextAsset>();
        public int ProvidedCount;
        public int ReleasedCount;
        public bool FailLoad;
        public bool DelayLoad;
        private ProvideHandle? pending;
        public bool HasPending => pending.HasValue;

        public MemoryAssetProvider()
        {
            // Addressables caches provider IDs. Each fixture must have its own ID.
            m_ProviderId = "CoreTest-" + Guid.NewGuid().ToString("N");
        }

        public override void Provide(ProvideHandle handle)
        {
            ProvidedCount++;
            if (DelayLoad)
            {
                pending = handle;
                return;
            }
            Complete(handle);
        }

        public void CompletePending()
        {
            if (!pending.HasValue)
                return;
            ProvideHandle handle = pending.Value;
            pending = null;
            Complete(handle);
        }

        private void Complete(ProvideHandle handle)
        {
            if (FailLoad)
                handle.Complete<UnityEngine.Object>(null, false, new InvalidOperationException("Fixture asset load failure"));
            else
                handle.Complete<UnityEngine.Object>(Assets[handle.Location.InternalId], true, null);
        }

        public override void Release(IResourceLocation location, object asset)
        {
            ReleasedCount++;
        }
    }
}

public sealed class PreparationCoroutineHost : MonoBehaviour { }
